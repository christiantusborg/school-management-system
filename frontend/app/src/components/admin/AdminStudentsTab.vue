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
      <select v-if="!partnerId" v-model="filterPartnerName" class="inp">
        <option value="">All partners</option>
        <option v-for="p in partnersAvailable" :key="p" :value="p">{{ p }}</option>
      </select>
      <button class="btn-refresh" :disabled="loading" @click="load">{{ loading ? 'Loading…' : '↻' }}</button>
      <button class="btn-export" @click="exportModal = makeExportModal()">📥 Export students</button>
      <button v-if="partnerId" class="btn-add-student" @click="emit('add-student')">➕ Add student</button>
    </div>

    <div v-if="!loading && filtered.length === 0" class="empty">No students match.</div>
    <table v-else-if="!loading" class="data-table">
      <thead>
        <tr>
          <th>Student #</th><th>Name</th><th v-if="!partnerId">Partner</th>
          <th>Email</th><th>Enrolments</th><th>Actions</th><th></th>
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
              <span v-if="e.paymentOverdue" class="s-badge s-badge-overdue"
                    title="An installment or additional invoice is unpaid past its due date (Programs → Payment).">Payment overdue</span>
            </div>
            <div v-if="s.signingUp" class="enrol-line">
              <span class="s-badge s-badge-signup"
                    title="The signup wizard was started but never submitted. Re-entering the same email in the wizard continues where it stopped.">
                Signing up — step {{ Math.max(s.wizardStep, 1) }} of 6</span>
            </div>
          </td>
          <td class="enrol-actions-cell">
            <div v-if="s.signingUp" class="enrol-actions">
              <button class="btn-review-sm btn-continue-signup" :disabled="s.openingSignup"
                      title="Open the signup wizard exactly where this applicant stopped — no password needed."
                      @click.stop="continueSignup(s)">
                {{ s.openingSignup ? 'Opening…' : '▶ Continue signup' }}
              </button>
            </div>
            <div v-for="e in s.enrollments" :key="e.studentEnrollmentId" class="enrol-actions">
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
          <td>
            <button v-if="canDeleteStudent" class="btn-delete-student"
                    :disabled="deletingStudentId === s.studentId"
                    title="Remove this applicant"
                    @click.stop="deleteStudent(s)">
              {{ deletingStudentId === s.studentId ? 'Deleting…' : 'Delete' }}
            </button>
          </td>
        </tr>
      </tbody>
    </table>

    <!-- Grade review modal (also teleported inline into the drawer's
         Programs → Grades sub-tab when opened with inline=true) -->
    <Teleport v-if="gradeModal" defer to="#grade-editor-inline-slot" :disabled="!gradeModal.inline">
    <transition name="fade">
      <div :class="gradeModal.inline ? 'grade-inline-wrap' : 'manage-overlay'" @click.self="gradeModal.inline ? null : (gradeModal = null)">
        <div class="manage-modal grade-modal" :class="{ 'grade-inline': gradeModal.inline }">
          <div v-if="!gradeModal.inline" class="manage-hdr">
            <h3>{{ gradeModal.mode === 'submit' ? 'Submit grades' : 'Approve grades' }}</h3>
            <button class="drawer-close" @click="gradeModal = null">✕</button>
          </div>
          <p v-if="!gradeModal.inline" class="manage-sub">{{ gradeModal.studentName }} · {{ gradeModal.programmeCode }} · {{ gradeModal.specializationName }}</p>
          <div class="manage-body">
            <p v-if="gradeModal.error" class="err-banner">{{ gradeModal.error }}</p>
            <p v-if="gradeModal.loading" class="muted">Loading grades…</p>
            <p v-else-if="gradeModal.mode === 'submit'" class="muted manage-hint">
              Enter a score (0–100) for each subject. On submit, the enrolment moves to Awaiting grades approval.
            </p>
            <p v-if="gradeModal.mode === 'submit' && gradeModal.requiredEcts" class="manage-hint"
               :class="adminEctsRemaining > 0 ? 'ects-warn' : 'ects-ok'">
              Completed {{ adminCompletedEcts }} of {{ gradeModal.requiredEcts }} required ECTS.
              <span v-if="adminEctsRemaining > 0">Need {{ adminEctsRemaining }} more before you can submit.</span>
              <span v-else>Threshold reached — you can submit.</span>
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
                <span class="gr-letter" :title="`School grade for ${row.score ?? '—'}`">{{ scoreToLetter(row.score) }}</span>
              </div>
            </div>
            <p v-else-if="!gradeModal.loading" class="muted">No grades submitted for this enrolment.</p>

            <!-- Thesis/dissertation project title — shown once the thesis module has a grade. -->
            <div v-if="gradeModal.mode === 'submit' || adminThesisGraded" class="project-title-row">
              <label>Project title <span class="muted" style="font-weight:400;">(thesis/dissertation — shown on the transcript)</span></label>
              <input v-if="gradeModal.mode === 'submit'" v-model="gradeModal.projectTitle" class="grade-input" style="width:100%;" placeholder="e.g. The impact of …" />
              <strong v-else>{{ gradeModal.projectTitle || '—' }}</strong>
            </div>

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
              <button v-if="gradeModal.mode !== 'reject' && !gradeModal.inline" class="btn-link" @click="gradeModal = null">Cancel</button>
              <button v-else class="btn-link" @click="gradeModal.mode = 'view'">← Back</button>

              <template v-if="gradeModal.mode === 'submit'">
                <button class="btn-link" :disabled="gradeModal.downloadingProvisional" @click="downloadAdminProvisional">
                  {{ gradeModal.downloadingProvisional ? 'Preparing…' : '⤓ Provisional transcript' }}
                </button>
                <button class="btn-confirm-manage" style="background:#003366;border-color:#003366;"
                        :disabled="gradeModal.savingDraft" @click="saveAdminGradesDraft">
                  {{ gradeModal.savingDraft ? 'Saving…' : 'Save grades' }}
                </button>
                <button v-if="!gradeModal.postApproval" class="btn-confirm-manage btn-approve-final"
                        :disabled="!canCommitAdminGrades || gradeModal.submitting"
                        :title="adminEctsRemaining > 0 ? `Need ${adminEctsRemaining} more ECTS to reach the ${gradeModal.requiredEcts} ECTS completion threshold.` : ''"
                        @click="confirmGradeSubmission">
                  {{ gradeModal.submitting ? 'Submitting…' : '✓ Submit grades' }}
                </button>
                <span v-else class="muted" style="font-size:.76rem;">
                  Grades already approved — Save updates the scores; regenerate the transcript/certificates from Letters afterwards.
                </span>
              </template>
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
    </Teleport>

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
                    <dt>Student ID</dt>
                    <dd>
                      <span class="mono">{{ detailModal.studentNumber }}</span>
                      <span v-if="detailModal.data.isLegacyStudent" class="s-badge st-active">Old student</span>
                    </dd>
                  </dl>

                  <!-- Admission-Office-only manual Student ID + Old-student flag,
                       for students migrated from the old system. -->
                  <div v-if="canEditLegacyId" class="legacy-box">
                    <label class="legacy-check">
                      <input type="checkbox" v-model="legacyDraft.isLegacy" />
                      <span><strong>Old student</strong> (migrated from the old system — set their existing Student ID manually)</span>
                    </label>
                    <div v-if="legacyDraft.isLegacy" class="legacy-id-row">
                      <label>Student ID</label>
                      <input v-model="legacyDraft.studentNumber" class="legacy-id-input" placeholder="e.g. IBSS-2019-0123" />
                      <button class="btn-row-details btn-row-details-sm" :disabled="savingLegacy" @click="saveLegacyId">
                        {{ savingLegacy ? 'Saving…' : 'Save ID' }}
                      </button>
                    </div>
                    <button v-else class="btn-row-details btn-row-details-sm" :disabled="savingLegacy" @click="saveLegacyId">
                      {{ savingLegacy ? 'Saving…' : 'Save' }}
                    </button>
                    <span v-if="legacyError" class="err-banner" style="display:block;margin-top:.4rem;">{{ legacyError }}</span>
                    <span v-else-if="legacyOk" class="ok-banner" style="display:block;margin-top:.4rem;">Saved — recorded in the Activity log</span>
                  </div>
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
                    <label class="edit-field edit-field-wide">
                      <span>Specialization for degree</span>
                      <input v-model="detailModal.data.background.degreeSpecialization" placeholder="Specialty from previous education" />
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
                              </div>
            </div>

            <!-- Programs tab: everything enrolment-scoped, one menu entry
                 per programme the student signed up for -->
            <div v-show="detailModal.activeTab === 'programs'" class="tab-pane">
              <p v-if="!detailEnrollments.length" class="muted">No enrolments.</p>
              <div v-else class="programs-layout">
                <aside class="programs-menu">
                  <button v-for="e in detailEnrollments" :key="e.studentEnrollmentId"
                          :class="['prog-menu-item', { active: e.studentEnrollmentId === activeEnrollment?.studentEnrollmentId }]"
                          @click="detailModal.activeEnrollmentId = e.studentEnrollmentId">
                    <span class="prog-menu-name">{{ e.programmeName }}</span>
                    <span class="prog-menu-spec">{{ e.specializationName }}</span>
                    <span class="prog-menu-status">{{ e.statusName }}</span>
                  </button>
                  <button class="btn-add" style="margin-top:.3rem;" @click="openAddProg">+ Add programme</button>
                  <div v-if="addProg.open" class="add-prog-box">
                    <select v-model="addProg.programmeId" class="inp" style="width:100%; margin-bottom:.35rem;">
                      <option value="">— Programme —</option>
                      <option v-for="p in enrolmentProgOptions" :key="p.programmeId" :value="p.programmeId">
                        {{ p.name }}{{ p.schoolName ? ` (${p.schoolName})` : '' }}
                      </option>
                    </select>
                    <select v-model="addProg.specializationId" class="inp" style="width:100%; margin-bottom:.35rem;" :disabled="!addProg.programmeId">
                      <option value="">— Specialization —</option>
                      <option v-for="m in addProgSpecs" :key="m.specializationId" :value="m.specializationId">{{ m.name }}</option>
                    </select>
                    <div style="display:flex; gap:.35rem;">
                      <button class="btn-row-details btn-row-details-sm" :disabled="!addProg.specializationId || addProg.busy" @click="saveAddProg">
                        {{ addProg.busy ? 'Adding…' : 'Add' }}
                      </button>
                      <button class="btn-row-details btn-row-details-sm" @click="addProg.open = false">Cancel</button>
                    </div>
                    <p v-if="addProg.error" class="card-toggle-err" style="margin:.3rem 0 0;">{{ addProg.error }}</p>
                  </div>
                </aside>
                <div class="programs-content" v-if="activeEnrollment">
                  <div class="prog-subtabs">
                    <button v-for="st in PROGRAM_SUBTABS" :key="st.id"
                            :class="['tab-btn', { active: programSubTab === st.id }]"
                            @click="programSubTab = st.id">{{ st.label }}</button>
                  </div>

                  <!-- Enrolment sub-tab -->
                  <div v-if="programSubTab === 'enrolment'" class="detail-grid prog-enrol-grid">
<div class="detail-section" v-if="activeEnrollment">
                  <h4>Enrolment</h4>
                  <dl>
                    <dt>Programme</dt>
                    <dd v-if="canEditSpecialization">
                      <select v-model="programmeDraft" class="dur-input" style="width:min(520px, 90%);" @change="onProgrammeDraftChange">
                        <option v-for="p in enrolmentProgOptions" :key="p.programmeId" :value="p.programmeId">
                          {{ p.name }}{{ p.schoolName ? ` (${p.schoolName})` : '' }}{{ p.showCode && p.code ? ` · ${p.code}` : '' }}
                        </option>
                      </select>
                    </dd>
                    <dd v-else>{{ activeEnrollment.programmeName }}</dd>
                    <dt>Specialisation</dt>
                    <dd v-if="canEditSpecialization">
                      <select v-model="specializationDraft" class="dur-input" style="width:min(520px, 90%);">
                        <option v-for="m in enrolmentSpecOptions" :key="m.specializationId" :value="m.specializationId">{{ m.name }}</option>
                      </select>
                      <button class="btn-row-details btn-row-details-sm"
                              :disabled="savingSpecialization || !specializationDraft || (programmeDraft === activeEnrollment.programmeId && specializationDraft === activeEnrollment.specializationId)"
                              @click="saveSpecialization">
                        {{ savingSpecialization ? 'Saving…' : 'Save' }}
                      </button>
                      <span v-if="specializationSaveError" class="err-banner" style="display:inline-block;margin-left:.5rem;">{{ specializationSaveError }}</span>
                      <span v-else-if="specializationSaveOk" class="ok-banner" style="display:inline-block;margin-left:.5rem;">Saved</span>
                      <div v-if="programmeDraft !== activeEnrollment.programmeId" class="dur-warn">⚠ Changing programme moves the enrolment; subjects/grades for the new programme apply.</div>
                    </dd>
                    <dd v-else>{{ activeEnrollment.specializationName }}</dd>
                    <dt>Study language</dt>
                    <dd v-if="canEditDuration">
                      <input type="text" class="dur-input" style="width:min(380px, 70%);" v-model="teachingLanguageDraft"
                             placeholder="English (programme default)" />
                      <button class="btn-row-details btn-row-details-sm" :disabled="savingLanguage" @click="saveTeachingLanguage">
                        {{ savingLanguage ? 'Saving…' : 'Save' }}
                      </button>
                      <span v-if="languageSaveError" class="err-banner" style="display:inline-block;margin-left:.5rem;">{{ languageSaveError }}</span>
                      <span v-else-if="languageSaveOk" class="ok-banner" style="display:inline-block;margin-left:.5rem;">Saved</span>
                      <div class="muted" style="font-size:.72rem;margin-top:.2rem;">Override on all letters; blank = programme default.</div>
                    </dd>
                    <dd v-else>{{ activeEnrollment.instructionLanguage || '—' }}</dd>
                    <dt>Mode</dt>
                    <dd v-if="canEditDuration">
                      <select v-model.number="modeDraft" class="dur-input" style="width:min(380px, 70%);">
                        <option v-for="m in modeOptions" :key="m.modeOfStudyId" :value="m.modeOfStudyId">{{ m.name }}</option>
                      </select>
                      <button class="btn-row-details btn-row-details-sm"
                              :disabled="savingMode || !modeDraft || modeDraft === activeEnrollment.modeOfStudyId"
                              @click="saveModeOfStudy">
                        {{ savingMode ? 'Saving…' : 'Save' }}
                      </button>
                      <span v-if="modeSaveError" class="err-banner" style="display:inline-block;margin-left:.5rem;">{{ modeSaveError }}</span>
                      <span v-else-if="modeSaveOk" class="ok-banner" style="display:inline-block;margin-left:.5rem;">Saved</span>
                    </dd>
                    <dd v-else>{{ activeEnrollment.modeOfStudyName ?? '—' }}</dd>
                    <dt>Commencement</dt>
                    <dd v-if="canEditDuration">
                      <input type="date" class="dur-input" style="width:150px;" v-model="commencementDraft" />
                      <button class="btn-row-details btn-row-details-sm" :disabled="savingCommencement"
                              @click="saveCommencement">
                        {{ savingCommencement ? 'Saving…' : 'Save' }}
                      </button>
                      <span v-if="commencementSaveError" class="err-banner" style="display:inline-block;margin-left:.5rem;">{{ commencementSaveError }}</span>
                      <span v-else-if="commencementSaveOk" class="ok-banner" style="display:inline-block;margin-left:.5rem;">Saved</span>
                      <div v-if="commencementPastWarning" class="dur-warn">⚠ {{ commencementPastWarning }}</div>
                    </dd>
                    <dd v-else>{{ formatDate(activeEnrollment.commencementDate) || '—' }}</dd>
                    <dt>Default duration</dt><dd>{{ activeEnrollment.durationOfStudyMonths ?? '—' }} months</dd>
                    <dt>Programme range</dt>
                    <dd v-if="activeEnrollment.programmeMaxDurationMonths">
                      {{ activeEnrollment.programmeMinDurationMonths }}–{{ activeEnrollment.programmeMaxDurationMonths }} months
                    </dd>
                    <dd v-else>—</dd>
                    <dt>Approved duration</dt>
                    <dd v-if="canEditDuration">
                      <input type="number" class="dur-input" min="1"
                             v-model.number="approvedDurationDraft" />
                      months
                      <button class="btn-row-details btn-row-details-sm" :disabled="savingDuration"
                              @click="saveApprovedDuration">
                        {{ savingDuration ? 'Saving…' : 'Save' }}
                      </button>
                      <span v-if="durationSaveError" class="err-banner" style="display:inline-block;margin-left:.5rem;">{{ durationSaveError }}</span>
                      <span v-else-if="durationSaveOk" class="ok-banner" style="display:inline-block;margin-left:.5rem;">Saved</span>
                      <div v-if="durationRangeWarning" class="dur-warn">⚠ {{ durationRangeWarning }}</div>
                      <div v-if="showRegenOffer" class="dur-regen">
                        Letters released earlier still show the old completion date.
                        <button class="btn-mini" :disabled="regeneratingLetters" @click="regenerateLetters">
                          {{ regeneratingLetters ? 'Regenerating…' : 'Regenerate letters' }}
                        </button>
                        <span v-if="regenResult" style="margin-left:.5rem;">{{ regenResult }}</span>
                      </div>
                    </dd>
                    <dd v-else>
                      {{ activeEnrollment.approvedDurationMonths ?? activeEnrollment.durationOfStudyMonths ?? '—' }} months
                      <span class="muted">(Administrator level required to change)</span>
                    </dd>
                    <dt>Expected completion</dt>
                    <dd>{{ expectedCompletion || '—' }}</dd>
                    <dt>Status</dt>
                    <dd>
                      <template v-if="!statusEdit">
                        <span :class="['s-badge', statusClass(activeEnrollment.statusCode)]">{{ activeEnrollment.statusName }}</span>
                        <button class="btn-row-details btn-row-details-sm" style="margin-left:.5rem;" @click="openStatusEdit">✎ Change status</button>
                      </template>
                      <template v-else>
                        <select v-model="statusDraft" class="dur-input" style="width:230px;">
                          <option v-for="s in enrollmentStatuses" :key="s.statusId" :value="s.statusId">{{ s.name }}</option>
                        </select>
                        <input v-model="statusNote" class="dur-input" style="width:230px;margin-left:.4rem;" placeholder="Reason (optional)" />
                        <button class="btn-row-details btn-row-details-sm" :disabled="savingStatus" @click="saveStatus">
                          {{ savingStatus ? 'Saving…' : 'Save' }}
                        </button>
                        <button class="btn-link" style="margin-left:.4rem;" @click="statusEdit = false">Cancel</button>
                        <span v-if="statusError" class="err-banner" style="display:inline-block;margin-left:.5rem;">{{ statusError }}</span>
                        <div class="muted" style="font-size:.72rem;margin-top:.2rem;">Moves this enrolment to any status (e.g. re-open a rejected application). Logged in the activity log.</div>
                      </template>
                    </dd>
                    <template v-if="canEditDuration">
                      <dt>Offer letter date</dt>
                      <dd>
                        <input type="date" class="dur-input" style="width:150px;" v-model="offerLetterDateDraft" />
                      </dd>
                      <dt>Admission letter date</dt>
                      <dd>
                        <input type="date" class="dur-input" style="width:150px;" v-model="admissionLetterDateDraft" />
                      </dd>
                      <dt>Graduation date</dt>
                      <dd>
                        <input type="date" class="dur-input" style="width:150px;" v-model="graduationDateDraft" />
                        <span class="muted" style="font-size:.72rem;margin-left:.4rem;">defaults to expected completion</span>
                      </dd>
                      <dt>Transcript date</dt>
                      <dd>
                        <input type="date" class="dur-input" style="width:150px;" v-model="transcriptDateDraft" />
                        <button class="btn-row-details btn-row-details-sm" :disabled="savingLetterDates" @click="saveLetterDates">
                          {{ savingLetterDates ? 'Saving…' : 'Save letter dates' }}
                        </button>
                        <span v-if="letterDatesError" class="err-banner" style="display:inline-block;margin-left:.5rem;">{{ letterDatesError }}</span>
                        <span v-else-if="letterDatesOk" class="ok-banner" style="display:inline-block;margin-left:.5rem;">Saved</span>
                        <div class="muted" style="font-size:.72rem;margin-top:.2rem;">Overrides the date printed on the offer/admission letter; blank = release date. Already-released letters are re-rendered.</div>
                      </dd>
                    </template>
                  </dl>
                </div>

                <!-- Module cohorts: modules are scheduled via the partner's
                     Module Cohort Schedule; pick the cohort the student
                     attends per module (saves immediately). -->
                <div class="detail-section" style="grid-column: 1 / -1;">
                  <h4>Module cohorts</h4>
                  <p v-if="studentCohorts.error" class="err-banner">{{ studentCohorts.error }}</p>
                  <template v-if="Object.keys(studentCohorts.bySubject).length">
                    <div v-for="mod in studentCohorts.bySubject" :key="mod.subjectId" class="ms-row">
                      <div class="ms-head">
                        <span class="ms-code">{{ mod.code }}</span>
                        <span class="ms-name">{{ mod.name }}</span>
                        <template v-if="mod.cohorts.length">
                          <span class="muted" style="font-size:.74rem; margin-left:auto;">Cohort:</span>
                          <select class="ms-inp" style="min-width:220px"
                                  :value="mod.assignedCohortId ?? ''"
                                  @change="setStudentCohort(mod.subjectId, $event.target.value)">
                            <option value="">— none —</option>
                            <option v-for="c in mod.cohorts" :key="c.moduleCohortId" :value="c.moduleCohortId">
                              {{ c.cohortNumber }}{{ c.startDate ? ` (${new Date(c.startDate).toLocaleDateString('en-GB')} → ${c.endDate ? new Date(c.endDate).toLocaleDateString('en-GB') : 'TBC'})` : '' }}
                            </option>
                          </select>
                        </template>
                        <span v-else class="muted" style="font-size:.74rem; margin-left:auto;">
                          no cohorts scheduled — create one under the partner's Module Cohorts tab</span>
                      </div>
                    </div>
                  </template>
                  <p v-else class="muted">No modules on this specialization.</p>
                </div>
              </div>

                  <!-- Grades sub-tab: the full grade editor, teleported inline.
                       v-show (not v-if): the slot must stay mounted so the
                       teleport never targets a missing element. -->
                  <div v-show="programSubTab === 'grades'">
                    <div id="grade-editor-inline-slot"></div>
                    <p v-if="!gradeModal" class="muted">Loading grade editor…</p>
                  </div>

            <!-- Letters tab -->
            <div v-if="programSubTab === 'letters'" class="tab-pane">
              <p v-if="!activeEnrollment" class="muted">No enrolment selected.</p>
              <div v-else class="letters-list">
                <template v-for="t in LETTER_TYPES" :key="t.key">
                  <div class="letter-row" :class="{ disabled: !activeEnrollment.letters?.[t.key] }">
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
                    <div class="letter-actions">
                      <button class="btn-mini" :disabled="!activeEnrollment.letters?.[t.key]"
                              @click="downloadLetter(activeEnrollment.letters?.[t.key])">Download</button>
                      <button v-if="canRegenerateLetters" class="btn-mini btn-mini-ghost"
                              :disabled="regeneratingLetterKey === t.key"
                              @click="regenerateLetter(t)">
                        {{ regeneratingLetterKey === t.key
                            ? (activeEnrollment.letters?.[t.key] ? 'Regenerating…' : 'Generating…')
                            : (activeEnrollment.letters?.[t.key] ? 'Regenerate' : 'Generate') }}
                      </button>
                      <button v-if="canRegenerateLetters && EMAILABLE_KEYS.includes(t.key)" class="btn-mini btn-mini-email"
                              :disabled="!activeEnrollment.letters?.[t.key]"
                              @click="openSendEmail(t)">✉ Send</button>
                    </div>
                  </div>
                  <!-- Provisional transcript: rendered live, never stored — listed
                       with the other letters right after the transcript rows. -->
                  <div v-if="t.key === 'printableTranscript'" class="letter-row">
                    <span class="letter-icon">📑</span>
                    <div class="letter-info">
                      <div class="letter-name">Provisional Transcript</div>
                      <div class="letter-sub">Rendered live from the grades saved so far · watermarked until grades are submitted</div>
                    </div>
                    <div class="letter-actions">
                      <button class="btn-mini" :disabled="downloadingLetterProvisional" @click="downloadLetterProvisional()">
                        {{ downloadingLetterProvisional ? 'Preparing…' : 'Download' }}
                      </button>
                    </div>
                  </div>
                </template>
                <p v-if="letterRegenResult" class="muted" style="margin-top:.4rem;">{{ letterRegenResult }}</p>
              </div>

              <!-- Ad-hoc send dialog -->
              <div v-if="emailSend.open" class="email-send-pop">
                <div class="email-send-card">
                  <div class="esp-head">
                    <strong>Send {{ emailSend.label }} email</strong>
                    <button class="btn-close" @click="emailSend.open = false">✕</button>
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
                    <button class="btn-mini btn-mini-ghost" @click="emailSend.open = false">Cancel</button>
                    <button class="btn-mini btn-mini-email" :disabled="emailSend.sending" @click="sendLetterEmail">
                      {{ emailSend.sending ? 'Sending…' : 'Send now' }}
                    </button>
                  </div>
                </div>
              </div>
            </div>


            <!-- Payment tab -->
            <div v-if="programSubTab === 'payment'" class="tab-pane">
              <p v-if="!activeEnrollment" class="muted">No enrolment selected.</p>
              <template v-else>
                <div class="pay-config">
                  <div class="pay-field">
                    <label>Total tuition fee</label>
                    <div class="pay-fee-row">
                      <select v-model="payment.currency" class="pay-cur">
                        <option v-for="c in currencyOptions" :key="c.code" :value="c.code">{{ c.code }}</option>
                      </select>
                      <input type="number" min="0" step="0.01" v-model.number="payment.total" placeholder="e.g. 6000" />
                    </div>
                  </div>
                  <div class="pay-field">
                    <label>Number of payments</label>
                    <select v-model.number="payment.count">
                      <option v-for="n in 12" :key="n" :value="n">{{ n }}</option>
                    </select>
                  </div>
                  <button class="btn-row-details btn-row-details-sm" @click="generateSchedule">↻ Generate schedule</button>
                </div>

                <table v-if="payment.installments.length" class="pay-table">
                  <thead><tr><th>#</th><th>Amount</th><th>Due date</th><th>Paid</th><th>Paid date</th><th>Invoice</th></tr></thead>
                  <tbody>
                    <template v-for="(inst, idx) in payment.installments" :key="idx">
                      <tr>
                        <td>{{ idx + 1 }}</td>
                        <td><input type="number" min="0" step="0.01" v-model.number="inst.amount" class="pay-inp" /></td>
                        <td><input type="date" v-model="inst.dueDate" class="pay-inp" /></td>
                        <td class="pay-center"><input type="checkbox" v-model="inst.isPaid" /></td>
                        <td><input v-if="inst.isPaid" type="date" v-model="inst.paidDate" class="pay-inp" /></td>
                        <td>
                          <a class="pay-invoice-link" @click="downloadInvoice(idx + 1)">⤓ Invoice</a>
                        </td>
                      </tr>
                      <tr class="pay-methods-row">
                        <td></td>
                        <td colspan="5">
                          <div class="pay-methods">
                            <div class="pay-method">
                              <label class="pay-method-toggle">
                                <input type="checkbox" v-model="inst.payByCard" />
                                Pay by card (payment link)
                              </label>
                              <input v-if="inst.payByCard" type="text" v-model="inst.cardPaymentLink"
                                placeholder="https://… payment link for this installment" class="pay-method-input" />
                            </div>
                            <div class="pay-method">
                              <label class="pay-method-toggle">
                                <input type="checkbox" v-model="inst.payByBank" />
                                Pay by bank transfer
                              </label>
                              <textarea v-if="inst.payByBank" v-model="inst.bankAccountDetails" rows="2"
                                placeholder="Bank name, IBAN / account no., payment reference…" class="pay-method-input"></textarea>
                            </div>
                          </div>
                        </td>
                      </tr>
                    </template>
                  </tbody>
                </table>
                <p v-else class="muted" style="margin:.6rem 0;">No payments yet. Enter a total and number of payments, then Generate schedule.</p>

                <!-- Additional invoices: manual one-off fees (attestation, delivery,
                     reprinting, …). Never split — each is paid as a whole. -->
                <div class="pay-add-head">
                  <strong>Additional invoices</strong>
                  <button class="btn-row-details btn-row-details-sm" @click="addAdditionalInvoice">+ Create additional invoice</button>
                </div>
                <p v-if="!payment.additional.length" class="muted" style="margin:.3rem 0 .6rem; font-size:.78rem;">
                  One-off fees (attestation fee, delivery fee, reprinting fee, …). Each additional invoice
                  has its own lines, due date and payment methods, and is paid as one — never in installments.
                </p>
                <div v-for="(ai, aidx) in payment.additional" :key="aidx" class="pay-ai-card">
                  <div class="pay-ai-head">
                    <strong>Additional invoice {{ aidx + 1 }}</strong>
                    <span class="muted" style="font-size:.78rem;">Total: {{ payment.currency }} {{ fmtMoney(aiTotal(ai)) }}</span>
                    <span style="flex:1;"></span>
                    <a class="pay-invoice-link" @click="downloadAdditionalInvoice(aidx + 1)">⤓ Invoice</a>
                    <button class="btn-mini btn-mini-ghost" title="Remove this additional invoice" @click="removeAdditionalInvoice(aidx)">✕ Remove</button>
                  </div>
                  <div v-for="(line, lidx) in ai.lines" :key="lidx" class="pay-ai-line">
                    <div class="pay-ai-line-fields">
                      <input type="number" min="0" step="0.01" v-model.number="line.amount" placeholder="Amount" class="pay-inp" />
                      <input type="text" v-model="line.text" placeholder="What is this fee? (e.g. Attestation fee)" class="pay-inp pay-ai-text" />
                    </div>
                    <button v-if="ai.lines.length > 1" class="btn-mini btn-mini-ghost" title="Remove line" @click="ai.lines.splice(lidx, 1)">✕</button>
                  </div>
                  <button class="btn-mini btn-mini-ghost" style="margin:.2rem 0 .4rem;" @click="ai.lines.push({ text: '', amount: 0 })">+ Add line</button>
                  <div class="pay-ai-meta">
                    <label>Due date <input type="date" v-model="ai.dueDate" class="pay-inp" /></label>
                    <label class="pay-method-toggle"><input type="checkbox" v-model="ai.isPaid" /> Paid</label>
                    <label v-if="ai.isPaid">Paid date <input type="date" v-model="ai.paidDate" class="pay-inp" /></label>
                  </div>
                  <div class="pay-methods">
                    <div class="pay-method">
                      <label class="pay-method-toggle">
                        <input type="checkbox" v-model="ai.payByCard" />
                        Pay by card (payment link)
                      </label>
                      <input v-if="ai.payByCard" type="text" v-model="ai.cardPaymentLink"
                        placeholder="https://… payment link for this invoice" class="pay-method-input" />
                    </div>
                    <div class="pay-method">
                      <label class="pay-method-toggle">
                        <input type="checkbox" v-model="ai.payByBank" />
                        Pay by bank transfer
                      </label>
                      <textarea v-if="ai.payByBank" v-model="ai.bankAccountDetails" rows="2"
                        placeholder="Bank name, IBAN / account no., payment reference…" class="pay-method-input"></textarea>
                    </div>
                  </div>
                </div>

                <div class="pay-summary">
                  <div>Total tuition: <strong>{{ payment.currency }} {{ fmtMoney(payment.total) }}</strong></div>
                  <div v-if="additionalSum > 0">Additional invoices: <strong>{{ payment.currency }} {{ fmtMoney(additionalSum) }}</strong></div>
                  <div>Total paid: <strong>{{ payment.currency }} {{ fmtMoney(paidSum) }}</strong></div>
                  <div :class="balanceDue > 0 ? 'ects-warn' : 'ects-ok'">Balance due: <strong>{{ payment.currency }} {{ fmtMoney(balanceDue) }}</strong></div>
                </div>

                <p v-if="payment.installments.length" class="muted" style="margin:.2rem 0 0; font-size:.75rem;">
                  Each installment's enabled payment methods are printed on its invoice PDF (hidden once paid).
                  The full invoice shows the next unpaid installment's methods. Save the plan to apply.
                </p>

                <div style="margin-top:.75rem; display:flex; align-items:center; gap:.6rem;">
                  <button class="btn-row-details btn-row-details-sm" :disabled="payment.saving" @click="savePayment">
                    {{ payment.saving ? 'Saving…' : 'Save payment plan' }}
                  </button>
                  <a v-if="payment.installments.length" class="pay-invoice-link" :class="{ disabled: payment.downloadingInvoice }" @click="downloadInvoice()">
                    {{ payment.downloadingInvoice ? 'Preparing…' : '⤓ Download full invoice' }}
                  </a>
                  <span v-if="payment.error" class="err-banner" style="display:inline-block;">{{ payment.error }}</span>
                  <span v-else-if="payment.ok" class="ok-banner" style="display:inline-block;">Saved</span>
                </div>
              </template>
            </div>

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
                    <span v-if="d.aiConfidence != null || d.aiFraudRisk != null" class="ai-badge"
                          :style="{ background: aiColor(d) }"
                          :title="`Confidence ${fmtScore(d.aiConfidence)} · Fraud indicator ${fmtScore(d.aiFraudRisk)}`">(AI)</span>
                    <span v-else-if="d.aiScannable === false" class="ai-badge ai-badge-none"
                          title="Not scanned by AI: this document is generated by the system itself.">(AI)</span>
                    <span v-else class="ai-badge" style="background:#1a6fd4;"
                          title="Not scanned yet — queued for the background AI scan.">(AI)</span>
                    
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
                    <span v-if="d.aiConfidence != null || d.aiFraudRisk != null" class="ai-badge"
                          :style="{ background: aiColor(d) }"
                          :title="`Confidence ${fmtScore(d.aiConfidence)} · Fraud indicator ${fmtScore(d.aiFraudRisk)}`">(AI)</span>
                    <span v-else-if="d.aiScannable === false" class="ai-badge ai-badge-none"
                          title="Not scanned by AI: this document is generated by the system itself.">(AI)</span>
                    <span v-else class="ai-badge" style="background:#1a6fd4;"
                          title="Not scanned yet — queued for the background AI scan.">(AI)</span>
                    
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

            <!-- Moodle tab -->
            <div v-if="detailModal.activeTab === 'moodle'" class="tab-pane">
              <div class="moodle-row">
                <div>
                  <div class="moodle-title">Moodle enabled</div>
                  <div class="muted" style="font-size:.78rem;">Whether this student is enabled in the Moodle LMS.</div>
                </div>
                <label class="moodle-toggle">
                  <input type="checkbox" v-model="moodleDraft.enabled" />
                  <span>{{ moodleDraft.enabled ? 'Yes' : 'No' }}</span>
                </label>
              </div>
              <div class="moodle-creds">
                <div class="moodle-field">
                  <label>User for Moodle</label>
                  <input v-model="moodleDraft.username" placeholder="Moodle username" />
                </div>
                <div class="moodle-field">
                  <label>Password for Moodle</label>
                  <input v-model="moodleDraft.password" placeholder="Moodle password" />
                </div>
              </div>
              <div style="margin-top:.75rem;">
                <button class="btn-row-details btn-row-details-sm" :disabled="savingMoodle" @click="saveMoodle">
                  {{ savingMoodle ? 'Saving…' : 'Save Moodle settings' }}
                </button>
                <span v-if="moodleError" class="err-banner" style="display:inline-block;margin-left:.5rem;">{{ moodleError }}</span>
                <span v-else-if="moodleOk" class="ok-banner" style="display:inline-block;margin-left:.5rem;">Saved</span>
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

    <!-- Partner-stage review: admin steps into the partner queue using the
         full partner wizard, but POSTs to the admin-side endpoint so audit
         attributes the action to Admission Office. -->
    <StudentReviewWizard v-if="reviewingStudent && reviewingMode === 'partner-stage'"
      :student="reviewingStudent"
      :review-endpoint="adminReviewEndpoint"
      @close="closeReview" @submitted="onReviewSubmitted" />
    <AdminReviewWizard v-else-if="reviewingStudent" :student="reviewingStudent"
      @close="closeReview" @submitted="onReviewSubmitted" />

    <!-- Export students wizard -->
    <transition name="fade">
      <div v-if="exportModal" class="manage-overlay" @click.self="exportModal = null">
        <div class="manage-modal export-modal">
          <div class="manage-hdr">
            <h3>Export students</h3>
            <button class="drawer-close" @click="exportModal = null">✕</button>
          </div>

          <div class="export-steps">
            <div v-for="(s, i) in EXPORT_STEPS" :key="s.id"
                 :class="['export-step-pill',
                          { active: exportModal.step === i + 1,
                            done: exportModal.step > i + 1 }]">
              <span class="export-step-num">{{ i + 1 }}</span>
              <span class="export-step-label">{{ s.label }}</span>
            </div>
          </div>

          <div class="manage-body">
            <p v-if="exportModal.error" class="err-banner">{{ exportModal.error }}</p>

            <!-- Step 1: Partners -->
            <div v-if="exportModal.step === 1" class="export-section">
              <label class="export-radio"><input type="radio" value="all" v-model="exportModal.partnersMode" /> All partners</label>
              <label class="export-radio"><input type="radio" value="pick" v-model="exportModal.partnersMode" /> Pick specific partners</label>
              <div v-if="exportModal.partnersMode === 'pick'" class="export-chip-list" style="margin-top:.5rem;">
                <label v-for="p in exportPartners" :key="p.partnerId" class="export-chip">
                  <input type="checkbox" :value="p.partnerId"
                         :checked="exportModal.selectedPartnerIds.includes(p.partnerId)"
                         @change="togglePartner(p.partnerId, $event.target.checked)" />
                  {{ p.name }}
                </label>
              </div>
            </div>

            <!-- Step 2: Status -->
            <div v-if="exportModal.step === 2" class="export-section">
              <p class="export-help" style="margin-bottom:.5rem;">Tick none to include every status.</p>
              <div class="export-chip-list">
                <label v-for="f in STATUS_FILTERS.filter(x => x.id !== '' && x.id !== 'action-required' && !x.overdue)" :key="f.id"
                       class="export-chip">
                  <input type="checkbox" :value="f.id"
                         :checked="exportModal.selectedStatusFilters.includes(f.id)"
                         @change="toggleStatusFilter(f.id, $event.target.checked)" />
                  {{ f.label }}
                </label>
              </div>
            </div>

            <!-- Step 3: Fields -->
            <div v-if="exportModal.step === 3" class="export-section">
              <div v-for="g in EXPORT_FIELD_GROUPS" :key="g.id" class="export-field-group">
                <label class="export-group-toggle">
                  <input type="checkbox"
                         :checked="groupAllSelected(g)"
                         :indeterminate.prop="groupSomeSelected(g)"
                         @change="toggleGroup(g, $event.target.checked)" />
                  <strong>{{ g.label }}</strong>
                </label>
                <div class="export-field-list">
                  <label v-for="f in g.fields" :key="f.id" class="export-field-check">
                    <input type="checkbox" :value="f.id"
                           :checked="exportModal.selectedFields.includes(f.id)"
                           @change="toggleField(f.id, $event.target.checked)" />
                    {{ f.label }}
                  </label>
                </div>
              </div>
            </div>

            <!-- Step 4: Format -->
            <div v-if="exportModal.step === 4" class="export-section">
              <label class="export-radio"><input type="radio" value="xlsx" v-model="exportModal.format" /> Excel (.xlsx)</label>
              <label class="export-radio"><input type="radio" value="csv" v-model="exportModal.format" /> CSV</label>
            </div>

            <!-- Step 5: Review & Download -->
            <div v-if="exportModal.step === 5" class="export-section">
              <div class="export-review-summary">
                <div><strong>{{ exportModal.sample?.count ?? exportModal.previewCount ?? '—' }}</strong> students</div>
                <div><strong>{{ exportModal.selectedFields.length }}</strong> columns</div>
                <div>Format: <strong>{{ exportModal.format === 'xlsx' ? 'Excel (.xlsx)' : 'CSV' }}</strong></div>
              </div>
              <p v-if="exportModal.sampleLoading" class="muted">Loading preview…</p>
              <p v-else-if="!exportModal.sample?.rows?.length" class="muted">No rows match the current scope.</p>
              <template v-else>
                <p class="export-help">Preview — first {{ exportModal.sample.rows.length }} of {{ exportModal.sample.count }} rows:</p>
                <div class="export-preview-table-wrap">
                  <table class="export-preview-table">
                    <thead>
                      <tr>
                        <th v-for="c in exportModal.sample.columns" :key="c.id">{{ c.header }}</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr v-for="(row, i) in exportModal.sample.rows" :key="i">
                        <td v-for="c in exportModal.sample.columns" :key="c.id">{{ formatCell(row[c.id]) }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
                <p v-if="exportModal.sample.count > exportModal.sample.rows.length" class="export-help">
                  … and {{ exportModal.sample.count - exportModal.sample.rows.length }} more rows in the full export.
                </p>
              </template>
            </div>

            <div class="manage-footer export-footer">
              <span class="export-count">
                <template v-if="exportModal.previewLoading">Calculating…</template>
                <template v-else-if="exportModal.previewCount != null">
                  {{ exportModal.previewCount }} student{{ exportModal.previewCount === 1 ? '' : 's' }} match
                </template>
              </span>
              <button v-if="exportModal.step > 1" class="btn-link" @click="goExportStep(exportModal.step - 1)">← Back</button>
              <button v-else class="btn-link" @click="exportModal = null">Cancel</button>
              <button v-if="exportModal.step < EXPORT_STEPS.length" class="btn-confirm-manage btn-approve-final"
                      :disabled="!canAdvanceExport"
                      @click="goExportStep(exportModal.step + 1)">
                Next →
              </button>
              <button v-else class="btn-confirm-manage btn-approve-final"
                      :disabled="exportModal.exporting || (exportModal.sample?.count ?? 0) === 0"
                      @click="runExport">
                {{ exportModal.exporting ? 'Building…' : '📥 Download' }}
              </button>
            </div>
          </div>
        </div>
      </div>
    </transition>

    <transition name="fade">
      <div v-if="reviewToast" class="review-toast">{{ reviewToast }}</div>
    </transition>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch, reactive, nextTick } from 'vue'
import Fuse from 'fuse.js'
import api from '../../api/client.js'
import { auth } from '../../store/auth.js'
import AdminReviewWizard from './AdminReviewWizard.vue'
import StudentReviewWizard from '../partner/StudentReviewWizard.vue'
import EnrollmentActivityLog from '../letters/EnrollmentActivityLog.vue'
import AdditionalDocumentUploadDialog from '../letters/AdditionalDocumentUploadDialog.vue'
import { ACCEPTED_DOC_ACCEPT_ATTR } from '../../utils/uploadPolicy.js'

const props = defineProps({
  partnerId: { type: String, default: '' },
})
const emit = defineEmits(['add-student'])

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
  // Not a status: the signup wizard was started but never finished
  // (flag computed by the list endpoint from Student.WizardStep).
  { id: 'signing-up',                label: 'Signing up',                  codes: null, signingUp: true },
  { id: 'awaiting-student-accept',   label: 'Awaiting Student Acceptance', codes: ['AcceptOffer'] },
  { id: 'admitted',                  label: 'Admitted',                    codes: ['ApplicationApprovedAdmission', 'AcceptAdmission'] },
  { id: 'awaiting-grades-submit',    label: 'Awaiting Grades Submit',      codes: ['AwaitingGradesSubmit'] },
  { id: 'graduated',                 label: 'Graduated',                   codes: ['GradesApproved'] },
  // Not a status: any enrolment with an unpaid installment / additional
  // invoice past its due date (flag computed by the list endpoint).
  { id: 'payment-overdue',           label: 'Payment overdue',             codes: null, overdue: true },
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

// Admin can review when the enrolment is in either review queue: the
// partner stage (ApplicationSubmitted / ApplicationAwaitingReviewByPartner)
// — where admin steps in on behalf of a slow partner — or the admission
// stage (ApplicationAwaitingReviewByAdmission). Disabled otherwise so
// admin can't approve a previously-rejected doc directly.
function canAdminReview(e) {
  return e.statusCode === 'ApplicationAwaitingReviewByAdmission'
    || e.statusCode === 'ApplicationSubmitted'
    || e.statusCode === 'ApplicationAwaitingReviewByPartner'
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
  { id: 'details',     label: 'Details' },
  { id: 'programs',    label: 'Programs' },
  { id: 'documents',   label: 'Documents' },
  { id: 'moodle',      label: 'Moodle' },
  { id: 'activity',    label: 'Activity log' },
]
const ALL_LETTER_TYPES = [
  { key: 'offerLetter',            label: 'Offer Letter',           icon: '📄' },
  { key: 'admissionLetter',        label: 'Admission Letter',       icon: '📋' },
  { key: 'transcript',             label: 'Digital Transcript',     icon: '📑' },
  { key: 'printableTranscript',    label: 'Printable Transcript',   icon: '📑' },
  { key: 'certificate',            label: 'Digital Certificate',    icon: '🎓' },
  { key: 'provisionalCertificate', label: 'Printable Cert',         icon: '🎓' },
  // Only for programmes with the digital-student-card toggle on.
  { key: 'studentIdCard',          label: 'Student ID Card',        icon: '🪪', requiresCardToggle: true },
]
const LETTER_TYPES = computed(() => ALL_LETTER_TYPES.filter(t =>
  !t.requiresCardToggle || activeEnrollment.value?.issueDigitalStudentCard))
const PROGRAM_SUBTABS = [
  { id: 'enrolment', label: 'Enrolment' },
  { id: 'grades',    label: 'Grades' },
  { id: 'letters',   label: 'Letters' },
  { id: 'payment',   label: 'Payment' },
]
const programSubTab = ref('enrolment')

// ── Add another programme to this student (Programs left menu) ──────────────
const addProg = reactive({ open: false, programmeId: '', specializationId: '', specs: [], busy: false, error: '' })
const addProgSpecs = computed(() =>
  addProg.specs.filter(m => m.programmeId === addProg.programmeId && !m.deletedAt))
async function openAddProg() {
  addProg.open = true
  addProg.error = ''
  addProg.programmeId = ''
  addProg.specializationId = ''
  loadEnrolmentProgOptions()
  try {
    const res = await api.get('/v1/school/specializations')
    addProg.specs = res.data.items ?? []
  } catch { addProg.specs = [] }
}
async function saveAddProg() {
  if (!detailModal.value || !addProg.specializationId || addProg.busy) return
  addProg.busy = true
  addProg.error = ''
  try {
    const res = await api.post(`/v1/admin/students/${detailModal.value.studentId}/enrollments`,
      { specializationId: addProg.specializationId })
    addProg.open = false
    await refreshDetailModal()
    if (res.data?.enrollmentId) detailModal.value.activeEnrollmentId = res.data.enrollmentId
  } catch (e) {
    addProg.error = e.response?.data?.error ?? e.message ?? 'Failed to add'
  } finally {
    addProg.busy = false
  }
}

const detailModal = ref(null)
const detailEnrollments = computed(() => detailModal.value?.data?.enrollments ?? [])
const activeEnrollment = computed(() =>
  detailEnrollments.value.find(e => e.studentEnrollmentId === detailModal.value?.activeEnrollmentId)
  ?? detailEnrollments.value[0]
  ?? null
)

// Programs → Grades: mounts the SAME grade editor as the Submit-grades
// dialog, teleported inline. Opened/closed by the watcher below.
function openInlineGrades() {
  if (!detailModal.value || !activeEnrollment.value) return
  const acc = detailModal.value.data?.account ?? {}
  const s = { studentId: detailModal.value.studentId, firstName: acc.firstName, lastName: acc.lastName }
  const e = activeEnrollment.value
  // Match the editor mode to the enrolment's stage, mirroring the list flow:
  // pre-submission statuses get the editable submit form; Awaiting grades
  // approval gets the approve/reject review; approved (or anything else)
  // gets a read-only view so a finished enrolment can't be re-submitted.
  if (e.statusCode === 'AwaitingGradesApproval') {
    openGradeReview(s, e)
  } else {
    // Editable everywhere else — including after approval: the Admission
    // Office may correct scores at any time (draft save has no status gate).
    // Only the formal Submit transition is hidden once it no longer applies.
    openGradeSubmit(s, e)
  }
  if (gradeModal.value) {
    gradeModal.value.inline = true
    const SUBMITTABLE = ['AcceptOffer', 'ApplicationApprovedAdmission', 'AcceptAdmission', 'AwaitingGradesSubmit']
    if (!SUBMITTABLE.includes(e.statusCode) && e.statusCode !== 'AwaitingGradesApproval')
      gradeModal.value.postApproval = true
  }
}
watch(() => [detailModal.value?.activeTab, programSubTab.value, activeEnrollment.value?.studentEnrollmentId],
  ([tab, sub]) => {
    if (tab === 'programs' && sub === 'grades' && detailModal.value) openInlineGrades()
    else if (gradeModal.value?.inline) gradeModal.value = null
  })


const approvedDurationDraft = computed({
  get() {
    return activeEnrollment.value?.approvedDurationMonths
      ?? activeEnrollment.value?.durationOfStudyMonths
      ?? null
  },
  set(v) {
    if (activeEnrollment.value) activeEnrollment.value.approvedDurationMonths = v
  },
})
const savingDuration = ref(false)
const durationSaveError = ref('')
const durationSaveOk = ref(false)

// Commencement (start date) override, gated to the same top admin levels as
// duration since it also shifts the expected completion date.
const commencementDraft = computed({
  get() { return activeEnrollment.value?.commencementDate?.slice(0, 10) ?? '' },
  set(v) { if (activeEnrollment.value) activeEnrollment.value.commencementDate = v },
})
const savingCommencement = ref(false)
const commencementSaveError = ref('')
const commencementSaveOk = ref(false)

// Per-enrolment programme + specialization change. Admin edit, gated to the
// top two admin levels like the other enrolment overrides.
const canEditSpecialization = computed(() =>
  ['SuperAdministrator', 'Administrator'].includes(auth.adminLevel))
const enrolmentProgOptions = ref([])
const enrolmentSpecOptions = ref([])
const programmeDraft = ref('')
const specializationDraft = ref('')
const savingSpecialization = ref(false)
const specializationSaveError = ref('')
const specializationSaveOk = ref(false)

// Programmes the student's partner can be enrolled in: MGW core programmes
// plus the partner's own custom programmes. The backend re-validates on save.
async function loadEnrolmentProgOptions() {
  const partnerId = detailModal.value?.data?.partner?.partnerId
  try {
    const [coreRes, partnerRes] = await Promise.all([
      api.get('/v1/school/programmes', { params: { ownership: 'core' } }),
      api.get('/v1/school/programmes', { params: { ownership: 'partner' } }).catch(() => ({ data: { items: [] } })),
    ])
    const core = (coreRes.data.items ?? []).filter(p => !p.deletedAt)
    const owned = (partnerRes.data.items ?? []).filter(p => !p.deletedAt && p.ownerId === partnerId)
    const byId = new Map()
    for (const p of [...core, ...owned]) byId.set(p.programmeId, {
      programmeId: p.programmeId,
      name: p.name,
      schoolName: p.schoolName ?? null,
      code: p.code ?? null,
    })
    const opts = Array.from(byId.values()).sort((a, b) => (a.name ?? '').localeCompare(b.name ?? ''))
    // Label is "name (school)"; the code is appended only when two options
    // would otherwise read identically (e.g. duplicate MBAs at one school),
    // since codes usually just repeat the school and double it up visually.
    const keyOf = p => `${(p.name ?? '').toLowerCase()}|${(p.schoolName ?? '').toLowerCase()}`
    const counts = {}
    for (const p of opts) counts[keyOf(p)] = (counts[keyOf(p)] ?? 0) + 1
    for (const p of opts) p.showCode = counts[keyOf(p)] > 1
    enrolmentProgOptions.value = opts
  } catch { enrolmentProgOptions.value = [] }
}

async function loadSpecsForProgramme(programmeId, selectSpecId = null) {
  if (!programmeId) { enrolmentSpecOptions.value = []; return }
  try {
    const res = await api.get('/v1/school/specializations', { params: { programmeId } })
    enrolmentSpecOptions.value = (res.data.items ?? []).map(s => ({ specializationId: s.specializationId, name: s.name }))
  } catch { enrolmentSpecOptions.value = [] }
  // Keep the current selection if it belongs to this programme, else pick first.
  const has = enrolmentSpecOptions.value.some(s => s.specializationId === selectSpecId)
  specializationDraft.value = has ? selectSpecId : (enrolmentSpecOptions.value[0]?.specializationId ?? '')
}

async function loadEnrolmentSpecOptions() {
  const e = activeEnrollment.value
  if (!e?.programmeId) { enrolmentSpecOptions.value = []; enrolmentProgOptions.value = []; return }
  programmeDraft.value = e.programmeId
  await Promise.all([
    loadEnrolmentProgOptions(),
    loadSpecsForProgramme(e.programmeId, e.specializationId),
  ])
}

// When the admin picks a different programme, load its specializations.
function onProgrammeDraftChange() {
  loadSpecsForProgramme(programmeDraft.value)
}

async function saveSpecialization() {
  if (!detailModal.value?.studentId || !activeEnrollment.value || savingSpecialization.value) return
  if (!specializationDraft.value) return
  savingSpecialization.value = true
  specializationSaveError.value = ''
  specializationSaveOk.value = false
  try {
    const res = await api.patch(
      `/v1/admin/students/${detailModal.value.studentId}/enrollments/${activeEnrollment.value.studentEnrollmentId}/specialization`,
      { specializationId: specializationDraft.value })
    activeEnrollment.value.programmeId = res.data.programmeId
    activeEnrollment.value.programmeName = res.data.programmeName
    activeEnrollment.value.specializationId = res.data.specializationId
    activeEnrollment.value.specializationName = res.data.specializationName
    specializationSaveOk.value = true
    setTimeout(() => { specializationSaveOk.value = false }, 2500)
    load() // refresh row labels
    await refreshDetailModal()
  } catch (err) {
    specializationSaveError.value = err.response?.data?.error ?? err.message ?? 'Save failed'
  } finally {
    savingSpecialization.value = false
  }
}

// Offer/admission letter date overrides (Admission Office only).
const offerLetterDateDraft = computed({
  get() { return activeEnrollment.value?.offerLetterDate?.slice(0, 10) ?? '' },
  set(v) { if (activeEnrollment.value) activeEnrollment.value.offerLetterDate = v || null },
})
const admissionLetterDateDraft = computed({
  get() { return activeEnrollment.value?.admissionLetterDate?.slice(0, 10) ?? '' },
  set(v) { if (activeEnrollment.value) activeEnrollment.value.admissionLetterDate = v || null },
})
const transcriptDateDraft = computed({
  get() { return activeEnrollment.value?.transcriptDate?.slice(0, 10) ?? '' },
  set(v) { if (activeEnrollment.value) activeEnrollment.value.transcriptDate = v || null },
})
// Graduation date override. Blank = fall back to the expected completion date
// (handled server-side when rendering letters). Kept independent of the
// expected-completion computation so editing the approved duration never
// clobbers a value the user just typed here.
const graduationDateDraft = computed({
  get() { return activeEnrollment.value?.graduationDate?.slice(0, 10) ?? '' },
  set(v) { if (activeEnrollment.value) activeEnrollment.value.graduationDate = v || null },
})
const savingLetterDates = ref(false)
const letterDatesError = ref('')
const letterDatesOk = ref(false)

// ── Admin status change (re-open a rejected enrolment, etc.) ─────────────────
const enrollmentStatuses = ref([])
const statusEdit = ref(false)
const statusDraft = ref(null)
const statusNote = ref('')
const savingStatus = ref(false)
const statusError = ref('')

async function openStatusEdit() {
  statusError.value = ''
  statusNote.value = ''
  statusDraft.value = null
  statusEdit.value = true
  if (enrollmentStatuses.value.length === 0) {
    try {
      const res = await api.get('/v1/admin/enrollment-statuses')
      enrollmentStatuses.value = res.data.items ?? []
    } catch (err) {
      statusError.value = err.response?.data?.error ?? err.message ?? 'Failed to load statuses'
    }
  }
  // Preselect the current status.
  const cur = enrollmentStatuses.value.find(s => s.code === activeEnrollment.value?.statusCode)
  statusDraft.value = cur?.statusId ?? enrollmentStatuses.value[0]?.statusId ?? null
}

async function saveStatus() {
  if (!detailModal.value?.studentId || !activeEnrollment.value || !statusDraft.value || savingStatus.value) return
  savingStatus.value = true
  statusError.value = ''
  try {
    const res = await api.patch(
      `/v1/admin/students/${detailModal.value.studentId}/enrollments/${activeEnrollment.value.studentEnrollmentId}/status`,
      { statusId: statusDraft.value, note: statusNote.value })
    activeEnrollment.value.statusCode = res.data.statusCode
    activeEnrollment.value.statusName = res.data.statusName
    statusEdit.value = false
    await refreshDetailModal()
    await load()
  } catch (err) {
    statusError.value = err.response?.data?.error ?? err.message ?? 'Failed to change status'
  } finally {
    savingStatus.value = false
  }
}

// ── Moodle tab (per-student LMS enabled + login credentials) ─────────────────
const savingMoodle = ref(false)
const moodleError = ref('')
const moodleOk = ref(false)
// Editable draft, kept in sync with the loaded student so switching students
// or reopening the modal shows that student's Moodle settings.
const moodleDraft = reactive({ enabled: false, username: '', password: '' })
watch(() => detailModal.value?.data, (d) => {
  moodleDraft.enabled = !!d?.moodleEnabled
  moodleDraft.username = d?.moodleUsername ?? ''
  moodleDraft.password = d?.moodlePassword ?? ''
  statusEdit.value = false
}, { immediate: true })

// ── Module cohorts (Enrolment sub-tab) — module scheduling lives in the
// Module Cohort Schedule; per student only the cohort pick per module remains.
const studentCohorts = reactive({ bySubject: {}, error: '' })
async function loadStudentCohorts() {
  const m = detailModal.value
  const enr = activeEnrollment.value
  if (!m || !enr) return
  studentCohorts.error = ''
  try {
    const res = await api.get(`/v1/admin/students/${m.studentId}/enrollments/${enr.studentEnrollmentId}/cohorts`)
    const map = {}
    for (const mod of res.data.modules ?? []) map[mod.subjectId] = mod
    studentCohorts.bySubject = map
  } catch { studentCohorts.bySubject = {} }
}
async function setStudentCohort(subjectId, cohortId) {
  const m = detailModal.value
  const enr = activeEnrollment.value
  if (!m || !enr) return
  try {
    await api.put(`/v1/admin/students/${m.studentId}/enrollments/${enr.studentEnrollmentId}/cohorts`, {
      subjectId, cohortId: cohortId || null,
    })
    await loadStudentCohorts()
  } catch (e) {
    studentCohorts.error = e.response?.data?.error ?? e.message ?? 'Failed to set cohort'
  }
}

watch(() => [detailModal.value?.activeTab, programSubTab.value, activeEnrollment.value?.studentEnrollmentId],
  ([tab, sub]) => {
    if (tab === 'programs' && sub === 'enrolment') loadStudentCohorts()
  })

// ── Payment tab (per-enrolment tuition plan + invoice) ───────────────────────
const payment = reactive({
  exists: false, total: 0, currency: 'USD', count: 1, installments: [], additional: [],
  saving: false, downloadingInvoice: false, error: '', ok: '',
})
// Configurable currency list (System Config → Currencies). Falls back to a
// minimal default if the list can't be loaded. Always includes the currently
// selected currency so a saved value never disappears from the dropdown.
const currencyList = ref([{ code: 'USD' }])
const currencyOptions = computed(() => {
  const list = [...currencyList.value]
  if (payment.currency && !list.some(c => c.code === payment.currency)) list.unshift({ code: payment.currency })
  return list
})
async function loadCurrencies() {
  try {
    const res = await api.get('/v1/school/currencies/options')
    if (res.data.items?.length) currencyList.value = res.data.items
  } catch { /* keep fallback */ }
}

function aiTotal(ai) { return (ai.lines || []).reduce((s, l) => s + (Number(l.amount) || 0), 0) }
const additionalSum = computed(() => payment.additional.reduce((s, ai) => s + aiTotal(ai), 0))
const paidSum = computed(() =>
  payment.installments.filter(i => i.isPaid).reduce((s, i) => s + (Number(i.amount) || 0), 0)
  + payment.additional.filter(ai => ai.isPaid).reduce((s, ai) => s + aiTotal(ai), 0))
const balanceDue = computed(() => (Number(payment.total) || 0) + additionalSum.value - paidSum.value)
function fmtMoney(v) { return (Number(v) || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }

// Load the plan whenever the Payment tab is opened for the active enrolment.
watch(() => [detailModal.value?.activeTab, programSubTab.value, activeEnrollment.value?.studentEnrollmentId], async ([tab, sub, eid]) => {
  if (!(tab === 'programs' && sub === 'payment') || !eid || !detailModal.value?.studentId) return
  payment.error = ''
  if (currencyList.value.length <= 1) loadCurrencies()
  try {
    const res = await api.get(`/v1/admin/students/${detailModal.value.studentId}/enrollments/${eid}/payment`)
    const d = res.data
    payment.exists = !!d.exists
    payment.total = d.totalTuitionFee ?? 0
    payment.currency = d.currency ?? 'USD'
    payment.count = d.numberOfPayments || 1
    payment.installments = (d.installments ?? []).map(i => ({
      amount: i.amount, dueDate: i.dueDate?.slice(0, 10) ?? '', isPaid: !!i.isPaid, paidDate: i.paidDate?.slice(0, 10) ?? '',
      payByCard: !!i.payByCardEnabled, cardPaymentLink: i.cardPaymentLink ?? '',
      payByBank: !!i.payByBankEnabled, bankAccountDetails: i.bankAccountDetails ?? '',
    }))
    payment.additional = (d.additionalInvoices ?? []).map(a => ({
      lines: (a.lines ?? []).map(l => ({ text: l.text ?? '', amount: l.amount ?? 0 })),
      dueDate: a.dueDate?.slice(0, 10) ?? '', isPaid: !!a.isPaid, paidDate: a.paidDate?.slice(0, 10) ?? '',
      payByCard: !!a.payByCardEnabled, cardPaymentLink: a.cardPaymentLink ?? '',
      payByBank: !!a.payByBankEnabled, bankAccountDetails: a.bankAccountDetails ?? '',
    }))
    payment.additional.forEach(a => { if (!a.lines.length) a.lines.push({ text: '', amount: 0 }) })
  } catch (err) {
    payment.error = err.response?.data?.error ?? err.message ?? 'Failed to load'
  }
})

// Auto-split the total into `count` equal installments (last absorbs rounding),
// due monthly from commencement (or today). Existing rows are replaced.
function generateSchedule() {
  const n = Math.max(1, Number(payment.count) || 1)
  const total = Number(payment.total) || 0
  const per = Math.floor((total / n) * 100) / 100
  const start = activeEnrollment.value?.commencementDate ? new Date(activeEnrollment.value.commencementDate) : new Date()
  const rows = []
  let allocated = 0
  for (let k = 0; k < n; k++) {
    const amount = k === n - 1 ? Math.round((total - allocated) * 100) / 100 : per
    allocated += per
    const due = new Date(start)
    due.setMonth(due.getMonth() + k)
    rows.push({ amount, dueDate: due.toISOString().slice(0, 10), isPaid: false, paidDate: '',
      payByCard: false, cardPaymentLink: '', payByBank: false, bankAccountDetails: '' })
  }
  payment.installments = rows
}

async function savePayment() {
  if (!detailModal.value?.studentId || !activeEnrollment.value || payment.saving) return
  payment.saving = true; payment.error = ''; payment.ok = ''
  try {
    const body = {
      totalTuitionFee: Number(payment.total) || 0,
      currency: payment.currency,
      installments: payment.installments.map((i, idx) => ({
        sequence: idx + 1,
        amount: Number(i.amount) || 0,
        dueDate: i.dueDate || null,
        isPaid: !!i.isPaid,
        paidDate: i.isPaid ? (i.paidDate || null) : null,
        payByCardEnabled: !!i.payByCard,
        cardPaymentLink: i.cardPaymentLink || null,
        payByBankEnabled: !!i.payByBank,
        bankAccountDetails: i.bankAccountDetails || null,
      })),
      additionalInvoices: payment.additional.map((a, idx) => ({
        sequence: idx + 1,
        dueDate: a.dueDate || null,
        isPaid: !!a.isPaid,
        paidDate: a.isPaid ? (a.paidDate || null) : null,
        payByCardEnabled: !!a.payByCard,
        cardPaymentLink: a.cardPaymentLink || null,
        payByBankEnabled: !!a.payByBank,
        bankAccountDetails: a.bankAccountDetails || null,
        lines: (a.lines || []).map(l => ({ text: l.text || '', amount: Number(l.amount) || 0 })),
      })),
    }
    const res = await api.put(
      `/v1/admin/students/${detailModal.value.studentId}/enrollments/${activeEnrollment.value.studentEnrollmentId}/payment`, body)
    payment.exists = !!res.data.exists
    payment.ok = 'Saved'
    setTimeout(() => { payment.ok = '' }, 2500)
    // Refresh the students list in the background so the "Payment overdue"
    // chip/badge reflects the new due dates / paid flags without a page reload.
    load()
  } catch (err) {
    payment.error = err.response?.data?.error ?? err.message ?? 'Save failed'
  } finally {
    payment.saving = false
  }
}

// New additional invoice: always starts with one line (amount + description).
function addAdditionalInvoice() {
  payment.additional.push({
    lines: [{ text: '', amount: 0 }],
    dueDate: '', isPaid: false, paidDate: '',
    payByCard: false, cardPaymentLink: '', payByBank: false, bankAccountDetails: '',
  })
}
function removeAdditionalInvoice(idx) {
  payment.additional.splice(idx, 1)
}

// seq = installment number for a single-installment invoice; omitted = full plan.
async function downloadInvoice(seq = null, additionalSeq = null) {
  if (!detailModal.value?.studentId || !activeEnrollment.value || payment.downloadingInvoice) return
  // Persist any unsaved edits first so the PDF always matches the screen
  // (amounts, paid flags, payment links / bank details).
  await savePayment()
  if (payment.error) return
  payment.downloadingInvoice = true; payment.error = ''
  try {
    const q = seq ? `?installment=${seq}` : additionalSeq ? `?additional=${additionalSeq}` : ''
    const res = await api.get(
      `/v1/admin/students/${detailModal.value.studentId}/enrollments/${activeEnrollment.value.studentEnrollmentId}/payment/invoice${q}`,
      { responseType: 'blob' })
    openBlobPdf(res.data, seq ? `invoice-installment-${seq}.pdf` : additionalSeq ? `invoice-additional-${additionalSeq}.pdf` : 'invoice.pdf')
  } catch (err) {
    payment.error = err.response?.status === 404 ? 'Save the payment plan first.' : (err.response?.data?.error ?? err.message ?? 'Download failed')
  } finally {
    payment.downloadingInvoice = false
  }
}
function downloadAdditionalInvoice(seq) { return downloadInvoice(null, seq) }

async function saveMoodle() {
  if (!detailModal.value?.studentId || savingMoodle.value) return
  savingMoodle.value = true
  moodleError.value = ''
  moodleOk.value = false
  try {
    const res = await api.patch(
      `/v1/admin/students/${detailModal.value.studentId}/moodle`,
      { enabled: moodleDraft.enabled, username: moodleDraft.username, password: moodleDraft.password })
    if (detailModal.value?.data) {
      detailModal.value.data.moodleEnabled = res.data.moodleEnabled
      detailModal.value.data.moodleUsername = res.data.moodleUsername
      detailModal.value.data.moodlePassword = res.data.moodlePassword
    }
    moodleOk.value = true
    setTimeout(() => { moodleOk.value = false }, 2500)
  } catch (err) {
    moodleError.value = err.response?.data?.error ?? err.message ?? 'Save failed'
  } finally {
    savingMoodle.value = false
  }
}

async function saveLetterDates() {
  if (!detailModal.value?.studentId || !activeEnrollment.value || savingLetterDates.value) return
  savingLetterDates.value = true
  letterDatesError.value = ''
  letterDatesOk.value = false
  try {
    const res = await api.patch(
      `/v1/admin/students/${detailModal.value.studentId}/enrollments/${activeEnrollment.value.studentEnrollmentId}/letter-dates`,
      {
        offerLetterDate: offerLetterDateDraft.value || null,
        admissionLetterDate: admissionLetterDateDraft.value || null,
        transcriptDate: transcriptDateDraft.value || null,
        graduationDate: graduationDateDraft.value || null,
      })
    activeEnrollment.value.offerLetterDate = res.data.offerLetterDate
    activeEnrollment.value.admissionLetterDate = res.data.admissionLetterDate
    activeEnrollment.value.transcriptDate = res.data.transcriptDate
    activeEnrollment.value.graduationDate = res.data.graduationDate
    letterDatesOk.value = true
    setTimeout(() => { letterDatesOk.value = false }, 2500)
    await refreshDetailModal()
  } catch (err) {
    letterDatesError.value = err.response?.data?.error ?? err.message ?? 'Save failed'
  } finally {
    savingLetterDates.value = false
  }
}

// Per-enrolment teaching-language override (blank = programme default).
const teachingLanguageDraft = computed({
  get() { return activeEnrollment.value?.instructionLanguageOverride ?? '' },
  set(v) { if (activeEnrollment.value) activeEnrollment.value.instructionLanguageOverride = v },
})
const savingLanguage = ref(false)
const languageSaveError = ref('')
const languageSaveOk = ref(false)

async function saveTeachingLanguage() {
  if (!detailModal.value?.studentId || !activeEnrollment.value || savingLanguage.value) return
  savingLanguage.value = true
  languageSaveError.value = ''
  languageSaveOk.value = false
  try {
    const res = await api.patch(
      `/v1/admin/students/${detailModal.value.studentId}/enrollments/${activeEnrollment.value.studentEnrollmentId}/teaching-language`,
      { instructionLanguageOverride: (teachingLanguageDraft.value || '').trim() || null })
    activeEnrollment.value.instructionLanguageOverride = res.data.instructionLanguageOverride
    // Reflect the new effective value in the read-only display fields.
    activeEnrollment.value.instructionLanguage = res.data.instructionLanguageOverride || activeEnrollment.value.instructionLanguage
    languageSaveOk.value = true
    setTimeout(() => { languageSaveOk.value = false }, 2500)
    await refreshDetailModal()
  } catch (err) {
    languageSaveError.value = err.response?.data?.error ?? err.message ?? 'Save failed'
  } finally {
    savingLanguage.value = false
  }
}
// ── Mode of study (Admission-Office editable, like study language) ──────────
const modeOptions = ref([])
const modeDraft = ref(null)
const savingMode = ref(false)
const modeSaveError = ref('')
const modeSaveOk = ref(false)

async function loadModeOptions() {
  if (modeOptions.value.length) return
  try {
    const res = await api.get('/v1/school/system-config/modes-of-study')
    modeOptions.value = res.data.items ?? []
  } catch { /* dropdown stays empty; read-only name still shows for non-editors */ }
}

watch(() => [activeEnrollment.value?.studentEnrollmentId, activeEnrollment.value?.modeOfStudyId], () => {
  modeDraft.value = activeEnrollment.value?.modeOfStudyId ?? null
  if (activeEnrollment.value) loadModeOptions()
}, { immediate: true })

async function saveModeOfStudy() {
  if (!detailModal.value?.studentId || !activeEnrollment.value || savingMode.value || !modeDraft.value) return
  savingMode.value = true
  modeSaveError.value = ''
  modeSaveOk.value = false
  try {
    const res = await api.patch(
      `/v1/admin/students/${detailModal.value.studentId}/enrollments/${activeEnrollment.value.studentEnrollmentId}/mode-of-study`,
      { modeOfStudyId: modeDraft.value })
    activeEnrollment.value.modeOfStudyId = res.data.modeOfStudyId
    activeEnrollment.value.modeOfStudyName = res.data.modeOfStudyName
    modeSaveOk.value = true
    setTimeout(() => { modeSaveOk.value = false }, 2500)
    await refreshDetailModal()
  } catch (err) {
    modeSaveError.value = err.response?.data?.error ?? err.message ?? 'Save failed'
  } finally {
    savingMode.value = false
  }
}

const commencementPastWarning = computed(() => {
  const v = commencementDraft.value
  if (!v) return ''
  return v < new Date().toISOString().slice(0, 10) ? 'This start date is in the past.' : ''
})
const showRegenOffer = ref(false)
const regeneratingLetters = ref(false)
const regenResult = ref('')
// Per-row regenerate (Letters tab): which letter key is currently rebuilding.
const regeneratingLetterKey = ref('')
const letterRegenResult = ref('')
// Offer/Admission letters can also email the student (PDF attached).
const EMAILABLE_KEYS = ['offerLetter', 'admissionLetter']
const emailSend = ref({ open: false, key: '', label: '', cc: '', bcc: '', sending: false, error: '', ok: '' })

// Duration override is reserved for the top two admin levels: changing it
// shifts an admitted student's completion date.
const canEditDuration = computed(() =>
  ['SuperAdministrator', 'Administrator'].includes(auth.adminLevel))

// Deleting an applicant is destructive, so it matches the backend gate:
// Administrator and SuperAdministrator only.
const canDeleteStudent = canEditDuration
const deletingStudentId = ref(null)

// Manual Student ID + Old-student flag — Admission Office (Administrator+) only,
// matching the backend gate.
const canEditLegacyId = canEditDuration
const legacyDraft = reactive({ isLegacy: false, studentNumber: '' })
const savingLegacy = ref(false)
const legacyError = ref('')
const legacyOk = ref(false)

async function saveLegacyId() {
  if (!detailModal.value?.studentId || savingLegacy.value) return
  const num = (legacyDraft.studentNumber || '').trim()
  if (legacyDraft.isLegacy && !num) { legacyError.value = 'Enter the student’s existing ID.'; return }
  savingLegacy.value = true
  legacyError.value = ''
  legacyOk.value = false
  try {
    const res = await api.patch(`/v1/admin/students/${detailModal.value.studentId}/legacy-id`, {
      studentNumber: legacyDraft.isLegacy ? num : detailModal.value.studentNumber,
      isLegacyStudent: legacyDraft.isLegacy,
    })
    detailModal.value.studentNumber = res.data.studentNumber
    if (detailModal.value.data) detailModal.value.data.isLegacyStudent = res.data.isLegacyStudent
    legacyOk.value = true
    setTimeout(() => { legacyOk.value = false }, 2500)
    load() // refresh the row's student number in the list
  } catch (err) {
    legacyError.value = err.response?.data?.error ?? err.message ?? 'Save failed'
  } finally {
    savingLegacy.value = false
  }
}

async function deleteStudent(s) {
  if (deletingStudentId.value) return
  const name = `${s.firstName ?? ''} ${s.lastName ?? ''}`.trim() || s.studentNumber
  if (!confirm(
    `Remove applicant "${name}" (${s.studentNumber})?\n\n`
    + 'This removes the student, all their enrolments, and disables their login. '
    + 'Use this only for wrongly-created applicants. Continue?')) return
  deletingStudentId.value = s.studentId
  loadError.value = ''
  try {
    await api.delete(`/v1/admin/students/${s.studentId}`)
    // Drop it from the current list without a full round-trip; load() also
    // refreshes the chip counts.
    list.value = list.value.filter(x => x.studentId !== s.studentId)
    if (detailModal.value?.studentId === s.studentId) detailModal.value = null
    await load()
  } catch (err) {
    loadError.value = err.response?.data?.error ?? err.message ?? 'Failed to delete student'
  } finally {
    deletingStudentId.value = null
  }
}

// Regenerating a released letter re-renders its PDF with current data, so it
// is gated to the same top two admin levels as the duration override.
const canRegenerateLetters = canEditDuration

// Admins may save outside the programme range; warn but don't block.
const durationRangeWarning = computed(() => {
  const e = activeEnrollment.value
  const v = approvedDurationDraft.value
  if (!e || !v || !e.programmeMaxDurationMonths) return ''
  if (v < e.programmeMinDurationMonths || v > e.programmeMaxDurationMonths)
    return `Outside the programme range (${e.programmeMinDurationMonths}–${e.programmeMaxDurationMonths} months). You can still save.`
  return ''
})

const expectedCompletion = computed(() => {
  const e = activeEnrollment.value
  const months = e?.approvedDurationMonths ?? e?.durationOfStudyMonths
  if (!e?.commencementDate || !months) return ''
  const d = new Date(e.commencementDate)
  d.setMonth(d.getMonth() + months)
  d.setDate(d.getDate() - 1) // last day of study, not the first day after
  return formatDate(d.toISOString())
})

// Level 300 = Approved by Admission; anything at or past it is "admitted".
const isAdmittedOrLater = computed(() => (activeEnrollment.value?.statusLevel ?? 0) >= 300)
const hasReleasedLetters = computed(() => {
  const letters = activeEnrollment.value?.letters
  return !!letters && Object.values(letters).some(Boolean)
})

async function saveApprovedDuration() {
  if (!detailModal.value?.studentId || !activeEnrollment.value) return
  if (isAdmittedOrLater.value) {
    const ok = confirm(
      'This student is already admitted.\n\n'
      + 'Changing the duration moves the expected completion date, and any letters '
      + 'released earlier (offer, admission, transcript, certificate) will still show '
      + 'the old date until regenerated.\n\nContinue?')
    if (!ok) return
  }
  savingDuration.value = true
  durationSaveError.value = ''
  durationSaveOk.value = false
  regenResult.value = ''
  try {
    await api.patch(
      `/v1/admin/students/${detailModal.value.studentId}/enrollments/${activeEnrollment.value.studentEnrollmentId}/duration`,
      { approvedDurationMonths: activeEnrollment.value.approvedDurationMonths })
    durationSaveOk.value = true
    setTimeout(() => { durationSaveOk.value = false }, 2500)
    showRegenOffer.value = isAdmittedOrLater.value && hasReleasedLetters.value
    await refreshDetailModal()
  } catch (err) {
    durationSaveError.value = err.response?.data?.error ?? err.message ?? 'Save failed'
  } finally {
    savingDuration.value = false
  }
}

async function saveCommencement() {
  if (!detailModal.value?.studentId || !activeEnrollment.value) return
  const v = commencementDraft.value
  if (!v) { commencementSaveError.value = 'Pick a date first.'; return }
  // Backdating is allowed but confirmed so a past start date isn't a typo.
  if (v < new Date().toISOString().slice(0, 10)) {
    const pretty = new Date(v + 'T00:00:00').toLocaleDateString()
    if (!confirm(`The commencement date (${pretty}) is in the past. Save this backdated start date?`)) return
  }
  savingCommencement.value = true
  commencementSaveError.value = ''
  commencementSaveOk.value = false
  regenResult.value = ''
  try {
    await api.patch(
      `/v1/admin/students/${detailModal.value.studentId}/enrollments/${activeEnrollment.value.studentEnrollmentId}/commencement`,
      { commencementDate: v })
    commencementSaveOk.value = true
    setTimeout(() => { commencementSaveOk.value = false }, 2500)
    showRegenOffer.value = isAdmittedOrLater.value && hasReleasedLetters.value
    await refreshDetailModal()
  } catch (err) {
    commencementSaveError.value = err.response?.data?.error ?? err.message ?? 'Save failed'
  } finally {
    savingCommencement.value = false
  }
}

async function regenerateLetters() {
  if (!detailModal.value?.studentId || !activeEnrollment.value) return
  regeneratingLetters.value = true
  regenResult.value = ''
  try {
    const res = await api.post(
      `/v1/admin/students/${detailModal.value.studentId}/enrollments/${activeEnrollment.value.studentEnrollmentId}/letters/regenerate`)
    const types = res.data?.regenerated ?? []
    regenResult.value = types.length
      ? `Regenerated: ${types.join(', ')}`
      : 'Nothing regenerated (no published templates).'
    await refreshDetailModal()
  } catch (err) {
    regenResult.value = err.response?.data?.error ?? err.message ?? 'Regenerate failed'
  } finally {
    regeneratingLetters.value = false
  }
}

// Regenerate a single released letter (per-row button). camelCase key →
// PascalCase LetterType the backend enum expects (offerLetter → OfferLetter).
async function regenerateLetter(t) {
  if (!detailModal.value?.studentId || !activeEnrollment.value) return
  // Works for not-yet-released letters too: the backend release creates the
  // document when the template is published (used to back-fill a missing
  // Printable Cert / Digital Certificate for an already-graduated student).
  const wasReleased = !!activeEnrollment.value.letters?.[t.key]
  regeneratingLetterKey.value = t.key
  letterRegenResult.value = ''
  try {
    const enumName = t.key.charAt(0).toUpperCase() + t.key.slice(1)
    const res = await api.post(
      `/v1/admin/students/${detailModal.value.studentId}/enrollments/${activeEnrollment.value.studentEnrollmentId}/letters/regenerate`,
      null, { params: { letterType: enumName } })
    letterRegenResult.value = (res.data?.regenerated ?? []).length
      ? `${t.label} ${wasReleased ? 'regenerated' : 'generated'}.`
      : `Nothing for ${t.label} (template not published).`
    await refreshDetailModal()
  } catch (err) {
    letterRegenResult.value = err.response?.data?.error ?? err.message ?? 'Regenerate failed'
  } finally {
    regeneratingLetterKey.value = ''
  }
}

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
      `/v1/admin/students/${detailModal.value.studentId}/enrollments/${activeEnrollment.value.studentEnrollmentId}/letters/${enumName}/send-email`,
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

watch(() => detailModal.value?.activeEnrollmentId, () => {
  showRegenOffer.value = false
  regenResult.value = ''
  durationSaveError.value = ''
  specializationSaveError.value = ''
  specializationSaveOk.value = false
  loadEnrolmentSpecOptions()
})

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
    // Seed the legacy-ID editor from the loaded student.
    legacyDraft.isLegacy = !!res.data.isLegacyStudent
    legacyDraft.studentNumber = res.data.studentNumber ?? ''
    legacyError.value = ''
    legacyOk.value = false
    // Pin the active enrolment to the most-actionable / first one returned.
    if (!detailModal.value.activeEnrollmentId && res.data.enrollments?.length) {
      detailModal.value.activeEnrollmentId = res.data.enrollments[0].studentEnrollmentId
    }
    // Load the specialization options now that the enrolment data is present
    // (the activeEnrollmentId watcher can fire before data arrives).
    await loadEnrolmentSpecOptions()
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
  data.background = data.background || { highestDegree: null, degreeSpecialization: null, yearsWorkExperience: 0, languages: [] }
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
      degreeSpecialization: d.background?.degreeSpecialization ?? null,
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
    await loadEnrolmentSpecOptions()
  } catch { /* keep stale view */ }
}

// (AI) badge: score = average of confidence and (1 - fraudRisk); hue runs
// red (0) → green (120). High confidence good, low fraud good.
function aiScore(d) {
  const c = d.aiConfidence != null ? Number(d.aiConfidence) : null
  const f = d.aiFraudRisk != null ? Number(d.aiFraudRisk) : null
  if (c == null && f == null) return null
  const parts = []
  if (c != null) parts.push(c)
  if (f != null) parts.push(1 - f)
  return parts.reduce((a, b) => a + b, 0) / parts.length
}
function aiColor(d) {
  const s = aiScore(d)
  if (s == null) return '#999'
  return `hsl(${Math.round(120 * Math.max(0, Math.min(1, s)))}, 75%, 38%)`
}
function fmtScore(v) { return v == null ? '—' : Number(v).toFixed(2) }

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

// Trigger a download/open of a blob PDF response.
function openBlobPdf(blob, filename) {
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  a.target = '_blank'
  document.body.appendChild(a); a.click(); document.body.removeChild(a)
  setTimeout(() => URL.revokeObjectURL(url), 60_000)
}

// Provisional transcript download from the Letters tab (before the official
// transcript is released). Uses the active enrolment of the open detail modal.
const downloadingLetterProvisional = ref(false)
async function downloadLetterProvisional() {
  if (!detailModal.value?.studentId || !activeEnrollment.value || downloadingLetterProvisional.value) return
  downloadingLetterProvisional.value = true
  try {
    const res = await api.get(
      `/v1/admin/students/${detailModal.value.studentId}/enrollments/${activeEnrollment.value.studentEnrollmentId}/transcript/provisional`,
      { responseType: 'blob' })
    openBlobPdf(res.data, 'provisional-transcript.pdf')
  } catch (err) {
    reviewToast.value = err.response?.status === 404
      ? 'No published transcript template yet, or no grades saved.'
      : (err.response?.data?.error ?? err.message ?? 'Download failed')
    setTimeout(() => { reviewToast.value = '' }, 3500)
  } finally {
    downloadingLetterProvisional.value = false
  }
}

async function downloadLetter(letter) {
  if (!letter?.studentDocumentId || !detailModal.value) return
  try {
    const res = await api.get(
      `/v1/admin/students/${detailModal.value.studentId}/documents/${letter.studentDocumentId}/file`,
      { responseType: 'blob' })
    openBlobPdf(res.data, letter.fileName ?? 'letter.pdf')
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
// "Continue signup": mint a wizard token for the unfinished application and
// open the public wizard in a new tab exactly where the applicant stopped.
async function continueSignup(s) {
  if (s.openingSignup) return
  s.openingSignup = true
  loadError.value = ''
  try {
    const res = await api.post(`/v1/admin/students/${s.studentId}/signup-token`)
    const { wizardToken, partnerSlug } = res.data
    window.open(`/#/apply?partner=${encodeURIComponent(partnerSlug)}&resume=${encodeURIComponent(wizardToken)}`, '_blank')
  } catch (e) {
    loadError.value = e.response?.data?.error ?? e.message ?? 'Could not open the signup wizard'
  } finally {
    s.openingSignup = false
  }
}

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
    requiredEcts: null,
    projectTitle: '',
    mode: 'submit',
    rejectReason: '',
    rejectPreset: '',
    confirmTuitionPaid: false,
    loading: true,
    submitting: false,
    savingDraft: false,
    downloadingProvisional: false,
    error: '',
  })
  try {
    const res = await api.get(`/v1/admin/students/${s.studentId}/enrollments/${e.studentEnrollmentId}/subjects`)
    gradeModal.value.subjects = (res.data.items ?? []).map(r => ({ ...r, score: r.score ?? null }))
    gradeModal.value.requiredEcts = res.data.requiredEcts ?? null
    gradeModal.value.projectTitle = res.data.projectTitle ?? ''
  } catch (err) {
    gradeModal.value.error = err.response?.data?.error ?? err.message ?? 'Failed to load subjects'
  } finally {
    gradeModal.value.loading = false
  }
}

// A subject counts as completed once any score (0-100) is entered.
function scoredRows(m) {
  return (m?.subjects ?? []).filter(r => Number.isInteger(r.score) && r.score >= 0 && r.score <= 100)
}
// Sum of ECTS across completed subjects for the admin grade modal.
const adminCompletedEcts = computed(() =>
  scoredRows(gradeModal.value).reduce((sum, r) => sum + Number(r.ects || 0), 0))
// True once a thesis/dissertation module has a grade — reveals the project-title field.
const adminThesisGraded = computed(() => {
  const m = gradeModal.value
  if (!m?.subjects?.length) return false
  return m.subjects.some(r => r.isThesis && Number.isInteger(r.score) && r.score >= 0 && r.score <= 100)
})
// ECTS still needed to reach the programme's completion threshold (0 if met).
const adminEctsRemaining = computed(() => {
  const req = Number(gradeModal.value?.requiredEcts || 0)
  if (!req) return 0
  return Math.max(0, req - adminCompletedEcts.value)
})
// Submit is gated by the threshold: enough completed ECTS. When no threshold
// is set on the programme, fall back to "at least one subject scored".
const canCommitAdminGrades = computed(() => {
  const m = gradeModal.value
  const scored = scoredRows(m)
  if (!scored.length) return false
  const req = Number(m?.requiredEcts || 0)
  return req ? adminCompletedEcts.value >= req : true
})

// Admin draft-save (no status change), mirroring the partner draft.
async function saveAdminGradesDraft() {
  const m = gradeModal.value
  if (!m || m.savingDraft) return
  m.savingDraft = true; m.error = ''
  try {
    const items = m.subjects.filter(r => Number.isFinite(r.score)).map(r => ({ subjectId: r.subjectId, score: r.score }))
    await api.post(`/v1/admin/students/${m.studentId}/enrollments/${m.enrollmentId}/grades/draft`, { items, projectTitle: m.projectTitle })
    reviewToast.value = `Saved ${items.length} grade(s). Provisional transcript updated.`
    setTimeout(() => { reviewToast.value = '' }, 3000)
  } catch (err) {
    m.error = err.response?.data?.error ?? err.message ?? 'Failed to save grades'
  } finally {
    if (gradeModal.value) gradeModal.value.savingDraft = false
  }
}

// Download the provisional transcript. Auto-saves the on-screen grades first
// so the PDF always matches the editor — no manual Save needed beforehand.
async function downloadAdminProvisional() {
  const m = gradeModal.value
  if (!m || m.downloadingProvisional) return
  await saveAdminGradesDraft()
  if (m.error) return
  m.downloadingProvisional = true; m.error = ''
  try {
    const res = await api.get(
      `/v1/admin/students/${m.studentId}/enrollments/${m.enrollmentId}/transcript/provisional`,
      { responseType: 'blob' })
    openBlobPdf(res.data, 'provisional-transcript.pdf')
  } catch (err) {
    m.error = err.response?.status === 404
      ? 'No published transcript template for this programme yet, or save grades first.'
      : (err.response?.data?.error ?? err.message ?? 'Download failed')
  } finally {
    if (gradeModal.value) gradeModal.value.downloadingProvisional = false
  }
}

async function confirmGradeSubmission() {
  const m = gradeModal.value
  if (!m || m.submitting || !canCommitAdminGrades.value) return
  m.submitting = true
  m.error = ''
  try {
    await api.post(
      `/v1/admin/students/${m.studentId}/enrollments/${m.enrollmentId}/grades`,
      { items: scoredRows(m).map(r => ({ subjectId: r.subjectId, score: r.score })), projectTitle: m.projectTitle })
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
  if (!count || count <= 12) return 1
  return 2
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
function countFor(id) {
  if (id === '') return list.value.length
  const f = STATUS_FILTERS.find(x => x.id === id)
  if (!f) return 0
  let n = 0
  for (const s of list.value) {
    if (f.overdue) { if (s.enrollments.some(e => e.paymentOverdue)) n++; continue }
    if (f.signingUp) { if (s.signingUp) n++; continue }
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

const filterPartnerName = ref('')
// Partner filter options come straight from the loaded rows — no extra call.
const partnersAvailable = computed(() =>
  [...new Set(list.value.map(s => s.partnerName).filter(Boolean))].sort((a, b) => a.localeCompare(b)))

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
  if (filterPartnerName.value)
    rows = rows.filter(s => s.partnerName === filterPartnerName.value)
  if (filterStatusId.value !== '') {
    const f = STATUS_FILTERS.find(x => x.id === filterStatusId.value)
    rows = rows.filter(s => {
      if (f?.overdue) return s.enrollments.some(e => e.paymentOverdue)
      if (f?.signingUp) return s.signingUp
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
    degreeSpecialization: d.background?.degreeSpecialization ?? '',
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
        durationMonths: e.approvedDurationMonths ?? e.durationOfStudyMonths ?? null,
        programmeMinDurationMonths: e.programmeMinDurationMonths ?? null,
        programmeMaxDurationMonths: e.programmeMaxDurationMonths ?? null,
        tuitionFeeUsd: Number(e.tuitionFeeUsd ?? 0),
        paymentPlan: null,
      })),
  })
}

const reviewingMode = ref(null) // 'partner-stage' | 'admission-stage'
const adminReviewEndpoint = (studentGuid, enrollmentId) =>
  `/v1/admin/students/${studentGuid}/enrollments/${enrollmentId}/review`

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
    const adapted = adaptForWizard(res.data, enrollmentId)
    const targetEnr = (res.data.enrollments || []).find(e =>
      !enrollmentId || e.studentEnrollmentId === enrollmentId)
      ?? res.data.enrollments?.[0]
    reviewingMode.value = (targetEnr?.statusCode === 'ApplicationSubmitted'
      || targetEnr?.statusCode === 'ApplicationAwaitingReviewByPartner')
      ? 'partner-stage'
      : 'admission-stage'
    reviewingStudent.value = adapted
  } catch (e) {
    loadError.value = e.response?.data?.error ?? e.message ?? 'Failed to load student'
  }
}
function closeReview() { reviewingStudent.value = null; reviewingMode.value = null }
async function onReviewSubmitted(s) {
  reviewToast.value = `Review submitted for ${s.firstName} ${s.lastName}`
  setTimeout(() => { reviewToast.value = '' }, 3200)
  await load()
  load()
}

// filterStatusId is a client-side filter — no refetch needed.
watch([filterProgrammeId, filterSpecializationId, () => props.partnerId], load)
onMounted(load)

// --- Export students wizard ---
const exportModal = ref(null)
const exportPartners = ref([])
const EXPORT_STEPS = [
  { id: 'partners', label: 'Partners' },
  { id: 'status',   label: 'Status' },
  { id: 'fields',   label: 'Fields' },
  { id: 'format',   label: 'Format' },
  { id: 'review',   label: 'Review' },
]

const EXPORT_FIELD_GROUPS = [
  { id: 'identity', label: 'Identity', fields: [
    { id: 'studentNumber', label: 'Student #' },
    { id: 'partnerName',   label: 'Partner' },
  ]},
  { id: 'account', label: 'Account', fields: [
    { id: 'username',      label: 'Username' },
    { id: 'email',         label: 'Email' },
    { id: 'emailVerified', label: 'Email verified' },
  ]},
  { id: 'personal', label: 'Personal', fields: [
    { id: 'firstName',       label: 'First name' },
    { id: 'lastName',        label: 'Last name' },
    { id: 'dateOfBirth',     label: 'Date of birth' },
    { id: 'passportId',      label: 'Passport / ID' },
    { id: 'nationalityName', label: 'Nationality' },
    { id: 'addressLine1',    label: 'Address' },
    { id: 'city',            label: 'City' },
    { id: 'stateRegion',     label: 'State / Region' },
    { id: 'postalCode',      label: 'Postal code' },
    { id: 'countryName',     label: 'Country' },
  ]},
  { id: 'background', label: 'Background', fields: [
    { id: 'highestDegree',       label: 'Highest degree' },
    { id: 'yearsWorkExperience', label: 'Years experience' },
    { id: 'languageResult',      label: 'Language test result' },
    { id: 'wizardStep',          label: 'Signup wizard step' },
    { id: 'languages',           label: 'Languages' },
  ]},
  { id: 'enrolments', label: 'Enrolment summary', fields: [
    { id: 'enrolments', label: 'Enrolments (joined into one cell)' },
  ]},
  // Picking ANY field from this group switches the export to one row per
  // (student × enrolment). Student fields are duplicated across the rows.
  { id: 'enrolmentDetail', label: 'Enrolment detail — switches to row-per-enrolment', fields: [
    { id: 'programmeCode',       label: 'Programme code' },
    { id: 'programmeName',       label: 'Programme' },
    { id: 'specializationName',  label: 'Specialisation' },
    { id: 'modeOfStudy',         label: 'Mode of study' },
    { id: 'instructionLanguage', label: 'Instruction language' },
    { id: 'pathwayName',         label: 'Pathway' },
    { id: 'pathwayMinYearsExp',  label: 'Pathway min yrs exp' },
    { id: 'offerAcceptanceMode', label: 'Offer acceptance mode' },
    { id: 'statusCode',          label: 'Status code' },
    { id: 'statusName',          label: 'Status' },
    { id: 'currentStatusEnteredAt', label: 'Status entered at' },
    { id: 'daysInCurrentStatus', label: 'Days in current status' },
    { id: 'commencementDate',    label: 'Start date' },
    { id: 'durationMonths',      label: 'Duration (months, default)' },
    { id: 'programmeMinDurationMonths', label: 'Programme min duration (months)' },
    { id: 'programmeMaxDurationMonths', label: 'Programme max duration (months)' },
    { id: 'approvedDurationMonths', label: 'Approved duration (months)' },
    { id: 'applicationDate',     label: 'Application date' },
    { id: 'daysSinceApplication',label: 'Days since application' },
    { id: 'approvedDate',        label: 'Approved date' },
    { id: 'graduatedDate',       label: 'Graduated date' },
    { id: 'offerLetterDate',     label: 'Offer letter date' },
    { id: 'admissionLetterDate', label: 'Admission letter date' },
    { id: 'transcriptDate',      label: 'Transcript date' },
    { id: 'certificateDate',     label: 'Certificate date' },
    { id: 'provisionalCertificateDate', label: 'Provisional certificate date' },
    { id: 'tuitionFee',          label: 'Tuition fee' },
    { id: 'additionalFees',      label: 'Additional fees' },
    { id: 'totalFees',           label: 'Total fees' },
    { id: 'feeCurrency',         label: 'Fee currency' },
    { id: 'numberOfPayments',    label: 'Number of payments' },
    { id: 'paymentsPaid',        label: 'Payments paid' },
    { id: 'totalPaid',           label: 'Total paid' },
    { id: 'outstanding',         label: 'Outstanding' },
    { id: 'docsUploaded',        label: 'Documents uploaded' },
    { id: 'docsVerified',        label: 'Documents verified' },
    { id: 'docsRejected',        label: 'Documents rejected' },
  ]},
]
// Default selection: every student-level field plus the joined enrolment
// overview. Per-enrolment detail fields stay off by default so the export
// doesn't surprise the user with multiple rows per student until they
// explicitly opt in.
const ALL_EXPORT_FIELDS = EXPORT_FIELD_GROUPS
  .filter(g => g.id !== 'enrolmentDetail')
  .flatMap(g => g.fields.map(f => f.id))

function makeExportModal() {
  loadExportPartners()
  const m = reactive({
    step: 1,
    partnersMode: 'all',
    selectedPartnerIds: [],
    selectedStatusFilters: [],
    selectedFields: [...ALL_EXPORT_FIELDS],
    format: 'xlsx',
    previewCount: null,
    previewLoading: false,
    previewToken: 0,
    sample: null,
    sampleLoading: false,
    sampleToken: 0,
    exporting: false,
    error: '',
  })
  return m
}

const canAdvanceExport = computed(() => {
  const m = exportModal.value
  if (!m) return false
  // Step 3 is Fields — require at least one column.
  if (m.step === 3) return m.selectedFields.length > 0
  return true
})

function goExportStep(n) {
  const m = exportModal.value
  if (!m) return
  if (n < 1 || n > EXPORT_STEPS.length) return
  m.step = n
  // Step 5 is Review — fetch the sample on entry.
  if (n === EXPORT_STEPS.length) loadExportSample()
}

async function loadExportSample() {
  const m = exportModal.value
  if (!m) return
  const token = ++m.sampleToken
  m.sampleLoading = true
  m.error = ''
  try {
    const res = await api.post('/v1/admin/students/export/sample', buildExportBody(m))
    if (m.sampleToken === token) m.sample = res.data
  } catch (err) {
    if (m.sampleToken === token) m.error = err.response?.data?.error ?? err.message ?? 'Preview failed'
  } finally {
    if (m.sampleToken === token) m.sampleLoading = false
  }
}

function formatCell(v) {
  if (v === null || v === undefined) return '—'
  if (typeof v === 'boolean') return v ? 'Yes' : 'No'
  return String(v)
}

function togglePartner(id, checked) {
  if (!exportModal.value) return
  const list = exportModal.value.selectedPartnerIds
  if (checked) {
    if (!list.includes(id)) list.push(id)
  } else {
    exportModal.value.selectedPartnerIds = list.filter(x => x !== id)
  }
}

async function loadExportPartners() {
  if (exportPartners.value.length) return
  try {
    const res = await api.get('/v1/admin/school/partners')
    exportPartners.value = res.data.items ?? []
  } catch {
    exportPartners.value = []
  }
}

function toggleStatusFilter(id, checked) {
  if (!exportModal.value) return
  const list = exportModal.value.selectedStatusFilters
  if (checked) {
    if (!list.includes(id)) list.push(id)
  } else {
    exportModal.value.selectedStatusFilters = list.filter(x => x !== id)
  }
}

function toggleField(id, checked) {
  if (!exportModal.value) return
  const list = exportModal.value.selectedFields
  if (checked) {
    if (!list.includes(id)) list.push(id)
  } else {
    exportModal.value.selectedFields = list.filter(x => x !== id)
  }
}

function groupAllSelected(g) {
  if (!exportModal.value) return false
  return g.fields.every(f => exportModal.value.selectedFields.includes(f.id))
}
function groupSomeSelected(g) {
  if (!exportModal.value) return false
  const some = g.fields.some(f => exportModal.value.selectedFields.includes(f.id))
  return some && !groupAllSelected(g)
}
function toggleGroup(g, checked) {
  if (!exportModal.value) return
  const ids = g.fields.map(f => f.id)
  const cur = exportModal.value.selectedFields
  if (checked) {
    exportModal.value.selectedFields = Array.from(new Set([...cur, ...ids]))
  } else {
    exportModal.value.selectedFields = cur.filter(x => !ids.includes(x))
  }
}

// Resolve status filter IDs to the wire-level status codes the backend expects.
// Empty selection means "all statuses".
function resolveExportStatusCodes(m) {
  const codes = new Set()
  for (const id of m.selectedStatusFilters) {
    const f = STATUS_FILTERS.find(x => x.id === id)
    if (!f?.codes) continue
    f.codes.forEach(c => codes.add(c))
  }
  return Array.from(codes)
}

function buildExportBody(m) {
  return {
    partnerIds: m.partnersMode === 'pick' ? m.selectedPartnerIds : [],
    statusCodes: resolveExportStatusCodes(m),
    fields: m.selectedFields,
    format: m.format,
  }
}

let exportPreviewTimer = null
watch(
  () => exportModal.value && [
    exportModal.value.partnersMode,
    [...exportModal.value.selectedPartnerIds],
    [...exportModal.value.selectedStatusFilters],
  ],
  () => {
    if (!exportModal.value) return
    if (exportPreviewTimer) clearTimeout(exportPreviewTimer)
    const m = exportModal.value
    const token = ++m.previewToken
    m.previewLoading = true
    exportPreviewTimer = setTimeout(async () => {
      try {
        const res = await api.post('/v1/admin/students/export/preview', buildExportBody(m))
        if (m.previewToken === token) m.previewCount = res.data.count ?? 0
      } catch (err) {
        if (m.previewToken === token) m.error = err.response?.data?.error ?? err.message ?? 'Preview failed'
      } finally {
        if (m.previewToken === token) m.previewLoading = false
      }
    }, 300)
  },
  { deep: true, immediate: false },
)

watch(exportModal, m => {
  if (!m) return
  // Kick the first preview when the modal opens.
  m.previewToken++
  m.previewLoading = true
  api.post('/v1/admin/students/export/preview', buildExportBody(m))
    .then(res => { if (exportModal.value === m) m.previewCount = res.data.count ?? 0 })
    .catch(err => { if (exportModal.value === m) m.error = err.response?.data?.error ?? err.message ?? 'Preview failed' })
    .finally(() => { if (exportModal.value === m) m.previewLoading = false })
})

async function runExport() {
  const m = exportModal.value
  if (!m || m.exporting) return
  m.exporting = true
  m.error = ''
  try {
    const res = await api.post('/v1/admin/students/export', buildExportBody(m), { responseType: 'blob' })
    const blob = res.data
    const cd = res.headers?.['content-disposition'] || ''
    const match = /filename="?([^"]+)"?/.exec(cd)
    const fallback = m.format === 'xlsx' ? 'students.xlsx' : 'students.csv'
    const filename = match?.[1] ?? fallback
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = filename
    document.body.appendChild(a); a.click(); document.body.removeChild(a)
    setTimeout(() => URL.revokeObjectURL(url), 60_000)
    exportModal.value = null
  } catch (err) {
    m.error = err.response?.data?.error ?? err.message ?? 'Export failed'
  } finally {
    if (exportModal.value) exportModal.value.exporting = false
  }
}
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
.btn-continue-signup { background: #b66a00; }
.btn-review-sm:hover:not(:disabled) { background: #0055a5; }
.btn-review-sm:disabled { background: #c0c8d2; cursor: not-allowed; opacity: 0.7; }
.btn-grades-approve { background: #16a34a; }
.btn-grades-approve:hover:not(:disabled) { background: #15803d; }
.btn-grades-submit { background: #2563eb; }
.btn-grades-submit:hover:not(:disabled) { background: #1e40af; }
.st-grades { background: #ede9fe; color: #5b21b6; }

.enrol-line { font-size: .85rem; }
.enrol-actions-cell { white-space: nowrap; vertical-align: top; }
.enrol-actions { display: flex; gap: .35rem; align-items: center; margin-bottom: .45rem; }
.enrol-actions .btn-review-sm, .enrol-actions .btn-row-details-sm { margin: 0; white-space: nowrap; }
.enr-prog { background: #e8f0f8; color: #003366; border-radius: 4px; padding: 1px 6px; font-size: .75rem; font-weight: 700; margin: 0 .3rem; }
.s-badge { font-size: .7rem; padding: 1px 6px; border-radius: 10px; margin-left: .3rem; font-weight: 600; }
.s-badge-overdue { background: #fde7e5; color: #a8241e; border: 1px solid #e8b3af; }
.s-badge-signup  { background: #fff4e6; color: #b66a00; border: 1px solid #f0d2a8; }
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
  display: grid; grid-template-columns: max-content minmax(140px, 1fr) auto auto auto; gap: .55rem;
  align-items: center; padding: .35rem .25rem;
  border-bottom: 1px solid #eef2f7; font-size: .82rem;
}
.gr-letter { display: inline-block; min-width: 30px; text-align: center; padding: 2px 6px;
  border-radius: 6px; font-weight: 700; font-size: .78rem; background: #eef2f7; color: #334155; }
.gr-code { font-family: ui-monospace, monospace; font-size: .76rem; color: #003366; overflow-wrap: anywhere; }
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
.legacy-box { margin-top: .7rem; padding: .6rem .7rem; border: 1px solid #e6ebf2; border-radius: 7px; background: #f8fafc; }
.legacy-check { display: flex; align-items: flex-start; gap: .45rem; font-size: .82rem; color: #44506a; cursor: pointer; }
.legacy-id-row { display: flex; align-items: center; gap: .5rem; margin-top: .55rem; flex-wrap: wrap; }
.legacy-id-row > label { font-size: .8rem; font-weight: 600; color: #44506a; }
.legacy-id-input { padding: .35rem .5rem; border: 1px solid #d8dde5; border-radius: 6px; font-size: .85rem; font-family: ui-monospace, monospace; min-width: 200px; }
.btn-delete-student { padding: .2rem .6rem; border: 1px solid #c0392b; background: #fff; color: #c0392b; border-radius: 4px; font-size: .72rem; font-weight: 600; cursor: pointer; white-space: nowrap; }
.btn-delete-student:hover:not(:disabled) { background: #c0392b; color: #fff; }
.btn-delete-student:disabled { opacity: .6; cursor: not-allowed; }

/* Student detail modal (3 tabs) — fixed height so switching tabs
   doesn't make the modal grow or shrink. Tab content scrolls within. */
.detail-modal { width: 1180px; max-width: 96vw; height: 88vh; max-height: 980px; display: flex; flex-direction: column; }
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
.ects-warn { color: #92400e; font-weight: 600; }
.ects-ok { color: #065f46; font-weight: 600; }
.moodle-row { display: flex; align-items: center; justify-content: space-between; gap: 1rem; padding: .85rem 1rem; background: #f6f9fd; border: 1px solid #e0e6ee; border-radius: 8px; max-width: 460px; }
.moodle-title { font-weight: 600; color: #1a2d4f; }
.moodle-toggle { display: flex; align-items: center; gap: .45rem; font-weight: 600; cursor: pointer; }
.moodle-toggle input { width: 1.05rem; height: 1.05rem; cursor: pointer; }
.moodle-creds { display: flex; gap: 1rem; margin-top: .85rem; max-width: 460px; }
.moodle-field { display: flex; flex-direction: column; gap: .3rem; flex: 1; }
.moodle-field label { font-size: .78rem; font-weight: 600; color: #44506a; }
.moodle-field input { padding: .45rem .6rem; border: 1px solid #cfd7e3; border-radius: 6px; font-size: .85rem; }
.pay-config { display: flex; align-items: flex-end; gap: 1rem; flex-wrap: wrap; margin-bottom: .8rem; }
.pay-field { display: flex; flex-direction: column; gap: .3rem; }
.pay-field label { font-size: .78rem; font-weight: 600; color: #44506a; }
.pay-field select, .pay-field input { padding: .4rem .55rem; border: 1px solid #cfd7e3; border-radius: 6px; font-size: .85rem; }
.pay-fee-row { display: flex; gap: .35rem; }
.pay-cur { width: 72px; }
.pay-table { width: 100%; border-collapse: collapse; font-size: .84rem; margin-bottom: .6rem; }
.pay-table th { text-align: left; padding: .4rem .5rem; background: #f6f9fd; border-bottom: 1px solid #e0e6ee; font-size: .74rem; text-transform: uppercase; color: #5f6e85; }
.pay-table td { padding: .3rem .5rem; border-bottom: 1px solid #f0f3f7; }
.pay-center { text-align: center; }
.pay-inp { padding: .3rem .45rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .82rem; width: 130px; }
.pay-summary { display: flex; gap: 1.5rem; font-size: .88rem; padding: .5rem 0; border-top: 1px solid #eef1f5; }
.pay-methods-row td { border-bottom: 1px solid #eef1f5; padding: 0 0 .5rem; }
.pay-methods { display: flex; gap: 1.25rem; flex-wrap: wrap; padding: .35rem .5rem; background: #f7f9fc; border: 1px solid #e3e9f1; border-radius: 8px; }
.pay-method { flex: 1 1 240px; }
.pay-method-toggle { display: flex; align-items: center; gap: .4rem; font-size: .78rem; font-weight: 600; cursor: pointer; }
.pay-method-input { display: block; width: 100%; margin-top: .25rem; padding: .3rem .5rem; font-size: .78rem; border: 1px solid #ccd5e0; border-radius: 6px; font-family: inherit; }
.pay-invoice-link { color: #1a4d8c; font-weight: 600; cursor: pointer; font-size: .85rem; text-decoration: underline; }
.pay-invoice-link.disabled { color: #9aa5b5; cursor: default; text-decoration: none; }
.ms-row { padding: .4rem 0; border-bottom: 1px solid #f0f3f7; font-size: .85rem; }
.ms-head { display: flex; gap: .5rem; align-items: baseline; margin-bottom: .2rem; }
.ms-line { display: flex; align-items: center; gap: .5rem; flex-wrap: wrap; padding: .1rem 0 .1rem 1rem; }
.ms-kind { width: 40px; font-weight: 700; color: #6b7888; font-size: .74rem; text-transform: uppercase; }
.ms-code { font-family: monospace; font-weight: 700; color: #003366; min-width: 110px; }
.ms-name { flex: 1 1 220px; }
.ms-inp { padding: .25rem .45rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .82rem; }
.ms-resolved { font-weight: 600; color: #1a4d8c; font-size: .82rem; }
.pay-add-head { display: flex; align-items: center; gap: .75rem; margin-top: 1rem; padding-top: .6rem; border-top: 1px solid #eef1f5; }
.pay-ai-card { margin: .5rem 0; padding: .6rem .7rem; background: #fbfcfe; border: 1px solid #e3e9f1; border-radius: 8px; }
.pay-ai-head { display: flex; align-items: center; gap: .75rem; margin-bottom: .45rem; }
.pay-ai-line { display: flex; align-items: flex-start; gap: .4rem; margin-bottom: .35rem; }
.pay-ai-line-fields { display: flex; flex-direction: column; gap: .25rem; flex: 1; max-width: 520px; }
.pay-ai-line-fields .pay-ai-text { width: 100%; }
.pay-ai-meta { display: flex; align-items: center; gap: 1rem; flex-wrap: wrap; margin: .35rem 0 .45rem; font-size: .78rem; }
.pay-ai-meta label { display: flex; align-items: center; gap: .35rem; font-weight: 600; }
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
.letter-actions { display: flex; gap: .4rem; flex-shrink: 0; }
.btn-mini-ghost { background: #fff; color: #1a4d8c; }
.btn-mini-ghost:hover:not(:disabled) { background: #eef4fb; }
.btn-mini-ghost:disabled { background: #f1f5f9; color: #94a3b8; }
.btn-mini-email { background: #fff; color: #6b4ea3; border-color: #6b4ea3; }
.btn-mini-email:hover:not(:disabled) { background: #f1ecf9; }
.btn-mini-email:disabled { background: #f1f5f9; color: #94a3b8; border-color: #cbd5e1; }
.email-send-pop { position: fixed; inset: 0; background: rgba(0,0,0,.35); display: flex; align-items: center; justify-content: center; z-index: 1200; }
.email-send-card { background: #fff; border-radius: 9px; width: min(420px, 92%); padding: 1rem 1.1rem; box-shadow: 0 8px 30px rgba(0,0,0,.2); }
.esp-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: .3rem; }
.esp-label { display: block; font-size: .74rem; font-weight: 700; color: #6b7888; text-transform: uppercase; letter-spacing: .04em; margin: .5rem 0 .2rem; }
.email-send-card input { width: 100%; padding: .4rem .5rem; border: 1px solid #d8dde5; border-radius: 5px; font-size: .82rem; }
.esp-actions { display: flex; justify-content: flex-end; gap: .5rem; margin-top: .8rem; }

.dur-input { width: 70px; padding: .2rem .35rem; border: 1px solid #d8dde5; border-radius: 4px; font-size: .82rem; }
.dur-warn { margin-top: .3rem; color: #b45309; font-size: .8rem; }
.dur-regen { margin-top: .4rem; padding: .4rem .55rem; background: #fff7ed; border: 1px solid #fdba74; border-radius: 5px; font-size: .8rem; color: #7c2d12; }
.btn-export { margin-left: auto; padding: .35rem .85rem; border: 1px solid #1a4d8c; background: #1a4d8c; color: #fff; border-radius: 5px; font-size: .82rem; font-weight: 600; cursor: pointer; }
.btn-export:hover { background: #143b6c; }
.btn-add-student { padding: .35rem .85rem; border: 1px solid #1f7a44; background: #1f7a44; color: #fff; border-radius: 5px; font-size: .82rem; font-weight: 600; cursor: pointer; }
.btn-add-student:hover { background: #185f35; }

.export-modal { max-width: 720px; }
.export-section { padding: .85rem 0; border-bottom: 1px solid #eef2f7; }
.export-section:last-of-type { border-bottom: none; }
.export-section h4 { margin: 0 0 .55rem; font-size: .92rem; color: #1a2d4f; }
.export-row { display: grid; grid-template-columns: 110px 1fr; gap: .55rem; align-items: start; margin-bottom: .45rem; }
.export-label { font-size: .82rem; color: #4b5563; padding-top: .3rem; }
.export-control { display: flex; flex-direction: column; gap: .4rem; }
.export-radio { display: inline-flex; align-items: center; gap: .35rem; margin-right: .9rem; font-size: .85rem; cursor: pointer; }
.export-help { font-size: .76rem; color: #6b7888; }
.export-multi { padding: .35rem; border: 1px solid #d8dde5; border-radius: 5px; font-size: .82rem; min-width: 220px; }
.export-chip-list { display: flex; flex-wrap: wrap; gap: .35rem; }
.export-chip { display: inline-flex; align-items: center; gap: .3rem; padding: .25rem .55rem; border: 1px solid #e0e6ee; background: #f7f9fc; border-radius: 999px; font-size: .76rem; cursor: pointer; }
.export-chip:hover { background: #eef3fa; }
.export-field-group { padding: .35rem 0; }
.export-group-toggle { display: inline-flex; align-items: center; gap: .35rem; font-size: .87rem; cursor: pointer; }
.export-field-list { display: grid; grid-template-columns: repeat(3, 1fr); gap: .25rem .85rem; margin-left: 1.5rem; margin-top: .25rem; }
.export-field-check { display: inline-flex; align-items: center; gap: .35rem; font-size: .8rem; cursor: pointer; color: #243049; }
.export-include-docs { display: flex; align-items: center; gap: .4rem; margin-top: .65rem; padding: .55rem .75rem; background: #f7f9fc; border: 1px solid #e6ebf2; border-radius: 6px; font-size: .85rem; }
.export-footer { display: flex; align-items: center; gap: .65rem; padding-top: .8rem; }
.export-count { margin-right: auto; font-size: .85rem; color: #243049; }

.export-steps { display: flex; align-items: center; gap: .5rem; padding: .65rem 1rem .35rem; border-bottom: 1px solid #eef2f7; }
.export-step-pill { display: flex; align-items: center; gap: .4rem; padding: .25rem .65rem; border: 1px solid #e0e6ee; background: #f7f9fc; border-radius: 999px; font-size: .78rem; color: #6b7888; }
.export-step-pill.done { background: #ecfdf5; border-color: #a7f3d0; color: #047857; }
.export-step-pill.active { background: #eff6ff; border-color: #93c5fd; color: #1d4ed8; font-weight: 700; }
.export-step-num { display: inline-flex; align-items: center; justify-content: center; width: 18px; height: 18px; background: #1a4d8c; color: #fff; border-radius: 50%; font-size: .68rem; font-weight: 700; }
.export-step-pill.done .export-step-num { background: #047857; }
.export-step-pill:not(.active):not(.done) .export-step-num { background: #cbd5e1; }

.export-review-summary { display: flex; gap: 1.4rem; padding: .6rem .85rem; background: #f7f9fc; border: 1px solid #e6ebf2; border-radius: 6px; margin-bottom: .65rem; font-size: .9rem; }
.export-preview-table-wrap { max-width: 100%; max-height: 320px; overflow: auto; border: 1px solid #e6ebf2; border-radius: 5px; }
.export-preview-table { width: 100%; border-collapse: collapse; font-size: .78rem; }
.export-preview-table th { position: sticky; top: 0; background: #f1f5f9; text-align: left; padding: .4rem .55rem; border-bottom: 1px solid #e2e8f0; font-weight: 700; white-space: nowrap; }
.export-preview-table td { padding: .35rem .55rem; border-bottom: 1px solid #f1f5f9; vertical-align: top; white-space: nowrap; }
.export-preview-table tr:nth-child(even) td { background: #fbfdff; }
.ai-badge-none { background:#fff !important; color:#888 !important; border:1px solid #ccc; }
.ai-badge { display: inline-block; margin-right: .35rem; padding: .1rem .4rem; border-radius: 9px; color: #fff; font-size: .68rem; font-weight: 800; cursor: help; }
</style>

<style scoped>
.project-title-row { margin: .6rem 0; display: flex; flex-direction: column; gap: .3rem; }
.project-title-row label { font-size: .82rem; font-weight: 600; color: #1a2d4f; }

.programs-layout { display: flex; gap: 1rem; align-items: flex-start; }
.add-prog-box { background: #f7f9fb; border: 1.5px solid #dfe6ee; border-radius: 8px; padding: .5rem; }
.programs-menu { width: 215px; flex-shrink: 0; display: flex; flex-direction: column; gap: .4rem; }
.prog-menu-item { display: flex; flex-direction: column; gap: .1rem; text-align: left; background: #f7f9fb; border: 1.5px solid #dfe6ee; border-radius: 8px; padding: .55rem .7rem; cursor: pointer; }
.prog-menu-item:hover { border-color: #a0b8d0; }
.prog-menu-item.active { border-color: #0b2e59; background: #eef3fb; }
.prog-menu-name { font-weight: 700; font-size: .82rem; color: #0b2e59; }
.prog-menu-spec { font-size: .76rem; color: #667; }
.prog-menu-status { font-size: .7rem; color: #856404; }
.programs-content { flex: 1; min-width: 0; }
.prog-subtabs { display: flex; gap: .25rem; border-bottom: 2px solid #e8edf4; margin-bottom: .9rem; }
.prog-enrol-grid { grid-template-columns: 1fr; }

.grade-inline-wrap { position: static; }
.grade-modal.grade-inline { width: 100%; max-width: 100%; box-shadow: none; border: 1px solid #e3e9f1; border-radius: 8px; }
</style>
