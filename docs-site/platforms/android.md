# Android (MAUI)

`VaultGuard.App` is a .NET MAUI app - Blazor Hybrid, so it hosts the same `VaultGuard.Components.Shared`
UI the web app uses, wrapped in a native shell. It multi-targets `net10.0-android` (plus Windows/iOS/
MacCatalyst depending on host OS) and talks to `VaultGuard.API` over HTTP with an
[API key](/api/reference#authentication) rather than running the core in-process.

::: warning Screenshots not yet available
This build environment has no Android emulator, so there's no way to capture real screenshots here.
Once someone takes them from a device or emulator, drop them into `screenshots/mobile/android/{dark,light}/`
in the repository and they'll show up here automatically.
:::

## Connecting to a server

On first launch, the app asks for the API URL and an [API key](/guide/getting-started#getting-an-api-key-for-mobile-the-browser-extension)
generated from the web app. Both can be changed later in **Settings → Storage → API Configuration**.

<style>
.screenshot-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 16px;
  margin: 24px 0;
}
.screenshot-grid figure { margin: 0; }
.screenshot-grid img { width: 100%; border-radius: 8px; border: 1px solid var(--vp-c-divider); }
.screenshot-grid figcaption { text-align: center; font-size: 0.85em; color: var(--vp-c-text-2); margin-top: 6px; }
</style>
