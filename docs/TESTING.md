# Testing

VaultGuard has unit/integration tests and browser-driven UI tests, with an Allure dashboard for results.

## Test projects

| Project | Framework | What it covers |
| --- | --- | --- |
| `VaultGuard.BackEnd.Tests` | NUnit | Services, controllers, crypto, seeding, validation - the bulk of the unit/integration suite (~194 tests). |
| `VaultGuard.Tests.QrLogin` | xUnit | Passkey engine (`PasskeyServiceTests`), 2FA/passkey DTOs, QR login |
| `VaultGuard.Tests.OTP` | xUnit | OTP / TOTP |
| `VaultGuard.Tests.Playwright` | MSTest + Playwright | **UI automation** against the Blazor web app (its own library) |
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

## Allure dashboard

[Allure](https://allurereport.org/) turns test results into an interactive HTML dashboard.

**How it is wired (framework-agnostic):** every test project references `JunitXml.TestLogger`, so
`dotnet test --logger junit` produces a JUnit XML file per suite - uniformly for NUnit, xUnit and
MSTest/Playwright. Allure's bundled JUnit plugin reads those XML files, so **all suites appear in one
dashboard** without any per-framework Allure adapter or attributes. (We tried the native `Allure.Xunit` /
`Allure.NUnit` / `Allure.MSTest` adapters first; the xUnit one wouldn't engage under the VSTest v3 runner and
the MSTest one needs a base class that conflicts with Playwright's `PageTest`. JUnit XML sidesteps both.)

**Prerequisites for generating the HTML:** the Allure CLI, or Node (`npx`) + a JRE (the Allure CLI runs on
Java under the hood). Both are used automatically by the script.

Generate and view the dashboard:

```powershell
# Runs the non-UI suites, writes JUnit XML into ./allure-results and builds ./allure-report
pwsh scripts/run-tests-allure.ps1

# Include the Playwright UI suite (launches the web app + a browser; slower)
pwsh scripts/run-tests-allure.ps1 -IncludeUi

# Open a live, auto-refreshing report instead of a static site
pwsh scripts/run-tests-allure.ps1 -Serve
```

Open `allure-report/index.html`. Both `allure-results/` and `allure-report/` are git-ignored.

### Adding another suite to the dashboard

Reference `JunitXml.TestLogger` in the project and add it to the `$projects` list in
`scripts/run-tests-allure.ps1`. Nothing else - no attributes, no base classes.

### Running a suite with JUnit output manually

```bash
dotnet test <project> --logger "junit;LogFilePath=allure-results/<name>.junit.xml"
```
