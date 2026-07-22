<template>
  <div class="mcc-wrap">
    <div class="mcc-head">
      <div>
        <h2 class="mcc-title">Module Cohorts</h2>
        <p class="mcc-sub">The cohort number pattern plus the cohort TYPES: every type is a template carrying its
          own upload fields and data fields. A type assigned to a cohort is locked — clone it to make changes.</p>
      </div>
      <button type="button" class="btn-primary-sm" :disabled="saving || loading" @click="save">
        {{ saving ? 'Saving…' : 'Save' }}
      </button>
    </div>

    <div v-if="error" class="err-banner">{{ error }}</div>
    <div v-if="savedFlash" class="ok-banner">✓ Saved.</div>
    <div v-if="loading" class="loading-row">Loading…</div>

    <template v-else>
      <label class="mcc-lbl">Cohort number pattern</label>
      <input v-model="pattern" class="mcc-inp" style="max-width:420px"
             placeholder="{partner}-{module}-{n}" />
      <p class="mcc-sub" style="margin:.25rem 0 1rem">{partner} = partner name, {module} = module code, {n} = sequence per partner+module (3 digits).</p>

      <div class="mcc-fields-head" style="margin-top:1.4rem">
        <label class="mcc-lbl" style="margin:0">Cohort types</label>
        <button type="button" class="btn-sm" @click="openType(null)">+ Add cohort type</button>
      </div>
      <p class="mcc-sub" style="margin:.2rem 0 .5rem">Each cohort picks ONE type when created; the type's form
        shows as an extra section on the cohort's Record tab. Removing a field hides it; re-adding the same
        label restores its values.</p>
      <table v-if="types.length" class="mcc-types">
        <thead><tr><th>Name</th><th>Fields</th><th>In use</th><th style="width:130px"></th></tr></thead>
        <tbody>
          <tr v-for="t in types" :key="t.cohortTypeId">
            <td style="font-weight:600">{{ t.name }}</td>
            <td>
              <span v-if="!t.fields.length && !t.uploadFields.length" class="mcc-muted">—</span>
              <span v-for="f in t.uploadFields" :key="'u' + f.id" class="mcc-chip mcc-chip-upl">{{ f.label }}<em> · upload</em></span>
              <span v-for="f in t.fields" :key="f.id" class="mcc-chip">{{ f.label }}<em> · {{ f.type }}</em></span>
            </td>
            <td>
              {{ t.inUse ? `${t.inUse} cohort${t.inUse === 1 ? '' : 's'}` : '—' }}
              <span v-if="t.locked" class="mcc-lock" title="Assigned to a cohort — locked; clone to change.">🔒</span>
            </td>
            <td>
              <button type="button" class="btn-sm" @click="openType(t)">{{ t.locked ? '👁 View' : '✎ Edit' }}</button>
              <button type="button" class="btn-sm" @click="cloneType(t)">📋 Clone</button>
              <button v-if="!t.locked" type="button" class="btn-sm btn-danger" @click="removeType(t)">✕</button>
            </td>
          </tr>
        </tbody>
      </table>
      <p v-else class="mcc-sub">No cohort types yet.</p>

      <!-- Type builder dialog -->
      <div v-if="typeOpen" class="mcc-backdrop" @click.self="typeOpen = false">
        <div class="mcc-dialog">
          <div class="mcc-dialog-head"><h3>{{ typeId ? 'Edit cohort type' : 'New cohort type' }}</h3>
            <button type="button" class="mcc-x" @click="typeOpen = false">✕</button></div>
          <div class="mcc-dialog-body">
            <div v-if="typeLocked" class="mcc-locked-banner">🔒 This cohort type is assigned to a cohort and is locked.
              Use Clone to create an editable copy.</div>
            <label class="mcc-lbl">Name *</label>
            <input v-model="typeName" class="mcc-inp" placeholder="e.g. Online run" :disabled="typeLocked" />

            <div class="mcc-fields-head" style="margin-top:.8rem">
              <label class="mcc-lbl" style="margin:0">Upload fields</label>
              <button v-if="!typeLocked" type="button" class="btn-sm" @click="typeUploads.push({ id: null, label: '', allowMultiple: true, isGradingSheet: false, visibleToStudents: false })">+ Add upload field</button>
            </div>
            <div v-for="(f, i) in typeUploads" :key="'u' + i" class="mcc-field-row" style="flex-wrap:wrap">
              <input v-model="f.label" class="mcc-inp" placeholder="Upload field label" style="flex:1.4" :disabled="typeLocked" />
              <select v-model="f.allowMultiple" class="mcc-inp" style="flex:.8" :disabled="typeLocked">
                <option :value="false">1 document</option>
                <option :value="true">Several documents</option>
              </select>
              <label class="mcc-check" title="Uploading here stamps 'Date Grading Sheet Uploaded'">
                <input type="checkbox" v-model="f.isGradingSheet" :disabled="typeLocked" /> grading sheet</label>
              <label class="mcc-check" title="Students may download this field's files on their cohorts">
                <input type="checkbox" v-model="f.visibleToStudents" :disabled="typeLocked" /> student-visible</label>
              <button v-if="!typeLocked" type="button" class="btn-sm btn-danger" @click="typeUploads.splice(i, 1)">✕</button>
            </div>

            <div class="mcc-fields-head" style="margin-top:.8rem">
              <label class="mcc-lbl" style="margin:0">Data fields</label>
              <button v-if="!typeLocked" type="button" class="btn-sm" @click="typeFields.push({ id: null, label: '', type: 'text', optionsText: '', isRequired: false })">+ Add field</button>
            </div>
            <div v-for="(f, i) in typeFields" :key="i" class="mcc-field-row" style="flex-wrap:wrap">
              <input v-model="f.label" class="mcc-inp" placeholder="Field label" style="flex:1.4" :disabled="typeLocked" />
              <select v-model="f.type" class="mcc-inp" style="flex:1" :disabled="typeLocked">
                <option value="text">Free text</option>
                <option value="number">Number</option>
                <option value="date">Date (calendar)</option>
                <option value="select">Dropdown</option>
                <option value="bool">Yes / No</option>
                <option value="file">File upload</option>
                <option value="info">Read-only text (edited here only)</option>
              </select>
              <label class="mcc-check"><input type="checkbox" v-model="f.isRequired" :disabled="typeLocked" /> req.</label>
              <button v-if="!typeLocked" type="button" class="btn-sm btn-danger" @click="typeFields.splice(i, 1)">✕</button>
              <textarea v-if="f.type === 'select'" v-model="f.optionsText" class="mcc-inp" style="flex-basis:100%" rows="2"
                        placeholder="Dropdown options — one per line" :disabled="typeLocked"></textarea>
              <textarea v-else-if="f.type === 'info'" v-model="f.optionsText" class="mcc-inp" style="flex-basis:100%" rows="3"
                        placeholder="Text shown read-only on every cohort of this type" :disabled="typeLocked"></textarea>
            </div>
            <div v-if="typeError" class="err-banner" style="margin-top:.5rem">{{ typeError }}</div>
          </div>
          <div class="mcc-dialog-foot">
            <button type="button" class="btn-sm" @click="typeOpen = false">Cancel</button>
            <button v-if="!typeLocked" type="button" class="btn-primary-sm" :disabled="typeSaving || !typeName.trim()" @click="saveType">
              {{ typeSaving ? 'Saving…' : 'Save' }}</button>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '../../api/client.js'

const pattern = ref('{partner}-{module}-{n}')
const fields = ref([])
const loading = ref(false)
const saving = ref(false)
const error = ref('')
const savedFlash = ref(false)

const types = ref([])
const typeOpen = ref(false)
const typeId = ref('')
const typeName = ref('')
const typeFields = ref([])
const typeUploads = ref([])
const typeLocked = ref(false)
const typeSaving = ref(false)
const typeError = ref('')

async function loadTypes() {
  try {
    const res = await api.get('/v1/admin/cohort-types')
    types.value = res.data.items ?? []
  } catch { types.value = [] }
}

function openType(t) {
  typeId.value = t?.cohortTypeId ?? ''
  typeName.value = t?.name ?? ''
  typeLocked.value = !!t?.locked
  typeFields.value = (t?.fields ?? []).map(f => ({
    id: f.id, label: f.label, type: f.type, optionsText: f.optionsText ?? '', isRequired: !!f.isRequired,
  }))
  typeUploads.value = (t?.uploadFields ?? []).map(f => ({
    id: f.id, label: f.label, allowMultiple: !!f.allowMultiple,
    isGradingSheet: !!f.isGradingSheet, visibleToStudents: !!f.visibleToStudents,
  }))
  typeError.value = ''
  typeOpen.value = true
}

async function cloneType(t) {
  error.value = ''
  try {
    const res = await api.post(`/v1/admin/cohort-types/${t.cohortTypeId}/clone`)
    await loadTypes()
    const clone = types.value.find(x => x.cohortTypeId === res.data.cohortTypeId)
    if (clone) openType(clone)
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Clone failed'
  }
}

async function saveType() {
  if (typeSaving.value) return
  typeSaving.value = true
  typeError.value = ''
  try {
    let id = typeId.value
    if (!id) {
      const res = await api.post('/v1/admin/cohort-types', { name: typeName.value.trim() })
      id = res.data.cohortTypeId
    } else {
      await api.patch(`/v1/admin/cohort-types/${id}`, { name: typeName.value.trim() })
    }
    await api.put(`/v1/admin/cohort-types/${id}/structure`, {
      fields: typeFields.value.filter(f => f.label.trim()).map(f => ({
        id: f.id, label: f.label.trim(), type: f.type,
        optionsText: ['select', 'info'].includes(f.type) ? f.optionsText : null,
        isRequired: !!f.isRequired,
      })),
      uploadFields: typeUploads.value.filter(f => f.label.trim()).map(f => ({
        id: f.id, label: f.label.trim(), allowMultiple: !!f.allowMultiple,
        isGradingSheet: !!f.isGradingSheet, visibleToStudents: !!f.visibleToStudents,
      })),
    })
    typeOpen.value = false
    await loadTypes()
  } catch (e) {
    typeError.value = e.response?.data?.error ?? e.message ?? 'Save failed'
  } finally {
    typeSaving.value = false
  }
}

async function removeType(t) {
  if (!confirm(`Delete the cohort type "${t.name}"?`)) return
  error.value = ''
  try {
    await api.delete(`/v1/admin/cohort-types/${t.cohortTypeId}`)
    await loadTypes()
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Delete failed'
  }
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    await loadTypes()
    const res = await api.get('/v1/admin/cohort-settings')
    pattern.value = res.data.cohortNumberPattern ?? '{partner}-{module}-{n}'
    fields.value = (res.data.fields ?? []).map(f => ({
      id: f.id, label: f.label, allowMultiple: !!f.allowMultiple, isGradingSheet: !!f.isGradingSheet,
    }))
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to load cohort settings'
  } finally {
    loading.value = false
  }
}

async function save() {
  if (saving.value) return
  saving.value = true
  error.value = ''
  savedFlash.value = false
  try {
    await api.put('/v1/admin/cohort-settings', {
      cohortNumberPattern: pattern.value.trim() || '{partner}-{module}-{n}',
    })
    savedFlash.value = true
    setTimeout(() => { savedFlash.value = false }, 4000)
    await load()
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Save failed'
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<style scoped>
.mcc-wrap { background: #fff; border-radius: 8px; padding: 1.25rem 1.5rem; box-shadow: 0 1px 3px rgba(0,0,0,0.07); }
.mcc-head { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; margin-bottom: .75rem; }
.mcc-title { margin: 0; font-size: 1.15rem; color: #003366; }
.mcc-sub { font-size: .78rem; color: #6b7888; margin: .2rem 0 0; max-width: 720px; }
.mcc-lbl { display: block; font-size: .75rem; font-weight: 700; color: #44536a; margin-bottom: .25rem; }
.mcc-inp { padding: .4rem .55rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .85rem; width: 100%; background: #fff; }
.mcc-fields-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: .5rem; }
.mcc-field-row { display: flex; gap: .4rem; align-items: center; margin-bottom: .35rem; }
.mcc-check { display: flex; align-items: center; gap: .25rem; font-size: .74rem; color: #44536a; white-space: nowrap; }
.btn-sm { background: #f2f5f9; border: 1px solid #cfd7e3; border-radius: 5px; padding: .28rem .6rem; font-size: .78rem; font-weight: 600; color: #2c3e50; cursor: pointer; }
.btn-sm:hover { background: #e8eef6; }
.btn-danger { color: #b3261e; border-color: #e2b8b5; background: #fdf3f2; }
.btn-primary-sm { background: #003366; color: #fff; border: none; border-radius: 5px; padding: .4rem .9rem; font-size: .8rem; font-weight: 600; cursor: pointer; }
.btn-primary-sm:disabled { opacity: .5; cursor: default; }
.err-banner { background: #fdf3f2; border: 1px solid #e2b8b5; color: #b3261e; padding: .45rem .7rem; border-radius: 6px; font-size: .8rem; margin-bottom: .5rem; }
.ok-banner { background: #e6f6ec; border: 1px solid #b9e1c7; color: #1c7a4a; padding: .45rem .7rem; border-radius: 6px; font-size: .8rem; margin-bottom: .5rem; font-weight: 600; }
.loading-row { color: #6b7888; font-size: .85rem; padding: .5rem 0; }
.mcc-sub { font-size: .78rem; color: #6b7888; }
.mcc-muted { color: #9aa5b1; }
.mcc-chip { display: inline-block; background: #eef3fa; border: 1px solid #d5e0ee; color: #2c3e50; border-radius: 12px; padding: .1rem .55rem; font-size: .72rem; margin: .1rem .25rem .1rem 0; }
.mcc-chip em { font-style: normal; color: #6b7888; }
.mcc-chip-upl { background: #fef7e8; border-color: #f0dcae; }
.mcc-lock { margin-left: .3rem; }
.mcc-locked-banner { background: #fff4e6; border: 1px solid #f0d2a8; color: #8a5a00; padding: .45rem .7rem; border-radius: 6px; font-size: .8rem; margin-bottom: .6rem; font-weight: 600; }
.mcc-types { width: 100%; border-collapse: collapse; font-size: .85rem; }
.mcc-types th { text-align: left; padding: .4rem .55rem; color: #6b7888; font-size: .73rem; text-transform: uppercase; border-bottom: 1.5px solid #e8edf4; }
.mcc-types td { padding: .4rem .55rem; border-bottom: 1px solid #eef1f5; vertical-align: middle; }
.mcc-backdrop { position: fixed; inset: 0; background: rgba(20,30,50,.55); z-index: 1200; display: flex; align-items: center; justify-content: center; padding: 1rem; }
.mcc-dialog { background: #fff; border-radius: 8px; width: min(680px, 100%); max-height: 90vh; display: flex; flex-direction: column; box-shadow: 0 10px 40px rgba(0,0,0,.2); }
.mcc-dialog-head { display: flex; justify-content: space-between; align-items: center; padding: .85rem 1.1rem; border-bottom: 1px solid #e6ebf2; }
.mcc-dialog-head h3 { margin: 0; font-size: 1rem; color: #1a2d4f; }
.mcc-x { background: none; border: none; font-size: 1rem; cursor: pointer; color: #6b7888; }
.mcc-dialog-body { padding: 1rem 1.1rem; overflow-y: auto; }
.mcc-dialog-foot { display: flex; justify-content: flex-end; gap: .5rem; padding: .75rem 1.1rem; border-top: 1px solid #e6ebf2; }
</style>
