<template>
  <div>
    <div class="ft-head">
      <div>
        <div class="manage-section-title">Faculties</div>
        <p class="ft-sub">This partner's teachers. A teacher is a partner user with the Teacher role; their profile
          is shaped by System Config → Faculty Profile Information (Faculty ID and Name generate on save).</p>
      </div>
      <button type="button" class="btn-primary-sm" @click="openAdd">+ Add teacher</button>
    </div>

    <div v-if="error" class="err-banner">{{ error }}</div>
    <div v-if="loading" class="loading-row">Loading…</div>

    <table v-else-if="items.length" class="data-table" style="margin-bottom:.75rem">
      <thead><tr><th>Teacher</th><th>Login</th><th>Email</th><th>Updated</th><th style="width:180px">Actions</th></tr></thead>
      <tbody>
        <tr v-for="t in items" :key="t.teacherId" class="data-row">
          <td class="ft-name">{{ t.displayName }}</td>
          <td>{{ t.username || '—' }}<span v-if="t.isEnabled === false" class="ft-off"> (disabled)</span></td>
          <td>{{ t.email || '—' }}</td>
          <td>{{ fmtDate(t.updatedAt) }}</td>
          <td class="actions-cell">
            <button type="button" class="btn-sm" @click="openProfile(t)">✎ Profile</button>
            <button type="button" class="btn-sm btn-danger" @click="removeTeacher(t)">✕</button>
          </td>
        </tr>
      </tbody>
    </table>
    <p v-else-if="!loading" class="ft-sub" style="margin:.5rem 0;">No teachers yet — click “+ Add teacher”.</p>

    <!-- Add teacher: login account + teacher record -->
    <div v-if="addOpen" class="ft-backdrop" @click.self="addOpen = false">
      <div class="ft-dialog" style="width:min(480px,100%)">
        <div class="ft-dialog-head"><h3>Add teacher</h3>
          <button type="button" class="ft-x" @click="addOpen = false">✕</button></div>
        <div class="ft-dialog-body">
          <label class="ft-lbl">Teacher's name *</label>
          <input v-model="addName" class="ft-inp" placeholder="e.g. Nguyen Van Anh" />
          <p class="ft-sub" style="margin:.7rem 0 .3rem">Portal login is OPTIONAL — leave the username empty to add the teacher without any user account.</p>
          <label class="ft-lbl">Login username (optional)</label>
          <input v-model="addUsername" class="ft-inp" placeholder="leave empty = no login" />
          <template v-if="addUsername.trim()">
            <label class="ft-lbl" style="margin-top:.6rem">Email</label>
            <input v-model="addEmail" class="ft-inp" type="email" placeholder="teacher@partner.edu" />
            <label class="ft-lbl" style="margin-top:.6rem">Password (leave blank to auto-generate)</label>
            <input v-model="addPassword" class="ft-inp" type="text" />
          </template>
          <div v-if="addError" class="err-banner" style="margin-top:.6rem">{{ addError }}</div>
        </div>
        <div class="ft-dialog-foot">
          <button type="button" class="btn-sm" @click="addOpen = false">Cancel</button>
          <button type="button" class="btn-primary-sm" :disabled="adding || !addName.trim()" @click="addTeacher">
            {{ adding ? 'Creating…' : 'Create teacher' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Profile fill dialog -->
    <div v-if="profOpen" class="ft-backdrop" @click.self="profOpen = false">
      <div class="ft-dialog">
        <div class="ft-dialog-head">
          <h3>{{ prof.displayName }}<span class="ft-muted"> — faculty profile</span></h3>
          <button type="button" class="ft-x" @click="profOpen = false">✕</button>
        </div>
        <div class="ft-dialog-body">
          <label class="ft-lbl">Teacher's name</label>
          <input v-model="prof.displayName" class="ft-inp" style="max-width:340px" />

          <div v-for="sec in prof.sections" :key="sec.id" class="ft-section">
            <div class="ft-section-title">{{ sec.title }}</div>

            <template v-if="sec.kind === 'fields'">
              <div v-for="f in sec.fields" :key="f.id" class="ft-field">
                <label class="ft-lbl">{{ f.label }}<span v-if="f.isRequired" class="ft-req">*</span></label>
                <div v-if="f.type === 'autoid' || f.type === 'computed'" class="ft-system">
                  {{ cell(fieldsRow(sec), f.id).value || '— generated on save —' }}
                </div>
                <input v-else-if="f.type === 'text'" v-model="cell(fieldsRow(sec), f.id).value" class="ft-inp" />
                <input v-else-if="f.type === 'number'" v-model="cell(fieldsRow(sec), f.id).value" type="number" class="ft-inp" style="max-width:200px" />
                <input v-else-if="f.type === 'date'" v-model="cell(fieldsRow(sec), f.id).value" type="date" class="ft-inp" style="max-width:200px" />
                <select v-else-if="f.type === 'select'" v-model="cell(fieldsRow(sec), f.id).value" class="ft-inp" style="max-width:280px">
                  <option value="">—</option>
                  <option v-for="o in f.options" :key="o" :value="o">{{ o }}</option>
                </select>
                <label v-else-if="f.type === 'bool'" class="ft-check">
                  <input type="checkbox" :checked="cell(fieldsRow(sec), f.id).value === 'true'"
                         @change="cell(fieldsRow(sec), f.id).value = $event.target.checked ? 'true' : ''" /> Yes
                </label>
                <div v-else-if="f.type === 'file'" class="ft-file">
                  <template v-if="cell(fieldsRow(sec), f.id).value">
                    <span class="ft-file-ok">✓ {{ cell(fieldsRow(sec), f.id).fileName || 'file' }}</span>
                    <button v-if="cell(fieldsRow(sec), f.id).valueId" type="button" class="btn-sm" @click="downloadCell(cell(fieldsRow(sec), f.id))">⤓</button>
                    <button type="button" class="btn-sm btn-danger" @click="clearCell(fieldsRow(sec), f.id)">✕</button>
                  </template>
                  <input v-else type="file" @change="uploadCell(fieldsRow(sec), f.id, $event)" />
                </div>
              </div>
            </template>

            <template v-else>
              <table class="data-table ft-grid">
                <thead><tr>
                  <th v-for="f in sec.fields" :key="f.id">{{ f.label }}<span v-if="f.isRequired" class="ft-req">*</span></th>
                  <th style="width:40px"></th>
                </tr></thead>
                <tbody>
                  <tr v-for="(row, ri) in gridRows(sec)" :key="ri">
                    <td v-for="f in sec.fields" :key="f.id">
                      <div v-if="f.type === 'autoid' || f.type === 'computed'" class="ft-system">{{ cell(row, f.id).value || '—' }}</div>
                      <input v-else-if="f.type === 'text'" v-model="cell(row, f.id).value" class="ft-inp" />
                      <input v-else-if="f.type === 'number'" v-model="cell(row, f.id).value" type="number" class="ft-inp" />
                      <input v-else-if="f.type === 'date'" v-model="cell(row, f.id).value" type="date" class="ft-inp" />
                      <select v-else-if="f.type === 'select'" v-model="cell(row, f.id).value" class="ft-inp">
                        <option value="">—</option>
                        <option v-for="o in f.options" :key="o" :value="o">{{ o }}</option>
                      </select>
                      <label v-else-if="f.type === 'bool'" class="ft-check">
                        <input type="checkbox" :checked="cell(row, f.id).value === 'true'"
                               @change="cell(row, f.id).value = $event.target.checked ? 'true' : ''" />
                      </label>
                      <div v-else-if="f.type === 'file'" class="ft-file">
                        <template v-if="cell(row, f.id).value">
                          <span class="ft-file-ok">✓ {{ cell(row, f.id).fileName || 'file' }}</span>
                          <button v-if="cell(row, f.id).valueId" type="button" class="btn-sm" @click="downloadCell(cell(row, f.id))">⤓</button>
                          <button type="button" class="btn-sm btn-danger" @click="clearCell(row, f.id)">✕</button>
                        </template>
                        <input v-else type="file" @change="uploadCell(row, f.id, $event)" />
                      </div>
                    </td>
                    <td><button type="button" class="btn-sm btn-danger" @click="removeGridRow(sec, row)">✕</button></td>
                  </tr>
                </tbody>
              </table>
              <button type="button" class="btn-sm" @click="addGridRow(sec)">+ Add row</button>
            </template>
          </div>

          <div v-if="profError" class="err-banner" style="margin-top:.6rem">{{ profError }}</div>
        </div>
        <div class="ft-dialog-foot">
          <button type="button" class="btn-sm" @click="profOpen = false">Cancel</button>
          <button type="button" class="btn-primary-sm" :disabled="savingProf || !!uploadingCell" @click="saveProfile">
            {{ savingProf ? 'Saving…' : 'Save' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, watch } from 'vue'
import api from '../../api/client.js'

const props = defineProps({
  partnerId: { type: String, required: true },
  partnerName: { type: String, default: '' },
})

const items = ref([])
const loading = ref(false)
const error = ref('')

const addOpen = ref(false)
const addName = ref('')
const addUsername = ref('')
const addEmail = ref('')
const addPassword = ref('')
const adding = ref(false)
const addError = ref('')

const profOpen = ref(false)
const prof = ref({ teacherId: '', displayName: '', sections: [], rows: [] })
const profError = ref('')
const savingProf = ref(false)
const uploadingCell = ref(false)

function fmtDate(d) {
  return d ? new Date(d).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }) : '—'
}

async function load() {
  if (!props.partnerId) return
  loading.value = true
  error.value = ''
  try {
    const res = await api.get(`/v1/admin/partners/${props.partnerId}/teachers`)
    items.value = res.data.items ?? []
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to load teachers'
  } finally {
    loading.value = false
  }
}

function openAdd() {
  addName.value = ''
  addUsername.value = ''
  addEmail.value = ''
  addPassword.value = ''
  addError.value = ''
  addOpen.value = true
}

// Creates the teacher record; a login user (Teacher role) is created ONLY
// when a username was typed — a teacher without any account is fine.
async function addTeacher() {
  if (adding.value || !addName.value.trim()) return
  adding.value = true
  addError.value = ''
  try {
    let teacherUserId = null
    let tempPassword = null
    if (addUsername.value.trim()) {
      const ures = await api.post(`/v1/admin/school/partners/${props.partnerId}/users`, {
        username: addUsername.value.trim(),
        email: addEmail.value.trim() || undefined,
        password: addPassword.value.trim() || undefined,
        isTeacher: true,
      })
      tempPassword = ures.data?.temporaryPassword ?? null
      // Resolve the freshly created user's id from the partner user list.
      const ulist = await api.get(`/v1/admin/school/partners/${props.partnerId}/users`)
      const created = (ulist.data.items ?? []).find(u =>
        (u.username || '').toLowerCase() === addUsername.value.trim().toLowerCase())
      if (!created) throw new Error('Login was created but could not be found afterwards.')
      teacherUserId = created.userId
    }

    const tres = await api.post(`/v1/admin/partners/${props.partnerId}/teachers`, {
      teacherUserId,
      displayName: addName.value.trim(),
    })
    if (tempPassword)
      alert(`Teacher login created.\nUsername: ${addUsername.value.trim()}\nTemporary password: ${tempPassword}\n\nCopy it now — it is not shown again.`)
    addOpen.value = false
    await load()
    const t = items.value.find(x => x.teacherId === tres.data.teacherId)
    if (t) await openProfile(t)
  } catch (e) {
    addError.value = e.response?.data?.error
      ?? (typeof e.response?.data === 'string' ? e.response.data : null)
      ?? e.message ?? 'Failed to create teacher'
  } finally {
    adding.value = false
  }
}

async function removeTeacher(t) {
  if (!confirm(`Remove the teacher "${t.displayName}"? The login stays and can be removed under Users.`)) return
  error.value = ''
  try {
    await api.delete(`/v1/admin/teachers/${t.teacherId}`)
    await load()
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Remove failed'
  }
}

async function openProfile(t) {
  profError.value = ''
  try {
    const res = await api.get(`/v1/admin/teachers/${t.teacherId}`)
    prof.value = {
      teacherId: res.data.teacherId,
      displayName: res.data.displayName ?? '',
      sections: res.data.sections ?? [],
      rows: (res.data.rows ?? []).map(r => ({
        id: r.id,
        sectionId: r.sectionId,
        values: Object.fromEntries(Object.entries(r.values ?? {}).map(([fid, c]) =>
          [fid, { value: c.value ?? '', fileName: c.fileName ?? '', valueId: c.valueId ?? '' }])),
      })),
    }
    profOpen.value = true
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to open profile'
  }
}

function fieldsRow(sec) {
  let row = prof.value.rows.find(r => r.sectionId === sec.id)
  if (!row) {
    row = { id: null, sectionId: sec.id, values: {} }
    prof.value.rows.push(row)
  }
  return row
}
function gridRows(sec) {
  return prof.value.rows.filter(r => r.sectionId === sec.id)
}
function addGridRow(sec) {
  prof.value.rows.push({ id: null, sectionId: sec.id, values: {} })
}
function removeGridRow(sec, row) {
  const i = prof.value.rows.indexOf(row)
  if (i >= 0) prof.value.rows.splice(i, 1)
}
function cell(row, fieldId) {
  if (!row.values[fieldId]) row.values[fieldId] = { value: '', fileName: '', valueId: '' }
  return row.values[fieldId]
}
function clearCell(row, fieldId) {
  row.values[fieldId] = { value: '', fileName: '', valueId: '' }
}

async function uploadCell(row, fieldId, ev) {
  const file = ev.target.files?.[0]
  if (!file) return
  uploadingCell.value = true
  profError.value = ''
  try {
    const fd = new FormData()
    fd.append('file', file)
    const res = await api.post('/v1/admin/faculty-files', fd, { headers: { 'Content-Type': 'multipart/form-data' } })
    row.values[fieldId] = { value: res.data.token, fileName: res.data.fileName, valueId: '' }
  } catch (e) {
    profError.value = e.response?.data?.error ?? e.message ?? 'Upload failed'
  } finally {
    uploadingCell.value = false
    ev.target.value = ''
  }
}

async function downloadCell(c) {
  try {
    const res = await api.get(`/v1/admin/faculty-values/${c.valueId}/file`, { responseType: 'blob' })
    const url = URL.createObjectURL(res.data)
    const a = document.createElement('a')
    a.href = url
    a.download = c.fileName || 'faculty-file'
    a.click()
    setTimeout(() => URL.revokeObjectURL(url), 60_000)
  } catch {
    profError.value = 'Download failed'
  }
}

async function saveProfile() {
  if (savingProf.value) return
  savingProf.value = true
  profError.value = ''
  try {
    await api.put(`/v1/admin/teachers/${prof.value.teacherId}`, {
      displayName: prof.value.displayName.trim() || null,
      rows: prof.value.rows.map(r => ({
        id: r.id,
        sectionId: r.sectionId,
        values: Object.fromEntries(Object.entries(r.values).map(([fid, c]) =>
          [fid, { value: c.value, fileName: c.fileName || null }])),
      })),
    })
    profOpen.value = false
    await load()
  } catch (e) {
    profError.value = e.response?.data?.error ?? e.message ?? 'Save failed'
  } finally {
    savingProf.value = false
  }
}

watch(() => props.partnerId, load, { immediate: true })
</script>

<style scoped>
/* AdminView's styles are scoped, so this tab carries its own copies. */
.ft-head { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; margin-bottom: .4rem; }
.ft-sub { font-size: .78rem; color: #6b7888; margin: .15rem 0 .5rem; }
.ft-name { font-weight: 600; color: #1a2d4f; }
.ft-muted { color: #8a97a8; font-weight: 400; }
.ft-off { color: #b3261e; font-size: .75rem; }
.manage-section-title { font-size: .95rem; font-weight: 700; color: #003366; }
.data-table { width: 100%; border-collapse: collapse; font-size: .85rem; }
.data-table th { text-align: left; padding: .45rem .6rem; color: #6b7888; font-size: .75rem; text-transform: uppercase; letter-spacing: .03em; border-bottom: 1.5px solid #e8edf4; }
.data-table td { padding: .45rem .6rem; border-bottom: 1px solid #eef1f5; vertical-align: middle; }
.actions-cell { display: flex; gap: .35rem; flex-wrap: wrap; }
.btn-sm { background: #f2f5f9; border: 1px solid #cfd7e3; border-radius: 5px; padding: .28rem .6rem; font-size: .78rem; font-weight: 600; color: #2c3e50; cursor: pointer; }
.btn-sm:hover { background: #e8eef6; }
.btn-danger { color: #b3261e; border-color: #e2b8b5; background: #fdf3f2; }
.btn-primary-sm { background: #003366; color: #fff; border: none; border-radius: 5px; padding: .35rem .8rem; font-size: .8rem; font-weight: 600; cursor: pointer; }
.btn-primary-sm:disabled { opacity: .5; cursor: default; }
.err-banner { background: #fdf3f2; border: 1px solid #e2b8b5; color: #b3261e; padding: .45rem .7rem; border-radius: 6px; font-size: .8rem; margin-bottom: .5rem; }
.loading-row { color: #6b7888; font-size: .85rem; padding: .5rem 0; }
.ft-backdrop { position: fixed; inset: 0; background: rgba(20,30,50,.55); z-index: 1200; display: flex; align-items: center; justify-content: center; padding: 1rem; }
.ft-dialog { background: #fff; border-radius: 8px; width: min(860px, 100%); max-height: 92vh; display: flex; flex-direction: column; box-shadow: 0 10px 40px rgba(0,0,0,.2); }
.ft-dialog-head { display: flex; justify-content: space-between; align-items: center; padding: .85rem 1.1rem; border-bottom: 1px solid #e6ebf2; }
.ft-dialog-head h3 { margin: 0; font-size: 1rem; color: #1a2d4f; }
.ft-x { background: none; border: none; font-size: 1rem; cursor: pointer; color: #6b7888; }
.ft-dialog-body { padding: 1rem 1.1rem; overflow-y: auto; }
.ft-dialog-foot { display: flex; justify-content: flex-end; gap: .5rem; padding: .75rem 1.1rem; border-top: 1px solid #e6ebf2; }
.ft-lbl { display: block; font-size: .75rem; font-weight: 700; color: #44536a; margin-bottom: .25rem; }
.ft-inp { padding: .4rem .55rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .85rem; width: 100%; background: #fff; }
.ft-section { border: 1px solid #e2e8f0; border-radius: 7px; padding: .7rem .8rem; margin-top: .85rem; background: #fafbfd; }
.ft-section-title { font-weight: 700; color: #003366; font-size: .85rem; margin-bottom: .5rem; }
.ft-field { margin-bottom: .55rem; }
.ft-req { color: #b3261e; margin-left: .15rem; }
.ft-check { display: flex; align-items: center; gap: .35rem; font-size: .82rem; color: #2c3e50; }
.ft-file { display: flex; align-items: center; gap: .45rem; font-size: .8rem; flex-wrap: wrap; }
.ft-file-ok { color: #1c7a4a; font-weight: 600; }
.ft-grid { margin-bottom: .45rem; }
.ft-grid td { padding: .3rem .35rem; }
.ft-system { padding: .4rem .55rem; background: #f2f5f9; border: 1px dashed #cfd7e3; border-radius: 5px; font-size: .82rem; color: #44536a; }
</style>
