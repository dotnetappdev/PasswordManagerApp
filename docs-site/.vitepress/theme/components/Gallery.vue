<script setup>
import { ref, computed, watch, onUnmounted } from 'vue'
import { withBase } from 'vitepress'

const props = defineProps({
  images: { type: Array, required: true }, // [{ src, alt, category }]
})

const filter = ref('All')
const activeIndex = ref(-1)

const categories = computed(() => {
  const seen = []
  for (const img of props.images) {
    if (!seen.includes(img.category)) seen.push(img.category)
  }
  return ['All', ...seen]
})

const filtered = computed(() =>
  filter.value === 'All' ? props.images : props.images.filter((i) => i.category === filter.value)
)

const active = computed(() => (activeIndex.value >= 0 ? filtered.value[activeIndex.value] : null))

function open(img) {
  activeIndex.value = filtered.value.indexOf(img)
}
function close() {
  activeIndex.value = -1
}
function next() {
  if (filtered.value.length === 0) return
  activeIndex.value = (activeIndex.value + 1) % filtered.value.length
}
function prev() {
  if (filtered.value.length === 0) return
  activeIndex.value = (activeIndex.value - 1 + filtered.value.length) % filtered.value.length
}
function onKeydown(e) {
  if (activeIndex.value < 0) return
  if (e.key === 'Escape') close()
  else if (e.key === 'ArrowRight') next()
  else if (e.key === 'ArrowLeft') prev()
}

watch(filter, () => close())

if (typeof window !== 'undefined') {
  window.addEventListener('keydown', onKeydown)
  onUnmounted(() => window.removeEventListener('keydown', onKeydown))
}
</script>

<template>
  <div class="vg-gallery">
    <div class="vg-filters" role="tablist" aria-label="Filter screenshots by category">
      <button
        v-for="c in categories"
        :key="c"
        type="button"
        class="vg-chip"
        :class="{ 'vg-chip--active': filter === c }"
        role="tab"
        :aria-selected="filter === c"
        @click="filter = c"
      >
        {{ c }}
      </button>
    </div>

    <div class="vg-grid">
      <button
        v-for="img in filtered"
        :key="img.src"
        type="button"
        class="vg-tile"
        @click="open(img)"
      >
        <img :src="withBase(img.src)" :alt="img.alt" loading="lazy" />
        <span class="vg-tile-overlay">
          <span class="vg-tile-label">{{ img.alt }}</span>
        </span>
      </button>
    </div>

    <Teleport to="body">
      <div v-if="active" class="vg-lightbox" @click.self="close">
        <button class="vg-lightbox-btn vg-lightbox-close" type="button" aria-label="Close" @click="close">✕</button>
        <button class="vg-lightbox-btn vg-lightbox-prev" type="button" aria-label="Previous image" @click="prev">‹</button>
        <figure class="vg-lightbox-figure">
          <img :src="withBase(active.src)" :alt="active.alt" class="vg-lightbox-img" />
          <figcaption class="vg-lightbox-caption">{{ active.alt }}</figcaption>
        </figure>
        <button class="vg-lightbox-btn vg-lightbox-next" type="button" aria-label="Next image" @click="next">›</button>
      </div>
    </Teleport>
  </div>
</template>

<style scoped>
.vg-gallery {
  margin: 24px 0 40px;
}

.vg-filters {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-bottom: 20px;
  position: sticky;
  top: var(--vp-nav-height, 64px);
  z-index: 10;
  padding: 10px 0;
  background: color-mix(in srgb, var(--vp-c-bg) 88%, transparent);
  backdrop-filter: blur(6px);
}

.vg-chip {
  font-size: 13px;
  font-weight: 600;
  padding: 6px 14px;
  border-radius: 999px;
  border: 1px solid var(--vp-c-divider);
  background: var(--vp-c-bg-soft);
  color: var(--vp-c-text-2);
  cursor: pointer;
  transition: all 0.18s ease;
  white-space: nowrap;
}

.vg-chip:hover {
  border-color: var(--vp-c-brand-1);
  color: var(--vp-c-brand-1);
}

.vg-chip--active {
  background: linear-gradient(135deg, var(--vp-c-brand-1), var(--vp-c-brand-2));
  border-color: transparent;
  color: white;
}

.vg-grid {
  column-count: 1;
  column-gap: 18px;
}

@media (min-width: 560px) {
  .vg-grid { column-count: 2; }
}
@media (min-width: 900px) {
  .vg-grid { column-count: 3; }
}
@media (min-width: 1300px) {
  .vg-grid { column-count: 4; }
}

.vg-tile {
  display: block;
  width: 100%;
  margin: 0 0 18px;
  break-inside: avoid;
  position: relative;
  border-radius: 12px;
  overflow: hidden;
  border: 1px solid var(--vp-c-divider);
  background: var(--vp-c-bg-soft);
  padding: 0;
  cursor: zoom-in;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
  transition: transform 0.22s ease, box-shadow 0.22s ease, border-color 0.22s ease;
}

.vg-tile:hover,
.vg-tile:focus-visible {
  transform: translateY(-3px);
  border-color: var(--vp-c-brand-1);
  box-shadow: 0 10px 28px rgba(0, 0, 0, 0.18);
}

.vg-tile img {
  display: block;
  width: 100%;
  height: auto;
  transition: transform 0.35s ease;
}

.vg-tile:hover img {
  transform: scale(1.035);
}

.vg-tile-overlay {
  position: absolute;
  inset: auto 0 0 0;
  padding: 22px 12px 10px;
  background: linear-gradient(to top, rgba(0, 0, 0, 0.72), transparent);
  opacity: 0;
  transform: translateY(6px);
  transition: opacity 0.2s ease, transform 0.2s ease;
}

.vg-tile:hover .vg-tile-overlay,
.vg-tile:focus-visible .vg-tile-overlay {
  opacity: 1;
  transform: translateY(0);
}

.vg-tile-label {
  color: #fff;
  font-size: 12.5px;
  font-weight: 600;
  text-shadow: 0 1px 3px rgba(0, 0, 0, 0.5);
}

.vg-lightbox {
  position: fixed;
  inset: 0;
  z-index: 100;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(10, 10, 14, 0.86);
  backdrop-filter: blur(4px);
  animation: vg-fade-in 0.15s ease;
}

@keyframes vg-fade-in {
  from { opacity: 0; }
  to { opacity: 1; }
}

.vg-lightbox-figure {
  max-width: min(92vw, 1200px);
  max-height: 88vh;
  margin: 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
}

.vg-lightbox-img {
  max-width: 100%;
  max-height: 78vh;
  border-radius: 10px;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.5);
  animation: vg-scale-in 0.18s ease;
}

@keyframes vg-scale-in {
  from { opacity: 0; transform: scale(0.97); }
  to { opacity: 1; transform: scale(1); }
}

.vg-lightbox-caption {
  color: #fff;
  font-size: 14px;
  font-weight: 600;
  opacity: 0.9;
}

.vg-lightbox-btn {
  position: absolute;
  border: none;
  background: rgba(255, 255, 255, 0.08);
  color: #fff;
  cursor: pointer;
  border-radius: 999px;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 0.15s ease;
}

.vg-lightbox-btn:hover {
  background: rgba(255, 255, 255, 0.2);
}

.vg-lightbox-close {
  top: 20px;
  right: 20px;
  width: 40px;
  height: 40px;
  font-size: 18px;
}

.vg-lightbox-prev,
.vg-lightbox-next {
  top: 50%;
  transform: translateY(-50%);
  width: 52px;
  height: 52px;
  font-size: 28px;
}

.vg-lightbox-prev { left: 16px; }
.vg-lightbox-next { right: 16px; }

@media (max-width: 640px) {
  .vg-lightbox-prev, .vg-lightbox-next { width: 40px; height: 40px; font-size: 22px; }
}
</style>
