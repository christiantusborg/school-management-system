<template>
  <div class="page-wrapper">
    <!-- Navbar -->
    <nav class="navbar">
      <span class="brand-text">IBSS Partner Portal &nbsp;—&nbsp; {{ auth.user?.name }}</span>
      <div class="nav-right">
        <router-link class="btn-logout btn-nav-link" to="/partner/change-password">Change password</router-link>
        <button class="btn-logout" @click="logout">Log out</button>
      </div>
    </nav>

    <!-- Public signup link — shareable with prospective students -->
    <div v-if="auth.user?.partnerSlug" class="signup-link-bar">
      <span class="signup-link-label">Student signup link:</span>
      <a class="signup-link-url" :href="signupLink" target="_blank" rel="noopener">{{ signupLink }}</a>
      <button class="btn-copy-link" @click="copySignupLink">{{ signupLinkCopied ? '✓ Copied' : 'Copy' }}</button>
    </div>

    <!-- Main tab bar -->
    <div class="main-tab-bar">
      <button :class="['main-tab-btn', { active: mainTab === 'students' }]" @click="mainTab = 'students'">My Students</button>
      <button :class="['main-tab-btn', { active: mainTab === 'core' }]" @click="mainTab = 'core'">My Core Programmes</button>
      <button :class="['main-tab-btn', { active: mainTab === 'programs' }]" @click="mainTab = 'programs'">
        My Programs
        <span v-if="pendingProgCount" class="tab-badge">{{ pendingProgCount }}</span>
      </button>
      <button :class="['main-tab-btn', { active: mainTab === 'users' }]" @click="mainTab = 'users'">My Users</button>
    </div>

    <!-- ══ MY CORE PROGRAMMES TAB ══════════════════════════════════════════════ -->
    <div v-show="mainTab === 'core'" class="container">
      <div class="page-header">
        <div>
          <h1 class="page-title">My Core Programmes</h1>
          <p class="page-sub">Programmes granted to you by IBSS admin. Disable any specialization you don't currently offer — it will be hidden from new enrolments.</p>
        </div>
      </div>

      <div v-if="coreAccessLoading" class="loading-row">Loading…</div>
      <div v-else-if="coreAccessError" class="err-banner">{{ coreAccessError }}</div>
      <div v-else-if="coreAccessItems.length === 0" class="empty-state-card">No programme access has been granted yet. Contact your IBSS admin.</div>

      <div v-else class="core-groups">
        <div v-for="group in coreAccessGrouped" :key="group.programmeId" class="core-group">
          <div class="core-group-head">
            <strong>{{ group.programmeName }}</strong>
            <span class="core-count">{{ group.specializations.length }} specialization{{ group.specializations.length === 1 ? '' : 's' }}</span>
          </div>
          <div class="core-specialization-list">
            <div v-for="m in group.specializations" :key="m.specializationId" class="core-specialization-row" :class="{ dim: m.disabledByPartner }">
              <div class="core-specialization-name">{{ m.specializationName }}</div>
              <label class="toggle">
                <input type="checkbox" :checked="!m.disabledByPartner" :disabled="coreBusy.has(m.specializationId)" @change="toggleSpecialization(m, !$event.target.checked)" />
                <span>{{ m.disabledByPartner ? 'Disabled' : 'Active' }}</span>
              </label>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ══ MY STUDENTS TAB ══════════════════════════════════════════════════════ -->
    <div v-show="mainTab === 'students'" class="container">
      <div class="page-header" style="display:flex; align-items:center; justify-content:space-between;">
        <h1 class="page-title">My Students</h1>
        <button class="btn-add-student" :disabled="!auth.user?.partnerSlug" @click="showAddStudent = true"
                :title="auth.user?.partnerSlug ? '' : 'Partner slug not loaded'">
          + Add Student
        </button>
      </div>
      <PartnerStudentsTab v-if="mainTab === 'students'" :key="studentsRefreshKey" />
    </div>

    <!-- Add Student modal — hosts the public signup wizard scoped to this partner -->
    <Teleport to="body">
      <div v-if="showAddStudent" class="add-student-backdrop" @click="closeAddStudent"></div>
      <div v-if="showAddStudent" class="add-student-modal" @click.stop>
        <div class="add-student-head">
          <h3>Add Student — {{ auth.user?.displayName }}</h3>
          <button class="btn-close-modal" @click="closeAddStudent">✕</button>
        </div>
        <iframe
          v-if="auth.user?.partnerSlug"
          :src="`/#/apply?partner=${auth.user.partnerSlug}`"
          class="add-student-iframe"
          title="Student signup">
        </iframe>
      </div>
    </Teleport>

    <!-- ══ MY STUDENTS LEGACY (mock-based, hidden) ════════════════════════════ -->
    <div v-if="false" class="container">
      <div class="page-header">
        <div>
          <h1 class="page-title">My Students</h1>
          <p class="page-sub">{{ myStudents.length }} student{{ myStudents.length !== 1 ? 's' : '' }} enrolled under {{ auth.user?.name }}</p>
        </div>
        <button class="btn-primary" @click="openAddStudent">+ Add Student</button>
      </div>

      <!-- Filter bar -->
      <div class="filter-bar">
        <div class="search-wrap">
          <span class="search-icon">&#128269;</span>
          <input
            v-model="searchQuery"
            class="search-input"
            placeholder="Search by name or student ID…"
            spellcheck="false"
            @keydown.escape="searchQuery = ''"
          />
          <button v-if="searchQuery" class="search-clear" @click="searchQuery = ''">✕</button>
        </div>
        <div class="search-hint-wrap">
          <span class="search-hint-icon">?</span>
          <div class="search-hint-tip">
            <strong>Fuzzy search</strong> — characters don't need to be consecutive.<br>
            <code>jnd</code> matches <em>Jane Doe</em><br>
            <code>ibsmba</code> matches <em>IBSS.MBA.23110102</em><br>
            Works on both full name and student ID.
          </div>
        </div>

        <!-- Programme multi-select -->
        <div class="ms-wrap" v-click-outside="() => showProgDrop = false">
          <button class="ms-btn" :class="{ 'ms-btn-active': filterProgs.size }" @click="showProgDrop = !showProgDrop">
            {{ filterProgs.size ? `${filterProgs.size} Programme${filterProgs.size > 1 ? 's' : ''}` : 'All Programmes' }}
            <span class="ms-caret">&#8964;</span>
          </button>
          <div v-if="showProgDrop" class="ms-dropdown">
            <label v-for="p in availableProgs" :key="p" class="ms-item">
              <input type="checkbox" :checked="filterProgs.has(p)" @change="toggleFilter(filterProgs, p)" />
              <span>{{ p }}</span>
            </label>
            <button v-if="filterProgs.size" class="ms-clear" @click="filterProgs.clear()">Clear</button>
          </div>
        </div>

        <!-- Specialization multi-select -->
        <div class="ms-wrap" v-click-outside="() => showMajDrop = false">
          <button class="ms-btn" :class="{ 'ms-btn-active': filterMajs.size }" @click="showMajDrop = !showMajDrop">
            {{ filterMajs.size ? `${filterMajs.size} Specialization${filterMajs.size > 1 ? 's' : ''}` : 'All Specializations' }}
            <span class="ms-caret">&#8964;</span>
          </button>
          <div v-if="showMajDrop" class="ms-dropdown">
            <label v-for="m in availableMajs" :key="m" class="ms-item">
              <input type="checkbox" :checked="filterMajs.has(m)" @change="toggleFilter(filterMajs, m)" />
              <span>{{ m }}</span>
            </label>
            <button v-if="filterMajs.size" class="ms-clear" @click="filterMajs.clear()">Clear</button>
          </div>
        </div>

        <button v-if="filterProgs.size || filterMajs.size" class="btn-clear-filter" @click="filterProgs.clear(); filterMajs.clear()">✕ Filters</button>
        <span class="filter-count">{{ filteredMyStudents.length }} of {{ myStudents.length }} students</span>
      </div>

      <!-- Empty state -->
      <div v-if="myStudents.length === 0" class="empty-state-card">
        No students yet. Click "+ Add Student" to get started.
      </div>
      <div v-else-if="filteredMyStudents.length === 0" class="empty-state-card">
        No students match the selected filters.
      </div>

      <!-- Student Cards -->
      <div v-for="s in filteredMyStudents" :key="s.id" class="student-card">
        <!-- Card Header: Student ID + Name + collapse toggle -->
        <div class="sc-header" @click="toggleCard(s.id)" style="cursor:pointer">
          <div class="sc-id-name">
            <span class="sc-sid">{{ s.studentId }}</span>
            <span class="sc-sep">·</span>
            <span class="sc-name">{{ s.firstName }} {{ s.lastName }}</span>
          </div>
          <div class="sc-header-right">
            <span v-if="hasPendingAbsence(s.studentId)" class="abs-badge">Absence</span>
            <button v-if="!s.partnerReview?.completedAt" class="btn-review-app" @click.stop="openReview(s)">Review Application</button>
            <span v-else class="reviewed-badge" :title="'Reviewed ' + s.partnerReview.completedAt">✓ Reviewed</span>
            <span v-if="collapsedCards.has(s.id)" class="sc-summary">
              {{ s.enrollments.length }} programme{{ s.enrollments.length !== 1 ? 's' : '' }}
              &nbsp;·&nbsp;
              {{ s.enrollments.reduce((t, e) => t + e.coursesCompleted, 0) }}/{{ s.enrollments.reduce((t, e) => t + e.coursesRequired, 0) }} courses
            </span>
            <span class="sc-chevron" :class="{ collapsed: collapsedCards.has(s.id) }">&#8964;</span>
          </div>
        </div>

        <!-- Enrollment rows table (collapsible) -->
        <div v-if="!collapsedCards.has(s.id)" class="sc-body">
          <table class="enr-table">
            <thead>
              <tr>
                <th class="th-prog">Programme &amp; Specialization</th>
                <th class="th-status">Status</th>
                <th class="th-acad">Academic Progress</th>
                <th class="th-pay">Payment</th>
                <th class="th-notes">Notes on Changes</th>
                <th class="th-edit"></th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="enr in s.enrollments" :key="enr.id" class="enr-row">
                <!-- Col 1: Programme & Specialization + Document list -->
                <td class="td-prog">
                  <div class="prog-name-main">{{ enr.programme }}</div>
                  <div class="prog-specialization-sub">{{ enr.specialization }}</div>
                  <div class="doc-list">
                    <a class="doc-row" :class="enr.offerType ? 'doc-avail' : 'doc-disabled'" href="#" @click.prevent="enr.offerType && openLetter(s, enr, 'offer')">
                      <span class="doc-icon">&#128196;</span> Offer Letter
                      <span v-if="!enr.offerType" class="doc-na">Not issued</span>
                    </a>
                    <a class="doc-row" :class="enr.paymentDone ? 'doc-avail' : 'doc-disabled'" href="#" @click.prevent="enr.paymentDone && openLetter(s, enr, 'admission')">
                      <span class="doc-icon">&#128196;</span> Admission Letter
                      <span v-if="!enr.paymentDone" class="doc-na">Not issued</span>
                    </a>
                    <a class="doc-row" :class="enr.certReleased ? 'doc-avail' : 'doc-disabled'" href="#" @click.prevent="enr.certReleased && openCert(s, enr)">
                      <span class="doc-icon">&#127891;</span> Certificate
                      <span v-if="!enr.certReleased" class="doc-na">Unreleased</span>
                    </a>
                    <a class="doc-row" :class="enr.transcriptReleased ? 'doc-avail' : 'doc-disabled'" href="#" @click.prevent="enr.transcriptReleased && openCert(s, enr)">
                      <span class="doc-icon">&#128203;</span> Transcript
                      <span v-if="!enr.transcriptReleased" class="doc-na">Unreleased</span>
                    </a>
                  </div>
                </td>
                <!-- Col 2: Status (admin-set, view-only) -->
                <td class="td-status">
                  <span :class="['enr-status-badge', enrollStatusClass(enr.enrollmentStatus)]">
                    {{ enr.enrollmentStatus }}
                  </span>
                  <div class="status-note">Set by IBSS Admin</div>
                </td>
                <!-- Col 3: Academic Progress -->
                <td class="td-acad">
                  <div class="ap-row"><span class="ap-lbl">Courses:</span> <strong>{{ enr.coursesCompleted }}/{{ enr.coursesRequired }}</strong></div>
                  <div class="ap-row"><span class="ap-lbl">Final Project:</span> <span :class="fpClass(enr.finalProjectStatus)">{{ enr.finalProjectStatus }}</span></div>
                  <div class="ap-row"><span class="ap-lbl">Marks:</span>
                    <span v-if="isGraded(s.studentId)" class="rel-yes">Graded</span>
                    <span v-else class="rel-no">Not yet graded</span>
                  </div>
                  <div v-if="enr.admissionConfirmed" class="ap-row"><span class="confirmed-chip">Admitted ✓</span></div>
                </td>
                <!-- Col 4: Payment Status (admin-managed, view-only) -->
                <td class="td-pay">
                  <div class="pay-section">
                    <div class="ap-lbl">Tuition Fee</div>
                    <span :class="['pay-chip', payChipClass(enr.tuitionFeeStatus)]">{{ enr.tuitionFeeStatus }}</span>
                  </div>
                  <div class="pay-section" style="margin-top:.6rem">
                    <div class="ap-lbl">Other Fees</div>
                    <span :class="['pay-chip', payChipClass(enr.otherFeesStatus)]">{{ enr.otherFeesStatus }}</span>
                  </div>
                  <div class="status-note">Managed by IBSS Admin</div>
                </td>
                <!-- Col 5: Notes on Changes (read-only list) -->
                <td class="td-notes">
                  <div v-if="enr.changeNotes.length" class="notes-list">
                    <div v-for="note in enr.changeNotes" :key="note.id" class="note-entry">
                      <div class="note-meta">
                        <span class="note-arrow">→</span>
                        <span class="note-req">{{ note.requestedChange || 'General' }}</span>
                        <span class="note-date">{{ note.date }}</span>
                      </div>
                      <div class="note-text">{{ note.text }}</div>
                    </div>
                  </div>
                  <div v-else class="notes-empty">No notes yet</div>
                </td>
                <!-- Col 6: Enrollment edit button -->
                <td class="td-edit">
                  <button class="btn-edit-enr" @click="openEnrEdit(s, enr)">&#9998; Edit</button>
                </td>
              </tr>
            </tbody>
          </table>
          <div class="sc-footer">
            <button class="btn-add-enr" @click="openAddEnrollment(s)">+ Add Programme Enrolment</button>
          </div>
        </div>
      </div>

      <!-- Absence Reports for my students -->
      <div v-if="myAbsences.length" class="partner-section">
        <h2 class="partner-section-title">Absence Reports</h2>
        <div class="partner-card">
          <table class="partner-tbl">
            <thead><tr><th>Student</th><th>Date</th><th>Type</th><th>Reason</th><th>Status</th></tr></thead>
            <tbody>
              <tr v-for="a in myAbsences" :key="a.id" class="data-row">
                <td><strong>{{ studentName(a.studentId) }}</strong></td>
                <td>{{ a.date }}</td>
                <td>{{ a.type }}</td>
                <td style="font-size:.82rem;color:#555">{{ a.reason || '—' }}</td>
                <td>
                  <span :class="a.status === 'acknowledged' ? 'badge-confirmed' : 'badge-new'">
                    {{ a.status === 'acknowledged' ? 'Acknowledged' : 'Pending' }}
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Support Tickets for my students -->
      <div v-if="myTickets.length" class="partner-section">
        <h2 class="partner-section-title">Support Tickets</h2>
        <div v-for="t in myTickets" :key="t.id" class="partner-card ticket-row">
          <div class="ticket-meta-p" @click="togglePartnerTicket(t.id)">
            <span class="ticket-subj-p">{{ t.subject }}</span>
            <span style="font-size:.82rem;color:#666">{{ studentName(t.studentId) }}</span>
            <span :class="t.status === 'resolved' ? 'badge-confirmed' : 'badge-offer'">{{ t.status === 'resolved' ? 'Resolved' : 'Open' }}</span>
            <span style="font-size:.75rem;color:#999">{{ expandedPartnerTickets.includes(t.id) ? '▲' : '▼' }}</span>
          </div>
          <div v-if="expandedPartnerTickets.includes(t.id)" class="ticket-thread-p">
            <div v-for="(r, i) in t.replies" :key="i" :class="['reply-p', 'reply-p-' + r.from]">
              <span class="reply-from-p">{{ r.from === 'student' ? studentName(t.studentId) : r.from === 'admin' ? 'Admin' : 'Partner' }}</span>
              <p>{{ r.text }}</p>
            </div>
            <div v-if="t.status !== 'resolved'" class="partner-reply-box">
              <textarea v-model="partnerReplyTexts[t.id]" rows="2" placeholder="Write a reply…"></textarea>
              <button class="btn-primary" style="font-size:.82rem;padding:.38rem .9rem;align-self:flex-end" :disabled="!partnerReplyTexts[t.id]" @click="partnerReply(t)">Reply</button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ══ MY PROGRAMS TAB ═════════════════════════════════════════════════════ -->
    <div v-show="mainTab === 'programs'" class="container">
      <div class="page-header">
        <div>
          <h1 class="page-title">My Programs</h1>
          <p class="page-sub">Clone IBSS core programmes, customise them, and submit for approval. Students can only be enrolled in approved programmes.</p>
        </div>
        <div style="display:flex;gap:.6rem">
          <button class="btn-primary" @click="showCloneModal = true">+ Clone a Core Programme</button>
          <button class="btn-primary" @click="showFromScratchModal = true">+ Create from Scratch</button>
        </div>
      </div>

      <div v-if="myProgError" class="err-banner">{{ myProgError }}</div>
      <div v-if="myProgLoading" class="loading-row">Loading…</div>

      <!-- Empty state -->
      <div v-if="!myProgLoading && myProgClones.length === 0" class="prog-empty-state">
        No programmes yet. Clone a core programme or create one from scratch to get started.
      </div>

      <!-- Programme cards -->
      <div v-for="clone in myProgClones" :key="clone.id" class="prog-card" :class="{ 'prog-card-disabled': clone.isDisabledByAdmin }">
        <!-- Card header — always visible -->
        <div class="prog-card-header">
          <div class="prog-card-info">
            <strong class="prog-card-name">{{ clone.name }}</strong>
            <span class="badge-code-p">{{ clone.code }}</span>
            <span :class="progStatusClass(clone.status)">{{ progStatusLabel(clone.status) }}</span>
            <span v-if="clone.isDisabledByAdmin" class="prog-admin-disabled-pill">Disabled by Admin</span>
            <label v-if="!clone.isDisabledByAdmin && clone.status === 'approved'" class="toggle" style="margin-left:.5rem">
              <input type="checkbox" :checked="clone.isActive" :disabled="progBusy.has(clone.id)" @change="toggleProgActive(clone, $event.target.checked)" />
              <span>{{ clone.isActive ? 'Active' : 'Inactive' }}</span>
            </label>
          </div>
          <div class="prog-card-actions">
            <template v-if="clone.isDisabledByAdmin">
              <span class="prog-approved-note">Locked by IBSS Admin</span>
              <button class="btn-act-p btn-view-p" @click="toggleProgEdit(clone.id)">
                {{ expandedProg === clone.id ? '▲ Collapse' : '▾ View Details' }}
              </button>
            </template>
            <template v-else-if="clone.hasEnrolments">
              <span class="prog-approved-note">Locked — students enrolled</span>
              <button class="btn-act-p btn-view-p" @click="toggleProgEdit(clone.id)">
                {{ expandedProg === clone.id ? '▲ Collapse' : '▾ View Details' }}
              </button>
            </template>
            <template v-else-if="clone.status === 'draft' || clone.status === 'rejected' || clone.status === 'approved'">
              <button class="btn-act-p btn-edit-p" @click="toggleProgEdit(clone.id)">
                {{ expandedProg === clone.id ? '▲ Collapse' : '✎ Edit Programme' }}
              </button>
              <button v-if="clone.status === 'draft' || clone.status === 'rejected'" class="btn-act-p btn-submit-p" @click="submitProgForApproval(clone)">
                Submit for Approval
              </button>
              <button v-if="clone.canDelete" class="btn-act-p btn-view-p" @click="deleteProgram(clone)">
                ✕ Delete
              </button>
            </template>
            <template v-else-if="clone.status === 'pending'">
              <button class="btn-act-p btn-view-p" @click="toggleProgEdit(clone.id)">
                {{ expandedProg === clone.id ? '▲ Collapse' : '▾ View Details' }}
              </button>
            </template>
          </div>
        </div>

        <div v-if="clone.isDisabledByAdmin" class="prog-admin-disabled-banner">
          &#128274; This programme has been disabled by IBSS Admin. It is read-only and not available to students. Contact IBSS if you believe this is in error.
        </div>

        <!-- Rejection reason -->
        <div v-if="!clone.isDisabledByAdmin && clone.status === 'rejected' && clone.rejectionReason" class="prog-rejection-note">
          Rejected: {{ clone.rejectionReason }}
        </div>

        <!-- Expanded panel -->
        <div v-if="expandedProg === clone.id" class="prog-edit-panel">

          <!-- ── EDITABLE (draft / rejected / approved-no-enrolments) ── -->
          <template v-if="isProgEditable(clone)">
            <div v-if="clone.status === 'approved'" class="prog-readonly-notice" style="background:#fff7e0;color:#8a6d00">
              &#9888; Editing will flip this approved programme back to Pending IBSS review.
            </div>
            <div class="prog-edit-row">
              <label class="prog-edit-label">Programme Name</label>
              <input v-model="clone.name" class="prog-edit-input" placeholder="Programme name…" />
            </div>
            <div class="prog-edit-row">
              <label class="prog-edit-label">Programme Code</label>
              <input v-model="clone.code" class="prog-edit-input" placeholder="Unique code…" />
            </div>

            <div class="prog-edit-row pathway-row-toggle" @click="togglePathwayPanel(clone.id)">
              <label class="prog-edit-label" style="cursor:pointer">
                <span class="arrow-sm">{{ pathwayOpen.has(clone.id) ? '▾' : '▸' }}</span>
                Pathways
              </label>
              <span class="badge-count-p">{{ (clone.pathwayIds ?? []).length }} selected</span>
            </div>
            <div v-if="pathwayOpen.has(clone.id)" class="prog-edit-row">
              <label class="prog-edit-label"></label>
              <div class="pathway-grid-p">
                <label v-for="p in pathwaysCatalog" :key="p.pathwayId" class="pathway-row-p">
                  <input type="checkbox"
                         :checked="(clone.pathwayIds ?? []).includes(p.pathwayId)"
                         @change="togglePathwayOnClone(clone, p.pathwayId)" />
                  {{ p.name }}
                </label>
                <p v-if="!pathwaysCatalog.length" class="ro-empty">No pathways defined.</p>
              </div>
            </div>

            <div class="prog-specializations-section">
              <div class="prog-specializations-title">Specializations</div>
              <div v-for="maj in clone.specializations" :key="maj.id" class="prog-maj-block">
                <div class="prog-maj-header" @click="toggleMajEdit(clone.id + maj.id)">
                  <span class="arrow-sm">{{ expandedMaj === clone.id + maj.id ? '▾' : '▸' }}</span>
                  <input v-model="maj.name" class="prog-maj-name-input" placeholder="Specialization name…" @click.stop />
                  <span class="badge-count-p">{{ maj.subjects.length }} subjects</span>
                  <button class="btn-del-maj" @click.stop="removeMajFromClone(clone, maj.id)"
                    title="Remove specialization">✕</button>
                </div>
                <div v-if="expandedMaj === clone.id + maj.id" class="prog-subj-block">
                  <!-- Column headers -->
                  <div class="subj-col-header">
                    <span class="col-code">Module Code</span>
                    <span class="col-name">Module Name</span>
                    <span class="col-ects">ECTS</span>
                    <span class="col-del"></span>
                  </div>
                  <div v-for="s in maj.subjects" :key="s.id" class="prog-subj-row">
                    <input v-model="s.code" class="inp-s-code" placeholder="e.g. MBA501" />
                    <input v-model="s.name" class="inp-s-name" placeholder="Module name" />
                    <input v-model.number="s.ects" class="inp-s-cr" type="number" min="1" placeholder="15" />
                    <button class="btn-x-s" @click="removeSubjFromMaj(maj, s.id)">✕</button>
                  </div>
                  <div class="prog-add-subj-row">
                    <input v-model="newSubjForms[clone.id + maj.id + '_code']" class="inp-s-code" placeholder="Code" />
                    <input v-model="newSubjForms[clone.id + maj.id + '_n']" class="inp-s-name" placeholder="Module name" />
                    <input v-model.number="newSubjForms[clone.id + maj.id + '_c']" class="inp-s-cr" type="number" min="1" placeholder="15" />
                    <button class="btn-add-s" @click="addSubjToMaj(clone, maj)">+ Add</button>
                  </div>
                </div>
              </div>
              <div class="prog-add-maj-row">
                <input v-model="newMajNameForms[clone.id]" class="prog-edit-input" placeholder="New specialization name…" />
                <button class="btn-add-maj" @click="addMajToClone(clone)">+ Add Specialization</button>
              </div>
            </div>

            <div class="drawer-actions" style="margin-top:.8rem">
              <button class="btn-save" :disabled="progBusy.has(clone.id)" @click="saveProgEdit(clone)">Save Changes</button>
            </div>
          </template>

          <!-- ── READ-ONLY (disabled, pending, or approved-with-enrolments) ── -->
          <template v-else>
            <div class="prog-readonly-notice">
              <span v-if="clone.isDisabledByAdmin">&#128274; Disabled by IBSS Admin — this programme is fully locked.</span>
              <span v-else-if="clone.hasEnrolments">&#128274; Students have enrolled in this programme — it is locked.</span>
              <span v-else-if="clone.status === 'approved'">&#128274; This programme has been approved by IBSS.</span>
              <span v-else>&#9203; Awaiting IBSS review — editing is locked while under review.</span>
            </div>
            <div v-if="(clone.pathwayIds ?? []).length" class="prog-edit-row">
              <label class="prog-edit-label">Pathways</label>
              <div class="ro-pathway-list">
                <span v-for="pid in clone.pathwayIds" :key="pid" class="ro-pathway-pill">
                  {{ pathwaysCatalog.find(p => p.pathwayId === pid)?.name ?? `#${pid}` }}
                </span>
              </div>
            </div>
            <div class="prog-specializations-section">
              <div class="prog-specializations-title">Specializations &amp; Subjects</div>
              <div v-for="maj in clone.specializations" :key="maj.id" class="prog-maj-block">
                <div class="prog-maj-header prog-maj-header-ro" @click="toggleMajEdit(clone.id + maj.id)">
                  <span class="arrow-sm">{{ expandedMaj === clone.id + maj.id ? '▾' : '▸' }}</span>
                  <span class="prog-maj-name-ro">{{ maj.name }}</span>
                  <span class="badge-count-p">{{ maj.subjects.length }} subjects</span>
                </div>
                <div v-if="expandedMaj === clone.id + maj.id" class="prog-subj-block">
                  <div class="subj-col-header">
                    <span class="col-code">Module Code</span>
                    <span class="col-name">Module Name</span>
                    <span class="col-ects">ECTS</span>
                  </div>
                  <div v-for="s in maj.subjects" :key="s.id" class="prog-subj-row-ro">
                    <span class="ro-subj-code">{{ s.code || '—' }}</span>
                    <span class="ro-subj-name">{{ s.name }}</span>
                    <span class="ro-subj-cr">{{ s.ects }}</span>
                  </div>
                  <p v-if="!maj.subjects.length" class="ro-empty">No subjects.</p>
                </div>
              </div>
            </div>
          </template>

        </div>

        <!-- Collapsed pill summary -->
        <div v-else class="prog-maj-summary">
          <span v-for="maj in clone.specializations" :key="maj.id" class="prog-maj-pill">
            {{ maj.name }} <span class="prog-maj-pill-count">{{ maj.subjects.length }}s</span>
          </span>
        </div>
      </div>
    </div>

    <!-- ══ MY USERS TAB ════════════════════════════════════════════════════════ -->
    <div v-show="mainTab === 'users'" class="container">
      <PartnerUsersTab v-if="mainTab === 'users'" :key="usersRefreshKey" />
    </div>

    <!-- Clone core programme modal -->
    <transition name="fade">
      <div v-if="showCloneModal" class="modal-overlay" @click.self="showCloneModal = false">
        <div class="clone-modal">
          <div class="clone-modal-header">
            <h3>Clone a Core Programme</h3>
            <button class="btn-modal-close" @click="showCloneModal = false">✕</button>
          </div>
          <p class="clone-modal-sub">Creates a copy of the programme under your account. You can then customise it before submitting for IBSS approval.</p>
          <div v-if="cloneSources.length === 0" class="prog-empty-state">
            You have no core programmes granted yet. Contact your IBSS admin.
          </div>
          <div class="clone-modal-list">
            <div v-for="prog in cloneSources" :key="prog.id" class="clone-modal-item">
              <div class="clone-modal-prog-info">
                <span class="clone-modal-prog-name">{{ prog.name }}</span>
                <span class="badge-count-p">{{ prog.specializationCount }} specialization{{ prog.specializationCount !== 1 ? 's' : '' }}</span>
              </div>
              <button class="btn-clone-prog" :disabled="progBusy.has(prog.id)" @click="doCloneProgram(prog.id)">Clone</button>
            </div>
          </div>
        </div>
      </div>
    </transition>

    <!-- Create-from-scratch modal -->
    <transition name="fade">
      <div v-if="showFromScratchModal" class="modal-overlay" @click.self="showFromScratchModal = false">
        <div class="clone-modal">
          <div class="clone-modal-header">
            <h3>Create Programme from Scratch</h3>
            <button class="btn-modal-close" @click="showFromScratchModal = false">✕</button>
          </div>
          <p class="clone-modal-sub">Creates a new blank programme under your account. Add specializations and subjects, then submit it for IBSS approval.</p>
          <div class="field" style="margin:.6rem 0">
            <label>Programme Name</label>
            <input v-model="fromScratchName" class="prog-edit-input" placeholder="e.g. Executive MBA" />
          </div>
          <div class="drawer-actions">
            <button class="btn-cancel" @click="showFromScratchModal = false">Cancel</button>
            <button class="btn-save" :disabled="!fromScratchName.trim() || fromScratchBusy" @click="doCreateFromScratch">Create</button>
          </div>
        </div>
      </div>
    </transition>

    <!-- ══ Student Edit Panel ════════════════════════════════════════════════════ -->
    <transition name="fade"><div v-if="showStudentEdit" class="drawer-overlay" @click.self="showStudentEdit = false" /></transition>
    <transition name="slide">
      <div v-if="showStudentEdit && editingStudent" class="drawer drawer-narrow">
        <div class="drawer-header">
          <div>
            <h2>Edit Student</h2>
            <p class="drawer-sub">{{ editingStudent.studentId }} — {{ editingStudent.firstName }} {{ editingStudent.lastName }}</p>
          </div>
          <button class="drawer-close" @click="showStudentEdit = false">✕</button>
        </div>
        <div class="drawer-form">
          <div class="field"><label>Student ID</label><input :value="editingStudent.studentId" disabled class="input-locked" /></div>
          <div class="row-2">
            <div class="field"><label>First Name</label><input :value="editingStudent.firstName" disabled class="input-locked" /></div>
            <div class="field"><label>Last Name</label><input :value="editingStudent.lastName" disabled class="input-locked" /></div>
          </div>
          <div class="field"><label>Date of Birth</label><input :value="editingStudent.dateOfBirth" disabled class="input-locked" /></div>
          <div class="field"><label>Passport / ID No.</label><input :value="editingStudent.passportId" disabled class="input-locked" /></div>
          <div class="field"><label>Email</label><input v-model="studentEditForm.email" type="email" /></div>
          <div class="field"><label>Address</label><input v-model="studentEditForm.address" /></div>
          <div class="field"><label>Highest Degree</label><input v-model="studentEditForm.highestDegree" placeholder="e.g. Bachelor of Science" /></div>
          <div class="row-2">
            <div class="field"><label>Language Result</label><input v-model="studentEditForm.languageResult" placeholder="e.g. IELTS 6.5" /></div>
            <div class="field"><label>Years Work Exp.</label><input v-model.number="studentEditForm.yearsWorkExperience" type="number" min="0" /></div>
          </div>
          <div v-if="studentEditSaved" class="success-msg">Saved successfully!</div>
        </div>
        <div class="drawer-actions">
          <button class="btn-cancel" @click="showStudentEdit = false">Cancel</button>
          <button class="btn-save" @click="saveStudentEdit">Save Changes</button>
        </div>
      </div>
    </transition>

    <!-- ══ Enrollment Edit Panel ══════════════════════════════════════════════════ -->
    <transition name="fade"><div v-if="showEnrEdit" class="drawer-overlay" @click.self="showEnrEdit = false" /></transition>
    <transition name="slide">
      <div v-if="showEnrEdit && editingEnrollment" class="drawer">
        <div class="drawer-header">
          <div>
            <h2>Edit Enrolment</h2>
            <p class="drawer-sub">{{ editingEnrStudent?.studentId }} &nbsp;·&nbsp; {{ editingEnrollment.programme }}</p>
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
            <span><strong>Student:</strong> {{ editingEnrStudent?.firstName }} {{ editingEnrStudent?.lastName }}</span>
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
              <label>Change Request Type</label>
              <select v-model="noteChanges[editingEnrollment.id]" class="note-sel">
                <option value="">— Select type —</option>
                <option v-for="st in PARTNER_REQUEST_STATUSES" :key="st" :value="st">{{ st }}</option>
              </select>
            </div>
            <div class="field">
              <label>Note / Description</label>
              <textarea v-model="noteTexts[editingEnrollment.id]" rows="3" placeholder="Describe the change request…" class="note-ta"></textarea>
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
            <div class="section-heading">Admission</div>
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

            <div class="section-heading" style="margin-top:1.25rem">Request Drop / Status Change</div>
            <p class="hint-text">Use this to formally request a status change (e.g. drop out, deferral). The request will be reviewed by IBSS Admin.</p>
            <div class="field">
              <label>Requested Status</label>
              <select v-model="dropRequest.status">
                <option value="">— Select —</option>
                <option v-for="st in PARTNER_REQUEST_STATUSES" :key="st" :value="st">{{ st }}</option>
              </select>
            </div>
            <div class="field">
              <label>Reason</label>
              <textarea v-model="dropRequest.reason" rows="3" placeholder="Explain the reason for this request…" class="note-ta"></textarea>
            </div>
            <div v-if="dropSubmitted" class="success-msg">Request submitted as a change note.</div>
          </div>
          <div class="drawer-actions">
            <button class="btn-cancel" @click="showEnrEdit = false">Close</button>
            <button class="btn-save" :disabled="!dropRequest.status || !dropRequest.reason" @click="submitDropRequest">Submit Request</button>
          </div>
        </template>
      </div>
    </transition>

    <!-- ══ Registration Wizard ══════════════════════════════════════════════════ -->
    <transition name="fade"><div v-if="showReg" class="drawer-overlay" @click.self="showReg = false" /></transition>
    <transition name="slide">
      <div v-if="showReg" class="drawer drawer-wide">
        <div class="drawer-header">
          <div>
            <h2>New Student Application</h2>
            <p class="drawer-sub">Step {{ regStep }} of 3 —
              <span v-if="regStep === 1">Registration Form</span>
              <span v-else-if="regStep === 2">Document Verification &amp; Pathway</span>
              <span v-else>Issue Offer</span>
            </p>
          </div>
          <button class="drawer-close" @click="showReg = false">✕</button>
        </div>

        <!-- ── Step 1: Registration Form ── -->
        <div v-if="regStep === 1" class="drawer-form">
          <div class="row-2">
            <div class="field"><label>First Name <span class="req">*</span></label><input v-model="regForm.firstName" required /></div>
            <div class="field"><label>Last Name <span class="req">*</span></label><input v-model="regForm.lastName" required /></div>
          </div>
          <div class="row-2">
            <div class="field"><label>Date of Birth <span class="req">*</span></label><input v-model="regForm.dateOfBirth" type="date" required /></div>
            <div class="field"><label>Email <span class="req">*</span></label><input v-model="regForm.email" type="email" required /></div>
          </div>
          <div class="field"><label>Passport / ID No. <span class="req">*</span></label><input v-model="regForm.passportId" required /></div>
          <div class="field"><label>Address</label><input v-model="regForm.address" /></div>
          <div class="field">
            <label>Partner</label>
            <input :value="auth.user?.name" disabled class="input-locked" />
          </div>
          <div class="row-2">
            <div class="field">
              <label>Programme <span class="req">*</span></label>
              <select v-model="regForm.programme" required @change="regForm.specialization = ''">
                <option value="">— Select —</option>
                <optgroup label="IBSS Core Programmes">
                  <option v-for="p in corePrograms" :key="p.id" :value="p.name">{{ p.name }}</option>
                </optgroup>
                <optgroup v-if="myProgClones.length" label="My Programmes">
                  <option v-for="c in myProgClones" :key="c.id" :value="c.name"
                    :disabled="c.status !== 'approved'"
                    :title="c.status !== 'approved' ? 'Pending IBSS approval — cannot enrol students yet' : ''">
                    {{ c.name }}{{ c.status !== 'approved' ? ' (not approved)' : '' }}
                  </option>
                </optgroup>
              </select>
            </div>
            <div class="field">
              <label>Specialization <span class="req">*</span></label>
              <select v-model="regForm.specialization" required>
                <option value="">— Select —</option>
                <option v-for="n in specializationsForProgramme(regForm.programme)" :key="n">{{ n }}</option>
              </select>
            </div>
          </div>
          <div class="row-2">
            <div class="field"><label>Commencement Date <span class="req">*</span></label><input v-model="regForm.commencementDate" type="date" required /></div>
            <div class="field"><label>Duration of Study</label><input v-model="regForm.durationOfStudy" placeholder="e.g. 18 months" /></div>
          </div>
          <div class="field">
            <label>Mode of Study <span class="req">*</span></label>
            <select v-model="regForm.modeOfStudy" required>
              <option value="">— Select —</option>
              <option>Distance/Online self-study</option>
              <option>Blended learning</option>
              <option>On-campus</option>
            </select>
          </div>
          <div class="row-2">
            <div class="field"><label>Highest Degree</label><input v-model="regForm.highestDegree" placeholder="e.g. Bachelor of Science" /></div>
            <div class="field"><label>Language Result</label><input v-model="regForm.languageResult" placeholder="e.g. IELTS 6.5" /></div>
          </div>
          <div class="field"><label>Years of Work Experience</label><input v-model.number="regForm.yearsWorkExperience" type="number" min="0" placeholder="0" /></div>

          <div class="upload-section">
            <h4 class="upload-title">Document Uploads</h4>
            <div class="upload-grid">
              <div class="upload-item">
                <label class="upload-label">Passport / ID</label>
                <input type="file" class="file-input" @change="handleFileUpload('docPassport', $event)" />
                <span v-if="regForm.docPassport" class="file-name">{{ regForm.docPassport }}</span>
              </div>
              <div class="upload-item">
                <label class="upload-label">Degree Certificate</label>
                <input type="file" class="file-input" @change="handleFileUpload('docDegree', $event)" />
                <span v-if="regForm.docDegree" class="file-name">{{ regForm.docDegree }}</span>
              </div>
              <div class="upload-item">
                <label class="upload-label">Language Result</label>
                <input type="file" class="file-input" @change="handleFileUpload('docLanguage', $event)" />
                <span v-if="regForm.docLanguage" class="file-name">{{ regForm.docLanguage }}</span>
              </div>
              <div class="upload-item">
                <label class="upload-label">CV / Résumé</label>
                <input type="file" class="file-input" @change="handleFileUpload('docCV', $event)" />
                <span v-if="regForm.docCV" class="file-name">{{ regForm.docCV }}</span>
              </div>
            </div>
          </div>

          <div class="drawer-actions">
            <button type="button" class="btn-cancel" @click="showReg = false">Cancel</button>
            <button type="button" class="btn-save"
              :disabled="!regForm.firstName || !regForm.lastName || !regForm.dateOfBirth || !regForm.email || !regForm.passportId || !regForm.programme || !regForm.specialization || !regForm.commencementDate || !regForm.modeOfStudy"
              @click="regStep = 2">Next →</button>
          </div>
        </div>

        <!-- ── Step 2: Document Verification & Pathway ── -->
        <div v-else-if="regStep === 2" class="drawer-form">
          <h4 class="section-heading">Section A — Document Verification</h4>
          <p class="hint-text">Confirm the authenticity of each submitted document.</p>
          <div class="check-list">
            <label class="check-item">
              <input type="checkbox" v-model="regDocs.passport" />
              <span>Passport / ID</span>
              <span v-if="regForm.docPassport" class="file-chip">{{ regForm.docPassport }}</span>
            </label>
            <label class="check-item">
              <input type="checkbox" v-model="regDocs.degree" />
              <span>Degree Certificate</span>
              <span v-if="regForm.docDegree" class="file-chip">{{ regForm.docDegree }}</span>
            </label>
            <label class="check-item">
              <input type="checkbox" v-model="regDocs.language" />
              <span>Language Result</span>
              <span v-if="regForm.docLanguage" class="file-chip">{{ regForm.docLanguage }}</span>
            </label>
            <label class="check-item">
              <input type="checkbox" v-model="regDocs.cv" />
              <span>CV / Résumé</span>
              <span v-if="regForm.docCV" class="file-chip">{{ regForm.docCV }}</span>
            </label>
          </div>

          <h4 class="section-heading" style="margin-top:1.5rem">Section B — Qualification Pathway</h4>
          <p class="hint-text">Select the entry pathway that matches the applicant's qualifications.</p>
          <div class="pathway-list">
            <label v-for="pw in regPathways" :key="pw.id" class="pathway-item">
              <input type="radio" :value="pw.id" v-model="regPathway" />
              <span>{{ pw.label }}</span>
            </label>
          </div>

          <div class="drawer-actions">
            <button type="button" class="btn-cancel" @click="regStep = 1">← Back</button>
            <button type="button" class="btn-save" :disabled="!step2Valid" @click="regStep = 3">Next →</button>
          </div>
        </div>

        <!-- ── Step 3: Issue Offer ── -->
        <div v-else class="drawer-form">
          <div class="summary-card">
            <h4 class="summary-title">Application Summary</h4>
            <div class="summary-row"><span class="summary-label">Name</span><span>{{ regForm.firstName }} {{ regForm.lastName }}</span></div>
            <div class="summary-row"><span class="summary-label">Programme</span><span>{{ regForm.programme }}</span></div>
            <div class="summary-row"><span class="summary-label">Specialization</span><span>{{ regForm.specialization }}</span></div>
            <div class="summary-row">
              <span class="summary-label">Pathway</span>
              <span>{{ regPathways.find(p => p.id === regPathway)?.label ?? '—' }}</span>
            </div>
            <div class="summary-row">
              <span class="summary-label">Docs Verified</span>
              <span>
                <span v-if="regDocs.passport" class="doc-chip">Passport</span>
                <span v-if="regDocs.degree"   class="doc-chip">Degree</span>
                <span v-if="regDocs.language" class="doc-chip">Language</span>
                <span v-if="regDocs.cv"       class="doc-chip">CV</span>
              </span>
            </div>
          </div>

          <h4 class="section-heading" style="margin-top:1.25rem">Select Offer Type</h4>
          <div class="offer-type-list">
            <label class="offer-type-item">
              <input type="radio" value="offer" v-model="regOfferType" />
              <div>
                <strong>Full Offer Letter</strong>
                <p class="offer-type-desc">Applicant meets all entry requirements. Issue a full unconditional offer.</p>
              </div>
            </label>
            <label class="offer-type-item">
              <input type="radio" value="conditional_offer" v-model="regOfferType" />
              <div>
                <strong>Conditional Offer Letter</strong>
                <p class="offer-type-desc">Applicant meets most requirements; offer is subject to outstanding conditions.</p>
              </div>
            </label>
          </div>

          <div v-if="regSuccess" class="success-msg">Application for <strong>{{ regSuccess }}</strong> submitted!</div>

          <div class="drawer-actions">
            <button type="button" class="btn-cancel" @click="regStep = 2">← Back</button>
            <button type="button" class="btn-save" :disabled="!regOfferType" @click="submitRegistration">Issue Offer Letter</button>
          </div>
        </div>

      </div>
    </transition>

    <!-- ══ Letter Modal ═══════════════════════════════════════════════════════ -->
    <transition name="fade"><div v-if="showLetter" class="modal-overlay" @click.self="showLetter = false" /></transition>
    <transition name="modal-pop">
      <div v-if="showLetter && letterStudent" class="modal">
        <div class="modal-header">
          <h2>{{ letterType === 'offer' ? 'Letter of Offer' : 'Letter of Admission' }}</h2>
          <div class="modal-header-actions">
            <button class="btn-print" @click="printLetter">Print / Save PDF</button>
            <button class="drawer-close" @click="showLetter = false">✕</button>
          </div>
        </div>
        <div class="modal-body letter-body">
          <div class="letter-sheet">
            <div class="letter-header-block">
              <div class="letter-logo-text">IBSS</div>
              <div class="letter-org">
                <strong>International Business School of Scandinavia</strong><br>
                <span class="letter-org-sub">in partnership with {{ letterStudent.partner }}</span>
              </div>
            </div>
            <hr class="letter-rule" />
            <p class="letter-date">{{ todayFormatted }}</p>
            <h3 class="letter-type-heading">{{ letterType === 'offer' ? 'LETTER OF OFFER' : 'LETTER OF ADMISSION' }}</h3>
            <p class="letter-dear">Dear {{ letterStudent.firstName }} {{ letterStudent.lastName }},</p>
            <p class="letter-body-text" v-if="letterType === 'offer'">
              We are pleased to offer you a place at the International Business School of Scandinavia
              through our partner institution <strong>{{ letterStudent.partner }}</strong>. This offer
              is subject to the verification of your academic qualifications and supporting documents.
            </p>
            <p class="letter-body-text" v-else>
              We are pleased to confirm your admission to the International Business School of Scandinavia
              through our partner institution <strong>{{ letterStudent.partner }}</strong>. Your place
              has been formally reserved and we look forward to welcoming you.
            </p>
            <table class="letter-details-table">
              <tr><td class="ldt-label">Student ID</td><td class="ldt-val mono">{{ letterStudent.studentId }}</td></tr>
              <tr><td class="ldt-label">Full Name</td><td class="ldt-val">{{ letterStudent.firstName }} {{ letterStudent.lastName }}</td></tr>
              <tr><td class="ldt-label">Programme</td><td class="ldt-val">{{ letterEnrollment?.programme }}</td></tr>
              <tr><td class="ldt-label">Specialization</td><td class="ldt-val">{{ letterEnrollment?.specialization }}</td></tr>
              <tr><td class="ldt-label">Commencement Date</td><td class="ldt-val">{{ fmtDate(letterEnrollment?.commencementDate) }}</td></tr>
              <tr><td class="ldt-label">Mode of Study</td><td class="ldt-val">{{ letterEnrollment?.modeOfStudy }}</td></tr>
              <tr><td class="ldt-label">Partner Institution</td><td class="ldt-val">{{ letterStudent.partner }}</td></tr>
            </table>
            <p class="letter-body-text" v-if="letterType === 'offer'">
              Please accept this offer by confirming your enrolment with your partner institution.
              Should you have any questions, please do not hesitate to contact our admissions office.
            </p>
            <p class="letter-body-text" v-else>
              Please retain this letter as confirmation of your admission. Your student ID and programme
              details are as stated above. We wish you every success in your studies.
            </p>
            <div class="letter-sign">
              <p>Yours sincerely,</p>
              <div class="letter-sig-line"></div>
              <p class="letter-sig-name">IBSS Admissions Office</p>
              <p class="letter-sig-org">International Business School of Scandinavia</p>
            </div>
          </div>
        </div>
      </div>
    </transition>

    <!-- ══ Certificate Modal ══════════════════════════════════════════════════ -->
    <transition name="fade"><div v-if="showCert" class="modal-overlay" @click.self="showCert = false" /></transition>
    <transition name="modal-pop">
      <div v-if="showCert && certStudent" class="modal modal-wide">
        <div class="modal-header">
          <h2>Academic Transcript</h2>
          <div class="modal-header-actions">
            <button class="btn-print" @click="printCert">Print / Save PDF</button>
            <button class="drawer-close" @click="showCert = false">✕</button>
          </div>
        </div>
        <div class="modal-body">
          <div class="letter-sheet">
            <div class="letter-header-block">
              <div class="letter-logo-text">IBSS</div>
              <div class="letter-org">
                <strong>International Business School of Scandinavia</strong><br>
                <span class="letter-org-sub">Academic Transcript — Official Record</span>
              </div>
            </div>
            <hr class="letter-rule" />
            <table class="letter-details-table" style="margin-bottom:1.5rem">
              <tr><td class="ldt-label">Student Name</td><td class="ldt-val">{{ certStudent.firstName }} {{ certStudent.lastName }}</td></tr>
              <tr><td class="ldt-label">Student ID</td><td class="ldt-val mono">{{ certStudent.studentId }}</td></tr>
              <tr><td class="ldt-label">Programme</td><td class="ldt-val">{{ certEnrollment?.programme }}</td></tr>
              <tr><td class="ldt-label">Specialization</td><td class="ldt-val">{{ certEnrollment?.specialization }}</td></tr>
              <tr><td class="ldt-label">Partner Institution</td><td class="ldt-val">{{ certStudent.partner }}</td></tr>
              <tr><td class="ldt-label">Commencement Date</td><td class="ldt-val">{{ fmtDate(certEnrollment?.commencementDate) }}</td></tr>
            </table>

            <h4 class="cert-section-title">Subject Results</h4>
            <table class="cert-grade-table">
              <thead>
                <tr>
                  <th>Subject</th>
                  <th class="tc">Credits</th>
                  <th class="tc">IBSS Grade</th>
                  <th class="tc">UK Grade</th>
                  <th class="tc">ECTS Grade</th>
                  <th class="tc">ECTS Points</th>
                  <th class="tc">Grade Points</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(row, name) in certGrades" :key="name">
                  <td>{{ name }}</td>
                  <td class="tc">{{ row.credits }}</td>
                  <td class="tc">{{ row.ibssGrade ?? '—' }}</td>
                  <td class="tc">{{ row.ukGrade ?? '—' }}</td>
                  <td class="tc">{{ row.ectsGrade ?? '—' }}</td>
                  <td class="tc">{{ row.ectsPoints != null ? row.ectsPoints.toFixed(1) : '—' }}</td>
                  <td class="tc cert-gp">{{ row.gradePoints != null ? row.gradePoints.toFixed(1) : '—' }}</td>
                </tr>
              </tbody>
              <tfoot>
                <tr class="cert-total-row">
                  <td><strong>Total</strong></td>
                  <td class="tc"><strong>{{ certTotalCredits }}</strong></td>
                  <td class="tc" colspan="4"></td>
                  <td class="tc cert-gp"><strong>{{ certTotalGradePoints.toFixed(1) }}</strong></td>
                </tr>
                <tr class="cert-gpa-row">
                  <td colspan="6" class="cert-gpa-label">Grade Point Average (GPA)</td>
                  <td class="tc cert-gp cert-gpa-val"><strong>{{ certGPA }}</strong></td>
                </tr>
              </tfoot>
            </table>

            <div class="cert-approved-stamp">
              <span class="cert-stamp-text">APPROVED ✓</span>
              <span class="cert-stamp-date">{{ todayFormatted }}</span>
            </div>

            <div class="letter-sign" style="margin-top:2rem">
              <p>Certified by,</p>
              <div class="letter-sig-line"></div>
              <p class="letter-sig-name">IBSS Academic Registry</p>
              <p class="letter-sig-org">International Business School of Scandinavia</p>
            </div>
          </div>
        </div>
      </div>
    </transition>

    <!-- ══ Add Enrollment Drawer ════════════════════════════════════════════════ -->
    <transition name="fade"><div v-if="showAddEnroll" class="drawer-overlay" @click.self="showAddEnroll = false" /></transition>
    <transition name="slide">
      <div v-if="showAddEnroll" class="drawer">
        <div class="drawer-header">
          <div>
            <h2>Add Programme Enrolment</h2>
            <p class="drawer-sub" v-if="addEnrollTarget">{{ addEnrollTarget.studentId }} — {{ addEnrollTarget.firstName }} {{ addEnrollTarget.lastName }}</p>
          </div>
          <button class="drawer-close" @click="showAddEnroll = false">✕</button>
        </div>
        <div class="drawer-form">
          <div class="field">
            <label>Programme <span class="req">*</span></label>
            <select v-model="addEnrollForm.programme" @change="addEnrollForm.specialization = ''">
              <option value="">— Select —</option>
              <optgroup label="IBSS Core Programmes">
                <option v-for="p in corePrograms" :key="p.id" :value="p.name">{{ p.name }}</option>
              </optgroup>
              <optgroup v-if="myProgClones.length" label="My Programmes">
                <option v-for="c in myProgClones" :key="c.id" :value="c.name"
                  :disabled="c.status !== 'approved'"
                  :title="c.status !== 'approved' ? 'Pending IBSS approval — cannot enrol students yet' : ''">
                  {{ c.name }}{{ c.status !== 'approved' ? ' (not approved)' : '' }}
                </option>
              </optgroup>
            </select>
          </div>
          <div class="field">
            <label>Specialization <span class="req">*</span></label>
            <select v-model="addEnrollForm.specialization">
              <option value="">— Select —</option>
              <option v-for="n in specializationsForProgramme(addEnrollForm.programme)" :key="n">{{ n }}</option>
            </select>
          </div>
          <div class="field">
            <label>Commencement Date</label>
            <input v-model="addEnrollForm.commencementDate" type="date" />
          </div>
          <div class="field">
            <label>Mode of Study</label>
            <select v-model="addEnrollForm.modeOfStudy">
              <option>Distance/Online self-study</option>
              <option>Blended learning</option>
              <option>On-campus</option>
            </select>
          </div>
          <div class="field">
            <label>Duration of Study</label>
            <input v-model="addEnrollForm.durationOfStudy" placeholder="e.g. 18 months" />
          </div>
          <div class="drawer-actions">
            <button class="btn-cancel" @click="showAddEnroll = false">Cancel</button>
            <button class="btn-save" :disabled="!addEnrollForm.programme || !addEnrollForm.specialization" @click="submitAddEnrollment">Add Enrolment</button>
          </div>
        </div>
      </div>
    </transition>

    <!-- ══ Student Review Wizard ════════════════════════════════════════════════ -->
    <StudentReviewWizard v-if="reviewingStudent" :student="reviewingStudent"
      @close="closeReview" @submitted="reviewCompleted" />

    <!-- ══ Review completed toast ══════════════════════════════════════════════ -->
    <transition name="fade">
      <div v-if="reviewToast" class="review-toast">{{ reviewToast }}</div>
    </transition>

  </div>
</template>

<script setup>
import { ref, computed, reactive, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { auth } from '../store/auth.js'
import apiClient from '../api/client.js'
import { students, getNextId, ENROLLMENT_STATUSES, nextEnrollId } from '../mock/data.js'

// Public student-signup URL for THIS partner. Shareable link the partner
// can paste into onboarding emails / their own website. Resolves at
// runtime from the browser's origin so dev / staging / prod each get
// the right host without env wiring.
const signupLink = computed(() => {
  if (!auth.user?.partnerSlug) return ''
  return `${window.location.origin}/#/apply?partner=${auth.user.partnerSlug}`
})
const signupLinkCopied = ref(false)
async function copySignupLink() {
  if (!signupLink.value) return
  try { await navigator.clipboard.writeText(signupLink.value) }
  catch { /* clipboard might be blocked — silently ignore */ }
  signupLinkCopied.value = true
  setTimeout(() => { signupLinkCopied.value = false }, 1800)
}

const vClickOutside = {
  mounted(el, binding) {
    el._out = e => { if (!el.contains(e.target)) binding.value(e) }
    document.addEventListener('mousedown', el._out)
  },
  unmounted(el) { document.removeEventListener('mousedown', el._out) },
}


// Statuses a partner is allowed to request
const PARTNER_REQUEST_STATUSES = [
  'Active (final project)',
  'Applicant withdraw',
  'Dismissed',
  'Drop out',
  'Transferred',
]
import { resolveSubjects, getGradeInfo, getProgrammeNames, getSpecializationNames, getProgramLevel, PATHWAYS, corePrograms, partnerRecords } from '../mock/programmes.js'
function uid() { return Math.random().toString(36).slice(2, 10) }
import { gradesStore, saveGrades, isGraded } from '../store/grades.js'
import { absences } from '../mock/absences.js'
import { tickets } from '../mock/tickets.js'
import PartnerStudentsTab from '../components/partner/tabs/PartnerStudentsTab.vue'
import PartnerUsersTab from '../components/partner/tabs/PartnerUsersTab.vue'
import StudentReviewWizard from '../components/partner/StudentReviewWizard.vue'

const router = useRouter()

// ── Main tab ──────────────────────────────────────────────────────────────────
const mainTab = ref('students')

// ── Add Student modal ────────────────────────────────────────────────────────
// Hosts the public signup wizard inside an iframe, scoped to this partner's
// slug. Closing the modal triggers a refresh of the students table.
const showAddStudent = ref(false)
function closeAddStudent() {
  showAddStudent.value = false
  // Tell the partner students tab to reload, since the student count likely
  // changed. The tab loads on tab switch; bumping a key forces a remount.
  studentsRefreshKey.value++
}
const studentsRefreshKey = ref(0)
const usersRefreshKey = ref(0)

// ── My Core Programmes (real API) ─────────────────────────────────────────────
const coreAccessItems = ref([])
const coreAccessLoading = ref(false)
const coreAccessError = ref('')
const coreBusy = reactive(new Set())

const coreAccessGrouped = computed(() => {
  const map = new Map()
  for (const item of coreAccessItems.value) {
    if (!map.has(item.programmeId)) {
      map.set(item.programmeId, { programmeId: item.programmeId, programmeName: item.programmeName, specializations: [] })
    }
    map.get(item.programmeId).specializations.push(item)
  }
  return [...map.values()]
})

async function loadCoreAccess() {
  coreAccessLoading.value = true
  coreAccessError.value = ''
  try {
    const res = await apiClient.get('/v1/partner/programme-access')
    coreAccessItems.value = res.data.items ?? []
  } catch (e) {
    coreAccessError.value = e.response?.data?.message ?? e.message ?? 'Failed to load'
  } finally {
    coreAccessLoading.value = false
  }
}

async function toggleSpecialization(item, disabled) {
  if (coreBusy.has(item.specializationId)) return
  coreBusy.add(item.specializationId)
  try {
    await apiClient.patch(`/v1/partner/programme-access/${item.specializationId}`, { disabled })
    item.disabledByPartner = disabled
  } catch (e) {
    coreAccessError.value = e.response?.data ?? e.message ?? 'Failed to toggle'
  } finally {
    coreBusy.delete(item.specializationId)
  }
}

watch(mainTab, t => {
  // Reload the active tab's data on every switch (was previously gated by
  // "only on first time"). The lazy guards were OK for first load but stale
  // data is worse than a 100ms extra round-trip.
  if (t === 'students') studentsRefreshKey.value++
  if (t === 'core' && !coreAccessLoading.value) loadCoreAccess()
  if (t === 'users') usersRefreshKey.value++
})
onMounted(() => { if (mainTab.value === 'core') loadCoreAccess() })

// ── My Programs (real API) ────────────────────────────────────────────────────
const myProgClones = ref([])
const myProgLoading = ref(false)
const myProgError = ref('')
const progBusy = reactive(new Set())

const pathwaysCatalog = ref([])
async function loadPathways() {
  try {
    const res = await apiClient.get('/v1/school/system-config/pathways')
    pathwaysCatalog.value = res.data.items ?? []
  } catch (e) { /* non-blocking */ }
}
loadPathways()

function togglePathwayOnClone(clone, pathwayId) {
  if (!Array.isArray(clone.pathwayIds)) clone.pathwayIds = []
  const idx = clone.pathwayIds.indexOf(pathwayId)
  if (idx >= 0) clone.pathwayIds.splice(idx, 1)
  else clone.pathwayIds.push(pathwayId)
}

const pathwayOpen = reactive(new Set())
function togglePathwayPanel(cloneId) {
  if (pathwayOpen.has(cloneId)) pathwayOpen.delete(cloneId)
  else pathwayOpen.add(cloneId)
}
const pendingProgCount = computed(() =>
  myProgClones.value.filter(c => c.status === 'pending').length
)

const cloneSources = computed(() => {
  const map = new Map()
  for (const item of coreAccessItems.value) {
    if (!map.has(item.programmeId)) {
      map.set(item.programmeId, { id: item.programmeId, name: item.programmeName, specializationCount: 0 })
    }
    map.get(item.programmeId).specializationCount++
  }
  return [...map.values()]
})

async function loadMyPrograms() {
  myProgLoading.value = true
  myProgError.value = ''
  try {
    const res = await apiClient.get('/v1/partner/my-programs')
    myProgClones.value = (res.data.items ?? []).map(toLocalProg)
  } catch (e) {
    myProgError.value = e.response?.data?.message ?? e.message ?? 'Failed to load'
  } finally {
    myProgLoading.value = false
  }
}

function toLocalProg(row) {
  return {
    id: row.programmeId,
    name: row.name,
    code: row.code,
    status: (row.status ?? 'Draft').toLowerCase(),
    isActive: !!row.isActive,
    isDisabledByAdmin: !!row.isDisabledByAdmin,
    rejectionReason: row.rejectionReason ?? null,
    hasEnrolments: !!row.hasEnrolments,
    canDelete: !!row.canDelete,
    pathwayIds: row.pathwayIds ?? [],
    _detailLoaded: false,
    specializations: [],
  }
}

async function loadProgDetail(clone) {
  const res = await apiClient.get(`/v1/partner/my-programs/${clone.id}`)
  const d = res.data
  clone.name = d.name
  clone.code = d.code
  clone.status = (d.status ?? 'Draft').toLowerCase()
  clone.isActive = !!d.isActive
  clone.isDisabledByAdmin = !!d.isDisabledByAdmin
  clone.rejectionReason = d.rejectionReason ?? null
  clone.hasEnrolments = !!d.hasEnrolments
  clone.pathwayIds = Array.isArray(d.pathwayIds) ? [...d.pathwayIds] : []
  clone.specializations = (d.specializations ?? []).map(m => ({
    id: m.specializationId,
    name: m.name,
    subjects: (m.subjects ?? []).map(s => ({
      id: s.subjectId,
      code: s.code,
      name: s.name,
      ects: s.ects,
    })),
  }))
  clone._detailLoaded = true
}

// ── My Programs tab state ─────────────────────────────────────────────────────
const showCloneModal       = ref(false)
const showFromScratchModal = ref(false)
const fromScratchName      = ref('')
const fromScratchBusy      = ref(false)
const expandedProg    = ref(null)
const expandedMaj     = ref(null)
const newSubjForms    = reactive({})
const newMajNameForms = reactive({})

function isProgEditable(clone) {
  if (clone.isDisabledByAdmin) return false
  if (clone.hasEnrolments) return false
  if (clone.status === 'pending') return false
  return clone.status === 'draft' || clone.status === 'rejected' || clone.status === 'approved'
}

async function toggleProgEdit(cloneId) {
  if (expandedProg.value === cloneId) {
    expandedProg.value = null
    expandedMaj.value = null
    return
  }
  expandedProg.value = cloneId
  expandedMaj.value = null
  const clone = myProgClones.value.find(c => c.id === cloneId)
  if (clone && !clone._detailLoaded) {
    try { await loadProgDetail(clone) }
    catch (e) { myProgError.value = e.response?.data?.message ?? e.message ?? 'Failed to load details' }
  }
}
function toggleMajEdit(key) {
  expandedMaj.value = expandedMaj.value === key ? null : key
}

async function doCloneProgram(srcProgId) {
  if (progBusy.has(srcProgId)) return
  progBusy.add(srcProgId)
  try {
    await apiClient.post('/v1/partner/my-programs', { sourceProgrammeId: srcProgId })
    showCloneModal.value = false
    await loadMyPrograms()
  } catch (e) {
    myProgError.value = e.response?.data?.error ?? e.message ?? 'Failed to clone'
  } finally {
    progBusy.delete(srcProgId)
  }
}

async function doCreateFromScratch() {
  const name = fromScratchName.value.trim()
  if (!name) return
  fromScratchBusy.value = true
  try {
    await apiClient.post('/v1/partner/my-programs', { sourceProgrammeId: null, name })
    showFromScratchModal.value = false
    fromScratchName.value = ''
    await loadMyPrograms()
  } catch (e) {
    myProgError.value = e.response?.data?.error ?? e.message ?? 'Failed to create'
  } finally {
    fromScratchBusy.value = false
  }
}

async function saveProgEdit(clone) {
  if (progBusy.has(clone.id)) return
  progBusy.add(clone.id)
  try {
    await apiClient.patch(`/v1/partner/my-programs/${clone.id}`, {
      name: clone.name,
      code: clone.code,
      specializations: clone.specializations.map(m => ({
        specializationId: isServerId(m.id) ? m.id : null,
        name: m.name,
        subjects: m.subjects.map(s => ({
          subjectId: isServerId(s.id) ? s.id : null,
          code: s.code ?? '',
          name: s.name ?? '',
          ects: Number(s.ects) || 0,
        })),
      })),
      pathwayIds: Array.isArray(clone.pathwayIds) ? [...clone.pathwayIds] : [],
    })
    await loadProgDetail(clone)
    const listRes = await apiClient.get('/v1/partner/my-programs')
    const refreshed = (listRes.data.items ?? []).find(i => i.programmeId === clone.id)
    if (refreshed) {
      clone.status = (refreshed.status ?? 'Draft').toLowerCase()
      clone.hasEnrolments = !!refreshed.hasEnrolments
      clone.canDelete = !!refreshed.canDelete
    }
  } catch (e) {
    myProgError.value = e.response?.data?.error ?? e.message ?? 'Failed to save'
  } finally {
    progBusy.delete(clone.id)
  }
}

async function submitProgForApproval(clone) {
  if (progBusy.has(clone.id)) return
  progBusy.add(clone.id)
  try {
    await apiClient.post(`/v1/partner/my-programs/${clone.id}/submit`)
    expandedProg.value = null
    await loadMyPrograms()
  } catch (e) {
    myProgError.value = e.response?.data?.error ?? e.message ?? 'Failed to submit'
  } finally {
    progBusy.delete(clone.id)
  }
}

async function toggleProgActive(clone, isActive) {
  if (progBusy.has(clone.id)) return
  progBusy.add(clone.id)
  try {
    await apiClient.patch(`/v1/partner/my-programs/${clone.id}/active`, { isActive })
    clone.isActive = isActive
  } catch (e) {
    myProgError.value = e.response?.data?.error ?? e.message ?? 'Failed to toggle'
  } finally {
    progBusy.delete(clone.id)
  }
}

async function deleteProgram(clone) {
  if (!confirm(`Delete programme "${clone.name}"? This cannot be undone.`)) return
  if (progBusy.has(clone.id)) return
  progBusy.add(clone.id)
  try {
    await apiClient.delete(`/v1/partner/my-programs/${clone.id}`)
    await loadMyPrograms()
  } catch (e) {
    myProgError.value = e.response?.data?.error ?? e.message ?? 'Failed to delete'
  } finally {
    progBusy.delete(clone.id)
  }
}

function isServerId(id) {
  return typeof id === 'string' && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(id)
}

function removeMajFromClone(clone, majId) {
  const i = clone.specializations.findIndex(m => m.id === majId)
  if (i >= 0) clone.specializations.splice(i, 1)
}

function addMajToClone(clone) {
  const name = (newMajNameForms[clone.id] ?? '').trim()
  if (!name) return
  clone.specializations.push({ id: uid(), name, subjects: [] })
  newMajNameForms[clone.id] = ''
}

function removeSubjFromMaj(maj, subjId) {
  const i = maj.subjects.findIndex(s => s.id === subjId)
  if (i >= 0) maj.subjects.splice(i, 1)
}

function addSubjToMaj(clone, maj) {
  const codeKey = clone.id + maj.id + '_code'
  const nKey    = clone.id + maj.id + '_n'
  const cKey    = clone.id + maj.id + '_c'
  const name = (newSubjForms[nKey] ?? '').trim()
  if (!name) return
  maj.subjects.push({ id: uid(), code: (newSubjForms[codeKey] ?? '').trim(), name, ects: Number(newSubjForms[cKey]) || 15 })
  newSubjForms[codeKey] = ''
  newSubjForms[nKey] = ''
  newSubjForms[cKey] = 15
}

watch(mainTab, t => {
  // Always reload My Programmes on tab switch so admin actions (approve /
  // reject / delete) show up without a manual refresh. Core access is
  // refreshed too because the page shows it as a sidebar list.
  if (t === 'programs' && !myProgLoading.value) {
    loadMyPrograms()
    if (!coreAccessLoading.value) loadCoreAccess()
  }
})
onMounted(() => { if (mainTab.value === 'programs') { loadMyPrograms(); if (coreAccessItems.value.length === 0) loadCoreAccess() } })

function progStatusLabel(status) {
  return { draft: 'Draft', pending: 'Pending Approval', approved: 'Approved ✓', rejected: 'Rejected' }[status] ?? status
}
function progStatusClass(status) {
  return { draft: 'ps-draft', pending: 'ps-pending', approved: 'ps-approved', rejected: 'ps-rejected' }[status] ?? ''
}

// ── Helpers ───────────────────────────────────────────────────────────────────
const programmeNames = computed(() => getProgrammeNames())
const specializationNames     = computed(() => getSpecializationNames())

// Returns specialization names for a given programme name (core or partner clone)
function specializationsForProgramme(programmeName) {
  if (!programmeName) return getSpecializationNames()
  // Check partner clones first
  const partnerClone = myProgClones.value.find(c => c.name === programmeName)
  if (partnerClone) return partnerClone.specializations.map(m => m.name)
  // Fall back to core programme
  const coreProg = corePrograms.find(p => p.name === programmeName)
  if (coreProg) return coreProg.specializations.map(m => m.name)
  return getSpecializationNames()
}

function fmtDate(d) {
  if (!d) return '—'
  return new Date(d).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })
}

const todayFormatted = new Date().toLocaleDateString('en-GB', { day: '2-digit', month: 'long', year: 'numeric' })

// ── My students ───────────────────────────────────────────────────────────────
const myStudents = computed(() =>
  students.filter(s => s.partner === auth.user?.name)
)

// ── Card collapse ─────────────────────────────────────────────────────────────
const collapsedCards = reactive(new Set())
function toggleCard(id) {
  if (collapsedCards.has(id)) collapsedCards.delete(id)
  else collapsedCards.add(id)
}

// ── Search / filter ───────────────────────────────────────────────────────────
const searchQuery  = ref('')
const showProgDrop = ref(false)
const showMajDrop  = ref(false)
const filterProgs  = reactive(new Set())
const filterMajs   = reactive(new Set())

function toggleFilter(set, val) {
  if (set.has(val)) set.delete(val)
  else set.add(val)
}

function fuzzy(hay, needle) {
  if (!needle) return true
  const h = hay.toLowerCase(), n = needle.toLowerCase().trim()
  let i = 0
  for (const ch of n) { const p = h.indexOf(ch, i); if (p < 0) return false; i = p + 1 }
  return true
}

const availableProgs = computed(() => {
  const s = new Set()
  myStudents.value.forEach(st => st.enrollments?.forEach(e => s.add(e.programme)))
  return [...s].sort()
})
const availableMajs = computed(() => {
  const s = new Set()
  myStudents.value.forEach(st => st.enrollments?.forEach(e => s.add(e.specialization)))
  return [...s].sort()
})

const filteredMyStudents = computed(() => {
  const q = searchQuery.value.trim()
  return myStudents.value.filter(s => {
    if (q && !fuzzy(`${s.firstName} ${s.lastName}`, q) && !fuzzy(s.studentId, q)) return false
    if (filterProgs.size && !s.enrollments?.some(e => filterProgs.has(e.programme))) return false
    if (filterMajs.size  && !s.enrollments?.some(e => filterMajs.has(e.specialization)))      return false
    return true
  })
})

// ── Status logic ──────────────────────────────────────────────────────────────
function displayStatus(s) {
  if (s.certReleased)         return 'approved'
  if (isGraded(s.studentId))  return 'graded'
  if (s.admissionConfirmed)   return 'confirmed'
  if (s.paymentDone)          return 'admission'
  if (s.offerType)            return 'offer'
  return 'new'
}

const STATUS_LABELS = { new: 'New', offer: 'Offer Issued', confirmed: 'Admitted', admission: 'Admission', graded: 'Graded', approved: 'Approved' }
function statusLabel(s) { return STATUS_LABELS[displayStatus(s)] ?? displayStatus(s) }

// ── Registration Wizard ───────────────────────────────────────────────────────
// ── Review Application wizard ─────────────────────────────────────────────────
const reviewingStudent = ref(null)
function openReview(s) { reviewingStudent.value = s }
function closeReview() { reviewingStudent.value = null }
function reviewCompleted(s) {
  reviewToast.value = `Review submitted for ${s.firstName} ${s.lastName}`
  setTimeout(() => { reviewToast.value = '' }, 3200)
}
const reviewToast = ref('')

const showReg   = ref(false)
const regStep   = ref(1)
const regSuccess = ref('')

const regForm = reactive({
  firstName: '', lastName: '', dateOfBirth: '', email: '',
  passportId: '', address: '',
  programme: '', specialization: '', commencementDate: '', durationOfStudy: '', modeOfStudy: '',
  highestDegree: '', languageResult: '', yearsWorkExperience: 0,
  docPassport: null, docDegree: null, docLanguage: null, docCV: null,
})
const regDocs     = reactive({ passport: false, degree: false, language: false, cv: false })
const regPathway  = ref(null)
const regOfferType = ref(null)

function openAddStudent() {
  Object.assign(regForm, {
    firstName: '', lastName: '', dateOfBirth: '', email: '',
    passportId: '', address: '',
    programme: '', specialization: '', commencementDate: '', durationOfStudy: '', modeOfStudy: '',
    highestDegree: '', languageResult: '', yearsWorkExperience: 0,
    docPassport: null, docDegree: null, docLanguage: null, docCV: null,
  })
  Object.assign(regDocs, { passport: false, degree: false, language: false, cv: false })
  regPathway.value   = null
  regOfferType.value = null
  regStep.value      = 1
  regSuccess.value   = ''
  showReg.value      = true
}

const regLevel    = computed(() => getProgramLevel(regForm.programme))
const regPathways = computed(() => PATHWAYS[regLevel.value] ?? [])
const step2Valid  = computed(() =>
  (regDocs.passport || regDocs.degree || regDocs.language || regDocs.cv) && regPathway.value
)

function handleFileUpload(field, e) {
  regForm[field] = e.target.files[0]?.name ?? null
}

const progCodeMap = { 'Master of Business Administration':'MBA', 'Bachelor of Business Administration':'BBA', 'Master of Finance':'MF', 'Bachelor of Computer Science':'BCS', 'Master of Marketing':'MM' }

function submitRegistration() {
  const seq  = getNextId()
  const code = progCodeMap[regForm.programme] ?? 'GEN'
  const now  = new Date()
  const sid  = `IBSS.${code}.${String(now.getFullYear()).slice(2)}${String(now.getMonth()+1).padStart(2,'0')}${String(seq).padStart(4,'0')}`
  students.push({
    id: seq, studentId: sid,
    firstName: regForm.firstName, lastName: regForm.lastName,
    dateOfBirth: regForm.dateOfBirth, email: regForm.email,
    passportId: regForm.passportId, address: regForm.address,
    partner: auth.user?.name,
    highestDegree: regForm.highestDegree, languageResult: regForm.languageResult,
    yearsWorkExperience: regForm.yearsWorkExperience,
    docPassport: regForm.docPassport, docDegree: regForm.docDegree,
    docLanguage: regForm.docLanguage, docCV: regForm.docCV,
    docsVerified: { ...regDocs },
    enrollments: [
      {
        id: nextEnrollId(),
        programme: regForm.programme,
        specialization: regForm.specialization,
        commencementDate: regForm.commencementDate,
        durationOfStudy: regForm.durationOfStudy,
        modeOfStudy: regForm.modeOfStudy,
        selectedPathway: regPathway.value,
        offerType: regOfferType.value,
        paymentDone: false, admissionConfirmed: false,
        missingDocsSubmitted: false, certReleased: false,
        enrollmentStatus: 'Active',
        coursesCompleted: 0, coursesRequired: 8,
        finalProjectStatus: 'not applicable',
        transcriptReleased: false,
        tuitionFeeStatus: 'unpaid', otherFeesStatus: 'not applicable',
        changeNotes: [],
      },
    ],
  })
  regSuccess.value = `${regForm.firstName} ${regForm.lastName} (${sid})`
  setTimeout(() => { showReg.value = false; regSuccess.value = '' }, 1800)
}

// ── Change notes ──────────────────────────────────────────────────────────────
const noteTexts   = reactive({})
const noteChanges = reactive({})
function addChangeNote(enr) {
  const text = noteTexts[enr.id]
  if (!text) return
  enr.changeNotes.push({
    id: Date.now(),
    text,
    requestedChange: noteChanges[enr.id] || 'General',
    date: new Date().toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }),
  })
  noteTexts[enr.id]   = ''
  noteChanges[enr.id] = ''
}

// ── Enrollment status helpers ─────────────────────────────────────────────────
function enrollStatusClass(status) {
  const map = {
    'Active':                   'es-active',
    'Active (final project)':   'es-active',
    'Graduated':                'es-graduated',
    'Potential applicant':      'es-potential',
    'Potential applicant paid': 'es-potential',
    'Applicant withdraw':       'es-inactive',
    'Cancelled':                'es-inactive',
    'Dismissed':                'es-inactive',
    'Drop out':                 'es-inactive',
    'Deferred':                 'es-deferred',
    'Transferred':              'es-deferred',
  }
  return map[status] ?? 'es-default'
}
function fpClass(fp) {
  if (fp === 'done')    return 'fp-done'
  if (fp === 'doing')   return 'fp-doing'
  if (fp === 'not done') return 'fp-notdone'
  return 'fp-na'
}
function payChipClass(val) {
  if (val === 'fully paid')    return 'pay-green'
  if (val === 'partially paid') return 'pay-amber'
  if (val === 'unpaid')        return 'pay-red'
  return 'pay-na'
}

// ── Add Enrollment per existing student ───────────────────────────────────────
const showAddEnroll   = ref(false)
const addEnrollTarget = ref(null)
const addEnrollForm   = reactive({ programme: '', specialization: '', commencementDate: '', modeOfStudy: 'Distance/Online self-study', durationOfStudy: '' })

function openAddEnrollment(s) {
  addEnrollTarget.value = s
  Object.assign(addEnrollForm, { programme: '', specialization: '', commencementDate: '', modeOfStudy: 'Distance/Online self-study', durationOfStudy: '' })
  showAddEnroll.value = true
}
function submitAddEnrollment() {
  const s = addEnrollTarget.value
  if (!s || !addEnrollForm.programme || !addEnrollForm.specialization) return
  s.enrollments.push({
    id: nextEnrollId(),
    programme: addEnrollForm.programme,
    specialization: addEnrollForm.specialization,
    commencementDate: addEnrollForm.commencementDate,
    modeOfStudy: addEnrollForm.modeOfStudy,
    durationOfStudy: addEnrollForm.durationOfStudy,
    selectedPathway: null, offerType: null,
    paymentDone: false, admissionConfirmed: false,
    missingDocsSubmitted: false, certReleased: false,
    enrollmentStatus: 'Potential applicant',
    coursesCompleted: 0, coursesRequired: 8,
    finalProjectStatus: 'not applicable',
    transcriptReleased: false,
    tuitionFeeStatus: 'unpaid', otherFeesStatus: 'not applicable',
    changeNotes: [],
  })
  showAddEnroll.value = false
}

// ── Letter modal ──────────────────────────────────────────────────────────────
const showLetter      = ref(false)
const letterType      = ref('offer')
const letterStudent   = ref(null)
const letterEnrollment = ref(null)

function openLetter(student, enr, type) {
  letterStudent.value   = student
  letterEnrollment.value = enr
  letterType.value      = type
  showLetter.value      = true
}

function printLetter() {
  const s    = letterStudent.value
  const type = letterType.value
  const title = type === 'offer' ? 'LETTER OF OFFER' : 'LETTER OF ADMISSION'
  const bodyText = type === 'offer'
    ? `We are pleased to offer you a place at the International Business School of Scandinavia through our partner institution <strong>${s.partner}</strong>. This offer is subject to the verification of your academic qualifications and supporting documents.`
    : `We are pleased to confirm your admission to the International Business School of Scandinavia through our partner institution <strong>${s.partner}</strong>. Your place has been formally reserved and we look forward to welcoming you.`
  const closing = type === 'offer'
    ? 'Please accept this offer by confirming your enrolment with your partner institution. Should you have any questions, please do not hesitate to contact our admissions office.'
    : 'Please retain this letter as confirmation of your admission. Your student ID and programme details are as stated above. We wish you every success in your studies.'

  const enr = letterEnrollment.value
  const html = `<!DOCTYPE html><html><head><meta charset="utf-8"><title>${title}</title>
  <style>
    @page { size: A4; margin: 22mm 20mm; }
    body { font-family: Georgia, serif; font-size: 11pt; color: #111; line-height: 1.6; }
    .header { display: flex; align-items: center; gap: 16px; margin-bottom: 8px; }
    .logo { font-size: 28pt; font-weight: 900; color: #003366; letter-spacing: -1px; }
    .org { font-size: 10pt; color: #333; }
    .org strong { font-size: 12pt; color: #003366; }
    hr { border: none; border-top: 2px solid #003366; margin: 12px 0 20px; }
    .date { color: #555; font-size: 10pt; margin-bottom: 20px; }
    .type { font-size: 14pt; font-weight: bold; letter-spacing: 1px; color: #003366; margin-bottom: 18px; text-transform: uppercase; }
    .dear { margin-bottom: 14px; }
    .bodytext { margin-bottom: 14px; text-align: justify; }
    table { width: 100%; border-collapse: collapse; margin: 20px 0; }
    td { padding: 6px 10px; border-bottom: 1px solid #e0e0e0; font-size: 10pt; }
    td:first-child { width: 38%; color: #555; font-weight: bold; }
    .sign { margin-top: 36px; }
    .sig-line { border-top: 1px solid #555; width: 180px; margin: 28px 0 6px; }
    .sig-name { font-weight: bold; font-size: 10pt; margin: 0; }
    .sig-org { font-size: 9pt; color: #555; margin: 2px 0; }
  </style></head><body>
  <div class="header"><div class="logo">IBSS</div><div class="org"><strong>International Business School of Scandinavia</strong><br>in partnership with ${s.partner}</div></div>
  <hr/>
  <div class="date">${todayFormatted}</div>
  <div class="type">${title}</div>
  <p class="dear">Dear ${s.firstName} ${s.lastName},</p>
  <p class="bodytext">${bodyText}</p>
  <table>
    <tr><td>Student ID</td><td>${s.studentId}</td></tr>
    <tr><td>Full Name</td><td>${s.firstName} ${s.lastName}</td></tr>
    <tr><td>Programme</td><td>${enr?.programme ?? ''}</td></tr>
    <tr><td>Specialization</td><td>${enr?.specialization ?? ''}</td></tr>
    <tr><td>Commencement Date</td><td>${fmtDate(enr?.commencementDate)}</td></tr>
    <tr><td>Mode of Study</td><td>${enr?.modeOfStudy ?? ''}</td></tr>
    <tr><td>Partner Institution</td><td>${s.partner}</td></tr>
  </table>
  <p class="bodytext">${closing}</p>
  <div class="sign"><p>Yours sincerely,</p><div class="sig-line"></div><p class="sig-name">IBSS Admissions Office</p><p class="sig-org">International Business School of Scandinavia</p></div>
  <script>window.onload=function(){window.print()}<\/script></body></html>`

  const win = window.open('', '_blank', 'width=820,height=680')
  win.document.write(html)
  win.document.close()
}

// ── Certificate modal ─────────────────────────────────────────────────────────
const showCert      = ref(false)
const certStudent   = ref(null)
const certEnrollment = ref(null)

const certGrades = computed(() => {
  if (!certStudent.value) return {}
  return gradesStore[certStudent.value.studentId] ?? {}
})
const certTotalCredits = computed(() =>
  Object.values(certGrades.value).reduce((s, r) => s + (r.credits ?? 0), 0)
)
const certTotalGradePoints = computed(() =>
  Object.values(certGrades.value).reduce((s, r) => s + (r.gradePoints ?? 0), 0)
)
const certGPA = computed(() => {
  const rows = Object.values(certGrades.value).filter(r => r.gradePoints != null)
  if (!rows.length) return '—'
  const cr = rows.reduce((s, r) => s + (r.credits ?? 0), 0)
  if (!cr) return '—'
  return (rows.reduce((s, r) => s + r.gradePoints, 0) / cr).toFixed(2)
})

function openCert(student, enr) {
  certStudent.value   = student
  certEnrollment.value = enr
  showCert.value      = true
}

function printCert() {
  const s      = certStudent.value
  const grades = gradesStore[s.studentId] ?? {}
  const rows   = Object.entries(grades).map(([name, r]) =>
    `<tr><td>${name}</td><td class="tc">${r.credits ?? ''}</td><td class="tc">${r.ibssGrade ?? '—'}</td><td class="tc">${r.ukGrade ?? '—'}</td><td class="tc">${r.ectsGrade ?? '—'}</td><td class="tc">${r.ectsPoints != null ? r.ectsPoints.toFixed(1) : '—'}</td><td class="tc gp">${r.gradePoints != null ? r.gradePoints.toFixed(1) : '—'}</td></tr>`
  ).join('')
  const totalCr  = Object.values(grades).reduce((s, r) => s + (r.credits ?? 0), 0)
  const totalGP  = Object.values(grades).reduce((s, r) => s + (r.gradePoints ?? 0), 0)
  const gpaRows  = Object.values(grades).filter(r => r.gradePoints != null)
  const gpa      = gpaRows.length
    ? (gpaRows.reduce((s, r) => s + r.gradePoints, 0) / gpaRows.reduce((s, r) => s + (r.credits ?? 0), 0)).toFixed(2)
    : '—'

  const html = `<!DOCTYPE html><html><head><meta charset="utf-8"><title>Academic Transcript — ${s.studentId}</title>
  <style>
    @page { size: A4; margin: 22mm 20mm; }
    body { font-family: Arial, sans-serif; font-size: 10.5pt; color: #111; line-height: 1.5; }
    .header { display: flex; align-items: center; gap: 16px; margin-bottom: 8px; }
    .logo { font-size: 28pt; font-weight: 900; color: #003366; }
    .org strong { font-size: 12pt; color: #003366; }
    .org span { font-size: 9pt; color: #666; }
    hr { border: none; border-top: 2px solid #003366; margin: 10px 0 18px; }
    .info-table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }
    .info-table td { padding: 5px 8px; border-bottom: 1px solid #eee; font-size: 10pt; }
    .info-table td:first-child { width: 36%; color: #555; font-weight: bold; }
    h4 { color: #003366; font-size: 11pt; margin-bottom: 8px; text-transform: uppercase; letter-spacing: 0.5px; }
    .grade-table { width: 100%; border-collapse: collapse; margin-bottom: 18px; font-size: 9.5pt; }
    .grade-table th { background: #003366; color: #fff; padding: 6px 8px; text-align: center; font-size: 8.5pt; }
    .grade-table th:first-child { text-align: left; }
    .grade-table td { padding: 5px 8px; border-bottom: 1px solid #e8edf4; }
    .tc { text-align: center; }
    .gp { background: #eaf0f8; font-weight: 600; }
    .total-row td { background: #f0f3f7; font-weight: bold; border-top: 2px solid #ccd; }
    .gpa-row td { background: #e8f0f8; }
    .gpa-label { text-align: right; font-style: italic; padding-right: 8px; }
    .stamp { margin: 24px 0 0; padding: 12px 18px; border: 3px double #0d6b55; display: inline-block; }
    .stamp-text { font-size: 14pt; font-weight: 900; color: #0d6b55; letter-spacing: 2px; }
    .stamp-date { font-size: 9pt; color: #555; margin-top: 2px; }
    .sign { margin-top: 28px; }
    .sig-line { border-top: 1px solid #555; width: 180px; margin: 24px 0 5px; }
    .sig-name { font-weight: bold; font-size: 10pt; margin: 0; }
    .sig-org { font-size: 9pt; color: #555; }
  </style></head><body>
  <div class="header"><div class="logo">IBSS</div><div class="org"><strong>International Business School of Scandinavia</strong><br><span>Academic Transcript — Official Record</span></div></div>
  <hr/>
  <table class="info-table">
    <tr><td>Student Name</td><td>${s.firstName} ${s.lastName}</td></tr>
    <tr><td>Student ID</td><td>${s.studentId}</td></tr>
    <tr><td>Programme</td><td>${certEnrollment.value?.programme ?? ''}</td></tr>
    <tr><td>Specialization</td><td>${certEnrollment.value?.specialization ?? ''}</td></tr>
    <tr><td>Partner Institution</td><td>${s.partner}</td></tr>
    <tr><td>Commencement Date</td><td>${fmtDate(certEnrollment.value?.commencementDate)}</td></tr>
  </table>
  <h4>Subject Results</h4>
  <table class="grade-table">
    <thead><tr><th>Subject</th><th>Credits</th><th>IBSS</th><th>UK</th><th>ECTS</th><th>ECTS Pts</th><th>Grade Pts</th></tr></thead>
    <tbody>${rows}</tbody>
    <tfoot>
      <tr class="total-row"><td><strong>Total</strong></td><td class="tc">${totalCr}</td><td class="tc" colspan="4"></td><td class="tc gp">${totalGP.toFixed(1)}</td></tr>
      <tr class="gpa-row"><td colspan="6" class="gpa-label">Grade Point Average (GPA)</td><td class="tc gp">${gpa}</td></tr>
    </tfoot>
  </table>
  <div class="stamp"><div class="stamp-text">APPROVED ✓</div><div class="stamp-date">${todayFormatted}</div></div>
  <div class="sign"><p>Certified by,</p><div class="sig-line"></div><p class="sig-name">IBSS Academic Registry</p><p class="sig-org">International Business School of Scandinavia</p></div>
  <script>window.onload=function(){window.print()}<\/script></body></html>`

  const win = window.open('', '_blank', 'width=820,height=680')
  win.document.write(html)
  win.document.close()
}

// ── Student Edit Panel ────────────────────────────────────────────────────────
const showStudentEdit  = ref(false)
const editingStudent   = ref(null)
const studentEditSaved = ref(false)
const studentEditForm  = reactive({ email: '', address: '', highestDegree: '', languageResult: '', yearsWorkExperience: 0 })

function openStudentEdit(s) {
  editingStudent.value = s
  Object.assign(studentEditForm, {
    email: s.email, address: s.address,
    highestDegree: s.highestDegree, languageResult: s.languageResult,
    yearsWorkExperience: s.yearsWorkExperience,
  })
  studentEditSaved.value = false
  showStudentEdit.value  = true
}
function saveStudentEdit() {
  const s = editingStudent.value
  s.email = studentEditForm.email
  s.address = studentEditForm.address
  s.highestDegree = studentEditForm.highestDegree
  s.languageResult = studentEditForm.languageResult
  s.yearsWorkExperience = studentEditForm.yearsWorkExperience
  studentEditSaved.value = true
  setTimeout(() => { showStudentEdit.value = false; studentEditSaved.value = false }, 1200)
}

// ── Enrollment Edit Panel ─────────────────────────────────────────────────────
const showEnrEdit       = ref(false)
const editingEnrollment = ref(null)
const editingEnrStudent = ref(null)
const enrEditTab        = ref('grades')
const saveSuccess       = ref(false)
const gradeRows         = ref([])
const dropRequest       = reactive({ status: '', reason: '' })
const dropSubmitted     = ref(false)

function openEnrEdit(student, enr) {
  editingEnrStudent.value = student
  editingEnrollment.value = enr
  enrEditTab.value        = 'grades'
  saveSuccess.value       = false
  dropSubmitted.value     = false
  dropRequest.status      = ''
  dropRequest.reason      = ''
  // Load grades
  const modules = resolveSubjects(auth.user?.name, enr.programme, enr.specialization)
  const saved   = gradesStore[student.studentId] ?? {}
  gradeRows.value = modules.map(mod => {
    const ex = saved[mod.name]
    return reactive({ name: mod.name, credits: mod.ects ?? mod.credits ?? 15, ibssGrade: ex?.ibssGrade ?? '', ukGrade: ex?.ukGrade ?? null, ectsGrade: ex?.ectsGrade ?? null, ectsPoints: ex?.ectsPoints ?? null, gradePoints: ex?.gradePoints ?? null, remark: ex?.remark ?? null })
  })
  showEnrEdit.value = true
}

function recalcRow(row) {
  const info = getGradeInfo(row.ibssGrade)
  if (info) {
    row.ukGrade = info.ukGrade; row.ectsGrade = info.ectsGrade
    row.ectsPoints = info.ectsPoints
    row.gradePoints = +(row.credits * info.ectsPoints).toFixed(2)
    row.remark = info.remark
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
    gradeMap[row.name] = { ibssGrade: row.ibssGrade, ukGrade: row.ukGrade, ectsGrade: row.ectsGrade, ectsPoints: row.ectsPoints, gradePoints: row.gradePoints, remark: row.remark, credits: row.credits }
  }
  saveGrades(editingEnrStudent.value.studentId, gradeMap)
  saveSuccess.value = true
  setTimeout(() => { showEnrEdit.value = false; saveSuccess.value = false }, 1500)
}

function submitDropRequest() {
  const enr = editingEnrollment.value
  enr.changeNotes.push({
    id: Date.now(),
    text: dropRequest.reason,
    requestedChange: dropRequest.status,
    date: new Date().toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }),
  })
  dropSubmitted.value = true
  dropRequest.status = ''
  dropRequest.reason = ''
}

// ── Absences & tickets for partner's students ─────────────────────────────────
const myStudentIds = computed(() => myStudents.value.map(s => s.studentId))
const myAbsences   = computed(() => absences.filter(a => myStudentIds.value.includes(a.studentId)).slice().reverse())
const myTickets    = computed(() => tickets.filter(t => myStudentIds.value.includes(t.studentId)).slice().reverse())

function hasPendingAbsence(sid) {
  return absences.some(a => a.studentId === sid && a.status === 'pending')
}
function studentName(sid) {
  const s = students.find(s => s.studentId === sid)
  return s ? `${s.firstName} ${s.lastName}` : sid
}

const expandedPartnerTickets = ref([])
const partnerReplyTexts = reactive({})
function togglePartnerTicket(id) {
  const i = expandedPartnerTickets.value.indexOf(id)
  if (i === -1) expandedPartnerTickets.value.push(id)
  else expandedPartnerTickets.value.splice(i, 1)
}
function partnerReply(t) {
  const text = partnerReplyTexts[t.id]
  if (!text) return
  t.replies.push({ from: 'partner', text, at: new Date().toISOString() })
  partnerReplyTexts[t.id] = ''
}

function logout() { auth.logout(); router.push('/login') }
</script>

<style scoped>
.page-wrapper { min-height: 100vh; background: #f2f5f9; }

/* + Add Student modal — hosts /apply wizard in an iframe */
.btn-add-student { background: #1a5276; color: #fff; border: 0; padding: .55rem 1rem;
  border-radius: 6px; font-weight: 600; cursor: pointer; font-size: .88rem; }
.btn-add-student:hover:not(:disabled) { background: #133e58; }
.btn-add-student:disabled { opacity: .5; cursor: not-allowed; }
.add-student-backdrop { position: fixed; inset: 0; background: rgba(15,23,42,.55); z-index: 2000; }
.add-student-modal { position: fixed; top: 4vh; left: 50%; transform: translateX(-50%);
  width: min(960px, 95vw); height: 92vh; background: #fff; border-radius: 10px;
  box-shadow: 0 20px 60px rgba(0,0,0,.3); z-index: 2001;
  display: flex; flex-direction: column; overflow: hidden; }
.add-student-head { display: flex; align-items: center; justify-content: space-between;
  padding: .85rem 1.25rem; border-bottom: 1px solid #e5eaf1; background: #fafbfc; }
.add-student-head h3 { margin: 0; color: #003366; font-size: 1rem; }
.btn-close-modal { background: transparent; border: 1px solid #d0d7e0; border-radius: 5px;
  padding: .3rem .65rem; cursor: pointer; font-size: .85rem; color: #555; }
.btn-close-modal:hover { background: #f0f3f7; }
.add-student-iframe { flex: 1; border: 0; width: 100%; }

.navbar { background: #1a5276; color: #fff; display: flex; align-items: center; justify-content: space-between; padding: 0.85rem 2rem; }
.brand-text { font-size: 1.05rem; font-weight: 700; }
.nav-right { display: flex; align-items: center; gap: 1rem; }
.btn-logout { background: transparent; border: 1.5px solid rgba(255,255,255,0.55); color: #fff; padding: 0.3rem 0.85rem; border-radius: 5px; cursor: pointer; font-size: 0.82rem; }
.btn-logout:hover { background: rgba(255,255,255,0.13); }
.btn-nav-link { text-decoration: none; display: inline-flex; align-items: center; }

.container { max-width: 1200px; margin: 2rem auto; padding: 0 1.5rem; }
.page-header { display: flex; align-items: flex-start; justify-content: space-between; margin-bottom: 1.25rem; }
.page-title { font-size: 1.5rem; font-weight: 700; color: #1a5276; }
.page-sub { font-size: 0.82rem; color: #888; margin-top: 0.2rem; }

.btn-primary { background: #1a5276; color: #fff; border: none; border-radius: 7px; padding: 0.6rem 1.25rem; font-size: 0.9rem; font-weight: 600; cursor: pointer; }
.btn-primary:hover { background: #2471a3; }

/* Table */
.table-wrap { background: #fff; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.07); overflow: auto; }
.students-table { width: 100%; border-collapse: collapse; font-size: 0.88rem; }
.students-table th { text-align: left; padding: 0.75rem 1rem; font-size: 0.74rem; text-transform: uppercase; letter-spacing: 0.05em; color: #666; border-bottom: 2px solid #e8edf4; background: #fafbfc; white-space: nowrap; }
.data-row td { padding: 0.7rem 1rem; border-bottom: 1px solid #f0f3f7; vertical-align: middle; }
.data-row:last-child td { border-bottom: none; }
.data-row:hover td { background: #f7f9fb; }
.mono { font-family: ui-monospace, monospace; font-size: 0.82rem; color: #555; }
.empty-row { text-align: center; padding: 2.5rem !important; color: #aaa; font-style: italic; }

/* Absence badge on student row */
.abs-badge { margin-left: .4rem; background: #fff3cd; color: #856404; border: 1px solid #fcd34d; padding: 1px 7px; border-radius: 10px; font-size: .7rem; font-weight: 700; }

/* Review Application button + reviewed badge */
.btn-review-app { background: #003366; color: #fff; border: none; border-radius: 5px; padding: 0.3rem 0.75rem; font-size: 0.76rem; font-weight: 600; cursor: pointer; }
.btn-review-app:hover { background: #0055a5; }
.reviewed-badge { background: #d1fae5; color: #065f46; border: 1px solid #6ee7b7; padding: 2px 8px; border-radius: 12px; font-size: 0.72rem; font-weight: 700; }

/* Review-completed toast */
.review-toast { position: fixed; bottom: 2rem; left: 50%; transform: translateX(-50%); background: #0d6b55; color: #fff; padding: 0.75rem 1.5rem; border-radius: 8px; font-size: 0.88rem; box-shadow: 0 4px 18px rgba(0,0,0,0.22); z-index: 500; }

/* Partner sections (absences + tickets) */
.partner-section { margin-top: 1.75rem; }
.partner-section-title { font-size: 1rem; font-weight: 700; color: #1a5276; margin-bottom: .65rem; }
.partner-card { background: #fff; border-radius: 9px; box-shadow: 0 2px 8px rgba(0,0,0,.06); overflow: hidden; margin-bottom: .6rem; }
.partner-tbl { width: 100%; border-collapse: collapse; font-size: .87rem; }
.partner-tbl th { text-align: left; padding: .65rem 1rem; font-size: .73rem; text-transform: uppercase; letter-spacing: .04em; color: #666; border-bottom: 2px solid #e8edf4; background: #fafbfc; }
.partner-tbl td { padding: .6rem 1rem; border-bottom: 1px solid #f0f3f7; }
.ticket-meta-p { display: flex; align-items: center; gap: .65rem; padding: .8rem 1.1rem; cursor: pointer; background: #fafbfc; }
.ticket-meta-p:hover { background: #f0f4f8; }
.ticket-subj-p { flex: 1; font-weight: 600; font-size: .9rem; }
.ticket-thread-p { padding: .75rem 1.1rem; display: flex; flex-direction: column; gap: .5rem; }
.reply-p { padding: .5rem .8rem; border-radius: 7px; font-size: .87rem; }
.reply-p-student { background: #e8f0fe; border-left: 3px solid #4a90d9; }
.reply-p-admin   { background: #e8f6e9; border-left: 3px solid #2d9e53; }
.reply-p-partner { background: #fff8e6; border-left: 3px solid #e6a817; }
.reply-from-p { font-weight: 700; margin-right: .4rem; font-size: .82rem; }
.reply-p p { margin: .25rem 0 0; }
.partner-reply-box { display: flex; flex-direction: column; gap: .45rem; margin-top: .5rem; }
.partner-reply-box textarea { padding: .5rem .7rem; border: 1.5px solid #ccc; border-radius: 6px; font-size: .88rem; resize: vertical; font-family: inherit; }

/* Status badges */
.badge-new        { background: #f0f3f7; color: #888; border: 1px solid #d0d7e0; padding: 2px 10px; border-radius: 20px; font-size: 0.76rem; font-weight: 600; white-space: nowrap; }
.badge-offer      { background: #e8f4fd; color: #1a6ca8; border: 1px solid #b8d9f5; padding: 2px 10px; border-radius: 20px; font-size: 0.76rem; font-weight: 600; white-space: nowrap; }
.badge-admission  { background: #dbeafe; color: #1d4ed8; border: 1px solid #93c5fd; padding: 2px 10px; border-radius: 20px; font-size: 0.76rem; font-weight: 600; white-space: nowrap; }
.badge-confirmed  { background: #fef3c7; color: #92400e; border: 1px solid #fcd34d; padding: 2px 10px; border-radius: 20px; font-size: 0.76rem; font-weight: 600; white-space: nowrap; }
.badge-graded     { background: #e0f5f0; color: #0d6b55; border: 1px solid #a8ddd0; padding: 2px 10px; border-radius: 20px; font-size: 0.76rem; font-weight: 600; white-space: nowrap; }
.badge-approved   { background: #d1fae5; color: #065f46; border: 1px solid #6ee7b7; padding: 2px 10px; border-radius: 20px; font-size: 0.76rem; font-weight: 600; white-space: nowrap; }

/* Action buttons */
.actions-cell { display: flex; gap: 0.4rem; flex-wrap: wrap; align-items: center; }
.btn-act { border: none; border-radius: 5px; padding: 0.28rem 0.7rem; font-size: 0.78rem; cursor: pointer; white-space: nowrap; font-weight: 500; }
.btn-act:disabled { opacity: 0.38; cursor: default; }
.btn-offer     { background: #e8f4fd; color: #1a6ca8; border: 1px solid #b8d9f5; }
.btn-offer:hover:not(:disabled) { background: #cce8f8; }
.btn-admission { background: #dbeafe; color: #1d4ed8; border: 1px solid #93c5fd; }
.btn-admission:hover:not(:disabled) { background: #bfdbfe; }
.btn-grade     { background: #1a5276; color: #fff; }
.btn-grade:hover:not(:disabled) { background: #2471a3; }
.btn-confirm   { background: #fef3c7; color: #92400e; border: 1px solid #fcd34d; }
.btn-confirm:hover:not(:disabled) { background: #fde68a; }
.confirmed-chip { font-size: 0.76rem; font-weight: 600; color: #065f46; white-space: nowrap; padding: 0.28rem 0; }
.btn-cert      { background: #d1fae5; color: #065f46; border: 1px solid #6ee7b7; }
.btn-cert:hover:not(:disabled) { background: #a7f3d0; }

/* Drawers */
.drawer-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.4); z-index: 100; }
.drawer { position: fixed; top: 0; right: 0; bottom: 0; width: 820px; max-width: 96vw; background: #fff; z-index: 101; display: flex; flex-direction: column; box-shadow: -4px 0 24px rgba(0,0,0,0.15); }
.drawer-narrow { width: 440px; }
.drawer-wide { width: 640px; }
.drawer-header { display: flex; align-items: flex-start; justify-content: space-between; padding: 1.25rem 1.5rem; border-bottom: 1.5px solid #e8edf4; flex-shrink: 0; }
.drawer-header h2 { font-size: 1.1rem; font-weight: 700; color: #1a5276; }
.drawer-sub { font-size: 0.82rem; color: #888; margin-top: 0.2rem; }
.drawer-close { background: none; border: none; font-size: 1.1rem; color: #888; cursor: pointer; padding: 0.2rem; }
.drawer-close:hover { color: #333; }
.drawer-form { flex: 1; overflow-y: auto; padding: 1.2rem 1.5rem; display: flex; flex-direction: column; gap: 0.9rem; }
.drawer-actions { display: flex; gap: 0.75rem; justify-content: flex-end; padding: 1rem 1.5rem; border-top: 1px solid #e8edf4; flex-shrink: 0; }
.btn-cancel { padding: 0.65rem 1.2rem; background: #f2f5f9; border: 1.5px solid #ccc; border-radius: 7px; font-size: 0.9rem; cursor: pointer; color: #555; }
.btn-save   { padding: 0.65rem 1.4rem; background: #1a5276; color: #fff; border: none; border-radius: 7px; font-size: 0.9rem; font-weight: 600; cursor: pointer; }
.btn-save:hover { background: #2471a3; }

/* Add student form fields */
.row-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 0.75rem; }
.field { display: flex; flex-direction: column; gap: 0.3rem; }
.field label { font-size: 0.82rem; font-weight: 600; color: #444; }
.req { color: #c0392b; }
.field input, .field select { padding: 0.58rem 0.75rem; border: 1.5px solid #ccc; border-radius: 7px; font-size: 0.9rem; font-family: inherit; outline: none; }
.field input:focus, .field select:focus { border-color: #1a5276; }
.input-locked { background: #f5f5f5; color: #666; cursor: not-allowed; }
.success-msg { margin: 0.75rem 1.5rem 0; background: #eafaf1; border: 1.5px solid #2ecc71; border-radius: 7px; padding: 0.65rem 1rem; color: #1e8449; font-size: 0.88rem; flex-shrink: 0; }

/* Modal */
.modal-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.45); z-index: 100; }
.modal { position: fixed; top: 50%; left: 50%; transform: translate(-50%, -50%); width: 680px; max-width: 96vw; max-height: 90vh; background: #fff; z-index: 101; border-radius: 12px; box-shadow: 0 12px 48px rgba(0,0,0,0.22); display: flex; flex-direction: column; }
.modal-wide { width: 860px; }
.modal-header { display: flex; align-items: center; justify-content: space-between; padding: 1.1rem 1.5rem; border-bottom: 1.5px solid #e8edf4; flex-shrink: 0; }
.modal-header h2 { font-size: 1.05rem; font-weight: 700; color: #1a5276; }
.modal-header-actions { display: flex; align-items: center; gap: 0.75rem; }
.btn-print { background: #1a5276; color: #fff; border: none; border-radius: 6px; padding: 0.38rem 0.95rem; font-size: 0.84rem; font-weight: 600; cursor: pointer; }
.btn-print:hover { background: #2471a3; }
.modal-body { flex: 1; overflow-y: auto; padding: 1.5rem; }

/* Letter preview */
.letter-sheet { background: #fff; border: 1px solid #e0e8f0; border-radius: 6px; padding: 2rem 2.25rem; max-width: 580px; margin: 0 auto; font-size: 0.9rem; line-height: 1.7; }
.letter-header-block { display: flex; align-items: flex-start; gap: 1rem; margin-bottom: 0.75rem; }
.letter-logo-text { font-size: 2rem; font-weight: 900; color: #003366; letter-spacing: -1px; flex-shrink: 0; line-height: 1; }
.letter-org { font-size: 0.82rem; }
.letter-org strong { color: #003366; font-size: 0.9rem; }
.letter-org-sub { color: #777; font-size: 0.78rem; }
.letter-rule { border: none; border-top: 2px solid #003366; margin: 0.75rem 0 1rem; }
.letter-date { font-size: 0.83rem; color: #666; margin-bottom: 1rem; }
.letter-type-heading { font-size: 1rem; font-weight: 700; letter-spacing: 1px; color: #003366; margin-bottom: 1rem; }
.letter-dear { margin-bottom: 0.75rem; }
.letter-body-text { text-align: justify; color: #333; margin-bottom: 0.75rem; }
.letter-details-table { width: 100%; border-collapse: collapse; margin: 1rem 0; font-size: 0.84rem; }
.ldt-label { width: 38%; font-weight: 600; color: #666; padding: 5px 8px 5px 0; border-bottom: 1px solid #eee; font-size: 0.8rem; text-transform: uppercase; letter-spacing: 0.03em; }
.ldt-val { padding: 5px 0; border-bottom: 1px solid #eee; color: #222; }
.letter-sign { margin-top: 1.5rem; font-size: 0.88rem; }
.letter-sig-line { border-top: 1px solid #999; width: 160px; margin: 1.5rem 0 0.4rem; }
.letter-sig-name { font-weight: 700; margin: 0; }
.letter-sig-org { color: #666; font-size: 0.82rem; margin: 2px 0; }

/* Certificate in modal */
.cert-section-title { font-size: 0.82rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.06em; color: #003366; margin: 0 0 0.6rem; }
.cert-grade-table { width: 100%; border-collapse: collapse; font-size: 0.84rem; margin-bottom: 1.25rem; }
.cert-grade-table th { background: #003366; color: #fff; padding: 0.5rem 0.75rem; text-align: center; font-size: 0.74rem; text-transform: uppercase; letter-spacing: 0.04em; }
.cert-grade-table th:first-child { text-align: left; }
.cert-grade-table td { padding: 0.45rem 0.75rem; border-bottom: 1px solid #e8edf4; }
.tc { text-align: center; }
.cert-gp { background: #eaf0f8; font-weight: 600; color: #003366; }
.cert-total-row td { background: #f0f3f7; font-size: 0.88rem; border-top: 2px solid #ccd; }
.cert-gpa-row td { background: #e8f0f8; }
.cert-gpa-label { text-align: right; font-size: 0.82rem; color: #555; padding-right: 0.75rem; font-style: italic; }
.cert-gpa-val { font-size: 0.95rem; }
.cert-approved-stamp { display: inline-block; border: 3px double #0d6b55; padding: 0.5rem 1rem; margin-top: 1.25rem; }
.cert-stamp-text { font-size: 1.1rem; font-weight: 900; color: #0d6b55; letter-spacing: 2px; }
.cert-stamp-date { font-size: 0.78rem; color: #555; margin-top: 2px; }

/* Grade drawer internals */
.student-info-strip { display: flex; gap: 1.5rem; flex-wrap: wrap; background: #f7f9fb; padding: 0.7rem 1.5rem; font-size: 0.83rem; color: #444; border-bottom: 1px solid #e8edf4; flex-shrink: 0; }
.grade-table-wrap { flex: 1; overflow: auto; padding: 1.25rem 1.5rem 0; }
.grade-table { width: 100%; border-collapse: collapse; font-size: 0.85rem; }
.grade-table th { text-align: center; padding: 0.55rem 0.6rem; font-size: 0.72rem; text-transform: uppercase; letter-spacing: 0.04em; color: #555; background: #f2f5f9; border: 1px solid #e0e7ef; white-space: nowrap; line-height: 1.3; }
.grade-table th:first-child { text-align: left; }
.grade-table td { padding: 0.55rem 0.6rem; border: 1px solid #e8edf4; vertical-align: middle; }
.grade-table tbody tr:hover td { background: #f9fbfd; }
.num-col { text-align: center; width: 72px; }
.calc-cell { color: #1a5276; font-weight: 600; background: #f7fbff; }
.highlight { background: #eaf4ff !important; color: #003366; font-weight: 700; }
.remark-cell { font-size: 0.78rem; color: #555; min-width: 220px; }
.grade-input { width: 62px; padding: 0.3rem 0.35rem; text-align: center; border: 1.5px solid #ccc; border-radius: 5px; font-size: 0.88rem; outline: none; }
.grade-input:focus { border-color: #1a5276; }
.total-row td { background: #f2f5f9; font-size: 0.88rem; border-top: 2px solid #d0dbe8; }
.gpa-row td { background: #eaf0f8; }
.gpa-label { text-align: right; font-size: 0.82rem; color: #555; padding-right: 0.8rem; }
.gpa-val { font-size: 1rem; color: #003366; }

/* Wizard — upload section */
.upload-section { border: 1.5px dashed #c5d8f0; border-radius: 8px; padding: 1rem 1.25rem; }
.upload-title { font-size: 0.85rem; font-weight: 700; color: #1a5276; margin: 0 0 0.75rem; text-transform: uppercase; letter-spacing: 0.04em; }
.upload-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 0.75rem; }
.upload-item { display: flex; flex-direction: column; gap: 0.3rem; }
.upload-label { font-size: 0.8rem; font-weight: 600; color: #444; }
.file-input { font-size: 0.8rem; }
.file-name { font-size: 0.76rem; color: #0d6b55; background: #e0f5f0; border-radius: 4px; padding: 2px 7px; margin-top: 2px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }

/* Wizard — section headings */
.section-heading { font-size: 0.88rem; font-weight: 700; color: #1a5276; margin: 0 0 0.5rem; text-transform: uppercase; letter-spacing: 0.04em; border-bottom: 1.5px solid #e8edf4; padding-bottom: 0.4rem; }
.hint-text { font-size: 0.8rem; color: #888; margin: 0 0 0.75rem; }

/* Wizard — doc checkboxes */
.check-list { display: flex; flex-direction: column; gap: 0.5rem; }
.check-item { display: flex; align-items: center; gap: 0.6rem; font-size: 0.88rem; cursor: pointer; padding: 0.45rem 0.75rem; border: 1px solid #e8edf4; border-radius: 7px; background: #fafbfc; }
.check-item input[type=checkbox] { width: 15px; height: 15px; cursor: pointer; flex-shrink: 0; }
.file-chip { margin-left: auto; font-size: 0.74rem; color: #0d6b55; background: #e0f5f0; border: 1px solid #a8ddd0; border-radius: 4px; padding: 1px 8px; max-width: 220px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }

/* Wizard — pathway radios */
.pathway-list { display: flex; flex-direction: column; gap: 0.5rem; }
.pathway-item { display: flex; align-items: flex-start; gap: 0.7rem; font-size: 0.88rem; cursor: pointer; padding: 0.55rem 0.75rem; border: 1.5px solid #e8edf4; border-radius: 7px; background: #fafbfc; transition: border-color 0.15s; }
.pathway-item:has(input:checked) { border-color: #1a5276; background: #eef4fb; }
.pathway-item input[type=radio] { width: 15px; height: 15px; cursor: pointer; flex-shrink: 0; margin-top: 2px; }

/* Wizard — summary card */
.summary-card { background: #f7fbff; border: 1.5px solid #c5d8f0; border-radius: 9px; padding: 1rem 1.25rem; }
.summary-title { font-size: 0.85rem; font-weight: 700; color: #1a5276; margin: 0 0 0.75rem; text-transform: uppercase; letter-spacing: 0.04em; }
.summary-row { display: flex; gap: 0.75rem; font-size: 0.88rem; padding: 0.3rem 0; border-bottom: 1px solid #e0eaf4; align-items: flex-start; }
.summary-row:last-child { border-bottom: none; }
.summary-label { width: 120px; flex-shrink: 0; font-weight: 600; color: #555; font-size: 0.82rem; }
.doc-chip { display: inline-block; background: #e0f5f0; color: #0d6b55; border: 1px solid #a8ddd0; border-radius: 4px; padding: 1px 8px; font-size: 0.74rem; font-weight: 600; margin: 2px 3px 2px 0; }

/* Wizard — offer type */
.offer-type-list { display: flex; flex-direction: column; gap: 0.65rem; }
.offer-type-item { display: flex; align-items: flex-start; gap: 0.85rem; padding: 0.85rem 1rem; border: 1.5px solid #e8edf4; border-radius: 9px; cursor: pointer; background: #fafbfc; transition: border-color 0.15s; }
.offer-type-item:has(input:checked) { border-color: #1a5276; background: #eef4fb; }
.offer-type-item input[type=radio] { width: 16px; height: 16px; flex-shrink: 0; margin-top: 3px; cursor: pointer; }
.offer-type-item strong { font-size: 0.9rem; color: #1a5276; }
.offer-type-desc { font-size: 0.8rem; color: #666; margin: 2px 0 0; }

/* ── Student cards ─────────────────────────────────────────────────────────── */
.empty-state-card { background: #fff; border-radius: 10px; padding: 2.5rem; text-align: center; color: #aaa; font-style: italic; margin-bottom: 1rem; }
.student-card { background: #fff; border-radius: 12px; box-shadow: 0 2px 10px rgba(0,0,0,.07); margin-bottom: 1.5rem; overflow: hidden; }

.sc-header { display: flex; align-items: center; justify-content: space-between; padding: .9rem 1.25rem; background: #003366; }
.sc-id-name { display: flex; align-items: center; gap: .6rem; flex-wrap: wrap; }
.sc-sid  { font-family: ui-monospace, monospace; font-size: .88rem; color: #a8c8ff; font-weight: 600; }
.sc-sep  { color: rgba(255,255,255,.4); }
.sc-name { font-size: 1rem; font-weight: 700; color: #fff; }
.sc-header-right { display: flex; align-items: center; gap: .5rem; }

.sc-body { overflow-x: auto; }
.enr-table { width: 100%; border-collapse: collapse; font-size: .85rem; min-width: 900px; }
.enr-table thead th { padding: .6rem .9rem; text-align: left; font-size: .72rem; text-transform: uppercase; letter-spacing: .05em; color: #666; border-bottom: 2px solid #e8edf4; background: #fafbfc; white-space: nowrap; }
.th-prog   { width: 20%; }
.th-status { width: 13%; }
.th-acad   { width: 16%; }
.th-pay    { width: 13%; }
.th-notes  { width: 25%; }
.th-edit   { width: 7%; }

.enr-row td { padding: .85rem .9rem; border-bottom: 1px solid #f0f3f7; vertical-align: top; }
.enr-row:last-child td { border-bottom: none; }
.enr-row:hover td { background: #fafcff; }

/* Col 1 */
.prog-name-main { font-weight: 700; color: #1a1a2e; font-size: .88rem; line-height: 1.3; }
.prog-specialization-sub { font-size: .8rem; color: #666; margin: .15rem 0 .55rem; }
.enr-actions { display: flex; flex-wrap: wrap; gap: .3rem; margin-top: .4rem; }

/* Col 2 */
.enr-status-badge { display: inline-block; padding: .25rem .7rem; border-radius: 20px; font-size: .75rem; font-weight: 700; white-space: nowrap; }
.es-active    { background: #d1fae5; color: #065f46; border: 1px solid #6ee7b7; }
.es-graduated { background: #dbeafe; color: #1e40af; border: 1px solid #93c5fd; }
.es-potential { background: #fef3c7; color: #92400e; border: 1px solid #fcd34d; }
.es-deferred  { background: #e0e7ff; color: #3730a3; border: 1px solid #a5b4fc; }
.es-inactive  { background: #fee2e2; color: #991b1b; border: 1px solid #fca5a5; }
.es-default   { background: #f0f3f7; color: #555; border: 1px solid #d0d7e0; }
.status-note  { font-size: .7rem; color: #aaa; margin-top: .3rem; font-style: italic; }

/* Col 3 */
.ap-row { display: flex; gap: .35rem; align-items: baseline; margin-bottom: .22rem; font-size: .82rem; }
.ap-lbl { color: #888; font-size: .76rem; min-width: 80px; }
.marks-link { color: #0055a5; text-decoration: none; font-size: .8rem; }
.marks-link:hover { text-decoration: underline; }
.rel-yes { color: #065f46; font-weight: 600; }
.rel-no  { color: #aaa; }
.fp-done    { color: #065f46; font-weight: 600; }
.fp-doing   { color: #92400e; font-weight: 600; }
.fp-notdone { color: #991b1b; }
.fp-na      { color: #aaa; }

/* Col 4 */
.pay-section { }
.pay-chip { display: inline-block; padding: .2rem .6rem; border-radius: 10px; font-size: .75rem; font-weight: 600; margin-top: .15rem; }
.pay-green { background: #d1fae5; color: #065f46; border: 1px solid #6ee7b7; }
.pay-amber { background: #fef3c7; color: #92400e; border: 1px solid #fcd34d; }
.pay-red   { background: #fee2e2; color: #991b1b; border: 1px solid #fca5a5; }
.pay-na    { background: #f0f3f7; color: #888; border: 1px solid #d0d7e0; }

/* Col 5 */
.notes-list { margin-bottom: .6rem; display: flex; flex-direction: column; gap: .45rem; }
.note-entry { background: #f7f9fc; border-left: 3px solid #003366; border-radius: 0 5px 5px 0; padding: .4rem .6rem; }
.note-meta  { display: flex; align-items: center; gap: .4rem; margin-bottom: .15rem; }
.note-arrow { color: #003366; font-weight: 700; font-size: .85rem; }
.note-req   { font-size: .78rem; font-weight: 700; color: #003366; }
.note-date  { font-size: .72rem; color: #888; margin-left: auto; }
.note-text  { font-size: .8rem; color: #444; }
.note-form  { display: flex; flex-direction: column; gap: .35rem; }
.note-ta    { padding: .45rem .6rem; border: 1.5px solid #ccc; border-radius: 6px; font-size: .82rem; font-family: inherit; resize: vertical; }
.note-ta:focus { border-color: #003366; outline: none; }
.note-sel   { padding: .4rem .6rem; border: 1.5px solid #ccc; border-radius: 6px; font-size: .82rem; background: #fff; }
.btn-add-note { padding: .35rem .8rem; background: #003366; color: #fff; border: none; border-radius: 5px; font-size: .8rem; cursor: pointer; align-self: flex-start; }
.btn-add-note:disabled { background: #aaa; cursor: not-allowed; }

/* Card footer */
.sc-footer { padding: .65rem 1.1rem; background: #f8f9fa; border-top: 1px solid #eee; }
.btn-add-enr { padding: .38rem .9rem; background: transparent; border: 1.5px dashed #0055a5; color: #0055a5; border-radius: 6px; font-size: .82rem; cursor: pointer; font-weight: 600; }
.btn-add-enr:hover { background: #e8f0f8; }

/* Filter bar / search */
.filter-bar { display: flex; align-items: center; gap: .65rem; margin-bottom: 1rem; flex-wrap: wrap; }
.search-wrap { position: relative; flex: 1; min-width: 200px; max-width: 320px; display: flex; align-items: center; }
.search-icon { position: absolute; left: .7rem; font-size: .95rem; pointer-events: none; color: #aaa; }
.search-input { width: 100%; padding: .48rem .7rem .48rem 2.1rem; border: 1.5px solid #d0dbe8; border-radius: 8px; font-size: .88rem; font-family: inherit; outline: none; background: #fff; color: #222; }
.search-input:focus { border-color: #1a5276; box-shadow: 0 0 0 3px rgba(26,82,118,.08); }
.search-clear { position: absolute; right: .5rem; background: none; border: none; color: #aaa; font-size: .9rem; cursor: pointer; padding: .2rem .3rem; line-height: 1; }
.search-clear:hover { color: #555; }
.search-hint-wrap { position: relative; display: flex; align-items: center; flex-shrink: 0; }
.search-hint-icon { width: 18px; height: 18px; border-radius: 50%; background: #d0dbe8; color: #555; font-size: .72rem; font-weight: 700; display: flex; align-items: center; justify-content: center; cursor: default; user-select: none; flex-shrink: 0; }
.search-hint-wrap:hover .search-hint-tip { display: block; }
.search-hint-tip { display: none; position: absolute; top: calc(100% + 7px); left: 50%; transform: translateX(-50%); background: #1e2d3d; color: #eee; font-size: .78rem; line-height: 1.6; padding: .65rem .85rem; border-radius: 8px; white-space: nowrap; box-shadow: 0 4px 16px rgba(0,0,0,.22); z-index: 300; pointer-events: none; }
.search-hint-tip::before { content: ''; position: absolute; bottom: 100%; left: 50%; transform: translateX(-50%); border: 5px solid transparent; border-bottom-color: #1e2d3d; }
.search-hint-tip strong { color: #fff; }
.search-hint-tip code { background: rgba(255,255,255,.15); border-radius: 3px; padding: 0 4px; font-family: ui-monospace, monospace; color: #adf; }
.search-hint-tip em { color: #ccc; font-style: normal; }
.btn-clear-filter { background: none; border: 1.5px solid #e0a8a8; color: #991b1b; border-radius: 6px; padding: .38rem .75rem; font-size: .8rem; cursor: pointer; font-weight: 600; white-space: nowrap; }
.btn-clear-filter:hover { background: #fee2e2; }
.filter-count { margin-left: auto; font-size: .8rem; color: #999; white-space: nowrap; }

/* Multi-select dropdown */
.ms-wrap { position: relative; }
.ms-btn { display: flex; align-items: center; gap: .4rem; background: #fff; border: 1.5px solid #d0dbe8; color: #333; border-radius: 7px; padding: .42rem .85rem; font-size: .85rem; cursor: pointer; white-space: nowrap; font-family: inherit; min-width: 145px; justify-content: space-between; }
.ms-btn:hover { border-color: #1a5276; background: #f7fbff; }
.ms-btn-active { border-color: #1a5276; color: #1a5276; font-weight: 700; background: #eef4fb; }
.ms-caret { font-size: .9rem; color: #888; }
.ms-dropdown { position: absolute; top: calc(100% + 4px); left: 0; min-width: 230px; background: #fff; border: 1.5px solid #d0dbe8; border-radius: 9px; box-shadow: 0 6px 22px rgba(0,0,0,.12); z-index: 200; padding: .45rem 0 .4rem; max-height: 260px; overflow-y: auto; }
.ms-item { display: flex; align-items: center; gap: .6rem; padding: .42rem .85rem; cursor: pointer; font-size: .86rem; color: #333; }
.ms-item:hover { background: #f0f5fb; }
.ms-item input[type=checkbox] { width: 15px; height: 15px; cursor: pointer; accent-color: #1a5276; flex-shrink: 0; }
.ms-clear { display: block; width: calc(100% - 1.7rem); margin: .4rem .85rem 0; background: none; border: 1px solid #e0a8a8; color: #991b1b; border-radius: 5px; padding: .3rem 0; font-size: .78rem; cursor: pointer; font-weight: 600; }
.ms-clear:hover { background: #fee2e2; }

/* Collapse chevron + summary */
.sc-chevron { font-size: 1.3rem; color: rgba(255,255,255,.65); transition: transform .2s; line-height: 1; margin-left: .25rem; display: inline-block; }
.sc-chevron.collapsed { transform: rotate(-90deg); }
.sc-summary { font-size: .78rem; color: rgba(255,255,255,.7); font-weight: 500; white-space: nowrap; }

/* Enrollment edit button */
.td-edit { vertical-align: top; text-align: right; }
.btn-edit-enr { background: #eef4fb; border: 1.5px solid #b8d9f5; color: #1a5276; padding: .28rem .7rem; border-radius: 5px; cursor: pointer; font-size: .8rem; font-weight: 600; white-space: nowrap; }
.btn-edit-enr:hover { background: #cce8f8; }

/* Document list in col 1 */
.doc-list { margin-top: .55rem; display: flex; flex-direction: column; gap: .18rem; }
.doc-row { display: flex; align-items: center; gap: .35rem; font-size: .79rem; padding: .22rem .3rem; border-radius: 4px; text-decoration: none; transition: background .12s; }
.doc-row.doc-avail { color: #0055a5; }
.doc-row.doc-avail:hover { background: #eef4fb; text-decoration: underline; }
.doc-row.doc-disabled { color: #bbb; cursor: default; pointer-events: none; }
.doc-icon { font-size: .82rem; flex-shrink: 0; }
.doc-na { margin-left: auto; font-size: .7rem; color: #ccc; font-style: italic; }

/* Notes empty */
.notes-empty { font-size: .8rem; color: #ccc; font-style: italic; }

/* Enrollment edit panel tabs */
.enr-edit-tabs { display: flex; gap: 0; border-bottom: 2px solid #e8edf4; flex-shrink: 0; padding: 0 1.5rem; background: #fafbfc; }
.enr-tab { background: none; border: none; border-bottom: 3px solid transparent; padding: .7rem 1.1rem; font-size: .87rem; font-weight: 600; color: #888; cursor: pointer; margin-bottom: -2px; }
.enr-tab.active { color: #1a5276; border-bottom-color: #1a5276; }
.enr-tab:hover:not(.active) { color: #555; }

/* Activity tab */
.activity-card { background: #f7f9fc; border: 1.5px solid #e0eaf4; border-radius: 8px; padding: .85rem 1rem; display: flex; align-items: center; flex-wrap: wrap; gap: .6rem; }
.activity-done { border-color: #a8ddd0; background: #edfaf5; }
.activity-desc { font-size: .86rem; color: #444; margin: 0; }
.btn-confirm-adm { background: #0d6b55; color: #fff; border: none; border-radius: 6px; padding: .45rem 1.1rem; font-size: .86rem; font-weight: 600; cursor: pointer; }
.btn-confirm-adm:hover { background: #0a5242; }

/* Transitions */
.fade-enter-active, .fade-leave-active { transition: opacity 0.22s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
.slide-enter-active, .slide-leave-active { transition: transform 0.25s ease; }
.slide-enter-from, .slide-leave-to { transform: translateX(100%); }
.modal-pop-enter-active, .modal-pop-leave-active { transition: opacity 0.18s, transform 0.18s; }
.modal-pop-enter-from, .modal-pop-leave-to { opacity: 0; transform: translate(-50%, -47%) scale(0.96); }

/* ── Main tab bar ──────────────────────────────────────────────────────────── */
.main-tab-bar { background: #fff; border-bottom: 2px solid #e8edf4; display: flex; padding: 0 2rem; }

/* Public signup link banner */
.signup-link-bar {
  display: flex; align-items: center; gap: .65rem; flex-wrap: wrap;
  padding: .55rem 2rem; background: #eef5ff; border-bottom: 1px solid #b6d4fe;
  font-size: .82rem; color: #084298;
}
.signup-link-label { font-weight: 700; }
.signup-link-url {
  flex: 1; min-width: 0; color: #003366; text-decoration: none;
  font-family: ui-monospace, monospace; font-size: .8rem;
  overflow-wrap: anywhere;
}
.signup-link-url:hover { text-decoration: underline; }
.btn-copy-link {
  background: #003366; color: #fff; border: none; padding: .25rem .85rem;
  border-radius: 5px; font-size: .76rem; font-weight: 700; cursor: pointer;
  white-space: nowrap;
}
.btn-copy-link:hover { background: #0055a5; }
.main-tab-btn { background: none; border: none; padding: 0.85rem 1.3rem; font-size: 0.9rem; font-weight: 600; color: #888; cursor: pointer; border-bottom: 3px solid transparent; margin-bottom: -2px; transition: color 0.15s, border-color 0.15s; display: flex; align-items: center; gap: 0.5rem; }
.main-tab-btn.active { color: #1a5276; border-bottom-color: #1a5276; }

/* My Core Programmes tab */
.core-groups { display: flex; flex-direction: column; gap: .75rem; }
.core-group { background: #fff; border: 1px solid #e0e6ee; border-radius: 10px; overflow: hidden; }
.core-group-head { display: flex; justify-content: space-between; padding: .8rem 1rem; background: #f6f9fd; border-bottom: 1px solid #e8edf3; }
.core-count { font-size: .8rem; color: #5f6e85; }
.core-specialization-list { display: flex; flex-direction: column; }
.core-specialization-row { display: flex; justify-content: space-between; align-items: center; padding: .65rem 1rem; border-top: 1px solid #f2f5fa; }
.core-specialization-row:first-child { border-top: none; }
.core-specialization-row.dim { background: #fafbfd; color: #8a93a4; }
.core-specialization-name { font-size: .92rem; }
.toggle { display: inline-flex; align-items: center; gap: .4rem; font-size: .82rem; color: #5f6e85; cursor: pointer; }
.err-banner { background: #fde7e5; color: #a8241e; padding: .55rem .8rem; border-radius: 6px; font-size: .88rem; margin: .4rem 0; }
.loading-row { padding: 1rem; color: #5f6e85; font-size: .9rem; }
.empty-state-card { padding: 1rem; background: #f6f9fd; color: #5f6e85; border-radius: 8px; text-align: center; }
.main-tab-btn:hover:not(.active) { color: #555; }
.tab-badge { background: #f59e0b; color: #fff; border-radius: 20px; padding: 1px 7px; font-size: 0.72rem; font-weight: 700; }

/* ── My Programs tab ───────────────────────────────────────────────────────── */
.prog-empty-state { background: #fff; border-radius: 10px; padding: 2.5rem; text-align: center; color: #aaa; font-style: italic; box-shadow: 0 2px 8px rgba(0,0,0,0.05); }

.prog-card { background: #fff; border-radius: 10px; box-shadow: 0 2px 8px rgba(0,0,0,0.06); margin-bottom: 1rem; overflow: hidden; }
.prog-card-header { display: flex; align-items: center; justify-content: space-between; padding: 1rem 1.25rem; flex-wrap: wrap; gap: 0.75rem; }
.prog-card-info { display: flex; align-items: center; gap: 0.55rem; flex-wrap: wrap; }
.prog-card-name { font-size: 1rem; font-weight: 700; color: #1a5276; }
.badge-code-p { background: #dbeafe; color: #1d4ed8; border-radius: 4px; padding: 1px 8px; font-size: 0.73rem; font-weight: 700; }
.badge-count-p { background: #f0f3f7; color: #777; border-radius: 4px; padding: 1px 7px; font-size: 0.73rem; }
.prog-source { font-size: 0.78rem; color: #999; }
.prog-card-actions { display: flex; gap: 0.5rem; flex-shrink: 0; }

/* Status pills */
.ps-draft    { background: #f0f3f7; color: #666; border-radius: 4px; padding: 2px 9px; font-size: 0.76rem; font-weight: 600; }
.ps-pending  { background: #fff3cd; color: #856404; border-radius: 4px; padding: 2px 9px; font-size: 0.76rem; font-weight: 700; }
.ps-approved { background: #d1fae5; color: #065f46; border-radius: 4px; padding: 2px 9px; font-size: 0.76rem; font-weight: 700; }
.ps-rejected { background: #fdecea; color: #c0392b; border-radius: 4px; padding: 2px 9px; font-size: 0.76rem; font-weight: 700; }

.btn-act-p { border: none; border-radius: 6px; padding: 0.38rem 0.9rem; font-size: 0.83rem; font-weight: 600; cursor: pointer; }
.btn-edit-p { background: #e8f0f8; color: #1a5276; border: 1px solid #c5d8f0; }
.btn-edit-p:hover { background: #d0e4f5; }
.btn-submit-p { background: #1a5276; color: #fff; }
.btn-submit-p:hover { background: #2471a3; }
.btn-view-p { background: #f0f3f7; color: #555; border: 1px solid #d0d7e0; }
.btn-view-p:hover { background: #e4e8ef; }

.prog-rejection-note { font-size: 0.82rem; color: #c0392b; background: #fdecea; padding: 0.4rem 1.25rem; border-top: 1px solid #f5c0bb; }
.prog-admin-disabled-pill { font-size: .68rem; letter-spacing: .02em; text-transform: uppercase; padding: 2px 8px; border-radius: 10px; background: #ecd4d2; color: #8a1515; margin-left: .2rem; }
.prog-admin-disabled-banner { background: #fde7e7; color: #8a1515; border-top: 1px solid #f5c0bb; padding: .55rem 1.25rem; font-size: .85rem; }
.prog-card-disabled { opacity: .75; background: #fcf3f3; }

/* Edit panel */
.prog-edit-panel { padding: 1rem 1.25rem 1.25rem; border-top: 1px solid #f0f3f7; background: #fafbfc; }
.prog-edit-row { display: flex; align-items: center; gap: 0.75rem; margin-bottom: 1rem; }
.prog-edit-label { font-size: 0.8rem; font-weight: 600; color: #555; width: 130px; flex-shrink: 0; }
.prog-edit-input { flex: 1; padding: 0.45rem 0.7rem; border: 1.5px solid #ccc; border-radius: 6px; font-size: 0.9rem; outline: none; font-family: inherit; }
.prog-edit-input:focus { border-color: #1a5276; }

.pathway-grid-p {
  flex: 1; display: flex; flex-direction: column; gap: 0.3rem;
  border: 1px solid #e8edf4; border-radius: 6px; padding: 0.55rem 0.75rem;
  max-height: 220px; overflow: auto; background: #fafbfc;
}
.pathway-row-p { display: flex; align-items: center; gap: 0.5rem; font-size: 0.85rem; cursor: pointer; }
.pathway-row-p input[type=checkbox] { width: 14px; height: 14px; accent-color: #003366; }

.pathway-row-toggle { cursor: pointer; user-select: none; margin-bottom: 0.5rem; }
.pathway-row-toggle:hover { background: #f7f9fb; border-radius: 6px; }

.ro-pathway-list { display: flex; flex-wrap: wrap; gap: 0.4rem; flex: 1; }
.ro-pathway-pill {
  background: #eef4fb; color: #003366; border-radius: 4px;
  padding: 2px 8px; font-size: 0.78rem; font-weight: 600;
}

.prog-specializations-section { margin-top: 0.25rem; }
.prog-specializations-title { font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.06em; color: #999; font-weight: 700; margin-bottom: 0.6rem; }
.prog-maj-block { border: 1px solid #e8edf4; border-radius: 7px; margin-bottom: 0.5rem; overflow: hidden; }
.prog-maj-header { display: flex; align-items: center; gap: 0.5rem; padding: 0.5rem 0.75rem; background: #f7f9fb; cursor: pointer; }
.prog-maj-header:hover { background: #eef2f8; }
.prog-maj-name-input { flex: 1; padding: 0.28rem 0.5rem; border: 1px solid #ccc; border-radius: 4px; font-size: 0.86rem; font-weight: 500; outline: none; cursor: text; }
.prog-maj-name-input:focus { border-color: #1a5276; }
.btn-del-maj { background: none; border: none; color: #ddd; cursor: pointer; font-size: 0.88rem; padding: 2px 4px; flex-shrink: 0; }
.btn-del-maj:hover:not(:disabled) { color: #c0392b; }
.btn-del-maj:disabled { cursor: default; }

.prog-subj-block { padding: 0.6rem 0.75rem 0.75rem; background: #fff; }

/* 3-column subject layout: code | name | ects */
.subj-col-header { display: flex; align-items: center; gap: 0.45rem; padding: 0 0 0.3rem; border-bottom: 1px solid #e8edf4; margin-bottom: 0.35rem; }
.subj-col-header span { font-size: 0.7rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.05em; color: #aaa; }
.col-code { width: 90px; flex-shrink: 0; }
.col-name { flex: 1; }
.col-ects { width: 50px; flex-shrink: 0; text-align: center; }
.col-del  { width: 20px; flex-shrink: 0; }

.prog-subj-row { display: flex; align-items: center; gap: 0.45rem; margin-bottom: 0.3rem; }
.inp-s-code { width: 90px; flex-shrink: 0; padding: 0.3rem 0.5rem; border: 1px solid #ccc; border-radius: 4px; font-size: 0.82rem; outline: none; font-family: ui-monospace, monospace; }
.inp-s-code:focus { border-color: #1a5276; }
.inp-s-name { flex: 1; padding: 0.3rem 0.5rem; border: 1px solid #ccc; border-radius: 4px; font-size: 0.83rem; outline: none; }
.inp-s-name:focus { border-color: #1a5276; }
.inp-s-cr { width: 50px; flex-shrink: 0; padding: 0.3rem 0.4rem; border: 1px solid #ccc; border-radius: 4px; font-size: 0.83rem; text-align: center; outline: none; }
.inp-s-cr:focus { border-color: #1a5276; }
.btn-x-s { background: none; border: none; color: #ccc; cursor: pointer; font-size: 0.82rem; flex-shrink: 0; }
.btn-x-s:hover { color: #c0392b; }
.prog-add-subj-row { display: flex; align-items: center; gap: 0.45rem; margin-top: 0.5rem; padding-top: 0.45rem; border-top: 1px dashed #e8edf4; }
.btn-add-s { background: #1a5276; color: #fff; border: none; border-radius: 5px; padding: 0.3rem 0.7rem; font-size: 0.8rem; cursor: pointer; white-space: nowrap; }
.btn-add-s:hover { background: #2471a3; }

.prog-add-maj-row { display: flex; gap: 0.5rem; align-items: center; margin-top: 0.75rem; padding-top: 0.65rem; border-top: 1px dashed #dde6f0; }
.btn-add-maj { background: #e8f0f8; color: #1a5276; border: 1px solid #c5d8f0; border-radius: 6px; padding: 0.38rem 0.9rem; font-size: 0.83rem; font-weight: 600; cursor: pointer; white-space: nowrap; }
.btn-add-maj:hover { background: #d0e4f5; }

.prog-maj-summary { display: flex; flex-wrap: wrap; gap: 0.4rem; padding: 0.6rem 1.25rem 0.9rem; border-top: 1px solid #f5f6f8; }
.prog-maj-pill { background: #f0f3f7; color: #555; border-radius: 20px; padding: 3px 12px; font-size: 0.78rem; display: flex; align-items: center; gap: 0.35rem; }
.prog-maj-pill-count { background: #dde4ec; border-radius: 20px; padding: 0 5px; font-size: 0.7rem; }

/* Clone modal */
.modal-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.45); z-index: 200; display: flex; align-items: center; justify-content: center; }
.clone-modal { background: #fff; border-radius: 12px; width: 520px; max-width: 95vw; box-shadow: 0 8px 40px rgba(0,0,0,0.2); overflow: hidden; }
.clone-modal-header { display: flex; align-items: center; justify-content: space-between; padding: 1.1rem 1.4rem 0.75rem; border-bottom: 1px solid #f0f3f7; }
.clone-modal-header h3 { font-size: 1.05rem; font-weight: 700; color: #1a5276; margin: 0; }
.btn-modal-close { background: none; border: none; color: #aaa; font-size: 1.1rem; cursor: pointer; }
.btn-modal-close:hover { color: #333; }
.clone-modal-sub { font-size: 0.82rem; color: #888; padding: 0.6rem 1.4rem 0; }
.clone-modal-list { padding: 0.75rem 1.4rem 1.2rem; display: flex; flex-direction: column; gap: 0.5rem; }
.clone-modal-item { display: flex; align-items: center; justify-content: space-between; padding: 0.65rem 0.9rem; border: 1px solid #e8edf4; border-radius: 8px; }
.clone-modal-prog-info { display: flex; align-items: center; gap: 0.5rem; }
.clone-modal-prog-name { font-size: 0.88rem; font-weight: 600; color: #222; }
.btn-clone-prog { background: #1a5276; color: #fff; border: none; border-radius: 6px; padding: 0.35rem 0.9rem; font-size: 0.83rem; font-weight: 600; cursor: pointer; }
.btn-clone-prog:hover { background: #2471a3; }

/* Arrow for maj headers */
.arrow-sm { color: #aaa; font-size: 0.75rem; width: 10px; flex-shrink: 0; }

/* Expand arrow in card header */
.prog-expand-arrow { color: #aaa; font-size: 0.8rem; flex-shrink: 0; }

/* Approved note */
.prog-approved-note { font-size: 0.79rem; color: #065f46; background: #d1fae5; border-radius: 5px; padding: 3px 10px; font-weight: 600; }

/* Read-only view */
.prog-readonly-notice { font-size: 0.84rem; color: #555; background: #f7f9fb; border: 1px solid #e0eaf4; border-radius: 7px; padding: 0.6rem 0.9rem; margin-bottom: 1rem; }
.prog-maj-header-ro { cursor: pointer; }
.prog-maj-header-ro:hover { background: #eef2f8; }
.prog-maj-name-ro { font-size: 0.88rem; font-weight: 600; color: #333; flex: 1; }
.prog-subj-row-ro { display: flex; align-items: center; gap: 0.45rem; padding: 0.28rem 0; border-bottom: 1px solid #f0f3f7; font-size: 0.84rem; }
.prog-subj-row-ro:last-child { border-bottom: none; }
.ro-subj-code { width: 90px; flex-shrink: 0; font-family: ui-monospace, monospace; font-size: 0.78rem; color: #1a5276; font-weight: 600; }
.ro-subj-name { flex: 1; color: #444; }
.ro-subj-cr { width: 50px; flex-shrink: 0; text-align: center; color: #666; font-size: 0.82rem; font-weight: 600; }
.ro-empty { color: #bbb; font-style: italic; font-size: 0.82rem; margin: 0.3rem 0; }
</style>
