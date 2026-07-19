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
- `JwtServiceTests` - the first coverage for `VaultGuard.Services.Services.JwtService`, the real JWT/
  refresh-token implementation `PasskeyService` uses (there's a second, unregistered `JwtService` under
  `VaultGuard.API` that nothing actually calls - don't confuse the two). Covers token claims, that two
  tokens for the same user never collide, decoding an already-expired token, rejecting a token signed
  with the wrong key, and the refresh-token store's round-trip/5-per-user cap/revoke behavior. Every test
  uses a fresh `Guid` as the user ID, since the refresh-token store is a `static` dictionary shared across
  the whole test run.
- `PassphraseGeneratorEdgeCaseTests` - boundary/formatting coverage for `PassphraseGenerator` beyond what
  `PassphraseGeneratorTests` already covers: explicit `null` options, both word-count clamp directions, an
  empty separator (words run together with no delimiter), a multi-character separator, and the exact
  shape of the word/number segments. This is the only backend password-generator worth unit testing -
  traditional random-character generation is duplicated as private, untestable methods inline in several
  client UIs (Blazor `PasswordEdit.razor`/`Settings.razor`, WPF/WinUI dialogs, native iOS/Android) with no
  shared service or interface behind any of them.

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

**Authentication:** `BlazorWebTestBase.SignInAsync()` (shared, not `ScreenshotCaptureTests`' own private
copy - see below) signs in with the seeded demo account, creating the default accounts first on a fresh
database. Every class above except `ScreenshotCaptureTests` calls it from a `[TestInitialize]` before its
tests run, since all of them navigate straight to a protected route - without signing in first, that just
bounces to `/login` and the test times out waiting for content that's never going to render.
`ScreenshotCaptureTests` deliberately keeps its own private, near-identical copy: it needs precise control
over *when* sign-in happens, since it captures the pre-auth onboarding screens (`/setup`, `/login`)
before ever calling it.

**Evidence:** every test method above calls `SaveEvidenceAsync(name)` at its key steps - a full-page
screenshot into `--results-directory`, attached to that test's result via `TestContext.AddResultFile` so
it shows up as an actual attachment, not just a stray file. Runs with `if: always()` in CI, so a failing
run's screenshots - what the page actually looked like when an assertion failed - are downloadable too,
as the `blazor-ui-test-screenshots` artifact.

**Locator specificity:** once the auth fix above let these tests actually reach an authenticated page
for the first time, a second, previously-invisible bug surfaced: most "page loaded" checks used a bare
`Page.GetByText("Vaults")`/`"Categories"`/`"Dashboard"`/etc. That word almost always also appears in the
nav drawer (a nav link, a nav group title, or both) rendered on every authenticated page, so once real
content was on screen, Playwright's strict mode correctly refused to guess which of the 2-7 matching
elements was meant and threw instead. Fixed by scoping to the page's own heading -
`Page.GetByRole(AriaRole.Heading, new() { Name = "Vaults", Exact = true })` - which only ever matches
that page's own `<h4>`/`<h6>` title, never a nav link or nav group title (neither has heading role). The
one page-load check against the Dashboard itself needed a different fix, since its stat-card labels
("Vaults", "Categories") are `<p>` captions, not headings - scoped to `Page.Locator("p").GetByText(...)`
instead. Two tests (`Dashboard_AppBar_HasBrandName`, `AppBar_HasDarkModeToggle`) were checking for
`"VaultGuard"` (no space), but the app bar actually renders `"🔐 Vault Guard"` (with a space) - a
genuine copy mismatch, not a locator-scoping issue, fixed by matching the real text. `SeededDemoDataTests`
had the same problem one level deeper: it checked the Dashboard's Recent Items widget (which only shows
the 8 most-recently-modified items) for `"Chase Bank"` and a `"Search your vault..."` placeholder that
never existed in the Blazor app at all (the real placeholder, on `/passwords`, is `"Search items..."`) -
rewritten to check `/passwords`, which lists every seeded item unconditionally and has the real search box.

**Known issue - ~9 tests in `VaultCrudTests`/`UserManagementCrudTests` intermittently fail (CI usually
lands 50-51/60):** every one of these lands on `Page.Url=/login` at the point of failure -
`BlazorWebTestBase.SignInAsync()` genuinely fails to authenticate, it's not a locator, timing, or
render issue (confirmed via `ExpectVisibleWithDiagnosticsAsync`, which logs the page URL/title/body
snippet on failure - use it instead of a bare `Expect(...)` if you're debugging this further). Two
fixes were tried and ruled out:
- Bumping the assertion timeout 5s→15s changed nothing - the exact same 9 tests failed either way,
  ruling out "just needs more time" for the target page's own render.
- Replacing `SignInAsync`'s fixed post-click delays with an actual `WaitForURLAsync` wait for the
  redirect away from `/login` also didn't help on its own - its own timeout warning never fired, which
  means the URL *does* leave `/login` successfully, then bounces back before the sign-in flow's own
  final check. Adding a settle-wait for that second bounce plus raising the retry count 2→4 made
  things measurably worse (50/60 vs 51/60) and revealed the failure is **deterministic once it
  happens** - all 4 retries failed identically for the affected tests, not intermittently - which
  rules out "just needs more retries" too, so this change was reverted.

Current best theory: `MainLayout.EnforceAuthAsync()`'s server-side "is the vault actually unlocked"
check (`VaultSessionService.IsVaultUnlocked`, an in-memory `ConcurrentDictionary` with no expiry) races
against the client-side post-login redirect, and something about being late in this assembly's long
shared test run (both affected classes are among the last to execute, after ~50 prior tests' worth of
requests against the one shared `VaultGuard.Web` process) makes that race lose more often - but this
hasn't been confirmed, only the symptom (bounces back to `/login`) has. Worth an actual look at the
`blazor-ui-test-screenshots` CI artifact or a debugger-attached local repro rather than more blind
CI-cycle guessing.

**New lead found by reading `EnsureAppStartedAsync`, not by CI-cycle guessing:** the shared
`VaultGuard.Web` process was started with `RedirectStandardOutput`/`RedirectStandardError` but nothing
ever read those pipes while the process was running (only after it had already exited, in
`WaitForAppAsync`'s failure path). On Linux a redirected pipe has a small fixed OS buffer (~64KB) -
once full, the child process **blocks** on its next console write. `appsettings.Development.json` logs
at `Default: Information`, which includes EF Core's `Executed DbCommand` logging (full SQL text for
every query) - verbose enough that ~50 shared tests' worth of page loads before `VaultCrudTests`/
`UserManagementCrudTests` run could plausibly fill that buffer. This lines up with every observed
symptom: deterministic once it starts (not flaky), only affects tests late in the run, and a stall deep
in request handling wouldn't show up as a client-side (Playwright) timeout distinguishable from any
other kind of slowness. Fixed in the same change that added this note: `EnsureAppStartedAsync` now
wires `OutputDataReceived`/`ErrorDataReceived` + `BeginOutputReadLine`/`BeginErrorReadLine` to keep the
pipe permanently drained into a bounded in-memory tail (`BlazorWebTestBase.GetAppLogTail()`), which is
now also logged by `ExpectVisibleWithDiagnosticsAsync` and `SignInAsync`'s bounce-back warning - so even
if this isn't the *whole* story, the next CI failure will show the app's own console state (exceptions,
slow queries, whatever it was doing) at the moment of failure, not just the browser-side symptom.

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
- **Categorization:** every fixture carries both pairs of grouping attributes (`Allure.NUnit.Attributes`),
  covering the dashboard's two different views of the same ~200+ tests:
  - `[AllureEpic]` + `[AllureFeature]` drive the **Behaviors** tab.
  - `[AllureParentSuite]` + `[AllureSuite]` drive the **Suites** tab - without these, Suites only ever
    shows the four top-level namespace folders (`Configuration`/`Controllers`/`Helpers`/`Services`), each
    dumping everything under it into one flat bucket (e.g. `Services` alone held 154 tests).

  Both pairs use the same values, so the two tabs mirror each other: 4 top-level groups -
  `Cryptography & Security`, `Vault Data Management`, `Accounts & API Access`,
  `Platform & Infrastructure` - each split into several sub-groups (e.g. `Cryptography & Security` →
  `Encryption & Key Derivation`, `Password Vault Cryptography`, `Two-Factor Authentication`,
  `Security Auditing`, `Input Validation`). Add all four attributes to a new fixture's class declaration
  to place it correctly in both tabs.
- **A third level - `[AllureStory]` / `[AllureSubSuite]`:** Epic/Feature and ParentSuite/Suite are both
  only two levels deep, so a Feature shared by several fixtures (e.g. `Password Items` held
  `PasswordItemsControllerTests`, `PasswordItemServiceTests` and `ProtectedItemHelperTests` with no
  further breakdown) or a single large fixture (e.g. `InputValidationHelperTests`, 23 tests in one flat
  bucket) still didn't show any internal structure. `[AllureStory("...")]` / `[AllureSubSuite("...")]`
  add a third tier under Feature/Suite respectively:
  - **Class-level**, when a Feature bundles multiple fixtures - each fixture gets its own Story naming
    its role (e.g. `Password Items` → `Controller Endpoints` / `Service Layer` /
    `Protected-Field Encryption Helper`, one per fixture).
  - **Method-level**, when a single fixture's tests cluster into distinct concerns - e.g.
    `InputValidationHelperTests` splits into `Username Validation` / `Email Validation` /
    `Master Password Strength Rules` / etc.; `JwtServiceTests` into `Token Generation` /
    `Refresh Token Store` / etc. Allure applies the closest attribute to a test method, so a method-level
    `[AllureStory]` overrides any class-level one - only add a class-level Story when *every* test in the
    fixture genuinely belongs to it.

  Not every fixture needs a Story - small, already-cohesive fixtures (a handful of tests covering one
  method, e.g. `SecurityAuditServiceTests`) are left at Feature-level only; forcing a third tier there
  would add noise, not clarity.
- **Setup/teardown as named steps:** `[AllureNUnit]` alone does *not* surface `[SetUp]`/`[TearDown]` in
  the report - each one needs an explicit `[AllureBefore("description")]` / `[AllureAfter("description")]`
  attribute (placed above the `[SetUp]`/`[TearDown]` attribute) or it's invisible in the test's step
  timeline. Every fixture with a `[SetUp]`/`[TearDown]` now has one, describing what that fixture's setup
  actually does (e.g. "Create a fresh EF Core in-memory database context", "Open a persistent in-memory
  SQLite connection...") rather than a generic placeholder.
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
