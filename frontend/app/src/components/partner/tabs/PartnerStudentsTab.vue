<template>
  <div class="ps-tab">
    <div v-if="loadError" class="err-banner">{{ loadError }}</div>

    <!-- Status filter chips -->
    <div class="status-row">
      <button v-for="s in STATUS_FILTERS" :key="s.id ?? 'all'"
              :class="['status-chip', { active: filterStatusId === s.id }]"
              @click="filterStatusId = s.id">
        {{ s.label }}
        <span v-if="s.id !== 'action-required'" class="chip-count">{{ countFor(s.id) }}</span>
      </button>
    </div>

    <!-- Search + secondary filters -->
    <div class="filter-row">
      <input v-model="search" class="inp" placeholder="Fuzzy search — name, email, programme, student #…" />
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
      <thead><tr><th>Student #</th><th>Name</th><th>Email</th><th>Enrolments</th><th>Status</th><th></th></tr></thead>
      <tbody>
        <tr v-for="s in pagedRows" :key="s.studentId" class="data-row" @click="openStudentDetail(s)">
          <td class="mono">{{ s.studentNumber }}</td>
          <td>
            <a class="s-name-link" @click.stop="openStudentDetail(s)">
              {{ s.firstName ?? '—' }} {{ s.lastName ?? '' }}
            </a>
            <br><small class="muted">@{{ s.username }}</small>
          </td>
          <td>{{ s.email ?? '—' }}<span v-if="!s.emailVerified" class="s-badge unverified">unverified</span></td>
          <td>
            <div v-for="e in s.enrollments" :key="e.studentEnrollmentId" class="enrol-line">
              <span class="enr-prog">{{ e.programmeCode }}</span> · {{ e.specializationName }}
            </div>
          </td>
          <td class="td-status-col">
            <div v-for="e in s.enrollments" :key="e.studentEnrollmentId" class="status-line">
              <span :class="['flow-badge', 'fb-' + flowState(s, e).color]"
                    :title="flowState(s, e).reason || flowState(s, e).label">
                {{ flowState(s, e).label }}
              </span>
              <button v-if="!auth.user?.isTeacher" class="btn-review-app btn-review-app-sm"
                      :disabled="!canPartnerReview(e)"
                      :title="canPartnerReview(e) ? '' : 'Awaiting student — they must resubmit before this can be reviewed again.'"
                      @click.stop="canPartnerReview(e) && openReviewFromRow(s.studentId, e.studentEnrollmentId)">
                Review
              </button>
              <button v-if="flowState(s, e).kind === 'admitted-grading'"
                      class="btn-grade" @click.stop="openGrades(s, e)">
                🎓 Grade
              </button>
              <button v-if="!auth.user?.isTeacher && (flowState(s, e).kind === 'active' || flowState(s, e).kind === 'active-sub')"
                      class="btn-manage-sub" @click.stop="openManage(s, e)">
                {{ flowState(s, e).kind === 'active-sub' ? 'Change' : 'Manage' }}
              </button>
              <button class="btn-row-details btn-row-details-sm" @click.stop="openStudentDetail(s, e.studentEnrollmentId)">
                Details
              </button>
            </div>
          </td>
          <td class="td-actions">
            <button v-if="!auth.user?.isTeacher" class="btn-link" @click.stop="openDetail(s.studentId)">Edit →</button>
          </td>
        </tr>
      </tbody>
    </table>

    <div v-if="pageCount > 1" class="pgn-bar">
      <button class="pgn-btn" :disabled="page === 1" @click="page = 1">«</button>
      <button class="pgn-btn" :disabled="page === 1" @click="page--">‹ Prev</button>
      <span class="pgn-info">Page {{ page }} / {{ pageCount }} · {{ filtered.length }} students</span>
      <button class="pgn-btn" :disabled="page === pageCount" @click="page++">Next ›</button>
      <button class="pgn-btn" :disabled="page === pageCount" @click="page = pageCount">»</button>
      <select v-model.number="pageSize" class="pgn-size">
        <option :value="25">25 / page</option>
        <option :value="50">50 / page</option>
        <option :value="100">100 / page</option>
        <option :value="250">250 / page</option>
      </select>
    </div>

    <!-- Detail drawer -->
    <template v-if="detail">
      <div class="overlay" @click="detail = null"></div>
      <div class="drawer">
        <div class="drawer-head">
          <div>
            <h2>{{ detail.account.firstName }} {{ detail.account.lastName }}</h2>
            <p class="sub">
              Student #{{ detail.studentNumber }} · {{ detail.account.email }}
              <template v-if="!detail.account.emailVerified">
                <span class="s-badge unverified">unverified</span>
                <button class="btn-confirm-email"
                        :disabled="confirmingEmail"
                        @click="confirmEmailOnBehalf">
                  {{ confirmingEmail ? 'Confirming…' : 'Confirm on behalf of student' }}
                </button>
              </template>
              <button class="btn-confirm-email"
                      :disabled="resettingStudentPw"
                      @click="resetStudentPassword">
                {{ resettingStudentPw ? 'Resetting…' : '🔑 Reset password' }}
              </button>
            </p>
            <p v-if="resetStudentPwValue" class="sub reset-pw-reveal-row">
              <strong>New password:</strong> <code>{{ resetStudentPwValue }}</code>
              <button class="btn-confirm-email" @click="copyResetStudentPw">Copy</button>
              <span class="muted">Save this — it won't be shown again.</span>
            </p>
            <p v-for="e in awaitingOfferAcceptance" :key="e.studentEnrollmentId" class="sub">
              <button class="btn-confirm-email"
                      :disabled="acceptingOfferId === e.studentEnrollmentId"
                      @click="acceptOfferOnBehalf(e)">
                {{ acceptingOfferId === e.studentEnrollmentId
                    ? 'Accepting…'
                    : `Accept ${e.programmeCode || 'offer'} on behalf of student` }}
              </button>
            </p>
          </div>
          <button class="drawer-close" @click="detail = null">✕</button>
        </div>

        <div class="drawer-body">
          <p v-if="detailError" class="err-banner">{{ detailError }}</p>
          <div v-if="isAdmitted" class="lock-banner">
            🔒 This student has been admitted to at least one programme.
            Personal and background details are read-only — only MGW can change them now.
          </div>

          <!-- Personal -->
          <details class="section" open>
            <summary>Personal</summary>
            <div class="row-2">
              <div class="field"><label>First name</label><input v-model="detail.account.firstName" /></div>
              <div class="field"><label>Last name</label><input v-model="detail.account.lastName" /></div>
            </div>
            <div class="row-2">
              <div class="field"><label>Date of birth</label><input type="date" v-model="dobInput" /></div>
              <div class="field"><label>Passport / ID</label><input v-model="detail.personal.passportId" /></div>
            </div>
            <div class="field">
              <label>Student card ID <span style="font-weight:400; color:#8a97a8">(overrides the ID on the student card only)</span></label>
              <input v-model="detail.personal.studentCardId" />
            </div>
            <div class="field">
              <label>Nationality</label>
              <select v-model.number="detail.personal.nationalityId">
                <option :value="null">—</option>
                <option v-for="n in nationalities" :key="n.nationalityId" :value="n.nationalityId">{{ n.name }}</option>
              </select>
            </div>
            <div class="field"><label>Address line 1</label><input v-model="detail.personal.address.line1" /></div>
            <div class="field"><label>Address line 2</label><input v-model="detail.personal.address.line2" /></div>
            <div class="row-3">
              <div class="field"><label>City</label><input v-model="detail.personal.address.city" /></div>
              <div class="field"><label>State / Region</label><input v-model="detail.personal.address.stateRegion" /></div>
              <div class="field"><label>Postal code</label><input v-model="detail.personal.address.postalCode" /></div>
            </div>
            <div class="field">
              <label>Country</label>
              <select v-model="detail.personal.address.countryCode">
                <option value="">—</option>
                <option v-for="n in nationalities" :key="n.code" :value="n.code">{{ n.name }}</option>
              </select>
            </div>
            <button class="btn-save" :disabled="saving || isAdmitted" @click="savePersonal">Save personal</button>
          </details>
        </div>
      </div>
    </template>

    <!-- Manage (sub-status) modal -->
    <transition name="fade">
      <div v-if="manageModal" class="manage-overlay" @click.self="manageModal = null">
        <div class="manage-modal">
          <div class="manage-hdr">
            <h3>Manage enrolment</h3>
            <button class="drawer-close" @click="manageModal = null">✕</button>
          </div>
          <p class="manage-sub">{{ manageModal.studentName }} · {{ manageModal.enrollmentLabel }}</p>

          <div class="manage-body manage-activity">
            <EnrollmentActivityLog :api-path="`/v1/partner/my-students/${manageModal.studentId}/enrollments/${manageModal.enrollmentId}/activity`" />
          </div>

          <div v-if="manageModal.step === 'choose'" class="manage-body">
            <p class="muted manage-hint">Select an action to apply to this enrolment.</p>
            <div class="manage-grid">
              <button v-for="opt in SUB_STATUS_OPTIONS" :key="opt.type"
                      :class="['manage-option', 'mo-' + opt.color]"
                      @click="pickSubStatus(opt)">
                <span class="manage-opt-label">{{ opt.label }}</span>
                <span v-if="!opt.requiresReason" class="manage-opt-sub">no reason required</span>
                <span v-else class="manage-opt-sub">reason required</span>
              </button>
            </div>
          </div>

          <div v-else-if="manageModal.step === 'grades'" class="manage-body">
            <div v-if="manageModal.rejection" class="grade-reject-banner">
              <div class="grade-reject-title">Admission sent these grades back — please update.</div>
              <div class="grade-reject-meta">
                Returned by {{ manageModal.rejection.byName || 'Admission Office' }} on {{ formatDate(manageModal.rejection.atUtc) }}
              </div>
              <pre class="grade-reject-note">{{ manageModal.rejection.reason }}</pre>
            </div>
            <p class="muted manage-hint">
              Enter a score (0–100) for each subject. <strong>Save</strong> keeps your grades as a draft (you can
              keep editing and download a provisional transcript). <strong>Program Complete</strong> sends the
              grades to Admission for approval.
            </p>
            <div class="grade-statusbar">
              <span>Current status: <strong>{{ manageModal.statusName || '—' }}</strong></span>
              <span>Final grade (avg): <strong>{{ gradeAverage ?? '—' }}</strong></span>
            </div>
            <p v-if="manageModal.requiredEcts" class="ects-line"
               :class="gradeEctsRemaining > 0 ? 'ects-warn' : 'ects-ok'">
              Completed {{ completedGradeEcts }} of {{ manageModal.requiredEcts }} required ECTS.
              <span v-if="gradeEctsRemaining > 0">Need {{ gradeEctsRemaining }} more to mark the programme complete.</span>
              <span v-else>Threshold reached — you can mark the programme complete.</span>
            </p>
            <div v-if="manageModal.gradesError" class="err-banner">{{ manageModal.gradesError }}</div>
            <div v-if="manageModal.gradesOk" class="ok-banner">{{ manageModal.gradesOk }}</div>
            <div v-if="manageModal.gradesLoading" class="muted">Loading subjects…</div>
            <div v-else-if="manageModal.subjects?.length" class="grade-grid"
                 :style="{ columnCount: gradeColumnCount(manageModal.subjects.length) }">
              <div v-for="row in manageModal.subjects" :key="row.subjectId" class="grade-row">
                <span class="gr-code mono">{{ row.code }}</span>
                <span class="gr-name">{{ row.name }}</span>
                <span class="gr-ects">{{ row.ects }} ects</span>
                <RubricGradeCell v-if="row.rubric" :row="row" :editable="true" />
                <input v-else type="number" min="0" max="100" v-model.number="row.score" class="grade-input gr-input" />
                <span class="gr-letter" :title="`School grade for ${row.score ?? '—'}`">{{ scoreToLetter(row.score) }}</span>
              </div>
            </div>
            <div v-else class="muted">No subjects defined for this specialization.</div>

            <!-- Thesis/dissertation project title — shown once the thesis module has a grade. -->
            <div v-if="thesisGraded" class="project-title-row">
              <label>Project title <span class="muted" style="font-weight:400;">(thesis/dissertation — shown on the transcript)</span></label>
              <input v-model="manageModal.projectTitle" class="grade-input" style="width:100%;" placeholder="e.g. The impact of …" />
            </div>

            <div class="manage-footer">
              <button class="btn-link" @click="manageModal.step = 'choose'">← Back</button>
              <button class="btn-link" :disabled="manageModal.downloadingProvisional" @click="downloadProvisional">
                {{ manageModal.downloadingProvisional ? 'Preparing…' : '⤓ Provisional transcript' }}
              </button>
              <button class="btn-save" :disabled="manageModal.savingDraft" @click="saveGradesDraft">
                {{ manageModal.savingDraft ? 'Saving…' : 'Save grades' }}
              </button>
              <button v-if="!auth.user?.isTeacher" class="btn-confirm-manage"
                      :disabled="!canCommitGrades || manageModal.gradesSubmitting"
                      :title="gradeEctsRemaining > 0 ? `Need ${gradeEctsRemaining} more ECTS to reach the ${manageModal.requiredEcts} ECTS completion threshold.` : ''"
                      @click="commitGrades">
                {{ manageModal.gradesSubmitting ? 'Submitting…' : 'Program Complete' }}
              </button>
            </div>
          </div>

          <div v-else class="manage-body">
            <div class="manage-selected">
              <span :class="['flow-badge', 'fb-' + manageModal.selectedColor]">{{ manageModal.selectedLabel }}</span>
            </div>
            <label class="manage-label">Reason (required, min 10 characters)</label>
            <textarea v-model="manageModal.reasonText" rows="4" placeholder="Explain why this status is being applied…"></textarea>
            <div class="manage-footer">
              <button class="btn-link" @click="manageModal.step = 'choose'">← Back</button>
              <button class="btn-confirm-manage"
                      :disabled="manageModal.reasonText.trim().length < 10"
                      @click="confirmSubStatus">Apply {{ manageModal.selectedLabel }}</button>
            </div>
          </div>
        </div>
      </div>
    </transition>

    <!-- Quick-view student detail modal: 3 tabs (Details / Letters / Activity) -->
    <transition name="fade">
      <div v-if="detailModal" class="manage-overlay" @click.self="detailModal = null">
        <div class="manage-modal detail-modal">
          <div class="manage-hdr">
            <div>
              <h3>{{ detailModal.name || '—' }}
                <span class="muted-sub">· {{ detailModal.studentNumber }}</span>
              </h3>
              <p class="manage-sub">{{ detailModal.email || '—' }}</p>
            </div>
            <button class="drawer-close" @click="detailModal = null">✕</button>
          </div>

          <p v-if="detailModal.error" class="err-banner">{{ detailModal.error }}</p>
          <p v-if="detailModal.loading" class="muted detail-loading">Loading…</p>

          <template v-else-if="detailModal.data">
            <div v-if="detailEnrollments.length > 1" class="enr-switch">
              <label>Enrolment:</label>
              <select v-model="detailModal.activeEnrollmentId">
                <option v-for="e in detailEnrollments" :key="e.studentEnrollmentId" :value="e.studentEnrollmentId">
                  {{ e.programmeCode }} · {{ e.specializationName }} ({{ e.statusName }})
                </option>
              </select>
            </div>

            <div class="detail-tabs">
              <button v-for="t in visibleDetailTabs" :key="t.id"
                      :class="['tab-btn', { active: detailModal.activeTab === t.id }]"
                      @click="detailModal.activeTab = t.id">{{ t.label }}</button>
            </div>

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
                </div>
                <div class="detail-section">
                  <h4>Personal</h4>
                  <dl>
                    <dt>Date of birth</dt><dd>{{ formatDateD(detailModal.data.personal?.dateOfBirth) || '—' }}</dd>
                    <dt>Passport / ID</dt><dd>{{ detailModal.data.personal?.passportId || '—' }}</dd>
                    <dt>Student card ID</dt><dd>{{ detailModal.data.personal?.studentCardId || '—' }}</dd>
                    <dt>Address</dt><dd>{{ formatAddressD(detailModal.data.personal?.address) || '—' }}</dd>
                  </dl>
                </div>
                <div class="detail-section">
                  <h4>Background</h4>
                  <dl>
                    <dt>Highest degree</dt><dd>{{ detailModal.data.background?.highestDegree || '—' }}</dd>
                    <dt>Specialization for degree</dt><dd>{{ detailModal.data.background?.degreeSpecialization || '—' }}</dd>
                    <dt>Years exp.</dt><dd>{{ detailModal.data.background?.yearsWorkExperience ?? '—' }}</dd>
                  </dl>
                </div>
                <div class="detail-section" v-if="activeEnrollment">
                  <h4>Enrolment</h4>
                  <dl>
                    <dt>Programme</dt><dd>{{ activeEnrollment.programmeName }}</dd>
                    <dt>Specialisation</dt><dd>{{ activeEnrollment.specializationName }}</dd>
                    <dt>Study language</dt><dd>{{ activeEnrollment.instructionLanguage || '—' }}</dd>
                    <dt>Mode</dt><dd>{{ activeEnrollment.modeOfStudyName ?? '—' }}</dd>
                    <dt>Commencement</dt><dd>{{ formatDateD(activeEnrollment.commencementDate) || '—' }}</dd>
                    <dt>Duration</dt><dd>{{ activeEnrollment.durationOfStudyMonths ?? '—' }} months</dd>
                    <dt>Status</dt><dd>{{ activeEnrollment.statusName }}</dd>
                  </dl>
                </div>
              </div>
            </div>

            <div v-if="detailModal.activeTab === 'documents'" class="tab-pane">
              <div v-for="enr in docsByEnrollment" :key="enr.enrollmentId" class="docs-group">
                <div class="docs-group-head">
                  <strong>{{ enr.programmeCode }}</strong> · {{ enr.specializationName }}
                  <span class="docs-group-count">{{ enr.coreDocs.length + enr.additionalDocs.length }}</span>
                  <button v-if="!auth.user?.isTeacher" class="btn-mini-d" style="margin-left:auto"
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
                        {{ d.fileName }} · uploaded {{ formatDateD(d.uploadedAt) }} · {{ d.statusName }}
                      </div>
                    </div>
                    <button class="btn-mini-d" @click="downloadStudentDocPartner(d)">Open</button>
                    <label class="btn-mini-d" v-if="!d.isVerified && !auth.user?.isTeacher" style="margin-left:6px">
                      Replace
                      <input type="file" :accept="ACCEPTED_DOC_ACCEPT_ATTR" hidden
                             @change="onPartnerReplace($event, enr.enrollmentId, d)" />
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
                        {{ d.fileName }} · uploaded {{ formatDateD(d.uploadedAt) }} · {{ d.statusName }}
                      </div>
                    </div>
                    <button class="btn-mini-d" @click="downloadStudentDocPartner(d)">Open</button>
                  </div>
                </div>
              </div>
              <p v-if="!docsByEnrollment.length" class="muted">No enrolments for this student yet.</p>
            </div>

            <AdditionalDocumentUploadDialog
              v-if="additionalDialog.open"
              types-endpoint="/v1/partner/document-types"
              :upload-endpoint="additionalDialog.uploadEndpoint"
              @close="additionalDialog.open = false"
              @uploaded="onAdditionalUploaded" />

            <div v-if="detailModal.activeTab === 'letters'" class="tab-pane">
              <p v-if="!activeEnrollment" class="muted">No enrolment selected.</p>
              <div v-else class="letters-list">
                <template v-for="t in visibleLetterTypes" :key="t.key">
                  <div class="letter-row" :class="{ disabled: !activeEnrollment.letters?.[t.key] }">
                    <span class="letter-icon">{{ t.icon }}</span>
                    <div class="letter-info">
                      <div class="letter-name">{{ t.label }}</div>
                      <div class="letter-sub">
                        <template v-if="activeEnrollment.letters?.[t.key]">
                          {{ activeEnrollment.letters[t.key].fileName }} · released {{ formatDateD(activeEnrollment.letters[t.key].uploadedAt) }}
                        </template>
                        <template v-else>Not yet released</template>
                      </div>
                    </div>
                    <button class="btn-mini-d" :disabled="!activeEnrollment.letters?.[t.key]"
                            @click="downloadLetterPartner(activeEnrollment.letters?.[t.key])">Download</button>
                    <button v-if="!auth.user?.isTeacher && EMAILABLE_KEYS.includes(t.key)"
                            class="btn-mini-d btn-mini-email"
                            :disabled="!activeEnrollment.letters?.[t.key]"
                            @click="openSendEmail(t)">✉ Send</button>
                  </div>
                  <!-- Provisional transcript: rendered live from saved grades,
                       listed with the other letters — same as the admin portal. -->
                  <div v-if="t.key === 'transcript'" class="letter-row">
                    <span class="letter-icon">📑</span>
                    <div class="letter-info">
                      <div class="letter-name">Provisional Transcript</div>
                      <div class="letter-sub">Rendered live from the grades saved so far · watermarked until grades are submitted</div>
                    </div>
                    <button class="btn-mini-d" :disabled="downloadingLetterProvisional"
                            @click="downloadLetterProvisionalPartner()">
                      {{ downloadingLetterProvisional ? 'Preparing…' : 'Download' }}
                    </button>
                  </div>
                </template>
              </div>

              <!-- Ad-hoc send dialog — same flow as the admin portal -->
              <div v-if="emailSend.open" class="email-send-pop">
                <div class="email-send-card">
                  <div class="esp-head">
                    <strong>Send {{ emailSend.label }} email</strong>
                    <button class="esp-close" @click="emailSend.open = false">✕</button>
                  </div>
                  <p class="muted" style="font-size:.78rem;">
                    Sends the saved email template (PDF attached) to the student plus the template's
                    enabled CC/BCC. Add one-off addresses below (comma-separated) if needed.
                  </p>
                  <label class="esp-label">Extra CC</label>
                  <input v-model="emailSend.cc" type="text" placeholder="a@x.com, b@y.com" />
                  <label class="esp-label">Extra BCC</label>
                  <input v-model="emailSend.bcc" type="text" placeholder="c@z.com" />
                  <div v-if="emailSend.error" class="err-banner" style="margin-top:.4rem;">{{ emailSend.error }}</div>
                  <div v-if="emailSend.ok" class="ok-banner" style="margin-top:.4rem;">{{ emailSend.ok }}</div>
                  <div class="esp-actions">
                    <button class="btn-mini-d btn-mini-ghost-d" @click="emailSend.open = false">Cancel</button>
                    <button class="btn-mini-d btn-mini-email" :disabled="emailSend.sending" @click="sendLetterEmail">
                      {{ emailSend.sending ? 'Sending…' : 'Send now' }}
                    </button>
                  </div>
                </div>
              </div>
            </div>

            <!-- Payment (read-only): what's due / paid + invoice downloads -->
            <div v-if="detailModal.activeTab === 'payment'" class="tab-pane">
              <p v-if="!activeEnrollment" class="muted">No enrolment selected.</p>
              <template v-else>
                <p v-if="partnerPay.error" class="err-banner">{{ partnerPay.error }}</p>
                <p v-if="partnerPay.loading" class="muted">Loading…</p>
                <template v-else-if="partnerPay.exists">
                  <table class="pp-table">
                    <thead><tr><th>#</th><th>Amount</th><th>Due date</th><th>Status</th><th></th></tr></thead>
                    <tbody>
                      <tr v-for="i in partnerPay.installments" :key="'i' + i.sequence">
                        <td>{{ i.sequence }}</td>
                        <td>{{ partnerPay.currency }} {{ fmtMoneyP(i.amount) }}</td>
                        <td>{{ formatDateD(i.dueDate) || '—' }}</td>
                        <td>
                          <span :class="['pp-pill', i.isPaid ? 'pp-paid' : 'pp-unpaid']">
                            {{ i.isPaid ? `Paid${i.paidDate ? ' · ' + formatDateD(i.paidDate) : ''}` : 'Unpaid' }}
                          </span>
                        </td>
                        <td><button class="btn-mini-d" :disabled="partnerPay.downloading" @click="downloadPartnerInvoice(i.sequence, null)">⤓ Invoice</button></td>
                      </tr>
                      <tr v-for="a in partnerPay.additionalInvoices" :key="'a' + a.sequence">
                        <td>A{{ a.sequence }}</td>
                        <td>{{ partnerPay.currency }} {{ fmtMoneyP(a.total) }} <span class="muted" style="font-size:.74rem;">{{ a.title }}</span></td>
                        <td>{{ formatDateD(a.dueDate) || '—' }}</td>
                        <td>
                          <span :class="['pp-pill', a.isPaid ? 'pp-paid' : 'pp-unpaid']">
                            {{ a.isPaid ? `Paid${a.paidDate ? ' · ' + formatDateD(a.paidDate) : ''}` : 'Unpaid' }}
                          </span>
                        </td>
                        <td><button class="btn-mini-d" :disabled="partnerPay.downloading" @click="downloadPartnerInvoice(null, a.sequence)">⤓ Invoice</button></td>
                      </tr>
                    </tbody>
                  </table>
                  <div class="pp-summary">
                    <span>Total tuition: <strong>{{ partnerPay.currency }} {{ fmtMoneyP(partnerPay.totalTuitionFee) }}</strong></span>
                    <span v-if="partnerPay.additionalTotal > 0">Additional invoices: <strong>{{ partnerPay.currency }} {{ fmtMoneyP(partnerPay.additionalTotal) }}</strong></span>
                    <span>Total paid: <strong>{{ partnerPay.currency }} {{ fmtMoneyP(partnerPay.totalPaid) }}</strong></span>
                    <span :class="partnerPay.balance > 0 ? 'pp-due' : 'pp-ok'">Balance due: <strong>{{ partnerPay.currency }} {{ fmtMoneyP(partnerPay.balance) }}</strong></span>
                  </div>
                  <button class="btn-mini-d" style="margin-top:.5rem" :disabled="partnerPay.downloading" @click="downloadPartnerInvoice(null, null)">
                    {{ partnerPay.downloading ? 'Preparing…' : '⤓ Download full invoice' }}
                  </button>
                  <p class="muted" style="font-size:.74rem; margin-top:.4rem;">View only — payment amounts and paid status are managed by the Admission Office.</p>
                </template>
                <p v-else class="muted">No payment plan has been set up for this enrolment yet.</p>
              </template>
            </div>

            <!-- Moodle (view-only): username + password behind a reveal toggle -->
            <div v-if="detailModal.activeTab === 'moodle'" class="tab-pane">
              <p v-if="partnerMoodle.error" class="err-banner">{{ partnerMoodle.error }}</p>
              <p v-if="partnerMoodle.loading" class="muted">Loading…</p>
              <template v-else>
                <div class="detail-section">
                  <h4>Moodle (LMS) login</h4>
                  <dl>
                    <dt>Status</dt><dd>{{ partnerMoodle.enabled ? 'Enabled' : 'Not enabled' }}</dd>
                    <dt>Username</dt><dd class="mono">{{ partnerMoodle.username || '—' }}</dd>
                    <dt>Password</dt>
                    <dd class="mono">
                      <template v-if="partnerMoodle.password">
                        {{ partnerMoodle.reveal ? partnerMoodle.password : '••••••••••' }}
                        <button class="btn-mini-d" style="margin-left:.5rem" @click="partnerMoodle.reveal = !partnerMoodle.reveal">
                          {{ partnerMoodle.reveal ? '🙈 Hide' : '👁 Show' }}
                        </button>
                      </template>
                      <template v-else>—</template>
                    </dd>
                  </dl>
                  <p class="muted" style="font-size:.74rem;">View only — Moodle credentials are managed by the Admission Office.</p>
                </div>
              </template>
            </div>

            <div v-if="detailModal.activeTab === 'log'" class="tab-pane">
              <StudentLogNotesPanel mode="partner"
                :api-root="`/v1/partner/my-students/${detailModal.studentId}`"
                :enrollments="detailEnrollments" />
            </div>

            <div v-if="detailModal.activeTab === 'activity'" class="tab-pane">
              <p v-if="!activeEnrollment" class="muted">No enrolment selected.</p>
              <EnrollmentActivityLog v-else
                :api-path="`/v1/partner/my-students/${detailModal.studentId}/enrollments/${activeEnrollment.studentEnrollmentId}/activity`"
                :default-open="true" />
            </div>
          </template>
        </div>
      </div>
    </transition>

    <!-- Student Review Wizard -->
    <StudentReviewWizard v-if="reviewingStudent" :student="reviewingStudent"
      @close="closeReview" @submitted="onReviewSubmitted" />

    <transition name="fade">
      <div v-if="reviewToast" class="review-toast">{{ reviewToast }}</div>
    </transition>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted, watch } from 'vue'
import RubricGradeCell from '../../admin/RubricGradeCell.vue'
import Fuse from 'fuse.js'
import api from '../../../api/client.js'
import { auth } from '../../../store/auth.js'
import StudentReviewWizard from '../StudentReviewWizard.vue'
import StudentLogNotesPanel from '../../admin/StudentLogNotesPanel.vue'
import EnrollmentActivityLog from '../../letters/EnrollmentActivityLog.vue'
import AdditionalDocumentUploadDialog from '../../letters/AdditionalDocumentUploadDialog.vue'
import { ACCEPTED_DOC_ACCEPT_ATTR } from '../../../utils/uploadPolicy.js'
import {
  partnerReviewState,
  getSubStatus,
  setSubStatus,
  subStatusLabel,
  SUB_STATUS_OPTIONS,
} from '../../../store/partnerReviewState.js'

// One chip per distinct workflow stage. Default landing is "Action required"
// (partner's queue). "All" sits at the end. Statuses that don't require
// partner action are also represented so the partner can browse any state.
const STATUS_FILTERS = [
  { id: 'action-required',           label: 'Action required',             codes: ['ApplicationSubmitted', 'ApplicationAwaitingReviewByPartner', 'AwaitingGradesSubmit'] },
  { id: 'submitted',                 label: 'Submitted',                   codes: ['ApplicationSubmitted', 'ApplicationAwaitingReviewByPartner'] },
  { id: 'rejected-awaiting-student', label: 'Rejected — Awaiting Student', codes: ['ApplicationRejectedByPartner', 'ApplicationRejectedByAdmission'] },
  { id: 'pending-admission',         label: 'Pending Admission Approval',  codes: ['ApplicationAwaitingReviewByAdmission'] },
  { id: 'applying',                  label: 'Applying (draft)',            codes: ['Draft'], includeNoEnrolment: true },
  { id: 'awaiting-student-accept',   label: 'Awaiting Student Acceptance', codes: ['AcceptOffer'] },
  { id: 'admitted',                  label: 'Admitted',                    codes: ['ApplicationApprovedAdmission', 'AcceptAdmission'] },
  { id: 'admitted-grading',          label: 'Admitted — Grading',          codes: ['AwaitingGradesSubmit'] },
  { id: 'awaiting-grades-approval',  label: 'Awaiting Grades Approval',    codes: ['AwaitingGradesApproval'] },
  { id: 'graduated',                 label: 'Graduated',                   codes: ['GradesApproved'] },
  { id: 'deferred',                  label: 'Deferred',                    codes: ['Deferred'] },
  { id: 'dropped-out',               label: 'Dropped Out',                 codes: ['DroppedOut'] },
  { id: '',                          label: 'All',                         codes: null },
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

const list = ref([])
const loading = ref(false)
const loadError = ref('')

const search = ref('')
const filterStatusId = ref('action-required')
const filterProgrammeId = ref('')
const filterSpecializationId = ref('')

const detail = ref(null)
const saving = ref(false)
const detailError = ref('')
const confirmingEmail = ref(false)
const acceptingOfferId = ref(null)
const resettingStudentPw = ref(false)
const resetStudentPwValue = ref('')

async function resetStudentPassword() {
  if (!detail.value || resettingStudentPw.value) return
  const studentName = `${detail.value.account.firstName ?? ''} ${detail.value.account.lastName ?? ''}`.trim() || 'this student'
  const entered = prompt(`Reset password for ${studentName}\n\nEnter a custom password (or leave blank for an auto-generated one):`, '')
  if (entered === null) return
  resettingStudentPw.value = true
  resetStudentPwValue.value = ''
  try {
    const body = entered.trim() ? { password: entered.trim() } : {}
    const res = await api.post(`/v1/partner/my-students/${detail.value.studentId}/reset-password`, body)
    resetStudentPwValue.value = res.data.temporaryPassword
  } catch (e) {
    detailError.value = e.response?.data?.error ?? e.message ?? 'Failed to reset password'
  } finally {
    resettingStudentPw.value = false
  }
}
function copyResetStudentPw() {
  navigator.clipboard.writeText(resetStudentPwValue.value).catch(() => {})
}

// Partner manually confirms a student's email — useful when the student
// has lost the verification email or signed up in person. Skips the OPAQUE
// login-init's EmailConfirmed gate so they can sign in immediately.
async function confirmEmailOnBehalf() {
  if (!detail.value || confirmingEmail.value) return
  confirmingEmail.value = true
  detailError.value = ''
  try {
    await api.post(`/v1/partner/my-students/${detail.value.studentId}/confirm-email`)
    detail.value.account.emailVerified = true
    await load()
  } catch (e) {
    detailError.value = e.response?.data?.error ?? e.message ?? 'Failed to confirm email'
  } finally {
    confirmingEmail.value = false
  }
}

// Enrolments currently waiting on the student's offer acceptance. The
// partner can click to accept on their behalf; the backend writes a note
// "Partner accepted offer on behalf of student." so the audit log shows
// who actually did it.
const awaitingOfferAcceptance = computed(() =>
  (detail.value?.enrollments ?? []).filter(e => e.statusCode === 'AcceptOffer'))

async function acceptOfferOnBehalf(enr) {
  if (!detail.value || acceptingOfferId.value) return
  acceptingOfferId.value = enr.studentEnrollmentId
  detailError.value = ''
  try {
    await api.post(`/v1/partner/my-students/${detail.value.studentId}/enrollments/${enr.studentEnrollmentId}/accept-offer-on-behalf`)
    // Reload both list (chip counts shift) and detail (enrolment status flips).
    await load()
    await openDetail(detail.value.studentId)
  } catch (e) {
    detailError.value = e.response?.data?.error ?? e.message ?? 'Failed to accept offer'
  } finally {
    acceptingOfferId.value = null
  }
}

const dobInput = computed({
  get: () => detail.value?.personal.dateOfBirth?.slice(0, 10) ?? '',
  set: v => { if (detail.value) detail.value.personal.dateOfBirth = v || null },
})

const nationalities = ref([])
const languages = ref([])

// Statuses where the partner cannot edit the enrolment from the drawer:
// admission has it (ApplicationAwaitingReviewByAdmission) or the student
// has accepted the offer and is now in Admission's hands (AcceptOffer +
// downstream codes that flow from there).
const LOCKED_STATUS_CODES = new Set([
  'ApplicationAwaitingReviewByAdmission',
  'AcceptOffer',
])
function enrollmentLocked(e) { return LOCKED_STATUS_CODES.has(e.statusCode) }
const allEnrollmentsLocked = computed(() =>
  (detail.value?.enrollments ?? []).length > 0
  && detail.value.enrollments.every(e => LOCKED_STATUS_CODES.has(e.statusCode))
)

// "Admitted" gate for the Personal/Background sections: once any
// enrolment has reached ApplicationApprovedAdmission (Level 300) or
// later, IBSS owns the identity record and the partner cannot edit
// it. Independent of the per-enrolment edit lock above.
const isAdmitted = computed(() =>
  (detail.value?.enrollments ?? []).some(e => (e.statusLevel ?? 0) >= 300)
)

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

// Fuzzy search across every field — name, email, programme, specialization.
const fuse = computed(() => new Fuse(list.value, {
  keys: [
    { name: 'studentNumber', weight: 0.9 },
    { name: 'firstName',     weight: 0.8 },
    { name: 'lastName',      weight: 0.8 },
    { name: 'username',      weight: 0.6 },
    { name: 'email',         weight: 0.6 },
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

// ── Pagination (client-side, over the filtered rows) ─────────────────────────
const page = ref(1)
const pageSize = ref(50)
const pageCount = computed(() => Math.max(1, Math.ceil(filtered.value.length / pageSize.value)))
const pagedRows = computed(() =>
  filtered.value.slice((page.value - 1) * pageSize.value, page.value * pageSize.value))
watch([search, filterProgrammeId, filterSpecializationId, filterStatusId, pageSize],
  () => { page.value = 1 })
watch(pageCount, n => { if (page.value > n) page.value = n })

async function load() {
  loading.value = true
  loadError.value = ''
  try {
    const params = {}
    if (filterProgrammeId.value) params.programmeId = filterProgrammeId.value
    if (filterSpecializationId.value) params.specializationId = filterSpecializationId.value
    const res = await api.get('/v1/partner/my-students', { params })
    list.value = res.data.items ?? []
  } catch (e) {
    loadError.value = e.response?.data?.error ?? e.message ?? 'Failed to load'
  } finally {
    loading.value = false
  }
}

// Quick-view detail modal (3 tabs: Details / Letters / Activity log).
// Sits alongside the existing big edit drawer (openDetail) — row click +
// blue name + "Details" button now go here for read-only browsing; the
// "Edit →" button still opens the editable drawer for deeper changes.
const DETAIL_TABS = [
  { id: 'details',     label: 'Details' },
  { id: 'documents',   label: 'Documents' },
  { id: 'letters',     label: 'Letters' },
  { id: 'payment',     label: 'Payment' },
  { id: 'moodle',      label: 'Moodle' },
  { id: 'log',         label: 'Log' },
  { id: 'activity',    label: 'Activity log' },
]
// The note log is partner STAFF only: teacher logins don't get the tab.
const visibleDetailTabs = computed(() =>
  auth.user?.isTeacher ? DETAIL_TABS.filter(t => t.id !== 'log') : DETAIL_TABS)
// Printable Cert (provisionalCertificate) is intentionally omitted: only the
// Admission Office may download the printable certificate version.
const LETTER_TYPES = [
  { key: 'offerLetter',            label: 'Offer Letter',        icon: '📄' },
  { key: 'admissionLetter',        label: 'Admission Letter',    icon: '📋' },
  { key: 'transcript',             label: 'Digital Transcript',  icon: '📑' },
  { key: 'certificate',            label: 'Digital Certificate', icon: '🎓' },
  // Only programmes with the digital-card toggle issue one; the row is
  // hidden (rather than "Not yet released") when no card exists.
  { key: 'studentIdCard',          label: 'Student ID Card',     icon: '🪪', onlyWhenIssued: true },
]
const visibleLetterTypes = computed(() => LETTER_TYPES.filter(t =>
  !t.onlyWhenIssued || !!activeEnrollment.value?.letters?.[t.key]))
const detailModal = ref(null)
const detailEnrollments = computed(() => detailModal.value?.data?.enrollments ?? [])

// ── Payment tab (read-only view of the Admission-Office plan) ────────────────
const partnerPay = reactive({
  exists: false, loading: false, error: '', downloading: false,
  totalTuitionFee: 0, currency: 'USD', installments: [], additionalInvoices: [],
  additionalTotal: 0, totalPaid: 0, balance: 0,
})
function fmtMoneyP(v) { return (Number(v) || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }

async function loadPartnerPayment() {
  const m = detailModal.value
  const enr = activeEnrollment.value
  if (!m || !enr) return
  partnerPay.loading = true; partnerPay.error = ''
  try {
    const res = await api.get(`/v1/partner/my-students/${m.studentId}/enrollments/${enr.studentEnrollmentId}/payment`)
    const d = res.data
    partnerPay.exists = !!d.exists
    partnerPay.totalTuitionFee = d.totalTuitionFee ?? 0
    partnerPay.currency = d.currency ?? 'USD'
    partnerPay.installments = d.installments ?? []
    partnerPay.additionalInvoices = d.additionalInvoices ?? []
    partnerPay.additionalTotal = d.additionalTotal ?? 0
    partnerPay.totalPaid = d.totalPaid ?? 0
    partnerPay.balance = d.balance ?? 0
  } catch (e) {
    partnerPay.error = e.response?.data?.error ?? e.message ?? 'Failed to load payment plan'
  } finally {
    partnerPay.loading = false
  }
}

async function downloadPartnerInvoice(seq, additionalSeq) {
  const m = detailModal.value
  const enr = activeEnrollment.value
  if (!m || !enr || partnerPay.downloading) return
  partnerPay.downloading = true; partnerPay.error = ''
  try {
    const q = seq ? `?installment=${seq}` : additionalSeq ? `?additional=${additionalSeq}` : ''
    const res = await api.get(
      `/v1/partner/my-students/${m.studentId}/enrollments/${enr.studentEnrollmentId}/payment/invoice${q}`,
      { responseType: 'blob' })
    const url = URL.createObjectURL(res.data)
    const a = document.createElement('a')
    a.href = url; a.download = seq ? `invoice-installment-${seq}.pdf` : additionalSeq ? `invoice-additional-${additionalSeq}.pdf` : 'invoice.pdf'
    a.target = '_blank'
    document.body.appendChild(a); a.click(); document.body.removeChild(a)
    setTimeout(() => URL.revokeObjectURL(url), 60_000)
  } catch (e) {
    partnerPay.error = e.response?.status === 404 ? 'No invoice available yet.' : 'Download failed'
  } finally {
    partnerPay.downloading = false
  }
}

// ── Moodle tab (view-only, password behind a reveal toggle) ──────────────────
const partnerMoodle = reactive({ loading: false, error: '', enabled: false, username: '', password: '', reveal: false })

async function loadPartnerMoodle() {
  const m = detailModal.value
  if (!m) return
  partnerMoodle.loading = true; partnerMoodle.error = ''; partnerMoodle.reveal = false
  try {
    const res = await api.get(`/v1/partner/my-students/${m.studentId}/moodle`)
    partnerMoodle.enabled = !!res.data.moodleEnabled
    partnerMoodle.username = res.data.moodleUsername ?? ''
    partnerMoodle.password = res.data.moodlePassword ?? ''
  } catch (e) {
    partnerMoodle.error = e.response?.data?.error ?? e.message ?? 'Failed to load Moodle login'
  } finally {
    partnerMoodle.loading = false
  }
}

watch(() => [detailModal.value?.activeTab, detailModal.value?.activeEnrollmentId], ([tab]) => {
  if (tab === 'payment') loadPartnerPayment()
  if (tab === 'moodle') loadPartnerMoodle()
})
const activeEnrollment = computed(() =>
  detailEnrollments.value.find(e => e.studentEnrollmentId === detailModal.value?.activeEnrollmentId)
  ?? detailEnrollments.value[0]
  ?? null
)

// Opens the quick-view modal. Optional `preselectEnrollmentId` lets the
// caller open the modal directly on a specific enrolment — used by the
// per-enrolment "Details" buttons on the row so a student with multiple
// programmes lands on the right one without an extra dropdown click.
async function openStudentDetail(s, preselectEnrollmentId = null) {
  detailModal.value = reactive({
    studentId: s.studentId,
    studentNumber: s.studentNumber,
    name: `${s.firstName ?? ''} ${s.lastName ?? ''}`.trim() || '—',
    email: s.email,
    activeTab: 'details',
    activeEnrollmentId: preselectEnrollmentId ?? s.enrollments?.[0]?.studentEnrollmentId ?? null,
    data: null,
    loading: true,
    error: '',
  })
  try {
    const res = await api.get(`/v1/partner/my-students/${s.studentId}`)
    detailModal.value.data = res.data
    if (!detailModal.value.activeEnrollmentId && res.data.enrollments?.length) {
      detailModal.value.activeEnrollmentId = res.data.enrollments[0].studentEnrollmentId
    }
  } catch (err) {
    detailModal.value.error = err.response?.data?.error ?? err.message ?? 'Failed to load student'
  } finally {
    detailModal.value.loading = false
  }
}

// Documents tab support: groups uploaded docs by the enrolment they were
// attached to so the partner sees which docs went on which application,
// partitioned into core slot docs vs additional supplementary docs based
// on the server-supplied `isAdditional` flag.
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
    `/v1/partner/my-students/${studentId}/enrollments/${enrollmentId}/documents`
  additionalDialog.open = true
}
async function onAdditionalUploaded() {
  await refreshPartnerDetailModal()
}
async function onPartnerReplace(ev, enrollmentId, doc) {
  const file = ev.target.files?.[0]
  ev.target.value = ''
  if (!file || !detailModal.value?.studentId) return
  try {
    const body = new FormData()
    body.append('documentTypeId', doc.documentTypeId)
    body.append('isAdditional', 'false')
    body.append('file', file)
    await api.post(
      `/v1/partner/my-students/${detailModal.value.studentId}/enrollments/${enrollmentId}/documents`,
      body)
    await refreshPartnerDetailModal()
  } catch (err) {
    detailModal.value.error = err.response?.data?.error
      ?? err.message ?? 'Replace failed.'
    setTimeout(() => { if (detailModal.value) detailModal.value.error = '' }, 3000)
  }
}
async function refreshPartnerDetailModal() {
  if (!detailModal.value?.studentId) return
  try {
    const res = await api.get(`/v1/partner/my-students/${detailModal.value.studentId}`)
    detailModal.value.data = res.data
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
async function downloadStudentDocPartner(d) {
  if (!d?.studentDocumentId || !detailModal.value) return
  try {
    const res = await api.get(
      `/v1/partner/my-students/${detailModal.value.studentId}/documents/${d.studentDocumentId}/file`,
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

// Provisional transcript from the partner Letters tab (before release).
const downloadingLetterProvisional = ref(false)
async function downloadLetterProvisionalPartner() {
  if (!detailModal.value?.studentId || !activeEnrollment.value || downloadingLetterProvisional.value) return
  downloadingLetterProvisional.value = true
  try {
    const res = await api.get(
      `/v1/partner/my-students/${detailModal.value.studentId}/enrollments/${activeEnrollment.value.studentEnrollmentId}/transcript/provisional`,
      { responseType: 'blob' })
    const url = URL.createObjectURL(res.data)
    const a = document.createElement('a')
    a.href = url; a.download = 'provisional-transcript.pdf'; a.target = '_blank'
    document.body.appendChild(a); a.click(); document.body.removeChild(a)
    setTimeout(() => URL.revokeObjectURL(url), 60_000)
  } catch (err) {
    reviewToast.value = err.response?.status === 404
      ? 'No published transcript template yet, or no grades saved.'
      : (err.response?.data?.error ?? err.message ?? 'Download failed')
    setTimeout(() => { reviewToast.value = '' }, 3500)
  } finally {
    downloadingLetterProvisional.value = false
  }
}

async function downloadLetterPartner(letter) {
  if (!letter?.studentDocumentId || !detailModal.value) return
  try {
    const res = await api.get(
      `/v1/partner/my-students/${detailModal.value.studentId}/documents/${letter.studentDocumentId}/file`,
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

// Manual (re-)send of the offer/admission email — same dialog as the admin
// portal, hitting the partner-scoped endpoint. Hidden for teachers.
const EMAILABLE_KEYS = ['offerLetter', 'admissionLetter']
const emailSend = ref({ open: false, key: '', label: '', cc: '', bcc: '', sending: false, error: '', ok: '' })

function openSendEmail(t) {
  emailSend.value = { open: true, key: t.key, label: t.label, cc: '', bcc: '', sending: false, error: '', ok: '' }
}

function splitEmails(s) {
  return (s || '').split(/[,;\s]+/).map(x => x.trim()).filter(Boolean)
}

async function sendLetterEmail() {
  if (!detailModal.value?.studentId || !activeEnrollment.value) return
  const es = emailSend.value
  es.sending = true; es.error = ''; es.ok = ''
  try {
    const enumName = es.key.charAt(0).toUpperCase() + es.key.slice(1)
    const res = await api.post(
      `/v1/partner/my-students/${detailModal.value.studentId}/enrollments/${activeEnrollment.value.studentEnrollmentId}/letters/${enumName}/send-email`,
      { ccAdHoc: splitEmails(es.cc), bccAdHoc: splitEmails(es.bcc) })
    const to = res.data?.to ?? 'student'
    es.ok = `Sent to ${to}${(res.data?.cc?.length ? ` (cc ${res.data.cc.length})` : '')}.`
    setTimeout(() => { emailSend.value.open = false }, 2000)
  } catch (err) {
    es.error = err.response?.data?.error
      ?? (err.response?.data?.outcome ? `Not sent: ${err.response.data.outcome}` : null)
      ?? err.message ?? 'Send failed'
  } finally {
    es.sending = false
  }
}

function formatDateD(iso) {
  if (!iso) return ''
  return new Date(iso).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })
}
function formatAddressD(addr) {
  if (!addr) return ''
  const parts = [addr.line1, addr.city, addr.stateRegion, addr.postalCode, addr.countryCode]
    .filter(s => !!(s && s.trim?.() !== ''))
  return parts.join(', ')
}

async function openDetail(studentId) {
  detail.value = null
  detailError.value = ''
  resetStudentPwValue.value = '' // don't bleed across students
  try {
    const [s, langs, nats] = await Promise.all([
      api.get(`/v1/partner/my-students/${studentId}`),
      api.get('/v1/public/languages'),
      api.get('/v1/public/nationalities'),
    ])
    languages.value = langs.data.items ?? []
    nationalities.value = nats.data.items ?? []
    const d = s.data
    // shallow inputs for date pickers per enrollment
    d.enrollments = d.enrollments.map(e => ({
      ...e,
      commencementDateInput: e.commencementDate?.slice(0, 10) ?? '',
    }))
    detail.value = d
  } catch (e) {
    loadError.value = e.response?.data?.error ?? e.message ?? 'Failed to load student'
  }
}

async function savePersonal() {
  saving.value = true; detailError.value = ''
  try {
    await api.patch(`/v1/partner/my-students/${detail.value.studentId}/personal`, {
      firstName: detail.value.account.firstName,
      lastName: detail.value.account.lastName,
      dateOfBirth: detail.value.personal.dateOfBirth,
      passportId: detail.value.personal.passportId,
      studentCardId: detail.value.personal.studentCardId,
      nationalityId: detail.value.personal.nationalityId,
      addressLine1: detail.value.personal.address.line1,
      addressLine2: detail.value.personal.address.line2,
      city: detail.value.personal.address.city,
      stateRegion: detail.value.personal.address.stateRegion,
      postalCode: detail.value.personal.address.postalCode,
      countryCode: detail.value.personal.address.countryCode,
    })
    await load()
  } catch (e) { detailError.value = e.response?.data?.error ?? e.message }
  finally { saving.value = false }
}

async function saveBackground() {
  saving.value = true; detailError.value = ''
  try {
    await api.patch(`/v1/partner/my-students/${detail.value.studentId}/background`, {
      highestDegree: detail.value.background.highestDegree,
      degreeSpecialization: detail.value.background.degreeSpecialization ?? null,
      yearsWorkExperience: detail.value.background.yearsWorkExperience,
      languages: detail.value.background.languages
        .filter(l => l.languageId > 0)
        .map(l => ({ languageId: l.languageId, proficiency: l.proficiency })),
    })
  } catch (e) { detailError.value = e.response?.data?.error ?? e.message }
  finally { saving.value = false }
}

async function saveEnrollments() {
  saving.value = true; detailError.value = ''
  try {
    await api.put(`/v1/partner/my-students/${detail.value.studentId}/enrollments`, {
      items: detail.value.enrollments.map(e => ({
        studentEnrollmentId: e.studentEnrollmentId,
        programmeId: e.programmeId,
        specializationId: e.specializationId,
        pathwayId: e.pathwayId ?? null,
        modeOfStudyId: e.modeOfStudyId,
        commencementDate: e.commencementDateInput || null,
        durationOfStudyMonths: e.durationOfStudyMonths || null,
      })),
    })
    await load()
    await openDetail(detail.value.studentId)
  } catch (e) { detailError.value = e.response?.data?.error ?? e.message }
  finally { saving.value = false }
}

async function toggleVerify(doc, verified) {
  saving.value = true; detailError.value = ''
  try {
    await api.post(`/v1/partner/my-students/${detail.value.studentId}/documents/${doc.studentDocumentId}/verify`,
      { verified })
    doc.isVerified = verified
  } catch (e) { detailError.value = e.response?.data?.error ?? e.message; doc.isVerified = !verified }
  finally { saving.value = false }
}

async function markReady(enrollmentId) {
  saving.value = true; detailError.value = ''
  try {
    await api.post(`/v1/partner/my-students/${detail.value.studentId}/enrollments/${enrollmentId}/mark-ready`)
    await load()
    await openDetail(detail.value.studentId)
  } catch (e) { detailError.value = e.response?.data?.error ?? e.message }
  finally { saving.value = false }
}

async function unmarkReady(enrollmentId) {
  saving.value = true; detailError.value = ''
  try {
    await api.post(`/v1/partner/my-students/${detail.value.studentId}/enrollments/${enrollmentId}/unmark-ready`)
    await load()
    await openDetail(detail.value.studentId)
  } catch (e) { detailError.value = e.response?.data?.error ?? e.message }
  finally { saving.value = false }
}

function formatDate(iso) {
  if (!iso) return ''
  return new Date(iso).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })
}

// filterStatusId is purely a client-side filter now — no refetch needed.
watch([filterProgrammeId, filterSpecializationId], () => load())
onMounted(load)

// ── Review Application wizard ───────────────────────────────────────────────
const reviewingStudent = ref(null)
const reviewToast = ref('')

function findDocBy(d, typeRegex) {
  if (!d?.documents) return null
  return d.documents.find(doc => typeRegex.test(doc.documentTypeName ?? '')) ?? null
}
function findDoc(d, typeRegex) { return findDocBy(d, typeRegex)?.fileName ?? null }
function findDocId(d, typeRegex) { return findDocBy(d, typeRegex)?.studentDocumentId ?? null }
function docVerified(d, typeRegex) { return !!findDocBy(d, typeRegex)?.isVerified }
function docMeta(d, typeRegex) {
  const m = findDocBy(d, typeRegex)
  if (!m) return null
  return {
    status: m.status ?? null,
    statusName: m.statusName ?? null,
    lastChangedByName: m.lastChangedByName ?? null,
    lastChangeReason: m.lastChangeReason ?? null,
    requirements: m.requirements ?? [],
  }
}

/// Same shape as docMeta(), but if no matching document exists OR the
/// matched doc carries no requirements, falls back to the slot-canonical
/// requirements list that the detail endpoint emits in `slotRequirements`.
function withSlotFallback(d, typeRegex, slot) {
  const meta = docMeta(d, typeRegex) ?? {
    status: null, statusName: null,
    lastChangedByName: null, lastChangeReason: null,
    requirements: [],
  }
  if ((meta.requirements ?? []).length === 0) {
    meta.requirements = d?.slotRequirements?.[slot] ?? []
  }
  return meta
}

function adaptForWizard(d, targetEnrollmentId = null) {
  if (!d) return null
  // Each StudentDocument is per-enrolment — the detail endpoint returns
  // the union for all of the student's enrolments. Scope to the targeted
  // one so the regex slot resolvers (findDocId / findDocBy) don't match
  // a sibling application's row by accident.
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
    studentCardId: d.personal?.studentCardId ?? '',
    address: addressStr,
    highestDegree: d.background?.highestDegree ?? '',
    degreeSpecialization: d.background?.degreeSpecialization ?? '',
    languageResult: langSummary,
    yearsWorkExperience: d.background?.yearsWorkExperience ?? 0,
    docPassport: findDoc(d, /passport|identity|id\b/i),
    docDegree:   findDoc(d, /degree|diploma|transcript/i),
    docLanguage: findDoc(d, /language|ielts|toefl|english/i),
    docCV:       findDoc(d, /curriculum|\bcv\b|r[eé]sum[eé]/i),
    docIds: {
      passport: findDocId(d, /passport|identity|id\b/i),
      degree:   findDocId(d, /degree|diploma|transcript/i),
      language: findDocId(d, /language|ielts|toefl|english/i),
      cv:       findDocId(d, /curriculum|\bcv\b|r[eé]sum[eé]/i),
    },
    docMeta: {
      // Each slot's `requirements` falls back to the backend's
      // `slotRequirements` (canonical per-slot list) when the student
      // hasn't uploaded a document for this slot — otherwise the
      // reviewer would see "No verification requirements configured".
      passport: withSlotFallback(d, /passport|identity|id\b/i, 'passport'),
      degree:   withSlotFallback(d, /degree|diploma|transcript|high school/i, 'degree'),
      language: withSlotFallback(d, /language|ielts|toefl|english/i, 'language'),
      cv:       withSlotFallback(d, /curriculum|\bcv\b|r[eé]sum[eé]/i, 'cv'),
    },
    docsVerified: {
      passport: docVerified(d, /passport|identity|id\b/i),
      degree:   docVerified(d, /degree|diploma|transcript/i),
      language: docVerified(d, /language|ielts|toefl|english/i),
      cv:       docVerified(d, /curriculum|\bcv\b|r[eé]sum[eé]/i),
    },
    partnerReview: {
      passport:  { status: 'pending', reason: '' },
      degree:    { status: 'pending', reason: '' },
      language:  { status: 'pending', reason: '' },
      cv:        { status: 'pending', reason: '' },
      programme: { status: 'pending', reason: '' },
      completedAt: null,
      partnerName: '',
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
        durationMonths: e.approvedDurationMonths ?? e.durationOfStudyMonths ?? null,
        programmeMinDurationMonths: e.programmeMinDurationMonths ?? null,
        programmeMaxDurationMonths: e.programmeMaxDurationMonths ?? null,
        tuitionFeeUsd: Number(e.tuitionFeeUsd ?? 0),
        paymentPlan: null,
      })),
  })
}

async function openReviewFromRow(studentId, enrollmentId = null) {
  detailError.value = ''
  try {
    if (!languages.value.length || !nationalities.value.length) {
      const [langs, nats] = await Promise.all([
        api.get('/v1/public/languages'),
        api.get('/v1/public/nationalities'),
      ])
      languages.value = langs.data.items ?? []
      nationalities.value = nats.data.items ?? []
    }
    const res = await api.get(`/v1/partner/my-students/${studentId}`)
    reviewingStudent.value = adaptForWizard(res.data, enrollmentId)
  } catch (e) {
    loadError.value = e.response?.data?.error ?? e.message ?? 'Failed to load student'
  }
}
function closeReview() { reviewingStudent.value = null }
async function onReviewSubmitted(s) {
  reviewToast.value = `Review submitted for ${s.firstName} ${s.lastName}`
  setTimeout(() => { reviewToast.value = '' }, 3200)
  // Pick up the new enrolment status (e.g. ApplicationRejectedByPartner /
  // ApplicationAwaitingReviewByAdmission) so the table chip flips to red /
  // amber instead of the stale "Pending Admission Approval" pill.
  await load()
}

// ── Status flow ─────────────────────────────────────────────────────────────
const SUB_COLOR = {
  grade: 'blue', cancel: 'gray', dismissed: 'red',
  dropout: 'red', deferred: 'amber', transferred: 'purple',
}
// Partner can review only when the enrolment is currently in their queue
// (Submitted = bucket 13 / Awaiting partner review). Anything else means
// it's either with the student (Rejected, awaiting replace+resubmit) or
// past the partner stage (admission queue, accepted, etc.) — disable the
// Review button so a partner can't bypass the resubmission gate.
function canPartnerReview(e) {
  return e.statusCode === 'ApplicationSubmitted'
      || e.statusCode === 'ApplicationAwaitingReviewByPartner'
}

function flowState(s, e) {
  const reviewDone = partnerReviewState.completed.has(s.studentNumber)
  const sub = getSubStatus(s.studentNumber, e.studentEnrollmentId)
  switch (e.statusCode) {
    case 'Draft':
      return { kind: 'draft', label: 'Draft', color: 'gray' }
    case 'ApplicationSubmitted':
    case 'ApplicationAwaitingReviewByPartner':
      return reviewDone
        ? { kind: 'potential', label: '🟠 Potential Applicant', color: 'orange' }
        : { kind: 'applying',  label: '🟡 Applying', color: 'yellow' }
    case 'ApplicationRejectedByPartner':
    case 'ApplicationRejectedByAdmission':
      return { kind: 'rejected', label: '✕ Rejected — Awaiting Student', color: 'red' }
    case 'ApplicationAwaitingReviewByAdmission':
      return { kind: 'pending', label: '🔵 Pending Admission Approval', color: 'blue' }
    case 'AwaitingGradesSubmit':
      if (sub) return {
        kind: 'active-sub',
        label: `Active — ${subStatusLabel(sub.type)}`,
        color: SUB_COLOR[sub.type] ?? 'gray',
        reason: sub.reason,
      }
      return { kind: 'admitted-grading', label: '🟢 Admitted — Grading', color: 'green' }
    case 'AwaitingGradesApproval':
      return { kind: 'awaiting-grades-approval', label: '🟠 Awaiting Grades Approval', color: 'amber' }
    case 'GradesApproved':
      return { kind: 'graduated', label: '🎓 Graduated', color: 'blue' }
    case 'AcceptOffer':
      if (sub) return {
        kind: 'active-sub',
        label: `Active — ${subStatusLabel(sub.type)}`,
        color: SUB_COLOR[sub.type] ?? 'gray',
        reason: sub.reason,
      }
      return { kind: 'awaiting-student', label: '🟣 Awaiting Student Acceptance', color: 'purple' }
    case 'ApplicationApprovedAdmission':
    case 'AcceptAdmission':
      if (sub) return {
        kind: 'active-sub',
        label: `Active — ${subStatusLabel(sub.type)}`,
        color: SUB_COLOR[sub.type] ?? 'gray',
        reason: sub.reason,
      }
      return { kind: 'admitted', label: '🟢 Admitted — Awaiting Start', color: 'green' }
    default:
      return { kind: 'other', label: e.statusName ?? 'Unknown', color: 'gray' }
  }
}

// ── Manage (sub-status) modal ──────────────────────────────────────────────
const manageModal = ref(null)
function openManage(s, e) {
  manageModal.value = reactive({
    studentNumber: s.studentNumber,
    studentId: s.studentId,
    enrollmentId: e.studentEnrollmentId,
    studentName: `${s.firstName ?? ''} ${s.lastName ?? ''}`.trim(),
    enrollmentLabel: `${e.programmeCode} · ${e.specializationName}`,
    statusName: e.statusName ?? null,
    step: 'choose',
    selectedType: null,
    selectedLabel: '',
    selectedColor: 'gray',
    reasonText: '',
    subjects: [],
    requiredEcts: null,
    projectTitle: '',
    rejection: null,
    gradesLoading: false,
    gradesSubmitting: false,
    gradesError: '',
    gradesOk: '',
    savingDraft: false,
    downloadingProvisional: false,
  })
}
// Loads subject list (and any prior rejection note) into the grades step
// of the currently-open manage modal. Shared between the sub-status picker
// path (Manage → Grade option) and the direct openGrades(s, e) entry that
// the "🎓 Grade" button uses.
async function loadGradesIntoModal() {
  const m = manageModal.value
  if (!m) return
  m.step = 'grades'
  m.gradesLoading = true
  m.gradesError = ''
  try {
    const res = await api.get(
      `/v1/partner/my-students/${m.studentId}/enrollments/${m.enrollmentId}/subjects`)
    m.subjects = (res.data.items ?? []).map(withRubricScores)
    m.requiredEcts = res.data.requiredEcts ?? null
    m.projectTitle = res.data.projectTitle ?? ''
    m.rejection = res.data.rejection ?? null
  } catch (err) {
    m.gradesError = err.response?.data?.error ?? err.message ?? 'Failed to load subjects'
  } finally {
    m.gradesLoading = false
  }
}

function openGrades(s, e) {
  // Open the manage modal directly in the grades step — same shape as
  // openManage builds, just skipping the choose-step picker.
  openManage(s, e)
  loadGradesIntoModal()
}

async function pickSubStatus(opt) {
  if (opt.type === 'grade') {
    await loadGradesIntoModal()
    return
  }
  if (!opt.requiresReason) {
    setSubStatus(manageModal.value.studentNumber, manageModal.value.enrollmentId, opt.type, '')
    reviewToast.value = `Applied "${opt.label}" to enrolment`
    setTimeout(() => { reviewToast.value = '' }, 3000)
    manageModal.value = null
    return
  }
  manageModal.value.step = 'reason'
  manageModal.value.selectedType = opt.type
  manageModal.value.selectedLabel = opt.label
  manageModal.value.selectedColor = opt.color
}

// Splits the grade list into up to 3 CSS columns, capped at ~10 rows
// per column. ≤10 rows → single column; 11–20 → 2; 21+ → 3.
function gradeColumnCount(count) {
  if (!count || count <= 10) return 1
  return Math.min(3, Math.ceil(count / 10))
}
// Live 0–100 → MGW letter grade. Mirrors MapScore in LetterTagResolver.cs
// so the letter shown here matches the transcript. Display-only; nothing is
// saved until Save/Submit grades.
function scoreToLetter(score) {
  if (score === null || score === undefined || score === '') return '—'
  const s = Math.floor(Number(score))
  if (Number.isNaN(s) || s < 0 || s > 100) return '—'
  // Grade Standard scale — must match MapScore (UkGrade) in the backend so
  // the letter shown here equals the IBAS Grade on the transcript.
  if (s >= 75) return 'A+'
  if (s >= 70) return 'A'
  if (s >= 65) return 'A-'
  if (s >= 60) return 'B+'
  if (s >= 55) return 'B'
  if (s >= 50) return 'B-'
  if (s >= 45) return 'C+'
  if (s >= 41) return 'C'
  if (s === 40) return 'C-'
  return 'F'
}

// Rubric-graded modules send their per-criterion scores; the backend always
// calculates the final grade from them. Simple modules send the plain score.
function rubricAwareEntry(r) {
  if (r.rubric) {
    const complete = r.rubric.rows.every(rr => Number.isInteger(r.rubricScores?.[rr.id]))
    if (!complete) return null
    return { subjectId: r.subjectId, rubric: r.rubric.rows.map(rr => ({ rowId: rr.id, score: r.rubricScores[rr.id] })) }
  }
  return Number.isFinite(r.score) ? { subjectId: r.subjectId, score: r.score } : null
}
function withRubricScores(r) {
  return r.rubric
    ? { ...r, score: r.score ?? null, rubricScores: Object.fromEntries(r.rubric.rows.map(rr => [rr.id, rr.score])) }
    : { ...r, score: r.score ?? null }
}
// A subject counts as completed once any score (0-100) is entered.
function scoredGradeRows(m) {
  return (m?.subjects ?? []).filter(r => Number.isInteger(r.score) && r.score >= 0 && r.score <= 100)
}
const completedGradeEcts = computed(() =>
  scoredGradeRows(manageModal.value).reduce((sum, r) => sum + Number(r.ects || 0), 0))
// True once a thesis/dissertation module has a grade — reveals the project-title field.
const thesisGraded = computed(() => {
  const m = manageModal.value
  if (!m?.subjects?.length) return false
  return m.subjects.some(r => r.isThesis && Number.isInteger(r.score) && r.score >= 0 && r.score <= 100)
})
const gradeEctsRemaining = computed(() => {
  const req = Number(manageModal.value?.requiredEcts || 0)
  if (!req) return 0
  return Math.max(0, req - completedGradeEcts.value)
})
// Submit is gated by the programme's completion threshold (enough completed
// ECTS). With no threshold set, fall back to "at least one subject scored".
const canCommitGrades = computed(() => {
  const m = manageModal.value
  if (!m || m.step !== 'grades') return false
  const scored = scoredGradeRows(m)
  if (!scored.length) return false
  const req = Number(m?.requiredEcts || 0)
  return req ? completedGradeEcts.value >= req : true
})

// Read-only "Final grade" = average of the scores entered so far (any subset).
const gradeAverage = computed(() => {
  const m = manageModal.value
  if (!m?.subjects?.length) return null
  const scored = m.subjects.filter(r => Number.isFinite(r.score))
  if (!scored.length) return null
  return (scored.reduce((sum, r) => sum + r.score, 0) / scored.length).toFixed(1)
})

// Save grades as a draft (any subset). Status stays in grading; the partner
// can keep editing and download a provisional transcript.
async function saveGradesDraft() {
  const m = manageModal.value
  if (!m || m.savingDraft) return
  m.savingDraft = true; m.gradesError = ''; m.gradesOk = ''
  try {
    const items = m.subjects.map(rubricAwareEntry).filter(Boolean)
    await api.post(
      `/v1/partner/my-students/${m.studentId}/enrollments/${m.enrollmentId}/grades/draft`,
      { items, projectTitle: m.projectTitle })
    m.gradesOk = `Saved ${items.length} grade(s). You can download a provisional transcript.`
    setTimeout(() => { if (manageModal.value) manageModal.value.gradesOk = '' }, 3000)
  } catch (err) {
    m.gradesError = err.response?.data?.error ?? err.message ?? 'Failed to save grades'
  } finally {
    if (manageModal.value) manageModal.value.savingDraft = false
  }
}

// Download the provisional transcript. Auto-saves the on-screen grades
// first so the PDF always matches the editor — same flow as the admin side.
async function downloadProvisional() {
  const m = manageModal.value
  if (!m || m.downloadingProvisional) return
  await saveGradesDraft()
  if (m.gradesError) return
  m.downloadingProvisional = true; m.gradesError = ''
  try {
    const res = await api.get(
      `/v1/partner/my-students/${m.studentId}/enrollments/${m.enrollmentId}/transcript/provisional`,
      { responseType: 'blob' })
    const url = URL.createObjectURL(res.data)
    const a = document.createElement('a')
    a.href = url
    a.download = 'provisional-transcript.pdf'
    a.target = '_blank'
    document.body.appendChild(a); a.click(); document.body.removeChild(a)
    setTimeout(() => URL.revokeObjectURL(url), 60_000)
  } catch (err) {
    m.gradesError = err.response?.status === 404
      ? 'No published transcript template for this programme yet, or save grades first.'
      : (err.response?.data?.error ?? err.message ?? 'Download failed')
  } finally {
    if (manageModal.value) manageModal.value.downloadingProvisional = false
  }
}

async function commitGrades() {
  const m = manageModal.value
  if (!canCommitGrades.value || m.gradesSubmitting) return
  m.gradesSubmitting = true
  m.gradesError = ''
  try {
    await api.post(
      `/v1/partner/my-students/${m.studentId}/enrollments/${m.enrollmentId}/grades`,
      { items: scoredGradeRows(m).map(rubricAwareEntry).filter(Boolean), projectTitle: m.projectTitle })
    reviewToast.value = 'Grades submitted to Admission for approval.'
    setTimeout(() => { reviewToast.value = '' }, 3200)
    manageModal.value = null
    await load()
  } catch (err) {
    m.gradesError = err.response?.data?.error ?? err.message ?? 'Failed to submit grades'
  } finally {
    if (manageModal.value) manageModal.value.gradesSubmitting = false
  }
}
function confirmSubStatus() {
  const m = manageModal.value
  if (!m || m.reasonText.trim().length < 10) return
  setSubStatus(m.studentNumber, m.enrollmentId, m.selectedType, m.reasonText.trim())
  reviewToast.value = `Applied "${m.selectedLabel}" to enrolment`
  setTimeout(() => { reviewToast.value = '' }, 3000)
  manageModal.value = null
}
</script>

<style scoped>
.ps-tab { padding: .25rem 0; }
.err-banner { background: #fef2f2; border: 1px solid #fca5a5; color: #b91c1c; padding: .5rem .8rem; border-radius: 6px; font-size: .85rem; margin-bottom: .65rem; }

.status-row { display: flex; gap: .4rem; margin-bottom: .65rem; flex-wrap: wrap; }
.status-chip { background: #eef2f7; border: 1.5px solid transparent; color: #5f6e85; padding: .35rem .8rem; border-radius: 18px; font-size: .82rem; cursor: pointer; }
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
.data-row:hover { background: #f7f9fb; }
.mono { font-family: monospace; font-size: .82rem; color: #0a264f; }
.muted { color: #888; font-size: .82rem; }
.btn-link { background: none; border: 0; color: #0055a5; cursor: pointer; font-size: .85rem; }

.enrol-line { font-size: .85rem; }
.enr-prog { background: #e8f0f8; color: #003366; border-radius: 4px; padding: 1px 6px; font-size: .75rem; font-weight: 700; margin: 0 .3rem; }
.s-badge { margin-left: .35rem; background: #ecf0f6; color: #5f6e85; font-size: .7rem; padding: 1px 7px; border-radius: 10px; }
.st-submitted { background: #fff3cd; color: #856404; }
.st-pending { background: #cfe2ff; color: #003366; }
.st-active { background: #d1fae5; color: #065f46; }
.st-draft { background: #f0f3f7; color: #777; }
.unverified { background: #fdecea; color: #c0392b; }
.btn-confirm-email {
  background: #fff; border: 1px solid #cfd7e3; color: #003366;
  border-radius: 5px; padding: 2px 9px; font-size: .72rem; font-weight: 600;
  cursor: pointer; margin-left: .35rem;
}
.btn-confirm-email:hover:not(:disabled) { background: #f0f6ff; border-color: #a0c0e0; }
.btn-confirm-email:disabled { opacity: .55; cursor: not-allowed; }
.reset-pw-reveal-row { background: #ecfdf5; border: 1px solid #6ee7b7; border-radius: 6px; padding: .4rem .65rem; margin-top: .4rem; font-size: .82rem; }
.reset-pw-reveal-row code { font-family: monospace; color: #065f46; background: #fff; padding: .1rem .4rem; border-radius: 3px; }

.overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.4); z-index: 60; }
.drawer { position: fixed; top: 0; right: 0; bottom: 0; width: 540px; max-width: 100vw; background: #fff; z-index: 61; box-shadow: -4px 0 16px rgba(0,0,0,0.15); display: flex; flex-direction: column; }
.drawer-head { display: flex; justify-content: space-between; align-items: flex-start; padding: 1rem 1.25rem; border-bottom: 1px solid #e5eaf1; }
.drawer-head h2 { margin: 0; font-size: 1.1rem; color: #003366; }
.sub { color: #888; font-size: .82rem; margin: .25rem 0 0; }
.drawer-close { background: none; border: 0; font-size: 1.2rem; cursor: pointer; color: #888; }
.drawer-head-actions { display: flex; align-items: center; gap: .55rem; }
.btn-review-app { background: #003366; color: #fff; border: none; border-radius: 5px; padding: .35rem .75rem; font-size: .78rem; font-weight: 600; cursor: pointer; white-space: nowrap; }
.btn-review-app:hover { background: #0055a5; }
.btn-review-app-sm { padding: 2px 8px; font-size: .72rem; font-weight: 600; }
.btn-review-app:disabled { background: #c0c8d2; cursor: not-allowed; opacity: 0.7; }
.td-actions { display: flex; align-items: center; gap: .5rem; justify-content: flex-end; }
.review-toast { position: fixed; bottom: 2rem; left: 50%; transform: translateX(-50%); background: #0d6b55; color: #fff; padding: .75rem 1.5rem; border-radius: 8px; font-size: .88rem; box-shadow: 0 4px 18px rgba(0,0,0,0.22); z-index: 500; }
.fade-enter-active, .fade-leave-active { transition: opacity .2s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
.drawer-body { flex: 1; overflow: auto; padding: 1rem 1.25rem; }

.section { margin-bottom: .9rem; border: 1px solid #e8edf4; border-radius: 7px; padding: .55rem .8rem; background: #fafbfc; }
.section summary { cursor: pointer; font-weight: 700; color: #003366; padding: .25rem 0; }
.section[open] { background: #fff; }

.row-2 { display: grid; grid-template-columns: 1fr 1fr; gap: .55rem; margin-top: .55rem; }
.row-3 { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: .55rem; margin-top: .55rem; }
.field { display: flex; flex-direction: column; gap: .25rem; }
.field label { font-size: .75rem; font-weight: 600; color: #444; }
.field input, .field select { padding: .42rem .55rem; border: 1.5px solid #d0d7e0; border-radius: 5px; font-size: .85rem; background: #fff; }

.lang-list { display: flex; flex-direction: column; gap: .35rem; margin-top: .5rem; }
.lang-row { display: grid; grid-template-columns: 1fr 160px 28px; gap: .35rem; }
.btn-x { background: none; border: 1px solid #fca5a5; color: #b91c1c; border-radius: 5px; padding: .15rem .4rem; cursor: pointer; }
.btn-add-mini { background: #f0f7ff; color: #003366; border: 1.5px dashed #a0c0e0; border-radius: 5px; padding: .3rem .65rem; cursor: pointer; font-size: .8rem; margin-top: .35rem; }
.btn-save { display: block; margin: .65rem 0 0; background: #003366; color: #fff; border: 0; padding: .4rem .85rem; border-radius: 5px; cursor: pointer; font-weight: 600; }
.btn-mark-ready { background: #d1fae5; color: #065f46; border: 1px solid #6ee7b7; border-radius: 5px; padding: .35rem .7rem; cursor: pointer; font-size: .82rem; font-weight: 600; }
.btn-mark-ready:disabled { opacity: .5; cursor: default; }
.btn-withdraw { background: #fff7e0; color: #856404; border: 1px solid #f5d684; border-radius: 5px; padding: .35rem .7rem; cursor: pointer; font-size: .82rem; font-weight: 600; }
.btn-withdraw:disabled { opacity: .5; cursor: default; }
.status-locked-note { font-size: .8rem; color: #888; font-style: italic; }
.lock-banner { background: #fff7e0; border: 1px solid #f5d684; color: #6b5310; padding: .55rem .8rem; border-radius: 6px; font-size: .85rem; margin-bottom: .65rem; }
.btn-save:disabled { opacity: .5; cursor: default; }

.enr-row { border: 1px solid #e8edf4; border-radius: 6px; padding: .55rem .75rem; margin-top: .5rem; background: #fafbfc; }
.enr-head { display: flex; align-items: center; flex-wrap: wrap; gap: .35rem; }
.enr-specialization { color: #555; font-size: .85rem; }
.enr-actions { margin-top: .5rem; }

.doc-row { display: flex; justify-content: space-between; align-items: center; padding: .5rem 0; border-bottom: 1px solid #f0f3f7; }
.doc-row:last-child { border: 0; }
.doc-info strong { color: #003366; }
.doc-meta { color: #888; font-size: .78rem; margin: .15rem 0 0; }
.verify-toggle { display: flex; gap: .35rem; align-items: center; cursor: pointer; font-size: .82rem; }

/* Flow status column */
.td-status-col { min-width: 200px; }
.status-line { display: flex; align-items: center; gap: .4rem; padding: 2px 0; }
.flow-badge { display: inline-block; font-size: .75rem; font-weight: 600; padding: 3px 9px; border-radius: 12px; white-space: nowrap; }
.fb-gray   { background: #eef2f7; color: #5f6e85; border: 1px solid #d0d7e0; }
.fb-yellow { background: #fff3cd; color: #856404; border: 1px solid #fcd34d; }
.fb-orange { background: #ffedd5; color: #9a3412; border: 1px solid #fdba74; }
.fb-blue   { background: #dbeafe; color: #1d4ed8; border: 1px solid #93c5fd; }
.fb-green  { background: #d1fae5; color: #065f46; border: 1px solid #6ee7b7; }
.fb-red    { background: #fee2e2; color: #991b1b; border: 1px solid #fca5a5; }
.fb-amber  { background: #fef3c7; color: #92400e; border: 1px solid #fcd34d; }
.fb-purple { background: #ede9fe; color: #5b21b6; border: 1px solid #c4b5fd; }

.btn-manage-sub { background: #fff; border: 1px solid #cfd7e3; color: #003366; border-radius: 5px; padding: 2px 8px; font-size: .72rem; cursor: pointer; }
.btn-manage-sub:hover { background: #f0f6ff; border-color: #a0c0e0; }
.btn-grade { background: #fff; border: 1px solid #1c7a4a; color: #1c7a4a; border-radius: 5px; padding: 2px 8px; font-size: .72rem; font-weight: 700; cursor: pointer; }
.btn-grade:hover { background: #eaf6ec; }

/* Manage modal */
.manage-modal { background: #fff; border-radius: 10px; width: 560px; max-width: 95vw; box-shadow: 0 12px 40px rgba(0,0,0,.25); padding: 0; overflow: hidden; }
.manage-modal:has(.grade-grid) { width: 1080px; }
.manage-hdr { display: flex; justify-content: space-between; align-items: center; padding: 1rem 1.25rem; border-bottom: 1.5px solid #e8edf4; }
.manage-hdr h3 { margin: 0; color: #003366; font-size: 1rem; }
.manage-sub { color: #888; font-size: .82rem; margin: 0; padding: .5rem 1.25rem .25rem; }
.manage-body { padding: .75rem 1.25rem 1.25rem; }
.manage-hint { font-size: .85rem; margin: .25rem 0 .85rem; }
.manage-grid { display: grid; grid-template-columns: 1fr 1fr; gap: .6rem; }
.manage-option { background: #fff; border: 1.5px solid #d0d7e0; border-radius: 8px; padding: .7rem .9rem; cursor: pointer; text-align: left; display: flex; flex-direction: column; gap: .2rem; transition: border-color .1s, background .1s; }
.manage-option:hover { background: #f7f9fb; border-color: #003366; }
.manage-opt-label { font-size: .92rem; font-weight: 700; color: #003366; }
.manage-opt-sub { font-size: .72rem; color: #888; }
.mo-blue   .manage-opt-label { color: #1d4ed8; }
.mo-red    .manage-opt-label { color: #991b1b; }
.mo-amber  .manage-opt-label { color: #92400e; }
.mo-purple .manage-opt-label { color: #5b21b6; }
.mo-gray   .manage-opt-label { color: #5f6e85; }

.manage-selected { margin-bottom: .85rem; }
.manage-label { display: block; font-size: .82rem; font-weight: 600; color: #444; margin-bottom: .35rem; }
.manage-body textarea { width: 100%; padding: .6rem .75rem; border: 1.5px solid #ccc; border-radius: 6px; font-size: .88rem; font-family: inherit; resize: vertical; }
.manage-body textarea:focus { outline: none; border-color: #003366; }
.manage-footer { display: flex; justify-content: space-between; align-items: center; margin-top: .85rem; }
.btn-confirm-manage { background: #0d6b55; color: #fff; border: none; border-radius: 6px; padding: .55rem 1.2rem; font-size: .88rem; font-weight: 600; cursor: pointer; }
.btn-confirm-manage:hover { background: #0a5a47; }
.btn-confirm-manage:disabled { opacity: .45; cursor: default; }

.manage-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.45); z-index: 70; display: flex; align-items: center; justify-content: center; }

.grade-input { width: 90px; padding: .35rem .55rem; border: 1.5px solid #cfd7e3; border-radius: 5px; font-size: .85rem; }
.grade-input:focus { border-color: #003366; outline: none; }

/* Multi-column grade list — flows top-to-bottom, max ~10 per column. */
.grade-statusbar { display: flex; gap: 1.5rem; flex-wrap: wrap; font-size: .82rem; color: #44506a; background: #f6f9fd; border: 1px solid #e6ebf2; border-radius: 6px; padding: .45rem .7rem; margin: .3rem 0 .5rem; }
.ok-banner { background: #eafaf0; border: 1px solid #b6e6c8; color: #1c7a4a; padding: .45rem .65rem; border-radius: 6px; font-size: .82rem; margin: .3rem 0; }
.grade-grid { column-gap: 1.2rem; margin-top: .5rem; }
.grade-row {
  break-inside: avoid;
  display: grid; grid-template-columns: 80px 1fr auto 60px auto; gap: .5rem;
  align-items: center; padding: .35rem .25rem;
  border-bottom: 1px solid #eef2f7; font-size: .82rem;
}
.gr-letter { display: inline-block; min-width: 30px; text-align: center; padding: 2px 6px;
  border-radius: 6px; font-weight: 700; font-size: .78rem; background: #eef2f7; color: #334155; }
.gr-code { font-family: ui-monospace, monospace; font-size: .76rem; color: #003366; }
.gr-name { color: #222; line-height: 1.3; min-width: 0; word-break: break-word; }
.gr-ects { color: #888; font-size: .72rem; white-space: nowrap; }
.ects-line { font-size: .82rem; margin: .3rem 0; }
.ects-warn { color: #92400e; font-weight: 600; }
.ects-ok { color: #1c7a4a; font-weight: 600; }
.gr-input { width: 60px; }

.grade-reject-banner { background: #fef2f2; border: 1.5px solid #fca5a5; border-left: 3px solid #b91c1c; border-radius: 7px; padding: .65rem .85rem; margin-bottom: .85rem; }
.grade-reject-title { color: #b91c1c; font-weight: 700; font-size: .9rem; }
.grade-reject-meta { color: #7f1d1d; font-size: .78rem; margin-top: .15rem; }
.grade-reject-note { margin: .45rem 0 0; font-family: inherit; font-size: .85rem; color: #444; white-space: pre-wrap; background: #fff; border: 1px solid #fbcaca; border-radius: 5px; padding: .5rem .65rem; }

/* Quick-view detail modal — mirrors the admin tab's modal so partner +
   admin views feel the same. Names are blue, modal is fixed height,
   tab content scrolls inside. */
.s-name-link { color: #1a4d8c; font-weight: 600; cursor: pointer; text-decoration: none; }
.s-name-link:hover { text-decoration: underline; color: #143b6c; }
.btn-row-details { padding: .25rem .65rem; border: 1px solid #1a4d8c; background: #fff; color: #1a4d8c; border-radius: 4px; font-size: .75rem; font-weight: 600; cursor: pointer; margin-right: .35rem; }
.btn-row-details:hover { background: #eef3fb; }
.btn-row-details-sm { padding: .15rem .5rem; font-size: .7rem; margin-right: 0; }

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
.btn-mini-d { padding: .3rem .75rem; border: 1px solid #1a4d8c; background: #1a4d8c; color: #fff; border-radius: 5px; font-size: .78rem; font-weight: 600; cursor: pointer; }
.btn-mini-d:disabled { opacity: .5; cursor: not-allowed; background: #cbd5e1; border-color: #cbd5e1; }
.btn-mini-d:hover:not(:disabled) { background: #143b6c; }
.btn-mini-email { background: #fff; color: #6b4ea3; border-color: #6b4ea3; }
.btn-mini-email:hover:not(:disabled) { background: #f1ecf9; }
.btn-mini-email:disabled { background: #f1f5f9; color: #94a3b8; border-color: #cbd5e1; }
.btn-mini-ghost-d { background: #fff; color: #5f6e85; border-color: #cbd5e1; }
.btn-mini-ghost-d:hover:not(:disabled) { background: #f1f5f9; }
.email-send-pop { position: fixed; inset: 0; background: rgba(0,0,0,.35); display: flex; align-items: center; justify-content: center; z-index: 1200; }
.email-send-card { background: #fff; border-radius: 9px; width: min(420px, 92%); padding: 1rem 1.1rem; box-shadow: 0 8px 30px rgba(0,0,0,.2); }
.esp-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: .3rem; }
.esp-close { border: 0; background: none; font-size: 1rem; cursor: pointer; color: #5f6e85; }
.esp-label { display: block; font-size: .74rem; font-weight: 700; color: #6b7888; text-transform: uppercase; letter-spacing: .04em; margin: .5rem 0 .2rem; }
.email-send-card input { width: 100%; padding: .4rem .5rem; border: 1px solid #d8dde5; border-radius: 5px; font-size: .82rem; }
.esp-actions { display: flex; justify-content: flex-end; gap: .5rem; margin-top: .8rem; }
.pp-table { width: 100%; border-collapse: collapse; font-size: .85rem; }
.pp-table th { text-align: left; padding: .4rem .6rem; color: #6b7888; font-size: .72rem; text-transform: uppercase; border-bottom: 1.5px solid #e8edf4; }
.pp-table td { padding: .45rem .6rem; border-bottom: 1px solid #eef1f5; }
.pp-pill { font-size: .72rem; font-weight: 700; padding: .1rem .5rem; border-radius: 10px; }
.pp-paid { background: #d1fae5; color: #065f46; }
.pp-unpaid { background: #fde7e5; color: #a8241e; }
.pp-summary { display: flex; gap: 1.25rem; flex-wrap: wrap; font-size: .85rem; padding: .5rem 0 0; }
.pp-due strong { color: #a8241e; }
.pp-ok strong { color: #065f46; }
</style>

<style scoped>
.project-title-row { margin: .6rem 0; display: flex; flex-direction: column; gap: .3rem; }
.project-title-row label { font-size: .82rem; font-weight: 600; color: #1a2d4f; }

.pgn-bar { display: flex; align-items: center; gap: .4rem; padding: .6rem 0 .2rem; }
.pgn-btn { background: #f2f5f9; border: 1px solid #cfd7e3; border-radius: 5px; padding: .3rem .7rem; font-size: .8rem; font-weight: 600; color: #2c3e50; cursor: pointer; }
.pgn-btn:disabled { opacity: .4; cursor: default; }
.pgn-btn:not(:disabled):hover { background: #e8eef6; }
.pgn-info { font-size: .8rem; color: #5f6e85; margin: 0 .4rem; }
.pgn-size { margin-left: auto; padding: .3rem .5rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .8rem; background: #fff; }
</style>
