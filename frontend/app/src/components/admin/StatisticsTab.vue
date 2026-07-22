<template>
  <div>
    <div class="page-header">
      <div>
        <h1 class="st-title">Statistics</h1>
        <p class="st-sub">Outcomes for enrolments whose commencement date falls in the period —
          passed (grades approved), dropped out, deferred, still active. Drafts and rejected
          applications are excluded. Click a row to fold out its programmes and specializations.</p>
      </div>
      <div class="st-report-btns">
        <button type="button" class="btn-report" :disabled="reporting" @click="downloadReport('pdf')">
          {{ reporting === 'pdf' ? 'Preparing…' : '⤓ Full report (PDF)' }}</button>
        <button type="button" class="btn-report" :disabled="reporting" @click="downloadReport('xlsx')">
          {{ reporting === 'xlsx' ? 'Preparing…' : '⤓ Spreadsheet (Excel)' }}</button>
      </div>
    </div>

    <div class="st-filters">
      <label class="st-lbl">Start date</label>
      <input v-model="from" type="date" class="st-inp" @change="load" />
      <label class="st-lbl">End date</label>
      <input v-model="to" type="date" class="st-inp" @change="load" />
      <button type="button" class="btn-sm" @click="load">↻</button>
      <button type="button" class="btn-sm" :disabled="exporting" @click="exportFile('csv')">⤓ Export CSV</button>
      <button type="button" class="btn-sm" :disabled="exporting" @click="exportFile('pdf')">⤓ Export PDF</button>
      <span v-if="sub === 'outcomes' && data.overall" class="st-total">
        {{ data.overall.total }} enrolment{{ data.overall.total === 1 ? '' : 's' }} in period ·
        Passed {{ data.overall.passedPct }}% · Dropped {{ data.overall.droppedPct }}% ·
        Deferred {{ data.overall.deferredPct }}%</span>
    </div>

    <div v-if="dlError" class="err-banner">{{ dlError }}</div>
    <div class="st-subtabs">
      <button v-for="t in SUB_TABS" :key="t.key" type="button"
              :class="['st-subtab', { on: sub === t.key }]" @click="openSub(t.key)">{{ t.label }}</button>
    </div>

    <GradesStats v-if="visited.has('grades')" v-show="sub === 'grades'" :from="from" :to="to" />
    <TeacherStats v-if="visited.has('teachers')" v-show="sub === 'teachers'" :from="from" :to="to" />
    <DemographicsStats v-if="visited.has('demographics')" v-show="sub === 'demographics'" :from="from" :to="to" />
    <OperationsStats v-if="visited.has('operations')" v-show="sub === 'operations'" :from="from" :to="to" />
    <FinanceStats v-if="visited.has('finance')" v-show="sub === 'finance'" :from="from" :to="to" />
    <TrendsStats v-if="visited.has('trends')" v-show="sub === 'trends'" :from="from" :to="to" />

    <template v-if="sub === 'outcomes'">
    <div v-if="error" class="err-banner">{{ error }}</div>
    <div v-if="loading" class="loading-row">Loading…</div>

    <template v-else>
      <section v-for="sec in sections" :key="sec.key">
        <h2 class="st-section" :style="sec.key === 'school' ? 'margin-top:1.5rem' : ''">{{ sec.title }}</h2>
        <table v-if="sec.rows.length" class="data-table">
          <thead><tr>
            <th>{{ sec.head }}</th><th>Students</th><th>Passed</th><th>Dropped Out</th><th>Deferred</th><th>Still active</th>
          </tr></thead>
          <tbody>
            <template v-for="r in sec.rows" :key="r.bucket.label">
              <tr class="data-row st-top-row" @click="toggle(sec.key, r.bucket.label)">
                <td style="font-weight:700">
                  <span class="st-caret">{{ isOpen(sec.key, r.bucket.label) ? '▾' : '▸' }}</span>
                  {{ r.bucket.label }}
                </td>
                <td>{{ r.bucket.total }}</td>
                <td><strong>{{ r.bucket.passedPct }}%</strong> <span class="st-muted">({{ r.bucket.passed }})</span></td>
                <td><strong>{{ r.bucket.droppedPct }}%</strong> <span class="st-muted">({{ r.bucket.dropped }})</span></td>
                <td><strong>{{ r.bucket.deferredPct }}%</strong> <span class="st-muted">({{ r.bucket.deferred }})</span></td>
                <td><strong>{{ r.bucket.activePct }}%</strong> <span class="st-muted">({{ r.bucket.active }})</span></td>
              </tr>
              <template v-if="isOpen(sec.key, r.bucket.label)">
                <template v-for="pg in r.programmes" :key="r.bucket.label + pg.programme.label">
                  <tr class="data-row st-prog-row">
                    <td class="st-prog">{{ pg.programme.label }}</td>
                    <td>{{ pg.programme.total }}</td>
                    <td>{{ pg.programme.passedPct }}% <span class="st-muted">({{ pg.programme.passed }})</span></td>
                    <td>{{ pg.programme.droppedPct }}% <span class="st-muted">({{ pg.programme.dropped }})</span></td>
                    <td>{{ pg.programme.deferredPct }}% <span class="st-muted">({{ pg.programme.deferred }})</span></td>
                    <td>{{ pg.programme.activePct }}% <span class="st-muted">({{ pg.programme.active }})</span></td>
                  </tr>
                  <tr v-for="sp in pg.specializations" :key="r.bucket.label + pg.programme.label + sp.label" class="data-row">
                    <td class="st-spec">└ {{ sp.label }}</td>
                    <td>{{ sp.total }}</td>
                    <td>{{ sp.passedPct }}% <span class="st-muted">({{ sp.passed }})</span></td>
                    <td>{{ sp.droppedPct }}% <span class="st-muted">({{ sp.dropped }})</span></td>
                    <td>{{ sp.deferredPct }}% <span class="st-muted">({{ sp.deferred }})</span></td>
                    <td>{{ sp.activePct }}% <span class="st-muted">({{ sp.active }})</span></td>
                  </tr>
                </template>
              </template>
            </template>
            <tr v-if="sec.key === 'partner' && data.overall" class="st-total-row">
              <td>All partners</td>
              <td>{{ data.overall.total }}</td>
              <td><strong>{{ data.overall.passedPct }}%</strong> <span class="st-muted">({{ data.overall.passed }})</span></td>
              <td><strong>{{ data.overall.droppedPct }}%</strong> <span class="st-muted">({{ data.overall.dropped }})</span></td>
              <td><strong>{{ data.overall.deferredPct }}%</strong> <span class="st-muted">({{ data.overall.deferred }})</span></td>
              <td><strong>{{ data.overall.activePct }}%</strong> <span class="st-muted">({{ data.overall.active }})</span></td>
            </tr>
          </tbody>
        </table>
        <p v-else class="st-sub">No enrolments commenced in this period.</p>
      </section>
    </template>
    </template>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import api from '../../api/client.js'
import GradesStats from './stats/GradesStats.vue'
import TeacherStats from './stats/TeacherStats.vue'
import DemographicsStats from './stats/DemographicsStats.vue'
import OperationsStats from './stats/OperationsStats.vue'
import FinanceStats from './stats/FinanceStats.vue'
import TrendsStats from './stats/TrendsStats.vue'

const SUB_TABS = [
  { key: 'outcomes', label: 'Outcomes' },
  { key: 'grades', label: 'Grades' },
  { key: 'teachers', label: 'Teachers' },
  { key: 'demographics', label: 'Demographics' },
  { key: 'operations', label: 'Operations & QA' },
  { key: 'finance', label: 'Finance' },
  { key: 'trends', label: 'Trends' },
]
const sub = ref('outcomes')
const visited = reactive(new Set())
function openSub(key) {
  sub.value = key
  if (key !== 'outcomes') visited.add(key)
}

const from = ref('')
const to = ref('')
const data = ref({ byPartner: [], bySchool: [], overall: null })
const loading = ref(false)
const error = ref('')
const exporting = ref(false)
const reporting = ref('')
const dlError = ref('')

// Firefox is picky about programmatic downloads: the anchor must be in the
// DOM, and revoking the object URL too early kills the save dialog.
function saveBlob(blob, filename) {
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  a.style.display = 'none'
  document.body.appendChild(a)
  a.click()
  setTimeout(() => { document.body.removeChild(a); URL.revokeObjectURL(url) }, 120_000)
}

async function blobError(e) {
  // Axios wraps error bodies in a Blob when responseType is blob — decode it.
  try {
    if (e.response?.data instanceof Blob) {
      const text = await e.response.data.text()
      try { return JSON.parse(text).error ?? text.slice(0, 200) } catch { return text.slice(0, 200) || e.message }
    }
  } catch { /* fall through */ }
  return e.response?.data?.error ?? e.message ?? 'Download failed'
}

// Full report: every tab in one document — PDF chapters or an XLSX workbook
// (Open XML: Excel, LibreOffice and Google Sheets all read it) with one
// worksheet per tab.
async function downloadReport(format) {
  if (reporting.value) return
  reporting.value = format
  dlError.value = ''
  try {
    const params = { format }
    if (from.value) params.from = from.value
    if (to.value) params.to = to.value
    const res = await api.get('/v1/admin/statistics/full-report', { params, responseType: 'blob' })
    saveBlob(res.data, format === 'xlsx' ? 'mgw-statistics.xlsx' : 'mgw-statistics-report.pdf')
  } catch (e) {
    dlError.value = 'Full report failed: ' + await blobError(e)
  } finally {
    reporting.value = ''
  }
}

async function exportFile(format) {
  if (exporting.value) return
  exporting.value = true
  dlError.value = ''
  try {
    const params = { format }
    if (from.value) params.from = from.value
    if (to.value) params.to = to.value
    const res = await api.get(`/v1/admin/statistics/${sub.value}/export`, { params, responseType: 'blob' })
    saveBlob(res.data, `statistics-${sub.value}.${format}`)
  } catch (e) {
    dlError.value = 'Export failed: ' + await blobError(e)
  } finally {
    exporting.value = false
  }
}
// Collapsed by default; keyed "partner|<label>" / "school|<label>".
const open = reactive({})

const sections = computed(() => [
  { key: 'partner', title: 'Per partner', head: 'Partner / Programme / Specialization', rows: data.value.byPartner ?? [] },
  { key: 'school', title: 'Per school', head: 'School / Programme / Specialization', rows: data.value.bySchool ?? [] },
])

function isOpen(sec, label) { return !!open[`${sec}|${label}`] }
function toggle(sec, label) { open[`${sec}|${label}`] = !open[`${sec}|${label}`] }

async function load() {
  loading.value = true
  error.value = ''
  try {
    const params = {}
    if (from.value) params.from = from.value
    if (to.value) params.to = to.value
    const res = await api.get('/v1/admin/statistics/outcomes', { params })
    data.value = res.data
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to load statistics'
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

<style scoped>
.st-title { margin: 0; font-size: 1.4rem; color: #003366; }
.page-header { display: flex; justify-content: space-between; align-items: flex-start; gap: 1rem; }
.st-report-btns { display: flex; gap: .5rem; flex-shrink: 0; }
.btn-report { background: #003366; border: 1px solid #003366; color: #fff; border-radius: 6px; padding: .45rem .9rem; font-size: .82rem; font-weight: 600; cursor: pointer; white-space: nowrap; }
.btn-report:disabled { opacity: .6; cursor: default; }
.st-subtabs { display: flex; gap: .15rem; border-bottom: 2px solid #e2e8f1; margin: .2rem 0 1rem; flex-wrap: wrap; }
.st-subtab { background: none; border: none; padding: .5rem .9rem; font-size: .86rem; font-weight: 600; color: #6b7888; cursor: pointer; border-bottom: 3px solid transparent; margin-bottom: -2px; }
.st-subtab.on { color: #003366; border-bottom-color: #003366; }
.st-subtab:hover:not(.on) { color: #333; }
.st-sub { font-size: .8rem; color: #6b7888; margin: .25rem 0 .75rem; }
.st-filters { display: flex; align-items: center; gap: .5rem; flex-wrap: wrap; margin-bottom: .9rem; }
.st-lbl { font-size: .78rem; color: #44536a; font-weight: 600; }
.st-inp { padding: .4rem .55rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .85rem; background: #fff; }
.st-total { font-size: .8rem; color: #1a2d4f; font-weight: 600; margin-left: .5rem; }
.st-section { font-size: 1rem; color: #003366; margin: .4rem 0 .5rem; }
.st-muted { color: #8a97a8; font-size: .78rem; }
.st-caret { color: #8a97a8; margin-right: .3rem; }
.st-top-row { cursor: pointer; }
.st-top-row:hover td { background: #f0f4fa; }
.st-prog-row td { background: #f6f9fd; }
.st-prog { padding-left: 1.8rem !important; font-weight: 600; color: #1a2d4f; }
.st-spec { padding-left: 3.2rem !important; color: #44536a; }
.st-total-row td { font-weight: 700; border-top: 2px solid #d5deea; background: #fafbfd; }
.data-table { width: 100%; border-collapse: collapse; font-size: .85rem; background: #fff; border-radius: 8px; overflow: hidden; box-shadow: 0 1px 3px rgba(0,0,0,.06); }
.data-table th { text-align: left; padding: .5rem .7rem; color: #6b7888; font-size: .74rem; text-transform: uppercase; letter-spacing: .03em; border-bottom: 1.5px solid #e8edf4; background: #fafbfd; }
.data-table td { padding: .5rem .7rem; border-bottom: 1px solid #eef1f5; }
.btn-sm { background: #f2f5f9; border: 1px solid #cfd7e3; border-radius: 5px; padding: .3rem .6rem; font-size: .78rem; font-weight: 600; color: #2c3e50; cursor: pointer; }
.err-banner { background: #fdf3f2; border: 1px solid #e2b8b5; color: #b3261e; padding: .45rem .7rem; border-radius: 6px; font-size: .8rem; margin-bottom: .5rem; }
.loading-row { color: #6b7888; font-size: .85rem; padding: .5rem 0; }
</style>
