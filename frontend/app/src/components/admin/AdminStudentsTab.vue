<template>
  <div class="ps-tab">
    <div v-if="loadError" class="err-banner">{{ loadError }}</div>

    <!-- Status filter chips, default focus on the IBSS action queue -->
    <div class="status-row">
      <button v-for="s in STATUS_FILTERS" :key="s.id ?? 'all'"
              :class="['status-chip', { active: filterStatusId === s.id }]"
              @click="filterStatusId = s.id">
        {{ s.label }}<span class="chip-count">{{ countFor(s.id) }}</span>
      </button>
    </div>

    <div class="filter-row">
      <input v-model="search" class="inp" placeholder="Search by name, username or student ID…" />
      <select v-model="filterProgrammeId" class="inp">
        <option value="">All programmes</option>
        <option v-for="p in programmesAvailable" :key="p.programmeId" :value="p.programmeId">{{ p.name }}</option>
      </select>
      <select v-model="filterSpecializationId" class="inp">
        <option value="">All specializations</option>
        <option v-for="m in specializationsAvailable" :key="m.specializationId" :value="m.specializationId">{{ m.name }}</option>
      </select>
      <button class="btn-refresh" :disabled="loading" @click="load">{{ loading ? 'Loading…' : '↻' }}</button>
    </div>

    <div v-if="!loading && filtered.length === 0" class="empty">No students match.</div>
    <table v-else-if="!loading" class="data-table">
      <thead>
        <tr>
          <th>Student #</th><th>Name</th><th v-if="!partnerId">Partner</th>
          <th>Email</th><th>Enrolments</th><th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="s in filtered" :key="s.studentId" class="data-row">
          <td class="mono">{{ s.studentNumber }}</td>
          <td>{{ s.firstName ?? '—' }} {{ s.lastName ?? '' }}<br><small class="muted">@{{ s.username }}</small></td>
          <td v-if="!partnerId">{{ s.partnerName }}</td>
          <td>{{ s.email ?? '—' }}<span v-if="!s.emailVerified" class="s-badge unverified">unverified</span></td>
          <td>
            <div v-for="e in s.enrollments" :key="e.studentEnrollmentId" class="enrol-line">
              <span class="enr-prog">{{ e.programmeCode }}</span> · {{ e.specializationName }}
              <span :class="['s-badge', statusClass(e.statusCode)]">{{ e.statusName }}</span>
              <button v-if="e.statusCode !== 'AwaitingGradesApproval'" class="btn-review-sm"
                      :disabled="!canAdminReview(e)"
                      :title="canAdminReview(e) ? '' : 'Not in the Admission queue.'"
                      @click.stop="canAdminReview(e) && openReview(s.studentId, e.studentEnrollmentId)">Review</button>
              <button v-else class="btn-review-sm btn-grades-approve"
                      @click.stop="openGradeReview(s, e)">
                Approve grades
              </button>
            </div>
          </td>
          <td></td>
        </tr>
      </tbody>
    </table>

    <!-- Grade review modal -->
    <transition name="fade">
      <div v-if="gradeModal" class="manage-overlay" @click.self="gradeModal = null">
        <div class="manage-modal grade-modal">
          <div class="manage-hdr">
            <h3>Approve grades</h3>
            <button class="drawer-close" @click="gradeModal = null">✕</button>
          </div>
          <p class="manage-sub">{{ gradeModal.studentName }} · {{ gradeModal.programmeCode }} · {{ gradeModal.specializationName }}</p>
          <div class="manage-body">
            <p v-if="gradeModal.error" class="err-banner">{{ gradeModal.error }}</p>
            <p v-if="gradeModal.loading" class="muted">Loading grades…</p>
            <div v-else-if="gradeModal.subjects?.length" class="grade-grid"
                 :style="{ columnCount: gradeColumnCount(gradeModal.subjects.length) }">
              <div v-for="row in gradeModal.subjects" :key="row.subjectId" class="grade-row">
                <span class="gr-code mono">{{ row.code }}</span>
                <span class="gr-name">{{ row.name }}</span>
                <span class="gr-ects">{{ row.ects }} ects</span>
                <strong :class="['grade-score', scoreClass(row.score)]">{{ row.score ?? '—' }}</strong>
              </div>
            </div>
            <p v-else class="muted">No grades submitted for this enrolment.</p>

            <div v-if="gradeModal.mode === 'reject'" class="reject-block">
              <label class="manage-label">Rejection reason (required, min 10 characters)</label>
              <textarea v-model="gradeModal.rejectReason" rows="3" placeholder="Tell the partner what to fix…"></textarea>
              <div class="reject-meta">
                <span :class="{ ok: (gradeModal.rejectReason || '').trim().length >= 10 }">
                  {{ (gradeModal.rejectReason || '').trim().length }} chars
                </span>
              </div>
            </div>

            <div class="manage-footer">
              <button v-if="gradeModal.mode !== 'reject'" class="btn-link" @click="gradeModal = null">Cancel</button>
              <button v-else class="btn-link" @click="gradeModal.mode = 'view'">← Back</button>

              <div class="grade-actions" v-if="gradeModal.mode !== 'reject'">
                <button class="btn-confirm-manage btn-reject-final"
                        :disabled="!gradeModal.subjects?.length || gradeModal.submitting"
                        @click="gradeModal.mode = 'reject'">
                  ✕ Reject
                </button>
                <button class="btn-confirm-manage btn-approve-final"
                        :disabled="!gradeModal.subjects?.length || gradeModal.submitting"
                        @click="confirmGradeApproval">
                  {{ gradeModal.submitting ? 'Approving…' : '✓ Approve' }}
                </button>
              </div>
              <button v-else class="btn-confirm-manage btn-reject-final"
                      :disabled="(gradeModal.rejectReason || '').trim().length < 10 || gradeModal.submitting"
                      @click="confirmGradeRejection">
                {{ gradeModal.submitting ? 'Rejecting…' : '✕ Reject & Send Back' }}
              </button>
            </div>
          </div>
        </div>
      </div>
    </transition>

    <AdminReviewWizard v-if="reviewingStudent" :student="reviewingStudent"
      @close="closeReview" @submitted="onReviewSubmitted" />

    <transition name="fade">
      <div v-if="reviewToast" class="review-toast">{{ reviewToast }}</div>
    </transition>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch, reactive } from 'vue'
import api from '../../api/client.js'
import AdminReviewWizard from './AdminReviewWizard.vue'

const props = defineProps({
  partnerId: { type: String, default: '' },
})

const STATUS_FILTERS = [
  { id: 'pending-admission',         label: 'Pending Admission Approval',     codes: ['ApplicationAwaitingReviewByAdmission'] },   // default — what admin needs to act on
  { id: 'awaiting-grades-approval',  label: 'Grades — Awaiting Approval',     codes: ['AwaitingGradesApproval'] },
  { id: '',                          label: 'All',                            codes: null },
  { id: 'submitted',                 label: 'Submitted',                      codes: ['ApplicationSubmitted', 'ApplicationAwaitingReviewByPartner'] },
  { id: 'rejected-awaiting-student', label: 'Rejected — Awaiting Student',    codes: ['ApplicationRejectedByPartner', 'ApplicationRejectedByAdmission'] },
  { id: 'applying',                  label: 'Applying (draft)',               codes: ['Draft'], includeNoEnrolment: true },
  { id: 'active',                    label: 'Active',                         codes: ['AcceptOffer', 'ApplicationApprovedAdmission', 'AcceptAdmission', 'AwaitingGradesSubmit'] },
]

const list = ref([])
const loading = ref(false)
const loadError = ref('')

const search = ref('')
const filterStatusId = ref('pending-admission')
const filterProgrammeId = ref('')
const filterSpecializationId = ref('')

const reviewingStudent = ref(null)
const reviewToast = ref('')
const languages = ref([])
const nationalities = ref([])

// Admin can review only when the enrolment is in the Admission queue
// (ApplicationAwaitingReviewByAdmission). Disabled otherwise so admin can't
// approve a previously-rejected doc directly.
function canAdminReview(e) {
  return e.statusCode === 'ApplicationAwaitingReviewByAdmission'
}

function statusClass(code) {
  switch (code) {
    case 'ApplicationSubmitted':
    case 'ApplicationAwaitingReviewByPartner':
      return 'st-submitted'
    case 'ApplicationAwaitingReviewByAdmission':
      return 'st-pending'
    case 'ApplicationRejectedByPartner':
    case 'ApplicationRejectedByAdmission':
      return 'st-rejected'
    case 'Draft':
      return 'st-draft'
    case 'AwaitingGradesApproval':
      return 'st-grades'
    case 'AcceptOffer':
    case 'ApplicationApprovedAdmission':
    case 'AcceptAdmission':
    case 'AwaitingGradesSubmit':
    case 'GradesApproved':
      return 'st-active'
    default:
      return ''
  }
}

// Grade approval modal — admin opens it from the row, sees the partner's
// submitted scores, then either approves (→ GradesApproved) or rejects
// with a reason (→ AwaitingGradesSubmit, partner sees the reason).
const gradeModal = ref(null)
async function openGradeReview(s, e) {
  gradeModal.value = reactive({
    studentId: s.studentId,
    enrollmentId: e.studentEnrollmentId,
    studentName: `${s.firstName ?? ''} ${s.lastName ?? ''}`.trim(),
    programmeCode: e.programmeCode,
    specializationName: e.specializationName,
    subjects: [],
    mode: 'view',          // 'view' | 'reject'
    rejectReason: '',
    loading: true,
    submitting: false,
    error: '',
  })
  try {
    const res = await api.get(`/v1/admin/students/${s.studentId}/enrollments/${e.studentEnrollmentId}/subjects`)
    gradeModal.value.subjects = res.data.items ?? []
  } catch (err) {
    gradeModal.value.error = err.response?.data?.error ?? err.message ?? 'Failed to load grades'
  } finally {
    gradeModal.value.loading = false
  }
}
async function confirmGradeApproval() {
  const m = gradeModal.value
  if (!m || m.submitting || !m.subjects?.length) return
  m.submitting = true
  m.error = ''
  try {
    await api.post(`/v1/admin/students/${m.studentId}/enrollments/${m.enrollmentId}/approve-grades`)
    reviewToast.value = 'Grades approved.'
    setTimeout(() => { reviewToast.value = '' }, 3000)
    gradeModal.value = null
    await load()
  } catch (err) {
    m.error = err.response?.data?.error ?? err.message ?? 'Failed to approve grades'
  } finally {
    if (gradeModal.value) gradeModal.value.submitting = false
  }
}
async function confirmGradeRejection() {
  const m = gradeModal.value
  if (!m || m.submitting) return
  const reason = (m.rejectReason || '').trim()
  if (reason.length < 10) return
  m.submitting = true
  m.error = ''
  try {
    await api.post(
      `/v1/admin/students/${m.studentId}/enrollments/${m.enrollmentId}/reject-grades`,
      { reason })
    reviewToast.value = 'Grades sent back to the partner.'
    setTimeout(() => { reviewToast.value = '' }, 3000)
    gradeModal.value = null
    await load()
  } catch (err) {
    m.error = err.response?.data?.error ?? err.message ?? 'Failed to reject grades'
  } finally {
    if (gradeModal.value) gradeModal.value.submitting = false
  }
}
function scoreClass(score) {
  if (score == null) return 'sc-none'
  if (score >= 80) return 'sc-good'
  if (score >= 50) return 'sc-mid'
  return 'sc-bad'
}
function gradeColumnCount(count) {
  if (!count || count <= 10) return 1
  return Math.min(3, Math.ceil(count / 10))
}
function countFor(id) {
  if (id === '') return list.value.length
  const f = STATUS_FILTERS.find(x => x.id === id)
  if (!f) return 0
  let n = 0
  for (const s of list.value) {
    if (f.includeNoEnrolment && s.enrollments.length === 0) { n++; continue }
    if (s.enrollments.some(e => f.codes?.includes(e.statusCode))) n++
  }
  return n
}

const programmesAvailable = computed(() => {
  const m = new Map()
  for (const s of list.value)
    for (const e of s.enrollments)
      if (!m.has(e.programmeId)) m.set(e.programmeId, { programmeId: e.programmeId, name: e.programmeName })
  return [...m.values()].sort((a, b) => a.name.localeCompare(b.name))
})
const specializationsAvailable = computed(() => {
  const m = new Map()
  for (const s of list.value)
    for (const e of s.enrollments)
      if (!m.has(e.specializationId)) m.set(e.specializationId, { specializationId: e.specializationId, name: e.specializationName })
  return [...m.values()].sort((a, b) => a.name.localeCompare(b.name))
})
const filtered = computed(() => {
  const q = search.value.trim().toLowerCase()
  return list.value.filter(s => {
    if (q) {
      const hay = `${s.firstName ?? ''} ${s.lastName ?? ''} ${s.username ?? ''} ${s.studentNumber}`.toLowerCase()
      if (!hay.includes(q)) return false
    }
    if (filterProgrammeId.value && !s.enrollments.some(e => e.programmeId === filterProgrammeId.value)) return false
    if (filterSpecializationId.value && !s.enrollments.some(e => e.specializationId === filterSpecializationId.value)) return false
    if (filterStatusId.value !== '') {
      const f = STATUS_FILTERS.find(x => x.id === filterStatusId.value)
      const matchesNoEnrolment = f?.includeNoEnrolment && s.enrollments.length === 0
      const matchesCode = s.enrollments.some(e => f?.codes?.includes(e.statusCode))
      if (!matchesNoEnrolment && !matchesCode) return false
    }
    return true
  })
})

async function load() {
  loading.value = true; loadError.value = ''
  try {
    const params = {}
    if (props.partnerId) params.partnerId = props.partnerId
    if (filterProgrammeId.value) params.programmeId = filterProgrammeId.value
    if (filterSpecializationId.value) params.specializationId = filterSpecializationId.value
    const res = await api.get('/v1/admin/students', { params })
    list.value = res.data.items ?? []
  } catch (e) {
    loadError.value = e.response?.data?.error ?? e.message ?? 'Failed to load'
  } finally { loading.value = false }
}

const PROFICIENCIES = [
  { id: 1, label: 'Beginner' },
  { id: 2, label: 'Intermediate' },
  { id: 3, label: 'Fluent' },
  { id: 4, label: 'Native' },
]

function findDocBy(d, typeRegex) {
  if (!d?.documents) return null
  return d.documents.find(doc => typeRegex.test(doc.documentTypeName ?? '')) ?? null
}
function findDoc(d, typeRegex)   { return findDocBy(d, typeRegex)?.fileName ?? null }
function findDocId(d, typeRegex) { return findDocBy(d, typeRegex)?.studentDocumentId ?? null }
function withSlotFallback(d, typeRegex, slot) {
  const m = findDocBy(d, typeRegex)
  const meta = m
    ? {
        status: m.status ?? null, statusName: m.statusName ?? null,
        lastChangedByName: m.lastChangedByName ?? null,
        lastChangeReason: m.lastChangeReason ?? null,
        requirements: m.requirements ?? [],
      }
    : { status: null, statusName: null, lastChangedByName: null, lastChangeReason: null, requirements: [] }
  if ((meta.requirements ?? []).length === 0) {
    meta.requirements = d?.slotRequirements?.[slot] ?? []
  }
  return meta
}

/// Mirrors PartnerStudentsTab.adaptForWizard so the shared StudentReview-style
/// wizard reads the same shape regardless of which side opened it.
function adaptForWizard(d, targetEnrollmentId = null) {
  if (!d) return null
  // Scope docs to the targeted enrolment — see partner equivalent for why.
  if (targetEnrollmentId) {
    d = { ...d, documents: (d.documents || []).filter(doc => doc.enrollmentId === targetEnrollmentId) }
  }
  const addr = d.personal?.address ?? {}
  const addressStr = [addr.line1, addr.line2, addr.city, addr.stateRegion, addr.postalCode]
    .filter(Boolean).join(', ')
  const langSummary = (d.background?.languages || [])
    .map(l => {
      const name = languages.value.find(x => x.languageId === l.languageId)?.name ?? ''
      const prof = PROFICIENCIES.find(p => p.id === l.proficiency)?.label ?? ''
      return [name, prof].filter(Boolean).join(' — ')
    })
    .filter(Boolean).join(' · ')

  return reactive({
    studentId: d.studentNumber,
    studentGuid: d.studentId,
    firstName: d.account?.firstName ?? '',
    lastName: d.account?.lastName ?? '',
    email: d.account?.email ?? '',
    dateOfBirth: d.personal?.dateOfBirth?.slice(0, 10) ?? '',
    passportId: d.personal?.passportId ?? '',
    address: addressStr,
    highestDegree: d.background?.highestDegree ?? '',
    languageResult: langSummary,
    yearsWorkExperience: d.background?.yearsWorkExperience ?? 0,
    docPassport: findDoc(d, /passport|identity|id\b/i),
    docDegree:   findDoc(d, /degree|diploma|transcript|high school/i),
    docLanguage: findDoc(d, /language|ielts|toefl|english/i),
    docCV:       findDoc(d, /curriculum|\bcv\b|r[eé]sum[eé]/i),
    docIds: {
      passport: findDocId(d, /passport|identity|id\b/i),
      degree:   findDocId(d, /degree|diploma|transcript|high school/i),
      language: findDocId(d, /language|ielts|toefl|english/i),
      cv:       findDocId(d, /curriculum|\bcv\b|r[eé]sum[eé]/i),
    },
    docMeta: {
      passport: withSlotFallback(d, /passport|identity|id\b/i, 'passport'),
      degree:   withSlotFallback(d, /degree|diploma|transcript|high school/i, 'degree'),
      language: withSlotFallback(d, /language|ielts|toefl|english/i, 'language'),
      cv:       withSlotFallback(d, /curriculum|\bcv\b|r[eé]sum[eé]/i, 'cv'),
    },
    partnerReview: {
      passport:  { status: 'pending', reason: '' },
      degree:    { status: 'pending', reason: '' },
      language:  { status: 'pending', reason: '' },
      cv:        { status: 'pending', reason: '' },
      programme: { status: 'pending', reason: '' },
      completedAt: null, partnerName: '',
    },
    enrollments: (d.enrollments || [])
      .filter(e => !targetEnrollmentId || e.studentEnrollmentId === targetEnrollmentId)
      .map(e => ({
        id: e.studentEnrollmentId,
        programme: e.programmeName,
        specialization: e.specializationName,
        modeOfStudy: e.modeOfStudyName,
        selectedPathway: e.pathwayName ?? null,
        commencementDate: e.commencementDate?.slice(0, 10) ?? '',
        durationMonths: e.durationOfStudyMonths ?? null,
        tuitionFeeUsd: Number(e.tuitionFeeUsd ?? 0),
        paymentPlan: null,
      })),
  })
}

async function openReview(studentId, enrollmentId = null) {
  loadError.value = ''
  try {
    if (!languages.value.length || !nationalities.value.length) {
      const [langs, nats] = await Promise.all([
        api.get('/v1/public/languages'),
        api.get('/v1/public/nationalities'),
      ])
      languages.value = langs.data.items ?? []
      nationalities.value = nats.data.items ?? []
    }
    const res = await api.get(`/v1/admin/students/${studentId}`)
    reviewingStudent.value = adaptForWizard(res.data, enrollmentId)
  } catch (e) {
    loadError.value = e.response?.data?.error ?? e.message ?? 'Failed to load student'
  }
}
function closeReview() { reviewingStudent.value = null }
async function onReviewSubmitted(s) {
  reviewToast.value = `Review submitted for ${s.firstName} ${s.lastName}`
  setTimeout(() => { reviewToast.value = '' }, 3200)
  await load()
  load()
}

// filterStatusId is a client-side filter — no refetch needed.
watch([filterProgrammeId, filterSpecializationId, () => props.partnerId], load)
onMounted(load)
</script>

<style scoped>
.ps-tab { padding: .25rem 0; }
.err-banner { background: #fef2f2; border: 1px solid #fca5a5; color: #b91c1c; padding: .5rem .8rem; border-radius: 6px; font-size: .85rem; margin-bottom: .65rem; }

.status-row { display: flex; gap: .4rem; margin-bottom: .65rem; flex-wrap: wrap; }
.status-chip { background: #eef2f7; border: 0; color: #5f6e85; padding: .35rem .8rem; border-radius: 18px; font-size: .82rem; cursor: pointer; }
.status-chip.active { background: #003366; color: #fff; }
.chip-count { background: rgba(255,255,255,0.25); margin-left: .35rem; padding: 0 .45rem; border-radius: 12px; font-size: .72rem; }
.status-chip:not(.active) .chip-count { background: rgba(0,0,0,0.06); color: #5f6e85; }

.filter-row { display: flex; align-items: center; gap: .65rem; margin-bottom: .65rem; flex-wrap: wrap; }
.inp { padding: .4rem .65rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .88rem; background: #fff; }
.btn-refresh { background: #fff; border: 1px solid #cfd7e3; padding: .35rem .75rem; border-radius: 5px; cursor: pointer; }

.empty { padding: 1rem; background: #f6f9fd; color: #5f6e85; border-radius: 8px; text-align: center; }
.data-table { width: 100%; border-collapse: collapse; }
.data-table th { text-align: left; font-size: .72rem; color: #5f6e85; text-transform: uppercase; letter-spacing: .04em; padding: .5rem .7rem; border-bottom: 1px solid #e5eaf1; }
.data-row { cursor: pointer; }
.data-row td { padding: .55rem .7rem; border-bottom: 1px solid #eef2f7; font-size: .88rem; vertical-align: top; }
.data-row:hover td { background: #f7f9fb; }
.mono { font-family: monospace; font-size: .82rem; color: #0a264f; }
.muted { color: #888; font-size: .82rem; }
.btn-link { background: none; border: 0; color: #0055a5; cursor: pointer; font-size: .85rem; }
.btn-review-sm { margin-left: .4rem; background: #003366; color: #fff; border: none; border-radius: 4px; padding: 1px 8px; font-size: .72rem; font-weight: 600; cursor: pointer; }
.btn-review-sm:hover:not(:disabled) { background: #0055a5; }
.btn-review-sm:disabled { background: #c0c8d2; cursor: not-allowed; opacity: 0.7; }
.btn-grades-approve { background: #16a34a; }
.btn-grades-approve:hover:not(:disabled) { background: #15803d; }
.st-grades { background: #ede9fe; color: #5b21b6; }

.enrol-line { font-size: .85rem; }
.enr-prog { background: #e8f0f8; color: #003366; border-radius: 4px; padding: 1px 6px; font-size: .75rem; font-weight: 700; margin: 0 .3rem; }
.s-badge { font-size: .7rem; padding: 1px 6px; border-radius: 10px; margin-left: .3rem; font-weight: 600; }
.st-submitted { background: #fff7e0; color: #8a6d00; }
.st-pending   { background: #e8f0f8; color: #0055a5; }
.st-rejected  { background: #fee2e2; color: #991b1b; }
.st-draft     { background: #eef2f7; color: #5f6e85; }
.st-active    { background: #d1fae5; color: #065f46; }
.s-badge.unverified { background: #fef2f2; color: #991b1b; }

.review-toast { position: fixed; bottom: 1.2rem; right: 1.2rem; background: #003366; color: #fff; padding: .6rem 1.1rem; border-radius: 8px; font-size: .85rem; z-index: 500; box-shadow: 0 4px 14px rgba(0,0,0,.25); }
.fade-enter-active, .fade-leave-active { transition: opacity 0.18s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }

/* Grade approval modal */
.manage-overlay { position: fixed; inset: 0; background: rgba(0,0,0,.45); z-index: 70; display: flex; align-items: center; justify-content: center; }
.manage-modal { background: #fff; border-radius: 10px; width: 640px; max-width: 95vw; box-shadow: 0 12px 40px rgba(0,0,0,.25); overflow: hidden; }
.grade-modal { width: 1080px; }
.manage-hdr { display: flex; justify-content: space-between; align-items: center; padding: 1rem 1.25rem; border-bottom: 1.5px solid #e8edf4; }
.manage-hdr h3 { margin: 0; color: #003366; font-size: 1rem; }
.manage-sub { color: #888; font-size: .82rem; margin: 0; padding: .5rem 1.25rem .25rem; }
.manage-body { padding: .75rem 1.25rem 1.25rem; }
.muted { color: #888; font-size: .85rem; }
.drawer-close { background: none; border: 0; font-size: 1.2rem; cursor: pointer; color: #888; }
.err-banner { background: #fef2f2; border: 1px solid #fca5a5; color: #b91c1c; padding: .5rem .8rem; border-radius: 6px; font-size: .85rem; margin-bottom: .65rem; }
.manage-footer { display: flex; justify-content: space-between; align-items: center; margin-top: .85rem; }
.btn-link { background: none; border: 0; color: #0055a5; cursor: pointer; font-size: .85rem; padding: 0; }
.btn-confirm-manage { background: #0d6b55; color: #fff; border: none; border-radius: 6px; padding: .55rem 1.2rem; font-size: .88rem; font-weight: 600; cursor: pointer; }
.btn-confirm-manage:hover:not(:disabled) { background: #0a5a47; }
.btn-confirm-manage:disabled { opacity: .45; cursor: default; }
.btn-approve-final { background: #16a34a; }
.btn-approve-final:hover:not(:disabled) { background: #15803d; }
.btn-reject-final { background: #b91c1c; }
.btn-reject-final:hover:not(:disabled) { background: #991b1b; }
.grade-actions { display: flex; gap: .5rem; }

.reject-block { background: #fff7f7; border: 1px solid #fbcaca; border-left: 3px solid #b91c1c; border-radius: 6px; padding: .7rem .85rem; margin-top: .85rem; }
.manage-label { display: block; font-size: .8rem; font-weight: 600; color: #7f1d1d; margin-bottom: .35rem; }
.reject-block textarea { width: 100%; padding: .55rem .7rem; border: 1.5px solid #fbcaca; border-radius: 6px; font-size: .88rem; font-family: inherit; resize: vertical; background: #fff; }
.reject-block textarea:focus { outline: none; border-color: #b91c1c; }
.reject-meta { text-align: right; font-size: .72rem; color: #b91c1c; margin-top: .25rem; }
.reject-meta .ok { color: #065f46; }

.grade-grid { column-gap: 1.2rem; margin-top: .5rem; }
.grade-row {
  break-inside: avoid;
  display: grid; grid-template-columns: 80px 1fr auto auto; gap: .5rem;
  align-items: center; padding: .35rem .25rem;
  border-bottom: 1px solid #eef2f7; font-size: .82rem;
}
.gr-code { font-family: ui-monospace, monospace; font-size: .76rem; color: #003366; }
.gr-name { color: #222; line-height: 1.3; min-width: 0; word-break: break-word; }
.gr-ects { color: #888; font-size: .72rem; white-space: nowrap; }
.grade-score { display: inline-block; min-width: 44px; padding: 2px 9px; border-radius: 12px; font-weight: 700; font-size: .8rem; text-align: center; }
.sc-good { background: #d1fae5; color: #065f46; }
.sc-mid  { background: #fff3cd; color: #856404; }
.sc-bad  { background: #fee2e2; color: #991b1b; }
.sc-none { background: #f0f3f7; color: #888; }
</style>
