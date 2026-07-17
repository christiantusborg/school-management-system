<template>
  <div class="pds-wrap">
    <div class="pds-head">
      <div>
        <h2 class="pds-title">Partner Datasheets</h2>
        <p class="pds-sub">{{ items.length }} datasheet template{{ items.length === 1 ? '' : 's' }} — extra structured data
          the Admission Office can keep on partners. Removing a field hides it but keeps all stored values;
          re-adding a field with the same label brings them back.</p>
      </div>
      <button class="btn-primary-sm" @click="openBuilder(null)">+ New Datasheet</button>
    </div>

    <div v-if="error" class="err-banner">{{ error }}</div>
    <div v-if="loading" class="loading-row">Loading…</div>

    <table v-else-if="items.length" class="data-table">
      <thead><tr><th>Name</th><th>Structure</th><th>In use</th><th style="width:170px">Actions</th></tr></thead>
      <tbody>
        <tr v-for="d in items" :key="d.partnerDatasheetDefinitionId" class="data-row">
          <td class="pds-name">{{ d.name }}</td>
          <td>
            <span v-if="!d.sections.length" class="pds-muted">—</span>
            <span v-for="s in d.sections" :key="s.id" class="pds-chip">
              {{ s.title }}<em> · {{ s.kind === 'grid' ? 'table' : 'fields' }} · {{ s.fields.length }}</em>
            </span>
          </td>
          <td>{{ d.inUse ? `${d.inUse} sheet${d.inUse === 1 ? '' : 's'}` : '—' }}</td>
          <td class="actions-cell">
            <button type="button" class="btn-sm" @click="openBuilder(d)">✎ Edit</button>
            <button type="button" class="btn-sm btn-danger" @click="removeDef(d)">✕</button>
          </td>
        </tr>
      </tbody>
    </table>
    <p v-else class="pds-sub">No datasheet templates yet — create one to get started.</p>

    <!-- Builder dialog: name + sections + fields -->
    <div v-if="builderOpen" class="pds-backdrop" @click.self="builderOpen = false">
      <div class="pds-dialog">
        <div class="pds-dialog-head">
          <h3>{{ editingId ? 'Edit datasheet' : 'New datasheet' }}</h3>
          <button type="button" class="pds-x" @click="builderOpen = false">✕</button>
        </div>
        <div class="pds-dialog-body">
          <label class="pds-lbl">Name</label>
          <input v-model="form.name" class="pds-inp" placeholder="e.g. Partner Visit Report" />

          <div v-for="(s, si) in form.sections" :key="si" class="pds-section">
            <div class="pds-section-head">
              <input v-model="s.title" class="pds-inp" placeholder="Section title" style="flex:1.5" />
              <select v-model="s.kind" class="pds-inp" style="flex:.8">
                <option value="fields">Fields</option>
                <option value="grid">Table (grid)</option>
              </select>
              <button type="button" class="btn-sm btn-danger" @click="form.sections.splice(si, 1)">✕ Section</button>
            </div>
            <div v-for="(f, fi) in s.fields" :key="fi" class="pds-field-row">
              <input v-model="f.label" class="pds-inp" :placeholder="s.kind === 'grid' ? 'Column label' : 'Field label'" style="flex:1.4" />
              <select v-model="f.type" class="pds-inp" style="flex:1">
                <option value="text">Free text</option>
                <option value="number">Number</option>
                <option value="date">Date (calendar)</option>
                <option value="select">Dropdown</option>
                <option value="bool">Yes / No</option>
                <option value="file">File upload</option>
              </select>
              <label class="pds-req"><input type="checkbox" v-model="f.isRequired" /> req.</label>
              <button type="button" class="btn-sm btn-danger" @click="s.fields.splice(fi, 1)">✕</button>
              <textarea v-if="f.type === 'select'" v-model="f.optionsText" class="pds-inp pds-opts" rows="2"
                        placeholder="Dropdown options — one per line"></textarea>
            </div>
            <button type="button" class="btn-sm" @click="s.fields.push({ id: null, label: '', type: 'text', optionsText: '', isRequired: false })">
              + Add {{ s.kind === 'grid' ? 'column' : 'field' }}
            </button>
          </div>

          <button type="button" class="btn-sm pds-add-section"
                  @click="form.sections.push({ id: null, title: '', kind: 'fields', fields: [] })">+ Add section</button>

          <div v-if="builderError" class="err-banner" style="margin-top:.5rem">{{ builderError }}</div>
        </div>
        <div class="pds-dialog-foot">
          <button type="button" class="btn-sm" @click="builderOpen = false">Cancel</button>
          <button type="button" class="btn-primary-sm" :disabled="saving || !form.name.trim()" @click="saveBuilder">
            {{ saving ? 'Saving…' : 'Save' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '../../api/client.js'

const items = ref([])
const loading = ref(false)
const error = ref('')

const builderOpen = ref(false)
const builderError = ref('')
const saving = ref(false)
const editingId = ref('')
const form = ref({ name: '', sections: [] })

async function load() {
  loading.value = true
  error.value = ''
  try {
    const res = await api.get('/v1/admin/partner-datasheet-definitions')
    items.value = res.data.items ?? []
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to load datasheets'
  } finally {
    loading.value = false
  }
}

function openBuilder(d) {
  editingId.value = d?.partnerDatasheetDefinitionId ?? ''
  form.value = {
    name: d?.name ?? '',
    sections: (d?.sections ?? []).map(s => ({
      id: s.id,
      title: s.title,
      kind: s.kind,
      fields: s.fields.map(f => ({
        id: f.id, label: f.label, type: f.type,
        optionsText: f.optionsText ?? '', isRequired: !!f.isRequired,
      })),
    })),
  }
  builderError.value = ''
  builderOpen.value = true
}

async function saveBuilder() {
  if (saving.value) return
  saving.value = true
  builderError.value = ''
  try {
    let defId = editingId.value
    if (!defId) {
      const res = await api.post('/v1/admin/partner-datasheet-definitions', { name: form.value.name.trim() })
      defId = res.data.partnerDatasheetDefinitionId
    } else {
      await api.patch(`/v1/admin/partner-datasheet-definitions/${defId}`, { name: form.value.name.trim() })
    }
    await api.put(`/v1/admin/partner-datasheet-definitions/${defId}/structure`, {
      sections: form.value.sections
        .filter(s => s.title.trim())
        .map(s => ({
          id: s.id,
          title: s.title.trim(),
          kind: s.kind,
          fields: s.fields
            .filter(f => f.label.trim())
            .map(f => ({
              id: f.id, label: f.label.trim(), type: f.type,
              optionsText: f.type === 'select' ? f.optionsText : null,
              isRequired: !!f.isRequired,
            })),
        })),
    })
    builderOpen.value = false
    await load()
  } catch (e) {
    builderError.value = e.response?.data?.error ?? e.message ?? 'Save failed'
  } finally {
    saving.value = false
  }
}

async function removeDef(d) {
  if (!confirm(`Delete the datasheet template "${d.name}"?`)) return
  error.value = ''
  try {
    await api.delete(`/v1/admin/partner-datasheet-definitions/${d.partnerDatasheetDefinitionId}`)
    await load()
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Delete failed'
  }
}

onMounted(load)
</script>

<style scoped>
.pds-wrap { background: #fff; border-radius: 8px; padding: 1.25rem 1.5rem; box-shadow: 0 1px 3px rgba(0,0,0,0.07); }
.pds-head { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; margin-bottom: .75rem; }
.pds-title { margin: 0; font-size: 1.15rem; color: #003366; }
.pds-sub { font-size: .78rem; color: #6b7888; margin: .2rem 0 0; }
.pds-name { font-weight: 600; color: #1a2d4f; }
.pds-muted { color: #9aa5b1; }
.pds-chip { display: inline-block; background: #eef3fa; border: 1px solid #d5e0ee; color: #2c3e50; border-radius: 12px; padding: .1rem .55rem; font-size: .72rem; margin: .1rem .25rem .1rem 0; }
.pds-chip em { font-style: normal; color: #6b7888; }
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
.pds-backdrop { position: fixed; inset: 0; background: rgba(20,30,50,.55); z-index: 1200; display: flex; align-items: center; justify-content: center; padding: 1rem; }
.pds-dialog { background: #fff; border-radius: 8px; width: min(760px, 100%); max-height: 92vh; display: flex; flex-direction: column; box-shadow: 0 10px 40px rgba(0,0,0,.2); }
.pds-dialog-head { display: flex; justify-content: space-between; align-items: center; padding: .85rem 1.1rem; border-bottom: 1px solid #e6ebf2; }
.pds-dialog-head h3 { margin: 0; font-size: 1rem; color: #1a2d4f; }
.pds-x { background: none; border: none; font-size: 1rem; cursor: pointer; color: #6b7888; }
.pds-dialog-body { padding: 1rem 1.1rem; overflow-y: auto; }
.pds-dialog-foot { display: flex; justify-content: flex-end; gap: .5rem; padding: .75rem 1.1rem; border-top: 1px solid #e6ebf2; }
.pds-lbl { display: block; font-size: .75rem; font-weight: 700; color: #44536a; margin-bottom: .25rem; }
.pds-inp { padding: .4rem .55rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .85rem; width: 100%; background: #fff; }
.pds-section { border: 1px solid #e2e8f0; border-radius: 7px; padding: .7rem; margin-top: .8rem; background: #fafbfd; }
.pds-section-head { display: flex; gap: .4rem; align-items: center; margin-bottom: .5rem; }
.pds-field-row { display: flex; gap: .4rem; align-items: center; margin-bottom: .35rem; flex-wrap: wrap; }
.pds-req { display: flex; align-items: center; gap: .25rem; font-size: .72rem; color: #44536a; white-space: nowrap; }
.pds-opts { flex-basis: 100%; margin-left: .1rem; }
.pds-add-section { margin-top: .8rem; }
</style>
