<template>
  <div class="fpc-wrap">
    <div class="fpc-head">
      <div>
        <h2 class="fpc-title">Faculty Profile Information</h2>
        <p class="fpc-sub">The single structure every teacher profile uses (Partners → Manage → ⚙ → Faculties, and
          the partner portal's Faculties tab). Same builder as datasheets — but this is not a datasheet template.
          Removing a field hides it but keeps stored values; re-adding the same label restores them.</p>
      </div>
      <button type="button" class="btn-primary-sm" :disabled="saving || loading" @click="save">
        {{ saving ? 'Saving…' : 'Save structure' }}
      </button>
    </div>

    <div v-if="error" class="err-banner">{{ error }}</div>
    <div v-if="savedFlash" class="ok-banner">✓ Structure saved — applies to every teacher profile immediately.</div>
    <div v-if="loading" class="loading-row">Loading…</div>

    <template v-else-if="structureLoaded">
      <div v-for="(s, si) in sections" :key="si" class="fpc-section">
        <div class="fpc-section-head">
          <input v-model="s.title" class="fpc-inp" placeholder="Section title" style="flex:1.5" />
          <select v-model="s.kind" class="fpc-inp" style="flex:.8">
            <option value="fields">Fields</option>
            <option value="grid">Table (grid)</option>
          </select>
          <button type="button" class="btn-sm btn-danger" @click="sections.splice(si, 1)">✕ Section</button>
        </div>
        <div v-for="(f, fi) in s.fields" :key="fi" class="fpc-field-row">
          <input v-model="f.label" class="fpc-inp" :placeholder="s.kind === 'grid' ? 'Column label' : 'Field label'" style="flex:1.4" />
          <select v-model="f.type" class="fpc-inp" style="flex:1">
            <option value="text">Free text</option>
            <option value="number">Number</option>
            <option value="date">Date (calendar)</option>
            <option value="select">Dropdown</option>
            <option value="bool">Yes / No</option>
            <option value="file">File upload</option>
            <option value="autoid">Auto ID (system)</option>
            <option value="computed">Combined (system)</option>
          </select>
          <label class="fpc-req"><input type="checkbox" v-model="f.isRequired" /> req.</label>
          <label class="fpc-req" :class="{ 'fpc-off': f.type === 'autoid' || f.type === 'computed' }">
            <input type="checkbox" v-model="f.partnerCanEdit" :disabled="f.type === 'autoid' || f.type === 'computed'" /> partner
          </label>
          <button type="button" class="btn-sm btn-danger" @click="s.fields.splice(fi, 1)">✕</button>
          <textarea v-if="f.type === 'select'" v-model="f.optionsText" class="fpc-inp fpc-opts" rows="2"
                    placeholder="Dropdown options — one per line"></textarea>
          <input v-else-if="f.type === 'autoid'" v-model="f.optionsText" class="fpc-inp fpc-opts"
                 placeholder="ID pattern, e.g. MGW-ALC-FAC-{partner}-{n}" />
          <input v-else-if="f.type === 'computed'" v-model="f.optionsText" class="fpc-inp fpc-opts"
                 placeholder="Template from other fields, e.g. {First name} {Last name}" />
        </div>
        <button type="button" class="btn-sm" @click="s.fields.push({ id: null, label: '', type: 'text', optionsText: '', isRequired: false, partnerCanEdit: false })">
          + Add {{ s.kind === 'grid' ? 'column' : 'field' }}
        </button>
      </div>

      <button type="button" class="btn-sm fpc-add-section"
              @click="sections.push({ id: null, title: '', kind: 'fields', fields: [] })">+ Add section</button>
    </template>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '../../api/client.js'

const structureLoaded = ref(false)
const sections = ref([])
const loading = ref(false)
const saving = ref(false)
const error = ref('')
const savedFlash = ref(false)

async function load() {
  loading.value = true
  error.value = ''
  try {
    const res = await api.get('/v1/admin/faculty-profile-structure')
    structureLoaded.value = true
    sections.value = (res.data.sections ?? []).map(s => ({
      id: s.id,
      title: s.title,
      kind: s.kind,
      fields: s.fields.map(f => ({
        id: f.id, label: f.label, type: f.type,
        optionsText: f.optionsText ?? '', isRequired: !!f.isRequired,
        partnerCanEdit: !!f.partnerCanEdit,
      })),
    }))
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to load the faculty profile structure'
  } finally {
    loading.value = false
  }
}

async function save() {
  if (saving.value || !structureLoaded.value) return
  saving.value = true
  error.value = ''
  savedFlash.value = false
  try {
    await api.put('/v1/admin/faculty-profile-structure', {
      sections: sections.value
        .filter(s => s.title.trim())
        .map(s => ({
          id: s.id,
          title: s.title.trim(),
          kind: s.kind,
          fields: s.fields
            .filter(f => f.label.trim())
            .map(f => ({
              id: f.id, label: f.label.trim(), type: f.type,
              optionsText: ['select', 'autoid', 'computed'].includes(f.type) ? f.optionsText : null,
              isRequired: !!f.isRequired,
              partnerCanEdit: !!f.partnerCanEdit,
            })),
        })),
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
.fpc-wrap { background: #fff; border-radius: 8px; padding: 1.25rem 1.5rem; box-shadow: 0 1px 3px rgba(0,0,0,0.07); }
.fpc-head { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; margin-bottom: .75rem; }
.fpc-title { margin: 0; font-size: 1.15rem; color: #003366; }
.fpc-sub { font-size: .78rem; color: #6b7888; margin: .2rem 0 0; max-width: 720px; }
.btn-sm { background: #f2f5f9; border: 1px solid #cfd7e3; border-radius: 5px; padding: .28rem .6rem; font-size: .78rem; font-weight: 600; color: #2c3e50; cursor: pointer; }
.btn-sm:hover { background: #e8eef6; }
.btn-danger { color: #b3261e; border-color: #e2b8b5; background: #fdf3f2; }
.btn-primary-sm { background: #003366; color: #fff; border: none; border-radius: 5px; padding: .4rem .9rem; font-size: .8rem; font-weight: 600; cursor: pointer; }
.btn-primary-sm:disabled { opacity: .5; cursor: default; }
.err-banner { background: #fdf3f2; border: 1px solid #e2b8b5; color: #b3261e; padding: .45rem .7rem; border-radius: 6px; font-size: .8rem; margin-bottom: .5rem; }
.ok-banner { background: #e6f6ec; border: 1px solid #b9e1c7; color: #1c7a4a; padding: .45rem .7rem; border-radius: 6px; font-size: .8rem; margin-bottom: .5rem; font-weight: 600; }
.loading-row { color: #6b7888; font-size: .85rem; padding: .5rem 0; }
.fpc-inp { padding: .4rem .55rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .85rem; width: 100%; background: #fff; }
.fpc-section { border: 1px solid #e2e8f0; border-radius: 7px; padding: .7rem; margin-top: .8rem; background: #fafbfd; }
.fpc-section-head { display: flex; gap: .4rem; align-items: center; margin-bottom: .5rem; }
.fpc-field-row { display: flex; gap: .4rem; align-items: center; margin-bottom: .35rem; flex-wrap: wrap; }
.fpc-req { display: flex; align-items: center; gap: .25rem; font-size: .72rem; color: #44536a; white-space: nowrap; }
.fpc-off { opacity: .45; }
.fpc-opts { flex-basis: 100%; margin-left: .1rem; }
.fpc-add-section { margin-top: .8rem; }
</style>
