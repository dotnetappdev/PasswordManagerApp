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
