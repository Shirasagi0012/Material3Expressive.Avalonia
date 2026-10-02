<script setup lang="ts">
import { computed, ref, shallowRef, watch, onUnmounted } from 'vue'
import { side, format, type Sample, type Metrics } from '../data/schema'
import { fileUrl } from '../data/artifacts'
import type { Input, Output, ImageSource } from '../imaging/messages'
import RasterCanvas from './RasterCanvas.vue'
const props = defineProps<{ run: string; caseId: string; sample: Sample }>()
const rasterId = ref(''),
  zoom = ref(2),
  opacity = ref(0.5),
  threshold = ref(0),
  regions = ref(false),
  loading = ref(false),
  error = ref('')
const rendered = shallowRef<Extract<Output, { type: 'rendered' }>>(),
  inspected = shallowRef<Extract<Output, { type: 'pixel' }>>()
const bounds = ref([0, 0, 0, 0]),
  regionMetrics = shallowRef<Metrics>(),
  x = ref(0),
  y = ref(0),
  pinned = ref(false),
  regionError = ref('')
const raster = computed(() => props.sample.rasters[rasterId.value])
const outlines = computed(() =>
  regions.value ? (raster.value?.regions?.map((r) => r.bounds) ?? []) : [],
)
let version = 0
const worker = new Worker(
  new URL('../imaging/raster.worker.ts', import.meta.url),
  { type: 'module' },
)
function send(message: Input) {
  worker.postMessage(message)
}
function render() {
  if (!loading.value && !error.value && raster.value?.metrics)
    send({
      version,
      type: 'render',
      opacity: Math.min(1, Math.max(0, Number(opacity.value) || 0)),
      threshold: Math.min(
        255,
        Math.max(0, Math.trunc(Number(threshold.value) || 0)),
      ),
    })
}
worker.onmessage = ({ data }: MessageEvent<Output>) => {
  if (data.version !== version) return
  if (data.type === 'error') {
    error.value = data.error
    loading.value = false
    rendered.value = undefined
  } else if (data.type === 'loaded') {
    loading.value = false
    render()
  } else if (data.type === 'rendered') rendered.value = data
  else if (data.type === 'pixel') inspected.value = data
  else if (data.type === 'region') regionMetrics.value = data.metrics
}
worker.onerror = (event) => {
  error.value = event.message
  loading.value = false
}
onUnmounted(() => worker.terminate())
watch(
  () => props.sample,
  () => {
    rasterId.value = Object.keys(props.sample.rasters)[0] ?? ''
  },
  { immediate: true },
)
watch(
  () => [props.run, props.caseId, props.sample.sample, rasterId.value],
  () => {
    version++
    error.value = ''
    rendered.value = undefined
    inspected.value = undefined
    regionMetrics.value = undefined
    regionError.value = ''
    pinned.value = false
    const r = raster.value
    loading.value = !!r
    const source = (name: 'compose' | 'avalonia'): ImageSource | null =>
      r?.present.includes(name)
        ? {
            url: fileUrl(
              props.run,
              `cases/${props.caseId}/${side(r.image, name)}`,
            ),
            width: side(r.width, name),
            height: side(r.height, name),
          }
        : null
    send({
      version,
      type: 'load',
      sources: { compose: source('compose'), avalonia: source('avalonia') },
      comparable: !!r?.metrics,
      recorded: r?.metrics,
    })
    bounds.value = r?.present.length
      ? [0, 0, side(r.width, r.present[0]!), side(r.height, r.present[0]!)]
      : [0, 0, 0, 0]
  },
  { immediate: true },
)
watch([opacity, threshold], () => {
  opacity.value = Math.min(1, Math.max(0, Number(opacity.value) || 0))
  threshold.value = Math.min(
    255,
    Math.max(0, Math.trunc(Number(threshold.value) || 0)),
  )
  render()
})
function analyzeRegion() {
  const e = raster.value
  if (!e) return
  const w = side(e.width, 'compose'),
    h = side(e.height, 'compose'),
    [left, top, width, height] = bounds.value
  regionError.value = ''
  regionMetrics.value = undefined
  if (
    bounds.value.some((n) => !Number.isInteger(n) || n < 0) ||
    left! + width! > w ||
    top! + height! > h
  ) {
    regionError.value = 'Region must fit within the image.'
    return
  }
  send({ version, type: 'region', bounds: [...bounds.value] })
}
function inspect(px: number, py: number, pin: boolean) {
  if (loading.value || error.value || (!pin && pinned.value)) return
  x.value = px
  y.value = py
  pinned.value = pin
  send({ version, type: 'pixel', x: px, y: py })
}
</script>
<template>
  <div v-if="!raster" class="empty">
    <p>No image captured at this sample.</p>
    <pre>{{ format(sample.rasterCollection) }}</pre>
  </div>
  <div v-else>
    <div class="controls">
      <label
        >Raster<select v-model="rasterId">
          <option v-for="(_, id) in sample.rasters" :key="id">{{ id }}</option>
        </select></label
      ><label
        >Zoom<select v-model.number="zoom">
          <option v-for="n in [1, 2, 4, 8]" :key="n" :value="n">
            {{ n }}×
          </option>
        </select></label
      ><label
        >Overlay<input
          v-model.number="opacity"
          type="range"
          min="0"
          max="1"
          step=".01"
          aria-label="Overlay opacity"
          :disabled="!raster.metrics" /></label
      ><label
        >Highlight Δ &gt;<input
          v-model.number="threshold"
          type="number"
          min="0"
          max="255"
          aria-label="Difference threshold"
          :disabled="!raster.metrics" /></label
      ><label><input v-model="regions" type="checkbox" />Point regions</label>
    </div>
    <p v-if="loading">Decoding selected PNGs…</p>
    <p v-if="error" class="error-message">{{ error }}</p>
    <p v-if="raster.reason">{{ raster.reason }}</p>
    <p v-if="raster.metrics" class="raster-description">
      Different pixels <b>{{ raster.metrics.differentPixels }}</b> · Max channel
      Δ <code>{{ raster.metrics.maximumChannelDelta }}</code> · Highlighted
      <b>{{ rendered?.highlighted ?? '…' }}</b
      ><small
        >Threshold affects the view only. Recorded comparison metrics are
        authoritative.</small
      >
    </p>
    <div class="pixel-reader">
      <div class="controls">
        <label
          >X<input
            v-model.number="x"
            type="number"
            aria-label="Pixel X" /></label
        ><label
          >Y<input
            v-model.number="y"
            type="number"
            aria-label="Pixel Y" /></label
        ><button :disabled="loading || !!error" @click="inspect(x, y, true)">
          Read pixel</button
        ><button v-if="pinned" @click="pinned = false">Unpin</button>
      </div>
      <pre class="pixel-readout" aria-live="polite">{{
        inspected
          ? `(${inspected.x}, ${inspected.y}) · straight-sRGBA8\nCompose ${format(inspected.compose)}\nAvalonia ${format(inspected.avalonia)}\nΔ signed ${format(inspected.delta)}\nΔ absolute ${format(inspected.delta?.map(Math.abs))}`
          : 'Hover a pixel to inspect; click to pin.'
      }}</pre>
    </div>
    <div class="raster-grid">
      <RasterCanvas
        v-for="s in raster.present"
        :key="s"
        :label="s === 'compose' ? 'Compose' : 'Avalonia'"
        :width="side(raster.width, s)"
        :height="side(raster.height, s)"
        :zoom="zoom"
        :src="fileUrl(run, `cases/${caseId}/${side(raster.image, s)}`)"
        :regions="outlines"
        @pixel="inspect"
      /><template v-if="rendered"
        ><RasterCanvas
          label="Overlay"
          :width="rendered.width"
          :height="rendered.height"
          :zoom="zoom"
          :pixels="rendered.overlay"
          :regions="outlines"
          @pixel="inspect" /><RasterCanvas
          label="Difference · max |channel Δ|"
          :width="rendered.width"
          :height="rendered.height"
          :zoom="zoom"
          :pixels="rendered.difference"
          :regions="outlines"
          @pixel="inspect"
      /></template>
    </div>
    <details>
      <summary>Recorded regions and metrics</summary>
      <pre>{{ format(raster) }}</pre>
    </details>
    <details v-if="raster.metrics">
      <summary>Analyze a region</summary>
      <div class="controls">
        <label v-for="(label, i) in ['X', 'Y', 'Width', 'Height']" :key="label"
          >{{ label
          }}<input
            v-model.number="bounds[i]"
            type="number"
            :aria-label="'Region ' + label" /></label
        ><button :disabled="loading || !!error" @click="analyzeRegion">
          Analyze region
        </button>
      </div>
      <p v-if="regionError" class="error-message">{{ regionError }}</p>
      <pre>{{ format(regionMetrics) }}</pre>
    </details>
  </div>
</template>
