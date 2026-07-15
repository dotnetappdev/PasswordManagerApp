# Android (MAUI)

`VaultGuard.App` is a .NET MAUI app - Blazor Hybrid, so it hosts the same `VaultGuard.Components.Shared`
UI the web app uses, wrapped in a native shell. It multi-targets `net10.0-android` (plus Windows/iOS/
MacCatalyst depending on host OS) and talks to `VaultGuard.API` over HTTP with an
[API key](/api/reference#authentication) rather than running the core in-process.

::: warning Screenshots pending
This build environment has no Android emulator, so these are placeholder frames, not live captures.
Real screenshots need to be taken from a device or emulator and dropped into
`screenshots/mobile/android/{dark,light}/` in the repository, replacing the files below.
:::

<div class="screenshot-grid">
  <figure><img src="/screenshots/mobile/android/dark/dashboard.png" alt="Dashboard (dark)" /><figcaption>Dashboard (dark)</figcaption></figure>
  <figure><img src="/screenshots/mobile/android/dark/all-items.png" alt="All items (dark)" /><figcaption>All Items (dark)</figcaption></figure>
  <figure><img src="/screenshots/mobile/android/dark/vaults.png" alt="Vaults (dark)" /><figcaption>Vaults (dark)</figcaption></figure>
  <figure><img src="/screenshots/mobile/android/dark/settings.png" alt="Settings (dark)" /><figcaption>Settings (dark)</figcaption></figure>
  <figure><img src="/screenshots/mobile/android/light/dashboard.png" alt="Dashboard (light)" /><figcaption>Dashboard (light)</figcaption></figure>
  <figure><img src="/screenshots/mobile/android/light/all-items.png" alt="All items (light)" /><figcaption>All Items (light)</figcaption></figure>
  <figure><img src="/screenshots/mobile/android/light/vaults.png" alt="Vaults (light)" /><figcaption>Vaults (light)</figcaption></figure>
  <figure><img src="/screenshots/mobile/android/light/settings.png" alt="Settings (light)" /><figcaption>Settings (light)</figcaption></figure>
</div>

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
