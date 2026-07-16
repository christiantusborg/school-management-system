<template>
  <div class="schools-root">
    <div class="crud-header">
      <div>
        <h2 class="crud-title">{{ title }}</h2>
        <p class="crud-sub" v-if="!loading">{{ activeCount }} item{{ activeCount !== 1 ? 's' : '' }}</p>
      </div>
      <div class="head-actions">
        <label class="show-deleted"><input type="checkbox" v-model="showDeleted" @change="load" /> Show deleted</label>
        <button class="btn-primary" @click="openCreate">+ Add {{ singular }}</button>
      </div>
    </div>

    <div v-if="error" class="crud-error">{{ error }}</div>

    <div class="table-wrap">
      <table class="data-table">
        <thead>
          <tr><th>Name</th><th>Order</th><th>Actions</th></tr>
        </thead>
        <tbody>
          <tr v-if="loading"><td colspan="3" class="empty-row">Loading…</td></tr>
          <tr v-else-if="items.length === 0"><td colspan="3" class="empty-row">No items yet.</td></tr>
          <tr v-for="c in items" :key="c[idKey]" class="data-row" :class="{ 'row-deleted': c.deletedAt }">
            <td><strong>{{ c.name }}</strong><span v-if="c.deletedAt" class="del-badge">deleted</span></td>
            <td>{{ c.displayOrder }}</td>
            <td class="actions-cell">
              <template v-if="!c.deletedAt">
                <button class="btn-action" @click="openEdit(c)">Edit</button>
                <button class="btn-action btn-action-danger" @click="confirmDelete = c">Delete</button>
              </template>
              <button v-else class="btn-action" @click="restore(c)">Restore</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <template v-if="showForm">
      <div class="overlay" @click="showForm = false"></div>
      <div class="drawer">
        <div class="drawer-header">
          <h2>{{ editTarget ? 'Edit' : 'Add' }} {{ singular }}</h2>
          <button class="drawer-close" @click="showForm = false">✕</button>
        </div>
        <div class="drawer-form">
          <div class="field">
            <label>Name <span class="req">*</span></label>
            <input v-model="form.name" maxlength="120" />
          </div>
          <div class="field">
            <label>Display order</label>
            <input v-model.number="form.displayOrder" type="number" min="0" />
          </div>
          <p v-if="formError" class="form-error">{{ formError }}</p>
          <div class="drawer-actions">
            <button class="btn-cancel" @click="showForm = false">Cancel</button>
            <button class="btn-save" :disabled="saving" @click="save">{{ saving ? 'Saving…' : 'Save' }}</button>
          </div>
        </div>
      </div>
    </template>

    <template v-if="confirmDelete">
      <div class="overlay" @click="confirmDelete = null"></div>
      <div class="confirm-modal">
        <p class="confirm-msg">Delete <strong>{{ confirmDelete.name }}</strong>? It will be soft-deleted and can be restored.</p>
        <div class="confirm-actions">
          <button class="btn-cancel" @click="confirmDelete = null">Cancel</button>
          <button class="btn-danger" @click="doDelete(confirmDelete)">Delete</button>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import api from '../../api/client.js'

const props = defineProps({
  title: { type: String, required: true },     // e.g. "Position Functions"
  singular: { type: String, required: true },  // e.g. "Position Function"
  endpoint: { type: String, required: true },  // e.g. "/v1/school/position-functions"
  idKey: { type: String, required: true },      // e.g. "positionFunctionId"
})

const items = ref([])
const loading = ref(false)
const error = ref(null)
const showDeleted = ref(false)
const showForm = ref(false)
const editTarget = ref(null)
const form = reactive({ name: '', displayOrder: 0 })
const formError = ref(null)
const saving = ref(false)
const confirmDelete = ref(null)

const activeCount = computed(() => items.value.filter(c => !c.deletedAt).length)

async function load() {
  loading.value = true
  error.value = null
  try {
    const res = await api.get(`${props.endpoint}?includeDeleted=${showDeleted.value}`)
    items.value = res.data.items ?? []
  } catch (e) {
    error.value = e.response?.data?.message ?? e.message ?? 'Failed to load'
    items.value = []
  } finally {
    loading.value = false
  }
}

function openCreate() {
  editTarget.value = null
  form.name = ''; form.displayOrder = 0
  formError.value = null
  showForm.value = true
}
function openEdit(c) {
  editTarget.value = c
  form.name = c.name ?? ''; form.displayOrder = c.displayOrder ?? 0
  formError.value = null
  showForm.value = true
}

async function save() {
  if (!form.name.trim()) { formError.value = 'Name is required'; return }
  saving.value = true; formError.value = null
  try {
    const body = { name: form.name.trim(), displayOrder: Number(form.displayOrder) || 0 }
    if (editTarget.value) await api.put(`${props.endpoint}/${editTarget.value[props.idKey]}`, body)
    else await api.post(props.endpoint, body)
    showForm.value = false
    await load()
  } catch (e) {
    formError.value = e.response?.data?.message ?? e.message ?? 'Save failed'
  } finally {
    saving.value = false
  }
}

async function doDelete(c) {
  confirmDelete.value = null
  error.value = null
  try { await api.delete(`${props.endpoint}/${c[props.idKey]}`); await load() }
  catch (e) { error.value = e.response?.data?.message ?? e.message ?? 'Delete failed' }
}

async function restore(c) {
  error.value = null
  try { await api.post(`${props.endpoint}/${c[props.idKey]}/restore`); await load() }
  catch (e) { error.value = e.response?.data?.message ?? e.message ?? 'Restore failed' }
}

onMounted(load)
</script>

<style scoped>
.crud-header { display: flex; align-items: flex-start; justify-content: space-between; margin-bottom: 1.25rem; }
.crud-title { font-size: 1.2rem; font-weight: 700; color: #003366; margin: 0; }
.crud-sub { font-size: 0.82rem; color: #888; margin: 0.2rem 0 0; }
.head-actions { display: flex; align-items: center; gap: 1rem; }
.show-deleted { display: flex; align-items: center; gap: .35rem; font-size: .82rem; color: #555; cursor: pointer; }
.crud-error { background: #fef2f2; border: 1.5px solid #fca5a5; border-radius: 7px; padding: 0.65rem 1rem; color: #b91c1c; font-size: 0.86rem; margin-bottom: 1rem; }
.table-wrap { background: #fff; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.07); overflow: auto; }
.data-table { width: 100%; border-collapse: collapse; font-size: 0.88rem; }
.data-table th { text-align: left; padding: 0.75rem 1rem; font-size: 0.74rem; text-transform: uppercase; letter-spacing: 0.05em; color: #666; border-bottom: 2px solid #e8edf4; background: #fafbfc; white-space: nowrap; }
.data-row td { padding: 0.72rem 1rem; border-bottom: 1px solid #f0f3f7; }
.data-row:hover td { background: #f7f9fb; }
.row-deleted td { opacity: .6; }
.del-badge { margin-left: .5rem; font-size: .68rem; background: #fee2e2; color: #b91c1c; padding: .1rem .4rem; border-radius: 4px; }
.empty-row { text-align: center; padding: 2.5rem !important; color: #aaa; font-style: italic; }
.actions-cell { white-space: nowrap; }
.btn-action { padding: 0.28rem 0.7rem; font-size: 0.8rem; border: 1.5px solid #d0d7e0; border-radius: 5px; background: #f7f9fb; color: #333; cursor: pointer; margin-right: 0.4rem; }
.btn-action:hover { background: #e8f0f8; border-color: #a0b8d0; }
.btn-action-danger { color: #b91c1c; border-color: #fca5a5; }
.btn-action-danger:hover { background: #fef2f2; }
.btn-primary { background: #003366; color: #fff; border: none; border-radius: 7px; padding: 0.6rem 1.25rem; font-size: 0.9rem; font-weight: 600; cursor: pointer; }
.btn-primary:hover { background: #0055a5; }
.overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.38); z-index: 100; }
.drawer { position: fixed; top: 0; right: 0; bottom: 0; width: 400px; max-width: 95vw; background: #fff; z-index: 101; display: flex; flex-direction: column; box-shadow: -4px 0 24px rgba(0,0,0,0.15); }
.drawer-header { display: flex; align-items: center; justify-content: space-between; padding: 1.2rem 1.5rem; border-bottom: 1.5px solid #e8edf4; }
.drawer-header h2 { font-size: 1.05rem; font-weight: 700; color: #003366; margin: 0; }
.drawer-close { background: none; border: none; font-size: 1.1rem; color: #888; cursor: pointer; }
.drawer-form { flex: 1; overflow-y: auto; padding: 1.2rem 1.5rem; display: flex; flex-direction: column; gap: 0.9rem; }
.field { display: flex; flex-direction: column; gap: 0.35rem; }
.field label { font-size: 0.82rem; font-weight: 600; color: #444; }
.req { color: #c0392b; }
.field input { padding: 0.58rem 0.75rem; border: 1.5px solid #ccc; border-radius: 7px; font-size: 0.9rem; font-family: inherit; outline: none; }
.field input:focus { border-color: #0055a5; }
.form-error { background: #fef2f2; border: 1.5px solid #fca5a5; border-radius: 6px; padding: 0.5rem 0.75rem; color: #b91c1c; font-size: 0.84rem; margin: 0; }
.drawer-actions { display: flex; gap: 0.75rem; justify-content: flex-end; padding-top: 0.5rem; }
.btn-cancel { padding: 0.62rem 1.2rem; background: #f2f5f9; border: 1.5px solid #ccc; border-radius: 7px; font-size: 0.9rem; cursor: pointer; color: #555; }
.btn-save { padding: 0.62rem 1.4rem; background: #003366; color: #fff; border: none; border-radius: 7px; font-size: 0.9rem; font-weight: 600; cursor: pointer; }
.btn-save:disabled { opacity: 0.6; cursor: not-allowed; }
.confirm-modal { position: fixed; top: 50%; left: 50%; transform: translate(-50%, -50%); width: 380px; max-width: 92vw; background: #fff; z-index: 101; border-radius: 10px; box-shadow: 0 8px 32px rgba(0,0,0,0.2); padding: 1.5rem; }
.confirm-msg { font-size: 0.95rem; color: #333; margin: 0 0 1.25rem; line-height: 1.5; }
.confirm-actions { display: flex; gap: 0.75rem; justify-content: flex-end; }
.btn-danger { padding: 0.62rem 1.2rem; background: #dc2626; color: #fff; border: none; border-radius: 7px; font-size: 0.9rem; font-weight: 600; cursor: pointer; }
.btn-danger:hover { background: #b91c1c; }
</style>
