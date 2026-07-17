<template>
  <div class="pdt-wrap">
    <div class="pdt-head">
      <div>
        <h2 class="pdt-title">Partnership Documents</h2>
        <p class="pdt-sub">{{ items.length }} document type{{ items.length === 1 ? '' : 's' }} — each type has one shared
          design and a set of fields the Admission Office fills out per partner document.</p>
      </div>
      <button class="btn-primary-sm" @click="openDialog(null)">+ New Document Type</button>
    </div>

    <div v-if="error" class="err-banner">{{ error }}</div>
    <div v-if="loading" class="loading-row">Loading…</div>

    <table v-else-if="items.length" class="data-table">
      <thead><tr><th>Name</th><th>Fields</th><th>In use</th><th style="width:260px">Actions</th></tr></thead>
      <tbody>
        <tr v-for="t in items" :key="t.partnerDocumentTypeId" class="data-row">
          <td class="pdt-name">{{ t.name }}</td>
          <td>
            <span v-if="!t.fields.length" class="pdt-muted">—</span>
            <span v-for="f in t.fields" :key="f.id" class="pdt-chip" :title="fieldTypeLabel(f)">
              {{ f.label }}<em> · {{ fieldTypeShort(f) }}</em>
            </span>
          </td>
          <td>{{ t.inUse ? `${t.inUse} document${t.inUse === 1 ? '' : 's'}` : '—' }}</td>
          <td class="actions-cell">
            <button class="btn-sm" @click="openDialog(t)">✎ Edit</button>
            <button class="btn-sm" @click="openDesigner(t)">🎨 Design</button>
            <button class="btn-sm btn-danger" @click="removeType(t)">✕</button>
          </td>
        </tr>
      </tbody>
    </table>
    <p v-else class="pdt-sub">No document types yet — create one to get started.</p>

    <!-- Create / edit dialog (name + fields) -->
    <div v-if="dialogOpen" class="pdt-backdrop" @click.self="dialogOpen = false">
      <div class="pdt-dialog">
        <div class="pdt-dialog-head">
          <h3>{{ editingId ? 'Edit document type' : 'New document type' }}</h3>
          <button class="pdt-x" @click="dialogOpen = false">✕</button>
        </div>
        <div class="pdt-dialog-body">
          <label class="pdt-lbl">Name</label>
          <input v-model="form.name" class="pdt-inp" placeholder="e.g. Certificate of Partnership" />

          <div class="pdt-fields-head">
            <label class="pdt-lbl" style="margin:0">Fill-out fields</label>
            <button class="btn-sm" @click="form.fields.push({ id: '', label: '', type: 'text', source: 'name' })">+ Add field</button>
          </div>
          <p class="pdt-sub" style="margin:.15rem 0 .4rem">Each field becomes a tag <code>[field label]</code> you can place in the design.
            File-upload fields replace a bound image placeholder (e.g. a logo).</p>

          <div v-for="(f, i) in form.fields" :key="i" class="pdt-field-row">
            <input v-model="f.label" class="pdt-inp" placeholder="Field label (e.g. School name)" style="flex:1.4" />
            <select v-model="f.type" class="pdt-inp" style="flex:1">
              <option value="text">Free text</option>
              <option value="date">Date (calendar)</option>
              <option value="image">File upload (image)</option>
              <option value="partner">Partner field</option>
            </select>
            <select v-if="f.type === 'partner'" v-model="f.source" class="pdt-inp" style="flex:1">
              <option v-for="s in partnerSources" :key="s.key" :value="s.key">{{ s.label }}</option>
            </select>
            <button class="btn-sm btn-danger" @click="form.fields.splice(i, 1)">✕</button>
          </div>

          <div v-if="dialogError" class="err-banner" style="margin-top:.5rem">{{ dialogError }}</div>
        </div>
        <div class="pdt-dialog-foot">
          <button class="btn-sm" @click="dialogOpen = false">Cancel</button>
          <button class="btn-primary-sm" :disabled="saving || !form.name.trim()" @click="saveDialog">
            {{ saving ? 'Saving…' : 'Save' }}
          </button>
        </div>
      </div>
    </div>

    <CertificateEditorModal
      :open="designerOpen"
      :partner-doc-type-id="designerTypeId"
      :letter-type="designerName"
      programme-name="Partner document"
      @close="designerOpen = false"
      @saved="load" />
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '../../api/client.js'
import CertificateEditorModal from '../letters/CertificateEditorModal.vue'

const items = ref([])
const partnerSources = ref([])
const loading = ref(false)
const error = ref('')

const dialogOpen = ref(false)
const dialogError = ref('')
const saving = ref(false)
const editingId = ref('')
const form = ref({ name: '', fields: [] })

const designerOpen = ref(false)
const designerTypeId = ref('')
const designerName = ref('')

function fieldTypeShort(f) {
  return f.type === 'date' ? 'date' : f.type === 'image' ? 'upload' : f.type === 'partner' ? 'partner' : 'text'
}
function fieldTypeLabel(f) {
  if (f.type === 'partner') {
    const s = partnerSources.value.find(x => x.key === f.source)
    return `Auto-filled from partner profile: ${s?.label ?? f.source}`
  }
  return { text: 'Free text', date: 'Date (calendar)', image: 'File upload (image)' }[f.type] ?? f.type
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    const res = await api.get('/v1/admin/partner-document-types')
    items.value = res.data.items ?? []
    partnerSources.value = res.data.partnerSources ?? []
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to load document types'
  } finally {
    loading.value = false
  }
}

function openDialog(t) {
  editingId.value = t?.partnerDocumentTypeId ?? ''
  form.value = {
    name: t?.name ?? '',
    fields: (t?.fields ?? []).map(f => ({ id: f.id, label: f.label, type: f.type, source: f.source ?? 'name' })),
  }
  dialogError.value = ''
  dialogOpen.value = true
}

async function saveDialog() {
  if (saving.value) return
  saving.value = true
  dialogError.value = ''
  try {
    const payload = {
      name: form.value.name.trim(),
      fields: form.value.fields
        .filter(f => f.label.trim())
        .map(f => ({ id: f.id || null, label: f.label.trim(), type: f.type, source: f.type === 'partner' ? f.source : null })),
    }
    if (editingId.value) await api.patch(`/v1/admin/partner-document-types/${editingId.value}`, payload)
    else await api.post('/v1/admin/partner-document-types', payload)
    dialogOpen.value = false
    await load()
  } catch (e) {
    dialogError.value = e.response?.data?.error ?? e.message ?? 'Save failed'
  } finally {
    saving.value = false
  }
}

async function removeType(t) {
  if (!confirm(`Delete the document type "${t.name}"?`)) return
  error.value = ''
  try {
    await api.delete(`/v1/admin/partner-document-types/${t.partnerDocumentTypeId}`)
    await load()
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Delete failed'
  }
}

function openDesigner(t) {
  designerTypeId.value = t.partnerDocumentTypeId
  designerName.value = t.name
  designerOpen.value = true
}

onMounted(load)
</script>

<style scoped>
.pdt-wrap { background: #fff; border-radius: 8px; padding: 1.25rem 1.5rem; box-shadow: 0 1px 3px rgba(0,0,0,0.07); }
.pdt-head { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; margin-bottom: .75rem; }
.pdt-title { margin: 0; font-size: 1.15rem; color: #003366; }
.pdt-sub { font-size: .78rem; color: #6b7888; margin: .2rem 0 0; }
.pdt-name { font-weight: 600; color: #1a2d4f; }
.pdt-muted { color: #9aa5b1; }
.pdt-chip { display: inline-block; background: #eef3fa; border: 1px solid #d5e0ee; color: #2c3e50; border-radius: 12px; padding: .1rem .55rem; font-size: .72rem; margin: .1rem .25rem .1rem 0; }
.pdt-chip em { font-style: normal; color: #6b7888; }
.data-table { width: 100%; border-collapse: collapse; font-size: .85rem; }
.data-table th { text-align: left; padding: .45rem .6rem; color: #6b7888; font-size: .75rem; text-transform: uppercase; letter-spacing: .03em; border-bottom: 1.5px solid #e8edf4; }
.data-table td { padding: .5rem .6rem; border-bottom: 1px solid #eef1f5; vertical-align: middle; }
.actions-cell { display: flex; gap: .35rem; flex-wrap: wrap; }
.btn-sm { background: #f2f5f9; border: 1px solid #cfd7e3; border-radius: 5px; padding: .28rem .6rem; font-size: .78rem; font-weight: 600; color: #2c3e50; cursor: pointer; }
.btn-sm:hover { background: #e8eef6; }
.btn-danger { color: #b3261e; border-color: #e2b8b5; background: #fdf3f2; }
.btn-primary-sm { background: #003366; color: #fff; border: none; border-radius: 5px; padding: .4rem .9rem; font-size: .8rem; font-weight: 600; cursor: pointer; }
.btn-primary-sm:disabled { opacity: .5; cursor: default; }
.err-banner { background: #fdf3f2; border: 1px solid #e2b8b5; color: #b3261e; padding: .45rem .7rem; border-radius: 6px; font-size: .8rem; margin-bottom: .5rem; }
.loading-row { color: #6b7888; font-size: .85rem; padding: .5rem 0; }
.pdt-backdrop { position: fixed; inset: 0; background: rgba(20,30,50,.55); z-index: 1200; display: flex; align-items: center; justify-content: center; padding: 1rem; }
.pdt-dialog { background: #fff; border-radius: 8px; width: min(620px, 100%); max-height: 90vh; display: flex; flex-direction: column; box-shadow: 0 10px 40px rgba(0,0,0,.2); }
.pdt-dialog-head { display: flex; justify-content: space-between; align-items: center; padding: .85rem 1.1rem; border-bottom: 1px solid #e6ebf2; }
.pdt-dialog-head h3 { margin: 0; font-size: 1rem; color: #1a2d4f; }
.pdt-x { background: none; border: none; font-size: 1rem; cursor: pointer; color: #6b7888; }
.pdt-dialog-body { padding: 1rem 1.1rem; overflow-y: auto; }
.pdt-dialog-foot { display: flex; justify-content: flex-end; gap: .5rem; padding: .75rem 1.1rem; border-top: 1px solid #e6ebf2; }
.pdt-lbl { display: block; font-size: .75rem; font-weight: 700; color: #44536a; margin-bottom: .25rem; }
.pdt-inp { padding: .4rem .55rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .85rem; width: 100%; background: #fff; }
.pdt-fields-head { display: flex; justify-content: space-between; align-items: center; margin-top: .85rem; }
.pdt-field-row { display: flex; gap: .4rem; align-items: center; margin-bottom: .4rem; }
code { background: #f2f5f9; border-radius: 4px; padding: 0 .3rem; font-size: .75rem; }
</style>
