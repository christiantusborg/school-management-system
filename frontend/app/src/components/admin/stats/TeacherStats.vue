<template>
  <div>
    <div v-if="error" class="err-banner">{{ error }}</div>
    <div v-if="loading" class="loading-row">Loading…</div>
    <template v-else-if="data">
      <p class="ss-sub">Marks given by each cohort teacher, benchmarked as the <strong>deviation from the
        module's global average</strong>: +5 means grading 5 points above everyone else on the SAME modules,
        so teaching hard modules doesn't make a teacher look strict. Teachers with fewer than 5 grades carry
        a small-sample flag — read those with care.</p>
      <table class="data-table">
        <thead><tr><th>Teacher</th><th>Partner</th><th>Cohorts</th><th>Students</th><th>Graded</th>
          <th>Average</th><th>vs module avg</th><th></th></tr></thead>
        <tbody>
          <tr v-for="t in data.teachers" :key="t.teacher + t.partner">
            <td style="font-weight:600">{{ t.teacher }}</td>
            <td>{{ t.partner }}</td>
            <td>{{ t.cohorts }}</td>
            <td>{{ t.students }}</td>
            <td>{{ t.graded }}</td>
            <td><strong v-if="t.avg != null">{{ t.avg }}</strong><span v-else class="ss-muted">—</span></td>
            <td>
              <template v-if="t.deviation != null">
                <strong :class="devClass(t.deviation)">{{ t.deviation > 0 ? '+' : '' }}{{ t.deviation }}</strong>
                <span class="ss-muted"> {{ devLabel(t.deviation) }}</span>
              </template>
              <span v-else class="ss-muted">—</span>
            </td>
            <td><span v-if="t.smallSample && t.graded" class="ss-chip ss-chip-warn">small sample</span></td>
          </tr>
        </tbody>
      </table>
      <p v-if="!data.teachers.length" class="ss-sub">No cohorts with an assigned teacher yet.</p>
    </template>
  </div>
</template>

<script setup>
import { ref, watch, onMounted } from 'vue'
import api from '../../../api/client.js'

const props = defineProps({ from: { type: String, default: '' }, to: { type: String, default: '' } })
const data = ref(null)
const loading = ref(false)
const error = ref('')

function devClass(d) { return d >= 3 ? 'ss-mid' : d <= -3 ? 'ss-bad' : 'ss-good' }
function devLabel(d) { return d >= 3 ? '↑ grades high' : d <= -3 ? '↓ grades strict' : '≈ in line' }

async function load() {
  loading.value = true
  error.value = ''
  try {
    const params = {}
    if (props.from) params.from = props.from
    if (props.to) params.to = props.to
    data.value = (await api.get('/v1/admin/statistics/teachers', { params })).data
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to load'
  } finally {
    loading.value = false
  }
}
watch(() => [props.from, props.to], load)
onMounted(load)
</script>

<style scoped>
@import './statShared.css';
</style>
