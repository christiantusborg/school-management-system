<template>
  <div>
    <div class="pd-head">
      <div>
        <div class="manage-section-title">{{ isFaculty ? 'Faculty' : 'Datasheets' }}</div>
        <p v-if="isFaculty" class="pd-sub">This partner's teachers. Each teacher gets a profile based on the
          structure defined in System Config → Faculty Profile Information (auto Faculty ID and combined
          name are generated on save). Partners fill their part from the portal; 🔒 fields are MGW-only.</p>
        <p v-else class="pd-sub">Extra structured data on this partner. Templates are built in
          System Config → Partner Datasheets; the same template can be attached any number of times
          (each sheet with its own title).</p>
      </div>
      <button type="button" class="btn-primary-sm" @click="openAddDialog">{{ isFaculty ? '+ Add teacher' : '+ Add datasheet' }}</button>
    </div>

    <div v-if="error" class="err-banner">{{ error }}</div>
    <div v-if="loading" class="loading-row">Loading…</div>

    <table v-else-if="items.length" class="data-table" style="margin-bottom:.75rem">
      <thead><tr><th>{{ isFaculty ? 'Faculty profile' : 'Datasheet' }}</th><th>{{ isFaculty ? 'Teacher' : 'Title' }}</th><th>Updated</th><th style="width:200px">Actions</th></tr></thead>
      <tbody>
        <tr v-for="s in displayRows" :key="s.partnerDatasheetId" class="data-row">
          <td class="pd-name">
            <span v-if="s.kind === 'item'" class="pd-indent">└</span>
            <span v-if="s.kind === 'group'">📁 </span>{{ s.definitionName }}
            <span v-if="s.kind === 'group'" class="pd-count">({{ childrenOf(s.partnerDatasheetId).length }})</span>
          </td>
          <td>{{ s.title || '—' }}</td>
          <td>{{ fmtDate(s.updatedAt) }}</td>
          <td class="actions-cell">
            <button v-if="s.kind !== 'group'" type="button" class="btn-sm" @click="openSheet(s)">✎ Open</button>
            <button v-if="s.kind === 'group'" type="button" class="btn-sm" @click="openDialogForItem(s)">+ Item</button>
            <button v-if="s.kind === 'group'" type="button" class="btn-sm" :title="s.partnerCanAddItems ? 'Partner may add items — click to lock' : 'Partner cannot add items — click to allow'"
                    @click="togglePartnerAdd(s)">
              {{ s.partnerCanAddItems ? '🔓 partner' : '🔒 partner' }}
            </button>
            <button type="button" class="btn-sm btn-danger" @click="removeSheet(s)">✕</button>
          </td>
        </tr>
      </tbody>
    </table>
    <p v-else-if="!loading" class="pd-sub" style="margin:.5rem 0;">
      {{ isFaculty ? 'No teachers yet — click “+ Add teacher”.' : 'No datasheets yet — click “+ Add datasheet”.' }}</p>

    <!-- Add dialog -->
    <div v-if="addOpen" class="pd-backdrop" @click.self="addOpen = false">
      <div class="pd-dialog" style="width:min(440px,100%)">
        <div class="pd-dialog-head"><h3>{{ isFaculty ? 'Add teacher' : 'Add datasheet' }}</h3>
          <button type="button" class="pd-x" @click="addOpen = false">✕</button></div>
        <div class="pd-dialog-body">
          <template v-if="!addParent && !isFaculty">
            <label class="pd-lbl">Datasheet template</label>
            <select v-model="addDefId" class="pd-inp">
              <option value="">— pick a template —</option>
              <option v-for="d in definitions" :key="d.partnerDatasheetDefinitionId" :value="d.partnerDatasheetDefinitionId">{{ d.name }}</option>
            </select>
            <label class="pd-lbl" style="margin-top:.6rem">Create as</label>
            <select v-model="addKind" class="pd-inp">
              <option value="standalone">Single sheet</option>
              <option value="group">Group — a folder holding many sheets of this template (e.g. one per teacher)</option>
            </select>
            <label v-if="addKind === 'group'" class="pd-check" style="margin-top:.5rem">
              <input type="checkbox" v-model="addPartnerCanAdd" />
              Partner may add items to this group from the partner portal
            </label>
          </template>
          <div v-else-if="addParent" class="pd-sub" style="margin-bottom:.4rem">
            New <strong>{{ addParent.definitionName }}</strong> item in the group “{{ addParent.title || addParent.definitionName }}”.
          </div>
          <label class="pd-lbl" style="margin-top:.6rem">{{ isFaculty ? "Teacher's name" : 'Title (optional)' }}</label>
          <input v-model="addTitle" class="pd-inp" :placeholder="isFaculty ? 'e.g. Nguyen Van Anh' : 'e.g. Visit 17 Jul 2026'" />

          <template v-if="isFaculty && !addParent">
            <label class="pd-check" style="margin-top:.8rem">
              <input type="checkbox" v-model="addCreateLogin" />
              Also create a portal login for this teacher (partner user with the Teacher role)
            </label>
            <template v-if="addCreateLogin">
              <label class="pd-lbl" style="margin-top:.5rem">Login username *</label>
              <input v-model="addLoginUsername" class="pd-inp" placeholder="e.g. nguyen.van.anh" />
              <label class="pd-lbl" style="margin-top:.5rem">Login email</label>
              <input v-model="addLoginEmail" class="pd-inp" type="email" placeholder="teacher@partner.edu" />
              <label class="pd-lbl" style="margin-top:.5rem">Password (leave blank to auto-generate)</label>
              <input v-model="addLoginPassword" class="pd-inp" type="text" />
            </template>
          </template>
          <div v-if="addError" class="err-banner" style="margin-top:.5rem">{{ addError }}</div>
        </div>
        <div class="pd-dialog-foot">
          <button type="button" class="btn-sm" @click="addOpen = false">Cancel</button>
          <button type="button" class="btn-primary-sm"
                  :disabled="adding || (!addDefId && !addParent && !isFaculty) || (isFaculty && addCreateLogin && !addLoginUsername.trim())"
                  @click="addSheet">
            {{ adding ? 'Adding…' : 'Add' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Fill dialog -->
    <div v-if="sheetOpen" class="pd-backdrop" @click.self="sheetOpen = false">
      <div class="pd-dialog">
        <div class="pd-dialog-head">
          <h3>{{ sheet.definitionName }}<span v-if="sheet.title" class="pd-muted"> — {{ sheet.title }}</span></h3>
          <button type="button" class="pd-x" @click="sheetOpen = false">✕</button>
        </div>
        <div class="pd-dialog-body">
          <label class="pd-lbl">Sheet title</label>
          <input v-model="sheet.title" class="pd-inp" placeholder="Optional title for this sheet" style="max-width:340px" />

          <div v-for="sec in sheet.sections" :key="sec.id" class="pd-section">
            <div class="pd-section-title">{{ sec.title }}</div>

            <!-- Fields section: one row, label/input pairs -->
            <template v-if="sec.kind === 'fields'">
              <div v-for="f in sec.fields" :key="f.id" class="pd-field">
                <label class="pd-lbl">{{ f.label }}<span v-if="f.isRequired" class="pd-req">*</span></label>
                <component :is="'div'">
                  <div v-if="f.type === 'autoid' || f.type === 'computed'" class="pd-system">
                    {{ cell(fieldsRow(sec), f.id).value || '— generated on save —' }}
                  </div>
                  <input v-else-if="f.type === 'text'" v-model="cell(fieldsRow(sec), f.id).value" class="pd-inp" />
                  <input v-else-if="f.type === 'number'" v-model="cell(fieldsRow(sec), f.id).value" type="number" class="pd-inp" style="max-width:200px" />
                  <input v-else-if="f.type === 'date'" v-model="cell(fieldsRow(sec), f.id).value" type="date" class="pd-inp" style="max-width:200px" />
                  <select v-else-if="f.type === 'select'" v-model="cell(fieldsRow(sec), f.id).value" class="pd-inp" style="max-width:280px">
                    <option value="">—</option>
                    <option v-for="o in f.options" :key="o" :value="o">{{ o }}</option>
                  </select>
                  <label v-else-if="f.type === 'bool'" class="pd-check">
                    <input type="checkbox" :checked="cell(fieldsRow(sec), f.id).value === 'true'"
                           @change="cell(fieldsRow(sec), f.id).value = $event.target.checked ? 'true' : ''" /> Yes
                  </label>
                  <div v-else-if="f.type === 'file' || f.type === 'files'" class="pd-files">
                    <div v-for="(fi, i) in cellFiles(cell(fieldsRow(sec), f.id))" :key="i" class="pd-file">
                      <span class="pd-file-ok">✓ {{ fi.name || 'file' }}</span>
                      <button v-if="cell(fieldsRow(sec), f.id).valueId" type="button" class="btn-sm" @click="downloadFilesItem(cell(fieldsRow(sec), f.id), i, fi)">⤓</button>
                      <span v-else class="pd-muted">(save to download)</span>
                      <button type="button" class="btn-sm btn-danger" @click="removeFilesItem(fieldsRow(sec), f.id, i)">✕</button>
                    </div>
                    <input type="file" multiple @change="uploadFilesCell(fieldsRow(sec), f.id, $event)" />
                  </div>
                </component>
              </div>
            </template>

            <!-- Grid section: table with columns + add/remove rows -->
            <template v-else>
              <table class="data-table pd-grid">
                <thead><tr>
                  <th v-for="f in sec.fields" :key="f.id">{{ f.label }}<span v-if="f.isRequired" class="pd-req">*</span></th>
                  <th style="width:40px"></th>
                </tr></thead>
                <tbody>
                  <tr v-for="(row, ri) in gridRows(sec)" :key="ri">
                    <td v-for="f in sec.fields" :key="f.id">
                      <div v-if="f.type === 'autoid' || f.type === 'computed'" class="pd-system">
                        {{ cell(row, f.id).value || '—' }}
                      </div>
                      <input v-else-if="f.type === 'text'" v-model="cell(row, f.id).value" class="pd-inp" />
                      <input v-else-if="f.type === 'number'" v-model="cell(row, f.id).value" type="number" class="pd-inp" />
                      <input v-else-if="f.type === 'date'" v-model="cell(row, f.id).value" type="date" class="pd-inp" />
                      <select v-else-if="f.type === 'select'" v-model="cell(row, f.id).value" class="pd-inp">
                        <option value="">—</option>
                        <option v-for="o in f.options" :key="o" :value="o">{{ o }}</option>
                      </select>
                      <label v-else-if="f.type === 'bool'" class="pd-check">
                        <input type="checkbox" :checked="cell(row, f.id).value === 'true'"
                               @change="cell(row, f.id).value = $event.target.checked ? 'true' : ''" />
                      </label>
                      <div v-else-if="f.type === 'file' || f.type === 'files'" class="pd-files">
                        <div v-for="(fi, i) in cellFiles(cell(row, f.id))" :key="i" class="pd-file">
                          <span class="pd-file-ok">✓ {{ fi.name || 'file' }}</span>
                          <button v-if="cell(row, f.id).valueId" type="button" class="btn-sm" @click="downloadFilesItem(cell(row, f.id), i, fi)">⤓</button>
                          <span v-else class="pd-muted">(save to download)</span>
                          <button type="button" class="btn-sm btn-danger" @click="removeFilesItem(row, f.id, i)">✕</button>
                        </div>
                        <input type="file" multiple @change="uploadFilesCell(row, f.id, $event)" />
                      </div>
                    </td>
                    <td><button type="button" class="btn-sm btn-danger" @click="removeGridRow(sec, row)">✕</button></td>
                  </tr>
                </tbody>
              </table>
              <button type="button" class="btn-sm" @click="addGridRow(sec)">+ Add row</button>
            </template>
          </div>

          <div v-if="sheetError" class="err-banner" style="margin-top:.6rem">{{ sheetError }}</div>
        </div>
        <div class="pd-dialog-foot">
          <button type="button" class="btn-sm" @click="sheetOpen = false">Cancel</button>
          <button type="button" class="btn-primary-sm" :disabled="savingSheet || !!uploadingCell" @click="saveSheet">
            {{ savingSheet ? 'Saving…' : 'Save' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import api from '../../api/client.js'

const props = defineProps({
  partnerId: { type: String, required: true },
  partnerName: { type: String, default: '' },
  // 'datasheet' (default) or 'faculty' — same engine, different feature.
  scope: { type: String, default: 'datasheet' },
})
const isFaculty = computed(() => props.scope === 'faculty')

const items = ref([])
const definitions = ref([])
const loading = ref(false)
const error = ref('')

const addOpen = ref(false)
const addDefId = ref('')
const addTitle = ref('')
const addKind = ref('standalone')
const addPartnerCanAdd = ref(false)
const addParent = ref(null)
const adding = ref(false)
const addError = ref('')
// Faculty mode: optionally create the teacher's portal login alongside the profile.
const addCreateLogin = ref(true)
const addLoginUsername = ref('')
const addLoginEmail = ref('')
const addLoginPassword = ref('')

// Tree order: top-level sheets/groups, each group's items right below it.
const displayRows = computed(() => {
  const out = []
  for (const s of items.value.filter(x => !x.parentId)) {
    out.push(s)
    if (s.kind === 'group') out.push(...childrenOf(s.partnerDatasheetId))
  }
  return out
})
function childrenOf(id) {
  return items.value.filter(x => x.parentId === id)
}
function openDialogForItem(group) {
  addParent.value = group
  addDefId.value = ''
  addTitle.value = ''
  addError.value = ''
  addOpen.value = true
}
async function togglePartnerAdd(group) {
  error.value = ''
  try {
    await api.patch(`/v1/admin/partner-datasheets/${group.partnerDatasheetId}`, {
      partnerCanAddItems: !group.partnerCanAddItems,
    })
    await load()
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Update failed'
  }
}

const sheetOpen = ref(false)
const sheet = ref({ partnerDatasheetId: '', title: '', definitionName: '', sections: [], rows: [] })
const sheetError = ref('')
const savingSheet = ref(false)
const uploadingCell = ref(false)

function fmtDate(d) {
  return d ? new Date(d).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }) : '—'
}

async function load() {
  if (!props.partnerId) return
  loading.value = true
  error.value = ''
  try {
    const res = await api.get(`/v1/admin/partners/${props.partnerId}/datasheets`, { params: { scope: props.scope } })
    items.value = res.data.items ?? []
    definitions.value = res.data.definitions ?? []
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to load datasheets'
  } finally {
    loading.value = false
  }
}

function openAddDialog() {
  addParent.value = null
  addDefId.value = ''
  addTitle.value = ''
  addKind.value = 'standalone'
  addPartnerCanAdd.value = false
  addCreateLogin.value = isFaculty.value
  addLoginUsername.value = ''
  addLoginEmail.value = ''
  addLoginPassword.value = ''
  addError.value = ''
  addOpen.value = true
}

async function addSheet() {
  const facultyDefId = isFaculty.value ? definitions.value[0]?.partnerDatasheetDefinitionId : null
  if (adding.value || (!addDefId.value && !addParent.value && !facultyDefId)) return
  adding.value = true
  addError.value = ''
  try {
    const res = await api.post(`/v1/admin/partners/${props.partnerId}/datasheets`, {
      definitionId: addParent.value ? null : (facultyDefId ?? addDefId.value),
      title: addTitle.value.trim() || null,
      kind: addParent.value ? null : addKind.value,
      parentId: addParent.value?.partnerDatasheetId ?? null,
      partnerCanAddItems: addKind.value === 'group' ? addPartnerCanAdd.value : false,
    })
    // Faculty mode: also create the teacher's partner-user login (Teacher role).
    if (isFaculty.value && !addParent.value && addCreateLogin.value && addLoginUsername.value.trim()) {
      const ures = await api.post(`/v1/admin/school/partners/${props.partnerId}/users`, {
        username: addLoginUsername.value.trim(),
        email: addLoginEmail.value.trim() || undefined,
        password: addLoginPassword.value.trim() || undefined,
        isTeacher: true,
      })
      if (ures.data?.temporaryPassword)
        alert(`Teacher login created.\nUsername: ${addLoginUsername.value.trim()}\nTemporary password: ${ures.data.temporaryPassword}\n\nCopy it now — it is not shown again.`)
    }
    const wasGroup = !addParent.value && addKind.value === 'group'
    addOpen.value = false
    addTitle.value = ''
    await load()
    if (!wasGroup) {
      const created = items.value.find(s => s.partnerDatasheetId === res.data.partnerDatasheetId)
      if (created) await openSheet(created)
    }
  } catch (e) {
    addError.value = e.response?.data?.error ?? e.message ?? 'Failed to add datasheet'
  } finally {
    adding.value = false
  }
}

async function removeSheet(s) {
  if (!confirm(`Remove the "${s.definitionName}" sheet${s.title ? ` "${s.title}"` : ''}? Its data will be hidden.`)) return
  error.value = ''
  try {
    await api.delete(`/v1/admin/partner-datasheets/${s.partnerDatasheetId}`)
    await load()
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Remove failed'
  }
}

async function openSheet(s) {
  sheetError.value = ''
  try {
    const res = await api.get(`/v1/admin/partner-datasheets/${s.partnerDatasheetId}`)
    // Local editable model: rows carry {id, sectionId, values: {fieldId: {value, fileName, valueId}}}.
    sheet.value = {
      partnerDatasheetId: res.data.partnerDatasheetId,
      title: res.data.title ?? '',
      definitionName: res.data.definitionName,
      sections: res.data.sections ?? [],
      rows: (res.data.rows ?? []).map(r => ({
        id: r.id,
        sectionId: r.sectionId,
        values: Object.fromEntries(Object.entries(r.values ?? {}).map(([fid, c]) =>
          [fid, { value: c.value ?? '', fileName: c.fileName ?? '', valueId: c.valueId ?? '' }])),
      })),
    }
    sheetOpen.value = true
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to open datasheet'
  }
}

// The single row backing a "fields" section — created on demand.
function fieldsRow(sec) {
  let row = sheet.value.rows.find(r => r.sectionId === sec.id)
  if (!row) {
    row = { id: null, sectionId: sec.id, values: {} }
    sheet.value.rows.push(row)
  }
  return row
}
function gridRows(sec) {
  return sheet.value.rows.filter(r => r.sectionId === sec.id)
}
function addGridRow(sec) {
  sheet.value.rows.push({ id: null, sectionId: sec.id, values: {} })
}
function removeGridRow(sec, row) {
  const i = sheet.value.rows.indexOf(row)
  if (i >= 0) sheet.value.rows.splice(i, 1)
}
function cell(row, fieldId) {
  if (!row.values[fieldId]) row.values[fieldId] = { value: '', fileName: '', valueId: '' }
  return row.values[fieldId]
}
function clearCell(row, fieldId) {
  row.values[fieldId] = { value: '', fileName: '', valueId: '' }
}

// A "files" cell stores a JSON array of { token, name } in its value string.
function filesOf(v) {
  if (!v) return []
  try { const a = JSON.parse(v); return Array.isArray(a) ? a : [] } catch { return [] }
}

// Backward-compatible list for a cell that may hold either the multi-file JSON
// array, or a LEGACY single-file token string (value = raw token, name in fileName).
function cellFiles(cell) {
  const v = cell && cell.value
  if (!v) return []
  try { const a = JSON.parse(v); if (Array.isArray(a)) return a } catch (e) {}
  return [{ token: v, name: (cell.fileName || 'file'), legacy: true }]
}

async function uploadCell(row, fieldId, ev) {
  const file = ev.target.files?.[0]
  if (!file) return
  uploadingCell.value = true
  sheetError.value = ''
  try {
    const fd = new FormData()
    fd.append('file', file)
    const res = await api.post('/v1/admin/partner-datasheet-files', fd, { headers: { 'Content-Type': 'multipart/form-data' } })
    row.values[fieldId] = { value: res.data.token, fileName: res.data.fileName, valueId: '' }
  } catch (e) {
    sheetError.value = e.response?.data?.error ?? e.message ?? 'Upload failed'
  } finally {
    uploadingCell.value = false
    ev.target.value = ''
  }
}

async function downloadCell(c) {
  try {
    const res = await api.get(`/v1/admin/partner-datasheet-values/${c.valueId}/file`, { responseType: 'blob' })
    const url = URL.createObjectURL(res.data)
    const a = document.createElement('a')
    a.href = url
    a.download = c.fileName || 'datasheet-file'
    a.click()
    setTimeout(() => URL.revokeObjectURL(url), 60_000)
  } catch {
    sheetError.value = 'Download failed'
  }
}

async function uploadFilesCell(row, fieldId, ev) {
  const files = Array.from(ev.target.files || [])
  if (!files.length) return
  uploadingCell.value = true
  sheetError.value = ''
  try {
    const c = cell(row, fieldId)
    const uploaded = []
    for (const file of files) {
      const fd = new FormData()
      fd.append('file', file)
      const res = await api.post('/v1/admin/partner-datasheet-files', fd, { headers: { 'Content-Type': 'multipart/form-data' } })
      uploaded.push({ token: res.data.token, name: res.data.fileName })
    }
    const list = [...cellFiles(c).map(f => ({ token: f.token, name: f.name })), ...uploaded]
    row.values[fieldId] = { value: JSON.stringify(list), fileName: '', valueId: c.valueId || '' }
  } catch (e) {
    sheetError.value = e.response?.data?.error ?? e.message ?? 'Upload failed'
  } finally {
    uploadingCell.value = false
    ev.target.value = ''
  }
}

function removeFilesItem(row, fieldId, index) {
  const c = cell(row, fieldId)
  const list = cellFiles(c).map(f => ({ token: f.token, name: f.name }))
  list.splice(index, 1)
  // Empty list clears the cell so the "empty = delete" save logic removes it.
  row.values[fieldId] = { value: list.length ? JSON.stringify(list) : '', fileName: '', valueId: c.valueId || '' }
}

async function downloadFilesItem(c, index, item) {
  if (!c.valueId) return
  try {
    // Legacy single files were stored under the old single-file path (no index).
    const path = item?.legacy
      ? `/v1/admin/partner-datasheet-values/${c.valueId}/file`
      : `/v1/admin/partner-datasheet-values/${c.valueId}/file/${index}`
    const res = await api.get(path, { responseType: 'blob' })
    const url = URL.createObjectURL(res.data)
    const a = document.createElement('a')
    a.href = url
    a.download = item?.name || c.fileName || 'datasheet-file'
    a.click()
    setTimeout(() => URL.revokeObjectURL(url), 60_000)
  } catch {
    sheetError.value = 'Download failed'
  }
}

async function saveSheet() {
  if (savingSheet.value) return
  savingSheet.value = true
  sheetError.value = ''
  try {
    await api.put(`/v1/admin/partner-datasheets/${sheet.value.partnerDatasheetId}`, {
      title: sheet.value.title.trim() || null,
      rows: sheet.value.rows.map(r => ({
        id: r.id,
        sectionId: r.sectionId,
        values: Object.fromEntries(Object.entries(r.values).map(([fid, c]) =>
          [fid, { value: c.value, fileName: c.fileName || null }])),
      })),
    })
    sheetOpen.value = false
    await load()
  } catch (e) {
    sheetError.value = e.response?.data?.error ?? e.message ?? 'Save failed'
  } finally {
    savingSheet.value = false
  }
}

watch(() => props.partnerId, load, { immediate: true })
</script>

<style scoped>
/* AdminView's styles are scoped, so this tab carries its own copies of the
   shared table/button looks. */
.pd-head { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; margin-bottom: .4rem; }
.pd-sub { font-size: .78rem; color: #6b7888; margin: .15rem 0 .5rem; }
.pd-name { font-weight: 600; color: #1a2d4f; }
.pd-muted { color: #8a97a8; font-weight: 400; }
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
.pd-backdrop { position: fixed; inset: 0; background: rgba(20,30,50,.55); z-index: 1200; display: flex; align-items: center; justify-content: center; padding: 1rem; }
.pd-dialog { background: #fff; border-radius: 8px; width: min(860px, 100%); max-height: 92vh; display: flex; flex-direction: column; box-shadow: 0 10px 40px rgba(0,0,0,.2); }
.pd-dialog-head { display: flex; justify-content: space-between; align-items: center; padding: .85rem 1.1rem; border-bottom: 1px solid #e6ebf2; }
.pd-dialog-head h3 { margin: 0; font-size: 1rem; color: #1a2d4f; }
.pd-x { background: none; border: none; font-size: 1rem; cursor: pointer; color: #6b7888; }
.pd-dialog-body { padding: 1rem 1.1rem; overflow-y: auto; }
.pd-dialog-foot { display: flex; justify-content: flex-end; gap: .5rem; padding: .75rem 1.1rem; border-top: 1px solid #e6ebf2; }
.pd-lbl { display: block; font-size: .75rem; font-weight: 700; color: #44536a; margin-bottom: .25rem; }
.pd-inp { padding: .4rem .55rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .85rem; width: 100%; background: #fff; }
.pd-section { border: 1px solid #e2e8f0; border-radius: 7px; padding: .7rem .8rem; margin-top: .85rem; background: #fafbfd; }
.pd-section-title { font-weight: 700; color: #003366; font-size: .85rem; margin-bottom: .5rem; }
.pd-field { margin-bottom: .55rem; }
.pd-req { color: #b3261e; margin-left: .15rem; }
.pd-check { display: flex; align-items: center; gap: .35rem; font-size: .82rem; color: #2c3e50; }
.pd-file { display: flex; align-items: center; gap: .45rem; font-size: .8rem; flex-wrap: wrap; }
.pd-files { display: flex; flex-direction: column; gap: .3rem; }
.pd-file-ok { color: #1c7a4a; font-weight: 600; }
.pd-grid { margin-bottom: .45rem; }
.pd-grid td { padding: .3rem .35rem; }
.pd-system { padding: .4rem .55rem; background: #f2f5f9; border: 1px dashed #cfd7e3; border-radius: 5px; font-size: .82rem; color: #44536a; }
.pd-indent { color: #9aa5b1; margin-right: .35rem; margin-left: .8rem; }
.pd-count { color: #6b7888; font-weight: 400; font-size: .78rem; }
</style>
