<script setup lang="ts">
import { computed, ref } from 'vue'
import {
  fields,
  value,
  side,
  format,
  categories,
  type Point,
} from '../data/schema'
const props = defineProps<{ point?: Point; selected: string }>()
defineEmits<{ select: [name: string] }>()
const category = ref(''),
  query = ref(''),
  equal = ref(true)
const compact = (v: unknown) => format(v).replace(/\s*\n\s*/g, ' ')
const collapsed = ref(new Set<string>())
function toggle(category: string) {
  const next = new Set(collapsed.value)
  if (next.has(category)) next.delete(category)
  else next.add(category)
  collapsed.value = next
}
const visible = computed(() =>
  fields(props.point).filter(
    (r) =>
      (!category.value || r.category === category.value) &&
      r.name.toLowerCase().includes(query.value.toLowerCase()) &&
      (equal.value || r.field.status !== 'equal'),
  ),
)
const groups = computed(() =>
  categories
    .map((category) => ({
      category,
      rows: visible.value.filter((r) => r.category === category),
    }))
    .filter((g) => g.rows.length),
)
</script>
<template>
  <div class="measurement-body">
    <div class="toolbar secondary">
      <label
        >Dimension
        <select v-model="category" aria-label="Measurement category">
          <option value="">All categories</option>
          <option v-for="c in categories" :key="c">{{ c }}</option>
        </select></label
      ><input
        type="search"
        v-model="query"
        placeholder="Filter measurements…"
        aria-label="Filter measurements"
      /><label><input v-model="equal" type="checkbox" />Show equal</label>
    </div>
    <div class="table-scroll">
      <table class="field-table">
        <thead>
          <tr>
            <th>Field</th>
            <th>Compose</th>
            <th>Avalonia</th>
            <th>Δ (A − C)</th>
            <th>Status</th>
            <th>Unit / space</th>
          </tr>
        </thead>
        <tbody>
          <template v-for="g in groups" :key="g.category"
            ><tr class="group">
              <td colspan="6">
                <button
                  class="group-button"
                  :aria-expanded="!collapsed.has(g.category)"
                  @click="toggle(g.category)"
                >
                  {{ collapsed.has(g.category) ? '▸' : '▾' }} {{ g.category
                  }}<span>{{ g.rows.length }} fields</span>
                </button>
              </td>
            </tr>
            <template v-if="!collapsed.has(g.category)"
              ><tr
                v-for="r in g.rows"
                :key="r.name"
                :class="[r.field.status, { selected: r.name === selected }]"
              >
                <td>
                  <button class="field-button" @click="$emit('select', r.name)">
                    {{ r.name }}
                  </button>
                </td>
                <td class="value-cell">
                  <pre :title="format(value(r.field, 'compose'))">{{
                    compact(value(r.field, 'compose'))
                  }}</pre>
                </td>
                <td class="value-cell">
                  <pre :title="format(value(r.field, 'avalonia'))">{{
                    compact(value(r.field, 'avalonia'))
                  }}</pre>
                </td>
                <td class="value-cell">
                  <pre :title="format(r.field.delta)">{{
                    r.field.delta === undefined ? '—' : compact(r.field.delta)
                  }}</pre>
                </td>
                <td>
                  <span
                    class="status"
                    :title="r.field.status"
                    :aria-label="r.field.status"
                    >{{
                      r.field.status === 'equal'
                        ? '='
                        : r.field.status === 'different'
                          ? '≠'
                          : '—'
                    }}</span
                  >
                </td>
                <td>
                  <span
                    class="unit"
                    v-for="s in ['compose', 'avalonia'] as const"
                    :key="s"
                    >{{ s === 'compose' ? 'C' : 'A' }}:
                    {{ side(r.field.unit, s) }} /
                    {{ side(r.field.space, s) }}</span
                  >
                </td>
              </tr></template
            ></template
          >
        </tbody>
      </table>
    </div>
    <p v-if="!visible.length" class="empty">No matching measurements.</p>
  </div>
</template>
