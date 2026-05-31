# 🔐 VaultGuard – Password Manager

A full-featured, self-hosted password manager built with **Blazor Server (.NET 10)**, **MudBlazor**, **Entity Framework Core**, and **ASP.NET Core Identity**.

---

## ✨ Features

- 🔑 **Multiple item types**: Login, Password, Passkey, SecureNote, WiFi, CreditCard
- 🏛️ **Vaults** – organize items into separate vaults
- 📂 **Categories & Collections** – flexible organization
- 🏷️ **Tags** – quick filtering and labeling
- 🌙 **Dark / Light mode** – fully themed UI
- 🔐 **Audit Logs** – track every change
- 📱 **Responsive** – works on desktop and mobile
- 🧪 **Seeded demo data** – ready to explore out of the box

---

## 📸 Screenshots

### 🖥️ Desktop – Dark Mode

| Dashboard | All Items |
|-----------|-----------|
| ![Dashboard Dark](screenshots/blazor/dark/dashboard.png) | ![All Items Dark](screenshots/blazor/dark/all-items.png) |

| Vaults | Categories |
|--------|-----------|
| ![Vaults Dark](screenshots/blazor/dark/vaults.png) | ![Categories Dark](screenshots/blazor/dark/categories.png) |

| Collections | Tags |
|-------------|------|
| ![Collections Dark](screenshots/blazor/dark/collections.png) | ![Tags Dark](screenshots/blazor/dark/tags.png) |

| Settings (Security) | Audit Logs |
|---------------------|------------|
| ![Settings Dark](screenshots/blazor/dark/settings.png) | ![Audit Logs Dark](screenshots/blazor/dark/audit-logs.png) |

---

### ☀️ Desktop – Light Mode

| Dashboard | All Items |
|-----------|-----------|
| ![Dashboard Light](screenshots/blazor/light/dashboard.png) | ![All Items Light](screenshots/blazor/light/all-items.png) |

| Collections |
|-------------|
| ![Collections Light](screenshots/blazor/light/collections.png) |

---

### 📱 Mobile – Light Mode (390 × 844)

| Dashboard |
|-----------|
| ![Mobile Dashboard Light](screenshots/blazor/mobile-light/dashboard.png) |

---

### 🪟 WinUI Desktop Screenshots

- WinUI screenshot capture and coverage guide: [screenshots/SCREENSHOT_GUIDE.md](screenshots/SCREENSHOT_GUIDE.md)
- WinUI screenshot folder and naming conventions: [screenshots/README.md](screenshots/README.md)
- Current WinUI dashboard/login placeholders:
  - [screenshots/winui-dashboard-placeholders.md](screenshots/winui-dashboard-placeholders.md)
  - [screenshots/winui-login-light.placeholder](screenshots/winui-login-light.placeholder)
  - [screenshots/winui-login-dark.placeholder](screenshots/winui-login-dark.placeholder)

---

### 🧩 Item Templates & Custom Fields (Desktop / MAUI-style forms)

| Login | Password | Secure Note |
|------|----------|-------------|
| ![Login Form](screenshots/password-form.png) | ![Password Form](screenshots/api-credentials-form.png) | ![Secure Note Form](screenshots/secure-note-form.png) |

| Credit Card | Credit Card + Date | Credit Card + Custom Field |
|-------------|--------------------|----------------------------|
| ![Credit Card Form](screenshots/credit-card-form.png) | ![Credit Card Form with Date](screenshots/credit-card-form-with-date-field.png) | ![Credit Card Form with Custom Field](screenshots/credit-card-form-with-custom-field.png) |

| API Credentials + Custom Field | Identity |
|--------------------------------|----------|
| ![API Credentials with Custom Field](screenshots/api-credentials-form-with-custom-field.png) | ![Identity Form](screenshots/identity-form.png) |

---

### 📲 iOS & Biometric Support

- iOS app support is available via the mobile implementations with Face ID / Touch ID support:
  - [REACT_NATIVE_MOBILE_APP.md](REACT_NATIVE_MOBILE_APP.md) (feature-complete cross-platform mobile app)
  - [PasswordManager.App](PasswordManager.App) (MAUI app with iOS target support)
  - [PasswordManager.Uno](PasswordManager.Uno) (Uno Platform mobile support)
- Capture workflow and screenshot checklist: [MOBILE_SCREENSHOTS.md](MOBILE_SCREENSHOTS.md)
- Mobile screenshot folder structure: [screenshots/mobile/README.md](screenshots/mobile/README.md)
- Biometric implementation details: [BIOMETRIC_AUTH_IMPLEMENTATION.md](BIOMETRIC_AUTH_IMPLEMENTATION.md)

---

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQLite (bundled via EF Core)

### Run Locally

```bash
cd PasswordManager.Web
dotnet run
```

Navigate to `http://localhost:5169` and log in with the seeded credentials:

- **Email**: `admin@passwordmanager.local`
- **Password**: `CommonMaster123!`

---

## 🛠️ Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | Blazor Server + MudBlazor 8 |
| Backend | ASP.NET Core (.NET 10) |
| ORM | Entity Framework Core 9 |
| Auth | ASP.NET Core Identity |
| Database | SQLite (dev) / MySQL (prod) |

---

## 📄 License

MIT
