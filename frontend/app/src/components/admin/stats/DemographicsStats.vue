<template>
  <div>
    <div v-if="error" class="err-banner">{{ error }}</div>
    <div v-if="loading" class="loading-row">Loading…</div>
    <template v-else-if="data">
      <p class="ss-total"><strong>{{ data.total }}</strong> enrolments in period · average age
        <strong>{{ data.avgAge }}</strong></p>
      <div v-for="d in data.dimensions" :key="d.key" class="ss-card" style="margin-bottom:.9rem">
        <div class="ss-card-title" style="font-size:.95rem; color:#003366; font-weight:700">{{ d.label }}</div>
        <div class="ss-stack" v-if="d.cats.length">
          <span v-for="(c, i) in d.cats" :key="c.label" :style="seg(d, c, i)" :title="c.label + ': ' + c.count"></span>
        </div>
        <div class="ss-legend">
          <span v-for="(c, i) in d.cats" :key="c.label" class="ss-leg">
            <i :style="{ background: palette[i % palette.length] }"></i>{{ c.label }}
            <strong>{{ c.count }}</strong> ({{ pct(d, c.count) }}%)</span>
        </div>
        <div class="ss-splitbtns">
          <button type="button" class="ss-toggle" @click="open[d.key + 'p'] = !open[d.key + 'p']">
            {{ open[d.key + 'p'] ? '▾' : '▸' }} Per partner</button>
          <button type="button" class="ss-toggle" @click="open[d.key + 'g'] = !open[d.key + 'g']">
            {{ open[d.key + 'g'] ? '▾' : '▸' }} Per programme</button>
        </div>
        <template v-for="(split, suffix) in { p: d.byPartner, g: d.byProgramme }" :key="suffix">
          <table v-if="open[d.key + suffix]" class="data-table" style="margin-top:.5rem">
            <thead><tr><th>{{ suffix === 'p' ? 'Partner' : 'Programme' }}</th><th>Students</th>
              <th style="width:45%">Distribution</th></tr></thead>
            <tbody>
              <tr v-for="row in split" :key="row.group">
                <td style="font-weight:600">{{ row.group }}</td>
                <td>{{ row.total }}</td>
                <td><div class="ss-stack" style="margin:0">
                  <span v-for="(c, i) in d.cats" :key="c.label"
                    :style="segRow(row, c, i)" :title="c.label + ': ' + (row.cats[c.label] ?? 0)"></span>
                </div></td>
              </tr>
            </tbody>
          </table>
        </template>
      </div>
    </template>
  </div>
</template>

<script setup>
import { ref, reactive, watch, onMounted } from 'vue'
import api from '../../../api/client.js'

const props = defineProps({ from: { type: String, default: '' }, to: { type: String, default: '' } })
const data = ref(null)
const loading = ref(false)
const error = ref('')
const open = reactive({})

const palette = ['#3b6ea5', '#3e8e58', '#e0a24a', '#c4554b', '#7b5ea5', '#4aa3a0',
  '#b0568c', '#8a8f3c', '#5b78c7', '#c47a3b', '#5aa06e', '#9aa4b5']

function pct(d, count) {
  const total = d.cats.reduce((s, c) => s + c.count, 0)
  return total ? Math.round((count * 1000) / total) / 10 : 0
}
function seg(d, c, i) {
  return { width: pct(d, c.count) + '%', background: palette[i % palette.length] }
}
function segRow(row, c, i) {
  const n = row.cats[c.label] ?? 0
  return { width: (row.total ? (n * 100) / row.total : 0) + '%', background: palette[i % palette.length] }
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    const params = {}
    if (props.from) params.from = props.from
    if (props.to) params.to = props.to
    data.value = (await api.get('/v1/admin/statistics/demographics', { params })).data
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
.ss-stack { display: flex; height: 16px; border-radius: 4px; overflow: hidden; background: #edf1f7; margin: .4rem 0; }
.ss-stack span { display: block; height: 100%; }
.ss-legend { display: flex; flex-wrap: wrap; gap: .2rem .8rem; margin-bottom: .35rem; }
.ss-splitbtns { display: flex; gap: .6rem; }
.ss-toggle { background: none; border: none; color: #3b6ea5; font-size: .78rem; font-weight: 600; cursor: pointer; padding: 0; }
</style>
