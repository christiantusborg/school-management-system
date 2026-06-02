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
      <input v-model="search" class="inp" placeholder="Fuzzy search — name, email, programme, partner…" />
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
        <tr v-for="s in filtered" :key="s.studentId" class="data-row" @click="openStudentDetail(s)">
          <td class="mono">{{ s.studentNumber }}</td>
          <td>
            <a class="s-name-link" @click.stop="openStudentDetail(s)">
              {{ s.firstName ?? '—' }} {{ s.lastName ?? '' }}
            </a>
            <br><small class="muted">@{{ s.username }}</small>
          </td>
          <td v-if="!partnerId">{{ s.partnerName }}</td>
          <td>{{ s.email ?? '—' }}<span v-if="!s.emailVerified" class="s-badge unverified">unverified</span></td>
          <td>
            <div v-for="e in s.enrollments" :key="e.studentEnrollmentId" class="enrol-line">
              <span class="enr-prog">{{ e.programmeCode }}</span> · {{ e.specializationName }}
              <span :class="['s-badge', statusClass(e.statusCode)]">{{ e.statusName }}</span>
              <button v-if="e.statusCode === 'AwaitingGradesApproval'" class="btn-review-sm btn-grades-approve"
                      @click.stop="openGradeReview(s, e)">
                Approve grades
              </button>
              <button v-else-if="e.statusCode === 'AwaitingGradesSubmit'" class="btn-review-sm btn-grades-submit"
                      @click.stop="openGradeSubmit(s, e)">
                Submit grades
              </button>
              <button v-else class="btn-review-sm"
                      :disabled="!canAdminReview(e)"
                      :title="canAdminReview(e) ? '' : 'Not in the Admission queue.'"
                      @click.stop="canAdminReview(e) && openReview(s.studentId, e.studentEnrollmentId)">Review</button>
              <button class="btn-row-details btn-row-details-sm" @click.stop="openStudentDetail(s, e.studentEnrollmentId)">
                Details
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
            <h3>{{ gradeModal.mode === 'submit' ? 'Submit grades' : 'Approve grades' }}</h3>
            <button class="drawer-close" @click="gradeModal = null">✕</button>
          </div>
          <p class="manage-sub">{{ gradeModal.studentName }} · {{ gradeModal.programmeCode }} · {{ gradeModal.specializationName }}</p>
          <div class="manage-body">
            <p v-if="gradeModal.error" class="err-banner">{{ gradeModal.error }}</p>
            <p v-if="gradeModal.loading" class="muted">Loading grades…</p>
            <p v-else-if="gradeModal.mode === 'submit'" class="muted manage-hint">
              Enter a score (0–100) for each subject. On submit, the enrolment moves to Awaiting grades approval.
            </p>
            <div v-if="!gradeModal.loading && gradeModal.subjects?.length" class="grade-grid"
                 :style="{ columnCount: gradeColumnCount(gradeModal.subjects.length) }">
              <div v-for="row in gradeModal.subjects" :key="row.subjectId" class="grade-row">
                <span class="gr-code mono">{{ row.code }}</span>
                <span class="gr-name">{{ row.name }}</span>
                <span class="gr-ects">{{ row.ects }} ects</span>
                <input v-if="gradeModal.mode === 'submit'" type="number" min="0" max="100"
                       v-model.number="row.score" class="grade-input gr-input" />
                <strong v-else :class="['grade-score', scoreClass(row.score)]">{{ row.score ?? '—' }}</strong>
              </div>
            </div>
            <p v-else-if="!gradeModal.loading" class="muted">No grades submitted for this enrolment.</p>

            <EnrollmentActivityLog v-if="gradeModal.studentId && gradeModal.enrollmentId"
              :api-path="`/v1/admin/students/${gradeModal.studentId}/enrollments/${gradeModal.enrollmentId}/activity`" />

            <!-- Approve-side wizard: confirm preconditions before the approve
                 button enables. Right now there's just the tuition check,
                 but the block is there so we can drop in more pre-flight
                 confirmations (e.g., academic integrity, attendance) later. -->
            <div v-if="gradeModal.mode === 'view' && gradeModal.subjects?.length" class="approve-checks">
              <div class="approve-checks-title">Before approving — confirm:</div>
              <label class="approve-check">
                <input type="checkbox" v-model="gradeModal.confirmTuitionPaid" />
                <span>The student's tuition is fully paid (no outstanding balance).</span>
              </label>
            </div>

            <div v-if="gradeModal.mode === 'reject'" class="reject-block">
              <label class="manage-label">Quick reasons</label>
              <select class="reject-preset" v-model="gradeModal.rejectPreset" @change="onRejectPresetChange">
                <option value="">— Pick a reason or write your own —</option>
                <option v-for="p in REJECT_PRESETS" :key="p.id" :value="p.id">{{ p.label }}</option>
              </select>
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

              <button v-if="gradeModal.mode === 'submit'" class="btn-confirm-manage btn-approve-final"
                      :disabled="!canCommitAdminGrades || gradeModal.submitting"
                      @click="confirmGradeSubmission">
                {{ gradeModal.submitting ? 'Submitting…' : '✓ Submit grades' }}
              </button>
              <div v-else-if="gradeModal.mode !== 'reject'" class="grade-actions">
                <button class="btn-confirm-manage btn-reject-final"
                        :disabled="!gradeModal.subjects?.length || gradeModal.submitting"
                        @click="gradeModal.mode = 'reject'">
                  ✕ Reject
                </button>
                <button class="btn-confirm-manage btn-approve-final"
                        :disabled="!gradeModal.subjects?.length || !gradeModal.confirmTuitionPaid || gradeModal.submitting"
                        :title="!gradeModal.confirmTuitionPaid ? 'Tick the tuition-paid checkbox first.' : ''"
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

    <!-- Student detail modal: 3 tabs (Details / Letters / Activity) -->
    <transition name="fade">
      <div v-if="detailModal" class="manage-overlay" @click.self="detailModal = null">
        <div class="manage-modal detail-modal">
          <div class="manage-hdr">
            <div>
              <h3>{{ detailModal.name || '—' }}
                <span class="muted-sub">· {{ detailModal.studentNumber }}</span>
              </h3>
              <p class="manage-sub">
                {{ detailModal.email || '—' }}
                <span v-if="detailModal.partnerName"> · {{ detailModal.partnerName }}</span>
                <template v-if="detailModal.data && !detailModal.data.account?.emailVerified">
                  <span class="s-badge unverified">unverified</span>
                  <button class="btn-row-details btn-row-details-sm"
                          :disabled="confirmingEmail"
                          @click="confirmEmailOnBehalf">
                    {{ confirmingEmail ? 'Confirming…' : '✉ Confirm email on behalf' }}
                  </button>
                </template>
              </p>
              <p v-for="e in awaitingOfferAcceptance" :key="e.studentEnrollmentId" class="manage-sub">
                <button class="btn-row-details btn-row-details-sm"
                        :disabled="acceptingOfferId === e.studentEnrollmentId"
                        @click="acceptOfferOnBehalf(e)">
                  {{ acceptingOfferId === e.studentEnrollmentId
                      ? 'Accepting…'
                      : `✓ Accept ${e.programmeCode || 'offer'} on behalf of student` }}
                </button>
              </p>
            </div>
            <button class="drawer-close" @click="detailModal = null">✕</button>
          </div>

          <p v-if="detailModal.error" class="err-banner">{{ detailModal.error }}</p>
          <p v-if="detailModal.loading" class="muted detail-loading">Loading…</p>

          <template v-else-if="detailModal.data">
            <!-- Enrollment selector when there are multiple -->
            <div v-if="detailEnrollments.length > 1" class="enr-switch">
              <label>Enrolment:</label>
              <select v-model="detailModal.activeEnrollmentId">
                <option v-for="e in detailEnrollments" :key="e.studentEnrollmentId" :value="e.studentEnrollmentId">
                  {{ e.programmeCode }} · {{ e.specializationName }} ({{ e.statusName }})
                </option>
              </select>
            </div>

            <div class="detail-tabs">
              <button v-for="t in DETAIL_TABS" :key="t.id"
                      :class="['tab-btn', { active: detailModal.activeTab === t.id }]"
                      @click="detailModal.activeTab = t.id">{{ t.label }}</button>
            </div>

            <!-- Details tab -->
            <div v-if="detailModal.activeTab === 'details'" class="tab-pane">
              <div class="detail-grid">
                <div class="detail-section">
                  <h4>Account</h4>
                  <dl>
                    <dt>Username</dt><dd>@{{ detailModal.data.account?.username }}</dd>
                    <dt>Email</dt><dd>{{ detailModal.data.account?.email ?? '—' }}<span v-if="!detailModal.data.account?.emailVerified" class="s-badge unverified">unverified</span></dd>
                    <dt>First name</dt><dd>{{ detailModal.data.account?.firstName ?? '—' }}</dd>
                    <dt>Last name</dt><dd>{{ detailModal.data.account?.lastName ?? '—' }}</dd>
                  </dl>
                  <div class="reset-pw-row">
                    <button class="btn-row-details" :disabled="resettingStudentPw" @click="resetStudentPassword">
                      {{ resettingStudentPw ? 'Resetting…' : '🔑 Reset student password' }}
                    </button>
                    <div v-if="resetStudentPwValue" class="reset-pw-reveal">
                      <strong>New password:</strong> <code>{{ resetStudentPwValue }}</code>
                      <button class="btn-row-details" @click="copyResetStudentPw">Copy</button>
                      <div class="reset-pw-hint">Save this — it won't be shown again.</div>
                    </div>
                  </div>
                </div>
                <div class="detail-section">
                  <h4>Personal</h4>
                  <p v-if="personalSaveError" class="err-banner">{{ personalSaveError }}</p>
                  <p v-if="personalSaveOk" class="ok-banner">{{ personalSaveOk }}</p>
                  <div class="edit-grid">
                    <label class="edit-field">
                      <span>First name</span>
                      <input v-model="detailModal.data.account.firstName" />
                    </label>
                    <label class="edit-field">
                      <span>Last name</span>
                      <input v-model="detailModal.data.account.lastName" />
                    </label>
                    <label class="edit-field">
                      <span>Date of birth</span>
                      <input type="date" v-model="personalDobInput" />
                    </label>
                    <label class="edit-field">
                      <span>Passport / ID</span>
                      <input v-model="detailModal.data.personal.passportId" />
                    </label>
                    <label class="edit-field edit-field-wide">
                      <span>Nationality</span>
                      <select v-model.number="detailModal.data.personal.nationalityId">
                        <option :value="null">—</option>
                        <option v-for="n in nationalities" :key="n.nationalityId" :value="n.nationalityId">{{ n.name }}</option>
                      </select>
                    </label>
                    <label class="edit-field edit-field-wide">
                      <span>Address line 1</span>
                      <input v-model="detailModal.data.personal.address.line1" />
                    </label>
                    <label class="edit-field">
                      <span>City</span>
                      <input v-model="detailModal.data.personal.address.city" />
                    </label>
                    <label class="edit-field">
                      <span>State / Region</span>
                      <input v-model="detailModal.data.personal.address.stateRegion" />
                    </label>
                    <label class="edit-field">
                      <span>Postal code</span>
                      <input v-model="detailModal.data.personal.address.postalCode" />
                    </label>
                    <label class="edit-field">
                      <span>Country</span>
                      <select v-model="detailModal.data.personal.address.countryCode">
                        <option value="">—</option>
                        <option v-for="n in nationalities" :key="n.code" :value="n.code">{{ n.name }}</option>
                      </select>
                    </label>
                  </div>
                  <button class="btn-row-details btn-save-admin" :disabled="savingPersonal" @click="saveAdminPersonal">
                    {{ savingPersonal ? 'Saving…' : 'Save personal' }}
                  </button>
                </div>
                <div class="detail-section">
                  <h4>Background</h4>
                  <p v-if="backgroundSaveError" class="err-banner">{{ backgroundSaveError }}</p>
                  <p v-if="backgroundSaveOk" class="ok-banner">{{ backgroundSaveOk }}</p>
                  <div class="edit-grid">
                    <label class="edit-field edit-field-wide">
                      <span>Highest degree</span>
                      <input v-model="detailModal.data.background.highestDegree" />
                    </label>
                    <label class="edit-field">
                      <span>Years of experience</span>
                      <input type="number" min="0" v-model.number="detailModal.data.background.yearsWorkExperience" />
                    </label>
                  </div>
                  <div class="lang-block">
                    <div class="lang-head">
                      <span>Languages</span>
                      <button class="btn-mini" @click="addAdminLanguage">+ Add language</button>
                    </div>
                    <div v-for="(l, idx) in (detailModal.data.background.languages || [])" :key="idx" class="lang-row">
                      <select v-model.number="l.languageId">
                        <option :value="0">— Pick language —</option>
                        <option v-for="lg in languages" :key="lg.languageId" :value="lg.languageId">{{ lg.name }}</option>
                      </select>
                      <select v-model.number="l.proficiency">
                        <option v-for="p in PROFICIENCIES" :key="p.id" :value="p.id">{{ p.label }}</option>
                      </select>
                      <button class="btn-mini btn-remove" @click="removeAdminLanguage(idx)">✕</button>
                    </div>
                  </div>
                  <button class="btn-row-details btn-save-admin" :disabled="savingBackground" @click="saveAdminBackground">
                    {{ savingBackground ? 'Saving…' : 'Save background' }}
                  </button>
                </div>
                <div class="detail-section" v-if="activeEnrollment">
                  <h4>Enrolment</h4>
                  <dl>
                    <dt>Programme</dt><dd>{{ activeEnrollment.programmeName }}</dd>
                    <dt>Specialisation</dt><dd>{{ activeEnrollment.specializationName }}</dd>
                    <dt>Mode</dt><dd>{{ activeEnrollment.modeOfStudyName ?? '—' }}</dd>
                    <dt>Commencement</dt><dd>{{ formatDate(activeEnrollment.commencementDate) || '—' }}</dd>
                    <dt>Duration</dt><dd>{{ activeEnrollment.durationOfStudyMonths ?? '—' }} months</dd>
                    <dt>Status</dt><dd><span :class="['s-badge', statusClass(activeEnrollment.statusCode)]">{{ activeEnrollment.statusName }}</span></dd>
                  </dl>
                </div>
              </div>
            </div>

            <!-- Documents tab -->
            <div v-if="detailModal.activeTab === 'documents'" class="tab-pane">
              <div v-for="enr in docsByEnrollment" :key="enr.enrollmentId" class="docs-group">
                <div class="docs-group-head">
                  <strong>{{ enr.programmeCode }}</strong> · {{ enr.specializationName }}
                  <span class="docs-group-count">{{ enr.coreDocs.length + enr.additionalDocs.length }}</span>
                  <button class="btn-mini" style="margin-left:auto"
                          @click="openAdditionalDialog(enr.enrollmentId)">
                    + Add additional document
                  </button>
                </div>
                <div class="docs-list" v-if="enr.coreDocs.length">
                  <div v-for="d in enr.coreDocs" :key="d.studentDocumentId" class="doc-row">
                    <span :class="['doc-pill', docPillClass(d.status)]">{{ docPillIcon(d.status) }}</span>
                    <div class="doc-info">
                      <div class="doc-name">{{ d.documentTypeName }}</div>
                      <div class="doc-sub">
                        {{ d.fileName }} · uploaded {{ formatDate(d.uploadedAt) }} · {{ d.statusName }}
                      </div>
                    </div>
                    <button class="btn-mini" @click="downloadStudentDoc(d)">Open</button>
                    <label class="btn-mini" v-if="!d.isVerified" style="margin-left:6px">
                      Replace
                      <input type="file" :accept="ACCEPTED_DOC_ACCEPT_ATTR" hidden
                             @change="onAdminReplace($event, enr.enrollmentId, d)" />
                    </label>
                  </div>
                </div>
                <p v-else class="muted" style="padding:6px 0;">No documents uploaded yet for this application.</p>
                <div v-if="enr.additionalDocs.length" class="docs-list">
                  <div class="docs-subhead">Additional documents</div>
                  <div v-for="d in enr.additionalDocs" :key="d.studentDocumentId" class="doc-row">
                    <span :class="['doc-pill', docPillClass(d.status)]">{{ docPillIcon(d.status) }}</span>
                    <div class="doc-info">
                      <div class="doc-name">
                        {{ d.documentTypeName }}
                        <span class="pill-additional">Additional</span>
                      </div>
                      <div class="doc-sub">
                        {{ d.fileName }} · uploaded {{ formatDate(d.uploadedAt) }} · {{ d.statusName }}
                      </div>
                    </div>
                    <button class="btn-mini" @click="downloadStudentDoc(d)">Open</button>
                  </div>
                </div>
              </div>
              <p v-if="!docsByEnrollment.length" class="muted">No enrolments for this student yet.</p>
            </div>

            <AdditionalDocumentUploadDialog
              v-if="additionalDialog.open"
              types-endpoint="/v1/admin/document-types"
              :upload-endpoint="additionalDialog.uploadEndpoint"
              @close="additionalDialog.open = false"
              @uploaded="onAdditionalUploaded" />

            <!-- Letters tab -->
            <div v-if="detailModal.activeTab === 'letters'" class="tab-pane">
              <p v-if="!activeEnrollment" class="muted">No enrolment selected.</p>
              <div v-else class="letters-list">
                <div v-for="t in LETTER_TYPES" :key="t.key" class="letter-row" :class="{ disabled: !activeEnrollment.letters?.[t.key] }">
                  <span class="letter-icon">{{ t.icon }}</span>
                  <div class="letter-info">
                    <div class="letter-name">{{ t.label }}</div>
                    <div class="letter-sub">
                      <template v-if="activeEnrollment.letters?.[t.key]">
                        {{ activeEnrollment.letters[t.key].fileName }} · released {{ formatDate(activeEnrollment.letters[t.key].uploadedAt) }}
                      </template>
                      <template v-else>Not yet released</template>
                    </div>
                  </div>
                  <button class="btn-mini" :disabled="!activeEnrollment.letters?.[t.key]"
                          @click="downloadLetter(activeEnrollment.letters?.[t.key])">Download</button>
                </div>
              </div>
            </div>

            <!-- Activity tab -->
            <div v-if="detailModal.activeTab === 'activity'" class="tab-pane">
              <p v-if="!activeEnrollment" class="muted">No enrolment selected.</p>
              <EnrollmentActivityLog v-else
                :api-path="`/v1/admin/students/${detailModal.studentId}/enrollments/${activeEnrollment.studentEnrollmentId}/activity`"
                :default-open="true" />
            </div>
          </template>
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
import Fuse from 'fuse.js'
import api from '../../api/client.js'
import AdminReviewWizard from './AdminReviewWizard.vue'
import EnrollmentActivityLog from '../letters/EnrollmentActivityLog.vue'
import AdditionalDocumentUploadDialog from '../letters/AdditionalDocumentUploadDialog.vue'
import { ACCEPTED_DOC_ACCEPT_ATTR } from '../../utils/uploadPolicy.js'

const props = defineProps({
  partnerId: { type: String, default: '' },
})

// One chip per distinct workflow stage so admin can drill into any single
// state. "Action required" is the default landing (admin's queue) and "All"
// is the catch-all at the end. Order: action queue → pre-admission → post-
// admission → post-grading. Counts are derived client-side from the list.
const STATUS_FILTERS = [
  { id: 'action-required',           label: 'Action required',             codes: ['ApplicationAwaitingReviewByAdmission', 'AwaitingGradesApproval'] },
  { id: 'pending-admission',         label: 'Pending Admission Approval',  codes: ['ApplicationAwaitingReviewByAdmission'] },
  { id: 'awaiting-grades-approval',  label: 'Grades — Awaiting Approval',  codes: ['AwaitingGradesApproval'] },
  { id: 'submitted',                 label: 'Submitted',                   codes: ['ApplicationSubmitted', 'ApplicationAwaitingReviewByPartner'] },
  { id: 'rejected-awaiting-student', label: 'Rejected — Awaiting Student', codes: ['ApplicationRejectedByPartner', 'ApplicationRejectedByAdmission'] },
  { id: 'applying',                  label: 'Applying (draft)',            codes: ['Draft'], includeNoEnrolment: true },
  { id: 'awaiting-student-accept',   label: 'Awaiting Student Acceptance', codes: ['AcceptOffer'] },
  { id: 'admitted',                  label: 'Admitted',                    codes: ['ApplicationApprovedAdmission', 'AcceptAdmission'] },
  { id: 'awaiting-grades-submit',    label: 'Awaiting Grades Submit',      codes: ['AwaitingGradesSubmit'] },
  { id: 'graduated',                 label: 'Graduated',                   codes: ['GradesApproved'] },
  { id: '',                          label: 'All',                         codes: null },
]

const list = ref([])
const loading = ref(false)
const loadError = ref('')

const search = ref('')
const filterStatusId = ref('action-required')
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

// Per-student detail modal — opens when the admin clicks any row in the
// list. Three tabs: Details (read-only profile + enrolment), Letters
// (download released PDFs), Activity (chronological log via the shared
// EnrollmentActivityLog component). Multi-enrolment students get a
// dropdown to switch which enrolment the Letters/Activity tabs scope to.
const DETAIL_TABS = [
  { id: 'details',   label: 'Details' },
  { id: 'documents', label: 'Documents' },
  { id: 'letters',   label: 'Letters' },
  { id: 'activity',  label: 'Activity log' },
]
const LETTER_TYPES = [
  { key: 'offerLetter',            label: 'Offer Letter',           icon: '📄' },
  { key: 'admissionLetter',        label: 'Admission Letter',       icon: '📋' },
  { key: 'transcript',             label: 'Transcript',             icon: '📑' },
  { key: 'certificate',            label: 'Certificate',            icon: '🎓' },
  { key: 'provisionalCertificate', label: 'Provisional Certificate', icon: '🎓' },
]
const detailModal = ref(null)
const detailEnrollments = computed(() => detailModal.value?.data?.enrollments ?? [])
const activeEnrollment = computed(() =>
  detailEnrollments.value.find(e => e.studentEnrollmentId === detailModal.value?.activeEnrollmentId)
  ?? detailEnrollments.value[0]
  ?? null
)

const awaitingOfferAcceptance = computed(() =>
  (detailModal.value?.data?.enrollments ?? []).filter(e => e.statusCode === 'AcceptOffer'))

const confirmingEmail = ref(false)
const acceptingOfferId = ref(null)

async function confirmEmailOnBehalf() {
  if (!detailModal.value || confirmingEmail.value) return
  confirmingEmail.value = true
  detailModal.value.error = ''
  try {
    await api.post(`/v1/admin/students/${detailModal.value.studentId}/confirm-email`)
    await refreshDetailModal()
    detailModal.value.email = detailModal.value.data?.account?.email ?? detailModal.value.email
    load()
  } catch (err) {
    detailModal.value.error = err.response?.data?.error ?? err.message ?? 'Failed to confirm email'
  } finally {
    confirmingEmail.value = false
  }
}

async function acceptOfferOnBehalf(enr) {
  if (!detailModal.value || acceptingOfferId.value) return
  acceptingOfferId.value = enr.studentEnrollmentId
  detailModal.value.error = ''
  try {
    await api.post(`/v1/admin/students/${detailModal.value.studentId}/enrollments/${enr.studentEnrollmentId}/accept-offer-on-behalf`)
    await refreshDetailModal()
    load()
  } catch (err) {
    detailModal.value.error = err.response?.data?.error ?? err.message ?? 'Failed to accept offer'
  } finally {
    acceptingOfferId.value = null
  }
}

// Reset-password state for the student detail modal. Mirrors the
// partner-user reset pattern: prompts for an optional custom password,
// shows the new password inline until the modal closes.
const resettingStudentPw = ref(false)
const resetStudentPwValue = ref('')

async function resetStudentPassword() {
  if (!detailModal.value || resettingStudentPw.value) return
  const entered = prompt(`Reset password for ${detailModal.value.name}\n\nEnter a custom password (or leave blank for an auto-generated one):`, '')
  if (entered === null) return
  resettingStudentPw.value = true
  resetStudentPwValue.value = ''
  try {
    const body = entered.trim() ? { password: entered.trim() } : {}
    const res = await api.post(`/v1/admin/students/${detailModal.value.studentId}/reset-password`, body)
    resetStudentPwValue.value = res.data.temporaryPassword
  } catch (err) {
    reviewToast.value = err.response?.data?.error ?? err.message ?? 'Failed to reset password'
    setTimeout(() => { reviewToast.value = '' }, 3000)
  } finally {
    resettingStudentPw.value = false
  }
}
function copyResetStudentPw() {
  navigator.clipboard.writeText(resetStudentPwValue.value).catch(() => {})
}

async function openStudentDetail(s, preselectEnrollmentId = null) {
  // Clear any reset-password reveal from a previous student so the value
  // doesn't bleed across modals.
  resetStudentPwValue.value = ''
  personalSaveError.value = ''
  personalSaveOk.value = ''
  backgroundSaveError.value = ''
  backgroundSaveOk.value = ''
  detailModal.value = reactive({
    studentId: s.studentId,
    studentNumber: s.studentNumber,
    name: `${s.firstName ?? ''} ${s.lastName ?? ''}`.trim() || '—',
    email: s.email,
    partnerName: s.partnerName,
    activeTab: 'details',
    activeEnrollmentId: preselectEnrollmentId ?? s.enrollments?.[0]?.studentEnrollmentId ?? null,
    data: null,
    loading: true,
    error: '',
  })
  try {
    if (!languages.value.length || !nationalities.value.length) {
      const [langs, nats] = await Promise.all([
        api.get('/v1/public/languages'),
        api.get('/v1/public/nationalities'),
      ])
      languages.value = langs.data.items ?? []
      nationalities.value = nats.data.items ?? []
    }
    const res = await api.get(`/v1/admin/students/${s.studentId}`)
    detailModal.value.data = normaliseDetailForEdit(res.data)
    // Pin the active enrolment to the most-actionable / first one returned.
    if (!detailModal.value.activeEnrollmentId && res.data.enrollments?.length) {
      detailModal.value.activeEnrollmentId = res.data.enrollments[0].studentEnrollmentId
    }
  } catch (err) {
    detailModal.value.error = err.response?.data?.error ?? err.message ?? 'Failed to load student'
  } finally {
    detailModal.value.loading = false
  }
}

// Server returns the detail with possibly-missing inner objects (e.g.
// background can be null for a freshly created student). Inline editing
// binds straight to these paths via v-model, so we ensure every input
// has a stable target — otherwise Vue throws on null reads.
function normaliseDetailForEdit(data) {
  data.account = data.account || { firstName: null, lastName: null }
  data.personal = data.personal || { dateOfBirth: null, passportId: null, nationalityId: null, address: {} }
  data.personal.address = data.personal.address || {}
  data.background = data.background || { highestDegree: null, yearsWorkExperience: 0, languages: [] }
  data.background.languages = data.background.languages || []
  return data
}

const personalSaveError = ref('')
const personalSaveOk = ref('')
const backgroundSaveError = ref('')
const backgroundSaveOk = ref('')
const savingPersonal = ref(false)
const savingBackground = ref(false)

// HTML date input wants "YYYY-MM-DD" — round-trip through a computed so the
// server's ISO string and the input's date string stay in sync.
const personalDobInput = computed({
  get() {
    const v = detailModal.value?.data?.personal?.dateOfBirth
    return v ? String(v).slice(0, 10) : ''
  },
  set(v) {
    if (!detailModal.value?.data?.personal) return
    detailModal.value.data.personal.dateOfBirth = v ? new Date(v).toISOString() : null
  },
})

async function saveAdminPersonal() {
  if (!detailModal.value?.data) return
  savingPersonal.value = true
  personalSaveError.value = ''
  personalSaveOk.value = ''
  try {
    const d = detailModal.value.data
    await api.patch(`/v1/admin/students/${detailModal.value.studentId}/personal`, {
      firstName: d.account?.firstName ?? null,
      lastName: d.account?.lastName ?? null,
      dateOfBirth: d.personal?.dateOfBirth ?? null,
      passportId: d.personal?.passportId ?? null,
      nationalityId: d.personal?.nationalityId ?? null,
      addressLine1: d.personal?.address?.line1 ?? null,
      addressLine2: d.personal?.address?.line2 ?? null,
      city: d.personal?.address?.city ?? null,
      stateRegion: d.personal?.address?.stateRegion ?? null,
      postalCode: d.personal?.address?.postalCode ?? null,
      countryCode: d.personal?.address?.countryCode ?? null,
    })
    personalSaveOk.value = 'Saved.'
    setTimeout(() => { personalSaveOk.value = '' }, 2500)
    await refreshDetailModal()
    load()
  } catch (err) {
    personalSaveError.value = err.response?.data?.error ?? err.message ?? 'Save failed.'
  } finally {
    savingPersonal.value = false
  }
}

async function saveAdminBackground() {
  if (!detailModal.value?.data) return
  savingBackground.value = true
  backgroundSaveError.value = ''
  backgroundSaveOk.value = ''
  try {
    const d = detailModal.value.data
    await api.patch(`/v1/admin/students/${detailModal.value.studentId}/background`, {
      highestDegree: d.background?.highestDegree ?? null,
      yearsWorkExperience: d.background?.yearsWorkExperience ?? 0,
      languages: (d.background?.languages || [])
        .filter(l => l.languageId > 0)
        .map(l => ({ languageId: l.languageId, proficiency: l.proficiency })),
    })
    backgroundSaveOk.value = 'Saved.'
    setTimeout(() => { backgroundSaveOk.value = '' }, 2500)
    await refreshDetailModal()
  } catch (err) {
    backgroundSaveError.value = err.response?.data?.error ?? err.message ?? 'Save failed.'
  } finally {
    savingBackground.value = false
  }
}

function addAdminLanguage() {
  if (!detailModal.value?.data?.background) return
  detailModal.value.data.background.languages.push({ languageId: 0, proficiency: 1 })
}
function removeAdminLanguage(idx) {
  detailModal.value.data.background.languages.splice(idx, 1)
}

// Documents tab: groups uploaded docs by enrolment (so the admin sees
// "this passport went on the BBA application, this CV went on the MBA")
// and partitions them into core vs additional based on the server's
// `isAdditional` flag. Includes every enrolment even if empty so the
// "Add additional document" affordance is always reachable.
const docsByEnrollment = computed(() => {
  const data = detailModal.value?.data
  if (!data?.enrollments?.length) return []
  const allDocs = data.documents || []
  return data.enrollments.map(e => {
    const docs = allDocs
      .filter(d => d.enrollmentId === e.studentEnrollmentId)
      .sort((a, b) => (a.documentTypeName || '').localeCompare(b.documentTypeName || ''))
    return {
      enrollmentId: e.studentEnrollmentId,
      programmeCode: e.programmeCode,
      programmeName: e.programmeName,
      specializationName: e.specializationName,
      coreDocs: docs.filter(d => !d.isAdditional),
      additionalDocs: docs.filter(d => d.isAdditional),
    }
  })
})

const additionalDialog = reactive({ open: false, uploadEndpoint: '' })
function openAdditionalDialog(enrollmentId) {
  const studentId = detailModal.value?.studentId
  if (!studentId || !enrollmentId) return
  additionalDialog.uploadEndpoint =
    `/v1/admin/students/${studentId}/enrollments/${enrollmentId}/documents`
  additionalDialog.open = true
}
async function onAdditionalUploaded() {
  if (!detailModal.value?.studentId) return
  await refreshDetailModal()
}

// Admin Replace flow on an existing slot doc — only enabled when the
// current doc isn't yet partner/admission-verified.
async function onAdminReplace(ev, enrollmentId, doc) {
  const file = ev.target.files?.[0]
  ev.target.value = ''
  if (!file || !detailModal.value?.studentId) return
  try {
    const body = new FormData()
    body.append('documentTypeId', doc.documentTypeId)
    body.append('isAdditional', 'false')
    body.append('file', file)
    await api.post(
      `/v1/admin/students/${detailModal.value.studentId}/enrollments/${enrollmentId}/documents`,
      body)
    await refreshDetailModal()
  } catch (err) {
    reviewToast.value = err.response?.data?.error
      ?? err.message
      ?? 'Replace failed.'
    setTimeout(() => { reviewToast.value = '' }, 3000)
  }
}

async function refreshDetailModal() {
  if (!detailModal.value?.studentId) return
  try {
    const res = await api.get(`/v1/admin/students/${detailModal.value.studentId}`)
    detailModal.value.data = normaliseDetailForEdit(res.data)
  } catch { /* keep stale view */ }
}

function docPillClass(status) {
  if (status === 'VerifiedByPartner' || status === 'VerifiedByEnrolment') return 'doc-pill-ok'
  if (status === 'RejectedByPartner' || status === 'RejectedByEnrolment') return 'doc-pill-bad'
  return 'doc-pill-pending'
}
function docPillIcon(status) {
  if (status === 'VerifiedByPartner' || status === 'VerifiedByEnrolment') return '✓'
  if (status === 'RejectedByPartner' || status === 'RejectedByEnrolment') return '✕'
  return '·'
}
async function downloadStudentDoc(d) {
  if (!d?.studentDocumentId || !detailModal.value) return
  try {
    const res = await api.get(
      `/v1/admin/students/${detailModal.value.studentId}/documents/${d.studentDocumentId}/file`,
      { responseType: 'blob' })
    const url = URL.createObjectURL(res.data)
    window.open(url, '_blank')
    setTimeout(() => URL.revokeObjectURL(url), 60_000)
  } catch (err) {
    reviewToast.value = err.response?.status === 404
      ? 'File not found.'
      : (err.response?.data?.error ?? err.message ?? 'Download failed')
    setTimeout(() => { reviewToast.value = '' }, 3000)
  }
}

async function downloadLetter(letter) {
  if (!letter?.studentDocumentId || !detailModal.value) return
  try {
    const res = await api.get(
      `/v1/admin/students/${detailModal.value.studentId}/documents/${letter.studentDocumentId}/file`,
      { responseType: 'blob' })
    const url = URL.createObjectURL(res.data)
    const a = document.createElement('a')
    a.href = url
    a.download = letter.fileName ?? 'letter.pdf'
    a.target = '_blank'
    document.body.appendChild(a); a.click(); document.body.removeChild(a)
    setTimeout(() => URL.revokeObjectURL(url), 60_000)
  } catch (err) {
    reviewToast.value = err.response?.status === 404
      ? 'File not found.'
      : (err.response?.data?.error ?? err.message ?? 'Download failed')
    setTimeout(() => { reviewToast.value = '' }, 3000)
  }
}

function formatDate(iso) {
  if (!iso) return ''
  return new Date(iso).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })
}
function formatAddress(addr) {
  if (!addr) return ''
  const parts = [addr.line1, addr.city, addr.stateRegion, addr.postalCode, addr.countryCode]
    .filter(s => !!(s && s.trim?.() !== ''))
  return parts.join(', ')
}

// Grade approval modal — admin opens it from the row, sees the partner's
// submitted scores, then either approves (→ GradesApproved) or rejects
// with a reason (→ AwaitingGradesSubmit, partner sees the reason).

// Predefined rejection messages. Picking one fills the textarea; admin can
// still tweak the wording before sending.
const REJECT_PRESETS = [
  { id: 'payment',     label: 'Tuition not fully paid', text: 'Grades cannot be approved while there is an outstanding tuition balance. Please clear the balance and resubmit.' },
  { id: 'incomplete',  label: 'Grades incomplete',      text: 'One or more required subjects are missing a grade. Please enter every subject\'s score and resubmit.' },
  { id: 'inconsistent',label: 'Inconsistent with records', text: 'The submitted grades do not match the academic record on file. Please verify each score against the source and resubmit.' },
  { id: 'other',       label: 'Other (write your own)', text: '' },
]

const gradeModal = ref(null)
async function openGradeReview(s, e) {
  gradeModal.value = reactive({
    studentId: s.studentId,
    enrollmentId: e.studentEnrollmentId,
    studentName: `${s.firstName ?? ''} ${s.lastName ?? ''}`.trim(),
    programmeCode: e.programmeCode,
    specializationName: e.specializationName,
    subjects: [],
    mode: 'view',          // 'view' | 'reject' | 'submit'
    rejectReason: '',
    rejectPreset: '',
    confirmTuitionPaid: false,
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

async function openGradeSubmit(s, e) {
  gradeModal.value = reactive({
    studentId: s.studentId,
    enrollmentId: e.studentEnrollmentId,
    studentName: `${s.firstName ?? ''} ${s.lastName ?? ''}`.trim(),
    programmeCode: e.programmeCode,
    specializationName: e.specializationName,
    subjects: [],
    mode: 'submit',
    rejectReason: '',
    rejectPreset: '',
    confirmTuitionPaid: false,
    loading: true,
    submitting: false,
    error: '',
  })
  try {
    const res = await api.get(`/v1/admin/students/${s.studentId}/enrollments/${e.studentEnrollmentId}/subjects`)
    gradeModal.value.subjects = (res.data.items ?? []).map(r => ({ ...r, score: r.score ?? null }))
  } catch (err) {
    gradeModal.value.error = err.response?.data?.error ?? err.message ?? 'Failed to load subjects'
  } finally {
    gradeModal.value.loading = false
  }
}

const canCommitAdminGrades = computed(() => {
  const m = gradeModal.value
  if (!m?.subjects?.length) return false
  return m.subjects.every(r => Number.isFinite(r.score) && r.score >= 0 && r.score <= 100)
})

async function confirmGradeSubmission() {
  const m = gradeModal.value
  if (!m || m.submitting || !canCommitAdminGrades.value) return
  m.submitting = true
  m.error = ''
  try {
    await api.post(
      `/v1/admin/students/${m.studentId}/enrollments/${m.enrollmentId}/grades`,
      { items: m.subjects.map(r => ({ subjectId: r.subjectId, score: r.score })) })
    reviewToast.value = 'Grades submitted.'
    setTimeout(() => { reviewToast.value = '' }, 3000)
    gradeModal.value = null
    await load()
    if (detailModal.value?.studentId === m.studentId) await refreshDetailModal()
  } catch (err) {
    m.error = err.response?.data?.error ?? err.message ?? 'Failed to submit grades'
  } finally {
    if (gradeModal.value) gradeModal.value.submitting = false
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
// Picks a templated rejection message and copies it into the textarea.
// "Other" leaves the textarea untouched so admin can write freely. We
// only overwrite when there's an actual preset body to copy in — picking
// the placeholder "—" doesn't blank a half-typed reason.
function onRejectPresetChange() {
  const m = gradeModal.value
  if (!m) return
  const preset = REJECT_PRESETS.find(p => p.id === m.rejectPreset)
  if (preset && preset.text) m.rejectReason = preset.text
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
// Fuzzy search across every field admin might type. Rebuilt whenever the
// list changes; Fuse's threshold tuned to allow typos but stay specific.
const fuse = computed(() => new Fuse(list.value, {
  keys: [
    { name: 'studentNumber', weight: 0.9 },
    { name: 'firstName',     weight: 0.8 },
    { name: 'lastName',      weight: 0.8 },
    { name: 'username',      weight: 0.6 },
    { name: 'email',         weight: 0.6 },
    { name: 'partnerName',   weight: 0.5 },
    { name: 'enrollments.programmeCode', weight: 0.5 },
    { name: 'enrollments.programmeName', weight: 0.5 },
    { name: 'enrollments.specializationName', weight: 0.4 },
  ],
  threshold: 0.35,
  ignoreLocation: true,
  useExtendedSearch: true,
  minMatchCharLength: 2,
}))

const filtered = computed(() => {
  const q = search.value.trim()
  // Start either from the fuzzy search hits or the full list.
  let rows = !q
    ? list.value
    : fuse.value.search(q).map(r => r.item)

  if (filterProgrammeId.value)
    rows = rows.filter(s => s.enrollments.some(e => e.programmeId === filterProgrammeId.value))
  if (filterSpecializationId.value)
    rows = rows.filter(s => s.enrollments.some(e => e.specializationId === filterSpecializationId.value))
  if (filterStatusId.value !== '') {
    const f = STATUS_FILTERS.find(x => x.id === filterStatusId.value)
    rows = rows.filter(s => {
      const matchesNoEnrolment = f?.includeNoEnrolment && s.enrollments.length === 0
      const matchesCode = s.enrollments.some(e => f?.codes?.includes(e.statusCode))
      return matchesNoEnrolment || matchesCode
    })
  }
  return rows
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
.btn-grades-submit { background: #2563eb; }
.btn-grades-submit:hover:not(:disabled) { background: #1e40af; }
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

.approve-checks { background: #f4f9f5; border: 1px solid #b9e1c7; border-left: 3px solid #1c7a4a; border-radius: 6px; padding: .7rem .85rem; margin-top: .85rem; }
.approve-checks-title { font-size: .8rem; font-weight: 700; color: #1c4f33; margin-bottom: .45rem; }
.approve-check { display: flex; align-items: flex-start; gap: .55rem; font-size: .88rem; color: #1c4f33; cursor: pointer; line-height: 1.35; }
.approve-check input { margin-top: .15rem; transform: scale(1.1); cursor: pointer; }
.reject-preset { width: 100%; padding: .45rem .6rem; border: 1.5px solid #fbcaca; border-radius: 6px; font-size: .85rem; background: #fff; margin-bottom: .65rem; cursor: pointer; }
.reject-preset:focus { outline: none; border-color: #b91c1c; }
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

/* Student name link in the list — visual cue that the row opens a
   detail view. Behaves like a hyperlink. The whole row is also clickable. */
.s-name-link { color: #1a4d8c; font-weight: 600; cursor: pointer; text-decoration: none; }
.s-name-link:hover { text-decoration: underline; color: #143b6c; }
.btn-row-details { padding: .25rem .65rem; border: 1px solid #1a4d8c; background: #fff; color: #1a4d8c; border-radius: 4px; font-size: .75rem; font-weight: 600; cursor: pointer; }
.btn-row-details:hover { background: #eef3fb; }
.btn-row-details-sm { padding: .15rem .5rem; font-size: .7rem; }

/* Student detail modal (3 tabs) — fixed height so switching tabs
   doesn't make the modal grow or shrink. Tab content scrolls within. */
.detail-modal { width: 760px; max-width: 95vw; height: 80vh; max-height: 720px; display: flex; flex-direction: column; }
.muted-sub { color: #6b7888; font-weight: 400; font-size: .82rem; margin-left: .25rem; }
.detail-loading { padding: 1.5rem; }
.enr-switch { display: flex; align-items: center; gap: .65rem; padding: .55rem 1rem; background: #f6f9fc; border-bottom: 1px solid #eef2f7; font-size: .85rem; }
.enr-switch label { font-weight: 600; color: #4a5a72; }
.enr-switch select { padding: .25rem .5rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .85rem; min-width: 280px; }
.detail-tabs { display: flex; gap: .25rem; padding: .5rem 1rem 0; border-bottom: 1px solid #eef2f7; background: #fff; }
.tab-btn { background: transparent; border: none; padding: .55rem 1.1rem; font-size: .88rem; font-weight: 600; color: #6b7888; cursor: pointer; border-bottom: 2px solid transparent; }
.tab-btn:hover { color: #1a2d4f; }
.tab-btn.active { color: #1a4d8c; border-bottom-color: #1a4d8c; }
.tab-pane { padding: 1rem 1.25rem 1.25rem; overflow-y: auto; flex: 1; }

.detail-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 1.25rem; }
.detail-section h4 { margin: 0 0 .45rem 0; font-size: .82rem; font-weight: 700; text-transform: uppercase; letter-spacing: .04em; color: #6b7888; }
.detail-section dl { margin: 0; display: grid; grid-template-columns: max-content 1fr; gap: .25rem .75rem; font-size: .85rem; }
.detail-section dt { color: #6b7888; }
.detail-section dd { margin: 0; color: #1a2d4f; word-break: break-word; }
.reset-pw-row { margin-top: .5rem; display: flex; flex-direction: column; gap: .35rem; }
.reset-pw-reveal { padding: .5rem .65rem; background: #ecfdf5; border: 1px solid #6ee7b7; border-radius: 6px; font-size: .8rem; display: flex; flex-wrap: wrap; align-items: center; gap: .5rem; }
.reset-pw-reveal code { font-family: monospace; color: #065f46; background: #fff; padding: .1rem .4rem; border-radius: 3px; }
.reset-pw-hint { width: 100%; font-size: .7rem; color: #047857; }

.edit-grid { display: grid; grid-template-columns: 1fr 1fr; gap: .55rem .75rem; font-size: .82rem; }
.edit-field { display: flex; flex-direction: column; gap: .15rem; color: #4a5a72; }
.edit-field-wide { grid-column: 1 / -1; }
.edit-field input, .edit-field select { padding: .35rem .55rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .85rem; background: #fff; color: #1a2d4f; }
.btn-save-admin { margin-top: .75rem; }
.ok-banner { background: #ecfdf5; border: 1px solid #6ee7b7; color: #065f46; padding: .4rem .65rem; border-radius: 5px; font-size: .8rem; margin: .35rem 0; }
.lang-block { margin-top: .6rem; }
.lang-head { display: flex; align-items: center; justify-content: space-between; font-size: .78rem; color: #6b7888; margin-bottom: .35rem; }
.lang-row { display: grid; grid-template-columns: 1fr 1fr auto; gap: .35rem; margin-bottom: .3rem; }
.lang-row select { padding: .3rem .5rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .82rem; background: #fff; }
.btn-remove { color: #b91c1c; }

.docs-group { margin-bottom: 1rem; }
.docs-group-head { font-size: .82rem; color: #1a2d4f; padding: .35rem .5rem; background: #eef3fb; border-left: 3px solid #1a4d8c; border-radius: 4px; margin-bottom: .35rem; display: flex; align-items: center; gap: .5rem; }
.docs-group-count { margin-left: auto; background: #fff; border: 1px solid #cfd7e3; border-radius: 10px; padding: .05rem .5rem; font-size: .7rem; font-weight: 700; color: #4a5a72; }
.docs-list { display: flex; flex-direction: column; gap: .35rem; }
.doc-row { display: flex; align-items: center; gap: .65rem; padding: .5rem .65rem; background: #fff; border: 1px solid #eef2f7; border-radius: 6px; }
.doc-pill { display: inline-flex; align-items: center; justify-content: center; width: 22px; height: 22px; border-radius: 50%; font-size: .78rem; font-weight: 700; flex-shrink: 0; }
.doc-pill-ok { background: #d1fae5; color: #065f46; }
.doc-pill-bad { background: #fee2e2; color: #991b1b; }
.doc-pill-pending { background: #fef3c7; color: #92400e; }
.doc-info { flex: 1; min-width: 0; }
.doc-name { font-size: .86rem; font-weight: 600; color: #1a2d4f; }
.doc-sub { font-size: .72rem; color: #6b7888; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.docs-subhead { font-size: .72rem; font-weight: 700; color: #6b7888; text-transform: uppercase; letter-spacing: .04em; margin: .5rem 0 .25rem; }
.pill-additional { display: inline-block; margin-left: .4rem; padding: 0 .4rem; background: #eef2ff; color: #3730a3; border: 1px solid #c7d2fe; border-radius: 999px; font-size: .65rem; font-weight: 700; vertical-align: middle; }

.letters-list { display: flex; flex-direction: column; gap: .5rem; }
.letter-row { display: flex; align-items: center; gap: .65rem; padding: .55rem .75rem; background: #f6f9fc; border: 1px solid #eef2f7; border-radius: 7px; }
.letter-row.disabled { opacity: .55; }
.letter-icon { font-size: 1.2rem; }
.letter-info { flex: 1; min-width: 0; }
.letter-name { font-weight: 600; font-size: .88rem; color: #1a2d4f; }
.letter-sub { font-size: .76rem; color: #6b7888; }
.btn-mini { padding: .3rem .75rem; border: 1px solid #1a4d8c; background: #1a4d8c; color: #fff; border-radius: 5px; font-size: .78rem; font-weight: 600; cursor: pointer; }
.btn-mini:disabled { opacity: .5; cursor: not-allowed; background: #cbd5e1; border-color: #cbd5e1; }
.btn-mini:hover:not(:disabled) { background: #143b6c; }
</style>
