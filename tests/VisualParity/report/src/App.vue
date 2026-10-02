<script setup lang="ts">
import { computed, watch } from 'vue'
import { useRoute } from 'vue-router'
const route = useRoute()
const isCase = computed(() => typeof route.params.case === 'string')
watch(
  isCase,
  (value) =>
    document.getElementById('app')?.classList.toggle('case-app', value),
  { immediate: true },
)
</script>
<template>
  <header class="brandbar">
    <RouterLink to="/runs" class="brandmark" aria-label="Visual parity"
      ><svg
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        stroke-width="2"
      >
        <path d="m8 6-5 6 5 6m8-12 5 6-5 6M14 3l-4 18" /></svg
    ></RouterLink>
    <span class="product">Material 3 Expressive for Avalonia</span
    ><span class="divider"></span
    ><RouterLink to="/runs">Visual Parity Test Report</RouterLink>
    <RouterLink v-if="isCase" to="/runs" class="button right"
      >All runs</RouterLink
    >
  </header>
  <RouterView />
</template>
