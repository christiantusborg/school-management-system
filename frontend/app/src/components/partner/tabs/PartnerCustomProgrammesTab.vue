<template>
  <div class="cp-tab">
    <div v-if="loadError" class="err-banner">{{ loadError }}</div>
    <div v-if="loading" class="empty-state-card">Loading…</div>
    <div v-else-if="!partnerId" class="empty-state-card">No partner selected.</div>
    <div v-else-if="programmes.length === 0" class="empty-state-card">This partner has not created any programmes yet.</div>

    <div v-else>
      <div class="filter-bar-mini">
        <label class="filter-label">Status:</label>
        <select v-model="statusFilter" class="inp-status">
          <option value="">Any</option>
          <option value="Draft">Draft</option>
          <option value="Pending">Pending</option>
          <option value="Approved">Approved</option>
          <option value="Rejected">Rejected</option>
        </select>
      </div>

      <div v-for="c in filtered" :key="c.programmeId" class="clone-card">
        <div class="clone-row">
        <div class="clone-main clickable" @click="toggleExpand(c.programmeId)">
          <span class="caret">{{ expanded.has(c.programmeId) ? '▾' : '▸' }}</span>
          <strong>{{ c.name }}{{ c.schoolName ? ` (${c.schoolName})` : '' }}</strong>
          <span class="mono-code">{{ c.code }}</span>
          <span :class="['status-pill', `s-${c.status.toLowerCase()}`]">{{ c.status }}</span>
          <span v-if="c.isDisabledByAdmin" class="status-pill s-disabled">Disabled by Admin</span>
          <span v-else-if="c.isActive && c.status === 'Approved'" class="status-pill s-active">Active</span>
          <span v-if="c.hasEnrolments" class="status-pill s-enrolled">Enrolments</span>
        </div>

        <div class="clone-actions">
          <span class="muted" v-if="c.status === 'Pending'">Review the specializations below</span>
          <button class="btn-sm" :disabled="busy.has(c.programmeId)" @click="toggleDisable(c)">
            {{ c.isDisabledByAdmin ? 'Enable' : 'Disable' }}
          </button>
        </div>
        </div>

        <div v-if="expanded.has(c.programmeId)" class="clone-details">
          <div class="detail-row">
            <span class="detail-label">Programme name</span>
            <span class="detail-value rename-row">
              <input v-model="renameDraft(c).name" class="inp-rename" placeholder="Programme name" />
            </span>
          </div>
          <div class="detail-row">
            <span class="detail-label">Code</span>
            <span class="detail-value rename-row">
              <input v-model="renameDraft(c).code" class="inp-rename mono-code-input" placeholder="Code" />
              <button class="btn-sm btn-ok" :disabled="renameBusy.has(c.programmeId)" @click="saveRename(c)">
                {{ renameBusy.has(c.programmeId) ? 'Saving…' : 'Save name & code' }}
              </button>
              <span v-if="renameSaved.has(c.programmeId)" class="muted" style="color:#1c7a4a"> ✓ saved</span>
              <span v-else-if="renameErr[c.programmeId]" class="reject-reason">{{ renameErr[c.programmeId] }}</span>
            </span>
          </div>
          <div class="detail-row">
            <span class="detail-label">Description</span>
            <span class="detail-value">{{ c.description?.trim() || '—' }}</span>
          </div>
          <div class="detail-row">
            <span class="detail-label">Award on completion</span>
            <span class="detail-value">{{ awardName(c.awardEducationLevelId) || '—' }}</span>
          </div>
          <div class="detail-row">
            <span class="detail-label">School</span>
            <span class="detail-value">
              <select :value="c.schoolId ?? ''" class="inp-status"
                      @change="setSchool(c, $event.target.value || null)">
                <option value="">— none —</option>
                <option v-for="s in schools" :key="s.schoolId" :value="s.schoolId">{{ s.name }}</option>
              </select>
              <span v-if="schoolBusy.has(c.programmeId)" class="muted"> saving…</span>
              <span v-else-if="schoolSaved.has(c.programmeId)" class="muted" style="color:#1c7a4a"> ✓ saved</span>
              <span v-else-if="schoolErr[c.programmeId]" class="reject-reason">{{ schoolErr[c.programmeId] }}</span>
            </span>
          </div>
          <div class="detail-row">
            <span class="detail-label">Duration of study</span>
            <span class="detail-value dur-range-row">
              <input type="number" min="1" v-model.number="durDraft(c).min" class="inp-dur" />
              <span>–</span>
              <input type="number" min="1" v-model.number="durDraft(c).max" class="inp-dur" />
              <span class="dur-toggle">
                <button type="button" :class="['dur-unit', { on: durDraft(c).unit === 'Month' }]" @click="durDraft(c).unit = 'Month'">Months</button>
                <button type="button" :class="['dur-unit', { on: durDraft(c).unit === 'Day' }]" @click="durDraft(c).unit = 'Day'">Days</button>
              </span>
              <button class="btn-sm btn-ok" :disabled="durBusy.has(c.programmeId)" @click="saveDuration(c)">
                {{ durBusy.has(c.programmeId) ? 'Saving…' : 'Save range' }}
              </button>
              <button class="btn-sm" @click="openBackfill(c)">Backfill student durations…</button>
              <span v-if="durSaved.has(c.programmeId)" class="muted" style="color:#1c7a4a"> ✓ saved</span>
              <span v-else-if="durErr[c.programmeId]" class="reject-reason">{{ durErr[c.programmeId] }}</span>
            </span>
          </div>
          <div class="detail-row">
            <span class="detail-label">Entry Requirements</span>
            <span class="detail-value">{{ pathwayNames(c.pathwayIds) || '—' }}</span>
          </div>
          <div v-if="c.rejectionReason" class="detail-row">
            <span class="detail-label">Rejection reason</span>
            <span class="detail-value">{{ c.rejectionReason }}</span>
          </div>

          <div class="section-label">Specializations &amp; Subjects</div>
          <div v-if="specsFor(c.programmeId).length === 0" class="empty-note">No specializations.</div>
          <div v-for="s in specsFor(c.programmeId)" :key="s.specializationId" class="spec-card">
            <div class="spec-row" @click="toggleSpec(s.specializationId)">
              <span class="caret">{{ openSpecs.has(s.specializationId) ? '▾' : '▸' }}</span>
              <strong>{{ s.name }}</strong>
              <span class="mono-code">{{ s.code }}</span>
              <button class="btn-sm" title="Edit specialization code" @click.stop="editSpecCode(s)">✎</button>
              <span class="muted">{{ subjectsFor(s.specializationId).length }} subjects</span>
              <span v-if="s.instructionLanguage" class="pill">{{ s.instructionLanguage }}</span>
              <span class="pill" :class="(s.offerAcceptanceMode === 'AutoAccept') ? 'pill-auto' : 'pill-student'">
                {{ s.offerAcceptanceMode === 'AutoAccept' ? 'auto-accept' : 'student-accept' }}
              </span>
              <span :class="['status-pill', `s-${specStatus(s).toLowerCase()}`]">{{ specStatus(s) }}</span>
              <span class="spec-actions" @click.stop>
                <template v-if="specStatus(s) === 'Pending' || specStatus(s) === 'Draft'">
                  <button class="btn-sm btn-ok" :disabled="specBusy.has(s.specializationId)" @click="approveSpec(s)">Approve</button>
                  <template v-if="specStatus(s) === 'Pending'">
                    <button v-if="rejectingSpecId !== s.specializationId" class="btn-sm btn-warn" @click="openRejectSpec(s)">Reject</button>
                    <template v-else>
                      <input v-model="specRejectReason" class="inp-add" placeholder="Reason for rejection…" />
                      <button class="btn-sm btn-warn" :disabled="!specRejectReason.trim() || specBusy.has(s.specializationId)" @click="confirmRejectSpec(s)">Confirm</button>
                      <button class="btn-sm" @click="cancelRejectSpec">Cancel</button>
                    </template>
                  </template>
                </template>
                <template v-else-if="specStatus(s) === 'Rejected'">
                  <span class="reject-reason">{{ s.approvalRejectionReason || '' }}</span>
                  <button class="btn-sm btn-ok" :disabled="specBusy.has(s.specializationId)" @click="approveSpec(s)">Approve</button>
                  <button class="btn-sm" :disabled="specBusy.has(s.specializationId)" @click="reopenSpec(s)">Move to Pending</button>
                </template>
              </span>
            </div>
            <div v-if="openSpecs.has(s.specializationId)" class="subj-table">
              <div class="subj-header">
                <span class="col-code">Code</span>
                <span class="col-name">Subject</span>
                <span class="col-rubric">Grading</span>
                <span class="col-thesis">Thesis</span>
                <span class="col-ects">ECTS</span>
              </div>
              <div v-if="subjectsFor(s.specializationId).length === 0" class="empty-note">No subjects.</div>
              <div v-for="sub in subjectsFor(s.specializationId)" :key="sub.subjectId" class="subj-row">
                <span class="col-code mono-code">{{ sub.code || '—' }}</span>
                <span class="col-name">{{ sub.name }}</span>
                <span class="col-rubric"><SubjectRubricButton :subject="sub" /></span>
                <span class="col-thesis">
                  <input type="radio" :name="`thesis-${s.specializationId}`"
                         :checked="sub.isThesis" :disabled="thesisBusy.has(s.specializationId)"
                         @change="setThesis(s.specializationId, sub)"
                         title="Mark this subject as the thesis / final project" />
                </span>
                <span class="col-ects">{{ sub.ects }}</span>
              </div>
              <p v-if="thesisErr[s.specializationId]" class="card-toggle-err" style="margin:.2rem 0 0;">{{ thesisErr[s.specializationId] }}</p>
            </div>
          </div>

          <div class="clone-spec-row">
            <select v-model="cloneSelection[c.programmeId]" class="inp-status">
              <option value="">— clone a specialization from… —</option>
              <optgroup label="This programme">
                <option v-for="src in cloneSources(c.programmeId, 'self')" :key="src.specializationId" :value="src.specializationId">
                  {{ src.name }} ({{ src.code }})
                </option>
              </optgroup>
              <optgroup label="Core programmes (same award level)">
                <option v-for="src in cloneSources(c.programmeId, 'core')" :key="src.specializationId" :value="src.specializationId">
                  {{ src.programmeCode }} · {{ src.name }} ({{ src.code }})
                </option>
              </optgroup>
            </select>
            <button class="btn-sm btn-ok" :disabled="!cloneSelection[c.programmeId] || cloneBusy.has(c.programmeId)"
                    @click="cloneSpec(c)">
              {{ cloneBusy.has(c.programmeId) ? 'Cloning…' : 'Clone specialization' }}
            </button>
            <span class="muted">Admin clones are approved immediately.</span>
            <span v-if="cloneErr[c.programmeId]" class="reject-reason">{{ cloneErr[c.programmeId] }}</span>
          </div>
        </div>

        <div class="card-toggle-row">
          <label class="card-toggle">
            <input type="checkbox" :checked="c.issueDigitalStudentCard" @change="toggleStudentCard(c, $event.target.checked)" />
            Digital student card
          </label>
          <span v-if="cardErr[c.programmeId]" class="card-toggle-err">{{ cardErr[c.programmeId] }}</span>
          <span v-else-if="cardSaved[c.programmeId]" class="card-toggle-ok">✓ Saved</span>
          <span class="muted" style="font-size:.72rem;">Generated at offer acceptance; designable below like the other letters.</span>
        </div>
        <LetterButtonsRow :programme-id="c.programmeId" :programme-name="c.name" :partner-id="partnerId"
          :show-student-card="!!c.issueDigitalStudentCard" />
      </div>
    </div>

    <div v-if="backfill" class="bf-overlay" @click.self="backfill = null">
      <div class="bf-modal">
        <div class="bf-head">
          <h3>Backfill approved durations — {{ backfill.name }}</h3>
          <button class="bf-x" @click="backfill = null">✕</button>
        </div>
        <p class="muted">Sets each student's approved duration to the number of days between their commencement and graduation dates (inclusive). Only students with BOTH dates change; review before applying.</p>
        <p v-if="backfill.error" class="reject-reason">{{ backfill.error }}</p>
        <p v-if="backfill.loading" class="muted">Loading preview…</p>
        <template v-else>
          <p class="bf-summary"><strong>{{ backfill.willChange }}</strong> will change · <strong>{{ backfill.skipped }}</strong> skipped</p>
          <div class="bf-table-wrap">
            <table class="bf-table">
              <thead><tr><th>Student #</th><th>Name</th><th>Commencement</th><th>Graduation</th><th>Current</th><th>New</th></tr></thead>
              <tbody>
                <tr v-for="r in backfill.items" :key="r.studentEnrollmentId" :class="{ skip: r.skipped }">
                  <td>{{ r.studentNumber }}</td>
                  <td>{{ r.name }}</td>
                  <td>{{ (r.commencement || '').slice(0, 10) || '—' }}</td>
                  <td>{{ (r.graduation || '').slice(0, 10) || '—' }}</td>
                  <td>{{ r.currentLabel || '—' }}</td>
                  <td>
                    <span v-if="r.skipped" class="bf-skip-reason">skip: {{ r.skipReason }}</span>
                    <strong v-else>{{ r.newDays }} days</strong>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
          <div class="bf-actions">
            <button class="btn-sm btn-ok" :disabled="backfill.applying || !backfill.willChange" @click="applyBackfill">
              {{ backfill.applying ? 'Applying…' : `Apply to ${backfill.willChange} student(s)` }}
            </button>
            <button class="btn-sm" @click="backfill = null">Cancel</button>
          </div>
        </template>
      </div>
    </div>
  </div>
</template>

<script setup>
import { reactive, ref, computed, watch, onMounted } from 'vue'
import apiClient from '../../../api/client.js'
import LetterButtonsRow from '../../letters/LetterButtonsRow.vue'
import SubjectRubricButton from '../../admin/SubjectRubricButton.vue'

const props = defineProps({
  partnerId: { type: String, default: '' },
  partnerName: { type: String, default: '' },
})

const programmes = ref([])
const specializations = ref([])
const subjects = ref([])
const educationLevels = ref([])
const pathways = ref([])
const schools = ref([])
const schoolBusy = reactive(new Set())
const schoolSaved = reactive(new Set())
const schoolErr = reactive({})
// Editable name/code drafts, seeded from the programme on first access.
// Duration range editing (admin) + backfill.
const durDrafts = reactive({})
const durBusy = reactive(new Set())
const durSaved = reactive(new Set())
const durErr = reactive({})
const backfill = ref(null)
function durDraft(c) {
  if (!durDrafts[c.programmeId])
    durDrafts[c.programmeId] = { min: c.minDurationMonths ?? 1, max: c.maxDurationMonths ?? 1, unit: c.durationRangeUnit || 'Month' }
  return durDrafts[c.programmeId]
}
async function saveDuration(c) {
  if (durBusy.has(c.programmeId)) return
  const d = durDrafts[c.programmeId]
  if (!d || d.min < 1 || d.max < d.min) { durErr[c.programmeId] = 'Need 1 ≤ min ≤ max.'; return }
  durBusy.add(c.programmeId); durSaved.delete(c.programmeId); durErr[c.programmeId] = ''
  try {
    await apiClient.patch(`/v1/school/programmes/${c.programmeId}/duration-range`,
      { minDurationMonths: d.min, maxDurationMonths: d.max, durationRangeUnit: d.unit })
    c.minDurationMonths = d.min; c.maxDurationMonths = d.max; c.durationRangeUnit = d.unit
    durSaved.add(c.programmeId)
    setTimeout(() => durSaved.delete(c.programmeId), 2500)
  } catch (e) { durErr[c.programmeId] = e.response?.data?.error ?? e.message ?? 'Save failed' }
  finally { durBusy.delete(c.programmeId) }
}
async function openBackfill(c) {
  backfill.value = { programmeId: c.programmeId, name: c.name, loading: true, applying: false, error: '', items: [], willChange: 0, skipped: 0 }
  try {
    const res = await apiClient.get(`/v1/school/programmes/${c.programmeId}/duration-backfill`)
    Object.assign(backfill.value, { loading: false, items: res.data.items ?? [], willChange: res.data.willChange ?? 0, skipped: res.data.skipped ?? 0 })
  } catch (e) {
    if (backfill.value) { backfill.value.loading = false; backfill.value.error = e.response?.data?.error ?? e.message ?? 'Failed to load' }
  }
}
async function applyBackfill() {
  if (!backfill.value || backfill.value.applying) return
  backfill.value.applying = true; backfill.value.error = ''
  try {
    const res = await apiClient.post(`/v1/school/programmes/${backfill.value.programmeId}/duration-backfill`)
    backfill.value = null
    alert(`Applied to ${res.data.applied} student(s), ${res.data.skipped} skipped.`)
  } catch (e) { backfill.value.applying = false; backfill.value.error = e.response?.data?.error ?? e.message ?? 'Apply failed' }
}

const renameDrafts = reactive({})
const renameBusy = reactive(new Set())
const renameSaved = reactive(new Set())
const renameErr = reactive({})
function renameDraft(c) {
  if (!renameDrafts[c.programmeId]) renameDrafts[c.programmeId] = { name: c.name, code: c.code }
  return renameDrafts[c.programmeId]
}
const loading = ref(false)
const loadError = ref('')
const statusFilter = ref('')
const rejectingId = ref(null)
const rejectReason = ref('')
const busy = ref(new Set())
const expanded = reactive(new Set()) // programmeId
const openSpecs = reactive(new Set()) // specializationId

function toggleExpand(id) {
  expanded.has(id) ? expanded.delete(id) : expanded.add(id)
  if (expanded.has(id) && !cloneSourceMap[id]) loadCloneSources(id)
}
function toggleSpec(id) {
  openSpecs.has(id) ? openSpecs.delete(id) : openSpecs.add(id)
}

function specsFor(programmeId) {
  return specializations.value.filter(s => s.programmeId === programmeId && !s.deletedAt)
}
function subjectsFor(specializationId) {
  return subjects.value.filter(s => s.specializationId === specializationId && !s.deletedAt)
}
function awardName(id) {
  if (!id) return ''
  const e = educationLevels.value.find(x => x.educationLevelId === id)
  return e?.name ?? ''
}
function pathwayNames(ids) {
  if (!ids?.length) return ''
  return ids
    .map(id => pathways.value.find(p => p.pathwayId === id)?.name)
    .filter(Boolean)
    .join(', ')
}

const filtered = computed(() =>
  statusFilter.value
    ? programmes.value.filter(p => p.status === statusFilter.value)
    : programmes.value
)

async function load() {
  if (!props.partnerId) { programmes.value = []; return }
  loading.value = true
  loadError.value = ''
  try {
    // Load programmes + their child catalogue data in parallel so the inline
    // expansion can render specializations / subjects / pathways without a
    // second roundtrip per row.
    const [progRes, specRes, subRes, pathRes, elRes, schoolRes] = await Promise.all([
      apiClient.get('/v1/school/programmes?ownership=partner'),
      apiClient.get('/v1/school/specializations'),
      apiClient.get('/v1/school/subjects'),
      apiClient.get('/v1/school/system-config/pathways'),
      apiClient.get('/v1/school/system-config/education-levels'),
      apiClient.get('/v1/school/schools/options'),
    ])
    schools.value = schoolRes.data.items ?? []
    // The list endpoint exposes the owner under `ownerId`, not `partnerId` —
    // the previous filter never matched, so partner-owned programmes were
    // silently hidden in the admin drawer.
    programmes.value = (progRes.data.items ?? []).filter(p => p.ownerId === props.partnerId)
    specializations.value = specRes.data.items ?? []
    subjects.value        = subRes.data.items ?? []
    pathways.value        = pathRes.data.items ?? []
    educationLevels.value = elRes.data.items ?? []
  } catch (e) {
    loadError.value = e.response?.data?.message ?? e.message ?? 'Failed to load'
  } finally {
    loading.value = false
  }
}

// ── Spec-level review (approval moved down from programme level) ────────────
const specBusy = ref(new Set())
const rejectingSpecId = ref(null)
const specRejectReason = ref('')
const cloneSourceMap = reactive({})
const cloneSelection = reactive({})
const cloneBusy = ref(new Set())
const cloneErr = reactive({})

function specStatus(s) {
  // approvalStatus: 0 Draft, 1 Pending, 2 Approved, 3 Rejected; null = Draft
  return ['Draft', 'Pending', 'Approved', 'Rejected'][s.approvalStatus ?? 0] ?? 'Draft'
}
// Admission can correct the specialization CODE on custom programmes.
async function editSpecCode(spec) {
  const code = prompt('Specialization code:', spec.code)
  if (code == null || !code.trim() || code.trim() === spec.code) return
  try {
    await apiClient.put(`/v1/school/specializations/${spec.specializationId}`, { code: code.trim() })
    await load()
  } catch (e) { loadError.value = e.response?.data?.message ?? e.response?.data?.error ?? e.message ?? 'Failed to update code' }
}

async function approveSpec(s) {
  if (specBusy.value.has(s.specializationId)) return
  specBusy.value.add(s.specializationId)
  try {
    await apiClient.post(`/v1/school/specializations/${s.specializationId}/approve`)
    await load()
  } catch (e) { loadError.value = e.response?.data?.error ?? e.message ?? 'Approve failed' }
  finally { specBusy.value.delete(s.specializationId) }
}
function openRejectSpec(s) { rejectingSpecId.value = s.specializationId; specRejectReason.value = '' }
function cancelRejectSpec() { rejectingSpecId.value = null; specRejectReason.value = '' }
async function confirmRejectSpec(s) {
  const reason = specRejectReason.value.trim()
  if (!reason || specBusy.value.has(s.specializationId)) return
  specBusy.value.add(s.specializationId)
  try {
    await apiClient.post(`/v1/school/specializations/${s.specializationId}/reject`, { reason })
    cancelRejectSpec()
    await load()
  } catch (e) { loadError.value = e.response?.data?.error ?? e.message ?? 'Reject failed' }
  finally { specBusy.value.delete(s.specializationId) }
}
async function reopenSpec(s) {
  if (specBusy.value.has(s.specializationId)) return
  specBusy.value.add(s.specializationId)
  try {
    await apiClient.post(`/v1/school/specializations/${s.specializationId}/reopen`)
    await load()
  } catch (e) { loadError.value = e.response?.data?.error ?? e.message ?? 'Reopen failed' }
  finally { specBusy.value.delete(s.specializationId) }
}
async function loadCloneSources(programmeId) {
  try {
    const res = await apiClient.get(`/v1/school/programmes/${programmeId}/spec-clone-sources`)
    cloneSourceMap[programmeId] = res.data.items ?? []
  } catch { cloneSourceMap[programmeId] = [] }
}
function cloneSources(programmeId, kind) {
  return (cloneSourceMap[programmeId] ?? []).filter(x => x.source === kind)
}
async function cloneSpec(c) {
  const src = cloneSelection[c.programmeId]
  if (!src || cloneBusy.value.has(c.programmeId)) return
  cloneBusy.value.add(c.programmeId)
  cloneErr[c.programmeId] = ''
  try {
    await apiClient.post(`/v1/school/programmes/${c.programmeId}/specializations/clone`, { sourceSpecializationId: src })
    cloneSelection[c.programmeId] = ''
    await Promise.all([load(), loadCloneSources(c.programmeId)])
  } catch (e) { cloneErr[c.programmeId] = e.response?.data?.error ?? e.message ?? 'Clone failed' }
  finally { cloneBusy.value.delete(c.programmeId) }
}

async function approve(c) {
  if (busy.value.has(c.programmeId)) return
  busy.value.add(c.programmeId)
  try {
    await apiClient.post(`/v1/school/programmes/${c.programmeId}/approve`)
    await load()
  } catch (e) { loadError.value = e.response?.data?.error ?? e.message ?? 'Approve failed' }
  finally { busy.value.delete(c.programmeId) }
}

function openReject(c) {
  rejectingId.value = c.programmeId
  rejectReason.value = ''
}

async function confirmReject(c) {
  const reason = rejectReason.value.trim()
  if (!reason) return
  if (busy.value.has(c.programmeId)) return
  busy.value.add(c.programmeId)
  try {
    await apiClient.post(`/v1/school/programmes/${c.programmeId}/reject`, { reason })
    rejectingId.value = null
    rejectReason.value = ''
    await load()
  } catch (e) { loadError.value = e.response?.data?.error ?? e.message ?? 'Reject failed' }
  finally { busy.value.delete(c.programmeId) }
}

function cancelReject() {
  rejectingId.value = null
  rejectReason.value = ''
}

async function reopen(c) {
  if (busy.value.has(c.programmeId)) return
  busy.value.add(c.programmeId)
  try {
    await apiClient.post(`/v1/school/programmes/${c.programmeId}/reopen`)
    await load()
  } catch (e) { loadError.value = e.response?.data?.error ?? e.message ?? 'Reopen failed' }
  finally { busy.value.delete(c.programmeId) }
}

// Admin renames the programme's name + code. Include award/school so the PUT's
// direct award assignment doesn't wipe them.
async function saveRename(c) {
  if (renameBusy.has(c.programmeId)) return
  const d = renameDrafts[c.programmeId]
  const name = (d?.name ?? '').trim()
  const code = (d?.code ?? '').trim()
  if (!name || !code) { renameErr[c.programmeId] = 'Name and code are required.'; return }
  renameBusy.add(c.programmeId)
  renameSaved.delete(c.programmeId)
  renameErr[c.programmeId] = ''
  try {
    await apiClient.put(`/v1/school/programmes/${c.programmeId}`, {
      name, code,
      awardEducationLevelId: c.awardEducationLevelId ?? null,
      schoolId: c.schoolId ?? null,
    })
    c.name = name
    c.code = code
    renameSaved.add(c.programmeId)
    setTimeout(() => renameSaved.delete(c.programmeId), 2500)
  } catch (e) {
    renameErr[c.programmeId] = e.response?.data?.message ?? e.message ?? 'Rename failed'
  } finally {
    renameBusy.delete(c.programmeId)
  }
}

// Admin can reassign a programme's school here. Include name/code/award in the
// PUT so the endpoint's direct award assignment doesn't wipe the award level.
async function setSchool(c, schoolId) {
  if (schoolBusy.has(c.programmeId)) return
  schoolBusy.add(c.programmeId)
  schoolSaved.delete(c.programmeId)
  schoolErr[c.programmeId] = ''
  try {
    await apiClient.put(`/v1/school/programmes/${c.programmeId}`, {
      name: c.name,
      code: c.code,
      awardEducationLevelId: c.awardEducationLevelId ?? null,
      schoolId,
    })
    c.schoolId = schoolId
    c.schoolName = schools.value.find(s => s.schoolId === schoolId)?.name ?? null
    schoolSaved.add(c.programmeId)
    setTimeout(() => schoolSaved.delete(c.programmeId), 2500)
  } catch (e) {
    schoolErr[c.programmeId] = e.response?.data?.message ?? e.message ?? 'Failed to change school'
  } finally {
    schoolBusy.delete(c.programmeId)
  }
}

async function toggleDisable(c) {
  if (busy.value.has(c.programmeId)) return
  busy.value.add(c.programmeId)
  try {
    await apiClient.patch(`/v1/school/programmes/${c.programmeId}/admin-disable`, { disabled: !c.isDisabledByAdmin })
    await load()
  } catch (e) { loadError.value = e.response?.data?.error ?? e.message ?? 'Disable toggle failed' }
  finally { busy.value.delete(c.programmeId) }
}

watch(() => props.partnerId, load)
onMounted(load)

// ── Thesis subject (radio: exactly one per specialization) ──────────────────
const thesisBusy = reactive(new Set())
const thesisErr = reactive({})
async function setThesis(specializationId, sub) {
  if (thesisBusy.has(specializationId)) return
  thesisBusy.add(specializationId)
  thesisErr[specializationId] = ''
  try {
    await apiClient.put(`/v1/school/subjects/${sub.subjectId}/thesis`)
    // Reflect the atomic server change locally: one thesis per specialization.
    subjects.value.forEach(x => {
      if (x.specializationId === specializationId) x.isThesis = x.subjectId === sub.subjectId
    })
  } catch (e) {
    thesisErr[specializationId] = e.response?.data?.message ?? e.message ?? 'Failed to save'
  } finally {
    thesisBusy.delete(specializationId)
  }
}

// ── Digital student card toggle (admin drawer; Admission-only surface) ──────
const cardErr = reactive({})
const cardSaved = reactive({})
async function toggleStudentCard(prog, checked) {
  const id = prog.programmeId
  cardErr[id] = ''
  try {
    // PUT overwrites awardEducationLevelId unconditionally, so echo it back.
    await apiClient.put(`/v1/school/programmes/${id}`, {
      name: prog.name,
      code: prog.code,
      awardEducationLevelId: prog.awardEducationLevelId ?? null,
      issueDigitalStudentCard: checked,
    })
    prog.issueDigitalStudentCard = checked
    cardSaved[id] = true
    setTimeout(() => { cardSaved[id] = false }, 3000)
  } catch (e) {
    cardErr[id] = e.response?.data?.message ?? e.message ?? 'Failed to save'
  }
}
</script>

<style scoped>
.cp-tab { padding: .25rem 0; }
.err-banner { background: #fde7e7; color: #8a1515; padding: .55rem .8rem; border-radius: 6px; font-size: .85rem; margin-bottom: .75rem; }
.filter-bar-mini { display: flex; align-items: center; gap: .5rem; margin-bottom: .6rem; }
.filter-label { font-size: .82rem; color: #5f6e85; }
.inp-status { padding: .3rem .5rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .82rem; }
.empty-state-card { padding: 1rem; background: #f6f9fd; color: #5f6e85; border-radius: 8px; text-align: center; }
.clone-row { display: flex; justify-content: space-between; align-items: center; padding: .55rem .8rem; background: #f6f9fd; border-radius: 6px; margin-bottom: .4rem; gap: .5rem; flex-wrap: wrap; }
.clone-main { display: flex; align-items: center; gap: .5rem; }
.mono-code { font-family: monospace; font-size: .72rem; color: #6b7888; background: #fff; border: 1px solid #e1e6ed; padding: 1px 5px; border-radius: 4px; }
.status-pill { font-size: .7rem; padding: 2px 8px; border-radius: 10px; text-transform: uppercase; }
.s-draft    { background: #ecf0f6; color: #5f6e85; }
.s-pending  { background: #fff1cc; color: #8a6b16; }
.s-approved { background: #d7f0df; color: #1c7a4a; }
.s-rejected { background: #fde7e5; color: #a8241e; }
.s-active   { background: #e0f1ff; color: #1058a4; }
.s-enrolled { background: #fff2cc; color: #7a5c00; }
.s-disabled { background: #ecd4d2; color: #8a1515; }
.clone-actions { display: flex; align-items: center; gap: .4rem; }
.btn-sm { padding: .3rem .6rem; font-size: .78rem; border: 1px solid #cfd7e3; background: #fff; border-radius: 5px; cursor: pointer; }
.btn-ok { border-color: #1c7a4a; color: #1c7a4a; }
.btn-warn { border-color: #b63329; color: #b63329; }
.inp-add { padding: .35rem .6rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .82rem; min-width: 200px; }
.rename-row { display: flex; align-items: center; gap: .5rem; flex-wrap: wrap; }
.inp-rename { padding: .3rem .55rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .84rem; min-width: 240px; }
.mono-code-input { font-family: monospace; min-width: 160px; }
.reject-reason { font-size: .78rem; color: #a8241e; }

.clone-card { background: #fff; border: 1px solid #e0e6ee; border-radius: 6px; margin-bottom: .5rem; overflow: hidden; }
.clickable { cursor: pointer; user-select: none; flex: 1 1 auto; }
.caret { font-size: .8rem; color: #6b7888; margin-right: .15rem; min-width: .8rem; }
.muted { font-size: .78rem; color: #5f6e85; }

.clone-details { padding: .65rem .85rem .85rem; border-top: 1px solid #e8edf3; background: #fbfcfe; }
.detail-row { display: flex; gap: .65rem; padding: .25rem 0; font-size: .82rem; }
.detail-label { color: #5f6e85; min-width: 9rem; font-weight: 600; }
.detail-value { color: #1a2d4f; flex: 1; }
.section-label { margin-top: .65rem; padding: .25rem 0; font-size: .72rem; color: #6b7888; font-weight: 700; text-transform: uppercase; letter-spacing: .04em; }
.empty-note { font-size: .8rem; color: #8a93a4; padding: .25rem 0 .5rem; font-style: italic; }

.spec-card { border: 1px solid #e8edf3; border-radius: 5px; margin-bottom: .35rem; }
.spec-actions { display: flex; align-items: center; gap: .35rem; margin-left: auto; }
.clone-spec-row { display: flex; align-items: center; gap: .5rem; margin-top: .5rem; flex-wrap: wrap; }
.spec-row { display: flex; align-items: center; gap: .5rem; padding: .45rem .65rem; cursor: pointer; user-select: none; background: #f7fafd; }
.pill { font-size: .68rem; padding: 1px 7px; border-radius: 10px; background: #ecf0f6; color: #5f6e85; }
.pill-student { background: #eef3fb; color: #1a4d8c; }
.pill-auto { background: #eaf6ec; color: #226c3b; }
.subj-table { padding: .35rem .9rem .65rem; }
.subj-header, .subj-row { display: flex; align-items: center; padding: .25rem 0; font-size: .82rem; }
.subj-header { color: #6b7888; font-weight: 700; text-transform: uppercase; font-size: .68rem; letter-spacing: .04em; border-bottom: 1px solid #e8edf3; }
.col-rubric { width: 150px; flex-shrink: 0; }
.col-code { width: 110px; flex-shrink: 0; }
.col-name { flex: 1; }
.col-ects { width: 50px; text-align: right; flex-shrink: 0; }
.col-thesis { width: 56px; text-align: center; flex-shrink: 0; }
.col-thesis input { cursor: pointer; }

.card-toggle-row { display: flex; align-items: center; gap: .7rem; padding: .35rem 0 0; }
.card-toggle { display: flex; align-items: center; gap: .4rem; font-size: .8rem; font-weight: 600; color: #1a4d8c; cursor: pointer; }
.card-toggle-ok { color: #1c7a4a; font-size: .76rem; font-weight: 600; }
.card-toggle-err { color: #b42318; font-size: .76rem; }
.dur-range-row { display: flex; align-items: center; gap: .4rem; flex-wrap: wrap; }
.inp-dur { width: 70px; padding: .3rem .45rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .82rem; }
.dur-toggle { display: inline-flex; border: 1px solid #cfd7e3; border-radius: 6px; overflow: hidden; }
.dur-unit { border: none; background: #fff; padding: .3rem .6rem; font-size: .78rem; cursor: pointer; }
.dur-unit.on { background: #0b2e59; color: #fff; }
.bf-overlay { position: fixed; inset: 0; background: rgba(10,25,47,.45); z-index: 1100; display: flex; align-items: center; justify-content: center; }
.bf-modal { width: 820px; max-width: 96vw; max-height: 90vh; overflow-y: auto; background: #fff; border-radius: 10px; padding: 1rem 1.2rem; }
.bf-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: .4rem; }
.bf-head h3 { margin: 0; font-size: 1rem; color: #0b2e59; }
.bf-x { background: none; border: none; cursor: pointer; font-size: 1rem; }
.bf-summary { font-size: .86rem; }
.bf-table-wrap { max-height: 50vh; overflow-y: auto; border: 1px solid #e8edf3; border-radius: 6px; }
.bf-table { width: 100%; border-collapse: collapse; font-size: .8rem; }
.bf-table th { position: sticky; top: 0; background: #f6f9fd; text-align: left; padding: .35rem .5rem; font-size: .66rem; text-transform: uppercase; color: #6b7888; }
.bf-table td { padding: .3rem .5rem; border-bottom: 1px solid #f0f3f7; }
.bf-table tr.skip { color: #8a93a4; }
.bf-skip-reason { font-size: .72rem; color: #8a6b16; }
.bf-actions { display: flex; gap: .5rem; margin-top: .6rem; }
</style>
