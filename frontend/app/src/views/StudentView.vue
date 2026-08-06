<template>
  <div class="student-portal">
    <nav class="navbar">
      <div class="navbar-brand">MGW Student Portal — {{ displayName }}</div>
      <button class="btn-logout" @click="logout">Log Out</button>
    </nav>

    <!-- Tab bar -->
    <div class="tab-bar">
      <button :class="['tab-btn', { active: tab === 'profile' }]" @click="tab = 'profile'">Profile</button>
      <button :class="['tab-btn', { active: tab === 'documents' }]" @click="tab = 'documents'">
        Uploaded Documents
        <span v-if="rejectedTotal" class="tab-dot" title="Documents need replacement"></span>
      </button>
      <button :class="['tab-btn', { active: tab === 'programs' }]" @click="tab = 'programs'">
        Programs
        <span v-if="offerReady" class="tab-dot" title="An offer is waiting"></span>
      </button>
      <button :class="['tab-btn', { active: tab === 'cohorts' }]" @click="tab = 'cohorts'">Module Cohorts</button>
      <button :class="['tab-btn', { active: tab === 'notes' }]" @click="tab = 'notes'">Notes</button>
      <button :class="['tab-btn', { active: tab === 'forms' }]" @click="tab = 'forms'">Forms</button>
      <button :class="['tab-btn', { active: tab === 'mail' }]" @click="tab = 'mail'">Mail</button>
    </div>

    <div class="tab-content">
      <div v-if="loadError" class="err-banner">{{ loadError }}</div>
      <div v-else-if="!loaded" class="loading">Loading…</div>

      <template v-else-if="data">
        <div v-if="!data.enrollments?.length && tab !== 'profile' && tab !== 'forms' && tab !== 'mail'" class="empty">
          You don't have any applications yet.
        </div>

        <!-- ══════════ PROFILE ══════════ -->
        <template v-if="tab === 'profile'">
          <div class="profile-grid">
            <div class="panel">
              <h3 class="panel-title">Account</h3>
              <dl class="summary">
                <div><dt>Name</dt><dd>{{ displayName }}</dd></div>
                <div><dt>Student ID</dt><dd>{{ data.studentNumber || '—' }}
                  <span v-for="(i, ix) in (data.identifiers ?? []).filter(x => !x.isPrimary)" :key="ix" class="muted" style="font-size:.8rem;"> · {{ i.value }}</span>
                </dd></div>
                <div><dt>Email</dt><dd>{{ data.account?.email || '—' }}
                  <span v-if="data.account?.emailVerified" class="doc-pill tone-green">Verified</span></dd></div>
                <div><dt>Username</dt><dd>{{ data.account?.username || '—' }}</dd></div>
                <div><dt>Partner</dt><dd>{{ data.partner?.name || '—' }}</dd></div>
              </dl>
            </div>
            <div class="panel">
              <h3 class="panel-title">Personal</h3>
              <dl class="summary">
                <div><dt>Date of birth</dt><dd>{{ formatDate(data.personal?.dateOfBirth) || '—' }}</dd></div>
                <div><dt>Passport / ID</dt><dd>{{ data.personal?.passportId || '—' }}</dd></div>
                <div><dt>Address</dt><dd>{{ addressLine || '—' }}</dd></div>
              </dl>
            </div>
            <div class="panel">
              <h3 class="panel-title">Background</h3>
              <dl class="summary">
                <div><dt>Highest degree</dt><dd>{{ data.background?.highestDegree || '—' }}</dd></div>
                <div><dt>Degree specialization</dt><dd>{{ data.background?.degreeSpecialization || '—' }}</dd></div>
                <div><dt>Years of work experience</dt><dd>{{ data.background?.yearsWorkExperience ?? '—' }}</dd></div>
              </dl>
            </div>
          </div>
          <p class="muted-extra">To correct any of this information, contact your partner{{ data.partner?.contactEmail ? ` (${data.partner.contactEmail})` : '' }} or the Admission Office.</p>
        </template>

        <!-- ══════════ UPLOADED DOCUMENTS ══════════ -->
        <template v-else-if="tab === 'documents'">
          <div v-for="enr in data.enrollments" :key="enr.enrollmentId" class="enr-card">
            <div class="enr-head">
              <div>
                <strong>{{ enr.programmeName }}</strong>
                <span class="badge-code">{{ enr.programmeCode }}</span>
                <span class="badge-specialization">{{ enr.specializationName }}</span>
              </div>
              <span class="badge-status" :class="`tone-${badgeFor(enr).tone}`">{{ badgeFor(enr).label }}</span>
            </div>

            <div v-if="enr.isRejected" class="action-banner action-bad">
              <div class="action-banner-title">{{ rejectedDocCount(enr) }} document(s) need replacement</div>
              <div v-if="enr.rejectionSummary?.byName" class="action-banner-meta">
                Returned by {{ enr.rejectionSummary.byName }} on {{ formatDate(enr.rejectionSummary.atUtc) }}
              </div>
            </div>

            <ul class="doc-list">
              <li v-for="doc in enr.requiredDocuments" :key="doc.documentTypeId" class="doc-row">
                <div class="doc-info">
                  <span :class="['doc-mark', doc.uploaded ? (doc.isRejected ? 'mark-bad' : 'mark-ok') : 'mark-pending']">
                    {{ doc.uploaded ? (doc.isRejected ? '×' : '✓') : '·' }}
                  </span>
                  <div class="doc-text">
                    <strong>{{ doc.name }}</strong>
                    <p class="doc-meta">
                      <span v-if="doc.uploaded">{{ doc.fileName }} · uploaded {{ formatDate(doc.uploadedAt) }}</span>
                      <span v-else>Not uploaded</span>
                    </p>
                    <span v-if="doc.statusName" class="doc-pill" :class="docPillTone(doc)">{{ doc.statusName }}</span>
                  </div>
                </div>
                <div class="doc-actions">
                  <label v-if="canReplace(enr, doc)" class="btn-upload">
                    {{ doc.uploaded ? 'Replace' : 'Upload' }}
                    <input type="file" :accept="acceptedTypes" @change="onPick($event, enr, doc)" />
                  </label>
                  <span v-else class="lock-note">{{ replaceLockReason(enr, doc) }}</span>
                </div>
              </li>
            </ul>

            <div v-for="doc in enr.requiredDocuments.filter(d => d.isRejected && d.rejectionReasons)"
                 :key="`r-${enr.enrollmentId}-${doc.documentTypeId}`" class="reject-card">
              <div class="reject-card-head">
                <strong>{{ doc.name }}</strong>
                <span v-if="doc.rejectionReasons.byName">by {{ doc.rejectionReasons.byName }}</span>
                <span class="reject-card-date">{{ formatDate(doc.rejectionReasons.atUtc) }}</span>
              </div>
              <div v-if="parsedReasons(doc).reasons.length" class="reject-chips">
                <span v-for="r in parsedReasons(doc).reasons" :key="r" class="reject-chip">{{ r }}</span>
              </div>
              <p v-if="parsedReasons(doc).freeText" class="reject-free">{{ parsedReasons(doc).freeText }}</p>
            </div>

            <div class="extra-docs">
              <div class="extra-docs-head">
                <strong>Additional documents</strong>
                <button class="btn-secondary" @click="openAddAdditional(enr.enrollmentId)">+ Add another document</button>
              </div>
              <ul v-if="enr.additionalDocuments?.length" class="doc-list">
                <li v-for="d in enr.additionalDocuments" :key="d.studentDocumentId" class="doc-row">
                  <div class="doc-info">
                    <span class="doc-mark mark-ok">✓</span>
                    <div class="doc-text">
                      <strong>{{ d.documentTypeName }}</strong>
                      <p class="doc-meta">{{ d.fileName }} · uploaded {{ formatDate(d.uploadedAt) }}</p>
                      <span class="doc-pill">{{ d.statusName }}</span>
                    </div>
                  </div>
                </li>
              </ul>
              <p v-else class="muted-extra">None added.</p>
            </div>

            <div v-if="enr.canResubmit" class="actions">
              <button
                class="btn-primary"
                :disabled="!canResubmit(enr) || busy"
                :title="canResubmit(enr) ? '' : 'Replace every rejected document first.'"
                @click="resubmit(enr)"
              >Resubmit application</button>
              <span v-if="!canResubmit(enr)" class="action-hint">Replace every rejected document first.</span>
            </div>
          </div>
        </template>

        <!-- ══════════ PROGRAMS ══════════ -->
        <template v-else-if="tab === 'programs'">
          <div v-if="data.enrollments?.length" class="programs-layout">
            <!-- Left menu -->
            <aside class="prog-menu">
              <button v-for="enr in data.enrollments" :key="enr.enrollmentId"
                      :class="['prog-item', { active: enr.enrollmentId === selectedEnrId }]"
                      @click="selectedEnrId = enr.enrollmentId">
                <strong>{{ enr.programmeName }}</strong>
                <span class="prog-item-sub">{{ enr.specializationName }}</span>
                <span class="badge-status" :class="`tone-${badgeFor(enr).tone}`">{{ badgeFor(enr).label }}</span>
              </button>
            </aside>

            <!-- Selected programme -->
            <div v-if="selectedEnr" class="prog-detail enr-card">
              <div class="enr-head">
                <div>
                  <strong>{{ selectedEnr.programmeName }}</strong>
                  <span class="badge-code">{{ selectedEnr.programmeCode }}</span>
                  <span class="badge-specialization">{{ selectedEnr.specializationName }}</span>
                </div>
                <span class="badge-status" :class="`tone-${badgeFor(selectedEnr).tone}`">{{ badgeFor(selectedEnr).label }}</span>
              </div>

              <div v-if="selectedEnr.isRejected" class="action-banner action-bad">
                <div class="action-banner-title">{{ rejectedDocCount(selectedEnr) }} document(s) need replacement</div>
                <div class="action-banner-meta">See the Uploaded Documents tab to replace them.</div>
              </div>
              <div v-else-if="selectedEnr.canAcceptOffer" class="action-banner action-blue">
                <div class="action-banner-title">Your offer is ready</div>
                <div class="action-banner-meta">Review the offer letter below, then accept to continue.</div>
                <button class="btn-primary" :disabled="busy" @click="acceptOffer(selectedEnr)">Accept Offer</button>
              </div>
              <div v-else-if="isReviewing(selectedEnr.statusCode)" class="action-banner action-info">
                Your application is being reviewed. We'll let you know as soon as there's an update.
              </div>
              <div v-else-if="selectedEnr.statusCode === 'ApplicationApprovedAdmission'" class="action-banner action-blue">
                <div class="action-banner-title">Approved by Admission</div>
                <div class="action-banner-meta">Final admission step coming soon.</div>
              </div>

              <dl class="summary">
                <div><dt>Specialization</dt><dd>{{ selectedEnr.specializationName || '—' }}</dd></div>
                <div><dt>Duration</dt><dd>{{ selectedEnr.durationOfStudyMonths ? `${selectedEnr.durationOfStudyMonths} months` : '—' }}</dd></div>
              </dl>

              <!-- Modules of the specialization -->
              <h4 class="section-h">Modules</h4>
              <div v-if="modulesByEnr[selectedEnr.enrollmentId]?.loading" class="loading">Loading modules…</div>
              <div v-else-if="modulesByEnr[selectedEnr.enrollmentId]?.error" class="err-banner">
                {{ modulesByEnr[selectedEnr.enrollmentId].error }}
              </div>
              <table v-else-if="modulesByEnr[selectedEnr.enrollmentId]?.modules?.length" class="mod-table">
                <thead>
                  <tr><th>Code</th><th>Module</th><th>ECTS</th><th>Cohort</th><th>Grade</th></tr>
                </thead>
                <tbody>
                  <tr v-for="m in modulesByEnr[selectedEnr.enrollmentId].modules" :key="m.subjectId">
                    <td class="mod-code">{{ m.code }}</td>
                    <td>{{ m.name }}<span v-if="m.isThesis" class="badge-specialization" style="margin-left:.35rem;">Thesis</span></td>
                    <td>{{ m.ects }}</td>
                    <td>{{ m.cohortNumber || '—' }}</td>
                    <td>
                      <span v-if="m.gradeLocked" class="muted-extra">🔒 Questionnaires pending</span>
                      <span v-else-if="m.score != null" class="grade-badge">{{ m.score }} / 100</span>
                      <span v-else class="muted-extra">—</span>
                    </td>
                  </tr>
                </tbody>
              </table>
              <p v-else class="muted-extra">No modules defined for this specialization yet.</p>

              <!-- Letters & Certs / Transcript -->
              <h4 class="section-h">Letters &amp; Certificates</h4>
              <div class="doc-strip">
                <div class="doc-mini" :class="{ disabled: !canDownloadOffer(selectedEnr) }">
                  <div class="doc-mini-icon">📄</div>
                  <div class="doc-mini-info">
                    <div class="doc-mini-name">Offer Letter</div>
                    <div class="doc-mini-sub">{{ canDownloadOffer(selectedEnr) ? 'Ready' : 'Not yet issued' }}</div>
                  </div>
                  <button class="btn-mini" :disabled="!canDownloadOffer(selectedEnr)" @click="downloadOffer(selectedEnr)">Download</button>
                </div>
                <div class="doc-mini" :class="{ disabled: !canDownloadAdmission(selectedEnr) }">
                  <div class="doc-mini-icon">📋</div>
                  <div class="doc-mini-info">
                    <div class="doc-mini-name">Admission Letter</div>
                    <div class="doc-mini-sub">{{ canDownloadAdmission(selectedEnr) ? 'Confirmed' : 'Available after admission' }}</div>
                  </div>
                  <button class="btn-mini" :disabled="!canDownloadAdmission(selectedEnr)" @click="downloadAdmission(selectedEnr)">Download</button>
                </div>
                <div class="doc-mini" :class="{ disabled: !canDownloadTranscript(selectedEnr) && !inGrading(selectedEnr) }">
                  <div class="doc-mini-icon">📑</div>
                  <div class="doc-mini-info">
                    <div class="doc-mini-name">Digital Transcript</div>
                    <div class="doc-mini-sub">
                      {{ canDownloadTranscript(selectedEnr) ? 'Ready'
                         : inGrading(selectedEnr) ? 'Provisional available while grading'
                         : 'Available after grades approved' }}
                    </div>
                  </div>
                  <button v-if="canDownloadTranscript(selectedEnr)" class="btn-mini" @click="downloadTranscript(selectedEnr)">Download</button>
                  <button v-else-if="inGrading(selectedEnr)" class="btn-mini" :disabled="provisionalBusy === selectedEnr.enrollmentId"
                          @click="downloadProvisional(selectedEnr)">
                    {{ provisionalBusy === selectedEnr.enrollmentId ? '…' : 'Provisional' }}
                  </button>
                  <button v-else class="btn-mini" disabled>Download</button>
                </div>
                <div class="doc-mini" :class="{ disabled: !canDownloadCertificate(selectedEnr) }">
                  <div class="doc-mini-icon">🎓</div>
                  <div class="doc-mini-info">
                    <div class="doc-mini-name">Digital Certificate</div>
                    <div class="doc-mini-sub">{{ canDownloadCertificate(selectedEnr) ? 'Ready' : 'Not yet available' }}</div>
                  </div>
                  <button class="btn-mini" :disabled="!canDownloadCertificate(selectedEnr)" @click="downloadCertificate(selectedEnr)">Download</button>
                </div>
                <!-- Config-created letters released for this enrolment. -->
                <div v-for="dl in (selectedEnr.dynamicLetters ?? [])" :key="dl.letterTypeDefinitionId" class="doc-mini">
                  <div class="doc-mini-icon">📄</div>
                  <div class="doc-mini-info">
                    <div class="doc-mini-name">{{ dl.name }}</div>
                    <div class="doc-mini-sub">Ready · {{ dl.letter?.fileName }}</div>
                  </div>
                  <button class="btn-mini" @click="downloadLetter(dl.letter)">Download</button>
                </div>
              </div>

              <EnrollmentActivityLog :key="`act-${selectedEnr.enrollmentId}`"
                :api-path="`/v1/student/me/enrollments/${selectedEnr.enrollmentId}/activity`" />
            </div>
          </div>
        </template>

        <!-- ══════════ MODULE COHORTS ══════════ -->
        <template v-else-if="tab === 'cohorts'">
          <div v-if="cohortsLoading" class="loading">Loading cohorts…</div>
          <div v-else-if="!allCohorts.length" class="empty">
            You haven't been assigned to any module cohorts yet.
          </div>
          <div v-else class="programs-layout">
            <!-- Left menu: assigned cohorts, grouped by programme -->
            <aside class="prog-menu">
              <template v-for="enr in data.enrollments" :key="`m-${enr.enrollmentId}`">
                <template v-if="cohortsByEnr[enr.enrollmentId]?.cohorts?.length">
                  <div class="menu-caption">{{ enr.programmeCode || enr.programmeName }}</div>
                  <button v-for="c in cohortsByEnr[enr.enrollmentId].cohorts" :key="c.moduleCohortId"
                          :class="['prog-item', { active: c.moduleCohortId === selectedCohort?.c.moduleCohortId }]"
                          @click="selectedCohortId = c.moduleCohortId">
                    <strong>{{ c.moduleName || c.moduleCode || 'Module' }}</strong>
                    <span class="prog-item-sub">{{ c.cohortNumber }}</span>
                    <span v-if="c.score != null" class="grade-badge">{{ c.score }} / 100</span>
                    <span v-else-if="c.gradeLocked" class="prog-item-sub">🔒 questionnaires pending</span>
                  </button>
                </template>
              </template>
            </aside>

            <!-- Right: selected cohort details -->
            <div v-if="selectedCohort" class="prog-detail enr-card">
              <div class="enr-head">
                <div>
                  <strong>{{ selectedCohort.c.moduleName || selectedCohort.c.moduleCode || 'Module' }}</strong>
                  <span v-if="selectedCohort.c.moduleCode" class="badge-code">{{ selectedCohort.c.moduleCode }}</span>
                  <span class="badge-specialization">{{ selectedCohort.enr.programmeName }}</span>
                </div>
                <span class="cohort-number">{{ selectedCohort.c.cohortNumber }}</span>
              </div>

              <dl class="summary">
                <div><dt>Teacher</dt><dd>{{ selectedCohort.c.teacherName || 'TBC' }}</dd></div>
                <div><dt>Start</dt><dd>{{ formatDate(selectedCohort.c.startDate) || 'TBC' }}</dd></div>
                <div><dt>End</dt><dd>{{ formatDate(selectedCohort.c.endDate) || 'TBC' }}</dd></div>
                <div>
                  <dt>Grade</dt>
                  <dd>
                    <span v-if="selectedCohort.c.gradeLocked" class="muted-extra">🔒 Complete the questionnaires below to see your grade</span>
                    <span v-else-if="selectedCohort.c.score != null" class="grade-badge">{{ selectedCohort.c.score }} / 100</span>
                    <span v-else class="muted-extra">Not graded yet</span>
                  </dd>
                </div>
              </dl>

              <template v-if="selectedCohort.c.questionnaires?.length">
                <h4 class="section-h">Course questionnaires</h4>
                <p v-if="selectedCohort.c.gradeLocked" class="doc-hint" style="margin:.1rem 0 .4rem;">
                  Your grade for this module unlocks once every questionnaire below is submitted.
                  Answers are anonymous.
                </p>
                <ul class="doc-list">
                  <li v-for="q in selectedCohort.c.questionnaires" :key="q.id" class="doc-row">
                    <div class="doc-info">
                      <span :class="['doc-mark', q.completed ? 'mark-ok' : 'mark-pending']">{{ q.completed ? '✓' : '·' }}</span>
                      <div class="doc-text">
                        <strong>{{ q.name }}</strong>
                        <p class="doc-meta">{{ q.completed ? 'Submitted' : 'Not filled out yet' }}</p>
                      </div>
                    </div>
                    <button v-if="!q.completed" class="btn-mini" @click="openQuestionnaire(q)">Fill out</button>
                  </li>
                </ul>
              </template>

              <h4 class="section-h">Module files</h4>
              <ul v-if="selectedCohort.c.files?.length" class="doc-list">
                <li v-for="f in selectedCohort.c.files" :key="f.id" class="doc-row">
                  <div class="doc-info">
                    <span class="doc-mark mark-ok">📎</span>
                    <div class="doc-text">
                      <strong>{{ f.fieldLabel }}</strong>
                      <p class="doc-meta">{{ f.fileName }}</p>
                    </div>
                  </div>
                  <button class="btn-mini" @click="downloadCohortFile(f)">Download</button>
                </li>
              </ul>
              <p v-else class="muted-extra">No files shared for this module yet.</p>

              <!-- Uploaded Assignments live on the cohort (same as the staff side). -->
              <h4 class="section-h">Uploaded Assignments</h4>
              <p class="doc-hint" style="margin:.1rem 0 .4rem;">
                Upload your module work (give each document a title) and follow your teachers' comments.
              </p>
              <AssignmentsPanel :key="`asg-${selectedCohort.c.moduleCohortId}`"
                :api-base="`/v1/student/me/enrollments/${selectedCohort.enr.enrollmentId}/assignments`"
                :subject-id="selectedCohort.c.subjectId" />
            </div>
          </div>
        </template>

        <!-- ══════════ NOTES ══════════ -->
        <template v-else-if="tab === 'notes'">
          <div class="panel">
            <h3 class="panel-title">Notes from the school</h3>
            <p v-if="notesState.loading" class="loading">Loading…</p>
            <p v-else-if="notesState.error" class="err-banner">{{ notesState.error }}</p>
            <p v-else-if="!notesState.notes.length" class="muted-extra">No notes have been shared with you.</p>
            <div v-else class="notes-list">
              <div v-for="n in notesState.notes" :key="n.studentLogNoteId" class="note-card">
                <div class="note-head">
                  <strong v-if="n.title">{{ n.title }}</strong>
                  <span class="badge-specialization">
                    {{ n.programmeName ? `${n.programmeName} / ${n.specializationName}` : 'General' }}
                  </span>
                  <span class="badge-code">{{ n.authorRole === 'Admission' ? 'Admission Office' : 'Partner' }}</span>
                  <span class="note-date">{{ formatDate(n.createdAt) }}</span>
                </div>
                <p class="note-content">{{ n.content }}</p>
              </div>
            </div>
          </div>
        </template>

        <!-- ══════════ FORMS ══════════ -->
        <template v-else-if="tab === 'mail'">
          <PartnerMailView endpoint="/v1/student/me/mail" />
        </template>

        <template v-else-if="tab === 'forms'">
          <div class="panel">
            <h3 class="panel-title">Forms</h3>
            <IntakeFillPanel api-base="/v1/student/me/intake-forms" />
          </div>
        </template>
      </template>
    </div>

    <div v-if="toast" class="toast">{{ toast }}</div>

    <!-- Cohort questionnaire fill modal -->
    <div v-if="fillQ.open" class="q-backdrop" @click="fillQ.open = false"></div>
    <div v-if="fillQ.open" class="q-modal">
      <div class="q-modal-head">
        <h3>{{ fillQ.name || 'Questionnaire' }}</h3>
        <button class="btn-mini" @click="fillQ.open = false">✕</button>
      </div>
      <div class="q-modal-body">
        <p v-if="fillQ.error" class="err-banner">{{ fillQ.error }}</p>
        <p v-else-if="!fillQ.questionnaire" class="loading">Loading…</p>
        <v-app v-else class="q-vapp">
          <v-main>
            <QuestionnaireRenderer
              :questionnaire="fillQ.questionnaire"
              :answers="fillQ.answers"
              mode="live"
              @change="onQChange"
              @submit="submitQuestionnaire" />
          </v-main>
        </v-app>
        <p v-if="fillQ.busy" class="loading">Submitting…</p>
      </div>
    </div>

    <AdditionalDocumentUploadDialog
      v-if="additionalDialog.open"
      types-endpoint="/v1/student/me/document-types"
      upload-endpoint="/v1/student/me/documents"
      :enrollment-id="additionalDialog.enrollmentId"
      @close="additionalDialog.open = false"
      @uploaded="onAdditionalUploaded" />
  </div>
</template>

<script setup>
import PartnerMailView from '../components/partner/PartnerMailView.vue'
import { ref, computed, onMounted, reactive, watch } from 'vue'
import { useRouter } from 'vue-router'
import EnrollmentActivityLog from '../components/letters/EnrollmentActivityLog.vue'
import IntakeFillPanel from '../components/intake/IntakeFillPanel.vue'
import AdditionalDocumentUploadDialog from '../components/letters/AdditionalDocumentUploadDialog.vue'
import AssignmentsPanel from '../components/assignments/AssignmentsPanel.vue'
import QuestionnaireRenderer from '../components/questionnaire/QuestionnaireRenderer.vue'
import { auth } from '../store/auth.js'
import api from '../api/client.js'
import { statusBadge, isReviewing, parseRejectionNote } from '../utils/applicationStatus.js'
import { ACCEPTED_DOC_ACCEPT_ATTR } from '../utils/uploadPolicy.js'

const router = useRouter()
const acceptedTypes = ACCEPTED_DOC_ACCEPT_ATTR

const tab = ref('profile')
const data = ref(null)
const loaded = ref(false)
const loadError = ref('')
const busy = ref(false)
const toast = ref('')

const displayName = computed(() => {
  const f = data.value?.account?.firstName
  const l = data.value?.account?.lastName
  return [f, l].filter(Boolean).join(' ') || auth.user?.displayName || 'Student'
})

const addressLine = computed(() => {
  const a = data.value?.personal?.address
  if (!a) return ''
  return [a.line1, a.city, a.stateRegion, a.postalCode, a.countryCode].filter(Boolean).join(', ')
})

// Tab notification dots.
const rejectedTotal = computed(() =>
  data.value?.enrollments?.reduce((n, e) => n + (e.requiredDocuments?.filter(d => d.isRejected).length ?? 0), 0) ?? 0)
const offerReady = computed(() => data.value?.enrollments?.some(e => e.canAcceptOffer) ?? false)

// Programs tab: left-menu selection.
const selectedEnrId = ref(null)
const selectedEnr = computed(() =>
  data.value?.enrollments?.find(e => e.enrollmentId === selectedEnrId.value)
  ?? data.value?.enrollments?.[0]
  ?? null)

function badgeFor(enr) { return statusBadge(enr.statusCode) }

function rejectedDocCount(enr) {
  return enr.requiredDocuments?.filter(d => d.isRejected).length ?? 0
}

function docPillTone(doc) {
  if (!doc.statusCode) return 'tone-grey'
  if (doc.statusCode === 'VerifiedByEnrolment' || doc.statusCode === 'VerifiedByPartner') return 'tone-green'
  if (doc.statusCode === 'RejectedByPartner' || doc.statusCode === 'RejectedByEnrolment') return 'tone-red'
  return 'tone-amber'
}

// Replace is allowed up until the partner approves the doc. After that
// (VerifiedByPartner / VerifiedByEnrolment) the server rejects the
// upload, so we hide the button. Anything else — pending, rejected,
// no upload yet — can be replaced freely by the student.
function canReplace(_enr, doc) {
  if (!doc.uploaded) return true
  return doc.statusCode !== 'VerifiedByPartner'
    && doc.statusCode !== 'VerifiedByEnrolment'
}
function replaceLockReason(_enr, doc) {
  if (doc.statusCode === 'VerifiedByEnrolment') return 'Verified by Admission — locked'
  if (doc.statusCode === 'VerifiedByPartner') return 'Verified by Partner — locked'
  return 'Locked'
}

function canResubmit(enr) {
  return enr.requiredDocuments.every(d => d.uploaded
    && d.statusCode !== 'RejectedByPartner'
    && d.statusCode !== 'RejectedByEnrolment')
}

function parsedReasons(doc) { return parseRejectionNote(doc.rejectionReasons?.note) }

// Letter availability is decided by whether a StudentDocument actually
// exists for that letter type — not by the workflow stage. A programme
// whose template is still unpublished will be at the right stage but
// won't have a letter doc, and the button stays disabled.
function canDownloadOffer(enr)       { return !!enr.letters?.offerLetter?.studentDocumentId }
function canDownloadAdmission(enr)   { return !!enr.letters?.admissionLetter?.studentDocumentId }
function canDownloadTranscript(enr)  { return !!enr.letters?.transcript?.studentDocumentId }
function canDownloadCertificate(enr) { return !!enr.letters?.certificate?.studentDocumentId }

function formatDate(iso) {
  if (!iso) return ''
  return new Date(iso).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })
}

async function load() {
  loadError.value = ''
  try {
    const res = await api.get('/v1/student/me/application')
    data.value = res.data
    if (!selectedEnrId.value && res.data?.enrollments?.length)
      selectedEnrId.value = res.data.enrollments[0].enrollmentId
  } catch (e) {
    loadError.value = e.response?.data?.error ?? e.message ?? 'Failed to load'
  } finally {
    loaded.value = true
  }
}

const additionalDialog = reactive({ open: false, enrollmentId: null })

function openAddAdditional(enrollmentId) {
  additionalDialog.enrollmentId = enrollmentId
  additionalDialog.open = true
}
async function onAdditionalUploaded() {
  await load()
}

async function onPick(event, enr, doc) {
  const file = event.target.files?.[0]
  event.target.value = ''
  if (!file) return
  if (file.size > 100 * 1024 * 1024) {
    showToast('File is larger than 100 MB.')
    return
  }
  busy.value = true
  const fd = new FormData()
  fd.append('enrollmentId', enr.enrollmentId)
  fd.append('documentTypeId', doc.documentTypeId)
  fd.append('file', file)
  try {
    await api.post('/v1/student/me/documents', fd, { headers: { 'Content-Type': 'multipart/form-data' } })
    await load()
    showToast(`${file.name} uploaded.`)
  } catch (e) {
    showToast(e.response?.data?.error ?? e.message ?? 'Upload failed')
  } finally {
    busy.value = false
  }
}

async function resubmit(enr) {
  if (!canResubmit(enr)) return
  busy.value = true
  try {
    await api.post(`/v1/student/me/application/${enr.enrollmentId}/resubmit`)
    await load()
    showToast('Application resubmitted.')
  } catch (e) {
    showToast(e.response?.data?.error ?? e.message ?? 'Resubmit failed')
  } finally {
    busy.value = false
  }
}

async function acceptOffer(enr) {
  busy.value = true
  try {
    await api.post(`/v1/student/me/application/${enr.enrollmentId}/accept-offer`)
    await load()
    showToast('Offer accepted.')
  } catch (e) {
    showToast(e.response?.data?.error ?? e.message ?? 'Accept failed')
  } finally {
    busy.value = false
  }
}

// Streams the released PDF from the backend and triggers a download.
async function downloadLetter(letter) {
  if (!letter?.studentDocumentId) return
  try {
    const res = await api.get(
      `/v1/student/me/documents/${letter.studentDocumentId}/file`,
      { responseType: 'blob' })
    const url = URL.createObjectURL(res.data)
    const a = document.createElement('a')
    a.href = url
    a.download = letter.fileName ?? 'letter.pdf'
    a.target = '_blank'
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
    setTimeout(() => URL.revokeObjectURL(url), 60_000)
  } catch (e) {
    showToast(e.response?.status === 404
      ? 'This letter is not available yet.'
      : (e.response?.data?.error ?? e.message ?? 'Download failed'))
  }
}
function downloadOffer(enr)       { return downloadLetter(enr.letters?.offerLetter) }
function downloadAdmission(enr)   { return downloadLetter(enr.letters?.admissionLetter) }
function downloadTranscript(enr)  { return downloadLetter(enr.letters?.transcript) }
function downloadCertificate(enr) { return downloadLetter(enr.letters?.certificate) }

// Grading window: the partner is entering grades but Admission hasn't approved
// yet, so the official transcript isn't released — a watermarked provisional
// one can still be previewed.
function inGrading(enr) {
  return enr.statusCode === 'AwaitingGradesSubmit' || enr.statusCode === 'AwaitingGradesApproval'
}

const provisionalBusy = ref(null)
async function downloadProvisional(enr) {
  if (provisionalBusy.value) return
  provisionalBusy.value = enr.enrollmentId
  try {
    const res = await api.get(
      `/v1/student/me/enrollments/${enr.enrollmentId}/transcript/provisional`,
      { responseType: 'blob' })
    const url = URL.createObjectURL(res.data)
    const a = document.createElement('a')
    a.href = url
    a.download = 'provisional-transcript.pdf'
    a.target = '_blank'
    document.body.appendChild(a); a.click(); document.body.removeChild(a)
    setTimeout(() => URL.revokeObjectURL(url), 60_000)
  } catch (e) {
    showToast(e.response?.status === 404
      ? 'Your transcript is not available to preview yet.'
      : (e.response?.data?.error ?? e.message ?? 'Download failed'))
  } finally {
    provisionalBusy.value = null
  }
}

// Programs tab: module list of the selected enrolment, lazy-loaded and cached.
const modulesByEnr = reactive({})
async function loadModules(enrollmentId) {
  if (!enrollmentId || modulesByEnr[enrollmentId]) return
  modulesByEnr[enrollmentId] = { loading: true, modules: [], error: '' }
  try {
    const res = await api.get(`/v1/student/me/enrollments/${enrollmentId}/modules`)
    modulesByEnr[enrollmentId] = { loading: false, modules: res.data.modules ?? [], error: '' }
  } catch (e) {
    modulesByEnr[enrollmentId] = {
      loading: false, modules: [],
      error: e.response?.data?.error ?? e.message ?? 'Failed to load modules',
    }
  }
}
watch([tab, selectedEnr], () => {
  if (tab.value === 'programs' && selectedEnr.value) loadModules(selectedEnr.value.enrollmentId)
}, { immediate: true })

// Module Cohorts tab: lazy-loaded per enrolment on first visit.
const cohortsByEnr = reactive({})
async function loadCohorts() {
  for (const enr of data.value?.enrollments ?? []) {
    if (cohortsByEnr[enr.enrollmentId]) continue
    cohortsByEnr[enr.enrollmentId] = { loading: true, cohorts: [], error: '' }
    try {
      const res = await api.get(`/v1/student/me/enrollments/${enr.enrollmentId}/cohorts`)
      cohortsByEnr[enr.enrollmentId] = { loading: false, cohorts: res.data.cohorts ?? [], error: '' }
    } catch (e) {
      cohortsByEnr[enr.enrollmentId] = {
        loading: false, cohorts: [],
        error: e.response?.data?.error ?? e.message ?? 'Failed to load cohorts',
      }
    }
  }
}
watch(tab, t => { if (t === 'cohorts') loadCohorts() })

// Left-menu selection on the Module Cohorts tab. Falls back to the first
// assigned cohort until the student picks one.
const selectedCohortId = ref(null)
const allCohorts = computed(() => {
  const out = []
  for (const enr of data.value?.enrollments ?? [])
    for (const c of cohortsByEnr[enr.enrollmentId]?.cohorts ?? []) out.push({ enr, c })
  return out
})
const cohortsLoading = computed(() =>
  (data.value?.enrollments ?? []).some(e => cohortsByEnr[e.enrollmentId]?.loading))
const selectedCohort = computed(() =>
  allCohorts.value.find(x => x.c.moduleCohortId === selectedCohortId.value)
  ?? allCohorts.value[0]
  ?? null)

// Notes tab: notes the school explicitly shared with the student.
const notesState = reactive({ loading: false, loaded: false, error: '', notes: [] })
async function loadNotes() {
  if (notesState.loaded || notesState.loading) return
  notesState.loading = true
  notesState.error = ''
  try {
    const res = await api.get('/v1/student/me/log-notes')
    notesState.notes = res.data.notes ?? []
    notesState.loaded = true
  } catch (e) {
    notesState.error = e.response?.data?.error ?? e.message ?? 'Failed to load notes'
  } finally {
    notesState.loading = false
  }
}
watch(tab, t => { if (t === 'notes') loadNotes() })

// Cohort questionnaire fill modal. On submit the cohort + module caches are
// cleared so the grade gate refreshes immediately.
const fillQ = reactive({ open: false, id: null, name: '', questionnaire: null, answers: {}, busy: false, error: '' })

async function openQuestionnaire(q) {
  fillQ.open = true
  fillQ.id = q.id
  fillQ.name = q.name
  fillQ.questionnaire = null
  fillQ.answers = {}
  fillQ.error = ''
  try {
    const res = await api.get(`/v1/student/me/cohort-questionnaires/${q.id}`)
    fillQ.name = res.data.name
    fillQ.questionnaire = JSON.parse(res.data.definitionJson)
  } catch (e) {
    fillQ.error = e.response?.data?.error ?? e.message ?? 'Failed to load the questionnaire'
  }
}

function onQChange(fieldId, value) {
  fillQ.answers[fieldId] = value
}

async function submitQuestionnaire() {
  if (fillQ.busy) return
  fillQ.busy = true
  fillQ.error = ''
  try {
    await api.post(`/v1/student/me/cohort-questionnaires/${fillQ.id}/submit`,
      { answersJson: JSON.stringify(fillQ.answers) })
    fillQ.open = false
    for (const k of Object.keys(cohortsByEnr)) delete cohortsByEnr[k]
    for (const k of Object.keys(modulesByEnr)) delete modulesByEnr[k]
    await loadCohorts()
    if (selectedEnr.value) await loadModules(selectedEnr.value.enrollmentId)
    showToast('Questionnaire submitted, thank you')
  } catch (e) {
    fillQ.error = e.response?.data?.error ?? e.message ?? 'Submit failed'
  } finally {
    fillQ.busy = false
  }
}

async function downloadCohortFile(f) {
  try {
    const res = await api.get(`/v1/student/me/cohort-files/${f.id}/file`, { responseType: 'blob' })
    const url = URL.createObjectURL(res.data)
    const a = document.createElement('a')
    a.href = url
    a.download = f.fileName ?? 'file'
    document.body.appendChild(a); a.click(); document.body.removeChild(a)
    setTimeout(() => URL.revokeObjectURL(url), 60_000)
  } catch (e) {
    showToast(e.response?.status === 404
      ? 'This file is not available.'
      : (e.response?.data?.error ?? e.message ?? 'Download failed'))
  }
}

function showToast(msg) {
  toast.value = msg
  setTimeout(() => { toast.value = '' }, 3000)
}

function logout() {
  auth.logout()
  router.push('/login')
}

onMounted(load)
</script>

<style scoped>
.student-portal { min-height: 100vh; background: #f0f4f8; font-family: sans-serif; }
.navbar { background: #003366; color: #fff; display: flex; align-items: center; justify-content: space-between; padding: 0.85rem 1.5rem; }
.navbar-brand { font-weight: 700; font-size: 1rem; }
.btn-logout { background: transparent; border: 1.5px solid rgba(255,255,255,.5); color: #fff; padding: 0.35rem 1rem; border-radius: 5px; cursor: pointer; font-size: 0.85rem; }
.btn-logout:hover { background: rgba(255,255,255,.15); }

.tab-bar { display: flex; gap: 0.25rem; background: #fff; border-bottom: 1px solid #e2e8f0; padding: 0 1.5rem; overflow-x: auto; }
.tab-btn {
  position: relative; background: none; border: none; border-bottom: 2.5px solid transparent;
  padding: 0.75rem 1rem; font-size: 0.88rem; font-weight: 600; color: #5f6e85; cursor: pointer; white-space: nowrap;
}
.tab-btn:hover { color: #003366; }
.tab-btn.active { color: #003366; border-bottom-color: #003366; }
.tab-dot {
  display: inline-block; width: 8px; height: 8px; border-radius: 50%;
  background: #dc2626; margin-left: 0.35rem; vertical-align: middle;
}

.tab-content { padding: 1.5rem 2rem; max-width: 1200px; margin: 0 auto; display: flex; flex-direction: column; gap: 0.85rem; }
.err-banner { background: #fef2f2; border: 1.5px solid #fca5a5; color: #b91c1c; padding: 0.65rem 1rem; border-radius: 7px; font-size: 0.86rem; }
.loading { color: #888; font-style: italic; padding: 2rem; text-align: center; }
.empty { color: #555; background: #fff; border-radius: 10px; padding: 1.4rem; text-align: center; box-shadow: 0 1px 4px rgba(0,0,0,.08); }

.panel { background: #fff; border-radius: 8px; padding: 0.9rem 1.1rem; box-shadow: 0 1px 4px rgba(0,0,0,.06); border: 1px solid #e8edf4; }
.panel-title { margin: 0 0 0.5rem; font-size: 0.92rem; color: #0a264f; }
.profile-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 0.85rem; }

.enr-card { background: #fff; border-radius: 8px; padding: 0.9rem 1.1rem; box-shadow: 0 1px 4px rgba(0,0,0,.06); border: 1px solid #e8edf4; }
.enr-head { display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 0.5rem; margin-bottom: 0.5rem; }
.enr-head strong { font-size: 0.95rem; color: #0a264f; }
.badge-code { background: #e8f0f8; color: #003366; border-radius: 4px; padding: 1px 7px; font-size: 0.72rem; font-weight: 700; margin-left: 0.45rem; }
.badge-specialization { background: #f0f3f7; color: #555; border-radius: 4px; padding: 1px 7px; font-size: 0.72rem; margin-left: 0.35rem; }
.badge-status { font-size: 0.7rem; font-weight: 700; padding: 3px 10px; border-radius: 12px; text-transform: uppercase; letter-spacing: 0.02em; }

.tone-grey  { background: #f0f3f7; color: #555; }
.tone-amber { background: #fff3cd; color: #856404; }
.tone-blue  { background: #cfe2ff; color: #084298; }
.tone-green { background: #d1fae5; color: #065f46; }
.tone-red   { background: #fee2e2; color: #b91c1c; }

.action-banner { padding: 0.55rem 0.85rem; border-radius: 7px; margin: 0.45rem 0 0.65rem; display: flex; align-items: center; gap: 0.75rem; flex-wrap: wrap; font-size: 0.85rem; }
.action-banner-title { font-weight: 700; font-size: 0.86rem; }
.action-banner-meta { font-size: 0.78rem; opacity: 0.8; }
.action-bad   { background: #fef2f2; border: 1.5px solid #fca5a5; color: #b91c1c; }
.action-blue  { background: #eef5ff; border: 1.5px solid #b6d4fe; color: #084298; }
.action-info  { background: #eef5ff; border: 1.5px solid #b6d4fe; color: #084298; font-size: 0.86rem; }
.action-banner .btn-primary { margin-left: auto; }

.summary { display: grid; grid-template-columns: repeat(auto-fill, minmax(140px, 1fr)); gap: 0.4rem 1rem; margin: 0.3rem 0 0.7rem; padding: 0; }
.summary div { display: flex; flex-direction: column; }
.summary dt { font-size: 0.68rem; text-transform: uppercase; letter-spacing: 0.04em; color: #888; margin: 0; }
.summary dd { font-size: 0.85rem; color: #222; margin: 0.1rem 0 0; }

.section-h { margin: 0.9rem 0 0.35rem; font-size: 0.88rem; color: #003366; }

.mod-table { width: 100%; border-collapse: collapse; font-size: 0.83rem; }
.mod-table th { text-align: left; font-size: 0.7rem; text-transform: uppercase; letter-spacing: 0.04em; color: #888; font-weight: 600; padding: 0.3rem 0.5rem; border-bottom: 1px solid #e8edf4; }
.mod-table td { padding: 0.4rem 0.5rem; border-bottom: 1px solid #f0f3f7; color: #222; }
.mod-table tr:last-child td { border-bottom: none; }
.mod-code { font-weight: 700; color: #003366; white-space: nowrap; }

/* Programs layout */
.programs-layout { display: grid; grid-template-columns: 250px 1fr; gap: 0.85rem; align-items: start; }
.prog-menu { display: flex; flex-direction: column; gap: 0.45rem; position: sticky; top: 1rem; }
.prog-item {
  display: flex; flex-direction: column; align-items: flex-start; gap: 0.25rem;
  background: #fff; border: 1px solid #e8edf4; border-radius: 8px;
  padding: 0.65rem 0.8rem; cursor: pointer; text-align: left;
  font-family: inherit; box-shadow: 0 1px 4px rgba(0,0,0,.04);
}
.prog-item:hover { border-color: #b6d4fe; }
.prog-item.active { border-color: #003366; box-shadow: 0 0 0 1.5px #003366 inset; }
.prog-item strong { font-size: 0.85rem; color: #0a264f; }
.prog-item-sub { font-size: 0.74rem; color: #667; }
.prog-detail { min-width: 0; }
.menu-caption { font-size: 0.68rem; text-transform: uppercase; letter-spacing: 0.05em; color: #888; font-weight: 700; margin: 0.4rem 0 0.05rem; }
@media (max-width: 720px) { .programs-layout { grid-template-columns: 1fr; } .prog-menu { position: static; } }

/* Cohort cards */
.cohort-grid { display: grid; grid-template-columns: 1fr; gap: 0.7rem; }
.cohort-card { border: 1px solid #e2e8f0; border-radius: 8px; padding: 0.7rem 0.85rem; background: #fafcff; }
.cohort-head { display: flex; justify-content: space-between; align-items: baseline; gap: 0.5rem; margin-bottom: 0.35rem; }
.cohort-head strong { font-size: 0.87rem; color: #0a264f; }
.cohort-number { font-size: 0.72rem; color: #667; font-weight: 700; }
.grade-badge { background: #d1fae5; color: #065f46; border-radius: 4px; padding: 1px 8px; font-weight: 700; font-size: 0.8rem; }
.cohort-files-h { font-size: 0.76rem; font-weight: 700; color: #003366; margin: 0.4rem 0 0.15rem; }

.doc-list { list-style: none; padding: 0; margin: 0; }
.doc-row { display: flex; justify-content: space-between; align-items: flex-start; padding: 0.5rem 0; border-bottom: 1px solid #f0f3f7; gap: 1rem; }
.doc-row:last-child { border-bottom: none; }
.doc-info { display: flex; gap: 0.5rem; align-items: flex-start; flex: 1; }
.doc-mark { width: 20px; height: 20px; display: inline-flex; align-items: center; justify-content: center; border-radius: 50%; font-weight: 700; flex-shrink: 0; font-size: 0.85rem; }
.mark-ok { background: #d1fae5; color: #065f46; }
.mark-bad { background: #fee2e2; color: #b91c1c; }
.mark-pending { background: #f0f3f7; color: #aaa; }
.doc-text { display: flex; flex-direction: column; gap: 0.15rem; }
.doc-text strong { font-size: 0.85rem; color: #222; }
.doc-meta { color: #888; font-size: 0.75rem; margin: 0; }
.doc-pill { display: inline-block; font-size: 0.68rem; font-weight: 600; padding: 1px 7px; border-radius: 4px; margin-top: 0.15rem; align-self: flex-start; }

.doc-actions { display: flex; gap: 0.4rem; align-items: center; }
.btn-upload { background: #003366; color: #fff; padding: 0.28rem 0.75rem; border-radius: 5px; font-size: 0.78rem; font-weight: 600; cursor: pointer; }
.btn-upload input { display: none; }
.lock-note { color: #888; font-size: 0.75rem; font-style: italic; }

.reject-card { background: #fff7f7; border: 1px solid #fbcaca; border-left: 3px solid #b91c1c; border-radius: 6px; padding: 0.6rem 0.8rem; margin: 0.5rem 0 0; }
.reject-card-head { display: flex; gap: 0.6rem; align-items: baseline; flex-wrap: wrap; font-size: 0.82rem; color: #7f1d1d; }
.reject-card-head strong { color: #b91c1c; }
.reject-card-date { margin-left: auto; color: #999; font-size: 0.74rem; }
.reject-chips { margin-top: 0.4rem; display: flex; flex-wrap: wrap; gap: 0.3rem; }
.reject-chip { background: #fee2e2; color: #b91c1c; border-radius: 12px; font-size: 0.72rem; padding: 1px 9px; }
.reject-free { margin: 0.45rem 0 0; color: #555; font-size: 0.83rem; white-space: pre-wrap; }

.actions { margin-top: 0.85rem; padding-top: 0.7rem; border-top: 1px solid #f0f3f7; display: flex; align-items: center; gap: 0.85rem; flex-wrap: wrap; }
.btn-primary { background: #16a34a; color: #fff; border: none; padding: 0.45rem 1.1rem; border-radius: 6px; font-weight: 700; font-size: 0.84rem; cursor: pointer; }
.btn-primary:hover:not(:disabled) { background: #15803d; }
.btn-primary:disabled { background: #aaa; cursor: not-allowed; }
.action-hint { color: #888; font-size: 0.8rem; }

.doc-strip { display: grid; grid-template-columns: repeat(2, 1fr); gap: 0.5rem; margin-top: 0.35rem; }
.doc-mini { display: flex; align-items: center; gap: 0.45rem; padding: 0.4rem 0.6rem; border: 1px solid #e2e8f0; border-radius: 6px; background: #fafcff; }
.doc-mini.disabled { opacity: 0.55; }
.doc-mini-icon { font-size: 1.05rem; }
.doc-mini-info { flex: 1; min-width: 0; }
.doc-mini-name { font-size: 0.78rem; font-weight: 700; color: #003366; }
.doc-mini-sub { font-size: 0.68rem; color: #888; }
.btn-mini { background: #003366; color: #fff; border: none; padding: 0.22rem 0.6rem; border-radius: 5px; font-size: 0.72rem; cursor: pointer; }
.btn-mini:disabled { background: #bbb; cursor: not-allowed; }

.toast { position: fixed; bottom: 2rem; right: 2rem; background: #003366; color: #fff; padding: 0.75rem 1.4rem; border-radius: 8px; font-size: 0.9rem; box-shadow: 0 4px 16px rgba(0,0,0,.2); z-index: 9999; }

.notes-list { display: flex; flex-direction: column; gap: 0.6rem; }
.note-card { border: 1px solid #e8edf4; border-left: 3px solid #003366; border-radius: 6px; padding: 0.55rem 0.8rem; background: #fafcff; }
.note-head { display: flex; align-items: baseline; gap: 0.5rem; flex-wrap: wrap; }
.note-head strong { color: #0a264f; font-size: 0.88rem; }
.note-date { margin-left: auto; color: #94a3b8; font-size: 0.74rem; }
.note-content { margin: 0.3rem 0 0; font-size: 0.85rem; color: #333; white-space: pre-wrap; }

.q-backdrop { position: fixed; inset: 0; background: rgba(10, 38, 79, 0.45); z-index: 1000; }
.q-modal {
  position: fixed; top: 4vh; left: 50%; transform: translateX(-50%);
  width: min(760px, 94vw); max-height: 90vh; display: flex; flex-direction: column;
  background: #fff; border-radius: 10px; box-shadow: 0 12px 40px rgba(0,0,0,.25); z-index: 1001;
}
.q-modal-head { display: flex; justify-content: space-between; align-items: center; padding: 0.8rem 1.1rem; border-bottom: 1px solid #e8edf4; }
.q-modal-head h3 { margin: 0; font-size: 1rem; color: #0a264f; }
.q-modal-body { padding: 0.6rem 1.1rem 1rem; overflow-y: auto; }
.q-vapp { background: transparent !important; }
.q-vapp :deep(.v-application__wrap) { min-height: 0; }

.extra-docs { margin-top: 0.85rem; padding-top: 0.7rem; border-top: 1px dashed #d8dee6; }
.extra-docs-head { display: flex; align-items: center; justify-content: space-between; gap: 0.65rem; margin-bottom: 0.4rem; }
.extra-docs-head strong { font-size: 0.85rem; color: #1a2d4f; }
.btn-secondary { background: #fff; color: #003366; border: 1px solid #003366; padding: 0.25rem 0.7rem; border-radius: 5px; font-size: 0.76rem; font-weight: 600; cursor: pointer; }
.btn-secondary:hover { background: #f0f5fa; }
.muted-extra { color: #999; font-size: 0.78rem; font-style: italic; margin: 0.2rem 0 0; }
.doc-hint { color: #888; font-size: 0.78rem; }
</style>
