<template>
  <div class="sln-panel">
    <!-- Compose -->
    <div class="sln-compose">
      <div class="sln-compose-row">
        <select v-model="level">
          <option value="">General (student)</option>
          <option v-for="e in enrollments" :key="e.studentEnrollmentId" :value="e.studentEnrollmentId">
            {{ enrLabel(e) }}
          </option>
        </select>
        <input v-model="title" type="text" maxlength="300" placeholder="Title" />
      </div>
      <textarea v-model="content" rows="3" placeholder="Write a note… (notes cannot be edited or deleted afterwards)"></textarea>
      <div class="sln-compose-row">
        <template v-if="mode === 'admin'">
          <label><input type="checkbox" v-model="visPartner" /> Visible to partner</label>
          <label><input type="checkbox" v-model="visStudent" /> Visible to student</label>
        </template>
        <template v-else>
          <span class="sln-muted">The Admission Office always sees your notes.</span>
          <label><input type="checkbox" v-model="visStudent" /> Visible to student</label>
        </template>
        <button class="sln-btn" :disabled="!content.trim() || busy" @click="add">Add note</button>
      </div>
    </div>

    <p v-if="error" class="sln-error">{{ error }}</p>
    <p v-if="loading" class="sln-muted">Loading…</p>
    <p v-else-if="!notes.length" class="sln-muted">No notes yet.</p>

    <!-- Immutable log, newest first -->
    <div v-for="n in notes" :key="n.studentLogNoteId" class="sln-note">
      <div class="sln-note-head">
        <strong v-if="n.title">{{ n.title }}</strong>
        <span class="sln-chip sln-chip-level">{{ levelLabel(n) }}</span>
        <span class="sln-chip" :class="n.authorRole === 'Admission' ? 'sln-chip-adm' : 'sln-chip-par'">
          {{ n.authorRole === 'Admission' ? 'Admission Office' : 'Partner' }}<template v-if="n.authorName"> · {{ n.authorName }}</template>
        </span>
        <span class="sln-date">{{ fmtDate(n.createdAt) }}</span>
      </div>
      <p class="sln-content">{{ n.content }}</p>
      <div class="sln-vis">
        <span class="sln-chip" :class="n.visibleToPartner ? 'sln-chip-on' : 'sln-chip-off'">Partner {{ n.visibleToPartner ? '✓' : '—' }}</span>
        <span class="sln-chip" :class="n.visibleToStudent ? 'sln-chip-on' : 'sln-chip-off'">Student {{ n.visibleToStudent ? '✓' : '—' }}</span>
        <template v-if="mode === 'admin'">
          <!-- Admission can grant and revoke; a partner note can never be
               hidden from the partner itself. -->
          <button v-if="n.authorRole !== 'Partner'" class="sln-btn-sm" :disabled="busy"
                  @click="widen(n, { visibleToPartner: !n.visibleToPartner })">
            {{ n.visibleToPartner ? 'Hide from partner' : 'Open to partner' }}</button>
          <button class="sln-btn-sm" :disabled="busy"
                  @click="widen(n, { visibleToStudent: !n.visibleToStudent })">
            {{ n.visibleToStudent ? 'Hide from student' : 'Open to student' }}</button>
        </template>
        <template v-else-if="canWiden(n)">
          <button v-if="!n.visibleToStudent" class="sln-btn-sm" :disabled="busy"
                  @click="widen(n, { visibleToStudent: true })">Open to student</button>
        </template>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, watch } from 'vue'
import api from '../../api/client.js'

const props = defineProps({
  mode: { type: String, required: true },        // 'admin' | 'partner'
  apiRoot: { type: String, required: true },     // '/v1/admin/students/{id}' | '/v1/partner/my-students/{id}'
  enrollments: { type: Array, default: () => [] },
})

const notes = ref([])
const loading = ref(false)
const busy = ref(false)
const error = ref('')
const level = ref('')
const title = ref('')
const content = ref('')
const visPartner = ref(false)
const visStudent = ref(false)

function enrLabel(e) {
  return [e.programmeCode || e.programmeName, e.specializationName].filter(Boolean).join(' / ') || 'Programme'
}

function levelLabel(n) {
  if (!n.enrollmentId) return 'General'
  const e = props.enrollments.find(x => x.studentEnrollmentId === n.enrollmentId)
  return e ? enrLabel(e) : 'Programme'
}

// Only the authoring side can widen; the server enforces the same rule.
function canWiden(n) {
  return props.mode === 'admin' ? n.authorRole === 'Admission' : n.authorRole === 'Partner'
}

function fmtDate(iso) {
  return new Date(iso).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    const res = await api.get(`${props.apiRoot}/log-notes`)
    notes.value = res.data.notes ?? []
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to load notes'
  } finally {
    loading.value = false
  }
}

async function add() {
  if (!content.value.trim() || busy.value) return
  busy.value = true
  error.value = ''
  try {
    const body = {
      enrollmentId: level.value || null,
      title: title.value,
      content: content.value,
      visibleToStudent: visStudent.value,
    }
    if (props.mode === 'admin') body.visibleToPartner = visPartner.value
    await api.post(`${props.apiRoot}/log-notes`, body)
    title.value = ''
    content.value = ''
    visPartner.value = false
    visStudent.value = false
    await load()
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Could not save the note'
  } finally {
    busy.value = false
  }
}

async function widen(n, patch) {
  busy.value = true
  error.value = ''
  try {
    const root = props.mode === 'admin' ? '/v1/admin/log-notes' : '/v1/partner/log-notes'
    const res = await api.post(`${root}/${n.studentLogNoteId}/visibility`, patch)
    n.visibleToPartner = res.data.visibleToPartner
    n.visibleToStudent = res.data.visibleToStudent
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Could not change visibility'
  } finally {
    busy.value = false
  }
}

watch(() => props.apiRoot, load)
onMounted(load)
</script>

<style scoped>
.sln-panel { display: flex; flex-direction: column; gap: .7rem; }
.sln-compose { border: 1px solid #e2e8f0; border-radius: 8px; padding: .7rem .9rem; background: #fafcff; display: flex; flex-direction: column; gap: .5rem; }
.sln-compose-row { display: flex; align-items: center; gap: .7rem; flex-wrap: wrap; font-size: .84rem; }
.sln-compose select, .sln-compose input[type="text"] { padding: .35rem .5rem; border: 1px solid #cbd5e1; border-radius: 5px; font-size: .84rem; background: #fff; }
.sln-compose input[type="text"] { flex: 1; min-width: 180px; }
.sln-compose textarea { padding: .45rem .6rem; border: 1px solid #cbd5e1; border-radius: 5px; font-size: .86rem; resize: vertical; font-family: inherit; }
.sln-btn { background: #0a264f; color: #fff; border: none; border-radius: 5px; padding: .4rem .9rem; font-size: .82rem; font-weight: 600; cursor: pointer; margin-left: auto; }
.sln-btn:disabled { opacity: .5; cursor: not-allowed; }
.sln-btn-sm { background: #fff; color: #0a264f; border: 1px solid #0a264f; border-radius: 5px; padding: .15rem .55rem; font-size: .72rem; font-weight: 600; cursor: pointer; }
.sln-error { background: #fef2f2; border: 1px solid #fecaca; color: #b91c1c; border-radius: 6px; padding: .5rem .8rem; font-size: .83rem; margin: 0; }
.sln-muted { color: #94a3b8; font-size: .84rem; font-style: italic; margin: 0; }
.sln-note { border: 1px solid #e8edf4; border-left: 3px solid #0a264f; border-radius: 6px; padding: .55rem .8rem; background: #fff; }
.sln-note-head { display: flex; align-items: baseline; gap: .5rem; flex-wrap: wrap; }
.sln-note-head strong { color: #0a264f; font-size: .88rem; }
.sln-date { margin-left: auto; color: #94a3b8; font-size: .74rem; }
.sln-content { margin: .3rem 0 .35rem; font-size: .85rem; color: #333; white-space: pre-wrap; }
.sln-vis { display: flex; align-items: center; gap: .4rem; flex-wrap: wrap; }
.sln-chip { border-radius: 10px; padding: 1px 8px; font-size: .7rem; font-weight: 600; }
.sln-chip-level { background: #eef2ff; color: #3730a3; }
.sln-chip-adm { background: #e8f0f8; color: #003366; }
.sln-chip-par { background: #fdf4e7; color: #92500e; }
.sln-chip-on { background: #ecfdf5; color: #047857; }
.sln-chip-off { background: #f1f5f9; color: #94a3b8; }
</style>
