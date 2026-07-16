<template>
  <div class="pv-wrap">
    <form class="pv-search" @submit.prevent="search">
      <label for="pv-q">Search:</label>
      <input id="pv-q" v-model="query" type="text" autocomplete="off"
        placeholder="Student ID, e.g. IBSS.DBA.23060508" />
      <button type="submit" :disabled="searching || !query.trim()">
        {{ searching ? 'Searching…' : 'Search' }}
      </button>
    </form>

    <p v-if="error" class="pv-error">{{ error }}</p>

    <table v-if="searched && items.length" class="pv-table">
      <thead>
        <tr>
          <th>Student ID</th>
          <th>Student name</th>
          <th>Student Status</th>
          <th>Program</th>
          <th>Major</th>
          <th>Actual Completion date</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="(r, i) in items" :key="i">
          <td>{{ r.studentNumber }}</td>
          <td>{{ r.studentName }}</td>
          <td>{{ r.status }}</td>
          <td>{{ r.programme }}</td>
          <td>{{ r.major }}</td>
          <td>{{ fmtDate(r.completionDate) }}</td>
        </tr>
      </tbody>
    </table>
    <p v-else-if="searched && !searching" class="pv-empty">
      No student found for this ID. Check the ID and try again.
    </p>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import api from '../api/client.js'

const query = ref('')
const items = ref([])
const searched = ref(false)
const searching = ref(false)
const error = ref('')

async function search() {
  const q = query.value.trim()
  if (!q || searching.value) return
  searching.value = true
  error.value = ''
  try {
    const res = await api.get('/v1/public/student-verify', { params: { studentNumber: q } })
    items.value = res.data.items ?? []
    searched.value = true
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Search failed'
    items.value = []
    searched.value = true
  } finally {
    searching.value = false
  }
}

function fmtDate(d) {
  if (!d) return ''
  const dt = new Date(d)
  return Number.isNaN(dt.getTime()) ? '' : dt.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })
}
</script>

<style scoped>
.pv-wrap { background: #fff; padding: 1rem; font-family: Roboto, 'Segoe UI', system-ui, sans-serif; color: #333; }
.pv-search { display: flex; align-items: center; gap: .6rem; margin-bottom: 1rem; flex-wrap: wrap; }
.pv-search label { font-size: .95rem; color: #555; }
.pv-search input { flex: 0 1 320px; padding: .5rem .7rem; border: 1.5px solid #bbb; border-radius: 5px; font-size: .95rem; }
.pv-search input:focus { outline: none; border-color: #1a4d8c; }
.pv-search button { padding: .5rem 1.1rem; border: 1px solid #1a4d8c; background: #1a4d8c; color: #fff; border-radius: 5px; font-size: .9rem; cursor: pointer; }
.pv-search button:disabled { opacity: .6; cursor: not-allowed; }
.pv-table { width: 100%; border-collapse: collapse; font-size: .93rem; }
.pv-table th { text-align: left; background: #f1f1f1; border: 1px solid #ddd; padding: .65rem .9rem; color: #555; font-weight: 700; }
.pv-table td { border: 1px solid #ddd; padding: .65rem .9rem; }
.pv-empty { color: #777; font-size: .93rem; }
.pv-error { color: #b42318; font-size: .93rem; }
</style>
