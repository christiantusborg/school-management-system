<template>
  <div>
    <div v-if="error" class="err-banner">{{ error }}</div>
    <div v-if="loading" class="loading-row">Loading…</div>
    <template v-else-if="data">
      <p class="ss-sub">Grades saved in the period (by graded date). Bands:
        <span class="ss-leg"><i class="b0"></i>0–39</span>
        <span class="ss-leg"><i class="b1"></i>40–59</span>
        <span class="ss-leg"><i class="b2"></i>60–79</span>
        <span class="ss-leg"><i class="b3"></i>80–100</span></p>
      <p class="ss-total"><strong>{{ data.overall.count }}</strong> grades ·
        average <strong>{{ data.overall.avg }}</strong> · pass rate <strong>{{ data.overall.passPct }}%</strong></p>
      <div class="ss-bands" style="max-width:420px"><span v-for="(b, i) in data.overall.bands" :key="i"
        :class="'b' + i" :style="{ width: b + '%' }" :title="bandTitle(i, b)"></span></div>

      <section v-for="sec in sections" :key="sec.title">
        <h3 class="ss-section">{{ sec.title }}</h3>
        <table class="data-table">
          <thead><tr><th>{{ sec.head }}</th><th>Grades</th><th>Average</th><th>Pass rate</th><th style="width:220px">Distribution</th></tr></thead>
          <tbody>
            <tr v-for="r in sec.rows" :key="r.label">
              <td style="font-weight:600">{{ r.label }}<span v-if="r.sub" class="ss-muted"> · {{ r.sub }}</span></td>
              <td>{{ r.count }}</td>
              <td><strong :class="heat(r.avg)">{{ r.avg }}</strong></td>
              <td>
                <div class="ss-barcell"><div class="ss-bar"><div class="ss-fill" :style="{ width: r.passPct + '%' }"></div></div>
                <span>{{ r.passPct }}%</span></div>
              </td>
              <td><div class="ss-bands"><span v-for="(b, i) in r.bands" :key="i" :class="'b' + i"
                :style="{ width: b + '%' }" :title="bandTitle(i, b)"></span></div></td>
            </tr>
          </tbody>
        </table>
      </section>

      <h3 class="ss-section">Module difficulty ranking <span class="ss-muted">(hardest first — lowest average)</span></h3>
      <table class="data-table">
        <thead><tr><th>Module</th><th>Grades</th><th>Average</th><th>Fail rate (&lt;40)</th><th style="width:220px">Distribution</th></tr></thead>
        <tbody>
          <tr v-for="m in data.byModule" :key="m.label">
            <td><span class="ss-mono">{{ m.label }}</span> <span class="ss-muted">{{ m.sub }}</span></td>
            <td>{{ m.count }}</td>
            <td><strong :class="heat(m.avg)">{{ m.avg }}</strong></td>
            <td><div class="ss-barcell"><div class="ss-bar"><div class="ss-fill ss-fill-bad" :style="{ width: m.failPct + '%' }"></div></div>
              <span>{{ m.failPct }}%</span></div></td>
            <td><div class="ss-bands"><span v-for="(b, i) in m.bands" :key="i" :class="'b' + i"
              :style="{ width: b + '%' }" :title="bandTitle(i, b)"></span></div></td>
          </tr>
        </tbody>
      </table>

      <template v-if="data.rubricCriteria.length">
        <h3 class="ss-section">Rubric criterion averages <span class="ss-muted">(which sections pull grades down)</span></h3>
        <div v-for="m in data.rubricCriteria" :key="m.module" class="ss-card">
          <div class="ss-card-title ss-mono">{{ m.module }}</div>
          <div v-for="c in m.criteria" :key="c.section" class="ss-critrow">
            <span class="ss-critname">{{ c.section }}</span>
            <div class="ss-bar" style="flex:1"><div class="ss-fill" :style="{ width: c.avg + '%' }"></div></div>
            <strong :class="heat(c.avg)" style="width:44px; text-align:right">{{ c.avg }}</strong>
            <span class="ss-muted" style="width:70px">({{ c.count }} scores)</span>
          </div>
        </div>
      </template>
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

const sections = computed(() => [
  { title: 'By partner', head: 'Partner', rows: data.value?.byPartner ?? [] },
  { title: 'By school', head: 'School', rows: data.value?.bySchool ?? [] },
  { title: 'By programme', head: 'Programme', rows: data.value?.byProgramme ?? [] },
])

function bandTitle(i, b) {
  return ['0–39', '40–59', '60–79', '80–100'][i] + ': ' + b + '%'
}
function heat(v) {
  return v >= 70 ? 'ss-good' : v >= 50 ? 'ss-mid' : 'ss-bad'
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    const params = {}
    if (props.from) params.from = props.from
    if (props.to) params.to = props.to
    data.value = (await api.get('/v1/admin/statistics/grades', { params })).data
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
