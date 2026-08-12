<template>
  <div>
    <div class="d-flex justify-space-between align-center mb-4">
      <div>
        <h2 class="text-h6 font-weight-bold">Public forms</h2>
        <p class="text-body-2 text-medium-emphasis mb-0">
          Publish a questionnaire at an unlisted public link (/f/…) that anyone
          can fill without an account — embeddable in an iframe on any website.
          Each form is a <strong>run</strong> for a date window; several runs of
          the same questionnaire are compared in Statistics.
        </p>
      </div>
      <v-btn color="primary" prepend-icon="mdi-plus" @click="openCreate">New public form</v-btn>
    </div>

    <v-alert v-if="error" type="error" density="compact" class="mb-3">{{ error }}</v-alert>

    <v-table density="comfortable">
      <thead>
        <tr>
          <th>Name</th><th>Link</th><th>Run window</th><th>Published</th>
          <th>Submissions</th><th style="width:300px;">Actions</th>
        </tr>
      </thead>
      <tbody>
        <tr v-if="!items.length"><td colspan="6" class="text-medium-emphasis">No public forms yet.</td></tr>
        <tr v-for="f in items" :key="f.publicFormId">
          <td>{{ f.name }}</td>
          <td>
            <a :href="fillUrl(f)" target="_blank" rel="noopener">{{ fillUrl(f) }}</a>
            <v-btn size="x-small" variant="text" icon="mdi-content-copy" @click="copyLink(f)" />
          </td>
          <td>
            <template v-if="f.runStartDate || f.runEndDate">
              <span class="text-body-2">{{ runLabel(f) }}</span>
              <v-chip v-if="f.isClosed" size="x-small" color="error" class="ml-1" label>Closed</v-chip>
            </template>
            <span v-else class="text-medium-emphasis">Always open</span>
          </td>
          <td>
            <v-icon :color="f.isPublished ? 'success' : 'grey'" size="small">
              {{ f.isPublished ? 'mdi-check-circle' : 'mdi-pause-circle' }}
            </v-icon>
          </td>
          <td>{{ f.submissionCount }}</td>
          <td>
            <v-btn size="small" variant="text" color="primary" prepend-icon="mdi-chart-bar" @click="openStats(f)">Statistics</v-btn>
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
            item-title="name" item-value="questionnaireTemplateId" label="Questionnaire template" class="mb-2"
            @update:model-value="tid => loadReferenceFields(tid, null)" />
          <div class="d-flex ga-2">
            <v-text-field v-model="form.runStartDate" label="Run start" type="date" class="mb-1" hide-details="auto" />
            <v-text-field v-model="form.runEndDate" label="Run end" type="date" class="mb-1" hide-details="auto" />
          </div>
          <p class="text-caption text-medium-emphasis mt-1 mb-2">
            The link closes automatically after the run-end date. Leave blank to keep it always open.
            Runs of the same questionnaire are grouped in Statistics.
          </p>

          <!-- Owner-set reference fields declared in the chosen questionnaire -->
          <template v-if="refFields.length">
            <v-divider class="my-2" />
            <div class="text-subtitle-2">Reference values</div>
            <p class="text-caption text-medium-emphasis mb-2">
              Set for this form. Shown read-only on the form and recorded with every submission.
            </p>
            <template v-for="rf in refFields" :key="rf.id">
              <v-select v-if="rf.type === 'refSchool'"
                :model-value="refValues[rf.id]?.value || null" :items="schools"
                item-title="name" item-value="schoolId" :label="rf.label"
                density="compact" class="mb-2" hide-details="auto"
                @update:model-value="v => pickSchool(rf, v)" />

              <v-select v-else-if="rf.type === 'refPartner'"
                :model-value="refValues[rf.id]?.value || null" :items="partners"
                item-title="name" item-value="partnerId" :label="rf.label"
                density="compact" class="mb-2" hide-details="auto"
                @update:model-value="v => pickPartner(rf, v)" />

              <div v-else-if="rf.type === 'refPartnerProgramme'" class="mb-2">
                <v-select
                  :model-value="refValues[rf.id]?.partnerValue || null" :items="partners"
                  item-title="name" item-value="partnerId" :label="`${rf.label} — Partner`"
                  density="compact" class="mb-1" hide-details="auto"
                  @update:model-value="v => pickPPPartner(rf, v)" />
                <v-select
                  :model-value="refValues[rf.id]?.value || null" :items="ppProgrammes[rf.id] || []"
                  item-title="name" item-value="programmeId" :label="`${rf.label} — Programme`"
                  density="compact" hide-details="auto"
                  :disabled="!refValues[rf.id]?.partnerValue"
                  :placeholder="refValues[rf.id]?.partnerValue ? 'Select programme' : 'Pick a partner first'"
                  @update:model-value="v => pickPPProgramme(rf, v)" />
              </div>

              <v-text-field v-else-if="rf.type === 'refText'"
                :model-value="refValues[rf.id]?.value || ''" :label="rf.label"
                density="compact" class="mb-2" hide-details="auto"
                @update:model-value="v => pickText(rf, v)" />
            </template>
            <v-divider class="my-2" />
          </template>

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
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import api from '../../api/client.js'

const router = useRouter()

const REF_TYPES = ['refSchool', 'refPartner', 'refPartnerProgramme', 'refText']
const refFields = ref([])              // reference fields declared in the chosen questionnaire
const refValues = reactive({})         // fieldId -> { value, partnerValue, partnerName, display }
const schools = ref([])
const partners = ref([])
const allProgrammes = ref([])
const ppProgrammes = reactive({})      // fieldId -> programmes for the chosen partner
let refDataLoaded = false

const items = ref([])
const templates = ref([])
const error = ref('')
const showForm = ref(false)
const editTarget = ref(null)
const form = ref({ name: '', description: '', questionnaireTemplateId: null, isPublished: false, runStartDate: '', runEndDate: '' })
const formError = ref('')
const saving = ref(false)
const showSubs = ref(false)
const subsFor = ref(null)
const subs = ref([])
const answerRows = ref([])

const fmt = d => d ? new Date(d).toLocaleString('en-GB', { dateStyle: 'medium', timeStyle: 'short' }) : ''
const fmtDate = d => d ? new Date(d).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }) : '…'
const fillUrl = f => `${location.origin}/#/f/${f.slug}`

// "01 Jan 2026 – 31 Jan 2026", or open-ended variants.
const runLabel = f => `${fmtDate(f.runStartDate)} – ${fmtDate(f.runEndDate)}`

// Native date input gives 'yyyy-mm-dd'. Anchor start to 00:00 and end to
// 23:59:59 (UTC) so the link stays open through the whole end date.
const toStartIso = d => (d ? `${d}T00:00:00Z` : null)
const toEndIso = d => (d ? `${d}T23:59:59Z` : null)
const isoToDate = v => (v ? String(v).slice(0, 10) : '')

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
  form.value = { name: '', description: '', questionnaireTemplateId: null, isPublished: false, runStartDate: '', runEndDate: '' }
  formError.value = ''
  loadReferenceFields(null, null)
  showForm.value = true
}
function openEdit(f) {
  editTarget.value = f
  form.value = {
    name: f.name, description: f.description ?? '',
    questionnaireTemplateId: f.questionnaireTemplateId, isPublished: f.isPublished,
    runStartDate: isoToDate(f.runStartDate), runEndDate: isoToDate(f.runEndDate),
  }
  formError.value = ''
  loadReferenceFields(f.questionnaireTemplateId, f.ownerReferences)
  showForm.value = true
}

// ── Owner-set reference fields (per public form) ──────────────────────────────
const unwrap = res => res?.data?.items ?? res?.data?.data?.items ?? []

async function ensureRefData() {
  if (refDataLoaded) return
  refDataLoaded = true
  try {
    const [sc, pt, pr] = await Promise.all([
      api.get('/v1/school/schools/options').catch(() => ({ data: { items: [] } })),
      api.get('/v1/admin/school/partners').catch(() => ({ data: { items: [] } })),
      api.get('/v1/school/programmes').catch(() => ({ data: { items: [] } })),
    ])
    schools.value = unwrap(sc)
    partners.value = unwrap(pt)
    allProgrammes.value = unwrap(pr)
  } catch { /* leave lists empty */ }
}

async function loadPP(fieldId, partnerId) {
  ppProgrammes[fieldId] = []
  if (!partnerId) return
  try {
    const res = await api.get(`/v1/admin/school/partners/${partnerId}/programme-access`)
    const ids = new Set(unwrap(res).map(i => i.programmeId))
    ppProgrammes[fieldId] = allProgrammes.value.filter(p => ids.has(p.programmeId))
  } catch { ppProgrammes[fieldId] = [] }
}

function extractRefFields(def) {
  const out = []
  for (const page of def?.pages ?? [])
    for (const section of page?.sections ?? [])
      for (const group of section?.groups ?? [])
        for (const item of group?.items ?? [])
          if (REF_TYPES.includes(item?.type))
            out.push({ id: item.id, type: item.type, label: item?.label?.fallback || item.id })
  return out
}

async function loadReferenceFields(templateId, existing) {
  refFields.value = []
  Object.keys(refValues).forEach(k => delete refValues[k])
  Object.keys(ppProgrammes).forEach(k => delete ppProgrammes[k])
  if (!templateId) return
  try {
    const res = await api.get(`/v1/intake/questionnaire-templates/${templateId}`)
    const defJson = res.data?.data?.definitionJson ?? res.data?.definitionJson
    let def = null
    try { def = JSON.parse(defJson) } catch { def = null }
    refFields.value = def ? extractRefFields(def) : []
    if (refFields.value.length) await ensureRefData()
    for (const rf of refFields.value) {
      const ex = (existing || []).find(x => x.fieldId === rf.id)
      refValues[rf.id] = ex
        ? { value: ex.value || '', partnerValue: ex.partnerValue || '', partnerName: '', display: ex.display || '' }
        : { value: '', partnerValue: '', partnerName: '', display: '' }
      if (rf.type === 'refPartnerProgramme' && refValues[rf.id].partnerValue)
        loadPP(rf.id, refValues[rf.id].partnerValue)
    }
  } catch { refFields.value = [] }
}

function pickSchool(rf, schoolId) {
  const s = schools.value.find(x => x.schoolId === schoolId)
  refValues[rf.id] = { ...refValues[rf.id], value: schoolId, display: s?.name || '' }
}
function pickPartner(rf, partnerId) {
  const p = partners.value.find(x => x.partnerId === partnerId)
  refValues[rf.id] = { ...refValues[rf.id], value: partnerId, display: p?.name || '' }
}
function pickPPPartner(rf, partnerId) {
  const p = partners.value.find(x => x.partnerId === partnerId)
  refValues[rf.id] = { value: '', partnerValue: partnerId, partnerName: p?.name || '', display: '' }
  loadPP(rf.id, partnerId)
}
function pickPPProgramme(rf, programmeId) {
  const pr = (ppProgrammes[rf.id] || []).find(x => x.programmeId === programmeId)
  const pName = refValues[rf.id]?.partnerName || ''
  refValues[rf.id] = { ...refValues[rf.id], value: programmeId, display: pName ? `${pName} — ${pr?.name || ''}` : (pr?.name || '') }
}
function pickText(rf, text) {
  refValues[rf.id] = { ...refValues[rf.id], value: text, display: text }
}

function buildOwnerReferences() {
  return refFields.value.map(rf => ({
    fieldId: rf.id,
    type: rf.type,
    value: refValues[rf.id]?.value || '',
    partnerValue: refValues[rf.id]?.partnerValue || '',
    display: refValues[rf.id]?.display || '',
  }))
}

async function save() {
  saving.value = true
  formError.value = ''
  if (form.value.runStartDate && form.value.runEndDate && form.value.runEndDate < form.value.runStartDate) {
    formError.value = 'Run end date cannot be before the start date.'
    saving.value = false
    return
  }
  // Reference fields must be filled before the form can be published.
  if (form.value.isPublished) {
    const missing = refFields.value.find(rf => !refValues[rf.id]?.value)
    if (missing) {
      formError.value = `Set a value for "${missing.label}" before publishing.`
      saving.value = false
      return
    }
  }
  const payload = {
    name: form.value.name,
    description: form.value.description,
    questionnaireTemplateId: form.value.questionnaireTemplateId,
    isPublished: form.value.isPublished,
    runStartDate: toStartIso(form.value.runStartDate),
    runEndDate: toEndIso(form.value.runEndDate),
    ownerReferences: buildOwnerReferences(),
  }
  try {
    if (editTarget.value)
      await api.put(`/v1/intake/public-forms/${editTarget.value.publicFormId}`, payload)
    else
      await api.post('/v1/intake/public-forms', payload)
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

function openStats(f) {
  router.push(`/admin/public-form-stats/${f.publicFormId}`)
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
