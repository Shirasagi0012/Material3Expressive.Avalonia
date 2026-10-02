<script setup lang="ts">
defineProps<{ status: string }>()
const labels: Record<string, string> = {
  equal: 'Equal',
  different: 'Different',
  notCompared: 'Not compared',
  error: 'Error',
  stable: 'Stable',
  unstable: 'Unstable',
  notChecked: 'Not checked',
}
</script>
<template>
  <span class="badge" :class="status">{{ labels[status] ?? status }}</span>
</template>
