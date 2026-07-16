<template>
  <div>
    <div class="d-flex justify-space-between align-center mb-4">
      <div>
        <h2 class="text-h6 font-weight-bold">Public forms</h2>
        <p class="text-body-2 text-medium-emphasis mb-0">
          Publish a questionnaire at an unlisted public link (/f/…) that anyone
          can fill without an account — embeddable in an iframe on any website.
        </p>
      </div>
      <v-btn color="primary" prepend-icon="mdi-plus" @click="openCreate">New public form</v-btn>
    </div>

    <v-alert v-if="error" type="error" density="compact" class="mb-3">{{ error }}</v-alert>

    <v-table density="comfortable">
      <thead>
        <tr><th>Name</th><th>Link</th><th>Published</th><th>Submissions</th><th style="width:220px;">Actions</th></tr>
      </thead>
      <tbody>
        <tr v-if="!items.length"><td colspan="5" class="text-medium-emphasis">No public forms yet.</td></tr>
        <tr v-for="f in items" :key="f.publicFormId">
          <td>{{ f.name }}</td>
          <td>
            <a :href="fillUrl(f)" target="_blank" rel="noopener">{{ fillUrl(f) }}</a>
            <v-btn size="x-small" variant="text" icon="mdi-content-copy" @click="copyLink(f)" />
          </td>
          <td>
            <v-icon :color="f.isPublished ? 'success' : 'grey'" size="small">
              {{ f.isPublished ? 'mdi-check-circle' : 'mdi-pause-circle' }}
            </v-icon>
          </td>
          <td>{{ f.submissionCount }}</td>
          <td>
            <v-btn size="small" variant="text" @click="openSubmissions(f)">Submissions</v-btn>
            <v-btn size="small" variant="text" @click="openEdit(f)">Edit</v-btn>
            <v-btn size="small" variant="text" color="error" @click="remove(f)">Delete</v-btn>
          </td>
        </tr>
      </tbody>
    </v-table>

    <v-dialog v-model="showForm" max-width="520">
      <v-card>
        <v-card-title>{{ editTarget ? 'Edit' : 'New' }} public form</v-card-title>
        <v-card-text>
          <v-text-field v-model="form.name" label="Name" class="mb-2" />
          <v-textarea v-model="form.description" label="Description (shown above the form)" rows="2" class="mb-2" />
          <v-select v-model="form.questionnaireTemplateId" :items="templates"
            item-title="name" item-value="questionnaireTemplateId" label="Questionnaire template" class="mb-2" />
          <v-switch v-model="form.isPublished" label="Published" color="success" hide-details />
          <p v-if="formError" class="text-error text-body-2 mt-2">{{ formError }}</p>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showForm = false">Cancel</v-btn>
          <v-btn color="primary" :loading="saving" @click="save">Save</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="showSubs" max-width="820">
      <v-card>
        <v-card-title>Submissions — {{ subsFor?.name }}</v-card-title>
        <v-card-text>
          <v-table density="compact">
            <thead><tr><th>Respondent</th><th>Received</th><th></th></tr></thead>
            <tbody>
              <tr v-if="!subs.length"><td colspan="3" class="text-medium-emphasis">No submissions yet.</td></tr>
              <tr v-for="s in subs" :key="s.publicFormSubmissionId">
                <td>{{ s.respondentName || s.respondentEmail || 'Anonymous' }}</td>
                <td>{{ fmt(s.createdAt) }}</td>
                <td><v-btn size="x-small" variant="text" @click="viewAnswers(s)">View</v-btn></td>
              </tr>
            </tbody>
          </v-table>
          <template v-if="answerRows.length">
            <v-divider class="my-3" />
            <v-table density="compact">
              <tbody>
                <tr v-for="row in answerRows" :key="row.id">
                  <td style="width:45%;" class="font-weight-medium">{{ row.label }}</td>
                  <td>{{ row.value }}</td>
                </tr>
              </tbody>
            </v-table>
          </template>
        </v-card-text>
        <v-card-actions><v-spacer /><v-btn variant="text" @click="showSubs = false">Close</v-btn></v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '../../api/client.js'

const items = ref([])
const templates = ref([])
const error = ref('')
const showForm = ref(false)
const editTarget = ref(null)
const form = ref({ name: '', description: '', questionnaireTemplateId: null, isPublished: false })
const formError = ref('')
const saving = ref(false)
const showSubs = ref(false)
const subsFor = ref(null)
const subs = ref([])
const answerRows = ref([])

const fmt = d => d ? new Date(d).toLocaleString('en-GB', { dateStyle: 'medium', timeStyle: 'short' }) : ''
const fillUrl = f => `${location.origin}/#/f/${f.slug}`

async function copyLink(f) {
  try { await navigator.clipboard.writeText(fillUrl(f)) } catch { /* ignore */ }
}

async function load() {
  error.value = ''
  try {
    const [forms, tpls] = await Promise.all([
      api.get('/v1/intake/public-forms'),
      api.get('/v1/intake/questionnaire-templates'),
    ])
    items.value = forms.data?.data?.items ?? []
    templates.value = tpls.data?.data?.items ?? []
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to load'
  }
}

function openCreate() {
  editTarget.value = null
  form.value = { name: '', description: '', questionnaireTemplateId: null, isPublished: false }
  formError.value = ''
  showForm.value = true
}
function openEdit(f) {
  editTarget.value = f
  form.value = {
    name: f.name, description: f.description ?? '',
    questionnaireTemplateId: f.questionnaireTemplateId, isPublished: f.isPublished,
  }
  formError.value = ''
  showForm.value = true
}

async function save() {
  saving.value = true
  formError.value = ''
  try {
    if (editTarget.value)
      await api.put(`/v1/intake/public-forms/${editTarget.value.publicFormId}`, form.value)
    else
      await api.post('/v1/intake/public-forms', form.value)
    showForm.value = false
    await load()
  } catch (e) {
    formError.value = e.response?.data?.error ?? e.message ?? 'Save failed'
  } finally {
    saving.value = false
  }
}

async function remove(f) {
  if (!confirm(`Delete public form "${f.name}"? Its link stops working.`)) return
  try {
    await api.delete(`/v1/intake/public-forms/${f.publicFormId}`)
    await load()
  } catch (e) {
    error.value = e.response?.data?.error ?? 'Delete failed'
  }
}

async function openSubmissions(f) {
  subsFor.value = f
  subs.value = []
  answerRows.value = []
  showSubs.value = true
  try {
    const res = await api.get(`/v1/intake/public-forms/${f.publicFormId}/submissions`)
    subs.value = res.data?.data?.items ?? []
  } catch (e) {
    error.value = e.response?.data?.error ?? 'Failed to load submissions'
  }
}

function viewAnswers(s) {
  let answers = {}
  try { answers = JSON.parse(s.answersJson ?? '{}') } catch { /* empty */ }
  answerRows.value = Object.entries(answers).map(([id, val]) => ({
    id, label: id,
    value: Array.isArray(val) ? val.join(', ') : (val === null || val === undefined ? '' : String(val)),
  }))
}

onMounted(load)
</script>
