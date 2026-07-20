# Getting Started

## Prerequisites

- **.NET 10 SDK** - `10.0.301` or newer (pinned in [`global.json`](https://github.com/dotnetappdev/PasswordManagerApp/blob/devmain/global.json))
- Visual Studio 2026, JetBrains Rider, or just the `dotnet` CLI
- For the mobile app: `dotnet workload install maui`

## Clone and build

```bash
git clone https://github.com/dotnetappdev/PasswordManagerApp.git
cd PasswordManagerApp

dotnet build VaultGuard.WPF/VaultGuard.WPF.csproj -c Release   # desktop
dotnet build VaultGuard.Web/VaultGuard.Web.csproj -c Release   # web
dotnet build VaultGuard.API/VaultGuard.API.csproj  -c Release  # api
```

## Run in development

```bash
cd VaultGuard.API && dotnet run    # API - separate terminal
cd VaultGuard.Web && dotnet run    # web app - separate terminal
cd VaultGuard.WPF && dotnet run    # desktop app - separate terminal
```

The first run creates a local SQLite database and walks you through setting a master password. No
external services are required to get started - SQLite, key derivation and encryption all happen
locally.

## Getting an API key (for mobile & the browser extension)

The mobile app and browser extension authenticate to the API with an **API key** generated in the web
app - you only ever need the **API URL** and the **key** itself.

1. Start the API and web app (above), or point at your deployed server.
2. Open the web app and sign in.
3. Go to **Settings → API Keys** (or browse to `/api-keys`).
4. Under **Create New API Key**, give it a descriptive name (e.g. *"My Pixel 8"*) and create it.
5. **Copy the key immediately** - it's shown exactly once. It's stored **hashed** afterwards, mirrored
   into a per-user local SQLite database so the same key authenticates whether the client talks to the
   API or a local vault.
6. Note your **API URL**, e.g. `https://localhost:7001`.

Send the key on every API request:

```
X-API-Key: <your-key>
```

In the mobile app, paste the API URL and key on the **Connect** screen (or **Settings → Storage → API
Configuration**) and tap **Test connection**. Revoke access any time by deleting the key from `/api-keys`.

### Generating a key without the UI (Postman / curl)

Handy when setting up a device or testing programmatically - prove who you are with your account
**email + master password**, and get back a key bound to your user. This specific endpoint does **not**
require an existing API key (it's how you get your first one):

```
POST {API_URL}/api/authentication/generate-api-key
Content-Type: application/json

{
  "name": "My Pixel 8",
  "email": "you@example.com",
  "masterPassword": "your-master-password"
}
```

```json
{
  "apiKey": "414C1BUrIaVHwulgSPZAW1-ELcL--MhSS9Cmx8Z8GZc",
  "userId": "0d6fe338-9517-42cf-aeb7-16410b285da7",
  "keyName": "My Pixel 8",
  "instructions": "Store this API key securely..."
}
```

See the full [API Reference](/api/reference) for everything else the API exposes.

## Project layout at a glance

See [Architecture](/guide/architecture#project-layout) for the full breakdown of every project in the
solution.

## Building the Windows installers

The `.exe` (Inno Setup) and `.msi` (WiX) installers are built by CI for every `v*` tag
(`.github/workflows/build-wpf.yml`) and attached to the GitHub release. To build them locally, see
[`installers/README.md`](https://github.com/dotnetappdev/PasswordManagerApp/blob/devmain/installers/README.md) in
the repository.
