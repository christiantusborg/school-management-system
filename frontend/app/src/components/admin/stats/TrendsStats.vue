<template>
  <div>
    <div class="ss-toggles">
      <button type="button" :class="['ss-gran', { on: gran === 'month' }]" @click="gran = 'month'">Monthly</button>
      <button type="button" :class="['ss-gran', { on: gran === 'quarter' }]" @click="gran = 'quarter'">Quarterly</button>
    </div>
    <div v-if="error" class="err-banner">{{ error }}</div>
    <div v-if="loading" class="loading-row">Loading…</div>
    <template v-else-if="data">
      <p class="ss-sub">Enrolments count by commencement {{ data.granularity }}; average grade by graded date.</p>
      <table class="data-table">
        <thead><tr><th>Period</th><th>Enrolments</th><th style="width:30%"></th>
          <th>Grades saved</th><th>Avg grade</th><th style="width:22%"></th></tr></thead>
        <tbody>
          <tr v-for="r in data.series" :key="r.period">
            <td class="ss-mono">{{ r.period }}</td>
            <td><strong>{{ r.enrolments }}</strong></td>
            <td><div class="ss-bar" style="width:100%"><div class="ss-fill"
              :style="{ width: barPct(r.enrolments, maxEnrol) + '%' }"></div></div></td>
            <td>{{ r.gradedCount }}</td>
            <td><strong v-if="r.avgGrade != null" :class="heat(r.avgGrade)">{{ r.avgGrade }}</strong>
              <span v-else class="ss-muted">—</span></td>
            <td><div class="ss-bar" style="width:100%"><div class="ss-fill" style="background:#3e8e58"
              :style="{ width: (r.avgGrade ?? 0) + '%' }"></div></div></td>
          </tr>
        </tbody>
      </table>
      <p v-if="!data.series.length" class="ss-sub">Nothing in this period.</p>
    </template>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import api from '../../../api/client.js'

const props = defineProps({ from: { type: String, default: '' }, to: { type: String, default: '' } })
const data = ref(null)
const loading = ref(false)
const error = ref('')
const gran = ref('month')

const maxEnrol = computed(() => Math.max(1, ...(data.value?.series ?? []).map(r => r.enrolments)))
function barPct(v, max) { return max ? (v * 100) / max : 0 }
function heat(v) { return v >= 70 ? 'ss-good' : v >= 50 ? 'ss-mid' : 'ss-bad' }

async function load() {
  loading.value = true
  error.value = ''
  try {
    const params = { granularity: gran.value }
    if (props.from) params.from = props.from
    if (props.to) params.to = props.to
    data.value = (await api.get('/v1/admin/statistics/trends', { params })).data
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to load'
  } finally {
    loading.value = false
  }
}
watch(() => [props.from, props.to], load)
watch(gran, load)
onMounted(load)
</script>

<style scoped>
@import './statShared.css';
.ss-toggles { display: flex; gap: .4rem; margin-bottom: .6rem; }
.ss-gran { background: #f2f5f9; border: 1px solid #cfd7e3; border-radius: 5px; padding: .3rem .7rem; font-size: .78rem; font-weight: 600; color: #2c3e50; cursor: pointer; }
.ss-gran.on { background: #003366; border-color: #003366; color: #fff; }
</style>
