<template>
  <div>
    <div v-if="error" class="err-banner">{{ error }}</div>
    <div v-if="loading" class="loading-row">Loading…</div>
    <template v-else-if="data">
      <p class="ss-sub">Cohorts filtered by start date. Grading-sheet legend:
        <span class="ss-leg"><i style="background:#3e8e58"></i>on time</span>
        <span class="ss-leg"><i style="background:#e0a24a"></i>late</span>
        <span class="ss-leg"><i style="background:#c4554b"></i>missing (past due)</span>
        <span class="ss-leg"><i style="background:#9aa4b5"></i>not due yet</span></p>
      <p class="ss-total">
        Cohort end → grading sheet uploaded:
        <strong>{{ data.uploadLeadAvgDays ?? '—' }}</strong> days avg <span class="ss-muted">({{ data.uploadLeadCount }} sheets)</span>
        &nbsp;·&nbsp; Partner submit → Admission approval:
        <strong>{{ data.approvalLeadAvgDays ?? '—' }}</strong> days avg <span class="ss-muted">({{ data.approvalLeadCount }} approvals)</span>
      </p>

      <table class="data-table">
        <thead><tr><th>Partner</th><th>Cohorts</th><th style="width:220px">Grading sheets</th>
          <th>On time / late / missing</th><th>Doc QA</th><th>Grade QA</th><th>No teacher</th></tr></thead>
        <tbody>
          <tr v-for="p in data.perPartner" :key="p.partner">
            <td style="font-weight:600">{{ p.partner }}</td>
            <td>{{ p.cohorts }}</td>
            <td><div class="ss-bands">
              <span :style="segStyle(p, p.onTime, '#3e8e58')"></span>
              <span :style="segStyle(p, p.late, '#e0a24a')"></span>
              <span :style="segStyle(p, p.missing, '#c4554b')"></span>
              <span :style="segStyle(p, p.notDueYet, '#9aa4b5')"></span>
            </div></td>
            <td>{{ p.onTime }} / {{ p.late }} / <strong :class="p.missing ? 'ss-bad' : ''">{{ p.missing }}</strong></td>
            <td><strong :class="heat(p.docQaPct)">{{ p.docQaPct }}%</strong></td>
            <td><strong :class="heat(p.gradeQaPct)">{{ p.gradeQaPct }}%</strong></td>
            <td><strong :class="p.withoutTeacher ? 'ss-bad' : ''">{{ p.withoutTeacher }}</strong></td>
          </tr>
        </tbody>
      </table>
      <p v-if="!data.perPartner.length" class="ss-sub">No cohorts in this period.</p>

      <h3 class="ss-section">Stalled students
        <span class="ss-muted">(active, commenced {{ data.stalledMonths }}+ months ago, not a single grade — evaluated against today)</span></h3>
      <table v-if="data.stalled.length" class="data-table">
        <thead><tr><th>Partner</th><th>Student #</th><th>Name</th><th>Programme</th><th>Commencement</th><th>Months</th></tr></thead>
        <tbody>
          <tr v-for="s in data.stalled" :key="s.studentNumber + s.programme">
            <td>{{ s.partner }}</td>
            <td class="ss-mono">{{ s.studentNumber }}</td>
            <td>{{ s.name }}</td>
            <td>{{ s.programme }}</td>
            <td>{{ (s.commencement ?? '').slice(0, 10) }}</td>
            <td><strong class="ss-bad">{{ monthsSince(s.commencement) }}</strong></td>
          </tr>
        </tbody>
      </table>
      <p v-else class="ss-sub">None — every long-running enrolment has at least one grade. 🎉</p>
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

function segStyle(p, n, color) {
  return { width: (p.cohorts ? (n * 100) / p.cohorts : 0) + '%', background: color }
}
function heat(v) { return v >= 80 ? 'ss-good' : v >= 50 ? 'ss-mid' : 'ss-bad' }
function monthsSince(d) {
  if (!d) return '—'
  return Math.floor((Date.now() - new Date(d).getTime()) / (30.44 * 24 * 3600 * 1000))
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    const params = {}
    if (props.from) params.from = props.from
    if (props.to) params.to = props.to
    data.value = (await api.get('/v1/admin/statistics/operations', { params })).data
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
