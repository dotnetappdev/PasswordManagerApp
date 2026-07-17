import DefaultTheme from 'vitepress/theme'
import type { Theme } from 'vitepress'
import Gallery from './components/Gallery.vue'
import ApiTryIt from './components/ApiTryIt.vue'

export default {
  extends: DefaultTheme,
  enhanceApp({ app }) {
    app.component('Gallery', Gallery)
    app.component('ApiTryIt', ApiTryIt)
  },
} satisfies Theme
