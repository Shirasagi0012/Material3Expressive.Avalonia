<script setup lang="ts">
import { computed } from 'vue'
import {
  fields,
  value,
  side,
  format,
  type Sample,
  type Side,
} from '../data/schema'
import StatusBadge from './StatusBadge.vue'
const props = defineProps<{
  samples: Sample[]
  pointId: string
  fieldName: string
}>()
defineEmits<{ select: [sample: string] }>()
const rows = computed(() =>
  props.samples.map((sample) => ({
    sample,
    row: fields(sample.points[props.pointId]).find(
      (r) => r.name === props.fieldName,
    ),
  })),
)
const plots = computed(() => {
  const series: { side: Side; component: number; path: string }[] = [],
    values: number[] = []
  const signatures = new Set<string>()
  for (const { row, sample } of rows.value)
    for (const s of ['compose', 'avalonia'] as const) {
      if (!row) continue
      const f = row.field,
        v = value(f, s)
      if (
        (typeof v === 'number' && Number.isFinite(v)) ||
        (Array.isArray(v) &&
          v.every((n) => typeof n === 'number' && Number.isFinite(n)))
      ) {
        values.push(...(typeof v === 'number' ? [v] : v))
        signatures.add(
          format([
            f.category ? side(f.category, s) : row.category,
            side(sample.points[props.pointId]!.role, s),
            side(sample.points[props.pointId]!.parent, s),
            Array.isArray(v) ? v.length : 1,
            side(f.kind, s),
            side(f.unit, s),
            side(f.space, s),
            side(f.definition, s),
            f.components ? side(f.components, s) : null,
          ]),
        )
      }
    }
  if (!values.length || signatures.size !== 1) return null
  const times = rows.value
    .flatMap((r) => [
      side(r.sample.timeUs, 'compose'),
      side(r.sample.timeUs, 'avalonia'),
    ])
    .filter((n): n is number => typeof n === 'number' && Number.isFinite(n))
  if (!times.length) return null
  const min = Math.min(...values),
    max = Math.max(...values),
    start = Math.min(...times),
    end = Math.max(...times)
  const x = (t: number) => 40 + (600 * (t - start)) / (end - start || 1),
    y = (v: number) => 110 - (90 * (v - min)) / (max - min || 1)
  const arity = rows.value.reduce(
    (n, r) =>
      Math.max(
        n,
        ...(['compose', 'avalonia'] as const).map((s) =>
          Array.isArray(r.row ? value(r.row.field, s) : undefined)
            ? (value(r.row!.field, s) as unknown[]).length
            : 1,
        ),
      ),
    1,
  )
  for (const s of ['compose', 'avalonia'] as const)
    for (let component = 0; component < arity; component++) {
      let path = '',
        connected = false
      for (const r of rows.value) {
        const v = r.row ? value(r.row.field, s) : undefined,
          n =
            typeof v === 'number'
              ? v
              : Array.isArray(v)
                ? v[component]
                : undefined,
          t = side(r.sample.timeUs, s)
        if (
          typeof n !== 'number' ||
          !Number.isFinite(n) ||
          typeof t !== 'number' ||
          !Number.isFinite(t)
        ) {
          connected = false
          continue
        }
        path += `${connected ? 'L' : 'M'}${x(t)},${y(n)} `
        connected = true
      }
      series.push({ side: s, component, path })
    }
  return { series, min, max, start, end }
})
</script>
<template>
  <h3>{{ pointId }}.{{ fieldName }}</h3>
  <p>Sampled values; missing or changed semantics are preserved.</p>
  <svg
    v-if="plots"
    viewBox="0 0 680 145"
    class="trend-chart"
    role="img"
    aria-label="Field history: Compose blue, Avalonia orange"
  >
    <path d="M40 20V110H640" stroke="#c4cad5" fill="none" />
    <path
      v-for="s in plots.series"
      :key="s.side + s.component"
      :d="s.path"
      fill="none"
      :stroke="s.side === 'compose' ? '#3761c8' : '#c86d2c'"
      stroke-width="2"
      :stroke-dasharray="s.component ? '4 2' : undefined"
    />
    <text x="40" y="135">{{ plots.start }} µs</text>
    <text x="545" y="135">{{ plots.end }} µs</text>
    <text x="45" y="18">{{ plots.min }} … {{ plots.max }}</text>
  </svg>
  <div class="table-scroll">
    <table>
      <thead>
        <tr>
          <th>Sample / time C / A</th>
          <th>Compose</th>
          <th>Avalonia</th>
          <th>Δ</th>
          <th>Status</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="r in rows" :key="r.sample.sample">
          <td>
            <button
              class="text-button"
              @click="$emit('select', r.sample.sample)"
            >
              {{ r.sample.sample }}</button
            ><small>{{ format(r.sample.timeUs) }}</small>
          </td>
          <td>
            <pre>{{
              r.row ? format(value(r.row.field, 'compose')) : 'absent'
            }}</pre>
          </td>
          <td>
            <pre>{{
              r.row ? format(value(r.row.field, 'avalonia')) : 'absent'
            }}</pre>
          </td>
          <td>
            <pre>{{ format(r.row?.field.delta) }}</pre>
          </td>
          <td>
            <StatusBadge :status="r.row?.field.status ?? 'notCompared'" />
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
