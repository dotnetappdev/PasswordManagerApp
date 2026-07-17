<script setup>
import '@scalar/api-reference/style.css'
import { ref, computed, defineAsyncComponent, onMounted } from 'vue'

// Loaded async (and only ever mounted client-side, see <ClientOnly> below) so this heavy,
// browser-only client library never touches VitePress's SSR build pass.
const ApiReference = defineAsyncComponent(() =>
  import('@scalar/api-reference').then((m) => m.ApiReference)
)

const STORAGE_KEY = 'vg_api_tryit_base_url'
const DEFAULT_URL = 'http://localhost:5280'

const baseUrlInput = ref(DEFAULT_URL)
const appliedUrl = ref(DEFAULT_URL)

onMounted(() => {
  const saved = window.localStorage.getItem(STORAGE_KEY)
  if (saved) {
    baseUrlInput.value = saved
    appliedUrl.value = saved
  }
})

function normalize(url) {
  return url.trim().replace(/\/+$/, '')
}

function load() {
  const normalized = normalize(baseUrlInput.value) || DEFAULT_URL
  baseUrlInput.value = normalized
  appliedUrl.value = normalized
  window.localStorage.setItem(STORAGE_KEY, normalized)
}

// `content` is not used - Scalar fetches `url` itself, which keeps this always in sync with
// whatever version of the API is actually running, rather than a spec baked in at docs-build time.
const configuration = computed(() => ({
  url: `${appliedUrl.value}/swagger/v1/swagger.json`,
  baseServerURL: appliedUrl.value,
  servers: [{ url: appliedUrl.value }],
  theme: 'bluePlanet',
  showSidebar: true,
  _integration: 'dotnet',
  withDefaultFonts: false,
}))
</script>

<template>
  <div class="api-tryit">
    <div class="api-tryit-bar">
      <label for="api-tryit-url">Your VaultGuard.API URL</label>
      <input
        id="api-tryit-url"
        v-model="baseUrlInput"
        type="text"
        placeholder="http://localhost:5280"
        spellcheck="false"
        @keyup.enter="load"
      />
      <button type="button" @click="load">Load</button>
    </div>
    <p class="api-tryit-hint">
      This calls <strong>your own running instance</strong> directly from your browser - nothing goes
      through this docs site or any third party. Run <code>VaultGuard.API</code> (see the
      <a href="/PasswordManagerApp/guide/getting-started">Getting Started guide</a>), point the field
      above at it, hit <strong>Load</strong>, then open any endpoint below and click
      <strong>Test Request</strong>. Because this page runs on a different origin than your API, add this
      site's origin to <code>Cors:AllowedOrigins</code> in your API config first (already true for plain
      <code>localhost</code> origins by default) - see
      <a href="/PasswordManagerApp/api/reference">API Reference</a> for details.
    </p>
    <ClientOnly>
      <Suspense>
        <ApiReference :key="appliedUrl" :configuration="configuration" />
        <template #fallback>
          <div class="api-tryit-loading">Loading API reference from {{ appliedUrl }}…</div>
        </template>
      </Suspense>
    </ClientOnly>
  </div>
</template>

<style scoped>
.api-tryit-bar {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
  margin: 16px 0 8px;
  padding: 12px;
  border: 1px solid var(--vp-c-divider);
  border-radius: 8px;
  background: var(--vp-c-bg-soft);
}
.api-tryit-bar label {
  font-weight: 600;
  font-size: 0.9em;
  white-space: nowrap;
}
.api-tryit-bar input {
  flex: 1;
  min-width: 220px;
  padding: 6px 10px;
  border: 1px solid var(--vp-c-divider);
  border-radius: 6px;
  background: var(--vp-c-bg);
  color: var(--vp-c-text-1);
  font-family: var(--vp-font-family-mono);
  font-size: 0.9em;
}
.api-tryit-bar button {
  padding: 6px 16px;
  border-radius: 6px;
  border: none;
  background: var(--vp-c-brand-1);
  color: white;
  font-weight: 600;
  cursor: pointer;
}
.api-tryit-bar button:hover {
  background: var(--vp-c-brand-2);
}
.api-tryit-hint {
  font-size: 0.85em;
  color: var(--vp-c-text-2);
  margin: 0 0 16px;
}
.api-tryit-loading {
  padding: 40px;
  text-align: center;
  color: var(--vp-c-text-2);
}
.api-tryit :deep(.scalar-app) {
  border-radius: 8px;
  overflow: hidden;
  min-height: 600px;
}
</style>
