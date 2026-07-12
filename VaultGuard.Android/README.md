# VaultGuard for Android

A native Kotlin / Jetpack Compose app with feature parity with the VaultGuard WPF desktop app. It can
run in two modes, chosen on first launch and changeable any time in **Settings → Storage**:

- **API mode** — syncs with your VaultGuard server using an **API URL + API key**.
- **Local mode** — keeps an encrypted SQLite vault on the device only (AES‑256‑GCM, PBKDF2 600k).

## Build

```bash
# From VaultGuard.Android/
./gradlew :app:assembleDebug
```

Requires JDK 17+ (the Android Studio JBR works). Open the folder in Android Studio and Run, or install the
APK from `app/build/outputs/apk/debug/app-debug.apk`.

## How to generate an API key (for API mode)

The mobile apps authenticate with an **API key** that you create in the VaultGuard **web app**. You only
need the **API URL** and the **key** — nothing else.

1. **Start the API and web app** (or point at your deployed server).
2. Open the **VaultGuard web app** in a browser and **sign in** with your account.
3. Go to **Settings → API Keys** (or browse directly to **`/api-keys`**).
4. Under **Create New API Key**, type a descriptive name (e.g. *"My Pixel 8"*) and tap **Create API Key**.
5. **Copy the generated key** shown in the dialog. ⚠️ It is shown **only once** — store it safely.
   - The key is stored hashed in the primary SQL database **and** mirrored into a per‑user local SQLite
     database, so the same key works whether the device talks to the API or a local vault.
6. Note your **API URL** — the base address of the API, e.g. `https://your-server:7001`.

### Enter it in the app

1. Launch VaultGuard on the phone. On the **Connect** screen choose **API Server**.
2. Paste the **API URL** and the **API Key**.
3. Tap **Test connection** to verify, then **Save & continue**.
4. Sign in with your email + master password (enter a 2FA code if prompted).

You can change these later in **Settings → Storage → API Configuration**.

## Features (WPF parity)

- Sidebar navigation matching the desktop app: All Items, Favourites, Categories (Logins, Credit Cards,
  Secure Notes, Wi‑Fi, Identities), Security Dashboard, Manage Categories, Vaults, Import, Archive,
  Recently Deleted, Settings, About, My Account.
- **Category filter chips** above the search bar; **search**; grouped, 1Password‑style item list.
- **Bottom navigation**: Home, **Favourites** (quick access to starred items), Items, Security, Search —
  plus a prominent accent **New Item** button (pencil glyph) that reads as the primary create action.
- Item detail with **reveal password**, copy, and a live **TOTP** code.
- Polished **add/edit** form with **password generator**, strength meter, **category picker**, and
  **QR scanning** for authenticator (TOTP) secrets.
- **Account popup menu** on the top‑left profile avatar (1Password‑style): identity header plus
  **My Profile, Switch Account, Settings, Lock** and **Exit**. The full navigation drawer opens from the
  hamburger icon.
- **Typed custom fields** with the exact same 13 field types as the desktop app — Text, Password, Date,
  Number, Email, URL, Text area, Phone, File, Toggle (Yes/No), Address, One‑Time Password and Sign‑in‑with.
  Full CRUD: each field has a type picker, type‑aware keyboard/masking, and a reveal toggle for secrets.
- **Vault CRUD** (create / rename / delete) in API mode.
- Eight‑tab **Settings** mirroring the desktop (Appearance, Accessibility, Security, Storage,
  Backup/Import/Export, Maintenance, Shortcuts, About), including editable API configuration and
  **seed / delete demo data**. **Maintenance → Delete Data** presents a scope selector — **Seed data**
  (demo items only), **User accounts** (all local accounts + their vaults), or **Everything** (full
  factory reset) — each with a confirmation dialog. The same scoped delete is on iOS.
- **Sign in a computer**: tap the QR icon on the home screen and scan the QR shown on the desktop/web
  sign‑in screen — the desktop is signed into the same account (API mode).
- Default **Personal** vault + the same six default categories as the desktop app.
- Light / dark / system / high‑contrast themes, Material You dynamic colour, UI zoom + font scaling.

## Notes / follow‑ups

- Push notifications from the API require Firebase Cloud Messaging config (`google-services.json`) — see
  the API `PushController` and `MOBILE_PUSH.md`.
- Cloud backup and file‑based import are performed on the desktop/web apps and sync down.
