# Screenshots

Interface captures for Vault Guard. Use sample data only — never real credentials.

> New here? See [`SCREENSHOT_GUIDE.md`](SCREENSHOT_GUIDE.md) for how to capture and name images.

## Web (Blazor)

### Dark

| Dashboard | All items | Vaults |
|:---:|:---:|:---:|
| ![](blazor/dark/dashboard.png) | ![](blazor/dark/all-items.png) | ![](blazor/dark/vaults.png) |
| **Collections** | **Categories** | **Tags** |
| ![](blazor/dark/collections.png) | ![](blazor/dark/categories.png) | ![](blazor/dark/tags.png) |
| **Settings** | **Audit logs** | |
| ![](blazor/dark/settings.png) | ![](blazor/dark/audit-logs.png) | |

### Light

| Dashboard | All items | Collections |
|:---:|:---:|:---:|
| ![](blazor/light/dashboard.png) | ![](blazor/light/all-items.png) | ![](blazor/light/collections.png) |

### Mobile (light)

| Dashboard |
|:---:|
| ![](blazor/mobile-light/dashboard.png) |

## Item forms

| Login | Credit card | Secure note |
|:---:|:---:|:---:|
| ![](password-form.png) | ![](credit-card-form.png) | ![](secure-note-form.png) |
| **Identity** | **API credentials** | **Card with custom field** |
| ![](identity-form.png) | ![](api-credentials-form.png) | ![](credit-card-form-with-custom-field.png) |

## Wanted

These galleries don't have captures yet — contributions welcome (follow [`SCREENSHOT_GUIDE.md`](SCREENSHOT_GUIDE.md)):

- **WPF desktop** — login, dashboard, item list, settings (light + dark)
- **WinUI 3** — same set
- **MAUI mobile** — `mobile/android/` and `mobile/ios/` (Android + iOS)

## Guidelines

- **Format / size:** PNG, minimum 1920×1080 (2560×1440 for high-DPI desktop captures).
- **Themes:** capture both light and dark for each screen.
- **Full shell:** include the title bar, sidebar and page content — not cropped to a single dialog.
- **Sample data only:** realistic but fake; no real passwords.
- **Consistency:** same window size, scaling and theme within a set.
