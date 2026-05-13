<template>
  <div class="page-wrapper">
    <nav class="navbar">
      <span class="brand-text">IBSS Admin Portal</span>
      <div class="nav-links">
        <RouterLink to="/admin" class="nav-link">Dashboard</RouterLink>
        <RouterLink to="/programmes" class="nav-link">Programmes</RouterLink>
      </div>
      <div class="nav-right">
        <span class="nav-user">{{ auth.user?.displayName }}</span>
        <button class="btn-logout" @click="logout">Log out</button>
      </div>
    </nav>

    <div v-if="!student" class="container not-found">
      <RouterLink to="/admin" class="back-link">← Back to Students</RouterLink>
      <p>Student not found.</p>
    </div>

    <div v-else class="container">
      <RouterLink to="/admin" class="back-link">← Back to Students</RouterLink>

      <!-- ── Student header ── -->
      <div class="student-header">
        <div class="header-left">
          <h1 class="student-name">{{ student.firstName }} {{ student.lastName }}</h1>
          <span class="sid-badge">{{ student.studentId }}</span>
        </div>
        <div class="header-right">
          <span :class="'status-badge status-' + studentStatus">{{ statusLabel }}</span>
          <button class="btn-edit-stu" @click="openEdit">Edit Student</button>
        </div>
      </div>

      <!-- ── Info grid ── -->
      <div class="info-card">
        <div class="info-grid">
          <div class="info-item"><span class="info-label">Partner</span><span>{{ student.partner }}</span></div>
          <div class="info-item"><span class="info-label">Email</span><span>{{ student.email || '—' }}</span></div>
          <div class="info-item"><span class="info-label">Date of Birth</span><span>{{ student.dateOfBirth || '—' }}</span></div>
          <div class="info-item"><span class="info-label">Passport / ID</span><span class="mono">{{ student.passportId }}</span></div>
          <div class="info-item"><span class="info-label">Address</span><span>{{ student.address || '—' }}</span></div>
          <div class="info-item"><span class="info-label">Highest Degree</span><span>{{ student.highestDegree || '—' }}</span></div>
          <div class="info-item"><span class="info-label">Language Result</span><span>{{ student.languageResult || '—' }}</span></div>
          <div class="info-item"><span class="info-label">Work Experience</span><span>{{ student.yearsWorkExperience != null ? student.yearsWorkExperience + ' yrs' : '—' }}</span></div>
        </div>
      </div>

      <!-- ── Enrollment card (card-based, like partner view) ── -->
      <div class="student-card">
        <div class="sc-header" @click="enrollCollapsed = !enrollCollapsed" style="cursor:pointer">
          <div class="sc-id-name">
            <span class="sc-sid">{{ student.studentId }}</span>
            <span class="sc-sep">·</span>
            <span class="sc-name">{{ student.firstName }} {{ student.lastName }}</span>
            <span class="sc-tag-admin">Admin View</span>
          </div>
          <div class="sc-header-right">
            <span v-if="enrollCollapsed" class="sc-summary">
              {{ student.enrollments.length }} enrolment{{ student.enrollments.length !== 1 ? 's' : '' }}
            </span>
            <span class="sc-chevron" :class="{ collapsed: enrollCollapsed }">&#8964;</span>
          </div>
        </div>

        <div v-if="!enrollCollapsed" class="sc-body">
          <div class="enr-table-wrap">
            <table class="enr-table">
              <thead>
                <tr>
                  <th class="th-prog">Programme &amp; Specialization</th>
                  <th class="th-status">Status</th>
                  <th class="th-acad">Academic Progress</th>
                  <th class="th-pay">Payment</th>
                  <th class="th-rel">Releases</th>
                  <th class="th-edit"></th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="enr in student.enrollments" :key="enr.id" class="enr-row">

                  <!-- Col 1: Programme & Specialization + doc links -->
                  <td class="td-prog">
                    <div class="prog-name-main">{{ enr.programme }}</div>
                    <div class="prog-specialization-sub">{{ enr.specialization }}</div>
                    <div class="doc-list">
                      <a class="doc-row" :class="enr.offerType ? 'doc-avail' : 'doc-disabled'" href="#"
                         @click.prevent="enr.offerType && printOfferLetter(student, enr, 'offer')">
                        <span class="doc-icon">&#128196;</span> Offer Letter
                        <span v-if="!enr.offerType" class="doc-na">Not issued</span>
                      </a>
                      <a class="doc-row" :class="enr.paymentDone ? 'doc-avail' : 'doc-disabled'" href="#"
                         @click.prevent="enr.paymentDone && printOfferLetter(student, enr, 'admission')">
                        <span class="doc-icon">&#128196;</span> Admission Letter
                        <span v-if="!enr.paymentDone" class="doc-na">Not issued</span>
                      </a>
                      <a class="doc-row" :class="enr.certReleased ? 'doc-avail' : 'doc-disabled'" href="#"
                         @click.prevent="enr.certReleased && printCertificate(student, true)">
                        <span class="doc-icon">&#127891;</span> Certificate
                        <span v-if="!enr.certReleased" class="doc-na">Unreleased</span>
                      </a>
                      <a class="doc-row" :class="enr.transcriptReleased ? 'doc-avail' : 'doc-disabled'" href="#"
                         @click.prevent="enr.transcriptReleased && printCertificate(student, true)">
                        <span class="doc-icon">&#128203;</span> Transcript
                        <span v-if="!enr.transcriptReleased" class="doc-na">Unreleased</span>
                      </a>
                    </div>
                  </td>

                  <!-- Col 2: Status — editable select -->
                  <td class="td-status">
                    <select v-model="enr.enrollmentStatus" class="status-sel-inline">
                      <option v-for="s in ENROLLMENT_STATUSES" :key="s" :value="s">{{ s }}</option>
                    </select>
                  </td>

                  <!-- Col 3: Academic Progress — editable -->
                  <td class="td-acad">
                    <div class="ap-row">
                      <span class="ap-lbl">Courses:</span>
                      <div class="course-inp">
                        <input v-model.number="enr.coursesCompleted" type="number" min="0" :max="enr.coursesRequired" class="inp-num-sm" />
                        <span class="num-sep">/</span>
                        <input v-model.number="enr.coursesRequired" type="number" min="1" class="inp-num-sm" />
                      </div>
                    </div>
                    <div class="ap-row" style="margin-top:.4rem">
                      <span class="ap-lbl">Final:</span>
                      <select v-model="enr.finalProjectStatus" class="sel-sm">
                        <option>not applicable</option>
                        <option>not done</option>
                        <option>doing</option>
                        <option>done</option>
                      </select>
                    </div>
                    <div class="ap-row" style="margin-top:.4rem">
                      <span class="ap-lbl">Marks:</span>
                      <span v-if="isGraded(student.studentId)" class="rel-yes">Graded</span>
                      <span v-else class="rel-no">Not yet</span>
                    </div>
                    <div v-if="enr.admissionConfirmed" class="ap-row" style="margin-top:.3rem">
                      <span class="confirmed-chip">Admitted ✓</span>
                    </div>
                  </td>

                  <!-- Col 4: Payment — editable -->
                  <td class="td-pay">
                    <div class="pay-section">
                      <div class="ap-lbl">Tuition Fee</div>
                      <select v-model="enr.tuitionFeeStatus" class="sel-sm">
                        <option>unpaid</option>
                        <option>partially paid</option>
                        <option>fully paid</option>
                      </select>
                    </div>
                    <div class="pay-section" style="margin-top:.45rem">
                      <div class="ap-lbl">Other Fees</div>
                      <select v-model="enr.otherFeesStatus" class="sel-sm">
                        <option>not applicable</option>
                        <option>unpaid</option>
                        <option>partially paid</option>
                        <option>fully paid</option>
                      </select>
                    </div>
                    <div style="margin-top:.5rem">
                      <button :class="enr.paymentDone ? 'tog-on' : 'tog-off'" @click="enr.paymentDone = !enr.paymentDone">
                        {{ enr.paymentDone ? 'Paid ✓' : 'Not Paid' }}
                      </button>
                    </div>
                  </td>

                  <!-- Col 5: Releases — toggles -->
                  <td class="td-rel">
                    <button :class="enr.certReleased ? 'tog-on' : 'tog-off'" class="tog-wide" @click="enr.certReleased = !enr.certReleased">
                      {{ enr.certReleased ? 'Cert ✓' : 'Cert —' }}
                    </button>
                    <button :class="enr.transcriptReleased ? 'tog-on' : 'tog-off'" class="tog-wide" style="margin-top:.4rem" @click="enr.transcriptReleased = !enr.transcriptReleased">
                      {{ enr.transcriptReleased ? 'Transcript ✓' : 'Transcript —' }}
                    </button>
                    <div style="margin-top:.5rem">
                      <button v-if="!enr.admissionConfirmed && enr.paymentDone" class="btn-confirm-adm-sm" @click="enr.admissionConfirmed = true">
                        Confirm Admission
                      </button>
                      <span v-else-if="enr.admissionConfirmed" class="confirmed-chip">Admitted ✓</span>
                      <span v-else class="rel-no" style="font-size:.75rem">Awaiting payment</span>
                    </div>
                    <div style="margin-top:.4rem">
                      <button v-if="!enr.offerType" class="btn-issue-offer" @click="issueOffer(enr, 'offer')">Issue Offer</button>
                      <span v-else :class="['offer-chip', enr.offerType === 'conditional_offer' ? 'offer-chip-cond' : 'offer-chip-full']">
                        {{ enr.offerType === 'conditional_offer' ? 'Conditional Offer' : 'Full Offer' }}
                      </span>
                    </div>
                  </td>

                  <!-- Col 6: Edit button -->
                  <td class="td-edit">
                    <button class="btn-edit-enr" @click="openEnrEdit(enr)">&#9998; Edit</button>
                  </td>

                </tr>
              </tbody>
            </table>
          </div>
          <div class="sc-footer">
            <button class="btn-add-enr" @click="showAddEnrol = true">+ Add Programme Enrolment</button>
          </div>
        </div>
      </div>

      <!-- ── Uploaded documents ── -->
      <div class="detail-card">
        <h3 class="card-title">Uploaded Documents</h3>
        <div class="doc-list">
          <div v-for="doc in allDocSlots" :key="doc.field" class="doc-row-admin" :class="{ 'doc-row-missing': !student[doc.field] }">
            <span class="doc-type-icon">{{ student[doc.field] ? '📄' : '📋' }}</span>
            <div class="doc-info">
              <span class="doc-type-label">{{ doc.label }}</span>
              <span v-if="student[doc.field]" class="doc-filename">{{ student[doc.field] }}</span>
              <span v-else class="doc-missing">Not uploaded</span>
            </div>
            <div v-if="student[doc.field]">
              <div v-if="student.docsVerified?.[doc.verifyKey]" class="doc-verified-badge">✓ Verified</div>
              <div v-else class="doc-verified-badge doc-unverified">Unverified</div>
            </div>
            <div class="doc-actions">
              <button v-if="student[doc.field]" class="btn-review" @click="openReview(doc)">Review</button>
              <label class="btn-upload-doc">
                {{ student[doc.field] ? 'Replace' : 'Upload' }}
                <input type="file" class="file-hidden-input" :accept="doc.accept" @change="handleAdminUpload(doc.field, $event)" />
              </label>
            </div>
          </div>
        </div>
      </div>

      <!-- ── Download Centre ── -->
      <div class="detail-card download-card">
        <h3 class="card-title">Download Centre</h3>
        <p class="download-hint">Select documents to download. Grade report and certificate are available in two versions: <strong>Digital</strong> includes the official IBSS stamp and digital signature; <strong>Physical</strong> is unsigned for manual signing and stamping.</p>
        <div class="dl-list">
          <template v-for="doc in uploadedDocs" :key="'up_' + doc.field">
            <label class="dl-row">
              <input type="checkbox" v-model="dlSelected" :value="'up_' + doc.field" class="dl-check" />
              <span class="dl-type-icon">📄</span>
              <span class="dl-name">{{ doc.label }} <span class="dl-filename">({{ doc.value }})</span></span>
              <span class="dl-tag tag-file">Uploaded</span>
            </label>
          </template>
          <template v-if="isGraded(student.studentId)">
            <div class="dl-section-divider">Grade Report</div>
            <label class="dl-row">
              <input type="checkbox" v-model="dlSelected" value="grade_digital" class="dl-check" />
              <span class="dl-type-icon">📊</span>
              <span class="dl-name">Grade Report — <strong>Digital</strong> <span class="dl-sub">with official stamp &amp; digital signature</span></span>
              <span class="dl-tag tag-grade">Grade · Digital</span>
            </label>
            <label class="dl-row">
              <input type="checkbox" v-model="dlSelected" value="grade_physical" class="dl-check" />
              <span class="dl-type-icon">📊</span>
              <span class="dl-name">Grade Report — <strong>Physical</strong> <span class="dl-sub">unsigned, for manual signing</span></span>
              <span class="dl-tag tag-grade-plain">Grade · Physical</span>
            </label>
          </template>
          <template v-if="isGraded(student.studentId)">
            <div class="dl-section-divider">Academic Certificate</div>
            <label class="dl-row">
              <input type="checkbox" v-model="dlSelected" value="cert_digital" class="dl-check" />
              <span class="dl-type-icon">🎓</span>
              <span class="dl-name">Academic Certificate — <strong>Digital</strong> <span class="dl-sub">with official stamp &amp; digital signature</span></span>
              <span class="dl-tag tag-cert">Cert · Digital</span>
            </label>
            <label class="dl-row">
              <input type="checkbox" v-model="dlSelected" value="cert_physical" class="dl-check" />
              <span class="dl-type-icon">🎓</span>
              <span class="dl-name">Academic Certificate — <strong>Physical</strong> <span class="dl-sub">unsigned, for manual signing</span></span>
              <span class="dl-tag tag-cert-plain">Cert · Physical</span>
            </label>
          </template>
          <div v-if="uploadedDocs.length === 0 && !isGraded(student.studentId)" class="dl-empty">
            No documents available yet.
          </div>
        </div>
        <div class="dl-actions">
          <label class="dl-select-all">
            <input type="checkbox" :checked="allSelected" @change="toggleAll" />
            Select All
          </label>
          <button class="btn-dl-all" :disabled="dlSelected.length === 0" @click="downloadSelected">
            Download Selected ({{ dlSelected.length }})
          </button>
        </div>
      </div>

      <!-- ── Absence History ── -->
      <div class="detail-card">
        <h3 class="card-title">Absence History</h3>
        <div v-if="studentAbsences.length === 0" class="dl-empty">No absence reports for this student.</div>
        <table v-else class="detail-tbl">
          <thead><tr><th>Date</th><th>Type</th><th>Reason</th><th>Document</th><th>Status</th><th></th></tr></thead>
          <tbody>
            <tr v-for="a in studentAbsences" :key="a.id">
              <td>{{ a.date }}</td>
              <td>{{ a.type }}</td>
              <td style="font-size:.83rem;color:#555">{{ a.reason || '—' }}</td>
              <td style="font-size:.82rem">{{ a.docFilename || '—' }}</td>
              <td>
                <span :class="a.status === 'acknowledged' ? 'status-badge status-confirmed' : 'status-badge status-new'" style="font-size:.74rem">
                  {{ a.status === 'acknowledged' ? 'Acknowledged' : 'Pending' }}
                </span>
              </td>
              <td>
                <button v-if="a.status !== 'acknowledged'" class="btn-sm-teal" @click="a.status = 'acknowledged'">Acknowledge</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- ── Messages ── -->
      <div class="detail-card">
        <h3 class="card-title">Messages &amp; Announcements</h3>
        <div class="msg-compose">
          <h4 class="compose-label">Send Direct Message to Student</h4>
          <div class="field"><label>Title</label><input v-model="msgForm.title" placeholder="Message subject…" /></div>
          <div class="field" style="margin-top:.6rem"><label>Body</label><textarea v-model="msgForm.body" rows="3" placeholder="Message…"></textarea></div>
          <button class="btn-save" style="margin-top:.65rem" :disabled="!msgForm.title || !msgForm.body" @click="sendMsg">Send Message</button>
          <span v-if="msgSent" class="saved-msg" style="margin-left:.75rem">Sent!</span>
        </div>
        <div v-if="studentAnnouncements.length" style="margin-top:1.1rem">
          <h4 class="compose-label">Past Notices &amp; Messages</h4>
          <div v-for="a in studentAnnouncements" :key="a.id" class="notice-row">
            <div class="notice-title-adm">{{ a.title }}</div>
            <div class="notice-body-adm">{{ a.body }}</div>
            <div class="notice-date-adm">{{ fmtDate(a.createdAt) }}</div>
          </div>
        </div>
      </div>

      <!-- Toast -->
      <transition name="toast-pop">
        <div v-if="toast" class="toast">{{ toast }}</div>
      </transition>
    </div>

    <!-- ── Edit Student Drawer ── -->
    <transition name="fade"><div v-if="showEdit" class="overlay" @click.self="showEdit = false" /></transition>
    <transition name="slide">
      <div v-if="showEdit" class="drawer">
        <div class="drawer-header">
          <div>
            <h2>Edit Student</h2>
            <p class="drawer-sub">{{ student?.studentId }}</p>
          </div>
          <button class="drawer-close" @click="showEdit = false">✕</button>
        </div>
        <div class="drawer-form">
          <p class="edit-section-label">Personal Information</p>
          <div class="row-2">
            <div class="field"><label>First Name</label><input v-model="editForm.firstName" /></div>
            <div class="field"><label>Last Name</label><input v-model="editForm.lastName" /></div>
          </div>
          <div class="row-2">
            <div class="field"><label>Date of Birth</label><input v-model="editForm.dateOfBirth" type="date" /></div>
            <div class="field"><label>Email</label><input v-model="editForm.email" type="email" /></div>
          </div>
          <div class="field"><label>Passport / ID No.</label><input v-model="editForm.passportId" /></div>
          <div class="field"><label>Address</label><input v-model="editForm.address" /></div>

          <p class="edit-section-label">Programme Details</p>
          <div class="field">
            <label>Partner</label>
            <select v-model="editForm.partner">
              <option v-for="n in partnerNames" :key="n">{{ n }}</option>
            </select>
          </div>
          <div class="row-2">
            <div class="field">
              <label>Highest Degree</label>
              <input v-model="editForm.highestDegree" />
            </div>
            <div class="field">
              <label>Language Result</label>
              <input v-model="editForm.languageResult" />
            </div>
          </div>
          <div class="field field-narrow"><label>Years Work Experience</label><input v-model.number="editForm.yearsWorkExperience" type="number" min="0" /></div>
        </div>
        <div class="drawer-actions">
          <button class="btn-cancel" @click="showEdit = false">Cancel</button>
          <button class="btn-save" @click="saveEdit">Save Changes</button>
        </div>
      </div>
    </transition>

    <!-- ── Enrollment Edit Drawer (Grades / Notes / Activity) ── -->
    <transition name="fade"><div v-if="showEnrEdit" class="overlay" @click.self="showEnrEdit = false" /></transition>
    <transition name="slide">
      <div v-if="showEnrEdit && editingEnrollment" class="drawer">
        <div class="drawer-header">
          <div>
            <h2>Edit Enrolment</h2>
            <p class="drawer-sub">{{ student?.studentId }} &nbsp;·&nbsp; {{ editingEnrollment.programme }}</p>
          </div>
          <button class="drawer-close" @click="showEnrEdit = false">✕</button>
        </div>

        <!-- Tabs -->
        <div class="enr-edit-tabs">
          <button :class="['enr-tab', { active: enrEditTab === 'grades' }]" @click="enrEditTab = 'grades'">Grades</button>
          <button :class="['enr-tab', { active: enrEditTab === 'notes' }]" @click="enrEditTab = 'notes'">Notes &amp; Changes</button>
          <button :class="['enr-tab', { active: enrEditTab === 'activity' }]" @click="enrEditTab = 'activity'">Activity</button>
        </div>

        <!-- Grades tab -->
        <template v-if="enrEditTab === 'grades'">
          <div class="student-info-strip">
            <span><strong>Student:</strong> {{ student?.firstName }} {{ student?.lastName }}</span>
            <span><strong>Programme:</strong> {{ editingEnrollment.programme }}</span>
            <span><strong>Specialization:</strong> {{ editingEnrollment.specialization }}</span>
          </div>
          <div class="grade-table-wrap">
            <table class="grade-table">
              <thead>
                <tr>
                  <th>Module</th>
                  <th class="num-col">Credit<br>Hours</th>
                  <th class="num-col">IBSS<br>Grade</th>
                  <th class="num-col">UK<br>Grade</th>
                  <th class="num-col">ECTS<br>Grade</th>
                  <th class="num-col">ECTS<br>Points</th>
                  <th class="num-col">Grade<br>Points</th>
                  <th>Remark</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="row in gradeRows" :key="row.name">
                  <td>{{ row.name }}</td>
                  <td class="num-col">{{ row.credits }}</td>
                  <td class="num-col">
                    <input v-model.number="row.ibssGrade" class="grade-input" type="number" min="0" max="100" placeholder="0–100" @input="recalcRow(row)" />
                  </td>
                  <td class="num-col calc-cell">{{ row.ukGrade ?? '—' }}</td>
                  <td class="num-col calc-cell">{{ row.ectsGrade ?? '—' }}</td>
                  <td class="num-col calc-cell">{{ row.ectsPoints != null ? row.ectsPoints.toFixed(1) : '—' }}</td>
                  <td class="num-col calc-cell" :class="row.gradePoints != null ? 'highlight' : ''">{{ row.gradePoints != null ? row.gradePoints.toFixed(1) : '—' }}</td>
                  <td class="remark-cell">{{ row.remark ?? '—' }}</td>
                </tr>
              </tbody>
              <tfoot>
                <tr class="total-row">
                  <td><strong>Total</strong></td>
                  <td class="num-col"><strong>{{ totalCredits }}</strong></td>
                  <td class="num-col"></td><td class="num-col"></td><td class="num-col"></td><td class="num-col"></td>
                  <td class="num-col highlight"><strong>{{ totalGradePoints.toFixed(1) }}</strong></td>
                  <td></td>
                </tr>
                <tr class="gpa-row">
                  <td colspan="6" class="gpa-label">Grade Point Average (Total Grade Points ÷ Total Credits)</td>
                  <td class="num-col gpa-val highlight"><strong>{{ gpa }}</strong></td>
                  <td></td>
                </tr>
              </tfoot>
            </table>
          </div>
          <div v-if="saveSuccess" class="success-msg">Grades saved successfully!</div>
          <div class="drawer-actions">
            <button class="btn-cancel" @click="showEnrEdit = false">Cancel</button>
            <button class="btn-save" @click="saveGradesForStudent">Save Grades</button>
          </div>
        </template>

        <!-- Notes & Changes tab -->
        <template v-if="enrEditTab === 'notes'">
          <div class="drawer-form">
            <div v-if="editingEnrollment.changeNotes.length" class="notes-list">
              <div v-for="note in editingEnrollment.changeNotes" :key="note.id" class="note-entry">
                <div class="note-meta">
                  <span class="note-arrow">→</span>
                  <span class="note-req">{{ note.requestedChange || 'General' }}</span>
                  <span class="note-date">{{ note.date }}</span>
                </div>
                <div class="note-text">{{ note.text }}</div>
              </div>
            </div>
            <div v-else class="hint-text">No notes yet for this enrolment.</div>
            <div class="section-heading" style="margin-top:.75rem">Add New Note</div>
            <div class="field">
              <label>Change Type</label>
              <select v-model="noteChanges[editingEnrollment.id]" class="note-sel">
                <option value="">— Select type —</option>
                <option v-for="st in ADMIN_NOTE_TYPES" :key="st" :value="st">{{ st }}</option>
              </select>
            </div>
            <div class="field">
              <label>Note / Description</label>
              <textarea v-model="noteTexts[editingEnrollment.id]" rows="3" placeholder="Describe the change…" class="note-ta"></textarea>
            </div>
          </div>
          <div class="drawer-actions">
            <button class="btn-cancel" @click="showEnrEdit = false">Close</button>
            <button class="btn-save" :disabled="!noteTexts[editingEnrollment.id]" @click="addChangeNote(editingEnrollment)">Add Note</button>
          </div>
        </template>

        <!-- Activity tab -->
        <template v-if="enrEditTab === 'activity'">
          <div class="drawer-form">
            <div class="section-heading">Offer Letter</div>
            <div class="activity-card">
              <div v-if="editingEnrollment.offerType">
                <span :class="editingEnrollment.offerType === 'offer' ? 'offer-chip offer-chip-full' : 'offer-chip offer-chip-cond'">
                  {{ editingEnrollment.offerType === 'offer' ? 'Full Offer Issued' : 'Conditional Offer Issued' }}
                </span>
                <span class="activity-desc" style="margin-left:.5rem">Click to print.</span>
                <div style="margin-top:.65rem;display:flex;gap:.5rem">
                  <button class="btn-sm-blue" @click="printOfferLetter(student, editingEnrollment, 'offer')">Print Offer Letter</button>
                  <button class="btn-sm-teal" @click="printOfferLetter(student, editingEnrollment, 'admission')">Print Admission Letter</button>
                </div>
              </div>
              <div v-else>
                <p class="activity-desc">No offer issued yet. Select type to issue:</p>
                <div style="margin-top:.65rem;display:flex;gap:.5rem">
                  <button class="btn-sm-blue" @click="issueOffer(editingEnrollment, 'offer')">Issue Full Offer</button>
                  <button class="btn-sm-teal" @click="issueOffer(editingEnrollment, 'conditional_offer')">Issue Conditional Offer</button>
                </div>
              </div>
            </div>

            <div class="section-heading" style="margin-top:1.25rem">Admission Confirmation</div>
            <div v-if="editingEnrollment.paymentDone && !editingEnrollment.admissionConfirmed" class="activity-card">
              <p class="activity-desc">Payment has been received. Confirm the student's admission to complete the enrolment.</p>
              <button class="btn-confirm-adm" @click="editingEnrollment.admissionConfirmed = true">Confirm Admission</button>
            </div>
            <div v-else-if="editingEnrollment.admissionConfirmed" class="activity-card activity-done">
              <span class="confirmed-chip">Admitted ✓</span>
              <span class="activity-desc" style="margin-left:.5rem">Admission confirmed.</span>
            </div>
            <div v-else class="activity-card">
              <p class="activity-desc" style="color:#aaa">Awaiting payment confirmation before admission can be confirmed.</p>
            </div>

            <div class="section-heading" style="margin-top:1.25rem">Document Releases</div>
            <div class="activity-card">
              <div class="toggle-row">
                <div>
                  <div class="toggle-label">Release Certificate</div>
                  <div class="toggle-sub">Allow partner to download certificate.</div>
                </div>
                <button :class="editingEnrollment.certReleased ? 'tog-on' : 'tog-off'" @click="editingEnrollment.certReleased = !editingEnrollment.certReleased">
                  {{ editingEnrollment.certReleased ? 'Released ✓' : 'Not Released' }}
                </button>
              </div>
              <div class="toggle-row" style="margin-top:.6rem">
                <div>
                  <div class="toggle-label">Release Transcript</div>
                  <div class="toggle-sub">Allow partner to download transcript.</div>
                </div>
                <button :class="editingEnrollment.transcriptReleased ? 'tog-on' : 'tog-off'" @click="editingEnrollment.transcriptReleased = !editingEnrollment.transcriptReleased">
                  {{ editingEnrollment.transcriptReleased ? 'Released ✓' : 'Not Released' }}
                </button>
              </div>
            </div>
          </div>
          <div class="drawer-actions">
            <button class="btn-cancel" @click="showEnrEdit = false">Close</button>
          </div>
        </template>
      </div>
    </transition>

    <!-- ── Document Review Modal ── -->
    <transition name="fade">
      <div v-if="reviewDoc" class="modal-overlay" @click.self="closeReview">
        <div class="review-modal">
          <div class="modal-hdr">
            <div>
              <h3>Review: {{ reviewDoc.label }}</h3>
              <p class="review-sub">{{ student?.[reviewDoc.field] }}</p>
            </div>
            <button class="btn-modal-close" @click="closeReview">✕</button>
          </div>
          <div class="review-actions">
            <button class="btn-reject" @click="rejectDoc">✕ Reject</button>
            <button class="btn-approve" @click="approveDoc">✓ Approve</button>
          </div>
          <div class="review-preview">
            <template v-if="reviewPreview.kind === 'image'">
              <img :src="reviewPreview.src" :alt="reviewDoc.label" class="preview-img" />
            </template>
            <template v-else-if="reviewPreview.kind === 'pdf'">
              <iframe :src="reviewPreview.src" class="preview-pdf" :title="reviewDoc.label"></iframe>
            </template>
            <div v-else class="preview-unsupported">
              <div class="preview-unsupported-icon">⚠</div>
              <div><strong>Preview not supported.</strong></div>
              <div class="preview-unsupported-sub">Only PNG, JPG, and PDF files can be previewed here.</div>
              <div class="preview-unsupported-file">{{ student?.[reviewDoc.field] }}</div>
            </div>
          </div>
        </div>
      </div>
    </transition>

    <!-- ── Add Enrolment Modal ── -->
    <transition name="fade">
      <div v-if="showAddEnrol" class="modal-overlay" @click.self="showAddEnrol = false">
        <div class="add-enrol-modal">
          <div class="modal-hdr">
            <h3>Add Programme Enrolment</h3>
            <button class="btn-modal-close" @click="showAddEnrol = false">✕</button>
          </div>
          <div class="drawer-form" style="max-height:60vh;overflow-y:auto">
            <div class="row-2">
              <div class="field">
                <label>Programme <span class="req">*</span></label>
                <select v-model="addEnrForm.programme" @change="addEnrForm.specialization = ''">
                  <option value="">— Select —</option>
                  <option v-for="p in programmeNames" :key="p">{{ p }}</option>
                </select>
              </div>
              <div class="field">
                <label>Specialization <span class="req">*</span></label>
                <select v-model="addEnrForm.specialization">
                  <option value="">— Select —</option>
                  <option v-for="m in specializationsForProgramme(addEnrForm.programme)" :key="m">{{ m }}</option>
                </select>
              </div>
            </div>
            <div class="row-2">
              <div class="field"><label>Commencement Date</label><input v-model="addEnrForm.commencementDate" type="date" /></div>
              <div class="field"><label>Duration of Study</label><input v-model="addEnrForm.durationOfStudy" placeholder="e.g. 18 months" /></div>
            </div>
            <div class="field">
              <label>Mode of Study</label>
              <select v-model="addEnrForm.modeOfStudy">
                <option>Distance/Online self-study</option>
                <option>Blended learning</option>
                <option>On-campus</option>
              </select>
            </div>
          </div>
          <div class="drawer-actions" style="border-top:1px solid #e8edf4;padding:1rem 1.5rem">
            <button class="btn-cancel" @click="showAddEnrol = false">Cancel</button>
            <button class="btn-save" :disabled="!addEnrForm.programme || !addEnrForm.specialization" @click="submitAddEnrolment">Add Enrolment</button>
          </div>
        </div>
      </div>
    </transition>

  </div>
</template>

<script setup>
import { ref, reactive, computed } from 'vue'
import { useRoute, useRouter, RouterLink } from 'vue-router'
import { auth } from '../store/auth.js'
import { students, ENROLLMENT_STATUSES, nextEnrollId } from '../mock/data.js'
import { gradesStore, saveGrades, isGraded } from '../store/grades.js'
import { resolveSubjects, getGradeInfo, getProgrammeNames, getSpecializationNames, getPartnerNames, corePrograms } from '../mock/programmes.js'
import { absences } from '../mock/absences.js'
import { announcements, nextAnnouncementId } from '../mock/announcements.js'

const programmeNames = getProgrammeNames()
const specializationNames     = getSpecializationNames()
const partnerNames   = getPartnerNames()

const route  = useRoute()
const router = useRouter()

const student = computed(() => students.find(s => String(s.id) === String(route.params.id)) ?? null)

function fmtDate(d) {
  if (!d) return '—'
  return new Date(d).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })
}
const todayFormatted = new Date().toLocaleDateString('en-GB', { day: '2-digit', month: 'long', year: 'numeric' })

// ── Enrollment card collapse ──────────────────────────────────────────────────
const enrollCollapsed = ref(false)

// ── Status (based on first enrollment) ───────────────────────────────────────
const studentStatus = computed(() => {
  const s   = student.value
  const enr = s?.enrollments?.[0]
  if (!enr) return 'new'
  if (enr.certReleased)      return 'approved'
  if (isGraded(s.studentId)) return 'graded'
  if (enr.admissionConfirmed) return 'confirmed'
  if (enr.paymentDone)       return 'admission'
  if (enr.offerType)         return 'offer'
  return 'new'
})
const STATUS_LABELS = { new: 'New', offer: 'Offer Issued', admission: 'Admission', confirmed: 'Admitted', graded: 'Graded', approved: 'Approved' }
const statusLabel = computed(() => STATUS_LABELS[studentStatus.value] ?? studentStatus.value)

// ── Admin note types ──────────────────────────────────────────────────────────
const ADMIN_NOTE_TYPES = [
  'Status Change',
  'Payment Update',
  'Document Release',
  'Admission Confirmed',
  'Active (final project)',
  'Applicant withdraw',
  'Dismissed',
  'Drop out',
  'Transferred',
  'General',
]

// ── Issue offer ───────────────────────────────────────────────────────────────
function issueOffer(enr, type) {
  enr.offerType = type
  showToast('Offer issued.')
}

// ── Specializations for programme (for add enrolment) ─────────────────────────────────
function specializationsForProgramme(programmeName) {
  const prog = corePrograms.find(p => p.name === programmeName)
  return prog ? prog.specializations.map(m => m.name) : []
}

// ── Doc slots ─────────────────────────────────────────────────────────────────
const allDocSlots = [
  { field: 'docPassport', label: 'Passport / ID',      verifyKey: 'passport', accept: '.pdf,.jpg,.jpeg,.png' },
  { field: 'docDegree',   label: 'Degree Certificate', verifyKey: 'degree',   accept: '.pdf,.jpg,.jpeg,.png' },
  { field: 'docLanguage', label: 'Language Result',    verifyKey: 'language', accept: '.pdf,.jpg,.jpeg,.png' },
  { field: 'docCV',       label: 'CV / Résumé',        verifyKey: 'cv',       accept: '.pdf,.doc,.docx'      },
]

const uploadedDocs = computed(() => {
  const s = student.value
  if (!s) return []
  return allDocSlots.filter(d => s[d.field]).map(d => ({ ...d, value: s[d.field] }))
})

function handleAdminUpload(field, e) {
  const s = student.value
  if (!s) return
  const name = e.target.files[0]?.name ?? null
  if (name) { s[field] = name; showToast(`Uploaded: ${name}`) }
}

// ── Edit Student drawer ───────────────────────────────────────────────────────
const showEdit  = ref(false)
const editForm  = reactive({
  firstName:'', lastName:'', dateOfBirth:'', email:'',
  passportId:'', address:'', partner:'',
  highestDegree:'', languageResult:'', yearsWorkExperience:0,
})

function openEdit() {
  const s = student.value
  Object.assign(editForm, {
    firstName: s.firstName, lastName: s.lastName,
    dateOfBirth: s.dateOfBirth ?? '', email: s.email ?? '',
    passportId: s.passportId, address: s.address ?? '',
    partner: s.partner,
    highestDegree: s.highestDegree ?? '', languageResult: s.languageResult ?? '',
    yearsWorkExperience: s.yearsWorkExperience ?? 0,
  })
  showEdit.value = true
}

function saveEdit() {
  const s = student.value
  Object.assign(s, {
    firstName: editForm.firstName, lastName: editForm.lastName,
    dateOfBirth: editForm.dateOfBirth, email: editForm.email,
    passportId: editForm.passportId, address: editForm.address,
    partner: editForm.partner,
    highestDegree: editForm.highestDegree, languageResult: editForm.languageResult,
    yearsWorkExperience: editForm.yearsWorkExperience,
  })
  showEdit.value = false
  showToast('Student record updated.')
}

// ── Enrollment Edit drawer (grades / notes / activity) ────────────────────────
const showEnrEdit       = ref(false)
const editingEnrollment = ref(null)
const enrEditTab        = ref('grades')
const saveSuccess       = ref(false)
const gradeRows         = ref([])
const noteTexts         = reactive({})
const noteChanges       = reactive({})

function openEnrEdit(enr) {
  editingEnrollment.value = enr
  enrEditTab.value        = 'grades'
  saveSuccess.value       = false
  // Load grades
  const modules = resolveSubjects(student.value.partner, enr.programme, enr.specialization)
  const saved   = gradesStore[student.value.studentId] ?? {}
  gradeRows.value = modules.map(mod => {
    const ex = saved[mod.name]
    return reactive({
      name: mod.name,
      credits: mod.ects ?? mod.credits ?? 15,
      ibssGrade: ex?.ibssGrade ?? '',
      ukGrade: ex?.ukGrade ?? null,
      ectsGrade: ex?.ectsGrade ?? null,
      ectsPoints: ex?.ectsPoints ?? null,
      gradePoints: ex?.gradePoints ?? null,
      remark: ex?.remark ?? null,
    })
  })
  showEnrEdit.value = true
}

function recalcRow(row) {
  const info = getGradeInfo(row.ibssGrade)
  if (info) {
    row.ukGrade    = info.ukGrade
    row.ectsGrade  = info.ectsGrade
    row.ectsPoints = info.ectsPoints
    row.gradePoints = row.credits * info.ectsPoints
    row.remark     = info.remark
  } else {
    row.ukGrade = row.ectsGrade = row.ectsPoints = row.gradePoints = row.remark = null
  }
}

const totalCredits     = computed(() => gradeRows.value.reduce((s, r) => s + r.credits, 0))
const totalGradePoints = computed(() => gradeRows.value.reduce((s, r) => s + (r.gradePoints ?? 0), 0))
const gpa = computed(() => {
  const gr = gradeRows.value.filter(r => r.gradePoints != null)
  if (!gr.length) return '—'
  const cr = gr.reduce((s, r) => s + r.credits, 0)
  return cr ? (gr.reduce((s, r) => s + r.gradePoints, 0) / cr).toFixed(2) : '—'
})

function saveGradesForStudent() {
  const gradeMap = {}
  for (const row of gradeRows.value) {
    gradeMap[row.name] = {
      ibssGrade: row.ibssGrade, ukGrade: row.ukGrade,
      ectsGrade: row.ectsGrade, ectsPoints: row.ectsPoints,
      gradePoints: row.gradePoints, remark: row.remark, credits: row.credits,
    }
  }
  saveGrades(student.value.studentId, gradeMap)
  saveSuccess.value = true
  setTimeout(() => { showEnrEdit.value = false; saveSuccess.value = false }, 1500)
}

function addChangeNote(enr) {
  const text = noteTexts[enr.id]
  if (!text) return
  enr.changeNotes.push({
    id: Date.now(), text,
    requestedChange: noteChanges[enr.id] || 'General',
    date: new Date().toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }),
  })
  noteTexts[enr.id]   = ''
  noteChanges[enr.id] = ''
}

// ── Add Enrolment modal ───────────────────────────────────────────────────────
const showAddEnrol = ref(false)
const addEnrForm   = reactive({ programme: '', specialization: '', commencementDate: '', durationOfStudy: '', modeOfStudy: 'Distance/Online self-study' })

function submitAddEnrolment() {
  const s = student.value
  if (!s || !addEnrForm.programme || !addEnrForm.specialization) return
  s.enrollments.push({
    id: nextEnrollId(),
    programme: addEnrForm.programme, specialization: addEnrForm.specialization,
    commencementDate: addEnrForm.commencementDate, durationOfStudy: addEnrForm.durationOfStudy,
    modeOfStudy: addEnrForm.modeOfStudy,
    enrollmentStatus: 'Active',
    coursesCompleted: 0, coursesRequired: 4,
    finalProjectStatus: 'not applicable',
    tuitionFeeStatus: 'unpaid', otherFeesStatus: 'not applicable',
    paymentDone: false, offerType: null, admissionConfirmed: false,
    certReleased: false, transcriptReleased: false,
    changeNotes: [],
  })
  showAddEnrol.value = false
  showToast('Enrolment added.')
}

// ── Print Offer / Admission Letter ────────────────────────────────────────────
function printOfferLetter(s, enr, type) {
  const title   = type === 'offer' ? 'LETTER OF OFFER' : 'LETTER OF ADMISSION'
  const isOffer = type === 'offer'
  const body    = isOffer
    ? `We are pleased to offer you a place at the International Business School of Scandinavia through our partner institution <strong>${s.partner}</strong>. This offer is subject to the verification of your academic qualifications and supporting documents.`
    : `We are pleased to confirm your admission to the International Business School of Scandinavia through our partner institution <strong>${s.partner}</strong>. Your place has been formally reserved and we look forward to welcoming you.`

  const html = `<!DOCTYPE html><html><head><meta charset="utf-8"><title>${title} — ${s.studentId}</title>
  <style>
    @page { size: A4; margin: 22mm 20mm; }
    body { font-family: Arial, sans-serif; font-size: 10.5pt; color: #111; line-height: 1.6; }
    .logo { font-size: 26pt; font-weight: 900; color: #003366; }
    .org strong { color: #003366; } .org span { color: #666; font-size:9pt; }
    hr { border: none; border-top: 2px solid #003366; margin: 10px 0 18px; }
    .type { font-size:14pt; font-weight:900; color:#003366; text-transform:uppercase; letter-spacing:1px; margin:18px 0 10px; }
    table { width:100%; border-collapse:collapse; margin:16px 0; }
    td { padding:5px 8px; border-bottom:1px solid #eee; font-size:10pt; }
    td:first-child { width:36%; color:#555; font-weight:bold; }
    .sign { margin-top:40px; } .sig-line { border-top:1px solid #555; width:200px; margin:28px 0 5px; }
  </style></head><body>
  <div style="display:flex;align-items:center;gap:14px;margin-bottom:6px">
    <div class="logo">IBSS</div>
    <div class="org"><strong>International Business School of Scandinavia</strong><br><span>in partnership with ${s.partner}</span></div>
  </div>
  <hr/>
  <p>${todayFormatted}</p>
  <div class="type">${title}</div>
  <p>Dear ${s.firstName} ${s.lastName},</p>
  <p>${body}</p>
  <table>
    <tr><td>Student ID</td><td>${s.studentId}</td></tr>
    <tr><td>Full Name</td><td>${s.firstName} ${s.lastName}</td></tr>
    <tr><td>Programme</td><td>${enr.programme}</td></tr>
    <tr><td>Specialization</td><td>${enr.specialization}</td></tr>
    <tr><td>Commencement Date</td><td>${fmtDate(enr.commencementDate)}</td></tr>
    <tr><td>Mode of Study</td><td>${enr.modeOfStudy || '—'}</td></tr>
    <tr><td>Partner Institution</td><td>${s.partner}</td></tr>
  </table>
  <p>${isOffer ? 'Please accept this offer by confirming your enrolment with your partner institution.' : 'Please retain this letter as confirmation of your admission.'}</p>
  <div class="sign"><p>Yours sincerely,</p><div class="sig-line"></div><p><strong>IBSS Admissions Office</strong></p><p style="color:#555;font-size:9pt">International Business School of Scandinavia</p></div>
  <script>window.onload=function(){window.print()}<\/script></body></html>`

  const win = window.open('', '_blank', 'width=820,height=700')
  win.document.write(html); win.document.close()
}

// ── Download centre ───────────────────────────────────────────────────────────
const dlSelected = ref([])
const allAvailable = computed(() => {
  const items = uploadedDocs.value.map(d => 'up_' + d.field)
  if (isGraded(student.value?.studentId)) {
    items.push('grade_digital', 'grade_physical', 'cert_digital', 'cert_physical')
  }
  return items
})
const allSelected = computed(() =>
  allAvailable.value.length > 0 && allAvailable.value.every(v => dlSelected.value.includes(v))
)
function toggleAll() {
  if (allSelected.value) dlSelected.value = []
  else dlSelected.value = [...allAvailable.value]
}

function downloadSelected() {
  const s = student.value
  let delay = 0
  for (const key of dlSelected.value) {
    if (key.startsWith('up_')) {
      const field = key.replace('up_', '')
      const doc = uploadedDocs.value.find(d => d.field === field)
      if (doc) setTimeout(() => showToast(`Mock download: "${doc.value}"`), delay)
      delay += 400
    } else if (key === 'grade_digital') {
      setTimeout(() => printGradeReport(s, true), delay); delay += 600
    } else if (key === 'grade_physical') {
      setTimeout(() => printGradeReport(s, false), delay); delay += 600
    } else if (key === 'cert_digital') {
      setTimeout(() => printCertificate(s, true), delay); delay += 600
    } else if (key === 'cert_physical') {
      setTimeout(() => printCertificate(s, false), delay); delay += 600
    }
  }
}

// ── Grade Report print ────────────────────────────────────────────────────────
function printGradeReport(s, withStamp) {
  const grades = gradesStore[s.studentId] ?? {}
  const rows = Object.entries(grades).map(([name, r]) =>
    `<tr><td>${name}</td><td class="tc">${r.credits ?? ''}</td><td class="tc">${r.ibssGrade ?? '—'}</td><td class="tc">${r.ukGrade ?? '—'}</td><td class="tc">${r.ectsGrade ?? '—'}</td><td class="tc">${r.ectsPoints != null ? r.ectsPoints.toFixed(1) : '—'}</td><td class="tc gp">${r.gradePoints != null ? r.gradePoints.toFixed(1) : '—'}</td></tr>`
  ).join('')
  const totalCr = Object.values(grades).reduce((a, r) => a + (r.credits ?? 0), 0)
  const totalGP = Object.values(grades).reduce((a, r) => a + (r.gradePoints ?? 0), 0)
  const gpaRows = Object.values(grades).filter(r => r.gradePoints != null)
  const gpa2 = gpaRows.length ? (gpaRows.reduce((a, r) => a + r.gradePoints, 0) / gpaRows.reduce((a, r) => a + (r.credits ?? 0), 0)).toFixed(2) : '—'

  const stampBlock = withStamp ? `
    <div class="stamp"><div class="stamp-text">APPROVED ✓</div><div class="stamp-date">${todayFormatted}</div></div>
    <div class="sign"><p>Verified by,</p><div class="sig-line"></div><p class="sig-name">IBSS Academic Registry</p><p class="sig-org">International Business School of Scandinavia</p>
    <p class="sig-note">Digitally verified — ${todayFormatted}</p></div>` : `
    <div class="phys-note">FOR PHYSICAL SIGNING — Stamp and signature to be applied manually.</div>
    <div class="sign-blank"><p>Verified by,</p><div class="sig-line"></div><p class="sig-name">_______________________________</p><p class="sig-org">IBSS Academic Registry</p></div>`

  const enr = s.enrollments?.[0] ?? {}
  const html = `<!DOCTYPE html><html><head><meta charset="utf-8"><title>Grade Report — ${s.studentId}</title>
  <style>
    @page { size: A4; margin: 22mm 20mm; }
    body { font-family: Arial, sans-serif; font-size: 10.5pt; color: #111; line-height: 1.5; }
    .header { display: flex; align-items: center; gap: 16px; margin-bottom: 8px; }
    .logo { font-size: 28pt; font-weight: 900; color: #003366; }
    .org strong { font-size: 12pt; color: #003366; } .org span { font-size: 9pt; color: #666; }
    hr { border: none; border-top: 2px solid #003366; margin: 10px 0 18px; }
    .info-table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }
    .info-table td { padding: 5px 8px; border-bottom: 1px solid #eee; font-size: 10pt; }
    .info-table td:first-child { width: 36%; color: #555; font-weight: bold; }
    h4 { color: #003366; font-size: 11pt; margin-bottom: 8px; text-transform: uppercase; letter-spacing: 0.5px; }
    .grade-table { width: 100%; border-collapse: collapse; margin-bottom: 18px; font-size: 9.5pt; }
    .grade-table th { background: #003366; color: #fff; padding: 6px 8px; text-align: center; font-size: 8.5pt; }
    .grade-table th:first-child { text-align: left; }
    .grade-table td { padding: 5px 8px; border-bottom: 1px solid #e8edf4; }
    .tc { text-align: center; } .gp { background: #eaf0f8; font-weight: 600; }
    .total-row td { background: #f0f3f7; font-weight: bold; border-top: 2px solid #ccd; }
    .gpa-row td { background: #e8f0f8; }
    .gpa-label { text-align: right; font-style: italic; padding-right: 8px; }
    .stamp { margin: 24px 0 0; padding: 12px 18px; border: 3px double #0d6b55; display: inline-block; }
    .stamp-text { font-size: 14pt; font-weight: 900; color: #0d6b55; letter-spacing: 2px; }
    .stamp-date { font-size: 9pt; color: #555; margin-top: 2px; }
    .sign { margin-top: 28px; } .sig-line { border-top: 1px solid #555; width: 180px; margin: 24px 0 5px; }
    .sig-name { font-weight: bold; font-size: 10pt; margin: 0; } .sig-org { font-size: 9pt; color: #555; }
    .sig-note { font-size: 8.5pt; color: #0d6b55; margin-top: 4px; font-style: italic; }
    .phys-note { margin: 24px 0 8px; padding: 8px 14px; border: 2px dashed #e0a800; color: #7a5200; font-size: 9pt; font-weight: bold; display: inline-block; }
    .sign-blank { margin-top: 20px; }
  </style></head><body>
  <div class="header"><div class="logo">IBSS</div><div class="org"><strong>International Business School of Scandinavia</strong><br><span>Grade Report${withStamp ? ' — Official Digital Copy' : ' — Physical Copy (Unsigned)'}</span></div></div>
  <hr/>
  <table class="info-table">
    <tr><td>Student Name</td><td>${s.firstName} ${s.lastName}</td></tr>
    <tr><td>Student ID</td><td>${s.studentId}</td></tr>
    <tr><td>Programme</td><td>${enr.programme ?? '—'}</td></tr>
    <tr><td>Specialization</td><td>${enr.specialization ?? '—'}</td></tr>
    <tr><td>Partner Institution</td><td>${s.partner}</td></tr>
    <tr><td>Commencement Date</td><td>${fmtDate(enr.commencementDate)}</td></tr>
  </table>
  <h4>Subject Results</h4>
  <table class="grade-table">
    <thead><tr><th>Subject</th><th>Credits</th><th>IBSS</th><th>UK</th><th>ECTS</th><th>ECTS Pts</th><th>Grade Pts</th></tr></thead>
    <tbody>${rows}</tbody>
    <tfoot>
      <tr class="total-row"><td><strong>Total</strong></td><td class="tc">${totalCr}</td><td class="tc" colspan="4"></td><td class="tc gp">${totalGP.toFixed(1)}</td></tr>
      <tr class="gpa-row"><td colspan="6" class="gpa-label">Grade Point Average (GPA)</td><td class="tc gp">${gpa2}</td></tr>
    </tfoot>
  </table>
  ${stampBlock}
  <script>window.onload=function(){window.print()}<\/script></body></html>`

  const win = window.open('', '_blank', 'width=820,height=700')
  win.document.write(html); win.document.close()
}

// ── Academic Certificate print ────────────────────────────────────────────────
function printCertificate(s, withStamp) {
  const grades = gradesStore[s.studentId] ?? {}
  const rows = Object.entries(grades).map(([name, r]) =>
    `<tr><td>${name}</td><td class="tc">${r.credits ?? ''}</td><td class="tc">${r.ibssGrade ?? '—'}</td><td class="tc">${r.ukGrade ?? '—'}</td><td class="tc">${r.ectsGrade ?? '—'}</td><td class="tc">${r.ectsPoints != null ? r.ectsPoints.toFixed(1) : '—'}</td><td class="tc gp">${r.gradePoints != null ? r.gradePoints.toFixed(1) : '—'}</td></tr>`
  ).join('')
  const totalCr = Object.values(grades).reduce((a, r) => a + (r.credits ?? 0), 0)
  const totalGP = Object.values(grades).reduce((a, r) => a + (r.gradePoints ?? 0), 0)
  const gpaRows = Object.values(grades).filter(r => r.gradePoints != null)
  const gpa2 = gpaRows.length ? (gpaRows.reduce((a, r) => a + r.gradePoints, 0) / gpaRows.reduce((a, r) => a + (r.credits ?? 0), 0)).toFixed(2) : '—'

  const enr = s.enrollments?.[0] ?? {}
  const bottomBlock = withStamp ? `
    <div class="cert-declaration"><p>This is to certify that the above-named student has successfully completed all academic requirements for the award of <strong>${enr.programme ?? ''}</strong> — <strong>${enr.specialization ?? ''}</strong> at the International Business School of Scandinavia.</p></div>
    <div class="stamp"><div class="stamp-text">APPROVED ✓</div><div class="stamp-date">${todayFormatted}</div></div>
    <div class="sign"><p>Certified by,</p><div class="sig-line"></div><p class="sig-name">IBSS Academic Registrar</p><p class="sig-org">International Business School of Scandinavia</p>
    <p class="sig-note">Digitally certified — ${todayFormatted}</p></div>` : `
    <div class="cert-declaration"><p>This is to certify that the above-named student has successfully completed all academic requirements for the award of <strong>${enr.programme ?? ''}</strong> — <strong>${enr.specialization ?? ''}</strong> at the International Business School of Scandinavia.</p></div>
    <div class="phys-note">FOR PHYSICAL SIGNING — Official seal and signature to be applied manually.</div>
    <div class="sign-blank"><div style="display:flex;gap:60px;margin-top:20px"><div><div class="sig-line"></div><p class="sig-name">Academic Registrar</p></div><div><div class="sig-line"></div><p class="sig-name">Date</p></div></div></div>`

  const html = `<!DOCTYPE html><html><head><meta charset="utf-8"><title>Academic Certificate — ${s.studentId}</title>
  <style>
    @page { size: A4; margin: 22mm 20mm; }
    body { font-family: Georgia, serif; font-size: 10.5pt; color: #111; line-height: 1.5; }
    .header { display: flex; align-items: center; gap: 16px; margin-bottom: 8px; }
    .logo { font-size: 28pt; font-weight: 900; color: #003366; letter-spacing: -1px; }
    .org strong { font-size: 12pt; color: #003366; } .org span { font-size: 9pt; color: #666; }
    hr { border: none; border-top: 2.5px solid #003366; margin: 10px 0 18px; }
    .cert-title { text-align: center; font-size: 16pt; font-weight: bold; color: #003366; letter-spacing: 1px; margin: 0 0 18px; text-transform: uppercase; }
    .info-table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }
    .info-table td { padding: 5px 8px; border-bottom: 1px solid #eee; font-size: 10pt; }
    .info-table td:first-child { width: 36%; color: #555; font-weight: bold; }
    h4 { color: #003366; font-size: 11pt; margin-bottom: 8px; text-transform: uppercase; letter-spacing: 0.5px; font-family: Arial, sans-serif; }
    .grade-table { width: 100%; border-collapse: collapse; margin-bottom: 18px; font-size: 9.5pt; font-family: Arial, sans-serif; }
    .grade-table th { background: #003366; color: #fff; padding: 6px 8px; text-align: center; font-size: 8.5pt; }
    .grade-table th:first-child { text-align: left; }
    .grade-table td { padding: 5px 8px; border-bottom: 1px solid #e8edf4; }
    .tc { text-align: center; } .gp { background: #eaf0f8; font-weight: 600; }
    .total-row td { background: #f0f3f7; font-weight: bold; border-top: 2px solid #ccd; font-family: Arial, sans-serif; }
    .gpa-row td { background: #e8f0f8; font-family: Arial, sans-serif; }
    .gpa-label { text-align: right; font-style: italic; padding-right: 8px; }
    .cert-declaration { margin: 20px 0 16px; padding: 12px 16px; background: #f7fbff; border-left: 4px solid #003366; font-size: 10pt; line-height: 1.6; }
    .stamp { margin: 16px 0 0; padding: 12px 18px; border: 3px double #0d6b55; display: inline-block; }
    .stamp-text { font-size: 14pt; font-weight: 900; color: #0d6b55; letter-spacing: 2px; font-family: Arial, sans-serif; }
    .stamp-date { font-size: 9pt; color: #555; margin-top: 2px; font-family: Arial, sans-serif; }
    .sign { margin-top: 28px; font-family: Arial, sans-serif; } .sig-line { border-top: 1px solid #555; width: 180px; margin: 24px 0 5px; }
    .sig-name { font-weight: bold; font-size: 10pt; margin: 0; } .sig-org { font-size: 9pt; color: #555; }
    .sig-note { font-size: 8.5pt; color: #0d6b55; margin-top: 4px; font-style: italic; }
    .phys-note { margin: 16px 0 8px; padding: 8px 14px; border: 2px dashed #e0a800; color: #7a5200; font-size: 9pt; font-weight: bold; display: inline-block; font-family: Arial, sans-serif; }
    .sign-blank { font-family: Arial, sans-serif; }
  </style></head><body>
  <div class="header"><div class="logo">IBSS</div><div class="org"><strong>International Business School of Scandinavia</strong><br><span>Academic Certificate${withStamp ? ' — Official Digital Copy' : ' — Physical Copy (Unsigned)'}</span></div></div>
  <hr/>
  <div class="cert-title">Academic Transcript &amp; Certificate</div>
  <table class="info-table">
    <tr><td>Student Name</td><td>${s.firstName} ${s.lastName}</td></tr>
    <tr><td>Student ID</td><td>${s.studentId}</td></tr>
    <tr><td>Programme</td><td>${enr.programme ?? '—'}</td></tr>
    <tr><td>Specialization / Specialisation</td><td>${enr.specialization ?? '—'}</td></tr>
    <tr><td>Partner Institution</td><td>${s.partner}</td></tr>
    <tr><td>Commencement Date</td><td>${fmtDate(enr.commencementDate)}</td></tr>
    <tr><td>Mode of Study</td><td>${enr.modeOfStudy || '—'}</td></tr>
  </table>
  <h4>Academic Results</h4>
  <table class="grade-table">
    <thead><tr><th>Subject</th><th>Credits</th><th>IBSS</th><th>UK</th><th>ECTS</th><th>ECTS Pts</th><th>Grade Pts</th></tr></thead>
    <tbody>${rows}</tbody>
    <tfoot>
      <tr class="total-row"><td><strong>Total</strong></td><td class="tc">${totalCr}</td><td class="tc" colspan="4"></td><td class="tc gp">${totalGP.toFixed(1)}</td></tr>
      <tr class="gpa-row"><td colspan="6" class="gpa-label">Grade Point Average (GPA)</td><td class="tc gp">${gpa2}</td></tr>
    </tfoot>
  </table>
  ${bottomBlock}
  <script>window.onload=function(){window.print()}<\/script></body></html>`

  const win = window.open('', '_blank', 'width=820,height=700')
  win.document.write(html); win.document.close()
}

// ── Absences ──────────────────────────────────────────────────────────────────
const studentAbsences = computed(() =>
  absences.filter(a => a.studentId === student.value?.studentId).slice().reverse()
)

// ── Messages ──────────────────────────────────────────────────────────────────
const studentAnnouncements = computed(() =>
  announcements.filter(a => a.targetStudentId === null || a.targetStudentId === student.value?.studentId).slice().reverse()
)
const msgForm = reactive({ title: '', body: '' })
const msgSent = ref(false)
function sendMsg() {
  announcements.push({
    id: nextAnnouncementId(),
    title: msgForm.title, body: msgForm.body,
    targetStudentId: student.value.studentId,
    createdAt: new Date().toISOString(),
  })
  Object.assign(msgForm, { title: '', body: '' })
  msgSent.value = true
  setTimeout(() => { msgSent.value = false }, 2500)
  showToast('Message sent to student.')
}

// ── Toast ─────────────────────────────────────────────────────────────────────
const toast = ref('')
let toastTimer = null
function showToast(msg) {
  toast.value = msg
  clearTimeout(toastTimer)
  toastTimer = setTimeout(() => { toast.value = '' }, 2800)
}
// ── Document review modal ────────────────────────────────────────────────────
const reviewDoc = ref(null)

const reviewPreview = computed(() => {
  if (!reviewDoc.value || !student.value) return { kind: 'none' }
  const filename = student.value[reviewDoc.value.field] ?? ''
  const ext = filename.split('.').pop()?.toLowerCase()
  if (ext === 'png' || ext === 'jpg' || ext === 'jpeg') {
    return { kind: 'image', src: '/ibss-logo-wide.jpg' }
  }
  if (ext === 'pdf') {
    return { kind: 'pdf', src: '/sample-document.pdf' }
  }
  return { kind: 'unsupported' }
})

function openReview(doc) { reviewDoc.value = doc }
function closeReview()   { reviewDoc.value = null }

function approveDoc() {
  const s = student.value
  if (!s || !reviewDoc.value) return
  if (!s.docsVerified) s.docsVerified = {}
  s.docsVerified[reviewDoc.value.verifyKey] = true
  showToast(`Approved: ${reviewDoc.value.label}`)
  closeReview()
}

function rejectDoc() {
  const s = student.value
  if (!s || !reviewDoc.value) return
  if (!s.docsVerified) s.docsVerified = {}
  s.docsVerified[reviewDoc.value.verifyKey] = false
  showToast(`Rejected: ${reviewDoc.value.label}`)
  closeReview()
}

function logout() { auth.logout(); router.push('/login') }
</script>

<style scoped>
.page-wrapper { min-height: 100vh; background: #f2f5f9; }

/* Navbar */
.navbar { background: #003366; color: #fff; display: flex; align-items: center; justify-content: space-between; padding: 0.85rem 2rem; gap: 1rem; }
.brand-text { font-size: 1.05rem; font-weight: 700; white-space: nowrap; }
.nav-links { display: flex; gap: 0.25rem; flex: 1; padding: 0 1rem; }
.nav-link { color: rgba(255,255,255,0.75); text-decoration: none; padding: 0.35rem 0.9rem; border-radius: 5px; font-size: 0.88rem; transition: background 0.15s; }
.nav-link:hover, .nav-link.router-link-active { background: rgba(255,255,255,0.15); color: #fff; }
.nav-right { display: flex; align-items: center; gap: 1rem; }
.nav-user { font-size: 0.85rem; opacity: 0.85; }
.btn-logout { background: transparent; border: 1.5px solid rgba(255,255,255,0.55); color: #fff; padding: 0.3rem 0.85rem; border-radius: 5px; cursor: pointer; font-size: 0.82rem; }
.btn-logout:hover { background: rgba(255,255,255,0.13); }

/* Container */
.container { max-width: 1200px; margin: 2rem auto; padding: 0 1.5rem; display: flex; flex-direction: column; gap: 1.25rem; }
.not-found { padding: 3rem; color: #888; }
.back-link { font-size: 0.88rem; color: #003366; text-decoration: none; font-weight: 600; display: inline-block; }
.back-link:hover { text-decoration: underline; }

/* Header */
.student-header { display: flex; align-items: center; justify-content: space-between; }
.header-left { display: flex; align-items: center; gap: 0.85rem; }
.student-name { font-size: 1.5rem; font-weight: 700; color: #003366; margin: 0; }
.sid-badge { font-family: ui-monospace, monospace; font-size: 0.88rem; background: #e8f0f8; color: #003366; border: 1px solid #c5d8f0; border-radius: 5px; padding: 2px 10px; }
.status-badge { padding: 4px 14px; border-radius: 20px; font-size: 0.78rem; font-weight: 700; white-space: nowrap; }
.status-new       { background: #f0f3f7; color: #888; border: 1px solid #d0d7e0; }
.status-offer     { background: #e8f4fd; color: #1a6ca8; border: 1px solid #b8d9f5; }
.status-admission { background: #dbeafe; color: #1d4ed8; border: 1px solid #93c5fd; }
.status-confirmed { background: #fef3c7; color: #92400e; border: 1px solid #fcd34d; }
.status-graded    { background: #e0f5f0; color: #0d6b55; border: 1px solid #a8ddd0; }
.status-approved  { background: #d1fae5; color: #065f46; border: 1px solid #6ee7b7; }
.header-right { display: flex; align-items: center; gap: 0.85rem; }
.btn-edit-stu { background: #fff; border: 1.5px solid #c5d8f0; color: #003366; border-radius: 7px; padding: 0.42rem 1.1rem; font-size: 0.86rem; font-weight: 600; cursor: pointer; }
.btn-edit-stu:hover { background: #eef4fb; }

/* Info card */
.info-card { background: #fff; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.06); padding: 1.25rem 1.5rem; }
.info-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 0.85rem 1rem; }
.info-item { display: flex; flex-direction: column; gap: 0.2rem; }
.info-label { font-size: 0.71rem; text-transform: uppercase; letter-spacing: 0.05em; color: #888; font-weight: 600; }
.info-item span { font-size: 0.88rem; color: #222; }
.mono { font-family: ui-monospace, monospace; font-size: 0.82rem; }

/* Student card (enrollment card) */
.student-card { background: #fff; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.06); overflow: hidden; }
.sc-header { display: flex; align-items: center; justify-content: space-between; padding: 0.85rem 1.25rem; border-bottom: 1.5px solid #e8edf4; background: #fafbfc; }
.sc-id-name { display: flex; align-items: center; gap: 0.55rem; }
.sc-sid { font-family: ui-monospace, monospace; font-size: 0.82rem; color: #003366; font-weight: 700; background: #e8f0f8; border: 1px solid #c5d8f0; border-radius: 4px; padding: 1px 7px; }
.sc-sep { color: #bbb; }
.sc-name { font-size: 0.95rem; font-weight: 700; color: #222; }
.sc-tag-admin { font-size: 0.68rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.05em; background: #003366; color: #fff; border-radius: 4px; padding: 1px 7px; }
.sc-header-right { display: flex; align-items: center; gap: 0.75rem; }
.sc-summary { font-size: 0.8rem; color: #888; }
.sc-chevron { font-size: 1.1rem; color: #aaa; transition: transform 0.2s; display: inline-block; }
.sc-chevron.collapsed { transform: rotate(-90deg); }

/* Enrollment table */
.sc-body { overflow: auto; }
.enr-table-wrap { overflow-x: auto; }
.enr-table { width: 100%; border-collapse: collapse; min-width: 900px; }
.enr-table thead th { background: #f0f4f8; color: #555; font-size: 0.73rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.04em; padding: 0.6rem 0.9rem; text-align: left; border-bottom: 2px solid #e0e8f4; white-space: nowrap; }
.enr-row { border-bottom: 1px solid #f0f3f7; }
.enr-row:last-child { border-bottom: none; }
.enr-row td { padding: 0.75rem 0.9rem; vertical-align: top; }
.th-prog  { width: 22%; }
.th-status { width: 14%; }
.th-acad  { width: 18%; }
.th-pay   { width: 17%; }
.th-rel   { width: 16%; }
.th-edit  { width: 80px; }

/* Programme cell */
.td-prog {}
.prog-name-main { font-size: 0.88rem; font-weight: 700; color: #003366; }
.prog-specialization-sub { font-size: 0.8rem; color: #555; margin-top: 1px; margin-bottom: 0.5rem; }

/* Doc links in enrollment table */
.doc-list { display: flex; flex-direction: column; gap: 0.22rem; }
.doc-row { display: flex; align-items: center; gap: 0.35rem; font-size: 0.76rem; text-decoration: none; border-radius: 4px; padding: 2px 5px; transition: background 0.12s; }
.doc-avail { color: #003366; }
.doc-avail:hover { background: #e8f0f8; }
.doc-disabled { color: #bbb; cursor: default; }
.doc-icon { font-size: 0.9rem; flex-shrink: 0; }
.doc-na { font-size: 0.72rem; color: #ccc; margin-left: 2px; }

/* Status cell */
.td-status { vertical-align: middle; }
.status-sel-inline { width: 100%; padding: 0.42rem 0.5rem; border: 1.5px solid #ccc; border-radius: 6px; font-size: 0.82rem; background: #fff; }
.status-sel-inline:focus { border-color: #003366; outline: none; }

/* Academic Progress cell */
.td-acad {}
.ap-row { display: flex; align-items: center; gap: 0.4rem; flex-wrap: wrap; }
.ap-lbl { font-size: 0.75rem; color: #888; font-weight: 600; white-space: nowrap; }
.course-inp { display: flex; align-items: center; gap: 0.25rem; }
.inp-num-sm { width: 44px; padding: 0.28rem 0.35rem; border: 1.5px solid #ccc; border-radius: 5px; font-size: 0.82rem; text-align: center; }
.inp-num-sm:focus { border-color: #003366; outline: none; }
.num-sep { color: #888; font-size: 0.88rem; }
.sel-sm { padding: 0.28rem 0.4rem; border: 1.5px solid #ccc; border-radius: 5px; font-size: 0.8rem; background: #fff; }
.sel-sm:focus { border-color: #003366; outline: none; }
.rel-yes { font-size: 0.76rem; color: #0d6b55; font-weight: 700; }
.rel-no  { font-size: 0.76rem; color: #bbb; }
.confirmed-chip { font-size: 0.72rem; font-weight: 700; padding: 2px 8px; background: #d1fae5; color: #065f46; border: 1px solid #6ee7b7; border-radius: 20px; }

/* Payment cell */
.td-pay {}
.pay-section { display: flex; flex-direction: column; gap: 0.2rem; }

/* Releases cell */
.td-rel { display: flex; flex-direction: column; gap: 0; }
.tog-wide { width: 100%; white-space: nowrap; }
.tog-off { background: #f0f3f7; color: #888; border: 1px solid #d0d7e0; border-radius: 20px; padding: 0.28rem 0.75rem; font-size: 0.76rem; cursor: pointer; font-weight: 600; }
.tog-off:hover { background: #e0e8f0; }
.tog-on  { background: #d1fae5; color: #065f46; border: 1px solid #6ee7b7; border-radius: 20px; padding: 0.28rem 0.75rem; font-size: 0.76rem; cursor: pointer; font-weight: 700; }
.tog-on:hover { background: #a7f3d0; }
.btn-confirm-adm-sm { font-size: 0.74rem; padding: 0.28rem 0.65rem; background: #003366; color: #fff; border: none; border-radius: 5px; cursor: pointer; white-space: nowrap; }
.btn-confirm-adm-sm:hover { background: #0055a5; }
.btn-issue-offer { font-size: 0.74rem; padding: 0.28rem 0.65rem; background: #f0f8ff; color: #1a6ca8; border: 1px solid #b8d9f5; border-radius: 5px; cursor: pointer; white-space: nowrap; }
.btn-issue-offer:hover { background: #dbeafe; }
.offer-chip { font-size: 0.72rem; font-weight: 700; padding: 2px 8px; border-radius: 20px; }
.offer-chip-full { background: #e8f4fd; color: #1a6ca8; border: 1px solid #b8d9f5; }
.offer-chip-cond { background: #fef3c7; color: #92400e; border: 1px solid #fcd34d; }

/* Edit button */
.td-edit { vertical-align: middle; text-align: center; }
.btn-edit-enr { background: #f0f4f8; color: #003366; border: 1px solid #c5d8f0; border-radius: 6px; padding: 0.38rem 0.75rem; font-size: 0.8rem; font-weight: 600; cursor: pointer; white-space: nowrap; }
.btn-edit-enr:hover { background: #e0ecfa; }

/* Card footer */
.sc-footer { padding: 0.75rem 1.25rem; background: #fafbfc; border-top: 1px solid #f0f3f7; }
.btn-add-enr { background: #003366; color: #fff; border: none; border-radius: 6px; padding: 0.45rem 1rem; font-size: 0.82rem; font-weight: 600; cursor: pointer; }
.btn-add-enr:hover { background: #0055a5; }

/* Detail card */
.detail-card { background: #fff; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.06); padding: 1.25rem 1.5rem; }
.card-title { font-size: 0.95rem; font-weight: 700; color: #003366; margin: 0 0 1rem; padding-bottom: 0.6rem; border-bottom: 1.5px solid #e8edf4; }

/* Uploaded Documents */
.doc-list { display: flex; flex-direction: column; gap: 0.6rem; }
.doc-row-admin { display: flex; align-items: center; gap: 0.75rem; padding: 0.6rem 0.75rem; background: #f7f9fb; border: 1px solid #e8edf4; border-radius: 7px; }
.doc-row-missing { background: #fafbfc; border-style: dashed; }
.doc-type-icon { font-size: 1.2rem; flex-shrink: 0; }
.doc-info { flex: 1; min-width: 0; }
.doc-type-label { font-size: 0.78rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.04em; color: #666; display: block; }
.doc-filename { font-size: 0.82rem; color: #003366; font-weight: 600; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; display: block; }
.doc-missing { font-size: 0.8rem; color: #bbb; font-style: italic; display: block; }
.doc-verified-badge { font-size: 0.72rem; font-weight: 700; padding: 2px 8px; border-radius: 20px; background: #d1fae5; color: #065f46; border: 1px solid #6ee7b7; white-space: nowrap; flex-shrink: 0; }
.doc-unverified { background: #f0f3f7; color: #888; border-color: #d0d7e0; }
.doc-actions { display: flex; gap: 0.4rem; align-items: center; flex-shrink: 0; }
.btn-view { background: #e8f0f8; color: #003366; border: 1px solid #c5d8f0; border-radius: 5px; padding: 0.25rem 0.75rem; font-size: 0.78rem; font-weight: 600; cursor: pointer; white-space: nowrap; }
.btn-view:hover { background: #d0e4f5; }
.btn-review { background: #003366; color: #fff; border: 1px solid #003366; border-radius: 5px; padding: 0.25rem 0.75rem; font-size: 0.78rem; font-weight: 600; cursor: pointer; white-space: nowrap; }
.btn-review:hover { background: #0055a5; border-color: #0055a5; }
.btn-upload-doc { position: relative; overflow: hidden; background: #003366; color: #fff; border: none; border-radius: 5px; padding: 0.25rem 0.75rem; font-size: 0.78rem; font-weight: 600; cursor: pointer; white-space: nowrap; }
.btn-upload-doc:hover { background: #0055a5; }
.file-hidden-input { position: absolute; inset: 0; opacity: 0; cursor: pointer; width: 100%; height: 100%; }

/* Download Centre */
.download-card { }
.download-hint { font-size: 0.84rem; color: #666; margin: 0 0 1.1rem; line-height: 1.6; }
.dl-list { display: flex; flex-direction: column; gap: 0.4rem; margin-bottom: 1.25rem; }
.dl-section-divider { font-size: 0.72rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.06em; color: #888; padding: 0.5rem 0 0.2rem; border-top: 1px solid #f0f3f7; margin-top: 0.3rem; }
.dl-row { display: flex; align-items: center; gap: 0.75rem; padding: 0.6rem 0.85rem; border: 1px solid #e8edf4; border-radius: 7px; background: #fafbfc; cursor: pointer; transition: background 0.12s; }
.dl-row:hover { background: #f0f6ff; border-color: #c5d8f0; }
.dl-check { width: 15px; height: 15px; cursor: pointer; flex-shrink: 0; accent-color: #003366; }
.dl-type-icon { font-size: 1.1rem; flex-shrink: 0; }
.dl-name { flex: 1; font-size: 0.86rem; color: #333; }
.dl-sub { font-size: 0.78rem; color: #888; font-weight: 400; margin-left: 0.35rem; }
.dl-filename { color: #888; font-size: 0.8rem; font-weight: 400; margin-left: 0.35rem; }
.dl-tag { font-size: 0.7rem; font-weight: 700; padding: 2px 8px; border-radius: 20px; white-space: nowrap; flex-shrink: 0; }
.tag-file      { background: #f0f3f7; color: #555; border: 1px solid #d0d7e0; }
.tag-grade     { background: #dbeafe; color: #1d4ed8; border: 1px solid #93c5fd; }
.tag-grade-plain { background: #f0f3f7; color: #555; border: 1px solid #d0d7e0; }
.tag-cert      { background: #d1fae5; color: #065f46; border: 1px solid #6ee7b7; }
.tag-cert-plain { background: #f0f3f7; color: #555; border: 1px solid #d0d7e0; }
.dl-empty { font-size: 0.86rem; color: #aaa; font-style: italic; padding: 0.75rem 0; }
.dl-actions { display: flex; align-items: center; justify-content: space-between; padding-top: 1rem; border-top: 1px solid #f0f3f7; }
.dl-select-all { display: flex; align-items: center; gap: 0.5rem; font-size: 0.86rem; color: #555; cursor: pointer; user-select: none; }
.dl-select-all input { width: 15px; height: 15px; cursor: pointer; accent-color: #003366; }
.btn-dl-all { background: #003366; color: #fff; border: none; border-radius: 7px; padding: 0.62rem 1.5rem; font-size: 0.9rem; font-weight: 700; cursor: pointer; }
.btn-dl-all:hover:not(:disabled) { background: #0055a5; }
.btn-dl-all:disabled { opacity: 0.38; cursor: default; }

/* Absence / Messages */
.detail-tbl { width: 100%; border-collapse: collapse; font-size: .87rem; }
.detail-tbl th { text-align: left; padding: .55rem .75rem; font-size: .74rem; text-transform: uppercase; letter-spacing: .04em; color: #666; border-bottom: 2px solid #e8edf4; background: #fafbfc; }
.detail-tbl td { padding: .55rem .75rem; border-bottom: 1px solid #f0f3f7; }
.btn-sm-teal { background: #e0f5f0; color: #0d6b55; border: 1px solid #a8ddd0; border-radius: 5px; padding: .28rem .7rem; font-size: .78rem; cursor: pointer; }
.btn-sm-teal:hover { background: #c0ece3; }
.btn-sm-blue { background: #e8f4fd; color: #1a6ca8; border: 1px solid #b8d9f5; border-radius: 5px; padding: .28rem .7rem; font-size: .78rem; cursor: pointer; }
.btn-sm-blue:hover { background: #d0e8f8; }
.msg-compose { background: #f8f9fa; border-radius: 8px; padding: 1rem; }
.compose-label { font-size: .84rem; font-weight: 700; color: #444; margin: 0 0 .65rem; }
.msg-compose .field { display: flex; flex-direction: column; gap: .3rem; }
.msg-compose .field label { font-size: .82rem; font-weight: 600; color: #555; }
.msg-compose .field input, .msg-compose .field textarea { padding: .5rem .7rem; border: 1.5px solid #ccc; border-radius: 6px; font-size: .9rem; font-family: inherit; outline: none; }
.msg-compose .field input:focus, .msg-compose .field textarea:focus { border-color: #003366; }
.saved-msg { font-size: .85rem; color: #2d9e53; font-weight: 600; }
.notice-row { background: #f7f9fb; border: 1px solid #e8edf4; border-radius: 7px; padding: .65rem .9rem; margin-top: .5rem; }
.notice-title-adm { font-size: .88rem; font-weight: 700; color: #003366; }
.notice-body-adm  { font-size: .84rem; color: #444; margin-top: .25rem; }
.notice-date-adm  { font-size: .75rem; color: #aaa; margin-top: .25rem; }

/* Drawer */
.overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.38); z-index: 200; }
.drawer { position: fixed; top: 0; right: 0; bottom: 0; width: 580px; max-width: 96vw; background: #fff; z-index: 201; display: flex; flex-direction: column; box-shadow: -4px 0 24px rgba(0,0,0,0.15); }
.drawer-header { display: flex; align-items: flex-start; justify-content: space-between; padding: 1.2rem 1.5rem; border-bottom: 1.5px solid #e8edf4; flex-shrink: 0; }
.drawer-header h2 { font-size: 1.05rem; font-weight: 700; color: #003366; margin: 0; }
.drawer-sub { font-size: 0.8rem; color: #888; margin-top: 0.15rem; }
.drawer-close { background: none; border: none; font-size: 1.1rem; color: #888; cursor: pointer; }
.drawer-close:hover { color: #333; }
.drawer-form { flex: 1; overflow-y: auto; padding: 1.25rem 1.5rem; display: flex; flex-direction: column; gap: 0.85rem; }
.drawer-actions { display: flex; gap: 0.75rem; justify-content: flex-end; padding: 1rem 1.5rem; border-top: 1px solid #e8edf4; flex-shrink: 0; }
.edit-section-label { font-size: 0.72rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.06em; color: #003366; margin: 0.5rem 0 0; padding-bottom: 0.35rem; border-bottom: 1.5px solid #e8edf4; }
.row-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 0.75rem; }
.field { display: flex; flex-direction: column; gap: 0.28rem; }
.field-narrow { max-width: 180px; }
.field label { font-size: 0.8rem; font-weight: 600; color: #444; }
.field input, .field select, .field textarea { padding: 0.55rem 0.75rem; border: 1.5px solid #ccc; border-radius: 7px; font-size: 0.88rem; font-family: inherit; outline: none; }
.field input:focus, .field select:focus, .field textarea:focus { border-color: #003366; }
.btn-cancel { padding: 0.6rem 1.2rem; background: #f2f5f9; border: 1.5px solid #ccc; border-radius: 7px; font-size: 0.88rem; cursor: pointer; color: #555; }
.btn-save { padding: 0.6rem 1.4rem; background: #003366; color: #fff; border: none; border-radius: 7px; font-size: 0.88rem; font-weight: 600; cursor: pointer; }
.btn-save:hover:not(:disabled) { background: #0055a5; }
.btn-save:disabled { opacity: 0.4; cursor: default; }
.req { color: #e53e3e; }

/* Enrollment edit tabs */
.enr-edit-tabs { display: flex; border-bottom: 1.5px solid #e8edf4; flex-shrink: 0; }
.enr-tab { flex: 1; background: none; border: none; padding: 0.75rem 1rem; font-size: 0.86rem; color: #888; cursor: pointer; border-bottom: 2.5px solid transparent; transition: color 0.15s; margin-bottom: -1.5px; }
.enr-tab:hover { color: #003366; }
.enr-tab.active { color: #003366; font-weight: 700; border-bottom-color: #003366; }

/* Student info strip */
.student-info-strip { display: flex; flex-wrap: wrap; gap: 1rem; background: #f0f4f8; padding: 0.65rem 1.5rem; font-size: 0.83rem; color: #555; flex-shrink: 0; }

/* Grade table */
.grade-table-wrap { flex: 1; overflow: auto; padding: 0 1.5rem; }
.grade-table { width: 100%; border-collapse: collapse; font-size: 0.83rem; min-width: 550px; }
.grade-table thead th { background: #003366; color: #fff; padding: 0.5rem 0.6rem; text-align: center; font-size: 0.75rem; line-height: 1.3; }
.grade-table thead th:first-child { text-align: left; }
.grade-table td { padding: 0.45rem 0.6rem; border-bottom: 1px solid #f0f3f7; }
.num-col { text-align: center; min-width: 52px; }
.calc-cell { background: #f8fafc; color: #555; font-size: 0.8rem; }
.highlight { background: #eaf0f8 !important; font-weight: 700; color: #003366; }
.remark-cell { font-size: 0.75rem; color: #666; max-width: 160px; }
.total-row td { background: #f0f3f7; font-weight: bold; border-top: 2px solid #ccd; }
.gpa-row td { background: #e8f0f8; }
.gpa-label { text-align: right; font-style: italic; padding-right: 0.6rem; font-size: 0.82rem; color: #555; }
.gpa-val { font-size: 0.9rem; }
.grade-input { width: 52px; padding: 0.25rem 0.3rem; border: 1.5px solid #ccc; border-radius: 5px; font-size: 0.82rem; text-align: center; }
.grade-input:focus { border-color: #003366; outline: none; }

/* Notes */
.notes-list { display: flex; flex-direction: column; gap: 0.6rem; }
.note-entry { background: #f7f9fb; border: 1px solid #e8edf4; border-radius: 7px; padding: 0.55rem 0.75rem; }
.note-meta { display: flex; align-items: center; gap: 0.4rem; margin-bottom: 0.3rem; }
.note-arrow { color: #003366; font-weight: 700; }
.note-req { font-size: 0.78rem; font-weight: 700; color: #003366; background: #e8f0f8; padding: 1px 7px; border-radius: 10px; }
.note-date { font-size: 0.74rem; color: #aaa; margin-left: auto; }
.note-text { font-size: 0.84rem; color: #333; }
.note-sel { padding: 0.45rem 0.65rem; border: 1.5px solid #ccc; border-radius: 7px; font-size: 0.88rem; background: #fff; width: 100%; }
.note-ta { border: 1.5px solid #ccc; border-radius: 7px; padding: 0.55rem 0.75rem; font-size: 0.88rem; font-family: inherit; resize: vertical; width: 100%; }
.section-heading { font-size: 0.78rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.06em; color: #003366; border-bottom: 1.5px solid #e8edf4; padding-bottom: 0.35rem; }
.hint-text { font-size: 0.84rem; color: #888; font-style: italic; }
.success-msg { font-size: 0.88rem; color: #0d6b55; font-weight: 600; padding: 0.65rem 1.5rem; }

/* Activity tab */
.activity-card { background: #f7f9fb; border: 1px solid #e8edf4; border-radius: 8px; padding: 0.85rem 1rem; }
.activity-card.activity-done { border-color: #a8ddd0; background: #f0fdf9; }
.activity-desc { font-size: 0.86rem; color: #555; margin: 0; }
.btn-confirm-adm { background: #0d6b55; color: #fff; border: none; border-radius: 7px; padding: 0.5rem 1.1rem; font-size: 0.86rem; font-weight: 600; cursor: pointer; margin-top: 0.65rem; }
.btn-confirm-adm:hover { background: #0a5a47; }
.toggle-row { display: flex; align-items: center; justify-content: space-between; gap: 1rem; }
.toggle-label { font-size: 0.88rem; font-weight: 600; color: #222; }
.toggle-sub { font-size: 0.78rem; color: #888; margin-top: 2px; }

/* Add Enrolment modal */
.modal-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.45); z-index: 300; display: flex; align-items: center; justify-content: center; }
.add-enrol-modal { background: #fff; border-radius: 12px; width: 520px; max-width: 95vw; box-shadow: 0 8px 40px rgba(0,0,0,0.22); }

/* Document review modal */
.review-modal { background: #fff; border-radius: 12px; width: 820px; max-width: 95vw; max-height: 92vh; display: flex; flex-direction: column; box-shadow: 0 8px 40px rgba(0,0,0,0.22); }
.review-sub { font-size: 0.8rem; color: #888; margin: 0.15rem 0 0; font-family: ui-monospace, monospace; }
.review-actions { display: flex; gap: 0.75rem; justify-content: flex-end; padding: 0.85rem 1.5rem; border-bottom: 1.5px solid #e8edf4; }
.btn-approve { background: #0d6b55; color: #fff; border: none; border-radius: 7px; padding: 0.55rem 1.35rem; font-size: 0.88rem; font-weight: 600; cursor: pointer; }
.btn-approve:hover { background: #0a5a47; }
.btn-reject { background: #fff; color: #b42318; border: 1.5px solid #fda29b; border-radius: 7px; padding: 0.55rem 1.35rem; font-size: 0.88rem; font-weight: 600; cursor: pointer; }
.btn-reject:hover { background: #fee4e2; }
.review-preview { flex: 1; min-height: 420px; background: #f2f5f9; padding: 1rem; display: flex; align-items: center; justify-content: center; overflow: auto; }
.preview-img { max-width: 100%; max-height: 70vh; border-radius: 7px; box-shadow: 0 2px 10px rgba(0,0,0,0.12); background: #fff; }
.preview-pdf { width: 100%; height: 70vh; border: none; border-radius: 7px; background: #fff; }
.preview-unsupported { text-align: center; color: #666; font-size: 0.9rem; padding: 2rem; }
.preview-unsupported-icon { font-size: 2rem; color: #b88a00; margin-bottom: 0.5rem; }
.preview-unsupported-sub { font-size: 0.82rem; color: #888; margin-top: 0.35rem; }
.preview-unsupported-file { margin-top: 0.85rem; font-family: ui-monospace, monospace; font-size: 0.8rem; color: #003366; background: #fff; padding: 0.35rem 0.75rem; border-radius: 5px; border: 1px solid #e8edf4; display: inline-block; }
.modal-hdr { display: flex; align-items: center; justify-content: space-between; padding: 1.1rem 1.5rem; border-bottom: 1.5px solid #e8edf4; }
.modal-hdr h3 { font-size: 1rem; font-weight: 700; color: #003366; margin: 0; }
.btn-modal-close { background: none; border: none; font-size: 1.1rem; color: #888; cursor: pointer; }
.btn-modal-close:hover { color: #333; }

/* Transitions */
.fade-enter-active, .fade-leave-active { transition: opacity 0.2s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
.slide-enter-active, .slide-leave-active { transition: transform 0.25s ease; }
.slide-enter-from, .slide-leave-to { transform: translateX(100%); }

/* Toast */
.toast { position: fixed; bottom: 2rem; left: 50%; transform: translateX(-50%); background: #003366; color: #fff; padding: 0.7rem 1.5rem; border-radius: 8px; font-size: 0.88rem; box-shadow: 0 4px 18px rgba(0,0,0,0.18); z-index: 999; white-space: nowrap; max-width: 90vw; overflow: hidden; text-overflow: ellipsis; }
.toast-pop-enter-active, .toast-pop-leave-active { transition: opacity 0.25s, transform 0.25s; }
.toast-pop-enter-from, .toast-pop-leave-to { opacity: 0; transform: translateX(-50%) translateY(12px); }
</style>
