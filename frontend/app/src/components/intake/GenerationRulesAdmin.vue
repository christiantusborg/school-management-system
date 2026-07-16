<template>
  <div>
    <div class="d-flex justify-space-between align-center mb-4">
      <div>
        <h2 class="text-h6 font-weight-bold">Generation rules</h2>
        <p class="text-body-2 text-medium-emphasis mb-0">
          A rule inspects a response's answers and decides which document
          templates the generate step should produce.
        </p>
      </div>
      <v-btn color="primary" prepend-icon="mdi-plus" @click="openCreate">New rule</v-btn>
    </div>

    <v-alert v-if="error" type="error" density="compact" class="mb-3">{{ error }}</v-alert>

    <v-table density="comfortable">
      <thead>
        <tr><th>Name</th><th>Includes templates</th><th>Modified</th><th style="width:160px;">Actions</th></tr>
      </thead>
      <tbody>
        <tr v-if="!items.length"><td colspan="4" class="text-medium-emphasis">No rules yet.</td></tr>
        <tr v-for="r in items" :key="r.generationRuleId">
          <td>{{ r.name }}</td>
          <td>{{ includeNames(r) }}</td>
          <td>{{ fmt(r.modifiedAt) }}</td>
          <td>
            <v-btn size="small" variant="text" @click="openEdit(r)">Edit</v-btn>
            <v-btn size="small" variant="text" color="error" @click="remove(r)">Delete</v-btn>
          </td>
        </tr>
      </tbody>
    </v-table>

    <v-dialog v-model="showForm" max-width="880">
      <v-card>
        <v-card-title>{{ editTarget ? 'Edit' : 'New' }} rule</v-card-title>
        <v-card-text>
          <v-text-field v-model="form.name" label="Name" class="mb-3" />
          <p class="text-body-2 font-weight-medium mb-1">Condition</p>
          <RuleBuilder v-model="form.ruleJson" />
          <v-select v-model="form.includeIds" :items="templates" multiple chips closable-chips
            item-title="name" item-value="documentTemplateId"
            label="Document templates to include when the condition matches" class="mt-4" />
          <p v-if="formError" class="text-error text-body-2">{{ formError }}</p>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showForm = false">Cancel</v-btn>
          <v-btn color="primary" :loading="saving" @click="save">Save</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '../../api/client.js'
import RuleBuilder from './RuleBuilder.vue'

const items = ref([])
const templates = ref([])
const error = ref('')
const showForm = ref(false)
const editTarget = ref(null)
const form = ref({ name: '', ruleJson: '', includeIds: [] })
const formError = ref('')
const saving = ref(false)

const fmt = d => d ? new Date(d).toLocaleString('en-GB', { dateStyle: 'medium', timeStyle: 'short' }) : ''

function includeNames(r) {
  const ids = (r.includeDocumentTemplateIdsCsv || '').split(',').map(s => s.trim()).filter(Boolean)
  if (!ids.length) return '—'
  return ids.map(id => templates.value.find(t => t.documentTemplateId === id)?.name ?? '…').join(', ')
}

async function load() {
  error.value = ''
  try {
    const [rules, tpls] = await Promise.all([
      api.get('/v1/intake/generation-rules'),
      api.get('/v1/intake/document-templates'),
    ])
    items.value = rules.data?.data?.items ?? []
    templates.value = tpls.data?.data?.items ?? []
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to load'
  }
}

function openCreate() {
  editTarget.value = null
  form.value = { name: '', ruleJson: '', includeIds: [] }
  formError.value = ''
  showForm.value = true
}
function openEdit(r) {
  editTarget.value = r
  form.value = {
    name: r.name,
    ruleJson: r.ruleJson,
    includeIds: (r.includeDocumentTemplateIdsCsv || '').split(',').map(s => s.trim()).filter(Boolean),
  }
  formError.value = ''
  showForm.value = true
}

async function save() {
  saving.value = true
  formError.value = ''
  try {
    const body = {
      name: form.value.name,
      ruleJson: form.value.ruleJson || '{}',
      includeDocumentTemplateIdsCsv: form.value.includeIds.join(','),
    }
    if (editTarget.value)
      await api.put(`/v1/intake/generation-rules/${editTarget.value.generationRuleId}`, body)
    else
      await api.post('/v1/intake/generation-rules', body)
    showForm.value = false
    await load()
  } catch (e) {
    formError.value = e.response?.data?.error ?? e.message ?? 'Save failed'
  } finally {
    saving.value = false
  }
}

async function remove(r) {
  if (!confirm(`Delete rule "${r.name}"?`)) return
  try {
    await api.delete(`/v1/intake/generation-rules/${r.generationRuleId}`)
    await load()
  } catch (e) {
    error.value = e.response?.data?.error ?? 'Delete failed'
  }
}

onMounted(load)
</script>
