# Web (Blazor)

`VaultGuard.Web` is a Blazor Server app built on MudBlazor. It runs the shared core in-process - there's
no separate backend for the web app to call.

::: tip
These are captured by the `Capture Screenshots` GitHub Actions workflow
(`.github/workflows/screenshots.yml`), which runs `ScreenshotCaptureTests` in
`VaultGuard.Tests.Playwright` against the current app on every push that touches the web app or its
shared UI, and commits fresh PNGs straight into `screenshots/blazor/{dark,light}`. To regenerate
locally: `HEADED=0 dotnet test VaultGuard.Tests.Playwright --filter FullyQualifiedName~ScreenshotCaptureTests`.
:::

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

## Item forms

Every item type shares one dynamic form engine across all four apps:

<div class="screenshot-grid">
  <figure><img src="/screenshots/password-form.png" alt="Password form" /><figcaption>Login / Password</figcaption></figure>
  <figure><img src="/screenshots/credit-card-form.png" alt="Credit card form" /><figcaption>Credit Card</figcaption></figure>
  <figure><img src="/screenshots/secure-note-form.png" alt="Secure note form" /><figcaption>Secure Note</figcaption></figure>
  <figure><img src="/screenshots/identity-form.png" alt="Identity form" /><figcaption>Identity</figcaption></figure>
  <figure><img src="/screenshots/api-credentials-form.png" alt="API credentials form" /><figcaption>API Credentials</figcaption></figure>
</div>

## Settings & import

<div class="screenshot-grid">
  <figure><img src="/screenshots/blazor/dark/settings.png" alt="Settings" /><figcaption>Settings</figcaption></figure>
  <figure><img src="/screenshots/blazor/dark/import.png" alt="Import" /><figcaption>Import</figcaption></figure>
</div>

## Light theme & high contrast

<div class="screenshot-grid">
  <figure><img src="/screenshots/blazor/light/dashboard.png" alt="Light dashboard" /><figcaption>Light theme</figcaption></figure>
  <figure><img src="/screenshots/blazor/dark/high-contrast.png" alt="High contrast" /><figcaption>High contrast</figcaption></figure>
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
