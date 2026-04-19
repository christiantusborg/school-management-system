<template>
  <div class="crud-root">
    <div v-if="schemaLoading" class="crud-info">Loading…</div>
    <div v-else-if="schemaError" class="crud-error">Schema error: {{ schemaError }}</div>

    <template v-else>
      <!-- Header -->
      <div class="crud-header">
        <div>
          <h2 class="crud-title">{{ config.title }}</h2>
          <p class="crud-sub" v-if="!loading">{{ items.length }} item{{ items.length !== 1 ? 's' : '' }}</p>
        </div>
        <button class="btn-primary" @click="openCreate">+ Add New</button>
      </div>

      <!-- Data error -->
      <div v-if="error" class="crud-error">{{ error }}</div>

      <!-- Table -->
      <div class="table-wrap">
        <table class="data-table">
          <thead>
            <tr>
              <th v-for="col in derivedColumns" :key="col.key">{{ col.label }}</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="loading">
              <td :colspan="derivedColumns.length + 1" class="empty-row">Loading…</td>
            </tr>
            <tr v-else-if="items.length === 0">
              <td :colspan="derivedColumns.length + 1" class="empty-row">No items yet.</td>
            </tr>
            <template v-else>
              <tr v-for="item in items" :key="item[derivedIdKey]" class="data-row">
                <td v-for="col in derivedColumns" :key="col.key">
                  <template v-if="col.type === 'checkbox'">{{ item[col.key] ? 'Yes' : 'No' }}</template>
                  <template v-else>{{ item[col.key] ?? '—' }}</template>
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

      <!-- Create / Edit drawer -->
      <template v-if="showForm">
        <div class="overlay" @click="closeForm"></div>
        <div class="drawer">
          <div class="drawer-header">
            <h2>{{ editTarget ? 'Edit' : 'Add' }} {{ singularTitle }}</h2>
            <button class="drawer-close" @click="closeForm">✕</button>
          </div>
          <div class="drawer-form">
            <div v-for="field in derivedFields" :key="field.key" class="field">
              <label>
                {{ field.label }}
                <span v-if="field.required" class="req"> *</span>
              </label>
              <textarea v-if="field.type === 'textarea'" v-model="formData[field.key]" rows="3"></textarea>
              <label v-else-if="field.type === 'checkbox'" class="checkbox-label">
                <input type="checkbox" v-model="formData[field.key]" />
                {{ formData[field.key] ? 'Yes' : 'No' }}
              </label>
              <input v-else :type="field.type === 'number' ? 'number' : 'text'" v-model="formData[field.key]" />
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

      <!-- Delete confirmation -->
      <template v-if="confirmDelete">
        <div class="overlay" @click="confirmDelete = null"></div>
        <div class="confirm-modal">
          <p class="confirm-msg">
            Delete <strong>{{ confirmDelete[derivedColumns[0]?.key] }}</strong>?
            This will soft-delete the record.
          </p>
          <div class="confirm-actions">
            <button class="btn-cancel" @click="confirmDelete = null">Cancel</button>
            <button class="btn-danger" @click="doDelete(confirmDelete)">Delete</button>
          </div>
        </div>
      </template>
    </template>
  </div>
</template>

<script setup>
import { ref, reactive, computed, watch, onMounted } from 'vue'
import api from '../../api/client.js'

const props = defineProps({
  config: { type: Object, required: true }, // { title, endpoint }
})

// ── Schema (from OPTIONS) ──────────────────────────────────────────────────
const schemaLoading = ref(true)
const schemaError = ref(null)
const derivedIdKey = ref(null)
const derivedColumns = ref([])
const derivedFields = ref([])

const singularTitle = computed(() =>
  props.config.title.replace(/s$/i, '')
)

async function fetchSchema() {
  schemaLoading.value = true
  schemaError.value = null
  try {
    const res = await api.options(props.config.endpoint)
    parseSchema(res.data)
  } catch (e) {
    schemaError.value = e.response?.data?.message ?? e.message ?? 'Failed to load schema'
  } finally {
    schemaLoading.value = false
  }
}

function parseSchema(schema) {
  const listItemProps = schema?.list?.result?.properties?.items?.items?.properties ?? {}

  derivedIdKey.value = Object.keys(listItemProps).find(k => /id$/i.test(k)) ?? null

  const SKIP = new Set([derivedIdKey.value, 'links', 'deletedAt'].filter(Boolean))
  derivedColumns.value = Object.keys(listItemProps)
    .filter(k => !SKIP.has(k))
    .map(k => ({ key: k, label: toTitleCase(k), type: inferType(listItemProps[k], k) }))

  const createReq = schema?.create?.request ?? {}
  const required = new Set(createReq.required ?? [])
  derivedFields.value = Object.entries(createReq.properties ?? {}).map(([key, s]) => ({
    key,
    label: toTitleCase(key),
    type: inferType(s, key),
    required: required.has(key),
  }))
}

function inferType(schema, key) {
  if (!schema) return 'text'
  if (schema.oneOf) {
    const nonNull = schema.oneOf.find(s => s.type !== 'null')
    return inferType(nonNull, key)
  }
  if (schema.type === 'boolean') return 'checkbox'
  if (schema.type === 'integer' || schema.type === 'number') return 'number'
  if (schema.format === 'date-time') return 'date'
  if (schema.type === 'string' && /description|notes|comment/i.test(key)) return 'textarea'
  return 'text'
}

function toTitleCase(camel) {
  return camel.replace(/([A-Z])/g, ' $1').replace(/^./, c => c.toUpperCase()).trim()
}

// ── Data CRUD ───────────────────────────────────────────────────────────────
const items = ref([])
const loading = ref(false)
const error = ref(null)
const showForm = ref(false)
const editTarget = ref(null)
const formData = reactive({})
const formError = ref(null)
const saving = ref(false)
const confirmDelete = ref(null)

async function fetchAll() {
  loading.value = true
  error.value = null
  try {
    const res = await api.get(props.config.endpoint)
    items.value = res.data.items ?? []
  } catch (e) {
    error.value = e.response?.data?.message ?? e.message ?? 'Failed to load'
    items.value = []
  } finally {
    loading.value = false
  }
}

function defaultFor(f) {
  return f.type === 'checkbox' ? false : ''
}

function openCreate() {
  editTarget.value = null
  derivedFields.value.forEach(f => { formData[f.key] = defaultFor(f) })
  formError.value = null
  showForm.value = true
}

function openEdit(item) {
  editTarget.value = item
  derivedFields.value.forEach(f => { formData[f.key] = item[f.key] ?? defaultFor(f) })
  formError.value = null
  showForm.value = true
}

function closeForm() { showForm.value = false }

async function save() {
  for (const f of derivedFields.value) {
    if (f.required && f.type !== 'checkbox' && !formData[f.key]?.toString().trim()) {
      formError.value = `${f.label} is required`
      return
    }
  }
  const body = {}
  derivedFields.value.forEach(f => {
    if (f.type === 'checkbox') body[f.key] = !!formData[f.key]
    else body[f.key] = formData[f.key]?.toString().trim() || null
  })

  saving.value = true
  formError.value = null
  try {
    if (editTarget.value) {
      await api.put(`${props.config.endpoint}/${editTarget.value[derivedIdKey.value]}`, body)
    } else {
      await api.post(props.config.endpoint, body)
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
  const id = item[derivedIdKey.value]
  confirmDelete.value = null
  error.value = null
  try {
    await api.delete(`${props.config.endpoint}/${id}`)
    await fetchAll()
  } catch (e) {
    error.value = e.response?.data?.message ?? e.message ?? 'Delete failed'
  }
}

watch(() => props.config.endpoint, async () => {
  await fetchSchema()
  await fetchAll()
})

onMounted(async () => {
  await fetchSchema()
  await fetchAll()
})
</script>

<style scoped>
.crud-root { }
.crud-info { color: #888; font-style: italic; padding: 1rem 0; }

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
.data-row td { padding: 0.72rem 1rem; border-bottom: 1px solid #f0f3f7; }
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
  background: #003366; color: #fff; border: none; border-radius: 7px;
  padding: 0.6rem 1.25rem; font-size: 0.9rem; font-weight: 600; cursor: pointer;
}
.btn-primary:hover { background: #0055a5; }

.overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.38); z-index: 100; }
.drawer {
  position: fixed; top: 0; right: 0; bottom: 0; width: 400px; max-width: 95vw;
  background: #fff; z-index: 101; display: flex; flex-direction: column;
  box-shadow: -4px 0 24px rgba(0,0,0,0.15);
}
.drawer-header {
  display: flex; align-items: center; justify-content: space-between;
  padding: 1.2rem 1.5rem; border-bottom: 1.5px solid #e8edf4; flex-shrink: 0;
}
.drawer-header h2 { font-size: 1.05rem; font-weight: 700; color: #003366; margin: 0; }
.drawer-close { background: none; border: none; font-size: 1.1rem; color: #888; cursor: pointer; }
.drawer-close:hover { color: #333; }
.drawer-form {
  flex: 1; overflow-y: auto; padding: 1.2rem 1.5rem; display: flex; flex-direction: column; gap: 0.9rem;
}

.field { display: flex; flex-direction: column; gap: 0.35rem; }
.field label { font-size: 0.82rem; font-weight: 600; color: #444; }
.req { color: #c0392b; }
.checkbox-label { display: flex; align-items: center; gap: 0.5rem; font-size: 0.9rem; cursor: pointer; font-weight: normal; }
.checkbox-label input[type="checkbox"] { width: 1rem; height: 1rem; cursor: pointer; }

.field input, .field textarea {
  padding: 0.58rem 0.75rem; border: 1.5px solid #ccc; border-radius: 7px;
  font-size: 0.9rem; font-family: inherit; outline: none; resize: vertical;
}
.field input:focus, .field textarea:focus { border-color: #0055a5; }

.form-error {
  background: #fef2f2; border: 1.5px solid #fca5a5; border-radius: 6px;
  padding: 0.5rem 0.75rem; color: #b91c1c; font-size: 0.84rem; margin: 0;
}
.drawer-actions { display: flex; gap: 0.75rem; justify-content: flex-end; padding-top: 0.5rem; }
.btn-cancel {
  padding: 0.62rem 1.2rem; background: #f2f5f9; border: 1.5px solid #ccc;
  border-radius: 7px; font-size: 0.9rem; cursor: pointer; color: #555;
}
.btn-save {
  padding: 0.62rem 1.4rem; background: #003366; color: #fff; border: none;
  border-radius: 7px; font-size: 0.9rem; font-weight: 600; cursor: pointer;
}
.btn-save:hover:not(:disabled) { background: #0055a5; }
.btn-save:disabled { opacity: 0.6; cursor: not-allowed; }

.confirm-modal {
  position: fixed; top: 50%; left: 50%; transform: translate(-50%, -50%);
  width: 360px; max-width: 92vw; background: #fff; z-index: 101;
  border-radius: 10px; box-shadow: 0 8px 32px rgba(0,0,0,0.2); padding: 1.5rem;
}
.confirm-msg { font-size: 0.95rem; color: #333; margin: 0 0 1.25rem; line-height: 1.5; }
.confirm-actions { display: flex; gap: 0.75rem; justify-content: flex-end; }
.btn-danger {
  padding: 0.62rem 1.2rem; background: #dc2626; color: #fff; border: none;
  border-radius: 7px; font-size: 0.9rem; font-weight: 600; cursor: pointer;
}
.btn-danger:hover { background: #b91c1c; }
</style>
