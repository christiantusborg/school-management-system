<template>
  <div class="crud-root">
    <div class="crud-header">
      <div>
        <h2 class="crud-title">Pathways</h2>
        <p class="crud-sub" v-if="!loading">{{ items.length }} item{{ items.length !== 1 ? 's' : '' }}</p>
      </div>
      <button class="btn-primary" @click="openCreate">+ Add New</button>
    </div>

    <div v-if="error" class="crud-error">{{ error }}</div>

    <div class="table-wrap">
      <table class="data-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Required Documents</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="loading">
            <td colspan="3" class="empty-row">Loading…</td>
          </tr>
          <tr v-else-if="items.length === 0">
            <td colspan="3" class="empty-row">No pathways yet.</td>
          </tr>
          <template v-else>
            <tr v-for="item in items" :key="item.pathwayId" class="data-row">
              <td>{{ item.name }}</td>
              <td>
                <span v-if="!docsByPathway[item.pathwayId]?.length" class="muted">—</span>
                <span v-else class="doc-tag" v-for="dt in docsByPathway[item.pathwayId]" :key="dt">
                  {{ documentTypeName(dt) }}
                </span>
              </td>
              <td class="actions-cell">
                <button class="btn-action" @click="openEdit(item)">Edit</button>
                <button class="btn-action btn-action-danger" @click="confirmDelete = item">Delete</button>
              </td>
            </tr>
          </template>
        </tbody>
      </table>
    </div>

    <template v-if="showForm">
      <div class="overlay" @click="closeForm"></div>
      <div class="drawer">
        <div class="drawer-header">
          <h2>{{ editTarget ? 'Edit' : 'Add' }} Pathway</h2>
          <button class="drawer-close" @click="closeForm">✕</button>
        </div>
        <div class="drawer-form">
          <div class="field">
            <label>Name <span class="req">*</span></label>
            <input v-model="formName" placeholder="e.g. Pathway 1: Direct Entry" />
          </div>
          <div class="field">
            <label>Required Documents</label>
            <div class="doc-grid">
              <label v-for="dt in documentTypes" :key="dt.documentTypeId" class="doc-row">
                <input type="checkbox"
                       :checked="formDocIds.includes(dt.documentTypeId)"
                       @change="toggleDoc(dt.documentTypeId)" />
                {{ dt.name }}
              </label>
              <p v-if="!documentTypes.length" class="muted">No document types defined.</p>
            </div>
          </div>
          <p v-if="formError" class="form-error">{{ formError }}</p>
          <div class="drawer-actions">
            <button class="btn-cancel" @click="closeForm">Cancel</button>
            <button class="btn-save" :disabled="saving" @click="save">
              {{ saving ? 'Saving…' : 'Save' }}
            </button>
          </div>
        </div>
      </div>
    </template>

    <template v-if="confirmDelete">
      <div class="overlay" @click="confirmDelete = null"></div>
      <div class="confirm-modal">
        <p class="confirm-msg">
          Delete <strong>{{ confirmDelete.name }}</strong>? This will soft-delete the pathway.
        </p>
        <div class="confirm-actions">
          <button class="btn-cancel" @click="confirmDelete = null">Cancel</button>
          <button class="btn-danger" @click="doDelete(confirmDelete)">Delete</button>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import api from '../../api/client.js'

const ENDPOINT = '/v1/school/system-config/pathways'
const DOC_ENDPOINT = '/v1/school/system-config/document-types'

const items = ref([])
const documentTypes = ref([])
const docsByPathway = reactive({})
const loading = ref(false)
const error = ref(null)

const showForm = ref(false)
const editTarget = ref(null)
const formName = ref('')
const formDocIds = ref([])
const formError = ref(null)
const saving = ref(false)
const confirmDelete = ref(null)

function documentTypeName(id) {
  return documentTypes.value.find(d => d.documentTypeId === id)?.name ?? `#${id}`
}

async function fetchAll() {
  loading.value = true
  error.value = null
  try {
    const [listRes, docRes] = await Promise.all([
      api.get(ENDPOINT),
      api.get(DOC_ENDPOINT),
    ])
    items.value = listRes.data.items ?? []
    documentTypes.value = docRes.data.items ?? []

    const detailReqs = items.value.map(p => api.get(`${ENDPOINT}/${p.pathwayId}`))
    const details = await Promise.all(detailReqs)
    details.forEach((res, idx) => {
      const id = items.value[idx].pathwayId
      docsByPathway[id] = res.data.documentTypeIds ?? []
    })
  } catch (e) {
    error.value = e.response?.data?.message ?? e.message ?? 'Failed to load'
    items.value = []
  } finally {
    loading.value = false
  }
}

function toggleDoc(id) {
  const idx = formDocIds.value.indexOf(id)
  if (idx >= 0) formDocIds.value.splice(idx, 1)
  else formDocIds.value.push(id)
}

function openCreate() {
  editTarget.value = null
  formName.value = ''
  formDocIds.value = []
  formError.value = null
  showForm.value = true
}

function openEdit(item) {
  editTarget.value = item
  formName.value = item.name
  formDocIds.value = [...(docsByPathway[item.pathwayId] ?? [])]
  formError.value = null
  showForm.value = true
}

function closeForm() {
  showForm.value = false
}

async function save() {
  const name = formName.value.trim()
  if (!name) {
    formError.value = 'Name is required'
    return
  }
  saving.value = true
  formError.value = null
  try {
    const body = { name, documentTypeIds: [...formDocIds.value] }
    if (editTarget.value) {
      await api.put(`${ENDPOINT}/${editTarget.value.pathwayId}`, body)
    } else {
      await api.post(ENDPOINT, body)
    }
    closeForm()
    await fetchAll()
  } catch (e) {
    formError.value = e.response?.data?.message ?? e.message ?? 'Save failed'
  } finally {
    saving.value = false
  }
}

async function doDelete(item) {
  const id = item.pathwayId
  confirmDelete.value = null
  error.value = null
  try {
    await api.delete(`${ENDPOINT}/${id}`)
    await fetchAll()
  } catch (e) {
    error.value = e.response?.data?.message ?? e.message ?? 'Delete failed'
  }
}

onMounted(fetchAll)
</script>

<style scoped>
.crud-root { }
.crud-header { display: flex; align-items: flex-start; justify-content: space-between; margin-bottom: 1.25rem; }
.crud-title { font-size: 1.2rem; font-weight: 700; color: #003366; margin: 0; }
.crud-sub { font-size: 0.82rem; color: #888; margin: 0.2rem 0 0; }

.crud-error {
  background: #fef2f2; border: 1.5px solid #fca5a5; border-radius: 7px;
  padding: 0.65rem 1rem; color: #b91c1c; font-size: 0.86rem; margin-bottom: 1rem;
}

.table-wrap { background: #fff; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.07); overflow: auto; }
.data-table { width: 100%; border-collapse: collapse; font-size: 0.88rem; }
.data-table th {
  text-align: left; padding: 0.75rem 1rem; font-size: 0.74rem; text-transform: uppercase;
  letter-spacing: 0.05em; color: #666; border-bottom: 2px solid #e8edf4; background: #fafbfc; white-space: nowrap;
}
.data-row td { padding: 0.72rem 1rem; border-bottom: 1px solid #f0f3f7; vertical-align: top; }
.data-row:last-child td { border-bottom: none; }
.data-row:hover td { background: #f7f9fb; }
.empty-row { text-align: center; padding: 2.5rem !important; color: #aaa; font-style: italic; }

.actions-cell { white-space: nowrap; }
.btn-action {
  padding: 0.28rem 0.7rem; font-size: 0.8rem; border: 1.5px solid #d0d7e0;
  border-radius: 5px; background: #f7f9fb; color: #333; cursor: pointer; margin-right: 0.4rem;
}
.btn-action:hover { background: #e8f0f8; border-color: #a0b8d0; }
.btn-action-danger { color: #b91c1c; border-color: #fca5a5; }
.btn-action-danger:hover { background: #fef2f2; }

.btn-primary {
  background: #003366; color: #fff; border: 0; padding: 0.5rem 1rem;
  border-radius: 6px; font-weight: 600; cursor: pointer;
}
.btn-primary:hover { background: #00264d; }

.doc-tag {
  display: inline-block; background: #eef4fb; color: #003366; border-radius: 4px;
  padding: 0.15rem 0.5rem; font-size: 0.78rem; margin: 0 0.3rem 0.25rem 0;
}
.muted { color: #aaa; font-style: italic; }

.overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.35); z-index: 50; }
.drawer {
  position: fixed; top: 0; right: 0; bottom: 0; width: 420px; background: #fff;
  z-index: 60; box-shadow: -4px 0 16px rgba(0,0,0,0.12); display: flex; flex-direction: column;
}
.drawer-header {
  display: flex; align-items: center; justify-content: space-between;
  padding: 1rem 1.25rem; border-bottom: 1px solid #e8edf4;
}
.drawer-header h2 { margin: 0; font-size: 1.05rem; color: #003366; }
.drawer-close { background: none; border: 0; font-size: 1.2rem; cursor: pointer; color: #888; }

.drawer-form { padding: 1rem 1.25rem; flex: 1; overflow: auto; }
.field { margin-bottom: 1rem; }
.field label { display: block; font-size: 0.85rem; font-weight: 600; color: #333; margin-bottom: 0.35rem; }
.field input[type="text"], .field input:not([type]) {
  width: 100%; padding: 0.55rem 0.7rem; border: 1.5px solid #d0d7e0; border-radius: 5px;
  font-size: 0.9rem; box-sizing: border-box;
}
.req { color: #b91c1c; }

.doc-grid { display: flex; flex-direction: column; gap: 0.35rem; max-height: 260px; overflow: auto;
            border: 1px solid #e8edf4; border-radius: 6px; padding: 0.55rem 0.75rem; }
.doc-row { display: flex; align-items: center; gap: 0.5rem; font-weight: 500; font-size: 0.88rem; cursor: pointer; }

.form-error { color: #b91c1c; font-size: 0.85rem; margin: 0.5rem 0; }
.drawer-actions { display: flex; gap: 0.5rem; justify-content: flex-end; padding-top: 0.5rem; }
.btn-cancel {
  background: #fff; color: #555; border: 1.5px solid #d0d7e0;
  padding: 0.45rem 1rem; border-radius: 5px; cursor: pointer;
}
.btn-save {
  background: #003366; color: #fff; border: 0; padding: 0.45rem 1.1rem;
  border-radius: 5px; font-weight: 600; cursor: pointer;
}
.btn-save:disabled { opacity: 0.6; cursor: default; }

.confirm-modal {
  position: fixed; top: 50%; left: 50%; transform: translate(-50%, -50%); z-index: 60;
  background: #fff; border-radius: 10px; padding: 1.5rem; box-shadow: 0 8px 32px rgba(0,0,0,0.2);
  min-width: 340px;
}
.confirm-msg { margin: 0 0 1rem; font-size: 0.95rem; }
.confirm-actions { display: flex; gap: 0.5rem; justify-content: flex-end; }
.btn-danger {
  background: #b91c1c; color: #fff; border: 0; padding: 0.45rem 1rem;
  border-radius: 5px; font-weight: 600; cursor: pointer;
}
</style>
