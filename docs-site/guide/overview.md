# Overview

VaultGuard is a self-hosted, zero-knowledge password manager. It stores logins, secure notes, credit
cards, identities, Wi-Fi credentials and passkeys in an encrypted vault, and gives you the same vault
from five different front-ends:

| Front-end | Project | Platform |
| --- | --- | --- |
| 🖥️ Desktop | `VaultGuard.WPF` | Windows 10/11 |
| 🌐 Web | `VaultGuard.Web` | Blazor Server |
| 🔌 API | `VaultGuard.API` | ASP.NET Core |
| 📱 Mobile | `VaultGuard.App` | .NET MAUI (Android / iOS / Windows) |
| 🧩 Extension | `VaultGuard.BrowserExtension` | Chrome / Edge / Firefox |

All five sit on top of one shared core - models, data access, crypto and business logic - so behaviour
(validation, encryption, permissions) is identical no matter which client you open.

## Why "zero-knowledge"

Your master password is never sent to the server and never written to disk. It is used, in memory, only
long enough to derive the keys that encrypt and decrypt your vault on your device. A stolen database,
backup file, or intercepted API payload is ciphertext without it - see [Security Model](/guide/security)
for exactly how that's enforced.

## Core concepts

- **Vault item** - a single secret (login, card, note, ...), always owned by exactly one user.
- **Type** - what kind of item it is (`Login`, `CreditCard`, `SecureNote`, `WiFi`, `Passkey`, `CryptoWallet`,
  ...) - 24 types in total, each with its own typed fields.
- **Collection / Category / Tag** - three independent, orthogonal ways to organise items: a collection is a
  broad grouping (e.g. "Work"), a category narrows by kind (e.g. "Logins"), tags are free-form labels.
- **Vault (container)** - a higher-level grouping used for sharing and access control.
- **API key** - a credential a client (mobile app, browser extension, script) uses instead of a full login
  session; scoped to one user, shown once, stored hash-only.

## Where to go next

- [Architecture](/guide/architecture) - how the projects fit together and the request/data flow.
- [Getting Started](/guide/getting-started) - clone, configure and run everything locally.
- [Security Model](/guide/security) - key derivation, encryption, and what's stored vs. never stored.
- [Platforms](/platforms/web) - a tour of each client with real screenshots.
- [API Reference](/api/reference) - endpoints, authentication, request/response shapes.
