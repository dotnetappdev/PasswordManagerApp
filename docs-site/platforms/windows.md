# Windows Desktop (WPF)

`VaultGuard.WPF` is a standalone Windows desktop app - no separate server required. It runs the shared
core in-process against a local SQLite vault (or a remote database provider, configured from Settings),
protected by the exact same key derivation, AES-256-GCM encryption and constant-time verification as the
server-backed builds - see [Security Model](/guide/security).

Distributed as an `.exe` (Inno Setup) or `.msi` (WiX) installer, both bundling the .NET runtime.

::: warning Screenshots not yet available
This build environment doesn't run a Windows desktop session, so there's no way to capture real
screenshots here. Once someone takes them on Windows, drop them into `screenshots/wpf/{dark,light}/`
in the repository and they'll show up here automatically.
:::

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
