<template>
  <div>
    <div class="lt-head">
      <div>
        <h2 class="lt-title">Letter Types</h2>
        <p class="lt-sub">
          Config-created letters: give a name, pick the student status that generates the letter
          automatically (once, the first time it is reached), and design the template per programme +
          partner in the Letters row. The built-in letters (Offer, Admission, Transcript…) are not
          affected. Every generation stores a version — history shows on the student drawer.
        </p>
      </div>
      <button class="btn-primary-sm" @click="openCreate">+ Add letter type</button>
    </div>

    <div v-if="error" class="err-banner">{{ error }}</div>
    <div v-if="loading" class="loading-row">Loading…</div>

    <table v-else-if="items.length" class="data-table">
      <thead>
        <tr>
          <th>Name</th><th>Prefix</th><th>Generates on status</th>
          <th>Student</th><th>Partner</th><th>Email</th><th>Old upload</th><th>Order</th><th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="d in items" :key="d.letterTypeDefinitionId">
          <td><strong>{{ d.name }}</strong></td>
          <td><code>MGW-{{ d.referencePrefix }}-…</code></td>
          <td>{{ d.triggerStatusName || 'Manual only' }}</td>
          <td>{{ d.visibleToStudent ? '✓' : '—' }}</td>
          <td>{{ d.visibleToPartner ? '✓' : '—' }}</td>
          <td :title="d.emailOnRelease ? 'Email sending for config-created letters ships with the email-template phase' : ''">
            {{ d.emailOnRelease ? '✓*' : '—' }}</td>
          <td>{{ d.allowLegacyUpload ? '✓' : '—' }}</td>
          <td>{{ d.sortOrder }}</td>
          <td class="lt-actions">
            <button class="btn-sm" @click="openEdit(d)">✎ Edit</button>
            <button class="btn-sm btn-danger" @click="confirmDelete = d">✕</button>
          </td>
        </tr>
      </tbody>
    </table>
    <p v-else class="lt-sub">No letter types yet. Add the first one.</p>
    <p v-if="items.some(d => d.emailOnRelease)" class="lt-sub" style="margin-top:.4rem;">
      * Email on release is saved but not sent yet for config-created letters — the email editor for
      these types arrives with the letter-email phase.
    </p>

    <div v-if="showForm" class="lt-overlay" @click.self="showForm = false">
      <div class="lt-dialog">
        <h3 style="margin:0 0 .8rem;">{{ editTarget ? 'Edit letter type' : 'New letter type' }}</h3>
        <div class="lt-grid">
          <div style="grid-column: 1 / -1;">
            <label class="lt-lbl">Name *</label>
            <input v-model="form.name" class="lt-inp" placeholder="e.g. Completion Confirmation Letter" />
          </div>
          <div>
            <label class="lt-lbl">Reference prefix</label>
            <input v-model="form.referencePrefix" class="lt-inp" placeholder="auto from name"
                   style="text-transform:uppercase;" maxlength="8" />
            <div class="lt-hint">Printed as MGW-{{ (form.referencePrefix || 'XX').toUpperCase() }}-A1B2C3D4</div>
          </div>
          <div>
            <label class="lt-lbl">Generate automatically at status</label>
            <select v-model="form.triggerStatusId" class="lt-inp">
              <option :value="null">Manual only</option>
              <option v-for="s in statuses" :key="s.statusId" :value="s.statusId">{{ s.name }}</option>
            </select>
            <div class="lt-hint">Fires once, the first time the enrolment reaches the status.</div>
          </div>
          <label class="lt-check"><input type="checkbox" v-model="form.visibleToStudent" /> Visible to student</label>
          <label class="lt-check"><input type="checkbox" v-model="form.visibleToPartner" /> Visible to partner</label>
          <label class="lt-check"><input type="checkbox" v-model="form.emailOnRelease" /> Email letter on release</label>
          <label class="lt-check"><input type="checkbox" v-model="form.allowLegacyUpload" /> Allow upload of old letter</label>
          <div>
            <label class="lt-lbl">Sort order</label>
            <input v-model.number="form.sortOrder" type="number" class="lt-inp" style="width:100px;" />
          </div>
        </div>
        <div v-if="formError" class="err-banner" style="margin-top:.6rem;">{{ formError }}</div>
        <div class="lt-foot">
          <button class="btn-sm" @click="showForm = false">Cancel</button>
          <button class="btn-primary-sm" :disabled="saving || !form.name.trim()" @click="save">
            {{ saving ? 'Saving…' : 'Save' }}</button>
        </div>
      </div>
    </div>

    <div v-if="confirmDelete" class="lt-overlay" @click.self="confirmDelete = null">
      <div class="lt-dialog" style="max-width:420px;">
        <h3 style="margin:0 0 .6rem;">Delete "{{ confirmDelete.name }}"?</h3>
        <p class="lt-sub">Already-released letters and their version history stay downloadable;
          the type just disappears from the letters surfaces and stops triggering.</p>
        <div class="lt-foot">
          <button class="btn-sm" @click="confirmDelete = null">Cancel</button>
          <button class="btn-sm btn-danger" @click="doDelete">Delete</button>
        </div>
      </div>
    </div>

    <div style="margin-top:2rem;">
      <SimpleListManager
        title="Letter Languages" singular="Letter Language"
        endpoint="/v1/admin/letter-languages" id-key="letterLanguageId" />
      <p class="lt-sub" style="margin-top:.4rem;">
        English is the built-in default for every letter and is not listed here. Each language added
        lets a template carry an extra version; a missing translation falls back to English.
      </p>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import api from '../../api/client.js'
import SimpleListManager from './SimpleListManager.vue'

const items = ref([])
const statuses = ref([])
const loading = ref(false)
const error = ref('')
const showForm = ref(false)
const editTarget = ref(null)
const saving = ref(false)
const formError = ref('')
const confirmDelete = ref(null)

const form = reactive({
  name: '', referencePrefix: '', triggerStatusId: null,
  visibleToStudent: true, visibleToPartner: true,
  emailOnRelease: false, allowLegacyUpload: false, sortOrder: 0,
})

async function load() {
  loading.value = true; error.value = ''
  try {
    const [defs, sts] = await Promise.all([
      api.get('/v1/admin/letter-type-definitions'),
      api.get('/v1/admin/enrollment-statuses'),
    ])
    items.value = defs.data.items ?? []
    statuses.value = sts.data.items ?? []
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to load'
  } finally {
    loading.value = false
  }
}
onMounted(load)

function openCreate() {
  editTarget.value = null
  Object.assign(form, {
    name: '', referencePrefix: '', triggerStatusId: null,
    visibleToStudent: true, visibleToPartner: true,
    emailOnRelease: false, allowLegacyUpload: false,
    sortOrder: (items.value.at(-1)?.sortOrder ?? 0) + 1,
  })
  formError.value = ''; showForm.value = true
}
function openEdit(d) {
  editTarget.value = d
  Object.assign(form, {
    name: d.name, referencePrefix: d.referencePrefix, triggerStatusId: d.triggerStatusId,
    visibleToStudent: d.visibleToStudent, visibleToPartner: d.visibleToPartner,
    emailOnRelease: d.emailOnRelease, allowLegacyUpload: d.allowLegacyUpload, sortOrder: d.sortOrder,
  })
  formError.value = ''; showForm.value = true
}

async function save() {
  if (saving.value) return
  saving.value = true; formError.value = ''
  try {
    const body = { ...form, name: form.name.trim(), referencePrefix: form.referencePrefix.trim() }
    if (editTarget.value)
      await api.put(`/v1/admin/letter-type-definitions/${editTarget.value.letterTypeDefinitionId}`, body)
    else
      await api.post('/v1/admin/letter-type-definitions', body)
    showForm.value = false
    await load()
  } catch (e) {
    formError.value = e.response?.data?.error ?? e.message ?? 'Save failed'
  } finally {
    saving.value = false
  }
}

async function doDelete() {
  const d = confirmDelete.value
  confirmDelete.value = null
  try {
    await api.delete(`/v1/admin/letter-type-definitions/${d.letterTypeDefinitionId}`)
    await load()
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Delete failed'
  }
}
</script>

<style scoped>
.lt-head { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; margin-bottom: 1rem; }
.lt-title { margin: 0 0 .3rem; font-size: 1.15rem; color: #003366; }
.lt-sub { color: #6b7888; font-size: .82rem; margin: 0; max-width: 720px; }
.lt-actions { white-space: nowrap; display: flex; gap: .3rem; }
.lt-overlay { position: fixed; inset: 0; background: rgba(15,30,50,.45); display: flex; align-items: center; justify-content: center; z-index: 60; }
.lt-dialog { background: #fff; border-radius: 10px; padding: 1.2rem 1.4rem; width: min(640px, 92vw); max-height: 90vh; overflow: auto; }
.lt-grid { display: grid; grid-template-columns: 1fr 1fr; gap: .7rem .9rem; align-items: start; }
.lt-lbl { display: block; font-size: .74rem; font-weight: 700; color: #4b5a6d; margin-bottom: .2rem; text-transform: uppercase; letter-spacing: .04em; }
.lt-inp { width: 100%; padding: .4rem .55rem; border: 1px solid #ccd5e0; border-radius: 6px; font-size: .88rem; box-sizing: border-box; }
.lt-hint { font-size: .72rem; color: #8a97a7; margin-top: .2rem; }
.lt-check { display: flex; align-items: center; gap: .4rem; font-size: .86rem; color: #2c3e50; padding-top: 1.1rem; }
.lt-foot { display: flex; justify-content: flex-end; gap: .5rem; margin-top: 1rem; }
</style>
