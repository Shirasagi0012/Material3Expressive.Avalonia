<script setup lang="ts">
import { computed, ref, shallowRef, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useCase } from '../composables/useCase'
import { useSample } from '../composables/useSample'
import { loadSample, fileUrl } from '../data/artifacts'
import { fields, format, type Sample, type Trace } from '../data/schema'
import SampleTimeline from '../components/SampleTimeline.vue'
import MeasurementTable from '../components/MeasurementTable.vue'
import FieldInspector from '../components/FieldInspector.vue'
import FieldHistory from '../components/FieldHistory.vue'
import RasterInspector from '../components/RasterInspector.vue'
import StatusBadge from '../components/StatusBadge.vue'
const route = useRoute(),
  router = useRouter(),
  run = computed(() => String(route.params.run)),
  caseId = computed(() => String(route.params.case))
const { testCase, environment, loading, error } = useCase(run, caseId)
const sampleId = computed(() =>
  typeof route.query.sample === 'string'
    ? route.query.sample
    : (testCase.value?.sampleSummaries[0]?.id ?? ''),
)
const {
  sample,
  loading: sampleLoading,
  error: sampleError,
} = useSample(run, testCase, sampleId)
const pointId = computed(() =>
  typeof route.query.point === 'string' &&
  (!sample.value || sample.value.points[route.query.point])
    ? route.query.point
    : (Object.keys(sample.value?.points ?? {})[0] ?? ''),
)
const point = computed(() => sample.value?.points[pointId.value])
const fieldName = computed(() =>
  typeof route.query.field === 'string' && route.query.field.length > 0
    ? route.query.field
    : (fields(point.value)[0]?.name ?? ''),
)
const row = computed(() =>
  fields(point.value).find((r) => r.name === fieldName.value),
)
const tabs = [
  'Details',
  'Descriptor',
  'Trends',
  'Raster',
  'Trace',
  'Environment',
  'Raw',
  'Repeatability',
  'Config',
  'Overview',
]
const tab = computed(() =>
  typeof route.query.tab === 'string' && tabs.includes(route.query.tab)
    ? route.query.tab
    : 'Details',
)
function select(query: Record<string, string>) {
  void router.push({ query: { ...route.query, ...query } })
}
const samplePosition = computed(
  () =>
    testCase.value?.sampleSummaries.findIndex((s) => s.id === sampleId.value) ??
    -1,
)
const navigation = ref<'Samples' | 'Fields'>('Samples'),
  navQuery = ref(''),
  rawMode = ref('field')
function setNavigation(value: 'Samples' | 'Fields') {
  navigation.value = value
  navQuery.value = ''
}
const pointFields = computed(() => fields(point.value))
const fieldGroups = computed(() =>
  ['geometry', 'text', 'properties', 'motion', 'mixed']
    .map((category) => ({
      category,
      rows: pointFields.value.filter((f) => f.category === category),
    }))
    .filter((g) => g.rows.length),
)
const visibleSamples = computed(
  () =>
    testCase.value?.sampleSummaries.filter((s) =>
      [s.id, s.stage, s.expectedTimeUs, s.actualTimeUs]
        .join(' ')
        .toLowerCase()
        .includes(navQuery.value.toLowerCase()),
    ) ?? [],
)
const sampleCounts = (
  s: NonNullable<typeof testCase.value>['sampleSummaries'][number],
) =>
  Object.values(s.categories).reduce(
    (n, c) => ({
      equal: n.equal + c.equal,
      different: n.different + c.different,
      notCompared: n.notCompared + c.notCompared,
    }),
    { ...s.rasters },
  )
const totals = computed(() =>
  testCase.value?.sampleSummaries.reduce(
    (n, s) => {
      const c = sampleCounts(s)
      return {
        equal: n.equal + c.equal,
        different: n.different + c.different,
        notCompared: n.notCompared + c.notCompared,
      }
    },
    { equal: 0, different: 0, notCompared: 0 },
  ),
)
const rawValue = computed(() =>
  rawMode.value === 'field'
    ? row.value?.field
    : rawMode.value === 'point'
      ? point.value
      : rawMode.value === 'case'
        ? testCase.value
        : sample.value,
)
const allSamples = shallowRef<Sample[]>([]),
  historyLoading = ref(false),
  historyError = ref('')
watch(
  [testCase, tab, navigation],
  async (_, __, cleanup) => {
    if (
      !testCase.value ||
      (!['Trends', 'Trace'].includes(tab.value) &&
        navigation.value !== 'Fields') ||
      allSamples.value.length === Object.keys(testCase.value.samples).length
    )
      return
    const controller = new AbortController()
    cleanup(() => controller.abort())
    historyLoading.value = true
    historyError.value = ''
    const ids = Object.keys(testCase.value.samples),
      result: Sample[] = []
    try {
      for (let i = 0; i < ids.length; i += 8)
        result.push(
          ...(await Promise.all(
            ids
              .slice(i, i + 8)
              .map((id) =>
                loadSample(run.value, testCase.value!, id, controller.signal),
              ),
          )),
        )
      if (!controller.signal.aborted) allSamples.value = result
    } catch (e) {
      if (!controller.signal.aborted) historyError.value = String(e)
    } finally {
      if (!controller.signal.aborted) historyLoading.value = false
    }
  },
  { immediate: true },
)
watch([run, caseId], () => {
  allSamples.value = []
  historyError.value = ''
  historyLoading.value = false
})
const histories = computed(() => {
  const entries = new Map<
    string,
    {
      point: string
      name: string
      sample: string
      status: string
      different: number
      notCompared: number
    }
  >()
  for (const s of allSamples.value)
    for (const [pid, p] of Object.entries(s.points))
      for (const f of fields(p)) {
        const key = `${pid}.${f.name}`,
          old = entries.get(key)
        if (old) {
          old.different += f.field.status === 'different' ? 1 : 0
          old.notCompared += f.field.status === 'notCompared' ? 1 : 0
        }
        if (
          !old ||
          (old.status !== 'different' && f.field.status === 'different') ||
          (old.status === 'equal' && f.field.status !== 'equal')
        )
          entries.set(key, {
            point: pid,
            name: f.name,
            sample: s.sample,
            status: f.field.status,
            different:
              old?.different ?? (f.field.status === 'different' ? 1 : 0),
            notCompared:
              old?.notCompared ?? (f.field.status === 'notCompared' ? 1 : 0),
          })
      }
  return [...entries.values()].filter((f) =>
    `${f.point}.${f.name}`.toLowerCase().includes(navQuery.value.toLowerCase()),
  )
})
const traces = computed(
  () =>
    Object.fromEntries(
      (['compose', 'avalonia'] as const).map((name) => {
        const combined: Trace[] = [
          ...allSamples.value.flatMap((s) => s.trace[name] ?? []),
          ...(testCase.value?.captures[name]?.traceTail ?? []),
        ]
        return [name, combined.sort((a, b) => a.sequence - b.sequence)]
      }),
    ) as Record<'compose' | 'avalonia', Trace[]>,
)
const environmentKeys = computed(() =>
  [
    ...new Set(
      Object.values(environment.value ?? {}).flatMap((v) =>
        Object.keys(v ?? {}),
      ),
    ),
  ].sort(),
)
</script>
<template>
  <div class="case-page">
    <div class="casebar">
      <RouterLink
        class="button"
        :to="`/runs/${run}`"
        aria-label="Back to case list"
        >← All cases</RouterLink
      >
      <div class="breadcrumb">
        <span class="muted">{{ testCase?.metadata.suite }}</span
        ><span class="chevron">›</span><span>{{ caseId }}</span
        ><span class="chevron">›</span>
        <h1>{{ testCase?.metadata.title ?? 'Loading case…' }}</h1>
      </div>
      <div class="counts" aria-label="Case coverage">
        <span
          v-for="(count, status) in totals"
          :key="status"
          class="chip"
          :class="status"
          >{{
            status === 'notCompared'
              ? 'Not compared'
              : status === 'different'
                ? 'Different'
                : 'Equal'
          }}
          <b>{{ count }}</b></span
        >
      </div>
      <a
        v-if="testCase"
        class="button"
        :href="fileUrl(run, `cases/${caseId}/overview.md`)"
        target="_blank"
        >Overview ↗</a
      >
      <a
        v-if="sampleId && testCase?.samples[sampleId]"
        class="button"
        :href="fileUrl(run, `cases/${caseId}/${testCase.samples[sampleId]}`)"
        target="_blank"
        >Sample YAML ↗</a
      >
    </div>
    <p v-if="loading" class="notice">Loading case…</p>
    <p v-if="error" class="notice error-banner">{{ error }}</p>
    <pre v-if="testCase?.errors.length" class="notice error-banner">{{
      testCase.errors.join('\n')
    }}</pre>
    <p v-if="testCase?.diagnostics" class="notice error-banner">
      Diagnostics retained: {{ testCase.diagnostics }}
    </p>
    <main v-if="testCase" class="shell">
      <aside class="panel sidebar" aria-label="Report navigation">
        <nav class="tabs" role="tablist" aria-label="Navigation">
          <button
            v-for="n in ['Samples', 'Fields'] as const"
            :key="n"
            role="tab"
            :aria-selected="navigation === n"
            @click="setNavigation(n)"
          >
            {{ n }}
          </button>
        </nav>
        <div class="sidebar-search">
          <input
            v-model="navQuery"
            type="search"
            :placeholder="
              navigation === 'Samples' ? 'Filter samples…' : 'Filter fields…'
            "
            aria-label="Filter navigation"
          />
        </div>
        <div v-if="navigation === 'Samples'" class="sidebar-content">
          <div class="nav-header">
            <span>Time (µs)</span><span>Sample / stage</span><span>Δ / —</span>
          </div>
          <button
            v-for="s in visibleSamples"
            :key="s.id"
            class="sample-row"
            :aria-current="s.id === sampleId"
            :title="
              `${s.id} · ${s.stage}\n` +
              Object.entries(s.categories)
                .map(
                  ([k, c]) =>
                    `${k}: ${c.different} different / ${c.notCompared} not compared`,
                )
                .join('\n')
            "
            @click="select({ sample: s.id })"
          >
            <span class="numeric">{{
              s.expectedTimeUs === s.actualTimeUs
                ? s.expectedTimeUs
                : `${s.expectedTimeUs ?? '—'} / ${s.actualTimeUs ?? '—'}`
            }}</span
            ><span
              ><span class="sample-name">{{ s.id }}</span
              ><small>{{ s.stage }}</small></span
            ><span class="delta-count" :class="s.status"
              >{{ sampleCounts(s).different
              }}<small v-if="sampleCounts(s).notCompared"
                >{{ sampleCounts(s).notCompared }} —</small
              ></span
            >
          </button>
          <p v-if="!visibleSamples.length" class="empty">
            No matching samples.
          </p>
        </div>
        <div v-else class="sidebar-content">
          <p v-if="historyLoading" class="empty">Loading field histories…</p>
          <p v-if="historyError" class="notice">{{ historyError }}</p>
          <button
            v-for="h in histories"
            :key="h.point + '/' + h.name"
            class="history-row"
            :aria-current="pointId === h.point && fieldName === h.name"
            @click="
              select({
                point: h.point,
                field: h.name,
                sample: h.sample,
                tab: 'Trends',
              })
            "
          >
            <span class="history-name">{{ h.point }}/{{ h.name }}</span
            ><span class="history-counts"
              ><span :class="h.different ? 'different' : 'equal'"
                >{{ h.different }} different</span
              ><span>{{ h.notCompared }} not compared</span></span
            >
          </button>
        </div>
        <div class="sidebar-foot">
          {{ testCase.sampleSummaries.length }} samples<span
            v-if="allSamples.length"
          >
            · {{ histories.length }} field histories</span
          >
        </div>
      </aside>
      <div class="workspace">
        <section class="panel measurements" aria-label="Sample measurements">
          <div class="toolbar">
            <div class="sample-heading">
              <h2>{{ sampleId || 'No samples submitted' }}</h2>
              <small v-if="sample"
                >{{ format(sample.stage) }} · time
                {{ format(sample.timeUs) }} µs · frame
                {{ format(sample.frameTimeUs) }} µs</small
              >
            </div>
            <label v-if="sample"
              >Point
              <select
                :value="pointId"
                aria-label="Measurement point"
                @change="
                  select({
                    point: ($event.target as HTMLSelectElement).value,
                    field: '',
                  })
                "
              >
                <option v-for="(p, id) in sample.points" :key="id" :value="id">
                  {{ id }} · {{ format(p.role) }}
                </option>
              </select></label
            >
            <div class="pager">
              <button
                :disabled="samplePosition <= 0"
                aria-label="Previous sample"
                @click="
                  select({
                    sample: testCase.sampleSummaries[samplePosition - 1]!.id,
                  })
                "
              >
                ← Previous</button
              ><button
                :disabled="
                  samplePosition < 0 ||
                  samplePosition >= testCase.sampleSummaries.length - 1
                "
                aria-label="Next sample"
                @click="
                  select({
                    sample: testCase.sampleSummaries[samplePosition + 1]!.id,
                  })
                "
              >
                Next →
              </button>
            </div>
          </div>
          <p v-if="sampleError" class="notice">{{ sampleError }}</p>
          <p v-if="sampleLoading" class="notice">Loading sample…</p>
          <p v-if="sample?.reason" class="notice">{{ sample.reason }}</p>
          <MeasurementTable
            v-if="sample"
            :point="point"
            :selected="fieldName"
            @select="select({ field: $event, tab: 'Details' })"
          />
          <p v-else-if="!sampleLoading" class="empty">No sample selected.</p>
        </section>
        <section class="panel inspector" aria-label="Observation inspector">
          <nav class="tabs" role="tablist" aria-label="Inspector">
            <button
              v-for="t in tabs"
              :key="t"
              role="tab"
              :aria-selected="tab === t"
              @click="select({ tab: t })"
            >
              {{ t }}
            </button>
          </nav>
          <div v-if="tab === 'Details'" id="panel-details" class="tab-panel">
            <div class="detail-layout">
              <nav class="field-tree" aria-label="Fields in current point">
                <template v-for="g in fieldGroups" :key="g.category"
                  ><div class="tree-group">{{ g.category }}</div>
                  <button
                    v-for="f in g.rows"
                    :key="f.name"
                    class="tree-button"
                    :aria-current="f.name === fieldName"
                    @click="select({ field: f.name })"
                  >
                    <span class="tree-name">{{ f.name }}</span
                    ><span
                      class="status"
                      :class="f.field.status"
                      :title="f.field.status"
                      >{{
                        f.field.status === 'equal'
                          ? '='
                          : f.field.status === 'different'
                            ? '≠'
                            : '—'
                      }}</span
                    >
                  </button></template
                >
              </nav>
              <div class="detail-content"><FieldInspector :row="row" /></div>
            </div>
          </div>
          <div v-else class="tab-panel section-content">
            <div v-if="tab === 'Descriptor'">
              <FieldInspector :row="row" mode="descriptor" />
              <details v-if="point">
                <summary>
                  Sample timing, point identity and comparison issues
                </summary>
                <pre>{{
                  format({
                    timeUs: sample?.timeUs,
                    frameTimeUs: sample?.frameTimeUs,
                    stage: sample?.stage,
                    role: point.role,
                    parent: point.parent,
                    status: point.status,
                    reason: point.reason,
                    issue: point.issue,
                  })
                }}</pre>
              </details>
            </div>
            <div v-else-if="tab === 'Trends'">
              <p v-if="historyLoading" class="muted">
                Loading field histories…
              </p>
              <p v-if="historyError" class="notice">{{ historyError }}</p>
              <FieldHistory
                :samples="allSamples"
                :point-id="pointId"
                :field-name="fieldName"
                @select="select({ sample: $event })"
              />
            </div>
            <RasterInspector
              v-else-if="tab === 'Raster' && sample"
              :run="run"
              :case-id="caseId"
              :sample="sample"
            />
            <div v-else-if="tab === 'Trace'">
              <p v-if="historyLoading">Loading execution trace…</p>
              <p v-if="historyError" class="error-message">
                {{ historyError }}
              </p>
              <div class="two-columns">
                <section v-for="s in ['compose', 'avalonia'] as const" :key="s">
                  <h3>{{ s }} · {{ traces[s]?.length }} operations</h3>
                  <div class="table-scroll">
                    <table>
                      <thead>
                        <tr>
                          <th># / time (µs)</th>
                          <th>Operation</th>
                          <th>Detail</th>
                        </tr>
                      </thead>
                      <tbody>
                        <tr v-for="t in traces[s]" :key="t.sequence">
                          <td>
                            {{ t.sequence }}<small>{{ t.timeUs }}</small>
                          </td>
                          <td>{{ t.operation }}</td>
                          <td>
                            {{ t.detail }}
                            <pre v-if="t.data">{{ format(t.data) }}</pre>
                          </td>
                        </tr>
                      </tbody>
                    </table>
                  </div>
                </section>
              </div>
            </div>
            <div v-else-if="tab === 'Environment'" class="table-scroll">
              <table>
                <thead>
                  <tr>
                    <th>Key</th>
                    <th>Compose</th>
                    <th>Avalonia</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="key in environmentKeys" :key="key">
                    <td>{{ key }}</td>
                    <td>
                      <pre>{{ environment?.compose?.[key] ?? 'absent' }}</pre>
                    </td>
                    <td>
                      <pre>{{ environment?.avalonia?.[key] ?? 'absent' }}</pre>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>

            <div v-else-if="tab === 'Raw'">
              <div class="raw-toolbar">
                <label
                  >Data
                  <select v-model="rawMode" aria-label="Raw data">
                    <option value="field">Selected field</option>
                    <option value="sample">Selected sample</option>
                    <option value="point">Selected point</option>
                    <option value="case">Case metadata and index</option>
                  </select></label
                >
              </div>
              <pre>{{ format(rawValue) }}</pre>
            </div>
            <div v-else-if="tab === 'Repeatability'">
              <p>Repeatability is separate from component parity.</p>
              <table v-if="testCase.repeatability">
                <thead>
                  <tr>
                    <th>Check</th>
                    <th>Status</th>
                    <th>Changes</th>
                  </tr>
                </thead>
                <tbody>
                  <tr
                    v-for="(changes, name) in testCase.repeatability.results"
                    :key="name"
                  >
                    <td>{{ name }}</td>
                    <td>
                      <StatusBadge
                        :status="changes.length ? 'unstable' : 'stable'"
                      />
                    </td>
                    <td>
                      <pre>{{ changes.join('\n') || 'No changes' }}</pre>
                    </td>
                  </tr>
                </tbody>
              </table>
              <p v-else>Not checked.</p>
            </div>
            <div v-else-if="tab === 'Config'">
              <h3>Actual configuration</h3>
              <pre>{{ format(testCase.metadata.config) }}</pre>
              <details>
                <summary>Capture identity and errors</summary>
                <pre>{{ format(testCase.captures) }}</pre>
              </details>
            </div>

            <div v-else-if="tab === 'Overview'">
              <div class="section-heading">
                <h2>Sample overview</h2>
                <small>Different / not compared by category</small>
              </div>
              <SampleTimeline
                :samples="testCase.sampleSummaries"
                :selected="sampleId"
                @select="select({ sample: $event })"
              />
            </div>
          </div>
        </section>
      </div>
    </main>
  </div>
</template>
