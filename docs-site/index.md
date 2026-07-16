---
layout: home

hero:
  name: VaultGuard
  text: A self-hosted, zero-knowledge password manager
  tagline: One encrypted vault. Windows, web, Android, iOS and a browser extension - the same app everywhere.
  image:
    src: /logo.png
    alt: VaultGuard
  actions:
    - theme: brand
      text: Get Started
      link: /guide/getting-started
    - theme: alt
      text: Screenshot Gallery
      link: /gallery
    - theme: alt
      text: Architecture
      link: /guide/architecture
    - theme: alt
      text: API Reference
      link: /api/reference

features:
  - icon: 🔐
    title: Zero-knowledge encryption
    details: Master password never leaves the device. AES-256-GCM item encryption, PBKDF2/Argon2id key derivation, HKDF key separation.
  - icon: 🖥️
    title: One core, five front-ends
    details: WPF desktop, Blazor web, MAUI mobile (Android/iOS), an ASP.NET Core API and a browser extension all share one models/crypto/services core.
  - icon: 🔌
    title: REST API with API keys
    details: Every client authenticates through the same API surface - issue a scoped API key from the web app and use it from a script, a mobile client, or curl.
  - icon: 🧩
    title: Typed items, your way
    details: Logins, secure notes, cards, identities, Wi-Fi, passkeys and 13 custom field types, organised into vaults, collections, categories and tags.
  - icon: 🛡️
    title: Security Center
    details: Weak/reused/old/no-2FA password detection plus privacy-preserving Have I Been Pwned breach checks (k-anonymity - only a hash prefix ever leaves the device).
  - icon: 🗄️
    title: Bring your own database
    details: SQLite out of the box; switch to SQL Server, MySQL or PostgreSQL from Settings.
---

<script setup>
const homeImages = [
  { src: '/screenshots/blazor/dark/dashboard.png', alt: 'Dashboard', category: 'Dashboard' },
  { src: '/screenshots/blazor/dark/dialog-add-item.png', alt: 'Add item — dynamic form + generator', category: 'Actions' },
  { src: '/screenshots/blazor/dark/security.png', alt: 'Security Center', category: 'Security' },
  { src: '/screenshots/blazor/dark/settings-appearance.png', alt: 'Settings — Appearance', category: 'Settings' },
  { src: '/screenshots/blazor/dark/vaults.png', alt: 'Vaults', category: 'Organize' },
  { src: '/screenshots/api/scalar.png', alt: 'API — Scalar reference UI', category: 'API' },
]
</script>

## See it in action

<Gallery :images="homeImages" />

**[View the full gallery — every screen, every theme →](/gallery)**

