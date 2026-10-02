<script setup lang="ts">
import { ref, watch, onMounted } from 'vue'
const props = defineProps<{
  label: string
  width: number
  height: number
  zoom: number
  src?: string
  pixels?: Uint8ClampedArray
  regions: number[][]
}>()
const emit = defineEmits<{ pixel: [x: number, y: number, pin: boolean] }>()
const canvas = ref<HTMLCanvasElement>()
function draw() {
  if (!canvas.value) return
  const ctx = canvas.value.getContext('2d')!
  canvas.value.width = props.width
  canvas.value.height = props.height
  if (props.pixels) {
    const image = ctx.createImageData(props.width, props.height)
    image.data.set(props.pixels)
    ctx.putImageData(image, 0, 0)
  }
  ctx.strokeStyle = '#d43890'
  ctx.lineWidth = 1
  for (const [x, y, w, h] of props.regions)
    ctx.strokeRect(x! + 0.5, y! + 0.5, Math.max(0, w! - 1), Math.max(0, h! - 1))
}
watch(() => [props.width, props.height, props.pixels, props.regions], draw, {
  flush: 'post',
})
onMounted(draw)
function inspect(event: MouseEvent, pin: boolean) {
  const rect = canvas.value!.getBoundingClientRect()
  emit(
    'pixel',
    Math.floor(((event.clientX - rect.left) * props.width) / rect.width),
    Math.floor(((event.clientY - rect.top) * props.height) / rect.height),
    pin,
  )
}
</script>
<template>
  <figure>
    <figcaption>{{ label }}</figcaption>
    <div class="image-scroll">
      <div
        class="raster-layers"
        :style="{ width: width * zoom + 'px', height: height * zoom + 'px' }"
      >
        <img v-if="src" :src="src" :alt="label" /><canvas
          ref="canvas"
          :aria-label="label + ' raster'"
          @mousemove="inspect($event, false)"
          @click="inspect($event, true)"
        />
      </div>
    </div>
  </figure>
</template>
