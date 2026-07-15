# Windows Desktop (WPF)

`VaultGuard.WPF` is a standalone Windows desktop app - no separate server required. It runs the shared
core in-process against a local SQLite vault (or a remote database provider, configured from Settings),
protected by the exact same key derivation, AES-256-GCM encryption and constant-time verification as the
server-backed builds - see [Security Model](/guide/security).

Distributed as an `.exe` (Inno Setup) or `.msi` (WiX) installer, both bundling the .NET runtime.

::: warning Screenshots pending
This build environment doesn't run a Windows desktop session, so these are placeholder frames, not live
captures. Real screenshots need to be taken on Windows and dropped into `screenshots/wpf/{dark,light}/`
in the repository, replacing the files below.
:::

<div class="screenshot-grid">
  <figure><img src="/screenshots/wpf/dark/login.png" alt="Login (dark)" /><figcaption>Login (dark)</figcaption></figure>
  <figure><img src="/screenshots/wpf/dark/dashboard.png" alt="Dashboard (dark)" /><figcaption>Dashboard (dark)</figcaption></figure>
  <figure><img src="/screenshots/wpf/dark/all-items.png" alt="All items (dark)" /><figcaption>All Items (dark)</figcaption></figure>
  <figure><img src="/screenshots/wpf/dark/vaults.png" alt="Vaults (dark)" /><figcaption>Vaults (dark)</figcaption></figure>
  <figure><img src="/screenshots/wpf/dark/settings.png" alt="Settings (dark)" /><figcaption>Settings (dark)</figcaption></figure>
  <figure><img src="/screenshots/wpf/light/dashboard.png" alt="Dashboard (light)" /><figcaption>Dashboard (light)</figcaption></figure>
</div>

<style>
.screenshot-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 16px;
  margin: 24px 0;
}
.screenshot-grid figure { margin: 0; }
.screenshot-grid img { width: 100%; border-radius: 8px; border: 1px solid var(--vp-c-divider); }
.screenshot-grid figcaption { text-align: center; font-size: 0.85em; color: var(--vp-c-text-2); margin-top: 6px; }
</style>
