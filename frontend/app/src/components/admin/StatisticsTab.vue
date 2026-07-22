<template>
  <div>
    <div class="page-header">
      <div>
        <h1 class="st-title">Statistics</h1>
        <p class="st-sub">Outcomes for enrolments whose commencement date falls in the period —
          passed (grades approved), dropped out, deferred, still active. Drafts and rejected
          applications are excluded.</p>
      </div>
    </div>

    <div class="st-filters">
      <label class="st-lbl">Start date</label>
      <input v-model="from" type="date" class="st-inp" @change="load" />
      <label class="st-lbl">End date</label>
      <input v-model="to" type="date" class="st-inp" @change="load" />
      <button type="button" class="btn-sm" @click="load">↻</button>
      <span v-if="data.overall" class="st-total">
        {{ data.overall.total }} enrolment{{ data.overall.total === 1 ? '' : 's' }} in period ·
        Passed {{ data.overall.passedPct }}% · Dropped {{ data.overall.droppedPct }}% ·
        Deferred {{ data.overall.deferredPct }}%</span>
    </div>

    <div v-if="error" class="err-banner">{{ error }}</div>
    <div v-if="loading" class="loading-row">Loading…</div>

    <template v-else>
      <h2 class="st-section">Per partner</h2>
      <table v-if="data.byPartner?.length" class="data-table">
        <thead><tr>
          <th>Partner</th><th>Students</th><th>Passed</th><th>Dropped Out</th><th>Deferred</th><th>Still active</th>
        </tr></thead>
        <tbody>
          <tr v-for="r in data.byPartner" :key="r.label" class="data-row">
            <td style="font-weight:600">{{ r.label }}</td>
            <td>{{ r.total }}</td>
            <td><strong>{{ r.passedPct }}%</strong> <span class="st-muted">({{ r.passed }})</span></td>
            <td><strong>{{ r.droppedPct }}%</strong> <span class="st-muted">({{ r.dropped }})</span></td>
            <td><strong>{{ r.deferredPct }}%</strong> <span class="st-muted">({{ r.deferred }})</span></td>
            <td><strong>{{ r.activePct }}%</strong> <span class="st-muted">({{ r.active }})</span></td>
          </tr>
          <tr v-if="data.overall" class="st-total-row">
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

      <h2 class="st-section" style="margin-top:1.5rem">Per school</h2>
      <table v-if="data.bySchool?.length" class="data-table">
        <thead><tr>
          <th>School</th><th>Students</th><th>Passed</th><th>Dropped Out</th><th>Deferred</th><th>Still active</th>
        </tr></thead>
        <tbody>
          <tr v-for="r in data.bySchool" :key="r.label" class="data-row">
            <td style="font-weight:600">{{ r.label }}</td>
            <td>{{ r.total }}</td>
            <td><strong>{{ r.passedPct }}%</strong> <span class="st-muted">({{ r.passed }})</span></td>
            <td><strong>{{ r.droppedPct }}%</strong> <span class="st-muted">({{ r.dropped }})</span></td>
            <td><strong>{{ r.deferredPct }}%</strong> <span class="st-muted">({{ r.deferred }})</span></td>
            <td><strong>{{ r.activePct }}%</strong> <span class="st-muted">({{ r.active }})</span></td>
          </tr>
        </tbody>
      </table>
      <p v-else class="st-sub">No enrolments commenced in this period.</p>

      <h2 class="st-section" style="margin-top:1.5rem">Per programme</h2>
      <table v-if="data.byProgramme?.length" class="data-table">
        <thead><tr>
          <th>Programme / Specialization</th><th>Students</th><th>Passed</th><th>Dropped Out</th><th>Deferred</th><th>Still active</th>
        </tr></thead>
        <tbody>
          <template v-for="pg in data.byProgramme" :key="pg.programme.label">
            <tr class="data-row st-prog-row">
              <td style="font-weight:700">{{ pg.programme.label }}</td>
              <td>{{ pg.programme.total }}</td>
              <td><strong>{{ pg.programme.passedPct }}%</strong> <span class="st-muted">({{ pg.programme.passed }})</span></td>
              <td><strong>{{ pg.programme.droppedPct }}%</strong> <span class="st-muted">({{ pg.programme.dropped }})</span></td>
              <td><strong>{{ pg.programme.deferredPct }}%</strong> <span class="st-muted">({{ pg.programme.deferred }})</span></td>
              <td><strong>{{ pg.programme.activePct }}%</strong> <span class="st-muted">({{ pg.programme.active }})</span></td>
            </tr>
            <tr v-for="sp in pg.specializations" :key="pg.programme.label + sp.label" class="data-row">
              <td class="st-spec">└ {{ sp.label }}</td>
              <td>{{ sp.total }}</td>
              <td>{{ sp.passedPct }}% <span class="st-muted">({{ sp.passed }})</span></td>
              <td>{{ sp.droppedPct }}% <span class="st-muted">({{ sp.dropped }})</span></td>
              <td>{{ sp.deferredPct }}% <span class="st-muted">({{ sp.deferred }})</span></td>
              <td>{{ sp.activePct }}% <span class="st-muted">({{ sp.active }})</span></td>
            </tr>
          </template>
        </tbody>
      </table>
      <p v-else class="st-sub">No enrolments commenced in this period.</p>
    </template>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '../../api/client.js'

const from = ref('')
const to = ref('')
const data = ref({ byPartner: [], bySchool: [], byProgramme: [], overall: null })
const loading = ref(false)
const error = ref('')

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
.st-sub { font-size: .8rem; color: #6b7888; margin: .25rem 0 .75rem; }
.st-filters { display: flex; align-items: center; gap: .5rem; flex-wrap: wrap; margin-bottom: .9rem; }
.st-lbl { font-size: .78rem; color: #44536a; font-weight: 600; }
.st-inp { padding: .4rem .55rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .85rem; background: #fff; }
.st-total { font-size: .8rem; color: #1a2d4f; font-weight: 600; margin-left: .5rem; }
.st-section { font-size: 1rem; color: #003366; margin: .4rem 0 .5rem; }
.st-muted { color: #8a97a8; font-size: .78rem; }
.st-total-row td { font-weight: 700; border-top: 2px solid #d5deea; background: #fafbfd; }
.st-prog-row td { background: #f6f9fd; border-top: 2px solid #e2eaf4; }
.st-spec { padding-left: 1.6rem !important; color: #44536a; }
.data-table { width: 100%; border-collapse: collapse; font-size: .85rem; background: #fff; border-radius: 8px; overflow: hidden; box-shadow: 0 1px 3px rgba(0,0,0,.06); }
.data-table th { text-align: left; padding: .5rem .7rem; color: #6b7888; font-size: .74rem; text-transform: uppercase; letter-spacing: .03em; border-bottom: 1.5px solid #e8edf4; background: #fafbfd; }
.data-table td { padding: .5rem .7rem; border-bottom: 1px solid #eef1f5; }
.btn-sm { background: #f2f5f9; border: 1px solid #cfd7e3; border-radius: 5px; padding: .3rem .6rem; font-size: .78rem; font-weight: 600; color: #2c3e50; cursor: pointer; }
.err-banner { background: #fdf3f2; border: 1px solid #e2b8b5; color: #b3261e; padding: .45rem .7rem; border-radius: 6px; font-size: .8rem; margin-bottom: .5rem; }
.loading-row { color: #6b7888; font-size: .85rem; padding: .5rem 0; }
</style>
