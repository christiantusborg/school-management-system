<template>
  <div>
    <div class="pc-head">
      <div>
        <div class="manage-section-title">Partnership documents</div>
        <p class="pc-sub">Certificates, agreements, authorization letters and other documents issued to this partner.
          Document types and their designs are managed in System Config → Partnership Documents;
          here you add a document and fill out its fields. Download always renders the latest design.</p>
      </div>
      <button v-if="auth.can('partner.documents.add')" class="btn-primary-sm" @click="openDialog(null)">+ Add document</button>
    </div>

    <div v-if="error" class="err-banner">{{ error }}</div>
    <div v-if="loading" class="loading-row">Loading…</div>

    <table v-else-if="items.length" class="data-table" style="margin-bottom:.75rem">
      <thead><tr><th>Type</th><th>Filled-out fields</th><th>Updated</th><th style="width:230px">Actions</th></tr></thead>
      <tbody>
        <tr v-for="d in items" :key="d.partnerDocumentId" class="data-row">
          <td class="pc-type">{{ d.typeName }}</td>
          <td class="pc-values">
            <span v-if="!valueSummary(d).length" class="pc-muted">—</span>
            <span v-for="(v, i) in valueSummary(d)" :key="i" class="pc-chip">{{ v }}</span>
          </td>
          <td>{{ fmtDate(d.updatedAt) }}</td>
          <td class="actions-cell">
            <button v-if="auth.can('partner.documents.edit')" class="btn-sm" @click="openDialog(d)">✎ Edit</button>
            <button class="btn-sm" :disabled="d.downloading" @click="downloadDoc(d)">
              {{ d.downloading ? '…' : '⤓ Download' }}
            </button>
            <button v-if="auth.can('partner.documents.delete')" class="btn-sm btn-danger" @click="removeDoc(d)">✕</button>
          </td>
        </tr>
      </tbody>
    </table>
    <p v-else-if="!loading" class="pc-sub" style="margin:.5rem 0;">No documents yet — click “+ Add document”.</p>

    <!-- Create / edit dialog: pick type, fill out its fields -->
    <div v-if="dialogOpen" class="pc-backdrop" @click.self="dialogOpen = false">
      <div class="pc-dialog">
        <div class="pc-dialog-head">
          <h3>{{ editingId ? 'Edit document' : 'New document' }}</h3>
          <button class="pc-x" @click="dialogOpen = false">✕</button>
        </div>
        <div class="pc-dialog-body">
          <label class="pc-lbl">Document type</label>
          <select v-if="!editingId" v-model="dlgTypeId" class="pc-inp">
            <option value="">— pick a type —</option>
            <option v-for="t in types" :key="t.partnerDocumentTypeId" :value="t.partnerDocumentTypeId">{{ t.name }}</option>
          </select>
          <div v-else class="pc-fixed-type">{{ dlgTypeName }}</div>

          <template v-if="dlgFields.length">
            <div v-for="f in dlgFields" :key="f.id" class="pc-field">
              <label class="pc-lbl">{{ f.label }}</label>
              <input v-if="f.type === 'text'" v-model="dlgValues[f.id]" class="pc-inp" />
              <input v-else-if="f.type === 'date'" v-model="dlgValues[f.id]" type="date" class="pc-inp" />
              <template v-else-if="f.type === 'image'">
                <div class="pc-upl-row">
                  <input type="file" accept="image/*" @change="onUploadField(f, $event)" />
                  <span v-if="uploadingField === f.id" class="pc-muted">Uploading…</span>
                  <span v-else-if="dlgValues[f.id]" class="pc-upl-ok">✓ {{ uploadNames[f.id] || 'image uploaded' }}</span>
                </div>
              </template>
              <div v-else-if="f.type === 'partner'" class="pc-fixed-type">
                Auto-filled from the partner profile ({{ sourceLabel(f.source) }})
              </div>
            </div>
          </template>
          <p v-else-if="dlgTypeId" class="pc-sub" style="margin:.5rem 0 0">This document type has no fill-out fields — just save it.</p>

          <div v-if="dialogError" class="err-banner" style="margin-top:.6rem">{{ dialogError }}</div>
        </div>
        <div class="pc-dialog-foot">
          <button class="btn-sm" @click="dialogOpen = false">Cancel</button>
          <button type="button" class="btn-primary-sm" :disabled="saving || !dlgTypeId || !!uploadingField" @click="saveDialog">
            {{ saving ? 'Saving…' : 'Save' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import api from '../../api/client.js'
import { auth } from '../../store/auth.js'

const props = defineProps({
  partnerId: { type: String, required: true },
  partnerName: { type: String, default: '' },
})

const items = ref([])
const types = ref([])
const partnerSources = ref([])
const loading = ref(false)
const error = ref('')

const dialogOpen = ref(false)
const dialogError = ref('')
const saving = ref(false)
const editingId = ref('')
const dlgTypeId = ref('')
const dlgTypeName = ref('')
const dlgValues = ref({})
const uploadNames = ref({})
const uploadingField = ref('')

const dlgFields = computed(() =>
  types.value.find(t => t.partnerDocumentTypeId === dlgTypeId.value)?.fields ?? [])

function sourceLabel(key) {
  return partnerSources.value.find(s => s.key === key)?.label ?? key
}
function fmtDate(d) {
  return d ? new Date(d).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }) : '—'
}
// Short "Label: value" chips for the table; image values show as the label only.
function valueSummary(d) {
  const type = types.value.find(t => t.partnerDocumentTypeId === d.partnerDocumentTypeId)
  if (!type) return []
  return type.fields
    .filter(f => f.type !== 'partner' && d.values?.[f.id])
    .map(f => (f.type === 'image' ? `${f.label}: 🖼` : `${f.label}: ${d.values[f.id]}`))
}

async function load() {
  if (!props.partnerId) return
  loading.value = true
  error.value = ''
  try {
    const res = await api.get(`/v1/admin/partners/${props.partnerId}/documents`)
    items.value = (res.data.items ?? []).map(d => ({ ...d, downloading: false }))
    types.value = res.data.types ?? []
    partnerSources.value = res.data.partnerSources ?? []
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to load documents'
  } finally {
    loading.value = false
  }
}

function openDialog(d) {
  editingId.value = d?.partnerDocumentId ?? ''
  dlgTypeId.value = d?.partnerDocumentTypeId ?? ''
  dlgTypeName.value = d?.typeName ?? ''
  dlgValues.value = { ...(d?.values ?? {}) }
  uploadNames.value = {}
  uploadingField.value = ''
  dialogError.value = ''
  dialogOpen.value = true
}

async function onUploadField(f, ev) {
  const file = ev.target.files?.[0]
  if (!file) return
  uploadingField.value = f.id
  dialogError.value = ''
  try {
    const fd = new FormData()
    fd.append('file', file)
    fd.append('name', `${props.partnerName || 'partner'} — ${f.label}`)
    const res = await api.post('/v1/admin/letter-assets', fd, { headers: { 'Content-Type': 'multipart/form-data' } })
    dlgValues.value[f.id] = res.data.letterAssetId
    uploadNames.value[f.id] = file.name
  } catch (e) {
    dialogError.value = e.response?.data?.message ?? e.message ?? 'Upload failed'
  } finally {
    uploadingField.value = ''
    ev.target.value = ''
  }
}

async function saveDialog() {
  if (saving.value || !dlgTypeId.value) return
  saving.value = true
  dialogError.value = ''
  try {
    const payload = { partnerDocumentTypeId: dlgTypeId.value, values: dlgValues.value }
    if (editingId.value) await api.put(`/v1/admin/partner-documents/${editingId.value}`, payload)
    else await api.post(`/v1/admin/partners/${props.partnerId}/documents`, payload)
    dialogOpen.value = false
    await load()
  } catch (e) {
    dialogError.value = e.response?.data?.error ?? e.message ?? 'Save failed'
  } finally {
    saving.value = false
  }
}

async function removeDoc(d) {
  if (!confirm(`Remove this "${d.typeName}" document?`)) return
  error.value = ''
  try {
    await api.delete(`/v1/admin/partner-documents/${d.partnerDocumentId}`)
    await load()
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Remove failed'
  }
}

async function downloadDoc(d) {
  if (d.downloading) return
  d.downloading = true
  error.value = ''
  try {
    const res = await api.get(`/v1/admin/partner-documents/${d.partnerDocumentId}/download`, { responseType: 'blob' })
    const url = URL.createObjectURL(res.data)
    window.open(url, '_blank')
    setTimeout(() => URL.revokeObjectURL(url), 60_000)
  } catch (e) {
    error.value = 'Download failed'
  } finally {
    d.downloading = false
  }
}

watch(() => props.partnerId, load, { immediate: true })
</script>

<style scoped>
/* AdminView's styles are scoped, so this tab carries its own copies of the
   shared table/button looks. */
.pc-head { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; margin-bottom: .4rem; }
.pc-sub { font-size: .78rem; color: #6b7888; margin: .15rem 0 .5rem; }
.pc-type { font-weight: 600; color: #1a2d4f; }
.pc-muted { color: #9aa5b1; }
.pc-chip { display: inline-block; background: #eef3fa; border: 1px solid #d5e0ee; color: #2c3e50; border-radius: 12px; padding: .1rem .55rem; font-size: .72rem; margin: .1rem .25rem .1rem 0; }
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
.pc-backdrop { position: fixed; inset: 0; background: rgba(20,30,50,.55); z-index: 1200; display: flex; align-items: center; justify-content: center; padding: 1rem; }
.pc-dialog { background: #fff; border-radius: 8px; width: min(540px, 100%); max-height: 90vh; display: flex; flex-direction: column; box-shadow: 0 10px 40px rgba(0,0,0,.2); }
.pc-dialog-head { display: flex; justify-content: space-between; align-items: center; padding: .85rem 1.1rem; border-bottom: 1px solid #e6ebf2; }
.pc-dialog-head h3 { margin: 0; font-size: 1rem; color: #1a2d4f; }
.pc-x { background: none; border: none; font-size: 1rem; cursor: pointer; color: #6b7888; }
.pc-dialog-body { padding: 1rem 1.1rem; overflow-y: auto; }
.pc-dialog-foot { display: flex; justify-content: flex-end; gap: .5rem; padding: .75rem 1.1rem; border-top: 1px solid #e6ebf2; }
.pc-lbl { display: block; font-size: .75rem; font-weight: 700; color: #44536a; margin-bottom: .25rem; }
.pc-inp { padding: .4rem .55rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .85rem; width: 100%; background: #fff; }
.pc-field { margin-top: .7rem; }
.pc-fixed-type { padding: .4rem .55rem; background: #f2f5f9; border: 1px solid #e2e8f0; border-radius: 5px; font-size: .82rem; color: #44536a; }
.pc-upl-row { display: flex; align-items: center; gap: .6rem; font-size: .8rem; }
.pc-upl-ok { color: #1c7a4a; font-weight: 600; }
</style>
