<template>
  <div>
    <div class="sg-controls">
      <label class="ss-muted" style="font-weight:700">Metric</label>
      <select v-model="metric" class="sg-sel">
        <option value="">Signed up</option>
        <option value="paid">💰 Paid (payments received)</option>
        <option v-for="st in STATUSES" :key="st.id" :value="st.id">Switched to {{ st.label }}</option>
      </select>
      <label v-if="data?.isSuper" class="sg-chk" title="Only staff with the Sales admin level — for commission runs">
        <input type="checkbox" v-model="salesOnly" /> Sales staff only</label>
      <div class="sg-view">
        <button type="button" :class="['sg-view-btn', { on: view === 'staff' }]" @click="view = 'staff'">👤 Per staff</button>
        <button type="button" :class="['sg-view-btn', { on: view === 'school' }]" @click="view = 'school'">🏫 Per school</button>
      </div>
    </div>

    <div v-if="error" class="err-banner">{{ error }}</div>
    <div v-if="loading" class="loading-row">Loading…</div>
    <template v-else-if="data">
      <!-- SuperAdministrator: the full board -->
      <template v-if="auth.can('statistics.financial')">
        <p class="ss-total">
          <strong>{{ data.total }}</strong> {{ metricNoun }} in period ·
          <strong>{{ data.staffSignups }}</strong> attributed to staff{{ data.salesOnly ? ' (Sales only)' : '' }} ·
          <strong>{{ data.selfSignups }}</strong> self signups
          <template v-if="isPaid"> · total <strong>{{ money(data.totalAmount) }}</strong></template>
        </p>

        <template v-if="view === 'school'">
          <h3 class="ss-section">🏫 Per school <span class="ss-muted">(all {{ metricNoun }}, attributed or not — click a row for details)</span></h3>
          <table v-if="data.bySchool?.length" class="data-table" style="max-width:900px">
            <thead><tr><th style="width:64px">Rank</th><th>School</th><th style="width:90px">Count</th>
              <th v-if="isPaid" style="width:130px">Amount</th><th style="width:32%"></th></tr></thead>
            <tbody>
              <template v-for="r in data.bySchool" :key="'sc' + r.rank">
                <tr class="lb-row" @click="openSchool = openSchool === r.rank ? 0 : r.rank">
                  <td class="lb-rank">{{ openSchool === r.rank ? '▾' : '▸' }} {{ medal(r.rank) }}</td>
                  <td style="font-weight:600">{{ r.name }}</td>
                  <td><strong>{{ r.count }}</strong></td>
                  <td v-if="isPaid"><strong class="ss-good">{{ money(r.amount) }}</strong></td>
                  <td><div class="ss-bar" style="width:100%"><div class="ss-fill lb-fill"
                    :style="{ width: schoolBarPct(r) + '%' }"></div></div></td>
                </tr>
                <tr v-if="openSchool === r.rank">
                  <td :colspan="isPaid ? 5 : 4" class="lb-details">
                    <DetailTable :details="r.details" :is-paid="isPaid" :money="money" />
                  </td>
                </tr>
              </template>
            </tbody>
          </table>
          <p v-else class="ss-sub">Nothing in this period.</p>
        </template>

        <h3 v-show="view === 'staff'" class="ss-section">🏆 Leaderboard <span class="ss-muted">(click a row for the detail list — commission basis)</span></h3>
        <table v-show="view === 'staff'" v-if="data.leaderboard.length" class="data-table" style="max-width:900px">
          <thead><tr><th style="width:64px">Rank</th><th>Who</th><th style="width:90px">Count</th>
            <th v-if="isPaid" style="width:130px">Amount</th><th style="width:32%"></th></tr></thead>
          <tbody>
            <template v-for="r in data.leaderboard" :key="r.rank">
              <tr :class="{ 'lb-top': r.rank === 1 }" class="lb-row" @click="openRow = openRow === r.rank ? 0 : r.rank">
                <td class="lb-rank">{{ openRow === r.rank ? '▾' : '▸' }} {{ medal(r.rank) }}</td>
                <td style="font-weight:600">{{ r.name }} <span class="ss-chip">{{ r.office }}</span></td>
                <td><strong>{{ r.count }}</strong></td>
                <td v-if="isPaid"><strong class="ss-good">{{ money(r.amount) }}</strong></td>
                <td><div class="ss-bar" style="width:100%"><div class="ss-fill lb-fill"
                  :style="{ width: barPct(r) + '%' }"></div></div></td>
              </tr>
              <tr v-if="openRow === r.rank">
                <td :colspan="isPaid ? 5 : 4" class="lb-details">
                  <DetailTable :details="r.details" :is-paid="isPaid" :money="money" />
                </td>
              </tr>
            </template>
          </tbody>
        </table>
        <p v-else v-show="view === 'staff'" class="ss-sub">Nothing attributed in this period.</p>

        <h3 class="ss-section">{{ metricNoun[0].toUpperCase() + metricNoun.slice(1) }} per day</h3>
        <DayChart :timeline="data.timeline" :is-paid="isPaid" :money="money" />
      </template>

      <!-- Everyone else: their own numbers -->
      <template v-else>
        <div class="me-card">
          <div class="me-big">{{ isPaid ? money(data.me.amount) : data.me.count }}</div>
          <div class="me-lbl">{{ isPaid ? `received across ${data.me.count} payments from your students` : `${metricNoun} you brought in this period` }}</div>
          <div v-if="data.me.rank" class="me-rank">
            {{ medal(data.me.rank) }} You are ranked <strong>#{{ data.me.rank }}</strong> of {{ data.me.of }} — keep going! 💪</div>
          <div v-else class="me-rank">Nothing attributed yet this period — share your referral link! 🔗</div>
        </div>
        <h3 class="ss-section">Your details</h3>
        <DetailTable v-if="view === 'staff'" :details="data.me.details" :is-paid="isPaid" :money="money" />
        <table v-else-if="data.bySchool?.length" class="data-table" style="max-width:700px">
          <thead><tr><th>School</th><th>Count</th><th v-if="isPaid">Amount</th></tr></thead>
          <tbody><tr v-for="r in data.bySchool" :key="r.name">
            <td style="font-weight:600">{{ r.name }}</td><td>{{ r.count }}</td>
            <td v-if="isPaid"><strong class="ss-good">{{ money(r.amount) }}</strong></td>
          </tr></tbody>
        </table>
        <p v-else class="ss-sub">Nothing in this period.</p>
        <h3 class="ss-section">Per day</h3>
        <DayChart :timeline="data.timeline" :is-paid="isPaid" :money="money" />
      </template>
    </template>
  </div>
</template>

<script setup>
import { ref, computed, h, watch, onMounted } from 'vue'
import api from '../../../api/client.js'
import { auth } from '../../../store/auth.js'

const props = defineProps({ from: { type: String, default: '' }, to: { type: String, default: '' } })
const data = ref(null)
const loading = ref(false)
const error = ref('')
const metric = ref('')
const salesOnly = ref(false)
const openRow = ref(0)
const openSchool = ref(0)
const view = ref('staff')
function schoolBarPct(r) {
  const top = data.value.bySchool[0]
  const denom = isPaid.value ? (top.amount || 1) : (top.count || 1)
  return ((isPaid.value ? r.amount : r.count) * 100) / denom
}

// System enrollment statuses (fixed ids).
const STATUSES = [
  { id: '22222222-2222-2222-2222-200000000001', label: 'Draft' },
  { id: '22222222-2222-2222-2222-200000000009', label: 'Application Submitted' },
  { id: '22222222-2222-2222-2222-200000000003', label: 'Application Rejected by Partner' },
  { id: '22222222-2222-2222-2222-200000000005', label: 'Application Rejected by Admission' },
  { id: '22222222-2222-2222-2222-200000000002', label: 'Awaiting Review by Partner' },
  { id: '22222222-2222-2222-2222-200000000004', label: 'Awaiting Review by Admission' },
  { id: '22222222-2222-2222-2222-20000000000a', label: 'Accept Offer' },
  { id: '22222222-2222-2222-2222-200000000006', label: 'Approved by Admission' },
  { id: '22222222-2222-2222-2222-200000000007', label: 'Accept Admission' },
  { id: '22222222-2222-2222-2222-20000000000b', label: 'Awaiting Grades Submit' },
  { id: '22222222-2222-2222-2222-20000000000c', label: 'Awaiting Grades Approval' },
  { id: '22222222-2222-2222-2222-200000000008', label: 'Grades Approved' },
  { id: '22222222-2222-2222-2222-20000000000d', label: 'Deferred' },
  { id: '22222222-2222-2222-2222-20000000000e', label: 'Dropped Out' },
]

const isPaid = computed(() => metric.value === 'paid')
const metricNoun = computed(() => isPaid.value ? 'payments'
  : metric.value ? 'status switches' : 'signups')

function medal(rank) {
  return rank === 1 ? '🥇' : rank === 2 ? '🥈' : rank === 3 ? '🥉' : `#${rank}`
}
function money(v) { return Number(v ?? 0).toLocaleString(undefined, { minimumFractionDigits: 2 }) }
function barPct(r) {
  const top = data.value.leaderboard[0]
  const denom = isPaid.value ? (top.amount || 1) : (top.count || 1)
  return ((isPaid.value ? r.amount : r.count) * 100) / denom
}

const DetailTable = {
  props: { details: { type: Array, default: () => [] }, isPaid: Boolean, money: Function },
  setup(p) {
    return () => !p.details.length
      ? h('p', { class: 'ss-sub' }, 'No entries.')
      : h('table', { class: 'data-table', style: 'max-width:760px' }, [
          h('thead', h('tr', [
            h('th', 'Date'), h('th', 'Student'), h('th', 'Student #'),
            ...(p.isPaid ? [h('th', 'Amount'), h('th', 'What')] : [h('th', 'Note')]),
          ])),
          h('tbody', p.details.map(d => h('tr', [
            h('td', { class: 'ss-mono' }, d.date),
            h('td', d.student),
            h('td', { class: 'ss-mono' }, d.studentNumber),
            ...(p.isPaid
              ? [h('td', h('strong', `${p.money(d.amount)} ${d.currency ?? ''}`)), h('td', d.note ?? '')]
              : [h('td', d.note ?? '')]),
          ]))),
        ])
  },
}

const DayChart = {
  props: { timeline: { type: Array, default: () => [] }, isPaid: Boolean, money: Function },
  setup(p) {
    return () => {
      if (!p.timeline.length) return h('p', { class: 'ss-sub' }, 'Nothing in this period.')
      const val = d => p.isPaid ? d.amount : d.count
      const max = Math.max(...p.timeline.map(val), 1)
      return h('div', { class: 'day-chart' }, p.timeline.map(d =>
        h('div', { class: 'day-col', title: `${d.date}: ${d.count}${p.isPaid ? ` · ${p.money(d.amount)}` : ''}` }, [
          h('div', { class: 'day-count' }, p.isPaid ? p.money(d.amount) : String(d.count)),
          h('div', { class: 'day-bar', style: { height: `${Math.max(6, (val(d) * 100) / max)}%` } }),
          h('div', { class: 'day-date' }, d.date.slice(5)),
        ])))
    }
  },
}

async function load() {
  loading.value = true
  error.value = ''
  openRow.value = 0
  openSchool.value = 0
  try {
    const params = {}
    if (props.from) params.from = props.from
    if (props.to) params.to = props.to
    if (metric.value) params.metric = metric.value
    if (salesOnly.value) params.salesOnly = true
    data.value = (await api.get('/v1/admin/statistics/signups', { params })).data
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to load'
  } finally {
    loading.value = false
  }
}
watch(() => [props.from, props.to], load)
watch([metric, salesOnly], load)
onMounted(load)
</script>

<style scoped>
@import './statShared.css';
.sg-controls { display: flex; align-items: center; gap: .6rem; margin-bottom: .8rem; flex-wrap: wrap; }
.sg-sel { padding: .4rem .55rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .84rem; background: #fff; }
.sg-chk { display: inline-flex; align-items: center; gap: .35rem; font-size: .82rem; color: #44536a; font-weight: 600; cursor: pointer; }
.sg-view { display: inline-flex; gap: .3rem; margin-left: .4rem; }
.sg-view-btn { background: #f2f5f9; border: 1px solid #cfd7e3; border-radius: 5px; padding: .3rem .7rem; font-size: .78rem; font-weight: 600; color: #2c3e50; cursor: pointer; }
.sg-view-btn.on { background: #003366; border-color: #003366; color: #fff; }
.lb-row { cursor: pointer; }
.lb-row:hover td { background: #f0f4fa; }
.lb-rank { font-size: 1rem; white-space: nowrap; }
.lb-top td { background: #fdf9ec; }
.lb-fill { background: #b8860b; }
.lb-details { background: #f8fafd; padding: .7rem 1rem !important; }
.me-card { background: linear-gradient(135deg, #003366, #1a4d80); color: #fff; border-radius: 12px; padding: 1.4rem 1.8rem; max-width: 500px; box-shadow: 0 4px 16px rgba(0,0,0,.15); margin-bottom: 1rem; }
.me-big { font-size: 2.6rem; font-weight: 800; line-height: 1; }
.me-lbl { font-size: .9rem; opacity: .85; margin-top: .3rem; }
.me-rank { margin-top: .8rem; font-size: .95rem; background: rgba(255,255,255,.12); border-radius: 8px; padding: .5rem .8rem; }
:deep(.day-chart) { display: flex; align-items: flex-end; gap: 4px; height: 180px; padding: .6rem; background: #fff; border: 1px solid #e8edf4; border-radius: 8px; overflow-x: auto; }
:deep(.day-col) { display: flex; flex-direction: column; align-items: center; justify-content: flex-end; min-width: 34px; height: 100%; }
:deep(.day-bar) { width: 22px; background: #3b6ea5; border-radius: 3px 3px 0 0; }
:deep(.day-count) { font-size: .66rem; color: #44536a; font-weight: 700; margin-bottom: 2px; white-space: nowrap; }
:deep(.day-date) { font-size: .62rem; color: #8a97a8; margin-top: 3px; white-space: nowrap; }
</style>
