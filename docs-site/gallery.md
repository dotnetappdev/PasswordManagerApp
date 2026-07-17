---
title: Screenshot Gallery
---

<script setup>
const images = [
  { src: '/screenshots/blazor/onboarding/database-setup.png', alt: 'Database setup wizard', category: 'Onboarding' },
  { src: '/screenshots/blazor/onboarding/login.png', alt: 'Create master key / sign in', category: 'Onboarding' },

  { src: '/screenshots/blazor/dark/dashboard.png', alt: 'Dashboard', category: 'Dashboard' },
  { src: '/screenshots/blazor/dark/all-items.png', alt: 'All items', category: 'Dashboard' },
  { src: '/screenshots/blazor/dark/archive.png', alt: 'Archive', category: 'Dashboard' },
  { src: '/screenshots/blazor/dark/recently-deleted.png', alt: 'Recently deleted', category: 'Dashboard' },
  { src: '/screenshots/blazor/dark/profile.png', alt: 'Profile', category: 'Dashboard' },
  { src: '/screenshots/blazor/dark/import.png', alt: 'Import', category: 'Dashboard' },

  { src: '/screenshots/blazor/dark/vaults.png', alt: 'Vaults', category: 'Organize' },
  { src: '/screenshots/blazor/dark/collections.png', alt: 'Collections', category: 'Organize' },
  { src: '/screenshots/blazor/dark/categories.png', alt: 'Categories', category: 'Organize' },
  { src: '/screenshots/blazor/dark/tags.png', alt: 'Tags', category: 'Organize' },

  { src: '/screenshots/blazor/dark/security.png', alt: 'Security Center', category: 'Security' },
  { src: '/screenshots/blazor/dark/passkeys.png', alt: 'Passkeys', category: 'Security' },
  { src: '/screenshots/blazor/dark/api-keys.png', alt: 'API Keys', category: 'Security' },
  { src: '/screenshots/blazor/dark/audit-logs.png', alt: 'Audit Logs', category: 'Security' },

  { src: '/screenshots/blazor/dark/settings.png', alt: 'Settings — Security', category: 'Settings' },
  { src: '/screenshots/blazor/dark/settings-appearance.png', alt: 'Settings — Appearance', category: 'Settings' },
  { src: '/screenshots/blazor/dark/settings-database.png', alt: 'Settings — Database', category: 'Settings' },
  { src: '/screenshots/blazor/dark/settings-sync.png', alt: 'Settings — Sync', category: 'Settings' },
  { src: '/screenshots/blazor/dark/settings-notifications.png', alt: 'Settings — Notifications', category: 'Settings' },
  { src: '/screenshots/blazor/dark/settings-vaults.png', alt: 'Settings — Vaults', category: 'Settings' },
  { src: '/screenshots/blazor/dark/settings-generator.png', alt: 'Settings — Password Generator', category: 'Settings' },
  { src: '/screenshots/blazor/dark/settings-encryption.png', alt: 'Settings — Encryption', category: 'Settings' },
  { src: '/screenshots/blazor/dark/settings-shortcuts.png', alt: 'Settings — Keyboard Shortcuts', category: 'Settings' },
  { src: '/screenshots/blazor/dark/settings-maintenance.png', alt: 'Settings — Maintenance', category: 'Settings' },
  { src: '/screenshots/blazor/dark/settings-about.png', alt: 'Settings — About', category: 'Settings' },

  { src: '/screenshots/blazor/dark/dialog-add-item.png', alt: 'Add item — dynamic form + generator', category: 'Actions' },
  { src: '/screenshots/blazor/dark/dialog-new-vault.png', alt: 'Create a new vault', category: 'Actions' },
  { src: '/screenshots/blazor/dark/dialog-new-collection.png', alt: 'Create a new collection', category: 'Actions' },
  { src: '/screenshots/blazor/dark/dialog-api-key-generated.png', alt: 'API key created — one-time reveal dialog', category: 'Actions' },

  { src: '/screenshots/password-form.png', alt: 'Login / Password form', category: 'Item Forms' },
  { src: '/screenshots/credit-card-form.png', alt: 'Credit card form', category: 'Item Forms' },
  { src: '/screenshots/secure-note-form.png', alt: 'Secure note form', category: 'Item Forms' },
  { src: '/screenshots/identity-form.png', alt: 'Identity form', category: 'Item Forms' },
  { src: '/screenshots/api-credentials-form.png', alt: 'API credentials form', category: 'Item Forms' },

  { src: '/screenshots/blazor/light/dashboard.png', alt: 'Light theme — Dashboard', category: 'Themes' },
  { src: '/screenshots/blazor/light/all-items.png', alt: 'Light theme — All items', category: 'Themes' },
  { src: '/screenshots/blazor/light/vaults.png', alt: 'Light theme — Vaults', category: 'Themes' },
  { src: '/screenshots/blazor/light/collections.png', alt: 'Light theme — Collections', category: 'Themes' },
  { src: '/screenshots/blazor/light/security.png', alt: 'Light theme — Security Center', category: 'Themes' },
  { src: '/screenshots/blazor/light/settings.png', alt: 'Light theme — Settings', category: 'Themes' },
  { src: '/screenshots/blazor/dark/high-contrast.png', alt: 'High contrast theme', category: 'Themes' },

  { src: '/screenshots/api/scalar.png', alt: 'API — Scalar reference UI', category: 'API' },
  { src: '/screenshots/api/scalar-tryit.png', alt: 'API — Scalar Test Request (try it) panel', category: 'API' },
]
</script>

# Screenshot Gallery

Every screenshot on this site in one place — filter by category, click any tile to zoom in. These are
**live captures**, not mockups: the `Capture Screenshots` GitHub Actions workflow drives the actual
Blazor web app with Playwright on every push and commits fresh PNGs straight into `screenshots/blazor/`.
See the [Web platform page](/platforms/web) for narrative context, or
[Getting Started](/guide/getting-started) to run it yourself.

<Gallery :images="images" />
