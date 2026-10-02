<script setup lang="ts">
import { value, side, format, type FieldRow, type Side } from '../data/schema'
import StatusBadge from './StatusBadge.vue'
const props = defineProps<{ row?: FieldRow; mode?: 'details' | 'descriptor' }>()
const compact = (v: unknown) => format(v).replace(/\s*\n\s*/g, ' ')
const descriptors = [
  'kind',
  'unit',
  'space',
  'definition',
  'source',
  'components',
  'category',
] as const
function components(s: Side) {
  if (!props.row) return []
  const v = value(props.row.field, s),
    names = props.row.field.components
      ? side(props.row.field.components, s)
      : []
  return names?.length && (typeof v === 'number' || Array.isArray(v))
    ? names.map((name, i) => ({
        name,
        value: typeof v === 'number' ? v : v[i],
      }))
    : []
}
</script>
<template>
  <p v-if="!row" class="empty">
    Select a measurement field to inspect its exact values.
  </p>
  <div v-else>
    <div class="detail-title">
      <h2>{{ row.category }}.{{ row.name }}</h2>
      <StatusBadge :status="row.field.status" /><span class="chip">{{
        format(row.field.kind)
      }}</span
      ><span class="chip"
        >{{ format(row.field.unit) }} / {{ format(row.field.space) }}</span
      >
    </div>
    <div v-if="mode === 'descriptor'" class="two-columns">
      <section
        v-for="s in ['compose', 'avalonia'] as const"
        :key="s"
        class="data-card"
      >
        <h3>{{ s === 'compose' ? 'Compose' : 'Avalonia' }}</h3>
        <table>
          <thead>
            <tr>
              <th>Descriptor</th>
              <th>Value</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="key in descriptors" :key="key">
              <td class="key">{{ key }}</td>
              <td>
                <pre>{{
                  format(
                    row.field[key] === undefined
                      ? undefined
                      : side(row.field[key], s),
                  )
                }}</pre>
              </td>
            </tr>
          </tbody>
        </table>
      </section>
    </div>
    <div v-else class="fact-grid">
      <section
        v-for="s in ['compose', 'avalonia'] as const"
        :key="s"
        class="fact"
      >
        <div class="fact-label">
          <span class="side-dot" :class="{ actual: s === 'avalonia' }"></span
          >{{ s === 'compose' ? 'Compose' : 'Avalonia' }}
        </div>
        <pre class="fact-value">{{ compact(value(row.field, s)) }}</pre>
        <table v-if="components(s).length">
          <thead>
            <tr>
              <th>Component</th>
              <th>Value</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="c in components(s)" :key="c.name">
              <td>{{ c.name }}</td>
              <td class="mono">{{ format(c.value) }}</td>
            </tr>
          </tbody>
        </table>
        <div class="fact-footer">{{ side(row.field.source, s) }}</div>
      </section>
      <section class="fact">
        <div class="fact-label">Δ (Avalonia − Compose)</div>
        <pre class="fact-value">{{
          row.field.delta === undefined ? '—' : compact(row.field.delta)
        }}</pre>
        <table v-if="Array.isArray(row.field.delta)">
          <thead>
            <tr>
              <th>Component</th>
              <th>Δ</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(n, i) in row.field.delta" :key="i">
              <td>
                {{
                  row.field.components
                    ? (side(row.field.components, 'compose')?.[i] ?? i)
                    : i
                }}
              </td>
              <td class="mono" :class="{ 'component-changed': n !== 0 }">
                {{ format(n) }}
              </td>
            </tr>
          </tbody>
        </table>
        <div class="fact-footer">
          {{
            row.field.status === 'notCompared'
              ? 'Values were not compared'
              : 'Signed component differences'
          }}
        </div>
      </section>
    </div>
    <p v-if="row.field.reason" class="notice">{{ row.field.reason }}</p>
    <details v-if="mode === 'descriptor'">
      <summary>Full descriptor and values</summary>
      <pre>{{ format(row.field) }}</pre>
    </details>
  </div>
</template>
