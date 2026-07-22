<template>
  <div>
    <div v-if="error" class="err-banner">{{ error }}</div>
    <div v-if="loading" class="loading-row">Loading…</div>
    <template v-else-if="data">
      <p class="ss-sub">Payment plan instalments whose due date falls in the period. "Paid" sums the amounts of
        instalments marked paid; "overdue" are unpaid instalments past their due date (as of today).</p>
      <table class="data-table">
        <thead><tr><th>Partner</th><th>Paid</th><th>Paid amount</th><th>Overdue</th><th>Overdue amount</th>
          <th>Upcoming</th><th>Upcoming amount</th><th>Students overdue</th></tr></thead>
        <tbody>
          <tr v-for="p in data.perPartner" :key="p.partner">
            <td style="font-weight:600">{{ p.partner }}</td>
            <td>{{ p.paidCount }}</td>
            <td><strong class="ss-good">{{ money(p.paidAmount) }}</strong></td>
            <td>{{ p.overdueCount }}</td>
            <td><strong :class="p.overdueAmount > 0 ? 'ss-bad' : ''">{{ money(p.overdueAmount) }}</strong></td>
            <td>{{ p.upcomingCount }}</td>
            <td>{{ money(p.upcomingAmount) }}</td>
            <td><strong :class="p.studentsWithOverdue ? 'ss-bad' : ''">{{ p.studentsWithOverdue }}</strong></td>
          </tr>
          <tr class="ss-totalrow">
            <td>{{ data.overall.partner }}</td>
            <td>{{ data.overall.paidCount }}</td>
            <td><strong class="ss-good">{{ money(data.overall.paidAmount) }}</strong></td>
            <td>{{ data.overall.overdueCount }}</td>
            <td><strong :class="data.overall.overdueAmount > 0 ? 'ss-bad' : ''">{{ money(data.overall.overdueAmount) }}</strong></td>
            <td>{{ data.overall.upcomingCount }}</td>
            <td>{{ money(data.overall.upcomingAmount) }}</td>
            <td>{{ data.overall.studentsWithOverdue }}</td>
          </tr>
        </tbody>
      </table>
      <p v-if="!data.perPartner.length" class="ss-sub">No payment plan instalments in this period.</p>
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

function money(v) { return Number(v ?? 0).toLocaleString(undefined, { minimumFractionDigits: 2 }) }

async function load() {
  loading.value = true
  error.value = ''
  try {
    const params = {}
    if (props.from) params.from = props.from
    if (props.to) params.to = props.to
    data.value = (await api.get('/v1/admin/statistics/finance', { params })).data
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
.ss-totalrow td { font-weight: 700; border-top: 2px solid #d5deea; background: #fafbfd; }
</style>
