<script setup lang="ts">
import type { Summary } from '../data/schema'
import { categories } from '../data/schema'
import StatusBadge from './StatusBadge.vue'
defineProps<{ samples: Summary[]; selected: string }>()
defineEmits<{ select: [id: string] }>()
</script>
<template>
  <div class="table-scroll sample-overview">
    <table class="sample-table">
      <thead>
        <tr>
          <th>Sample · time (µs)</th>
          <th>Status</th>
          <th v-for="c in categories" :key="c">
            {{ c }}<small>different / not compared</small>
          </th>
          <th>
            Images<small
              >equal / within tolerance / different / not compared</small
            >
          </th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="s in samples"
          :key="s.id"
          :class="{ selected: s.id === selected }"
        >
          <td>
            <button class="text-button" @click="$emit('select', s.id)">
              {{ s.id }}</button
            ><small
              >{{
                s.expectedTimeUs === s.actualTimeUs
                  ? s.expectedTimeUs
                  : `${s.expectedTimeUs ?? 'absent'} / ${s.actualTimeUs ?? 'absent'}`
              }}
              · {{ s.stage }}</small
            >
          </td>
          <td><StatusBadge :status="s.status" /></td>
          <td v-for="c in categories" :key="c">
            {{ s.categories[c].different }} / {{ s.categories[c].notCompared }}
          </td>
          <td
            v-if="
              s.rasters.equal +
              s.rasters.withinTolerance +
              s.rasters.different +
              s.rasters.notCompared
            "
          >
            {{ s.rasters.equal }} / {{ s.rasters.withinTolerance }} /
            {{ s.rasters.different }} /
            {{ s.rasters.notCompared }}
          </td>
          <td v-else>
            <small v-for="side in ['compose', 'avalonia'] as const" :key="side"
              >{{ side }}:
              {{
                s.collection[side]
                  ?.map((c) => `${c.id}: ${c.status}`)
                  .join(', ') || 'unrecorded'
              }}</small
            >
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
