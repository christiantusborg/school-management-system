<template>
  <div class="ps-tab">
    <div class="mock-banner">
      <strong>Backend pending.</strong> Student data is served from the in-app demo dataset. Backend Student CRUD endpoints are a follow-up.
    </div>

    <div class="filter-row">
      <input v-model="search" class="inp-add" placeholder="Search by name or student ID…" />
      <span class="count">{{ filtered.length }} student{{ filtered.length !== 1 ? 's' : '' }}</span>
    </div>

    <div v-if="filtered.length === 0" class="empty-state-card">No students match.</div>
    <table v-else class="data-table">
      <thead><tr><th>Student ID</th><th>Name</th><th>Enrolments</th></tr></thead>
      <tbody>
        <tr v-for="s in filtered" :key="s.id" class="data-row">
          <td class="mono">{{ s.studentId }}</td>
          <td>{{ s.firstName }} {{ s.lastName }}</td>
          <td>
            <div v-for="e in s.enrollments" :key="e.id" class="enrol-line">
              {{ e.programme }} · <em>{{ e.major }}</em>
              <span class="s-badge">{{ e.enrollmentStatus }}</span>
            </div>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { students } from '../../../mock/data.js'

const props = defineProps({ partnerName: { type: String, default: '' } })

const search = ref('')

const filtered = computed(() => {
  const term = search.value.toLowerCase().trim()
  return students.filter(s => {
    if (s.partner !== props.partnerName) return false
    if (!term) return true
    return `${s.firstName} ${s.lastName} ${s.studentId}`.toLowerCase().includes(term)
  })
})
</script>

<style scoped>
.ps-tab { padding: .25rem 0; }
.mock-banner { background: #fff7e0; border: 1px solid #f3d27e; color: #8a6b16; padding: .55rem .8rem; border-radius: 6px; font-size: .85rem; margin-bottom: .75rem; }
.filter-row { display: flex; align-items: center; gap: .75rem; margin-bottom: .55rem; }
.inp-add { padding: .35rem .6rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .88rem; min-width: 260px; }
.count { font-size: .82rem; color: #5f6e85; }
.empty-state-card { padding: 1rem; background: #f6f9fd; color: #5f6e85; border-radius: 8px; text-align: center; }
.data-table { width: 100%; border-collapse: collapse; }
.data-table th { text-align: left; font-size: .75rem; color: #5f6e85; text-transform: uppercase; letter-spacing: .04em; padding: .5rem .7rem; border-bottom: 1px solid #e5eaf1; }
.data-row td { padding: .55rem .7rem; border-bottom: 1px solid #eef2f7; font-size: .9rem; }
.mono { font-family: monospace; font-size: .82rem; color: #0a264f; }
.enrol-line { font-size: .85rem; }
.s-badge { margin-left: .45rem; background: #ecf0f6; color: #5f6e85; font-size: .7rem; padding: 1px 7px; border-radius: 10px; }
</style>
