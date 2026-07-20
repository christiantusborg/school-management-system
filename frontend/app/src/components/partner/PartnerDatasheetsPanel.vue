<template>
  <div>
    <div class="pp-head">
      <div>
        <h1>{{ isFaculty ? 'Faculties' : 'Datasheets' }}</h1>
        <p v-if="isFaculty" class="pp-sub">Your institution's teachers. Add one profile per teacher and fill in
          their information — 🔒 fields are maintained by the Admission Office. Faculty ID and Name are
          generated automatically on save.</p>
        <p v-else class="pp-sub">Additional records for your institution. Fields marked with a lock are
          maintained by the Admission Office.</p>
      </div>
      <div v-if="!readOnly" class="pp-add">
        <template v-if="!isFaculty">
          <select v-model="addDefId" class="pp-inp" style="min-width:220px">
            <option value="">— pick a datasheet —</option>
            <option v-for="d in editableDefs" :key="d.partnerDatasheetDefinitionId" :value="d.partnerDatasheetDefinitionId">{{ d.name }}</option>
          </select>
          <select v-model="addKind" class="pp-inp" style="min-width:140px">
            <option value="standalone">Single sheet</option>
            <option value="group">Group (folder)</option>
          </select>
        </template>
        <input v-model="addTitle" class="pp-inp" :placeholder="isFaculty ? 'Teacher\'s name' : 'Title, e.g. teacher\'s name'" style="min-width:200px" />
        <button type="button" class="pp-btn-primary" :disabled="adding || (!addDefId && !isFaculty)" @click="addSheet(null)">
          {{ adding ? 'Adding…' : (isFaculty ? '+ Add teacher' : '+ Add') }}
        </button>
      </div>
    </div>

    <div v-if="error" class="pp-err">{{ error }}</div>
    <div v-if="loading" class="pp-loading">Loading…</div>

    <table v-else-if="items.length" class="partner-tbl pp-tbl">
      <thead><tr><th>{{ isFaculty ? 'Faculty profile' : 'Datasheet' }}</th><th>{{ isFaculty ? 'Teacher' : 'Title' }}</th><th>Updated</th><th style="width:110px"></th></tr></thead>
      <tbody>
        <tr v-for="s in displayRows" :key="s.partnerDatasheetId">
          <td>
            <span v-if="s.kind === 'item'" class="pp-indent">└</span>
            <span v-if="s.kind === 'group'">📁 </span>{{ s.definitionName }}
            <span v-if="s.kind === 'group'" class="pp-count">({{ childrenOf(s.partnerDatasheetId).length }})</span>
          </td>
          <td>{{ s.title || '—' }}</td>
          <td>{{ fmtDate(s.updatedAt) }}</td>
          <td>
            <button v-if="s.kind !== 'group'" type="button" class="pp-btn" @click="openSheet(s)">✎ Open</button>
            <button v-else-if="!readOnly && s.partnerCanAddItems" type="button" class="pp-btn" @click="promptAddItem(s)">+ Add item</button>
          </td>
        </tr>
      </tbody>
    </table>
    <p v-else-if="!loading" class="pp-sub">{{ isFaculty ? 'No teachers yet.' : 'No datasheets yet.' }}</p>

    <!-- Fill dialog -->
    <div v-if="sheetOpen" class="pp-backdrop" @click.self="sheetOpen = false">
      <div class="pp-dialog">
        <div class="pp-dialog-head">
          <h3>{{ sheet.definitionName }}<span v-if="sheet.title" class="pp-muted"> — {{ sheet.title }}</span></h3>
          <button type="button" class="pp-x" @click="sheetOpen = false">✕</button>
        </div>
        <div class="pp-dialog-body">
          <template v-if="canEditSheet">
            <label class="pp-lbl">Sheet title</label>
            <input v-model="sheet.title" class="pp-inp" style="max-width:340px" />
          </template>

          <div v-for="sec in sheet.sections" :key="sec.id" class="pp-section">
            <div class="pp-section-title">{{ sec.title }}</div>

            <template v-if="sec.kind === 'fields'">
              <div v-for="f in sec.fields" :key="f.id" class="pp-field">
                <label class="pp-lbl">{{ f.label }}<span v-if="f.isRequired" class="pp-req">*</span>
                  <span v-if="!f.partnerCanEdit" title="Maintained by the Admission Office"> 🔒</span></label>
                <div v-if="!f.partnerCanEdit || f.type === 'autoid' || f.type === 'computed'" class="pp-locked">
                  {{ displayValue(cell(fieldsRow(sec), f.id), f) }}
                </div>
                <input v-else-if="f.type === 'text'" v-model="cell(fieldsRow(sec), f.id).value" class="pp-inp" />
                <input v-else-if="f.type === 'number'" v-model="cell(fieldsRow(sec), f.id).value" type="number" class="pp-inp" style="max-width:200px" />
                <input v-else-if="f.type === 'date'" v-model="cell(fieldsRow(sec), f.id).value" type="date" class="pp-inp" style="max-width:200px" />
                <select v-else-if="f.type === 'select'" v-model="cell(fieldsRow(sec), f.id).value" class="pp-inp" style="max-width:280px">
                  <option value="">—</option>
                  <option v-for="o in f.options" :key="o" :value="o">{{ o }}</option>
                </select>
                <label v-else-if="f.type === 'bool'" class="pp-check">
                  <input type="checkbox" :checked="cell(fieldsRow(sec), f.id).value === 'true'"
                         @change="cell(fieldsRow(sec), f.id).value = $event.target.checked ? 'true' : ''" /> Yes
                </label>
                <div v-else-if="f.type === 'file'" class="pp-file">
                  <template v-if="cell(fieldsRow(sec), f.id).value">
                    <span class="pp-file-ok">✓ {{ cell(fieldsRow(sec), f.id).fileName || 'file' }}</span>
                    <button v-if="cell(fieldsRow(sec), f.id).valueId" type="button" class="pp-btn" @click="downloadCell(cell(fieldsRow(sec), f.id))">⤓</button>
                    <button type="button" class="pp-btn pp-btn-danger" @click="clearCell(fieldsRow(sec), f.id)">✕</button>
                  </template>
                  <input v-else type="file" @change="uploadCell(fieldsRow(sec), f.id, $event)" />
                </div>
              </div>
            </template>

            <template v-else>
              <table class="partner-tbl pp-grid">
                <thead><tr>
                  <th v-for="f in sec.fields" :key="f.id">{{ f.label }}<span v-if="!f.partnerCanEdit"> 🔒</span></th>
                </tr></thead>
                <tbody>
                  <tr v-for="(row, ri) in gridRows(sec)" :key="ri">
                    <td v-for="f in sec.fields" :key="f.id">
                      <div v-if="!f.partnerCanEdit || f.type === 'autoid' || f.type === 'computed'" class="pp-locked">
                        {{ displayValue(cell(row, f.id), f) }}
                      </div>
                      <input v-else-if="f.type === 'text'" v-model="cell(row, f.id).value" class="pp-inp" />
                      <input v-else-if="f.type === 'number'" v-model="cell(row, f.id).value" type="number" class="pp-inp" />
                      <input v-else-if="f.type === 'date'" v-model="cell(row, f.id).value" type="date" class="pp-inp" />
                      <select v-else-if="f.type === 'select'" v-model="cell(row, f.id).value" class="pp-inp">
                        <option value="">—</option>
                        <option v-for="o in f.options" :key="o" :value="o">{{ o }}</option>
                      </select>
                      <label v-else-if="f.type === 'bool'" class="pp-check">
                        <input type="checkbox" :checked="cell(row, f.id).value === 'true'"
                               @change="cell(row, f.id).value = $event.target.checked ? 'true' : ''" />
                      </label>
                      <div v-else-if="f.type === 'file'" class="pp-file">
                        <template v-if="cell(row, f.id).value">
                          <span class="pp-file-ok">✓ {{ cell(row, f.id).fileName || 'file' }}</span>
                          <button v-if="cell(row, f.id).valueId" type="button" class="pp-btn" @click="downloadCell(cell(row, f.id))">⤓</button>
                        </template>
                        <input v-else type="file" @change="uploadCell(row, f.id, $event)" />
                      </div>
                    </td>
                  </tr>
                </tbody>
              </table>
              <button v-if="canEditSheet && sec.fields.some(f => f.partnerCanEdit)" type="button" class="pp-btn" @click="addGridRow(sec)">+ Add row</button>
            </template>
          </div>

          <div v-if="sheetError" class="pp-err" style="margin-top:.6rem">{{ sheetError }}</div>
        </div>
        <div class="pp-dialog-foot">
          <button type="button" class="pp-btn" @click="sheetOpen = false">Close</button>
          <button v-if="canEditSheet" type="button" class="pp-btn-primary" :disabled="savingSheet || !!uploadingCell" @click="saveSheet">
            {{ savingSheet ? 'Saving…' : 'Save' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import apiClient from '../../api/client.js'
import { auth } from '../../store/auth.js'

const props = defineProps({
  // 'datasheet' (default) or 'faculty' — same engine, different feature.
  scope: { type: String, default: 'datasheet' },
})
const isFaculty = computed(() => props.scope === 'faculty')

const items = ref([])
const definitions = ref([])
const loading = ref(false)
const error = ref('')

const addDefId = ref('')
const addTitle = ref('')
const addKind = ref('standalone')
const adding = ref(false)

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
function promptAddItem(group) {
  const title = prompt(`Title for the new ${group.definitionName} entry (e.g. the teacher's name):`)
  if (title === null) return
  addSheet(group, title)
}

const sheetOpen = ref(false)
const sheet = ref({ partnerDatasheetId: '', title: '', definitionName: '', partnerAccess: 'view', sections: [], rows: [] })
const sheetError = ref('')
const savingSheet = ref(false)
const uploadingCell = ref(false)

// Teacher partner-users are read-only everywhere.
const readOnly = computed(() => !!auth.user?.isTeacher)
const editableDefs = computed(() => definitions.value.filter(d => d.partnerAccess === 'edit'))
const canEditSheet = computed(() => !readOnly.value && sheet.value.partnerAccess === 'edit')

function fmtDate(d) {
  return d ? new Date(d).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }) : '—'
}
function displayValue(c, f) {
  if (f.type === 'bool') return c.value === 'true' ? 'Yes' : 'No'
  if (f.type === 'file') return c.fileName || (c.value ? 'file' : '—')
  return c.value || '—'
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    const res = await apiClient.get('/v1/partner/my/datasheets', { params: { scope: props.scope } })
    items.value = res.data.items ?? []
    definitions.value = res.data.definitions ?? []
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to load datasheets'
  } finally {
    loading.value = false
  }
}

async function addSheet(parent, itemTitle) {
  const facultyDefId = isFaculty.value ? definitions.value[0]?.partnerDatasheetDefinitionId : null
  if (adding.value || (!parent && !addDefId.value && !facultyDefId)) return
  adding.value = true
  error.value = ''
  try {
    const res = await apiClient.post('/v1/partner/my/datasheets', {
      definitionId: parent ? null : (facultyDefId ?? addDefId.value),
      title: (parent ? (itemTitle ?? '') : addTitle.value).trim() || null,
      kind: parent ? null : addKind.value,
      parentId: parent?.partnerDatasheetId ?? null,
    })
    const wasGroup = !parent && addKind.value === 'group'
    addDefId.value = ''
    addTitle.value = ''
    addKind.value = 'standalone'
    await load()
    if (!wasGroup) {
      const created = items.value.find(s => s.partnerDatasheetId === res.data.partnerDatasheetId)
      if (created) await openSheet(created)
    }
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to add datasheet'
  } finally {
    adding.value = false
  }
}

async function openSheet(s) {
  sheetError.value = ''
  try {
    const res = await apiClient.get(`/v1/partner/my/datasheets/${s.partnerDatasheetId}`)
    sheet.value = {
      partnerDatasheetId: res.data.partnerDatasheetId,
      title: res.data.title ?? '',
      definitionName: res.data.definitionName,
      partnerAccess: res.data.partnerAccess,
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
  sheetError.value = ''
  try {
    const fd = new FormData()
    fd.append('file', file)
    const res = await apiClient.post('/v1/partner/my/datasheet-files', fd, { headers: { 'Content-Type': 'multipart/form-data' } })
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
    const res = await apiClient.get(`/v1/partner/my/datasheet-values/${c.valueId}/file`, { responseType: 'blob' })
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

async function saveSheet() {
  if (savingSheet.value) return
  savingSheet.value = true
  sheetError.value = ''
  try {
    await apiClient.put(`/v1/partner/my/datasheets/${sheet.value.partnerDatasheetId}`, {
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

onMounted(load)
</script>

<style scoped>
.pp-head { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; flex-wrap: wrap; margin-bottom: .6rem; }
.pp-head h1 { margin: 0; font-size: 1.35rem; color: #003366; }
.pp-sub { font-size: .8rem; color: #6b7888; margin: .25rem 0 0; }
.pp-add { display: flex; gap: .5rem; align-items: center; flex-wrap: wrap; }
.pp-inp { padding: .4rem .55rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .85rem; width: 100%; background: #fff; }
.pp-add .pp-inp { width: auto; }
.pp-btn { background: #f2f5f9; border: 1px solid #cfd7e3; border-radius: 5px; padding: .3rem .65rem; font-size: .78rem; font-weight: 600; color: #2c3e50; cursor: pointer; }
.pp-btn:hover { background: #e8eef6; }
.pp-btn-danger { color: #b3261e; border-color: #e2b8b5; background: #fdf3f2; }
.pp-btn-primary { background: #003366; color: #fff; border: none; border-radius: 5px; padding: .4rem .9rem; font-size: .8rem; font-weight: 600; cursor: pointer; }
.pp-btn-primary:disabled { opacity: .5; cursor: default; }
.pp-err { background: #fdf3f2; border: 1px solid #e2b8b5; color: #b3261e; padding: .45rem .7rem; border-radius: 6px; font-size: .8rem; margin-bottom: .5rem; }
.pp-loading { color: #6b7888; font-size: .85rem; padding: .5rem 0; }
.pp-tbl { width: 100%; border-collapse: collapse; font-size: .85rem; background: #fff; border-radius: 8px; overflow: hidden; box-shadow: 0 1px 3px rgba(0,0,0,.06); }
.pp-tbl th { text-align: left; padding: .5rem .7rem; color: #6b7888; font-size: .75rem; text-transform: uppercase; letter-spacing: .03em; border-bottom: 1.5px solid #e8edf4; background: #fafbfd; }
.pp-tbl td { padding: .5rem .7rem; border-bottom: 1px solid #eef1f5; }
.pp-muted { color: #8a97a8; font-weight: 400; }
.pp-backdrop { position: fixed; inset: 0; background: rgba(20,30,50,.55); z-index: 1200; display: flex; align-items: center; justify-content: center; padding: 1rem; }
.pp-dialog { background: #fff; border-radius: 8px; width: min(880px, 100%); max-height: 92vh; display: flex; flex-direction: column; box-shadow: 0 10px 40px rgba(0,0,0,.2); }
.pp-dialog-head { display: flex; justify-content: space-between; align-items: center; padding: .85rem 1.1rem; border-bottom: 1px solid #e6ebf2; }
.pp-dialog-head h3 { margin: 0; font-size: 1rem; color: #1a2d4f; }
.pp-x { background: none; border: none; font-size: 1rem; cursor: pointer; color: #6b7888; }
.pp-dialog-body { padding: 1rem 1.1rem; overflow-y: auto; }
.pp-dialog-foot { display: flex; justify-content: flex-end; gap: .5rem; padding: .75rem 1.1rem; border-top: 1px solid #e6ebf2; }
.pp-lbl { display: block; font-size: .75rem; font-weight: 700; color: #44536a; margin-bottom: .25rem; }
.pp-req { color: #b3261e; margin-left: .15rem; }
.pp-section { border: 1px solid #e2e8f0; border-radius: 7px; padding: .7rem .8rem; margin-top: .85rem; background: #fafbfd; }
.pp-section-title { font-weight: 700; color: #003366; font-size: .85rem; margin-bottom: .5rem; }
.pp-field { margin-bottom: .55rem; }
.pp-check { display: flex; align-items: center; gap: .35rem; font-size: .82rem; color: #2c3e50; }
.pp-file { display: flex; align-items: center; gap: .45rem; font-size: .8rem; flex-wrap: wrap; }
.pp-file-ok { color: #1c7a4a; font-weight: 600; }
.pp-grid { margin-bottom: .45rem; box-shadow: none; }
.pp-grid td { padding: .3rem .35rem; }
.pp-locked { padding: .4rem .55rem; background: #f2f5f9; border: 1px dashed #cfd7e3; border-radius: 5px; font-size: .82rem; color: #44536a; min-height: 1.9rem; }
.pp-indent { color: #9aa5b1; margin-right: .35rem; margin-left: .8rem; }
.pp-count { color: #6b7888; font-weight: 400; font-size: .78rem; }
</style>
