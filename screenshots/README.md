# Screenshots

Interface captures for Vault Guard. Use sample data only — never real credentials.

> New here? See [`SCREENSHOT_GUIDE.md`](SCREENSHOT_GUIDE.md) for how to capture and name images.

## 🌐 Web (Blazor)

All pages below are captured for both themes by `ScreenshotCaptureTests` (see
[`VaultGuard.Tests.Playwright`](../VaultGuard.Tests.Playwright)) - only the dark set is shown per-section
here to keep this file short; swap `dark/` for `light/` in any path to see the light-theme version, or
browse the full [Screenshot Gallery](https://dotnetappdev.github.io/PasswordManagerApp/gallery).

### Dark theme

| Dashboard | All Items | Vaults |
|:---:|:---:|:---:|
| ![](blazor/dark/dashboard.png) | ![](blazor/dark/all-items.png) | ![](blazor/dark/vaults.png) |
| **Collections** | **Categories** | **Tags** |
| ![](blazor/dark/collections.png) | ![](blazor/dark/categories.png) | ![](blazor/dark/tags.png) |
| **Archive** | **Recently Deleted** | **Audit Logs** |
| ![](blazor/dark/archive.png) | ![](blazor/dark/recently-deleted.png) | ![](blazor/dark/audit-logs.png) |

#### Settings tabs

| Security | Appearance | Database |
|:---:|:---:|:---:|
| ![](blazor/dark/settings.png) | ![](blazor/dark/settings-appearance.png) | ![](blazor/dark/settings-database.png) |
| **About** | **Maintenance** | **Encryption** |
| ![](blazor/dark/settings-about.png) | ![](blazor/dark/settings-maintenance.png) | ![](blazor/dark/settings-encryption.png) |

#### Theme variants

Theme is one Settings → Appearance control with four options (Dark Mode / Light Mode / High Contrast /
System Default) - High Contrast always renders the same yellow-on-black palette on its own, it isn't an
overlay on top of light/dark.

| Dark Mode | Light Mode | High Contrast |
|:---:|:---:|:---:|
| ![](blazor/dark/dashboard.png) | ![](blazor/light/dashboard.png) | ![](blazor/dark/high-contrast.png) |

### Light theme

| Dashboard | All items | Settings |
|:---:|:---:|:---:|
| ![](blazor/light/dashboard.png) | ![](blazor/light/all-items.png) | ![](blazor/light/settings.png) |
| **Vaults** | **Collections** | **Security Center** |
| ![](blazor/light/vaults.png) | ![](blazor/light/collections.png) | ![](blazor/light/security.png) |

### Mobile browser (light)

| Dashboard |
|:---:|
| ![](blazor/mobile-light/dashboard.png) |

---

## 🔌 API reference (Scalar)

| Endpoint reference | Try it — Test Request panel |
|:---:|:---:|
| ![](api/scalar.png) | ![](api/scalar-tryit.png) |

---

## 🖥️ Desktop (WPF) and 📱 Mobile (Android/iOS)

Not available yet - capturing these needs a Windows desktop session (WPF) or an Android/iOS emulator
(neither exists in the environment these screenshots are generated from: no `/dev/kvm`, no Android SDK).
Drop real captures into `wpf/{dark,light}/` and `mobile/{android,ios}/{dark,light}/` and they'll show up
in the [documentation site](https://dotnetappdev.github.io/PasswordManagerApp/) automatically.

---

## Item forms

| Login | Credit card | Secure note |
|:---:|:---:|:---:|
| ![](password-form.png) | ![](credit-card-form.png) | ![](secure-note-form.png) |
| **Identity** | **API credentials** | **Card with custom field** |
| ![](identity-form.png) | ![](api-credentials-form.png) | ![](credit-card-form-with-custom-field.png) |

---

## Guidelines

- **Format / size:** PNG, minimum 1920×1080 (2560×1440 for high-DPI desktop captures); 390×844 for mobile.
- **Themes:** capture both light and dark for each screen.
- **Full shell:** include the title bar, sidebar and page content — not cropped to a single dialog.
- **Sample data only:** realistic but fake; no real passwords.
- **Consistency:** same window size, scaling and theme within a set.
- Replace placeholder images (indigo tile with label) with live captures as each platform/theme is available.
