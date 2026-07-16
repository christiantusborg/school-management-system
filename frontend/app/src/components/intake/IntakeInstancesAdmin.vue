<template>
  <div>
    <div class="d-flex justify-space-between align-center mb-4">
      <div>
        <h2 class="text-h6 font-weight-bold">Form assignments</h2>
        <p class="text-body-2 text-medium-emphasis mb-0">
          Attach a saved questionnaire template to an audience. Active assignments
          appear in the student portal, the partner portal, or as the optional
          Survey step of the signup wizard.
        </p>
      </div>
      <v-btn color="primary" prepend-icon="mdi-plus" @click="openCreate">New assignment</v-btn>
    </div>

    <v-alert v-if="error" type="error" density="compact" class="mb-3">{{ error }}</v-alert>

    <v-table density="comfortable">
      <thead>
        <tr>
          <th>Name</th><th>Template</th><th>Audience</th><th>Active</th>
          <th>Responses</th><th style="width:220px;">Actions</th>
        </tr>
      </thead>
      <tbody>
        <tr v-if="!items.length"><td colspan="6" class="text-medium-emphasis">No assignments yet.</td></tr>
        <tr v-for="i in items" :key="i.intakeInstanceId">
          <td>{{ i.name }}</td>
          <td>{{ i.templateName ?? '—' }}</td>
          <td><v-chip size="small">{{ audienceLabel(i.audience) }}</v-chip></td>
          <td>
            <v-icon :color="i.isActive ? 'success' : 'grey'" size="small">
              {{ i.isActive ? 'mdi-check-circle' : 'mdi-pause-circle' }}
            </v-icon>
          </td>
          <td>{{ i.submittedCount }} / {{ i.responseCount }}</td>
          <td>
            <v-btn size="small" variant="text" @click="openResponses(i)">Responses</v-btn>
            <v-btn size="small" variant="text" @click="openEdit(i)">Edit</v-btn>
            <v-btn size="small" variant="text" color="error" @click="remove(i)">Delete</v-btn>
          </td>
        </tr>
      </tbody>
    </v-table>

    <!-- Create / edit -->
    <v-dialog v-model="showForm" max-width="520">
      <v-card>
        <v-card-title>{{ editTarget ? 'Edit' : 'New' }} assignment</v-card-title>
        <v-card-text>
          <v-text-field v-model="form.name" label="Name" class="mb-2" />
          <v-select v-model="form.questionnaireTemplateId" :items="templates"
            item-title="name" item-value="questionnaireTemplateId" label="Questionnaire template" class="mb-2" />
          <v-select v-model="form.audience" :items="AUDIENCES"
            item-title="label" item-value="value" label="Audience" class="mb-2" />
          <v-switch v-model="form.isActive" label="Active" color="success" hide-details />
          <p v-if="formError" class="text-error text-body-2 mt-2">{{ formError }}</p>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showForm = false">Cancel</v-btn>
          <v-btn color="primary" :loading="saving" @click="save">Save</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Responses list -->
    <v-dialog v-model="showResponses" max-width="760">
      <v-card>
        <v-card-title>Responses — {{ responsesFor?.name }}</v-card-title>
        <v-card-text>
          <v-table density="compact">
            <thead>
              <tr><th>Respondent</th><th>State</th><th>Submitted</th><th></th></tr>
            </thead>
            <tbody>
              <tr v-if="!responses.length"><td colspan="4" class="text-medium-emphasis">No responses yet.</td></tr>
              <tr v-for="r in responses" :key="r.intakeResponseId">
                <td>{{ r.studentName || r.partnerName || r.studentNumber || 'Unknown' }}
                  <span v-if="r.studentNumber" class="text-medium-emphasis">· {{ r.studentNumber }}</span>
                </td>
                <td>
                  <v-chip size="x-small" :color="r.lifecycleState === 'Submitted' ? 'success' : 'warning'">
                    {{ r.lifecycleState }}
                  </v-chip>
                </td>
                <td>{{ fmt(r.submittedAt ?? r.modifiedAt) }}</td>
                <td><v-btn size="x-small" variant="text" @click="openAnswers(r)">View</v-btn></td>
              </tr>
            </tbody>
          </v-table>
        </v-card-text>
        <v-card-actions><v-spacer /><v-btn variant="text" @click="showResponses = false">Close</v-btn></v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Single response answers -->
    <v-dialog v-model="showAnswers" max-width="640">
      <v-card>
        <v-card-title>Answers</v-card-title>
        <v-card-text>
          <v-table density="compact">
            <tbody>
              <tr v-for="row in answerRows" :key="row.id">
                <td style="width:45%;" class="font-weight-medium">{{ row.label }}</td>
                <td>{{ row.value }}</td>
              </tr>
              <tr v-if="!answerRows.length"><td class="text-medium-emphasis">Empty response.</td></tr>
            </tbody>
          </v-table>
          <p v-if="answerHash" class="text-caption text-medium-emphasis mt-2">
            Questionnaire version hash: {{ answerHash }}
          </p>
        </v-card-text>
        <v-card-actions><v-spacer /><v-btn variant="text" @click="showAnswers = false">Close</v-btn></v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '../../api/client.js'

const AUDIENCES = [
  { value: 'Student', label: 'Students (student portal)' },
  { value: 'Partner', label: 'Partners (partner portal)' },
  { value: 'SignupWizard', label: 'Signup wizard (survey step)' },
]
const audienceLabel = v => AUDIENCES.find(a => a.value === v)?.label ?? v

const items = ref([])
const templates = ref([])
const error = ref('')
const showForm = ref(false)
const editTarget = ref(null)
const form = ref({ name: '', questionnaireTemplateId: null, audience: 'Student', isActive: true })
const formError = ref('')
const saving = ref(false)

const showResponses = ref(false)
const responsesFor = ref(null)
const responses = ref([])
const showAnswers = ref(false)
const answerRows = ref([])
const answerHash = ref('')

const fmt = d => d ? new Date(d).toLocaleString('en-GB', { dateStyle: 'medium', timeStyle: 'short' }) : ''

async function load() {
  error.value = ''
  try {
    const [inst, tpl] = await Promise.all([
      api.get('/v1/intake/intake-instances'),
      api.get('/v1/intake/questionnaire-templates'),
    ])
    items.value = inst.data?.data?.items ?? []
    templates.value = tpl.data?.data?.items ?? []
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to load'
  }
}

function openCreate() {
  editTarget.value = null
  form.value = { name: '', questionnaireTemplateId: null, audience: 'Student', isActive: true }
  formError.value = ''
  showForm.value = true
}
function openEdit(i) {
  editTarget.value = i
  form.value = {
    name: i.name, questionnaireTemplateId: i.questionnaireTemplateId,
    audience: i.audience, isActive: i.isActive,
  }
  formError.value = ''
  showForm.value = true
}

async function save() {
  saving.value = true
  formError.value = ''
  try {
    if (editTarget.value)
      await api.put(`/v1/intake/intake-instances/${editTarget.value.intakeInstanceId}`, form.value)
    else
      await api.post('/v1/intake/intake-instances', form.value)
    showForm.value = false
    await load()
  } catch (e) {
    formError.value = e.response?.data?.error ?? e.message ?? 'Save failed'
  } finally {
    saving.value = false
  }
}

async function remove(i) {
  if (!confirm(`Delete assignment "${i.name}"?`)) return
  try {
    await api.delete(`/v1/intake/intake-instances/${i.intakeInstanceId}`)
    await load()
  } catch (e) {
    error.value = e.response?.data?.error ?? 'Delete failed'
  }
}

async function openResponses(i) {
  responsesFor.value = i
  responses.value = []
  showResponses.value = true
  try {
    const res = await api.get(`/v1/intake/intake-instances/${i.intakeInstanceId}/responses`)
    responses.value = res.data?.data?.items ?? []
  } catch (e) {
    error.value = e.response?.data?.error ?? 'Failed to load responses'
  }
}

// Recursively walk a questionnaire definition collecting {id, label} so the
// answer viewer shows question text instead of raw field ids.
function collectFields(node, out) {
  if (!node || typeof node !== 'object') return
  if (Array.isArray(node)) { node.forEach(n => collectFields(n, out)); return }
  if (node.id && (node.label || node.title)) {
    const raw = node.label ?? node.title
    out[node.id] = typeof raw === 'object' ? (raw.fallback ?? node.id) : raw
  }
  Object.values(node).forEach(v => collectFields(v, out))
}

async function openAnswers(r) {
  try {
    const res = await api.get(`/v1/intake/intake-responses/${r.intakeResponseId}`)
    const d = res.data?.data
    const labels = {}
    try { collectFields(JSON.parse(d.definitionJson ?? '{}'), labels) } catch { /* labels stay raw ids */ }
    let answers = {}
    try { answers = JSON.parse(d.answersJson ?? '{}') } catch { /* empty */ }
    answerRows.value = Object.entries(answers).map(([id, val]) => ({
      id,
      label: labels[id] ?? id,
      value: Array.isArray(val) ? val.join(', ') : (val === null || val === undefined ? '' : String(val)),
    }))
    answerHash.value = d.questionnaireVersionHash ?? ''
    showAnswers.value = true
  } catch (e) {
    error.value = e.response?.data?.error ?? 'Failed to load response'
  }
}

onMounted(load)
</script>
