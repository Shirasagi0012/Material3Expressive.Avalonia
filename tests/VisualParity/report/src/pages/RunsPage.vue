<script setup lang="ts">
import { onMounted, onUnmounted, ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { listRuns } from '../data/artifacts'
import type { RunInfo } from '../data/schema'
const route = useRoute(),
  router = useRouter()
const query = computed({
  get: () => (typeof route.query.q === 'string' ? route.query.q : ''),
  set: (q: string) => {
    void router.replace({ query: { ...route.query, q: q || undefined } })
  },
})
const runs = ref<RunInfo[]>([]),
  errors = ref<string[]>([]),
  loading = ref(true)
const visible = computed(() =>
  runs.value.filter((r) =>
    `${r.id} ${r.command}`.toLowerCase().includes(query.value.toLowerCase()),
  ),
)
let timer: ReturnType<typeof setInterval>,
  controller = new AbortController(),
  pending = false
async function refresh() {
  if (pending) return
  pending = true
  try {
    const result = await listRuns(controller.signal)
    runs.value = result.runs
    errors.value = result.errors
  } catch (error) {
    if (!controller.signal.aborted) errors.value = [String(error)]
  } finally {
    loading.value = false
    pending = false
  }
}
onMounted(() => {
  void refresh()
  timer = setInterval(refresh, 5000)
})
onUnmounted(() => {
  clearInterval(timer)
  controller.abort()
})
</script>
<template>
  <main class="suite">
    <div class="page-heading">
      <div>
        <h1>Test runs</h1>
        <p>Choose a run to inspect captured facts and visual differences.</p>
      </div>
      <button @click="refresh">Refresh</button>
    </div>
    <div v-for="error in errors" :key="error" class="error-message">
      {{ error }}
    </div>
    <section class="panel">
      <div class="toolbar">
        <input
          v-model="query"
          type="search"
          placeholder="Filter runs…"
          aria-label="Filter runs"
        /><span>{{ visible.length }} runs</span>
      </div>
      <p v-if="loading">Loading runs…</p>
      <p v-else-if="!visible.length" class="empty">
        No runs found. Run parity tests to generate results.
      </p>
      <table v-else>
        <thead>
          <tr>
            <th>Run</th>
            <th>Started</th>
            <th>Command</th>
            <th>Cases</th>
            <th>State</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="run in visible" :key="run.key">
            <td>
              <RouterLink :to="`/runs/${run.key}`">{{ run.id }}</RouterLink>
            </td>
            <td>{{ new Date(run.startedAt).toLocaleString() }}</td>
            <td>{{ run.command }}</td>
            <td>{{ run.completedCases }} / {{ run.selectedCases }}</td>
            <td>
              {{
                run.state === 'running' ? 'Running / unfinished' : 'Completed'
              }}
            </td>
          </tr>
        </tbody>
      </table>
    </section>
  </main>
</template>
