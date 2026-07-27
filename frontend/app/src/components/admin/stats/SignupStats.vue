<template>
  <div>
    <div v-if="error" class="err-banner">{{ error }}</div>
    <div v-if="loading" class="loading-row">Loading…</div>
    <template v-else-if="data">
      <!-- SuperAdministrator: the full board -->
      <template v-if="data.isSuper">
        <p class="ss-total">
          <strong>{{ data.total }}</strong> signups in period ·
          <strong>{{ data.staffSignups }}</strong> brought in by staff ·
          <strong>{{ data.selfSignups }}</strong> self signups (public page)
        </p>

        <h3 class="ss-section">🏆 Leaderboard</h3>
        <table v-if="data.leaderboard.length" class="data-table" style="max-width:760px">
          <thead><tr><th style="width:64px">Rank</th><th>Who</th><th style="width:90px">Signups</th><th style="width:40%"></th></tr></thead>
          <tbody>
            <tr v-for="r in data.leaderboard" :key="r.rank" :class="{ 'lb-top': r.rank === 1 }">
              <td class="lb-rank">{{ medal(r.rank) }}</td>
              <td style="font-weight:600">{{ r.name }}
                <span class="ss-chip">{{ r.office }}</span></td>
              <td><strong>{{ r.count }}</strong></td>
              <td><div class="ss-bar" style="width:100%"><div class="ss-fill lb-fill"
                :style="{ width: (r.count * 100 / data.leaderboard[0].count) + '%' }"></div></div></td>
            </tr>
          </tbody>
        </table>
        <p v-else class="ss-sub">No staff-attributed signups in this period yet.</p>

        <h3 class="ss-section">Signups per day</h3>
        <DayChart :timeline="data.timeline" />
      </template>

      <!-- Everyone else: their own numbers -->
      <template v-else>
        <div class="me-card">
          <div class="me-big">{{ data.me.count }}</div>
          <div class="me-lbl">signups you brought in this period</div>
          <div v-if="data.me.rank" class="me-rank">
            {{ medal(data.me.rank) }} You are ranked <strong>#{{ data.me.rank }}</strong> of {{ data.me.of }} — keep going! 💪</div>
          <div v-else-if="data.me.of" class="me-rank">No attributed signups yet this period — share your referral link! 🔗</div>
        </div>
        <h3 class="ss-section">Your signups per day</h3>
        <DayChart :timeline="data.timeline" />
      </template>
    </template>
  </div>
</template>

<script setup>
import { ref, h, watch, onMounted } from 'vue'
import api from '../../../api/client.js'

const props = defineProps({ from: { type: String, default: '' }, to: { type: String, default: '' } })
const data = ref(null)
const loading = ref(false)
const error = ref('')

function medal(rank) {
  return rank === 1 ? '🥇' : rank === 2 ? '🥈' : rank === 3 ? '🥉' : `#${rank}`
}

// Tiny vertical-bar day chart, no library.
const DayChart = {
  props: { timeline: { type: Array, default: () => [] } },
  setup(p) {
    return () => {
      if (!p.timeline.length) return h('p', { class: 'ss-sub' }, 'Nothing in this period.')
      const max = Math.max(...p.timeline.map(d => d.count), 1)
      return h('div', { class: 'day-chart' }, p.timeline.map(d =>
        h('div', { class: 'day-col', title: `${d.date}: ${d.count}` }, [
          h('div', { class: 'day-count' }, String(d.count)),
          h('div', { class: 'day-bar', style: { height: `${Math.max(6, (d.count * 100) / max)}%` } }),
          h('div', { class: 'day-date' }, d.date.slice(5)),
        ])))
    }
  },
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    const params = {}
    if (props.from) params.from = props.from
    if (props.to) params.to = props.to
    data.value = (await api.get('/v1/admin/statistics/signups', { params })).data
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
.lb-rank { font-size: 1.05rem; }
.lb-top td { background: #fdf9ec; }
.lb-fill { background: #b8860b; }
.me-card { background: linear-gradient(135deg, #003366, #1a4d80); color: #fff; border-radius: 12px; padding: 1.4rem 1.8rem; max-width: 460px; box-shadow: 0 4px 16px rgba(0,0,0,.15); }
.me-big { font-size: 3rem; font-weight: 800; line-height: 1; }
.me-lbl { font-size: .9rem; opacity: .85; margin-top: .3rem; }
.me-rank { margin-top: .8rem; font-size: .95rem; background: rgba(255,255,255,.12); border-radius: 8px; padding: .5rem .8rem; }
:deep(.day-chart) { display: flex; align-items: flex-end; gap: 4px; height: 180px; padding: .6rem; background: #fff; border: 1px solid #e8edf4; border-radius: 8px; overflow-x: auto; }
:deep(.day-col) { display: flex; flex-direction: column; align-items: center; justify-content: flex-end; min-width: 34px; height: 100%; }
:deep(.day-bar) { width: 22px; background: #3b6ea5; border-radius: 3px 3px 0 0; }
:deep(.day-count) { font-size: .68rem; color: #44536a; font-weight: 700; margin-bottom: 2px; }
:deep(.day-date) { font-size: .62rem; color: #8a97a8; margin-top: 3px; white-space: nowrap; }
</style>
