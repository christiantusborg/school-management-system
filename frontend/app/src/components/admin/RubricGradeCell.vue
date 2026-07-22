<template>
  <span class="rgc">
    <strong :class="['rgc-val', { 'rgc-empty': displayScore == null }]">{{ displayScore ?? '—' }}</strong>
    <button type="button" class="rgc-btn" @click="openPanel = !openPanel" title="Rubric breakdown">
      ▦ {{ openPanel ? '▾' : '▸' }}</button>

    <div v-if="openPanel" class="rgc-panel">
      <div class="rgc-name">{{ row.rubric.name }}</div>
      <div class="rgc-grid rgc-head"><span>Section</span><span>Max %</span><span>Score</span><span>Wtd</span></div>
      <div v-for="r in row.rubric.rows" :key="r.id" class="rgc-grid" :title="r.criteria">
        <span class="rgc-sec">{{ r.section }}</span>
        <span>{{ r.maxPercent }}%</span>
        <span>
          <input v-if="editable" type="number" min="0" max="100" v-model.number="row.rubricScores[r.id]" class="rgc-inp" />
          <template v-else>{{ row.rubricScores[r.id] ?? '—' }}</template>
        </span>
        <span>{{ weighted(r) }}</span>
      </div>
      <div class="rgc-grid rgc-total">
        <span>Final grade</span><span>100%</span><span></span><span>{{ total ?? '—' }}</span>
      </div>
      <p v-if="editable && total == null" class="rgc-hint">Score every criterion 1–100 — the grade is always calculated.</p>
    </div>
  </span>
</template>

<script setup>
import { ref, computed, watch } from 'vue'

const props = defineProps({
  // Subject row with .rubric { name, rows: [{id, section, criteria,
  // maxPercent, score}] }, .rubricScores {rowId: score} and .score. The
  // computed total is written back to row.score so ECTS counters, letter
  // grades and save payloads keep working unchanged.
  row: { type: Object, required: true },
  editable: { type: Boolean, default: false },
})

const openPanel = ref(false)

function has(rowId) {
  const v = props.row.rubricScores?.[rowId]
  return v !== null && v !== undefined && v !== ''
}
function weighted(r) {
  return has(r.id) ? ((props.row.rubricScores[r.id] * r.maxPercent) / 100).toFixed(1) : '—'
}
const total = computed(() => {
  let sum = 0
  for (const r of props.row.rubric.rows) {
    if (!has(r.id)) return null
    sum += props.row.rubricScores[r.id] * r.maxPercent
  }
  return Math.round(sum / 100)
})
const displayScore = computed(() => total.value ?? props.row.score ?? null)

watch(total, v => {
  if (props.editable) props.row.score = v ?? null
})
</script>

<style scoped>
.rgc { position: relative; display: inline-flex; align-items: center; gap: .35rem; }
.rgc-val { font-size: .9rem; color: #1a2d4f; }
.rgc-empty { color: #8a97a8; font-weight: 400; }
.rgc-btn { background: #f2f5f9; border: 1px solid #cfd7e3; border-radius: 5px; padding: .15rem .45rem; font-size: .72rem; font-weight: 600; color: #44536a; cursor: pointer; white-space: nowrap; }
.rgc-panel { position: absolute; top: calc(100% + 4px); right: 0; z-index: 70; background: #fff; border: 1px solid #d5deea; border-radius: 8px; box-shadow: 0 8px 26px rgba(0,0,0,.18); padding: .6rem .75rem; width: 380px; }
.rgc-name { font-size: .78rem; font-weight: 700; color: #003366; margin-bottom: .3rem; }
.rgc-grid { display: grid; grid-template-columns: 1.6fr 55px 78px 48px; gap: .4rem; align-items: center; padding: .18rem 0; font-size: .78rem; }
.rgc-head { text-transform: uppercase; font-size: .64rem; letter-spacing: .03em; color: #6b7888; font-weight: 700; border-bottom: 1px solid #e2e8f1; }
.rgc-sec { font-weight: 600; color: #2c3e50; }
.rgc-total { border-top: 1.5px solid #d5deea; font-weight: 700; margin-top: .15rem; }
.rgc-inp { width: 64px; padding: .2rem .35rem; border: 1px solid #cfd7e3; border-radius: 4px; font-size: .78rem; }
.rgc-hint { font-size: .7rem; color: #8a97a8; margin: .3rem 0 0; }
</style>
