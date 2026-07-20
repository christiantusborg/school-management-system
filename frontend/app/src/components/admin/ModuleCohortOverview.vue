<template>
  <div>
    <div class="page-header">
      <div>
        <h1 class="mco-title">Module Cohorts</h1>
        <p class="mco-sub">All cohorts across partners, with the QA reports: filter by module start-date range and
          switch to a report view to list cohorts missing their QA check dates.</p>
      </div>
    </div>

    <div class="mco-filters">
      <select v-model="partnerId" class="mco-inp" style="min-width:220px" @change="load">
        <option value="">All partners</option>
        <option v-for="p in partners" :key="p.partnerId" :value="p.partnerId">{{ p.name }}</option>
      </select>
      <label class="mco-lbl-inline">Start from</label>
      <input v-model="from" type="date" class="mco-inp" @change="load" />
      <label class="mco-lbl-inline">to</label>
      <input v-model="to" type="date" class="mco-inp" @change="load" />
      <select v-model="report" class="mco-inp" style="min-width:260px" @change="load">
        <option value="">All cohorts</option>
        <option value="missing-doc-qa">Report: missing Document QA date</option>
        <option value="missing-grade-qa">Report: missing Grade-Sheet QA date</option>
      </select>
      <button type="button" class="btn-sm" @click="load">↻</button>
    </div>

    <div v-if="error" class="err-banner">{{ error }}</div>
    <div v-if="loading" class="loading-row">Loading…</div>

    <table v-else-if="items.length" class="data-table">
      <thead><tr>
        <th>Cohort #</th><th>Partner</th><th>Module</th><th>Teacher</th><th>Start → End</th>
        <th>Students</th><th>Doc QA</th><th>Grade QA</th><th>Grading sheet</th>
      </tr></thead>
      <tbody>
        <tr v-for="c in items" :key="c.moduleCohortId" class="data-row">
          <td style="font-weight:600">{{ c.cohortNumber }}</td>
          <td>{{ c.partnerName }}</td>
          <td>{{ c.moduleCode }} · {{ c.moduleName }}</td>
          <td>{{ c.teacherName || '—' }}</td>
          <td>{{ fmtDate(c.startDate) }} → {{ fmtDate(c.endDate) }}</td>
          <td>{{ c.studentCount }}</td>
          <td>{{ c.docQaDate ? fmtDate(c.docQaDate) : '—' }}</td>
          <td>{{ c.gradeQaDate ? fmtDate(c.gradeQaDate) : '—' }}</td>
          <td>
            <span v-if="c.gradingSheetUploadedDate">✓ {{ fmtDate(c.gradingSheetUploadedDate) }}</span>
            <span v-else-if="c.gradingSheetDueDate && new Date(c.gradingSheetDueDate) < new Date()" class="mco-overdue">
              overdue (due {{ fmtDate(c.gradingSheetDueDate) }})</span>
            <span v-else>due {{ fmtDate(c.gradingSheetDueDate) }}</span>
          </td>
        </tr>
      </tbody>
    </table>
    <p v-else-if="!loading" class="mco-sub">No cohorts match.</p>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '../../api/client.js'

const items = ref([])
const partners = ref([])
const partnerId = ref('')
const from = ref('')
const to = ref('')
const report = ref('')
const loading = ref(false)
const error = ref('')

function fmtDate(d) {
  return d ? new Date(d).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }) : '—'
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    const params = {}
    if (partnerId.value) params.partnerId = partnerId.value
    if (from.value) params.from = from.value
    if (to.value) params.to = to.value
    if (report.value) params.report = report.value
    const res = await api.get('/v1/admin/cohorts-overview', { params })
    items.value = res.data.items ?? []
    if (!partners.value.length) {
      const m = new Map()
      for (const c of items.value) if (!m.has(c.partnerId)) m.set(c.partnerId, { partnerId: c.partnerId, name: c.partnerName })
      partners.value = [...m.values()].sort((a, b) => (a.name || '').localeCompare(b.name || ''))
    }
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to load cohorts'
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

<style scoped>
.mco-title { margin: 0; font-size: 1.4rem; color: #003366; }
.mco-sub { font-size: .8rem; color: #6b7888; margin: .25rem 0 .75rem; }
.mco-filters { display: flex; align-items: center; gap: .5rem; flex-wrap: wrap; margin-bottom: .75rem; }
.mco-inp { padding: .4rem .55rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .85rem; background: #fff; }
.mco-lbl-inline { font-size: .78rem; color: #44536a; font-weight: 600; }
.mco-overdue { color: #a8241e; font-weight: 700; }
.data-table { width: 100%; border-collapse: collapse; font-size: .85rem; background: #fff; border-radius: 8px; overflow: hidden; box-shadow: 0 1px 3px rgba(0,0,0,.06); }
.data-table th { text-align: left; padding: .5rem .6rem; color: #6b7888; font-size: .73rem; text-transform: uppercase; letter-spacing: .03em; border-bottom: 1.5px solid #e8edf4; background: #fafbfd; }
.data-table td { padding: .45rem .6rem; border-bottom: 1px solid #eef1f5; }
.btn-sm { background: #f2f5f9; border: 1px solid #cfd7e3; border-radius: 5px; padding: .3rem .6rem; font-size: .78rem; font-weight: 600; color: #2c3e50; cursor: pointer; }
.err-banner { background: #fdf3f2; border: 1px solid #e2b8b5; color: #b3261e; padding: .45rem .7rem; border-radius: 6px; font-size: .8rem; margin-bottom: .5rem; }
.loading-row { color: #6b7888; font-size: .85rem; padding: .5rem 0; }
</style>
