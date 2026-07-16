import { defineConfig } from 'vitepress'
import { withMermaid } from 'vitepress-plugin-mermaid'

export default withMermaid(defineConfig({
  title: 'VaultGuard',
  description: 'Developer documentation for VaultGuard - a self-hosted, zero-knowledge password manager.',
  base: '/PasswordManagerApp/',
  cleanUrls: true,
  lastUpdated: true,

  head: [
    ['link', { rel: 'icon', href: '/PasswordManagerApp/logo.png' }],
  ],

  themeConfig: {
    logo: '/logo.png',
    siteTitle: 'VaultGuard',

    nav: [
      { text: 'Guide', link: '/guide/overview' },
      { text: 'Platforms', link: '/platforms/web' },
      { text: 'Gallery', link: '/gallery' },
      { text: 'API Reference', link: '/api/reference' },
      {
        text: 'More',
        items: [
          { text: 'Allure test report', link: 'https://dotnetappdev.github.io/PasswordManagerApp/allure-report/' },
          { text: 'GitHub', link: 'https://github.com/dotnetappdev/PasswordManagerApp' },
        ],
      },
    ],

    sidebar: {
      '/guide/': [
        {
          text: 'Guide',
          items: [
            { text: 'Overview', link: '/guide/overview' },
            { text: 'Architecture', link: '/guide/architecture' },
            { text: 'Getting Started', link: '/guide/getting-started' },
            { text: 'Security Model', link: '/guide/security' },
          ],
        },
      ],
      '/platforms/': [
        {
          text: 'Platforms',
          items: [
            { text: 'Web (Blazor)', link: '/platforms/web' },
            { text: 'Android (MAUI)', link: '/platforms/android' },
            { text: 'Windows Desktop (WPF)', link: '/platforms/windows' },
            { text: 'API Server', link: '/platforms/api' },
          ],
        },
      ],
      '/api/': [
        {
          text: 'API Reference',
          items: [
            { text: 'Overview & Auth', link: '/api/reference' },
            { text: 'Password Items', link: '/api/password-items' },
            { text: 'Collections, Categories & Tags', link: '/api/organization' },
            { text: 'Users & Settings', link: '/api/users' },
          ],
        },
      ],
    },

    socialLinks: [
      { icon: 'github', link: 'https://github.com/dotnetappdev/PasswordManagerApp' },
    ],

    footer: {
      message: 'Released under the license in the repository.',
      copyright: 'VaultGuard',
    },

    search: {
      provider: 'local',
    },
  },
}))
