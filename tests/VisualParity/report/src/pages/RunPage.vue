<script setup lang="ts">
import { computed, shallowRef, ref, watch, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { loadRun } from '../data/artifacts'
import type { Run } from '../data/schema'
import StatusBadge from '../components/StatusBadge.vue'
const route = useRoute(),
  router = useRouter(),
  run = shallowRef<Run>(),
  error = ref('')
const query = computed({
  get: () => (typeof route.query.q === 'string' ? route.query.q : ''),
  set: (q: string) => {
    void router.replace({ query: { ...route.query, q: q || undefined } })
  },
})
const suite = computed({
  get: () => (typeof route.query.suite === 'string' ? route.query.suite : ''),
  set: (suite: string) => {
    void router.replace({
      query: { ...route.query, suite: suite || undefined },
    })
  },
})
const status = computed({
  get: () => (typeof route.query.status === 'string' ? route.query.status : ''),
  set: (status: string) => {
    void router.replace({
      query: { ...route.query, status: status || undefined },
    })
  },
})
const id = computed(() => String(route.params.run))
const totals = computed(() =>
  run.value?.cases.reduce(
    (n, c) => ({
      equal: n.equal + c.coverage.equal,
      different: n.different + c.coverage.different,
      notCompared: n.notCompared + c.coverage.notCompared,
      errors: n.errors + c.errors,
    }),
    { equal: 0, different: 0, notCompared: 0, errors: 0 },
  ),
)
const suites = computed(() =>
  [...new Set(run.value?.cases.map((c) => c.suite) ?? [])].sort(),
)
const visible = computed(
  () =>
    run.value?.cases.filter(
      (c) =>
        `${c.id} ${c.title}`
          .toLowerCase()
          .includes(query.value.toLowerCase()) &&
        (!suite.value || c.suite === suite.value) &&
        (!status.value || c.status === status.value),
    ) ?? [],
)
let timer: ReturnType<typeof setInterval> | undefined
watch(
  id,
  async (_, __, cleanup) => {
    const controller = new AbortController()
    cleanup(() => {
      controller.abort()
      clearInterval(timer)
    })
    run.value = undefined
    error.value = ''
    let pending = false
    async function refresh() {
      if (pending) return
      pending = true
      try {
        const result = await loadRun(id.value, controller.signal)
        if (!controller.signal.aborted) run.value = result
        error.value = ''
      } catch (e) {
        if (!controller.signal.aborted) error.value = String(e)
      } finally {
        pending = false
      }
    }
    await refresh()
    if (!controller.signal.aborted) timer = setInterval(refresh, 5000)
  },
  { immediate: true },
)
onUnmounted(() => clearInterval(timer))
</script>
<template>
  <main class="suite">
    <RouterLink to="/runs">← All runs</RouterLink>
    <div class="page-heading">
      <div>
        <h1>{{ run?.id ?? 'Run' }}</h1>
        <p v-if="run">
          {{ run.command }} · {{ run.cases.length }} /
          {{ run.selectedCases }} cases · {{ run.state }}
        </p>
      </div>
    </div>
    <p v-if="error" class="error-message">{{ error }}</p>
    <p v-else-if="!run">Loading run…</p>
    <template v-else
      ><div class="suite-summary" aria-label="Suite coverage">
        <div class="summary-card">
          <span>Cases</span><b>{{ run!.cases.length }}</b>
        </div>
        <div v-for="(count, key) in totals" :key="key" class="summary-card">
          <span>{{
            key === 'notCompared'
              ? 'Not compared'
              : key === 'errors'
                ? 'Execution errors'
                : key === 'equal'
                  ? 'Equal'
                  : 'Different'
          }}</span
          ><b :class="key === 'errors' ? 'error' : key">{{ count }}</b>
        </div>
      </div>
      <section class="panel">
        <div class="toolbar">
          <input
            v-model="query"
            placeholder="Filter cases…"
            aria-label="Filter cases"
          /><select v-model="suite" aria-label="Suite">
            <option value="">All suites</option>
            <option v-for="s in suites" :key="s">{{ s }}</option></select
          ><select v-model="status" aria-label="Status">
            <option value="">All statuses</option>
            <option>equal</option>
            <option>different</option>
            <option>notCompared</option>
            <option>error</option></select
          ><span>{{ visible.length }} cases</span>
        </div>
        <div class="table-scroll">
          <table class="suite-table">
            <thead>
              <tr>
                <th>Case</th>
                <th>Samples</th>
                <th>Equal</th>
                <th>Different</th>
                <th>Not compared</th>
                <th>Errors</th>
                <th>Repeatability</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="c in visible" :key="c.id">
                <td>
                  <RouterLink
                    class="case-link"
                    :to="`/runs/${id}/cases/${c.id}`"
                    >{{ c.title }}</RouterLink
                  ><small>{{ c.suite }} · {{ c.id }}</small>
                  <div class="coverage-track" aria-hidden="true">
                    <span
                      v-for="k in [
                        'equal',
                        'different',
                        'notCompared',
                      ] as const"
                      :key="k"
                      :class="k"
                      :style="{
                        width:
                          (c.coverage.equal +
                          c.coverage.different +
                          c.coverage.notCompared
                            ? (c.coverage[k] /
                                (c.coverage.equal +
                                  c.coverage.different +
                                  c.coverage.notCompared)) *
                              100
                            : 0) + '%',
                      }"
                    ></span>
                  </div>
                  <StatusBadge :status="c.status" />
                </td>
                <td>{{ c.samples }}</td>
                <td>{{ c.coverage.equal }}</td>
                <td>{{ c.coverage.different }}</td>
                <td>{{ c.coverage.notCompared }}</td>
                <td>{{ c.errors }}</td>
                <td><StatusBadge :status="c.repeatability" /></td>
              </tr>
            </tbody>
          </table>
        </div></section
    ></template>
  </main>
</template>
