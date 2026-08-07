<template>
  <div class="ba">
    <div class="ba-top">
      <button class="ba-btn ba-primary" @click="openCreate">+ New bulk agreement</button>
      <span v-if="err" class="ba-err">{{ err }}</span>
    </div>

    <div v-if="form" class="ba-form">
      <h4>{{ form.bulkAgreementId ? `Edit ${form.agreementNumber}` : 'New bulk agreement' }}</h4>
      <div class="ba-row">
        <label>Kind</label>
        <select v-model.number="form.kind" :disabled="!!form.bulkAgreementId">
          <option :value="0">By Commencement date</option>
          <option :value="1">By Graduation date</option>
        </select>
        <label>Period</label>
        <input type="date" v-model="form.periodFrom" />
        <span>–</span>
        <input type="date" v-model="form.periodTo" />
        <label>Students</label>
        <input type="number" min="0" v-model.number="form.targetStudents" class="ba-num" />
      </div>
      <div class="ba-row">
        <label>Note</label>
        <input v-model="form.note" class="ba-note" placeholder="Optional note…" />
      </div>
      <div class="ba-specs">
        <div class="ba-specs-head">
          <strong>Specializations</strong>
          <button class="ba-btn" @click="toggleAll(true)">Select all</button>
          <button class="ba-btn" @click="toggleAll(false)">Clear</button>
        </div>
        <div v-for="p in options" :key="p.programmeId" class="ba-prog">
          <label class="ba-prog-head">
            <input type="checkbox" :checked="p.specializations.every(sp => form.specializationIds.includes(sp.specializationId))"
                   @change="toggleProgramme(p, $event.target.checked)" />
            <strong>{{ p.name }} ({{ p.code }})</strong>
          </label>
          <label v-for="sp in p.specializations" :key="sp.specializationId" class="ba-spec">
            <input type="checkbox" :value="sp.specializationId" v-model="form.specializationIds" /> {{ sp.name }}
          </label>
        </div>
      </div>
      <div class="ba-actions">
        <button class="ba-btn ba-primary" :disabled="busy || !form.periodFrom || !form.periodTo || !form.specializationIds.length"
                @click="save">{{ busy ? 'Saving…' : 'Save agreement' }}</button>
        <button class="ba-btn" @click="form = null">Cancel</button>
      </div>
    </div>

    <div v-for="a in items" :key="a.bulkAgreementId" class="ba-card">
      <div class="ba-card-head" @click="toggleOpen(a)">
        <span class="ba-caret">{{ open === a.bulkAgreementId ? '▾' : '▸' }}</span>
        <strong class="ba-number">{{ a.agreementNumber }}</strong>
        <span :class="['ba-kind', a.kind === 0 ? 'k0' : 'k1']">{{ a.kind === 0 ? 'Commencement' : 'Graduation' }}</span>
        <span class="ba-period">{{ (a.periodFrom || '').slice(0, 10) }} – {{ (a.periodTo || '').slice(0, 10) }}</span>
        <span class="ba-progress" :class="{ full: a.coveredCount >= a.targetStudents }">
          {{ a.coveredCount }} / {{ a.targetStudents }} students
        </span>
        <span class="ba-actions-inline" @click.stop>
          <button class="ba-btn" @click="download(a, 'csv')">CSV</button>
          <button class="ba-btn" @click="download(a, 'pdf')">PDF</button>
          <button class="ba-btn" @click="openEdit(a)">Edit</button>
          <button class="ba-btn ba-danger" @click="remove(a)">Delete</button>
        </span>
      </div>
      <div class="ba-specs-line">{{ (a.specializationNames || []).join(' · ') }}</div>
      <p v-if="a.note" class="ba-note-line">{{ a.note }}</p>
      <div v-if="open === a.bulkAgreementId" class="ba-students">
        <p v-if="loadingStudents" class="ba-muted">Loading…</p>
        <table v-else-if="students.length" class="ba-tbl">
          <thead><tr><th>Student #</th><th>Name</th><th>Programme</th><th>Specialization</th><th>{{ a.kind === 0 ? 'Commencement' : 'Graduation' }}</th></tr></thead>
          <tbody>
            <tr v-for="s in students" :key="s.studentEnrollmentId">
              <td><a class="ba-link" @click="$emit('open-student', s.studentId)">{{ s.studentNumber }}</a></td>
              <td><a class="ba-link" @click="$emit('open-student', s.studentId)">{{ s.name }}</a></td>
              <td>{{ s.programmeCode }}</td>
              <td>{{ s.specializationName }}</td>
              <td>{{ (s.date || '').slice(0, 10) }}</td>
            </tr>
          </tbody>
        </table>
        <p v-else class="ba-muted">No students covered yet.</p>
      </div>
    </div>
    <p v-if="!items.length && !form" class="ba-muted">No bulk agreements for this partner yet.</p>
  </div>
</template>

<script setup>
import { ref, watch, onMounted } from 'vue'
import api from '../../api/client.js'

const props = defineProps({ partnerId: { type: String, required: true } })
defineEmits(['open-student'])

const items = ref([])
const options = ref([])
const form = ref(null)
const open = ref('')
const students = ref([])
const loadingStudents = ref(false)
const busy = ref(false)
const err = ref('')

async function load() {
  if (!props.partnerId) return
  try {
    const [list, opts] = await Promise.all([
      api.get(`/v1/admin/partners/${props.partnerId}/bulk-agreements`),
      api.get('/v1/admin/students/enrolment-options', { params: { partnerId: props.partnerId } }),
    ])
    items.value = list.data.items ?? []
    options.value = opts.data.items ?? []
  } catch (e) { err.value = e.response?.data?.error ?? e.message }
}
function openCreate() {
  form.value = { bulkAgreementId: null, kind: 0, periodFrom: '', periodTo: '', targetStudents: 0, note: '', specializationIds: [] }
}
function openEdit(a) {
  form.value = {
    bulkAgreementId: a.bulkAgreementId, agreementNumber: a.agreementNumber, kind: a.kind,
    periodFrom: (a.periodFrom || '').slice(0, 10), periodTo: (a.periodTo || '').slice(0, 10),
    targetStudents: a.targetStudents, note: a.note ?? '', specializationIds: [...(a.specializationIds ?? [])],
  }
}
function toggleAll(on) {
  form.value.specializationIds = on
    ? options.value.flatMap(p => p.specializations.map(sp => sp.specializationId))
    : []
}
function toggleProgramme(p, on) {
  const ids = p.specializations.map(sp => sp.specializationId)
  const cur = new Set(form.value.specializationIds)
  ids.forEach(id => on ? cur.add(id) : cur.delete(id))
  form.value.specializationIds = [...cur]
}
async function save() {
  if (busy.value) return
  busy.value = true; err.value = ''
  try {
    const body = {
      kind: form.value.kind, periodFrom: form.value.periodFrom, periodTo: form.value.periodTo,
      targetStudents: Number(form.value.targetStudents) || 0, note: form.value.note || null,
      specializationIds: form.value.specializationIds,
    }
    if (form.value.bulkAgreementId)
      await api.patch(`/v1/admin/partners/${props.partnerId}/bulk-agreements/${form.value.bulkAgreementId}`, body)
    else
      await api.post(`/v1/admin/partners/${props.partnerId}/bulk-agreements`, body)
    form.value = null
    await load()
  } catch (e) { err.value = e.response?.data?.error ?? e.message }
  finally { busy.value = false }
}
async function remove(a) {
  if (!confirm(`Delete agreement ${a.agreementNumber}?`)) return
  try {
    await api.delete(`/v1/admin/partners/${props.partnerId}/bulk-agreements/${a.bulkAgreementId}`)
    await load()
  } catch (e) { err.value = e.response?.data?.error ?? e.message }
}
async function toggleOpen(a) {
  if (open.value === a.bulkAgreementId) { open.value = ''; return }
  open.value = a.bulkAgreementId
  loadingStudents.value = true
  students.value = []
  try {
    const res = await api.get(`/v1/admin/partners/${props.partnerId}/bulk-agreements/${a.bulkAgreementId}/students`)
    students.value = res.data.items ?? []
  } catch (e) { err.value = e.response?.data?.error ?? e.message }
  finally { loadingStudents.value = false }
}
async function download(a, format) {
  try {
    const res = await api.get(`/v1/admin/partners/${props.partnerId}/bulk-agreements/${a.bulkAgreementId}/export`,
      { params: { format }, responseType: 'blob' })
    const url = URL.createObjectURL(res.data)
    const el = document.createElement('a')
    el.href = url; el.download = `${a.agreementNumber}.${format}`; el.click()
    URL.revokeObjectURL(url)
  } catch { err.value = 'Download failed' }
}

watch(() => props.partnerId, load)
onMounted(load)
</script>

<style scoped>
.ba { padding: .25rem 0; }
.ba-muted { color: #5f6e85; font-size: .82rem; }
.ba-err { color: #b42318; font-size: .8rem; }
.ba-top { display: flex; align-items: center; gap: .6rem; margin-bottom: .6rem; }
.ba-btn { padding: .32rem .65rem; border: 1px solid #cfd7e3; background: #fff; border-radius: 6px; font-size: .78rem; cursor: pointer; }
.ba-primary { background: #0b2e59; color: #fff; border-color: #0b2e59; }
.ba-danger { border-color: #b63329; color: #b63329; }
.ba-form { background: #fff; border: 1.5px solid #cfd7e3; border-radius: 8px; padding: .7rem .9rem; margin-bottom: .7rem; }
.ba-form h4 { margin: 0 0 .5rem; font-size: .92rem; color: #0b2e59; }
.ba-row { display: flex; align-items: center; gap: .5rem; margin-bottom: .45rem; flex-wrap: wrap; }
.ba-row label { font-size: .74rem; font-weight: 700; color: #5f6e85; text-transform: uppercase; }
.ba-row input, .ba-row select { padding: .32rem .5rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .82rem; }
.ba-num { width: 90px; }
.ba-note { flex: 1; min-width: 260px; }
.ba-specs { border-top: 1px dashed #e6ebf2; padding-top: .5rem; }
.ba-specs-head { display: flex; align-items: center; gap: .5rem; margin-bottom: .4rem; font-size: .82rem; }
.ba-prog { margin-bottom: .35rem; }
.ba-prog-head { display: flex; align-items: center; gap: .4rem; font-size: .82rem; }
.ba-spec { display: inline-flex; align-items: center; gap: .3rem; font-size: .8rem; background: #f6f9fd; border: 1px solid #e0e6ee; border-radius: 6px; padding: .2rem .5rem; margin: .15rem .25rem .15rem 1.4rem; }
.ba-actions { display: flex; gap: .5rem; margin-top: .5rem; }
.ba-card { background: #fff; border: 1px solid #e0e6ee; border-radius: 8px; padding: .55rem .8rem; margin-bottom: .5rem; }
.ba-card-head { display: flex; align-items: center; gap: .6rem; cursor: pointer; flex-wrap: wrap; }
.ba-caret { color: #6b7888; }
.ba-number { font-family: monospace; color: #0b2e59; }
.ba-kind { font-size: .68rem; font-weight: 800; border-radius: 9px; padding: 1px 8px; text-transform: uppercase; }
.ba-kind.k0 { background: #e0f1ff; color: #1058a4; }
.ba-kind.k1 { background: #d7f0df; color: #1c7a4a; }
.ba-period { font-size: .78rem; color: #445; }
.ba-progress { font-size: .8rem; font-weight: 700; color: #8a6b16; background: #fff1cc; border-radius: 9px; padding: 1px 9px; }
.ba-progress.full { background: #d7f0df; color: #1c7a4a; }
.ba-actions-inline { margin-left: auto; display: flex; gap: .3rem; }
.ba-specs-line { font-size: .74rem; color: #667; margin-top: .25rem; }
.ba-note-line { font-size: .76rem; color: #5f6e85; margin: .2rem 0 0; }
.ba-students { border-top: 1px dashed #e6ebf2; margin-top: .45rem; padding-top: .45rem; }
.ba-tbl { width: 100%; border-collapse: collapse; font-size: .8rem; }
.ba-tbl th { text-align: left; font-size: .66rem; text-transform: uppercase; color: #6b7888; padding: .25rem .4rem; border-bottom: 1px solid #e8edf3; }
.ba-tbl td { padding: .3rem .4rem; border-bottom: 1px solid #f0f3f7; }
.ba-link { color: #1058a4; cursor: pointer; text-decoration: underline; }
</style>
