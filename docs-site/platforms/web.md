# Web (Blazor)

`VaultGuard.Web` is a Blazor Server app built on MudBlazor. It runs the shared core in-process - there's
no separate backend for the web app to call.

Prefer browsing visually? The **[Screenshot Gallery](/gallery)** has every page below (plus item-creation
dialogs and forms) filterable and zoomable in one place.

::: tip
These are captured by the `Capture Screenshots` GitHub Actions workflow
(`.github/workflows/screenshots.yml`), which runs `ScreenshotCaptureTests` in
`VaultGuard.Tests.Playwright` against the current app on every push that touches the web app or its
shared UI, and commits fresh PNGs straight into `screenshots/blazor/{dark,light,onboarding}`. To
regenerate locally: `HEADED=0 dotnet test VaultGuard.Tests.Playwright --filter FullyQualifiedName~ScreenshotCaptureTests`.
:::

## Getting set up

First run walks you through picking a database provider (SQLite, SQL Server, or MySQL/MariaDB) and
editing its connection settings on one screen, then unlocking with your master key:

<div class="screenshot-grid">
  <figure><img src="/screenshots/blazor/onboarding/database-setup.png" alt="Database setup wizard" /><figcaption>Database Setup</figcaption></figure>
  <figure><img src="/screenshots/blazor/onboarding/login.png" alt="Sign-in / profile picker" /><figcaption>Sign In</figcaption></figure>
</div>

See the [Getting Started guide](/guide/getting-started) for the full self-hosting walkthrough.

## Dashboard

<img src="/screenshots/blazor/dark/dashboard.png" alt="Dashboard" style="border-radius: 8px" />

## All items & organization

<div class="screenshot-grid">
  <figure><img src="/screenshots/blazor/dark/all-items.png" alt="All items" /><figcaption>All Items</figcaption></figure>
  <figure><img src="/screenshots/blazor/dark/vaults.png" alt="Vaults" /><figcaption>Vaults</figcaption></figure>
  <figure><img src="/screenshots/blazor/dark/collections.png" alt="Collections" /><figcaption>Collections</figcaption></figure>
  <figure><img src="/screenshots/blazor/dark/categories.png" alt="Categories" /><figcaption>Categories</figcaption></figure>
  <figure><img src="/screenshots/blazor/dark/tags.png" alt="Tags" /><figcaption>Tags</figcaption></figure>
  <figure><img src="/screenshots/blazor/dark/archive.png" alt="Archive" /><figcaption>Archive</figcaption></figure>
  <figure><img src="/screenshots/blazor/dark/recently-deleted.png" alt="Recently deleted" /><figcaption>Recently Deleted</figcaption></figure>
</div>

## Security & access

<div class="screenshot-grid">
  <figure><img src="/screenshots/blazor/dark/security.png" alt="Security Center" /><figcaption>Security Center</figcaption></figure>
  <figure><img src="/screenshots/blazor/dark/passkeys.png" alt="Passkeys" /><figcaption>Passkeys</figcaption></figure>
  <figure><img src="/screenshots/blazor/dark/api-keys.png" alt="API Keys" /><figcaption>API Keys</figcaption></figure>
  <figure><img src="/screenshots/blazor/dark/audit-logs.png" alt="Audit Logs" /><figcaption>Audit Logs</figcaption></figure>
</div>

## Actions

<div class="screenshot-grid">
  <figure><img src="/screenshots/blazor/dark/dialog-add-item.png" alt="Add item dialog" /><figcaption>Add Item (with the inline generator open)</figcaption></figure>
  <figure><img src="/screenshots/blazor/dark/dialog-new-vault.png" alt="New vault dialog" /><figcaption>New Vault</figcaption></figure>
  <figure><img src="/screenshots/blazor/dark/dialog-new-collection.png" alt="New collection dialog" /><figcaption>New Collection</figcaption></figure>
  <figure><img src="/screenshots/blazor/dark/dialog-api-key-generated.png" alt="API key generated dialog" /><figcaption>API Key Created (one-time reveal)</figcaption></figure>
</div>

## Item forms

Every item type shares one dynamic form engine across all four apps:

<div class="screenshot-grid">
  <figure><img src="/screenshots/password-form.png" alt="Password form" /><figcaption>Login / Password</figcaption></figure>
  <figure><img src="/screenshots/credit-card-form.png" alt="Credit card form" /><figcaption>Credit Card</figcaption></figure>
  <figure><img src="/screenshots/secure-note-form.png" alt="Secure note form" /><figcaption>Secure Note</figcaption></figure>
  <figure><img src="/screenshots/identity-form.png" alt="Identity form" /><figcaption>Identity</figcaption></figure>
  <figure><img src="/screenshots/api-credentials-form.png" alt="API credentials form" /><figcaption>API Credentials</figcaption></figure>
</div>

## Settings tour

Settings is one page with 11 deep-linkable tabs (`/settings?tab=<name>`) - everything from vault
encryption and two-factor auth to notification channels and update checks lives here:

<div class="screenshot-grid">
  <figure><img src="/screenshots/blazor/dark/settings.png" alt="Security tab" /><figcaption>Security</figcaption></figure>
  <figure><img src="/screenshots/blazor/dark/settings-appearance.png" alt="Appearance tab" /><figcaption>Appearance (theme, colour, UI zoom)</figcaption></figure>
  <figure><img src="/screenshots/blazor/dark/settings-database.png" alt="Database tab" /><figcaption>Database</figcaption></figure>
  <figure><img src="/screenshots/blazor/dark/settings-sync.png" alt="Sync tab" /><figcaption>Sync</figcaption></figure>
  <figure><img src="/screenshots/blazor/dark/settings-notifications.png" alt="Notifications tab" /><figcaption>Notifications</figcaption></figure>
  <figure><img src="/screenshots/blazor/dark/settings-vaults.png" alt="Vaults tab" /><figcaption>Vaults</figcaption></figure>
  <figure><img src="/screenshots/blazor/dark/settings-generator.png" alt="Generator tab" /><figcaption>Password Generator</figcaption></figure>
  <figure><img src="/screenshots/blazor/dark/settings-encryption.png" alt="Encryption tab" /><figcaption>Encryption</figcaption></figure>
  <figure><img src="/screenshots/blazor/dark/settings-shortcuts.png" alt="Shortcuts tab" /><figcaption>Keyboard Shortcuts</figcaption></figure>
  <figure><img src="/screenshots/blazor/dark/settings-maintenance.png" alt="Maintenance tab" /><figcaption>Maintenance</figcaption></figure>
  <figure><img src="/screenshots/blazor/dark/settings-about.png" alt="About tab" /><figcaption>About</figcaption></figure>
</div>

## Profile & import

<div class="screenshot-grid">
  <figure><img src="/screenshots/blazor/dark/profile.png" alt="Profile" /><figcaption>Profile</figcaption></figure>
  <figure><img src="/screenshots/blazor/dark/import.png" alt="Import" /><figcaption>Import</figcaption></figure>
</div>

## Light theme & high contrast

Theme lives in Settings → Appearance as one Theme control with four options - Dark Mode, Light Mode,
High Contrast and System Default - so switching is a single choice, not a light/dark toggle plus a
separate accessibility switch. High Contrast always renders the same yellow-on-black palette, regardless
of which of the other three was active beforehand.

<div class="screenshot-grid">
  <figure><img src="/screenshots/blazor/light/dashboard.png" alt="Light dashboard" /><figcaption>Light theme — Dashboard</figcaption></figure>
  <figure><img src="/screenshots/blazor/light/all-items.png" alt="Light all items" /><figcaption>Light theme — All Items</figcaption></figure>
  <figure><img src="/screenshots/blazor/light/vaults.png" alt="Light vaults" /><figcaption>Light theme — Vaults</figcaption></figure>
  <figure><img src="/screenshots/blazor/light/collections.png" alt="Light collections" /><figcaption>Light theme — Collections</figcaption></figure>
  <figure><img src="/screenshots/blazor/light/security.png" alt="Light security center" /><figcaption>Light theme — Security Center</figcaption></figure>
  <figure><img src="/screenshots/blazor/light/settings.png" alt="Light settings" /><figcaption>Light theme — Settings</figcaption></figure>
  <figure><img src="/screenshots/blazor/dark/high-contrast.png" alt="High contrast" /><figcaption>High Contrast</figcaption></figure>
</div>

## API reference (Scalar)

The API's OpenAPI docs are served by [Scalar](https://scalar.com/) - a dashboard-style reference UI with
a built-in interactive request client. Click any endpoint, hit **Test Request**, and the panel lets you
fill in headers/body and call the live API directly from the browser - no Postman/curl needed.

<div class="screenshot-grid">
  <figure><img src="/screenshots/api/scalar.png" alt="Scalar API reference" /><figcaption>Endpoint reference</figcaption></figure>
  <figure><img src="/screenshots/api/scalar-tryit.png" alt="Scalar Test Request panel" /><figcaption>Try it — Test Request panel</figcaption></figure>
</div>

<style>
.screenshot-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 16px;
  margin: 24px 0;
}
.screenshot-grid figure {
  margin: 0;
}
.screenshot-grid img {
  width: 100%;
  border-radius: 8px;
  border: 1px solid var(--vp-c-divider);
}
.screenshot-grid figcaption {
  text-align: center;
  font-size: 0.85em;
  color: var(--vp-c-text-2);
  margin-top: 6px;
}
</style>
