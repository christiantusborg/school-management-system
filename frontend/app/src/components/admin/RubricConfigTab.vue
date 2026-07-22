<template>
  <div>
    <div class="rb-head">
      <div>
        <h2 class="rb-title">Grading Rubrics</h2>
        <p class="rb-sub">Reusable rubric templates: rows of Section / Criteria / Max %, where the percentages
          must total exactly 100. Attach a rubric to a module on the Academic page — that module is then graded
          per criterion (1–100 each) on the cohort Grades tab, and the final module grade is the weighted total.</p>
      </div>
      <button type="button" class="btn-primary-sm" @click="openNew">+ New rubric</button>
    </div>

    <div v-if="error" class="err-banner">{{ error }}</div>
    <div v-if="loading" class="loading-row">Loading…</div>
    <table v-else-if="items.length" class="data-table">
      <thead><tr><th>Name</th><th>Rows</th><th>Used by</th><th style="width:150px"></th></tr></thead>
      <tbody>
        <tr v-for="t in items" :key="t.id" class="data-row">
          <td style="font-weight:600">{{ t.name }}</td>
          <td>{{ t.rows.length }} row{{ t.rows.length === 1 ? '' : 's' }}</td>
          <td>{{ t.usedBy }} module{{ t.usedBy === 1 ? '' : 's' }}</td>
          <td style="text-align:right; white-space:nowrap;">
            <button type="button" class="btn-sm" @click="openEdit(t)">✎ Edit</button>
            <button v-if="!t.usedBy" type="button" class="btn-sm rb-del" @click="del(t)">🗑</button>
          </td>
        </tr>
      </tbody>
    </table>
    <p v-else class="rb-sub">No rubrics yet — click <strong>+ New rubric</strong> to create the first template.</p>

    <div v-if="dlg" class="rb-overlay" @click.self="dlg = false">
      <div class="rb-dialog">
        <h3 class="rb-dlg-title">{{ editId ? 'Edit rubric' : 'New rubric' }}</h3>
        <label class="rb-lbl">Name</label>
        <input v-model="name" class="rb-inp" style="width:60%" placeholder="e.g. Dissertation Rubric" />

        <div class="rb-rows-head"><span>Section</span><span>Criteria</span><span>Max %</span><span></span></div>
        <div v-for="(r, i) in rows" :key="i" class="rb-row">
          <input v-model="r.section" class="rb-inp" placeholder="e.g. Literature Review" />
          <textarea v-model="r.criteria" class="rb-inp" rows="2" placeholder="What is assessed in this row…"></textarea>
          <input v-model.number="r.maxPercent" type="number" min="1" max="100" class="rb-inp" />
          <button type="button" class="btn-sm rb-del" @click="rows.splice(i, 1)">✕</button>
        </div>
        <div class="rb-row-foot">
          <button type="button" class="btn-sm" @click="rows.push({ section: '', criteria: '', maxPercent: null })">+ Add row</button>
          <span :class="['rb-total', total === 100 ? 'ok' : 'bad']">
            Total: {{ total }}% {{ total === 100 ? '✓' : '(must be exactly 100)' }}</span>
        </div>

        <p v-if="dlgError" class="err-banner" style="margin-top:.6rem">{{ dlgError }}</p>
        <div class="rb-actions">
          <button type="button" class="btn-sm" @click="dlg = false">Cancel</button>
          <button type="button" class="btn-primary-sm" :disabled="!canSave || saving" @click="save">
            {{ saving ? 'Saving…' : 'Save rubric' }}</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import api from '../../api/client.js'

const items = ref([])
const loading = ref(false)
const error = ref('')

const dlg = ref(false)
const editId = ref(null)
const origName = ref('')
const name = ref('')
const rows = ref([])
const dlgError = ref('')
const saving = ref(false)

const total = computed(() => rows.value.reduce((s, r) => s + (Number(r.maxPercent) || 0), 0))
const canSave = computed(() =>
  name.value.trim() && rows.value.length && total.value === 100 && rows.value.every(r => r.section.trim()))

async function load() {
  loading.value = true
  error.value = ''
  try {
    const res = await api.get('/v1/admin/rubric-templates')
    items.value = res.data.items ?? []
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to load rubrics'
  } finally {
    loading.value = false
  }
}

function openNew() {
  editId.value = null
  origName.value = ''
  name.value = ''
  rows.value = [{ section: '', criteria: '', maxPercent: null }]
  dlgError.value = ''
  dlg.value = true
}

function openEdit(t) {
  editId.value = t.id
  origName.value = t.name
  name.value = t.name
  rows.value = t.rows.map(r => ({ id: r.id, section: r.section, criteria: r.criteria, maxPercent: r.maxPercent }))
  dlgError.value = ''
  dlg.value = true
}

async function save() {
  if (saving.value || !canSave.value) return
  saving.value = true
  dlgError.value = ''
  try {
    let id = editId.value
    if (!id) {
      const res = await api.post('/v1/admin/rubric-templates', { name: name.value.trim() })
      id = res.data.id
    } else if (name.value.trim() !== origName.value) {
      await api.patch(`/v1/admin/rubric-templates/${id}`, { name: name.value.trim() })
    }
    await api.put(`/v1/admin/rubric-templates/${id}/structure`, {
      rows: rows.value.map(r => ({ id: r.id, section: r.section, criteria: r.criteria, maxPercent: r.maxPercent })),
    })
    dlg.value = false
    await load()
  } catch (e) {
    dlgError.value = e.response?.data?.error ?? e.message ?? 'Save failed'
  } finally {
    saving.value = false
  }
}

async function del(t) {
  if (!confirm(`Delete rubric "${t.name}"?`)) return
  error.value = ''
  try {
    await api.delete(`/v1/admin/rubric-templates/${t.id}`)
    await load()
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Delete failed'
  }
}

onMounted(load)
</script>

<style scoped>
.rb-head { display: flex; justify-content: space-between; align-items: flex-start; gap: 1rem; margin-bottom: .9rem; }
.rb-title { margin: 0; font-size: 1.25rem; color: #003366; }
.rb-sub { font-size: .8rem; color: #6b7888; margin: .25rem 0 0; max-width: 46rem; }
.rb-lbl { display: block; font-size: .78rem; color: #44536a; font-weight: 600; margin: .5rem 0 .2rem; }
.rb-inp { padding: .4rem .55rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .85rem; background: #fff; width: 100%; box-sizing: border-box; }
.rb-rows-head, .rb-row { display: grid; grid-template-columns: 1fr 2fr 90px 36px; gap: .5rem; align-items: start; margin-top: .45rem; }
.rb-rows-head { font-size: .72rem; text-transform: uppercase; letter-spacing: .03em; color: #6b7888; font-weight: 700; margin-top: 1rem; }
.rb-row-foot { display: flex; align-items: center; gap: .8rem; margin-top: .6rem; }
.rb-total { font-size: .82rem; font-weight: 700; }
.rb-total.ok { color: #1d7a3e; }
.rb-total.bad { color: #b3261e; }
.rb-del { color: #b3261e; }
.rb-overlay { position: fixed; inset: 0; background: rgba(15, 30, 55, .45); display: flex; align-items: flex-start; justify-content: center; padding: 4rem 1rem; z-index: 60; overflow-y: auto; }
.rb-dialog { background: #fff; border-radius: 10px; padding: 1.2rem 1.4rem 1.1rem; width: 100%; max-width: 760px; box-shadow: 0 12px 40px rgba(0,0,0,.25); }
.rb-dlg-title { margin: 0 0 .4rem; font-size: 1.05rem; color: #003366; }
.rb-actions { display: flex; justify-content: flex-end; gap: .6rem; margin-top: 1rem; }
.data-table { width: 100%; border-collapse: collapse; font-size: .85rem; background: #fff; border-radius: 8px; overflow: hidden; box-shadow: 0 1px 3px rgba(0,0,0,.06); }
.data-table th { text-align: left; padding: .5rem .7rem; color: #6b7888; font-size: .74rem; text-transform: uppercase; letter-spacing: .03em; border-bottom: 1.5px solid #e8edf4; background: #fafbfd; }
.data-table td { padding: .5rem .7rem; border-bottom: 1px solid #eef1f5; }
.btn-sm { background: #f2f5f9; border: 1px solid #cfd7e3; border-radius: 5px; padding: .3rem .6rem; font-size: .78rem; font-weight: 600; color: #2c3e50; cursor: pointer; }
.btn-primary-sm { background: #003366; border: 1px solid #003366; color: #fff; border-radius: 5px; padding: .35rem .8rem; font-size: .8rem; font-weight: 600; cursor: pointer; }
.btn-primary-sm:disabled { opacity: .5; cursor: default; }
.err-banner { background: #fdf3f2; border: 1px solid #e2b8b5; color: #b3261e; padding: .45rem .7rem; border-radius: 6px; font-size: .8rem; margin-bottom: .5rem; }
.loading-row { color: #6b7888; font-size: .85rem; padding: .5rem 0; }
</style>
