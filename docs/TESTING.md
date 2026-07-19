# Testing

VaultGuard has unit/integration tests and browser-driven UI tests, with an Allure dashboard for results.

## Test projects

| Project | Framework | What it covers |
| --- | --- | --- |
| `VaultGuard.BackEnd.Tests` | NUnit | Services, controllers, crypto, seeding, validation - the bulk of the unit/integration suite (~194 tests). **Allure-instrumented.** |
| `VaultGuard.Tests.QrLogin` | xUnit | Passkey engine (`PasskeyServiceTests`), 2FA/passkey DTOs, QR login |
| `VaultGuard.Tests.OTP` | xUnit | OTP / TOTP |
| `VaultGuard.Tests.Playwright` | MSTest + Playwright | **UI automation** against the Blazor web app (its own library) - ~56 assertion-based tests run in CI via `run-tests.yml`'s `blazor-ui-tests` job |
| `VaultGuard.Tests.UI` | - | Additional UI checks |

## Running the unit/integration tests

```bash
# Everything
dotnet test

# One project
dotnet test VaultGuard.BackEnd.Tests/VaultGuard.BackEnd.Tests.csproj

# One class / one test
dotnet test VaultGuard.Tests.QrLogin/VaultGuard.Tests.QrLogin.csproj --filter "FullyQualifiedName~PasskeyServiceTests"
dotnet test VaultGuard.BackEnd.Tests/VaultGuard.BackEnd.Tests.csproj --filter "Name=ValidateApiKey_AfterRevoke_ReturnsNull"
```

Notable coverage added recently:
- `PasskeyServiceTests` - challenge store (single-use/expiry/wrong-user), the fail-closed low-level verifier
  (guards the old auth-bypass), and the website (vault) passkey **create -> assert round trip** with real
  crypto so the zero-knowledge private-key storage is exercised.
- `ApiKeyServiceTests` - keys stored hash-only, plaintext returned once, validation by hash, revoke is a
  soft delete scoped to the owner.
- `CryptographyEdgeCaseTests` - argument-validation and boundary coverage for `CryptographyService` /
  `PasswordCryptoService` that the happy-path suite (`CryptographyTests`) doesn't exercise: invalid
  key/salt/iteration/Argon2id-parameter inputs, malformed Argon2id PHC strings (wrong part count,
  non-numeric params, invalid base64 - `VerifyMasterPassword` must reject all of these without throwing),
  `HashPassword`/`VerifyPassword` exercised directly (not just via `PasswordCryptoService`), per-salt
  uniqueness of the master-key lookup identifier, and a large-payload AES-GCM round trip.

## UI automation (Blazor web) - `VaultGuard.Tests.Playwright`

The UI tests live in their **own library**. The base class (`BlazorWebTestBase`) self-hosts the Blazor web
app on a temp SQLite database and drives it with Playwright/Chromium, so no separate server is needed.

```bash
# First time only: install the browsers
pwsh VaultGuard.Tests.Playwright/bin/Debug/net10.0/playwright.ps1 install chromium
# or let the base class install it on first run.

dotnet test VaultGuard.Tests.Playwright/VaultGuard.Tests.Playwright.csproj
```

Environment knobs: `HEADED=1` (default) shows the browser; `PLAYWRIGHT_BASE_URL` points the tests at an
already-running instance instead of self-hosting. Screenshots are saved next to the test results.

Covered pages include the dashboard, password items, categories, tags, vaults, settings (incl. the
deep-linkable `Settings?tab=Name` tabs) and the passkeys page.

**In CI:** `run-tests.yml`'s `blazor-ui-tests` job runs every real assertion-based class -
`ApplicationWorkflowTests`, `CategoryAndTagCrudTests`, `PasswordItemCrudTests`, `UserManagementCrudTests`,
`VaultCrudTests`, `PasskeysAndSettingsTests`, `SeededDemoDataTests` - as its own check, separate from the
`test` job above. `ScreenshotCaptureTests` is excluded there (image capture only, no assertions) - it runs
in `screenshots.yml` instead. This suite isn't Allure-wired: `Allure.MSTest` needs the test class to
derive from its base type, which conflicts with Playwright's `PageTest` base class - it reports via
`dotnet-trx`/`dorny/test-reporter` like the other suites instead.

## Allure dashboard

[Allure](https://allurereport.org/) turns test results into an interactive HTML dashboard.

**Live CI dashboard:** [dotnetappdev.github.io/PasswordManagerApp/allure-report/](https://dotnetappdev.github.io/PasswordManagerApp/allure-report/)
- republished by `.github/workflows/run-tests.yml` on every push to `devmain`/`main`/`develop`, with
history across the last 20 runs so you can see trends, not just the latest pass/fail. The same link is
also printed at the bottom of every run's summary page in the Actions tab. Pull request runs don't
publish here (see `keep_files` note below) - their results are attached as the run's `allure-report`
artifact instead, downloadable from that run's summary page.

- **Instrumentation:** NUnit fixtures in `VaultGuard.BackEnd.Tests` carry `[AllureNUnit]` (via `Allure.NUnit`),
  and an `allureConfig.json` sends per-test JSON to each project's `allure-results` output folder.
- **Categorization:** every fixture also carries `[AllureEpic]` + `[AllureFeature]` (from
  `Allure.NUnit.Attributes`), so the dashboard's Behaviors tab groups the ~200+ tests by area instead of
  dumping everything into one flat list keyed off namespace. Epics: `Cryptography & Security`,
  `Vault Data Management`, `Accounts & API Access`, `Platform & Infrastructure` - each with several
  Features underneath (e.g. `Cryptography & Security` → `Encryption & Key Derivation`,
  `Password Vault Cryptography`, `Two-Factor Authentication`, `Security Auditing`, `Input Validation`).
  Add the same two attributes to a new fixture's class declaration to place it in the right group.
- **Prerequisites for generating the HTML:** the Allure CLI, or Node (`npx`) + a JRE. Java is used by the
  Allure CLI under the hood.

Generate and view the dashboard:

```powershell
# Runs the instrumented suites, aggregates results and writes ./allure-report
pwsh scripts/run-tests-allure.ps1

# Or open a live, auto-refreshing report
pwsh scripts/run-tests-allure.ps1 -Serve
```

Open `allure-report/index.html`. Both `allure-results/` and `allure-report/` are git-ignored.

### Adding another suite to the dashboard

- **NUnit:** add `[AllureNUnit]` to the `[TestFixture]`, add `Allure.NUnit` + an `allureConfig.json` (copied
  to output), then list the project in `scripts/run-tests-allure.ps1`.
- **MSTest (Playwright):** `Allure.MSTest` needs the test class to derive from its base type, which conflicts
  with Playwright's `PageTest`, so the UI suite is not wired into Allure yet - run it on its own for now.
- **xUnit:** `Allure.Xunit` requires selecting its reporter (`-- xUnit.ReporterSwitch=allure` or a
  `.runsettings`); it did not engage cleanly under the current VSTest v3 runner, so the xUnit suites are run
  normally and are not in the Allure dashboard yet.
