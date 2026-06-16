<template>
  <div class="apply-page">
    <header class="apply-header">
      <div class="logo">IBSS</div>
      <div class="org">
        <strong>International Business School of Scandinavia</strong>
        <span v-if="partner">Application via <strong>{{ partner.name }}</strong></span>
      </div>
    </header>

    <div v-if="loadError" class="card card-err">
      <h2>{{ loadError.title }}</h2>
      <p>{{ loadError.detail }}</p>
    </div>

    <div v-else-if="!loaded" class="card">Loading…</div>

    <div v-else-if="state.submitted" class="card card-success">
      <div class="success-icon">✓</div>
      <h2>Application submitted</h2>
      <p v-if="state.emailVerified">All set — your application has been received and your email is confirmed. Your partner will be in touch.</p>
      <p v-else>
        Your application has been received. <strong>Please check your email</strong> and click the
        confirmation link to fully activate your account. Until then we'll keep your application
        on file but you cannot log in.
      </p>
    </div>

    <div v-else class="card">
      <ol class="step-bar">
        <li v-for="s in STEPS" :key="s.n"
            :class="{ active: state.step === s.n, done: state.step > s.n }">
          <span class="num">{{ state.step > s.n ? '✓' : s.n }}</span>
          <span class="lbl">{{ s.label }}</span>
        </li>
      </ol>

      <div v-if="!state.emailVerified && state.step > 1" class="banner-warn">
        Confirmation email sent to <strong>{{ form.email }}</strong>. Click the link any time to
        activate login. You can keep filling in your application now — everything is saved.
      </div>

      <p v-if="error" class="form-error">{{ error }}</p>

      <!-- ── Step 1: Account ── -->
      <section v-if="state.step === 1" class="step">
        <h2 class="step-title">Create your account</h2>
        <p class="step-hint">We'll save your progress as you go and send a confirmation email.</p>
        <div class="row-2">
          <div class="field"><label>First name *</label><input v-model="form.firstName" /></div>
          <div class="field"><label>Last name *</label><input v-model="form.lastName" /></div>
        </div>
        <div class="field"><label>Email *</label><input type="email" v-model="form.email" /></div>
        <div class="field"><label>Password *</label><input type="password" v-model="form.password" placeholder="At least 8 characters" /></div>
        <div class="actions actions-end">
          <button class="btn-primary" :disabled="!step1Valid || busy" @click="startAccount">
            {{ busy ? 'Creating…' : 'Create account &amp; continue →' }}
          </button>
        </div>
      </section>

      <!-- ── Step 2: Personal ── -->
      <section v-if="state.step === 2" class="step">
        <h2 class="step-title">Personal information</h2>
        <div class="row-2">
          <div class="field"><label>Date of birth</label><input type="date" v-model="form.dateOfBirth" /></div>
          <div class="field"><label>Passport / ID number</label><input v-model="form.passportId" /></div>
        </div>
        <div class="field">
          <label>Nationality</label>
          <select v-model.number="form.nationalityId">
            <option :value="null">— select —</option>
            <option v-for="n in nationalities" :key="n.nationalityId" :value="n.nationalityId">{{ n.name }}</option>
          </select>
        </div>

        <div class="section-divider">Address</div>
        <div class="field"><label>Street / line 1</label><input v-model="form.addressLine1" placeholder="123 Main Street" /></div>
        <div class="field"><label>Address line 2</label><input v-model="form.addressLine2" placeholder="Apt, suite, building (optional)" /></div>
        <div class="row-3">
          <div class="field"><label>City</label><input v-model="form.city" /></div>
          <div class="field"><label>State / Region</label><input v-model="form.stateRegion" /></div>
          <div class="field"><label>Postal code</label><input v-model="form.postalCode" /></div>
        </div>
        <div class="field">
          <label>Country</label>
          <select v-model="form.countryCode">
            <option value="">— select —</option>
            <option v-for="n in nationalities" :key="n.code" :value="n.code">{{ n.name }}</option>
          </select>
        </div>

        <div class="actions">
          <button class="btn-back" @click="goBack">← Back</button>
          <button class="btn-primary" :disabled="busy" @click="savePersonal">Save &amp; continue →</button>
        </div>
      </section>

      <!-- ── Step 3: Background ── -->
      <section v-if="state.step === 3" class="step">
        <h2 class="step-title">Academic &amp; professional background</h2>
        <div class="row-2">
          <div class="field">
            <label>Highest degree obtained</label>
            <select v-model="form.highestDegree">
              <option value="">— Select —</option>
              <option v-for="d in educationLevels" :key="d.educationLevelId" :value="d.name">{{ d.name }}</option>
            </select>
          </div>
          <div class="field field-narrow">
            <label>Years of work experience</label>
            <select v-model.number="form.yearsWorkExperience">
              <option v-for="n in yearsExperienceOptions" :key="n.value" :value="n.value">{{ n.label }}</option>
            </select>
          </div>
        </div>

        <div class="section-divider">Languages I speak</div>
        <div v-for="(row, idx) in form.languages" :key="idx" class="lang-row">
          <select v-model.number="row.languageId" class="lang-sel">
            <option :value="0">— select language —</option>
            <option v-for="l in availableLanguagesFor(idx)" :key="l.languageId" :value="l.languageId">{{ l.name }}</option>
          </select>
          <select v-model.number="row.proficiency" class="lang-prof">
            <option v-for="p in PROFICIENCIES" :key="p.id" :value="p.id">{{ p.label }}</option>
          </select>
          <button class="btn-x" @click="removeLanguage(idx)" title="Remove">✕</button>
        </div>
        <button v-if="canAddLanguage" class="btn-add" @click="addLanguage">+ Add language</button>

        <div class="actions">
          <button class="btn-back" @click="goBack">← Back</button>
          <button class="btn-primary" :disabled="busy" @click="saveBackground">Save &amp; continue →</button>
        </div>
      </section>

      <!-- ── Step 4: Programmes (multi) ── -->
      <section v-if="state.step === 4" class="step">
        <h2 class="step-title">Programme &amp; specialization</h2>
        <p class="step-hint">Apply for as many programmes as you like — required documents will be combined.</p>

        <div v-for="(row, idx) in form.programmes" :key="idx" class="prog-row">
          <div class="row-2">
            <div class="field">
              <label>Programme</label>
              <select v-model="row.programmeId" @change="onProgrammeChange(row)">
                <option value="">— select —</option>
                <option v-for="p in catalogue.programmes" :key="p.programmeId" :value="p.programmeId">
                  {{ p.name }} ({{ p.code }})
                </option>
              </select>
            </div>
            <div class="field">
              <label>Specialization</label>
              <select v-model="row.specializationId" :disabled="!row.programmeId">
                <option value="">— select —</option>
                <option v-for="m in specializationsFor(row.programmeId)" :key="m.specializationId" :value="m.specializationId">{{ m.name }}</option>
              </select>
            </div>
          </div>
          <div class="pathway-block">
            <div class="section-divider">Pathway (qualification route) — optional</div>
            <div class="field">
              <select v-model="row.pathwayId" :disabled="!row.programmeId || pathwaysFor(row.programmeId).length === 0">
                <option :value="null">— not chosen —</option>
                <option v-for="pw in pathwaysFor(row.programmeId)" :key="pw.pathwayId" :value="pw.pathwayId">
                  {{ pw.name }}{{ pathwayElevationHint(row.programmeId, pw) ? ` — available via your ${pathwayElevationHint(row.programmeId, pw)} application` : '' }}
                </option>
              </select>
            </div>
          </div>
          <div class="field">
            <label>Mode of study</label>
            <select v-model.number="row.modeOfStudyId">
              <option v-for="m in MODES" :key="m.id" :value="m.id">{{ m.label }}</option>
            </select>
          </div>
          <button v-if="form.programmes.length > 1" class="btn-row-x" @click="removeProgRow(idx)">Remove this programme</button>
        </div>
        <button class="btn-add" @click="addProgRow">+ Add another programme</button>

        <div class="actions">
          <button class="btn-back" @click="goBack">← Back</button>
          <button class="btn-primary" :disabled="!step4Valid || busy" @click="savePrograms">Save &amp; continue →</button>
        </div>
      </section>

      <!-- ── Step 5: Documents (per-application) ── -->
      <section v-if="state.step === 5" class="step">
        <h2 class="step-title">Supporting documents</h2>
        <p class="step-hint">
          Each application has its own document set — please upload separately for every programme you applied to.
        </p>

        <div v-for="(row, idx) in form.programmes" :key="`docs-${idx}-${row.specializationId}`" class="prog-docs">
          <div class="prog-docs-head">
            <strong>{{ programmeNameFor(row.programmeId) }}</strong>
            <span class="muted">— {{ specializationNameFor(row.programmeId, row.specializationId) }}</span>
          </div>
          <ul class="doc-list">
            <li v-for="req in requiredDocsFor(row.programmeId)" :key="`${row.specializationId}-${req.documentTypeId}`" class="doc-row">
              <div class="doc-name">
                <span :class="['doc-mark', uploadedFor(row.specializationId, req.documentTypeId) ? 'mark-ok' : 'mark-pending']">
                  {{ uploadedFor(row.specializationId, req.documentTypeId) ? '✓' : '·' }}
                </span>
                {{ req.name }}
              </div>
              <div class="doc-actions">
                <span v-if="uploadedFor(row.specializationId, req.documentTypeId)" class="doc-meta">
                  {{ uploadedFor(row.specializationId, req.documentTypeId).fileName }}
                </span>
                <label class="btn-upload">
                  {{ uploadedFor(row.specializationId, req.documentTypeId) ? 'Replace' : 'Upload' }}
                  <input type="file" :accept="ACCEPTED_DOC_ACCEPT_ATTR"
                         @change="onUpload($event, row.specializationId, req.documentTypeId)" />
                </label>
                <button v-if="uploadedFor(row.specializationId, req.documentTypeId)" class="btn-x"
                        @click="onRemove(uploadedFor(row.specializationId, req.documentTypeId).studentDocumentId)">Remove</button>
              </div>
            </li>
            <li v-if="requiredDocsFor(row.programmeId).length === 0" class="doc-empty">
              No documents required for this programme.
            </li>
          </ul>
        </div>

        <div class="actions">
          <button class="btn-back" @click="goBack">← Back</button>
          <button class="btn-primary" @click="state.step = 6">Continue →</button>
        </div>
      </section>

      <!-- ── Step 6: Consent + Submit ── -->
      <section v-if="state.step === 6" class="step">
        <h2 class="step-title">Consent &amp; declaration</h2>
        <ul class="consent-list">
          <li>Your data is used to evaluate your application and shared with the partner you applied through.</li>
          <li>It is not sold or used for marketing.</li>
          <li>Retained for 3 years (or until you ask us to delete it). You can access, correct, or delete it any time.</li>
        </ul>
        <label class="consent-row"><input type="checkbox" v-model="form.consentProcessing" /> I consent to IBSS and my chosen partner processing my data for this application (GDPR Art. 6(1)(a)).</label>
        <label class="consent-row"><input type="checkbox" v-model="form.consentTerms" /> I agree to the Terms of Service and Privacy Policy.</label>
        <label class="consent-row"><input type="checkbox" v-model="form.consentAccuracy" /> I declare the information I have provided is accurate.</label>
        <div class="actions">
          <button class="btn-back" @click="goBack">← Back</button>
          <button class="btn-primary" :disabled="!consentValid || busy" @click="submitApplication">
            {{ busy ? 'Submitting…' : 'Submit application' }}
          </button>
        </div>
      </section>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted, watch } from 'vue'
import api from '../api/client.js'
import { ACCEPTED_DOC_ACCEPT_ATTR } from '../utils/uploadPolicy.js'
import { resolvePartnerSlug } from '../lib/partner.js'
import { blindPassword, deriveClientPublicKey } from '../crypto/opaque.js'

const STEPS = [
  { n: 1, label: 'Account' },
  { n: 2, label: 'Personal' },
  { n: 3, label: 'Background' },
  { n: 4, label: 'Programmes' },
  { n: 5, label: 'Documents' },
]

const MODES = [
  { id: 1, label: 'Distance / Online self-study' },
  { id: 2, label: 'Blended learning' },
  { id: 3, label: 'Full-time on-campus' },
]

const PROFICIENCIES = [
  { id: 1, label: 'Beginner' },
  { id: 2, label: 'Intermediate' },
  { id: 3, label: 'Fluent' },
  { id: 4, label: 'Native' },
]

const slug = resolvePartnerSlug()

// Education levels are loaded from /v1/public/education-levels (configurable
// via System Config). The `name` is what gets persisted on the student record;
// the `educationLevelId` is used client-side to filter pathway options against
// each pathway's `acceptedEducationLevelIds` set.
const educationLevels = ref([])
const selectedEducationLevelId = computed(() => {
  if (!form.highestDegree) return null
  return educationLevels.value.find(d => d.name === form.highestDegree)?.educationLevelId ?? null
})

// Highest pathway requirement across the partner's full programme list.
// Drives the cap on the years dropdown.
const maxPathwayMinYears = computed(() => {
  let max = 0
  for (const p of catalogue.value.programmes ?? []) {
    for (const pw of p.pathways ?? []) {
      const m = pw.minimumYearsWorkExperience ?? 0
      if (m > max) max = m
    }
  }
  return max
})

const yearsExperienceOptions = computed(() => {
  const cap = maxPathwayMinYears.value
  // 0..(cap-1) literal years, then a single `${cap}+` bucket carrying value
  // `cap` itself (matches the pathway minimum exactly — so picking "12+" still
  // satisfies any 12-year-minimum pathway).
  const opts = [{ value: 0, label: '0' }]
  for (let i = 1; i < cap; i++) opts.push({ value: i, label: String(i) })
  if (cap > 0) opts.push({ value: cap, label: `${cap}+` })
  return opts
})

// Years dropdown range is dynamic per partner: it spans 0..max where max is
// the highest `minimumYearsWorkExperience` across every pathway in the
// partner's catalogue, plus a "max+" bucket so students with more than the
// cap can self-identify (their value is `max + 1` so any pathway gate passes).
// Falls back to a 0..15 range while the catalogue is still loading.

const loaded = ref(false)
const loadError = ref(null)
const busy = ref(false)
const error = ref('')

const catalogue = ref({ partner: null, programmes: [] })
const partner = computed(() => catalogue.value.partner)
const documentTypes = ref([])
const documents = ref([])
const languagesCatalog = ref([])
const nationalities = ref([])

const state = reactive({
  step: 1,
  emailVerified: false,
  submitted: false,
})

const form = reactive({
  firstName: '', lastName: '', email: '', password: '',
  dateOfBirth: '', passportId: '', nationalityId: null,
  addressLine1: '', addressLine2: '', city: '', stateRegion: '', postalCode: '', countryCode: '',
  highestDegree: '', yearsWorkExperience: 0,
  languages: [],   // [{ languageId, proficiency }]
  programmes: [{ programmeId: '', specializationId: '', modeOfStudyId: 1, pathwayId: null }],
  consentProcessing: false, consentTerms: false, consentAccuracy: false,
})

const wizardToken = ref(localStorage.getItem('wizardToken') || '')

function setToken(t) {
  wizardToken.value = t || ''
  if (t) localStorage.setItem('wizardToken', t)
  else localStorage.removeItem('wizardToken')
}

function authHeaders() { return wizardToken.value ? { 'X-Wizard-Token': wizardToken.value } : {} }

const step1Valid = computed(() =>
  form.firstName.trim() && form.lastName.trim() && form.email.trim() && form.password.length >= 8
)
const step4Valid = computed(() => form.programmes.length > 0
  && form.programmes.every(p => p.programmeId && p.specializationId))
const consentValid = computed(() => form.consentProcessing && form.consentTerms && form.consentAccuracy)

// ── Cross-application pathway elevation ──────────────────────────────────────
// If the applicant also picks a Bachelor in the same application, treat that
// Bachelor as a held qualification when filtering Master pathways (and so on
// up the chain). Each Programme in the catalogue carries `awardEducationLevelId`
// — the level it awards on completion — which we use as the elevation source.

// Map an award-level NAME → the document types that holding that award would
// supply (so we can drop them from the required-doc set when the chained
// programme is in the application).
const AWARD_DOC_NAMES = {
  "High School Certificate": ["High School Certificate", "High School Transcript"],
  "Diploma":                 ["Diploma Certificate", "Diploma Transcript"],
  "Associate Degree":        ["Associate Degree Certificate", "Associate Degree Transcript"],
  "Advanced Diploma":        ["Advanced Diploma Certificate", "Advanced Diploma Transcript"],
  "Bachelor's Degree":       ["Bachelor's Degree Certificate", "Bachelor's Degree Transcript"],
  "Postgraduate Diploma":    ["Bachelor's Degree Certificate", "Bachelor's Degree Transcript"],
  "Master's Degree":         ["Master's Degree Certificate", "Master's Degree Transcript"],
  "Doctorate / PhD":         ["Doctorate / PhD Certificate", "Doctorate / PhD Transcript"],
}

// Effective set of education-level IDs this applicant can claim when
// evaluating pathway eligibility for `currentProgrammeId`. Excludes the
// current row so a programme can't elevate ITSELF.
function effectiveLevelIdsFor(currentProgrammeId) {
  const set = new Set()
  if (selectedEducationLevelId.value != null) set.add(selectedEducationLevelId.value)
  for (const row of form.programmes) {
    if (!row.programmeId || row.programmeId === currentProgrammeId) continue
    const op = catalogue.value.programmes.find(x => x.programmeId === row.programmeId)
    if (op?.awardEducationLevelId) set.add(op.awardEducationLevelId)
  }
  return set
}

// If this pathway is admissible only because of cross-app elevation (i.e.
// the student's declared level alone wouldn't pass the degree gate), return
// the source programme's display name; otherwise null.
function pathwayElevationHint(currentProgrammeId, pathway) {
  const accepted = pathway.acceptedEducationLevelIds ?? []
  if (accepted.length === 0) return null
  const studentLevelId = selectedEducationLevelId.value
  if (studentLevelId != null && accepted.includes(studentLevelId)) return null
  for (const row of form.programmes) {
    if (!row.programmeId || row.programmeId === currentProgrammeId) continue
    const op = catalogue.value.programmes.find(x => x.programmeId === row.programmeId)
    if (op?.awardEducationLevelId && accepted.includes(op.awardEducationLevelId))
      return op.name
  }
  return null
}

// Doc types any OTHER programme in the bundle will supply on graduation.
function awardSuppliedDocIds() {
  const ids = new Set()
  for (const row of form.programmes) {
    const p = catalogue.value.programmes.find(x => x.programmeId === row.programmeId)
    if (!p?.awardEducationLevelId) continue
    const lvl = educationLevels.value.find(l => l.educationLevelId === p.awardEducationLevelId)
    if (!lvl) continue
    for (const docName of AWARD_DOC_NAMES[lvl.name] ?? []) {
      const dt = documentTypes.value.find(d => d.name === docName)
      if (dt) ids.add(dt.documentTypeId)
    }
  }
  return ids
}

const requiredDocIds = computed(() => {
  const ids = new Set()
  for (const row of form.programmes) {
    const p = catalogue.value.programmes.find(x => x.programmeId === row.programmeId)
    if (!p) continue
    if (row.pathwayId != null) {
      const pw = (p.pathways ?? []).find(x => x.pathwayId === row.pathwayId)
      for (const id of pw?.requiredDocumentTypeIds ?? []) ids.add(id)
    } else {
      for (const id of p.requiredDocumentTypeIds ?? []) ids.add(id)
    }
  }
  // Drop docs that an in-application chained programme will supply
  // (e.g. don't ask for Bachelor's certificate when BBA is in the bundle).
  const supplied = awardSuppliedDocIds()
  return [...ids].filter(id => !supplied.has(id)).sort((a, b) => a - b)
})

function documentTypeName(id) { return documentTypes.value.find(d => d.documentTypeId === id)?.name ?? `#${id}` }
function specializationsFor(progId) { return (catalogue.value.programmes.find(p => p.programmeId === progId)?.specializations ?? []) }
function pathwaysFor(progId) {
  const all = catalogue.value.programmes.find(p => p.programmeId === progId)?.pathways ?? []
  const effectiveLevels = effectiveLevelIdsFor(progId)
  const studentYears    = form.yearsWorkExperience ?? 0
  return all.filter(pw => {
    // Degree gate: pathway must accept at least one of the applicant's
    // EFFECTIVE levels (declared + awards from other selected programmes).
    // If the student hasn't picked anything yet, gate is open.
    const accepted = pw.acceptedEducationLevelIds ?? []
    if (effectiveLevels.size > 0 && accepted.length > 0
        && ![...effectiveLevels].some(id => accepted.includes(id)))
      return false
    // Years gate: student's declared years must meet the pathway's minimum.
    const minYrs = pw.minimumYearsWorkExperience ?? 0
    if (studentYears < minYrs) return false
    return true
  })
}
// Per-application uploads: looked up by (specialization, doc-type).
function uploadedFor(specializationId, documentTypeId) {
  return documents.value.find(d => d.specializationId === specializationId
    && d.documentTypeId === documentTypeId)
}

// Required-doc slot list per programme, populated on entering step 5 and on
// programme changes. Backed by /v1/public/programmes/{id}/required-documents.
const requiredDocsByProgramme = ref(new Map())
function requiredDocsFor(programmeId) {
  if (!programmeId) return []
  return requiredDocsByProgramme.value.get(programmeId) ?? []
}
async function ensureRequiredDocsLoaded() {
  const ids = [...new Set(form.programmes.map(p => p.programmeId).filter(Boolean))]
  await Promise.all(ids.map(async id => {
    if (requiredDocsByProgramme.value.has(id)) return
    try {
      const res = await api.get(`/v1/public/programmes/${id}/required-documents`)
      requiredDocsByProgramme.value.set(id, res.data.items ?? [])
    } catch {
      requiredDocsByProgramme.value.set(id, [])
    }
  }))
}

function programmeNameFor(programmeId) {
  return catalogue.value.programmes.find(p => p.programmeId === programmeId)?.name ?? '—'
}
function specializationNameFor(programmeId, specializationId) {
  const p = catalogue.value.programmes.find(x => x.programmeId === programmeId)
  return p?.specializations?.find(s => s.specializationId === specializationId)?.name ?? '—'
}

function onProgrammeChange(row) { row.specializationId = ''; row.pathwayId = null }

// If the student changes their highest degree OR years of experience, drop
// any selected pathway that no longer qualifies — otherwise step 4 would
// silently keep an invalid choice that the wizard quietly omits from the
// dropdown.
function dropInvalidPathwaySelections() {
  for (const row of form.programmes) {
    if (row.pathwayId == null) continue
    const valid = pathwaysFor(row.programmeId).some(pw => pw.pathwayId === row.pathwayId)
    if (!valid) row.pathwayId = null
  }
}
watch(selectedEducationLevelId, dropInvalidPathwaySelections)
watch(() => form.yearsWorkExperience, dropInvalidPathwaySelections)
// Re-evaluate when the bundle changes — adding/removing a programme can
// elevate (or de-elevate) other rows' pathway eligibility.
watch(() => form.programmes.map(p => p.programmeId).join(','), dropInvalidPathwaySelections)

function addProgRow() { form.programmes.push({ programmeId: '', specializationId: '', modeOfStudyId: 1, pathwayId: null }) }
function removeProgRow(idx) { form.programmes.splice(idx, 1) }

const canAddLanguage = computed(() => form.languages.length < languagesCatalog.value.length)
function availableLanguagesFor(idx) {
  const taken = new Set(form.languages.map((l, i) => i !== idx ? l.languageId : null).filter(Boolean))
  return languagesCatalog.value.filter(l => !taken.has(l.languageId))
}
function addLanguage() {
  if (!canAddLanguage.value) return
  form.languages.push({ languageId: 0, proficiency: 2 })
}
function removeLanguage(idx) { form.languages.splice(idx, 1) }

async function loadCatalogue() {
  if (!slug) {
    loadError.value = { title: 'Partner not specified', detail: 'This page must be opened from your partner school\'s link.' }
    return false
  }
  // Resolve the partner catalogue first — its 404 is meaningful ("partner not found").
  // Reference data is loaded in parallel; failures there get a generic message.
  try {
    const cat = await api.get(`/v1/public/partner/${slug}/catalogue`)
    catalogue.value = cat.data
  } catch (e) {
    loadError.value = e.response?.status === 404
      ? { title: 'Partner not found', detail: `No partner called "${slug}".` }
      : { title: 'Could not load partner', detail: e.response?.data?.error ?? e.message }
    return false
  }
  try {
    const [dt, langs, nats, levels] = await Promise.all([
      api.get('/v1/public/document-types'),
      api.get('/v1/public/languages'),
      api.get('/v1/public/nationalities'),
      api.get('/v1/public/education-levels'),
    ])
    documentTypes.value = dt.data.items ?? []
    languagesCatalog.value = langs.data.items ?? []
    nationalities.value = nats.data.items ?? []
    educationLevels.value = levels.data.items ?? []
  } catch (e) {
    loadError.value = { title: 'Could not load reference data', detail: e.response?.data?.error ?? e.message }
    return false
  }
  return true
}

async function loadDraft() {
  if (!wizardToken.value) return
  try {
    const res = await api.get('/v1/public/draft-signup/state', { headers: authHeaders() })
    const d = res.data
    state.emailVerified = !!d.emailVerified
    form.firstName = d.account?.firstName ?? ''
    form.lastName = d.account?.lastName ?? ''
    form.email = d.account?.email ?? ''
    form.dateOfBirth = d.personal?.dateOfBirth?.slice(0, 10) ?? ''
    form.passportId = d.personal?.passportId ?? ''
    form.nationalityId = d.personal?.nationalityId ?? null
    form.addressLine1 = d.personal?.address?.line1 ?? ''
    form.addressLine2 = d.personal?.address?.line2 ?? ''
    form.city = d.personal?.address?.city ?? ''
    form.stateRegion = d.personal?.address?.stateRegion ?? ''
    form.postalCode = d.personal?.address?.postalCode ?? ''
    form.countryCode = d.personal?.address?.countryCode ?? ''
    form.highestDegree = d.background?.highestDegree ?? ''
    // Cap to (catalogue max + 1) so the value matches the upper "max+" bucket
    // in the dynamic dropdown. Catalogue is loaded before loadDraft runs.
    const yrsCap = maxPathwayMinYears.value + 1
    form.yearsWorkExperience = Math.min(d.background?.yearsWorkExperience ?? 0, yrsCap)
    form.languages = (d.background?.languages ?? []).map(l => ({
      languageId: l.languageId, proficiency: l.proficiency,
    }))
    if (d.enrollments?.length) {
      form.programmes = d.enrollments.map(e => ({
        programmeId: e.programmeId, specializationId: e.specializationId,
        modeOfStudyId: e.modeOfStudyId || 1,
        pathwayId: e.pathwayId ?? null,
      }))
    }
    documents.value = d.documents ?? []
    if ((d.wizardStep ?? 0) >= 6) {
      state.submitted = true
    } else {
      state.step = Math.min((d.wizardStep || 1) + 1, 6)
    }
    if (state.step >= 5) await ensureRequiredDocsLoaded()
  } catch (e) {
    if (e.response?.status === 401) {
      setToken('')
    } else {
      error.value = e.response?.data?.error ?? e.message ?? 'Failed to load draft'
    }
  }
}

function goBack() { if (state.step > 1) state.step-- }

async function startAccount() {
  if (!step1Valid.value) return
  busy.value = true
  error.value = ''
  try {
    const { blind, blindedElement } = blindPassword(form.password)
    const startRes = await api.post('/v1/public/draft-signup/start', {
      partnerSlug: slug,
      firstName: form.firstName.trim(),
      lastName: form.lastName.trim(),
      email: form.email.trim(),
      blindedElement,
    })
    const { registrationId, evaluatedElement } = startRes.data
    const clientPublicKey = deriveClientPublicKey(form.password, blind, evaluatedElement)
    const finRes = await api.post('/v1/public/draft-signup/finish', { registrationId, clientPublicKey })
    setToken(finRes.data.wizardToken)
    state.emailVerified = false
    state.step = 2
    form.password = ''
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Account creation failed'
  } finally {
    busy.value = false
  }
}

async function savePersonal() {
  busy.value = true; error.value = ''
  try {
    await api.patch('/v1/public/draft-signup/personal', {
      dateOfBirth: form.dateOfBirth || null,
      passportId: form.passportId,
      nationalityId: form.nationalityId,
      addressLine1: form.addressLine1,
      addressLine2: form.addressLine2,
      city: form.city,
      stateRegion: form.stateRegion,
      postalCode: form.postalCode,
      countryCode: form.countryCode || null,
    }, { headers: authHeaders() })
    state.step = 3
  } catch (e) { error.value = e.response?.data?.error ?? e.message }
  finally { busy.value = false }
}

async function saveBackground() {
  busy.value = true; error.value = ''
  try {
    await api.patch('/v1/public/draft-signup/background', {
      highestDegree: form.highestDegree,
      yearsWorkExperience: form.yearsWorkExperience,
      languages: form.languages
        .filter(l => l.languageId > 0)
        .map(l => ({ languageId: l.languageId, proficiency: l.proficiency })),
    }, { headers: authHeaders() })
    state.step = 4
  } catch (e) { error.value = e.response?.data?.error ?? e.message }
  finally { busy.value = false }
}

async function savePrograms() {
  if (!step4Valid.value) return
  busy.value = true; error.value = ''
  try {
    await api.put('/v1/public/draft-signup/programmes', {
      items: form.programmes.map(p => ({
        programmeId: p.programmeId,
        specializationId: p.specializationId,
        modeOfStudyId: p.modeOfStudyId,
        pathwayId: p.pathwayId ?? null,
      })),
    }, { headers: authHeaders() })
    state.step = 5
    await Promise.all([refreshDocuments(), ensureRequiredDocsLoaded()])
  } catch (e) { error.value = e.response?.data?.error ?? e.message }
  finally { busy.value = false }
}

async function refreshDocuments() {
  try {
    const res = await api.get('/v1/public/draft-signup/state', { headers: authHeaders() })
    documents.value = res.data.documents ?? []
  } catch { /* ignore */ }
}

async function onUpload(ev, specializationId, documentTypeId) {
  const file = ev.target.files?.[0]
  ev.target.value = ''
  if (!file) return
  if (file.size > 100 * 1024 * 1024) { error.value = 'File is larger than 100 MB.'; return }
  const existing = uploadedFor(specializationId, documentTypeId)
  if (existing) {
    try { await api.delete(`/v1/public/draft-signup/documents/${existing.studentDocumentId}`, { headers: authHeaders() }) }
    catch { /* ignore */ }
  }
  const fd = new FormData()
  fd.append('specializationId', specializationId)
  fd.append('documentTypeId', documentTypeId)
  fd.append('file', file)
  try {
    await api.post('/v1/public/draft-signup/documents', fd, { headers: { ...authHeaders(), 'Content-Type': 'multipart/form-data' } })
    error.value = ''
    await refreshDocuments()
  } catch (e) { error.value = e.response?.data?.error ?? e.message ?? 'Upload failed' }
}

async function onRemove(id) {
  try {
    await api.delete(`/v1/public/draft-signup/documents/${id}`, { headers: authHeaders() })
    await refreshDocuments()
  } catch (e) { error.value = e.response?.data?.error ?? e.message }
}

async function submitApplication() {
  if (!consentValid.value) return
  busy.value = true; error.value = ''
  try {
    await api.post('/v1/public/draft-signup/submit', {
      consentProcessing: form.consentProcessing,
      consentTerms: form.consentTerms,
      consentAccuracy: form.consentAccuracy,
    }, { headers: authHeaders() })
    state.submitted = true
  } catch (e) { error.value = e.response?.data?.error ?? e.message ?? 'Submit failed' }
  finally { busy.value = false }
}

onMounted(async () => {
  if (!await loadCatalogue()) { loaded.value = true; return }
  await loadDraft()
  loaded.value = true
})
</script>

<style scoped>
.apply-page { min-height: 100vh; background: linear-gradient(135deg,#eaf1fb 0%, #f2f5f9 100%); padding: 2rem 1rem; }
.apply-header { max-width: 720px; margin: 0 auto 1.25rem; display: flex; align-items: center; gap: 1rem; }
.logo { font-size: 2.2rem; font-weight: 900; color: #003366; line-height: 1; }
.org { display: flex; flex-direction: column; }
.org strong { color: #003366; }
.org span { font-size: 0.82rem; color: #666; }

.card { max-width: 720px; margin: 0 auto; background: #fff; border-radius: 12px; padding: 1.75rem 2rem; box-shadow: 0 4px 20px rgba(0,0,0,0.06); }
.card-err { border: 1.5px solid #fca5a5; }
.card-err h2 { color: #b91c1c; margin: 0 0 0.4rem; }
.card-success { text-align: center; }
.success-icon { width: 56px; height: 56px; border-radius: 50%; background: #d1fae5; color: #065f46; font-size: 2rem; line-height: 56px; margin: 0 auto 1rem; font-weight: 700; }

.step-bar { list-style: none; padding: 0; margin: 0 0 1.25rem; display: flex; justify-content: space-between; gap: 0.4rem; }
.step-bar li { flex: 1; text-align: center; font-size: 0.78rem; color: #aaa; padding-bottom: 0.45rem; border-bottom: 3px solid #e8edf4; }
.step-bar .num { display: inline-flex; width: 26px; height: 26px; border-radius: 50%; align-items: center; justify-content: center; background: #f0f3f7; color: #999; font-weight: 700; margin-right: 0.4rem; }
.step-bar .lbl { font-weight: 600; }
.step-bar li.active { color: #003366; border-bottom-color: #003366; }
.step-bar li.active .num { background: #003366; color: #fff; }
.step-bar li.done { color: #065f46; border-bottom-color: #6ee7b7; }
.step-bar li.done .num { background: #d1fae5; color: #065f46; }

.banner-warn { background: #fff7e0; border: 1px solid #f5d684; color: #856404; padding: 0.6rem 0.85rem; border-radius: 6px; font-size: 0.86rem; margin-bottom: 1rem; }
.form-error { color: #b91c1c; font-size: 0.86rem; margin: 0 0 0.6rem; }

.step { display: flex; flex-direction: column; gap: 0.85rem; }
.step-title { color: #003366; font-size: 1.15rem; margin: 0; }
.step-hint { color: #666; font-size: 0.86rem; margin: 0; }

.section-divider { font-size: 0.72rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.06em; color: #888; margin-top: 0.6rem; padding-bottom: 0.3rem; border-bottom: 1px solid #e8edf4; }

.row-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 0.85rem; }
.row-3 { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 0.65rem; }
.field { display: flex; flex-direction: column; gap: 0.3rem; }
.field-narrow { max-width: 200px; }
.field label { font-size: 0.82rem; font-weight: 600; color: #444; }
.field input, .field select {
  padding: 0.55rem 0.7rem; border: 1.5px solid #d0d7e0; border-radius: 6px;
  font-size: 0.9rem; font-family: inherit; outline: none; background: #fff;
}
.field input:focus, .field select:focus { border-color: #0055a5; }

.lang-row { display: grid; grid-template-columns: 1fr 200px 32px; gap: 0.5rem; align-items: center; }
.lang-sel, .lang-prof { padding: 0.5rem 0.65rem; border: 1.5px solid #d0d7e0; border-radius: 6px; font-size: 0.88rem; background: #fff; }

.prog-row { border: 1px solid #e8edf4; border-radius: 8px; padding: 0.85rem 1rem; background: #fafbfc; display: flex; flex-direction: column; gap: 0.7rem; position: relative; }
.pathway-block { margin-top: 0.4rem; }
.btn-row-x { align-self: flex-start; background: none; color: #b91c1c; border: 1px solid #fca5a5; border-radius: 5px; padding: 0.25rem 0.65rem; font-size: 0.78rem; cursor: pointer; }
.btn-add { background: #f0f7ff; color: #003366; border: 1.5px dashed #a0c0e0; border-radius: 6px; padding: 0.5rem 0.9rem; cursor: pointer; font-weight: 600; font-size: 0.86rem; align-self: flex-start; }

.doc-list { list-style: none; padding: 0; margin: 0; }
.doc-row { display: flex; justify-content: space-between; align-items: center; padding: 0.6rem 0; border-bottom: 1px solid #f0f3f7; gap: 1rem; }
.doc-name { display: flex; align-items: center; gap: 0.5rem; font-size: 0.9rem; }
.doc-mark { width: 22px; height: 22px; display: inline-flex; align-items: center; justify-content: center; border-radius: 50%; font-weight: 700; }
.mark-ok { background: #d1fae5; color: #065f46; }
.mark-pending { background: #f0f3f7; color: #aaa; }
.doc-meta { color: #888; font-size: 0.8rem; margin-right: 0.5rem; }
.doc-actions { display: flex; align-items: center; gap: 0.4rem; }
.doc-empty { color: #888; font-style: italic; padding: 0.65rem 0; }
.btn-upload { background: #003366; color: #fff; padding: 0.32rem 0.85rem; border-radius: 5px; font-size: 0.8rem; font-weight: 600; cursor: pointer; }
.btn-upload input { display: none; }
.btn-x { background: none; border: 1px solid #fca5a5; color: #b91c1c; border-radius: 5px; padding: 0.28rem 0.55rem; cursor: pointer; font-size: 0.78rem; }

.actions { display: flex; justify-content: space-between; align-items: center; padding-top: 0.5rem; }
.actions-end { justify-content: flex-end; }
.btn-primary { background: #003366; color: #fff; border: 0; padding: 0.55rem 1.2rem; border-radius: 6px; font-weight: 600; cursor: pointer; }
.btn-primary:hover:not(:disabled) { background: #00264d; }
.btn-primary:disabled { opacity: 0.55; cursor: not-allowed; }
.btn-back { background: #fff; color: #555; border: 1.5px solid #d0d7e0; padding: 0.5rem 1rem; border-radius: 6px; cursor: pointer; }

.consent-list { background: #fafbfc; border: 1px solid #e8edf4; border-radius: 6px; padding: 0.6rem 1rem 0.6rem 1.7rem; font-size: 0.86rem; color: #444; }
.consent-row { display: flex; gap: 0.55rem; align-items: flex-start; font-size: 0.88rem; padding: 0.4rem 0; cursor: pointer; }
.consent-row input { margin-top: 0.18rem; }
</style>
