<template>
  <div class="page-wrapper">
    <nav class="navbar">
      <span class="brand-text">IBSS Admin Portal</span>
      <div class="nav-links">
        <RouterLink to="/admin" class="nav-link active-link">Dashboard</RouterLink>
        <RouterLink to="/admin/academic" class="nav-link">Academic</RouterLink>
        <RouterLink to="/admin/config" class="nav-link">System Config</RouterLink>
      </div>
      <div class="nav-right">
        <span class="nav-user">{{ auth.user?.displayName }}</span>
        <button class="btn-logout" @click="logout">Log out</button>
      </div>
    </nav>

    <!-- Tab bar -->
    <div class="tab-bar">
      <button :class="['tab-btn', { active: tab === 'students' }]" @click="tab = 'students'">Students</button>
      <button :class="['tab-btn', { active: tab === 'partners' }]" @click="tab = 'partners'">Partners</button>
      <button :class="['tab-btn', { active: tab === 'messages' }]" @click="tab = 'messages'">
        Messages
        <span v-if="pendingMsgCount" class="tab-badge">{{ pendingMsgCount }}</span>
      </button>
    </div>

    <!-- ══════════════════════ STUDENTS TAB ══════════════════════ -->
    <div v-show="tab === 'students'" class="container">
      <div class="page-header">
        <div>
          <h1 class="page-title">Students</h1>
          <p class="page-sub">{{ students.length }} student{{ students.length !== 1 ? 's' : '' }} total</p>
        </div>
        <button class="btn-primary" @click="openAddStudent">+ Add Student</button>
      </div>

      <div class="filters-bar">
        <div class="search-wrap">
          <svg class="search-icon" viewBox="0 0 20 20" fill="none">
            <circle cx="9" cy="9" r="6" stroke="#888" stroke-width="1.6"/>
            <path d="M13.5 13.5 17 17" stroke="#888" stroke-width="1.6" stroke-linecap="round"/>
          </svg>
          <input v-model="search" class="search-input" type="text" placeholder="Search by name or student ID…" @keydown.escape="search = ''" />
          <button v-if="search" class="search-clear" @click="search = ''">✕</button>
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
        <select v-model="filterPartner" class="filter-select"><option value="">All Partners</option><option v-for="n in partnerNames" :key="n">{{ n }}</option></select>
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
        <!-- Major multi-select -->
        <div class="ms-wrap" v-click-outside="() => showMajDrop = false">
          <button class="ms-btn" :class="{ 'ms-btn-active': filterMajs.size }" @click="showMajDrop = !showMajDrop">
            {{ filterMajs.size ? `${filterMajs.size} Major${filterMajs.size > 1 ? 's' : ''}` : 'All Majors' }}
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
        <button v-if="search || filterPartner || filterProgs.size || filterMajs.size" class="btn-ghost" @click="clearFilters">Clear filters</button>
        <span class="filter-count">{{ filteredStudents.length }} of {{ students.length }} students</span>
      </div>

      <!-- Empty state -->
      <div v-if="filteredStudents.length === 0" class="empty-state-card">
        No students match the current filters.
      </div>

      <!-- Student Cards -->
      <div v-for="s in filteredStudents" :key="s.id" class="student-card">
        <!-- Card header -->
        <div class="sc-header" @click="toggleCard(s.id)" style="cursor:pointer">
          <div class="sc-id-name">
            <span class="sc-sid">{{ s.studentId }}</span>
            <span class="sc-sep">·</span>
            <span class="sc-name">{{ s.firstName }} {{ s.lastName }}</span>
            <span class="sc-partner-chip">{{ s.partner }}</span>
          </div>
          <div class="sc-header-right">
            <span v-if="hasPendingAbsence(s.studentId)" class="abs-badge">Absence</span>
            <span :class="'s-badge s-' + studentStatus(s)">{{ STATUS_LABELS[studentStatus(s)] }}</span>
            <span v-if="collapsedCards.has(s.id)" class="sc-summary">
              {{ s.enrollments.length }} programme{{ s.enrollments.length !== 1 ? 's' : '' }}
              &nbsp;·&nbsp;
              {{ s.enrollments.reduce((t,e) => t+e.coursesCompleted, 0) }}/{{ s.enrollments.reduce((t,e) => t+e.coursesRequired, 0) }} courses
            </span>
            <span class="sc-chevron" :class="{ collapsed: collapsedCards.has(s.id) }">&#8964;</span>
          </div>
        </div>

        <!-- Enrollment table (collapsible) -->
        <div v-if="!collapsedCards.has(s.id)" class="sc-body">
          <div class="enr-table-wrap">
            <table class="enr-table">
              <thead>
                <tr>
                  <th class="th-prog">Programme &amp; Major</th>
                  <th class="th-status">Status</th>
                  <th class="th-acad">Academic Progress</th>
                  <th class="th-pay">Payment</th>
                  <th class="th-rel">Releases</th>
                  <th class="th-act"></th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="enr in s.enrollments" :key="enr.id" class="enr-row">

                  <!-- Col 1: Programme & Major + doc links -->
                  <td class="td-prog">
                    <div class="prog-name-main">{{ enr.programme }}</div>
                    <div class="prog-major-sub">{{ enr.major }}</div>
                    <div class="enr-doc-list">
                      <span :class="['doc-chip', enr.offerType ? 'doc-chip-on' : 'doc-chip-off']">
                        &#128196; Offer {{ enr.offerType ? (enr.offerType === 'offer' ? '(Full)' : '(Cond.)') : '—' }}
                      </span>
                      <span :class="['doc-chip', enr.paymentDone ? 'doc-chip-on' : 'doc-chip-off']">
                        &#128196; Admission {{ enr.paymentDone ? '✓' : '—' }}
                      </span>
                      <span :class="['doc-chip', enr.certReleased ? 'doc-chip-on' : 'doc-chip-off']">
                        &#127891; Cert {{ enr.certReleased ? '✓' : '—' }}
                      </span>
                      <span :class="['doc-chip', enr.transcriptReleased ? 'doc-chip-on' : 'doc-chip-off']">
                        &#128203; Transcript {{ enr.transcriptReleased ? '✓' : '—' }}
                      </span>
                    </div>
                  </td>

                  <!-- Col 2: Status — editable -->
                  <td class="td-status">
                    <select v-model="enr.enrollmentStatus" class="status-sel-inline">
                      <option v-for="st in ENROLLMENT_STATUSES" :key="st" :value="st">{{ st }}</option>
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
                      <span v-if="isGraded(s.studentId)" class="rel-yes">Graded</span>
                      <span v-else class="rel-no">Not yet</span>
                    </div>
                    <div v-if="enr.admissionConfirmed" class="ap-row" style="margin-top:.3rem">
                      <span class="confirmed-chip">Admitted ✓</span>
                    </div>
                  </td>

                  <!-- Col 4: Payment — editable -->
                  <td class="td-pay">
                    <div class="pay-section">
                      <div class="ap-lbl">Tuition</div>
                      <select v-model="enr.tuitionFeeStatus" class="sel-sm">
                        <option>unpaid</option>
                        <option>partially paid</option>
                        <option>fully paid</option>
                      </select>
                    </div>
                    <div class="pay-section" style="margin-top:.4rem">
                      <div class="ap-lbl">Other Fees</div>
                      <select v-model="enr.otherFeesStatus" class="sel-sm">
                        <option>not applicable</option>
                        <option>unpaid</option>
                        <option>partially paid</option>
                        <option>fully paid</option>
                      </select>
                    </div>
                    <div style="margin-top:.5rem">
                      <button :class="enr.paymentDone ? 'tog-on' : 'tog-off'" @click.stop="enr.paymentDone = !enr.paymentDone">
                        {{ enr.paymentDone ? 'Paid ✓' : 'Not Paid' }}
                      </button>
                    </div>
                  </td>

                  <!-- Col 5: Releases -->
                  <td class="td-rel">
                    <button :class="enr.certReleased ? 'tog-on' : 'tog-off'" class="tog-wide" @click.stop="enr.certReleased = !enr.certReleased">
                      {{ enr.certReleased ? 'Cert ✓' : 'Cert —' }}
                    </button>
                    <button :class="enr.transcriptReleased ? 'tog-on' : 'tog-off'" class="tog-wide" style="margin-top:.4rem" @click.stop="enr.transcriptReleased = !enr.transcriptReleased">
                      {{ enr.transcriptReleased ? 'Transcript ✓' : 'Transcript —' }}
                    </button>
                    <div style="margin-top:.5rem">
                      <button v-if="!enr.admissionConfirmed && enr.paymentDone" class="btn-confirm-adm-sm" @click.stop="enr.admissionConfirmed = true">Confirm Admission</button>
                      <span v-else-if="enr.admissionConfirmed" class="confirmed-chip">Admitted ✓</span>
                    </div>
                    <div style="margin-top:.4rem">
                      <button v-if="!enr.offerType" class="btn-issue-offer" @click.stop="enr.offerType = 'offer'">Issue Offer</button>
                      <span v-else :class="['offer-chip', enr.offerType === 'conditional_offer' ? 'offer-chip-cond' : 'offer-chip-full']">
                        {{ enr.offerType === 'conditional_offer' ? 'Cond. Offer' : 'Full Offer' }}
                      </span>
                    </div>
                  </td>

                  <!-- Col 6: Actions -->
                  <td class="td-act">
                    <RouterLink :to="`/admin/students/${s.id}`" class="btn-view-link">View →</RouterLink>
                  </td>

                </tr>
              </tbody>
            </table>
          </div>
          <div class="sc-footer">
            <RouterLink :to="`/admin/students/${s.id}`" class="sc-detail-link">Open Full Profile →</RouterLink>
          </div>
        </div>
      </div>
    </div>

    <!-- ══════════════════════ PARTNERS TAB ══════════════════════ -->
    <div v-show="tab === 'partners'" class="container">
      <div class="page-header">
        <div>
          <h1 class="page-title">Partners</h1>
          <p class="page-sub" v-if="!partnersLoading">{{ partners.length }} partner{{ partners.length !== 1 ? 's' : '' }} registered</p>
        </div>
        <button class="btn-primary" @click="openCreatePartner">+ Add Partner</button>
      </div>

      <div v-if="partnersError" class="err-banner">{{ partnersError }}</div>
      <div v-if="partnersLoading" class="loading-row">Loading…</div>

      <div v-else class="table-wrap">
        <table class="data-table">
          <thead>
            <tr>
              <th>Partner Name</th>
              <th>Users</th>
              <th>Status</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="partners.length === 0">
              <td colspan="4" class="empty-row">No partners yet.</td>
            </tr>
            <tr v-for="p in partners" :key="p.partnerId" class="data-row">
              <td><strong>{{ p.name }}</strong></td>
              <td>{{ p.userCount }} user{{ p.userCount !== 1 ? 's' : '' }}</td>
              <td>
                <span :class="p.isEnabled ? 'badge-enabled' : 'badge-disabled'">
                  {{ p.isEnabled ? 'Active' : 'Disabled' }}
                </span>
              </td>
              <td class="actions-cell">
                <button class="btn-sm" @click="openManagePartner(p)">Manage</button>
                <button v-if="p.isEnabled" class="btn-sm btn-warn" @click="disablePartner(p)">Disable</button>
                <button v-else class="btn-sm btn-ok" @click="enablePartner(p)">Enable</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Manage partner panel (inline, tabbed) -->
      <div v-if="managingPartner" class="manage-panel">
        <div class="manage-panel-header">
          <h2>{{ managingPartner.name }}</h2>
          <button class="btn-close-panel" @click="managingPartner = null">✕ Close</button>
        </div>

        <div class="manage-tab-bar">
          <button v-for="t in MANAGE_TABS" :key="t.k" :class="['manage-tab-btn', { active: manageTab === t.k }]" @click="manageTab = t.k">{{ t.label }}</button>
        </div>

        <div v-show="manageTab === 'profile'" class="manage-section">
          <PartnerProfileTab :partner-id="managingPartner.partnerId" @updated="loadPartners" />
        </div>

        <div v-show="manageTab === 'users'" class="manage-section">
          <div class="manage-section-title">Users</div>
          <div v-if="partnerUsersLoading" class="loading-row">Loading users…</div>
          <table v-else class="data-table" style="margin-bottom:.75rem">
            <thead><tr><th>Username</th><th>Name</th><th>Email</th><th>Status</th><th></th></tr></thead>
            <tbody>
              <tr v-if="partnerUsers.length === 0"><td colspan="5" class="empty-row">No users yet.</td></tr>
              <template v-for="u in partnerUsers" :key="u.userId">
                <tr class="data-row">
                  <td class="mono">{{ u.username }}</td>
                  <td>{{ [u.firstName, u.lastName].filter(Boolean).join(' ') || '—' }}</td>
                  <td>{{ u.email || '—' }}</td>
                  <td><span :class="u.isEnabled ? 'badge-enabled' : 'badge-disabled'">{{ u.isEnabled ? 'Active' : 'Disabled' }}</span></td>
                  <td class="actions-cell">
                    <button class="btn-sm" @click="startEditUser(u)">Edit</button>
                    <button class="btn-sm" @click="resetUserPassword(u)" :disabled="resettingUserId === u.userId">
                      {{ resettingUserId === u.userId ? 'Resetting…' : 'Reset Password' }}
                    </button>
                    <button v-if="u.isEnabled" class="btn-sm btn-warn" @click="disableUser(u)">Disable</button>
                    <button v-else class="btn-sm btn-ok" @click="enableUser(u)">Enable</button>
                  </td>
                </tr>
                <tr v-if="editingUserId === u.userId" class="edit-row">
                  <td colspan="5">
                    <div class="edit-row-inner">
                      <input v-model="editForm.username"  class="inp-add" placeholder="Username" />
                      <input v-model="editForm.firstName" class="inp-add" placeholder="First name" />
                      <input v-model="editForm.lastName"  class="inp-add" placeholder="Last name" />
                      <button class="btn-primary-sm" :disabled="editSaving || !editForm.username.trim()" @click="saveEditUser">
                        {{ editSaving ? 'Saving…' : 'Save' }}
                      </button>
                      <button class="btn-sm" @click="cancelEditUser">Cancel</button>
                    </div>
                    <p v-if="editError" class="form-error">{{ editError }}</p>
                  </td>
                </tr>
                <tr v-if="resetUserId === u.userId && resetPassword" class="reveal-row">
                  <td colspan="5">
                    <div class="password-reveal">
                      <strong>Temporary password for {{ u.username }}:</strong>
                      <code>{{ resetPassword }}</code>
                      <button class="btn-copy" @click="copyResetPassword">Copy</button>
                      <p class="password-note">Save this password — it won't be shown again.</p>
                    </div>
                  </td>
                </tr>
              </template>
            </tbody>
          </table>

          <div class="add-user-row">
            <input v-model="newUserUsername" class="inp-add" placeholder="New username" @keyup.enter="addUserToPartner" />
            <input v-model="newUserEmail" class="inp-add" placeholder="Email (optional)" />
            <button class="btn-primary-sm" :disabled="addingUser || !newUserUsername" @click="addUserToPartner">
              {{ addingUser ? 'Adding…' : '+ Add User' }}
            </button>
          </div>

          <!-- Show generated password after add -->
          <div v-if="newUserPassword" class="password-reveal">
            <strong>Temporary password for {{ newUserUsername }}:</strong>
            <code>{{ newUserPassword }}</code>
            <button class="btn-copy" @click="copyPassword">Copy</button>
            <p class="password-note">Save this password — it won't be shown again.</p>
          </div>
          <p v-if="addUserError" class="form-error">{{ addUserError }}</p>
        </div>

        <div v-show="manageTab === 'core'" class="manage-section">
          <PartnerCoreProgrammesTab :partner-id="managingPartner.partnerId" />
        </div>

        <div v-show="manageTab === 'custom'" class="manage-section">
          <PartnerCustomProgrammesTab :partner-id="managingPartner.partnerId" :partner-name="managingPartner.name" />
        </div>

        <div v-show="manageTab === 'students'" class="manage-section">
          <PartnerStudentsTab :partner-name="managingPartner.name" />
        </div>
      </div>
    </div>

    <!-- ══════════════════════ MESSAGES TAB ══════════════════════ -->
    <div v-show="tab === 'messages'" class="container">

      <!-- Announcements -->
      <div class="msg-section">
        <h2 class="section-title">Announcements</h2>
        <div class="msg-card">
          <h3>Post Announcement</h3>
          <div class="field"><label>Title <span class="req">*</span></label><input v-model="anForm.title" placeholder="Announcement title…" /></div>
          <div class="field" style="margin-top:.65rem"><label>Body <span class="req">*</span></label><textarea v-model="anForm.body" rows="3" placeholder="Message content…"></textarea></div>
          <div class="field" style="margin-top:.65rem">
            <label>Target</label>
            <select v-model="anForm.targetStudentId">
              <option :value="null">All Students</option>
              <option v-for="s in students" :key="s.studentId" :value="s.studentId">{{ s.firstName }} {{ s.lastName }} ({{ s.studentId }})</option>
            </select>
          </div>
          <button class="btn-primary" style="margin-top:.75rem" :disabled="!anForm.title || !anForm.body" @click="postAnnouncement">Post</button>
        </div>
        <div v-if="announcements.length" class="msg-card">
          <h3>Past Announcements</h3>
          <table class="data-table">
            <thead><tr><th>Title</th><th>Target</th><th>Date</th></tr></thead>
            <tbody>
              <tr v-for="a in [...announcements].reverse()" :key="a.id" class="data-row">
                <td><strong>{{ a.title }}</strong><div style="font-size:.8rem;color:#666">{{ a.body }}</div></td>
                <td>{{ a.targetStudentId ? studentNameById(a.targetStudentId) : 'All Students' }}</td>
                <td style="white-space:nowrap;font-size:.82rem">{{ fmtDate(a.createdAt) }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Support Tickets -->
      <div class="msg-section">
        <h2 class="section-title">Support Tickets</h2>
        <div v-if="tickets.length === 0" class="empty-msg">No support tickets yet.</div>
        <div v-for="t in [...tickets].reverse()" :key="t.id" class="msg-card ticket-card">
          <div class="ticket-meta" @click="toggleAdminTicket(t.id)">
            <span class="ticket-subject-admin">{{ t.subject }}</span>
            <span class="ticket-student-admin">{{ studentNameById(t.studentId) }}</span>
            <span :class="['s-badge', t.status === 'resolved' ? 's-approved' : 's-offer']">{{ t.status === 'resolved' ? 'Resolved' : 'Open' }}</span>
            <span style="font-size:.78rem;color:#888">{{ fmtDate(t.createdAt) }}</span>
            <span style="font-size:.75rem;color:#999">{{ expandedAdminTickets.includes(t.id) ? '▲' : '▼' }}</span>
          </div>
          <div v-if="expandedAdminTickets.includes(t.id)" class="ticket-thread-admin">
            <div v-for="(r, i) in t.replies" :key="i" :class="['reply-adm', 'reply-adm-' + r.from]">
              <span class="reply-from-lbl">{{ r.from === 'student' ? studentNameById(t.studentId) : r.from === 'admin' ? 'Admin' : 'Partner' }}</span>
              <span class="reply-at-lbl">{{ fmtDate(r.at) }}</span>
              <p>{{ r.text }}</p>
            </div>
            <div v-if="t.status !== 'resolved'" class="admin-reply-box">
              <textarea v-model="adminReplyTexts[t.id]" rows="2" placeholder="Write a reply…"></textarea>
              <div class="admin-reply-actions">
                <button class="btn-primary" style="font-size:.82rem;padding:.38rem .9rem" :disabled="!adminReplyTexts[t.id]" @click="adminReply(t)">Reply</button>
                <button class="btn-sm-teal" @click="resolveTicket(t)">Mark Resolved</button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Absence Reports -->
      <div class="msg-section">
        <h2 class="section-title">Absence Reports</h2>
        <div v-if="absences.length === 0" class="empty-msg">No absence reports yet.</div>
        <div v-else class="msg-card">
          <table class="data-table">
            <thead><tr><th>Student</th><th>Partner</th><th>Date</th><th>Type</th><th>Reason</th><th>Status</th><th></th></tr></thead>
            <tbody>
              <tr v-for="a in [...absences].reverse()" :key="a.id" class="data-row">
                <td><strong>{{ studentNameById(a.studentId) }}</strong></td>
                <td style="font-size:.82rem;color:#666">{{ studentPartnerById(a.studentId) }}</td>
                <td style="white-space:nowrap">{{ a.date }}</td>
                <td>{{ a.type }}</td>
                <td style="font-size:.82rem;color:#555">{{ a.reason || '—' }}</td>
                <td>
                  <span :class="['s-badge', a.status === 'acknowledged' ? 's-graded' : 's-new']">
                    {{ a.status === 'acknowledged' ? 'Acknowledged' : 'Pending' }}
                  </span>
                </td>
                <td>
                  <button v-if="a.status !== 'acknowledged'" class="btn-sm-teal" @click="acknowledgeAbsence(a)">Acknowledge</button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

    </div>

    <!-- ══ Add Student Drawer ══ -->
    <transition name="fade"><div v-if="showStudentDrawer" class="overlay" @click.self="showStudentDrawer = false" /></transition>
    <transition name="slide">
      <div v-if="showStudentDrawer" class="drawer">
        <div class="drawer-header">
          <h2>Add New Student</h2>
          <button class="drawer-close" @click="showStudentDrawer = false">✕</button>
        </div>
        <form @submit.prevent="submitStudent" class="drawer-form">
          <div class="row-2">
            <div class="field"><label>First Name <span class="req">*</span></label><input v-model="sForm.firstName" required /></div>
            <div class="field"><label>Last Name <span class="req">*</span></label><input v-model="sForm.lastName" required /></div>
          </div>
          <div class="field"><label>Passport / ID No. <span class="req">*</span></label><input v-model="sForm.passportId" required /></div>
          <div class="field"><label>Address</label><input v-model="sForm.address" /></div>
          <div class="field">
            <label>Partner <span class="req">*</span></label>
            <select v-model="sForm.partner" required>
              <option value="">— Select —</option>
              <option v-for="n in partnerNames" :key="n">{{ n }}</option>
            </select>
          </div>
          <div class="field">
            <label>Programme <span class="req">*</span></label>
            <select v-model="sForm.programme" required>
              <option value="">— Select —</option>
              <option v-for="n in programmeNames" :key="n">{{ n }}</option>
            </select>
          </div>
          <div class="field">
            <label>Major <span class="req">*</span></label>
            <select v-model="sForm.major" required>
              <option value="">— Select —</option>
              <option v-for="n in majorNames" :key="n">{{ n }}</option>
            </select>
          </div>
          <div class="field"><label>Commencement Date <span class="req">*</span></label><input v-model="sForm.commencementDate" type="date" required /></div>
          <div class="field">
            <label>Mode of Study <span class="req">*</span></label>
            <select v-model="sForm.modeOfStudy" required>
              <option value="">— Select —</option>
              <option>Distance/Online self-study</option>
              <option>Blended learning</option>
              <option>On-campus</option>
            </select>
          </div>
          <div v-if="sSuccess" class="success-msg">Student <strong>{{ sSuccess }}</strong> added!</div>
          <div class="drawer-actions">
            <button type="button" class="btn-cancel" @click="showStudentDrawer = false">Cancel</button>
            <button type="submit" class="btn-save">Save Student</button>
          </div>
        </form>
      </div>
    </transition>

    <!-- ══ Create Partner Wizard ══ -->
    <CreatePartnerWizard
      v-if="showPartnerWizard"
      @close="onWizardClose"
      @created="onWizardCreated"
    />

  </div>
</template>

<script setup>
import { ref, reactive, computed, watch } from 'vue'
import { useRouter, RouterLink } from 'vue-router'
import apiClient from '../api/client.js'
import { auth } from '../store/auth.js'
import { students, getNextId, nextEnrollId, ENROLLMENT_STATUSES } from '../mock/data.js'
import {
  partnerRecords, uid,
  getAllCoreAccessKeys,
  getProgrammeNames, getMajorNames, getPartnerNames,
} from '../mock/programmes.js'
import CreatePartnerWizard from '../components/partner/CreatePartnerWizard.vue'
import PartnerProfileTab from '../components/partner/tabs/PartnerProfileTab.vue'
import PartnerCoreProgrammesTab from '../components/partner/tabs/PartnerCoreProgrammesTab.vue'
import PartnerCustomProgrammesTab from '../components/partner/tabs/PartnerCustomProgrammesTab.vue'
import PartnerStudentsTab from '../components/partner/tabs/PartnerStudentsTab.vue'
import { isGraded } from '../store/grades.js'
import { announcements, nextAnnouncementId } from '../mock/announcements.js'
import { tickets } from '../mock/tickets.js'
import { absences } from '../mock/absences.js'

const router = useRouter()
const tab = ref('students')

// ── Derived dropdown lists (live) ─────────────────────────────────────────────
const partnerNames   = computed(() => getPartnerNames())
const programmeNames = computed(() => getProgrammeNames())
const majorNames     = computed(() => getMajorNames())

// ── Students tab ──────────────────────────────────────────────────────────────
const collapsedCards = reactive(new Set())
function toggleCard(id) {
  if (collapsedCards.has(id)) collapsedCards.delete(id)
  else collapsedCards.add(id)
}

const search = ref('')
const filterPartner = ref('')
const showProgDrop = ref(false)
const showMajDrop  = ref(false)
const filterProgs  = reactive(new Set())
const filterMajs   = reactive(new Set())

function toggleFilter(set, val) {
  if (set.has(val)) set.delete(val)
  else set.add(val)
}

const availableProgs = computed(() => {
  const s = new Set()
  students.forEach(st => st.enrollments?.forEach(e => s.add(e.programme)))
  return [...s].sort()
})
const availableMajs = computed(() => {
  const s = new Set()
  students.forEach(st => st.enrollments?.forEach(e => s.add(e.major)))
  return [...s].sort()
})

const vClickOutside = {
  mounted(el, binding) {
    el._out = e => { if (!el.contains(e.target)) binding.value(e) }
    document.addEventListener('mousedown', el._out)
  },
  unmounted(el) { document.removeEventListener('mousedown', el._out) },
}

function fuzzy(hay, needle) {
  if (!needle) return true
  const h = hay.toLowerCase(), n = needle.toLowerCase().trim()
  let i = 0
  for (const ch of n) { const p = h.indexOf(ch, i); if (p < 0) return false; i = p + 1 }
  return true
}

const filteredStudents = computed(() =>
  students.filter(s => {
    if (!fuzzy(`${s.firstName} ${s.lastName} ${s.studentId}`, search.value)) return false
    if (filterPartner.value && s.partner !== filterPartner.value) return false
    if (filterProgs.size && !s.enrollments?.some(e => filterProgs.has(e.programme))) return false
    if (filterMajs.size  && !s.enrollments?.some(e => filterMajs.has(e.major)))      return false
    return true
  })
)
function clearFilters() {
  search.value = ''
  filterPartner.value = ''
  filterProgs.clear()
  filterMajs.clear()
}
function fmtDate(d) {
  if (!d) return '—'
  return new Date(d).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })
}

function studentStatus(s) {
  const enr = s.enrollments?.[0]
  if (!enr) return 'new'
  if (enr.certReleased)      return 'approved'
  if (isGraded(s.studentId)) return 'graded'
  if (enr.admissionConfirmed) return 'confirmed'
  if (enr.paymentDone)       return 'admission'
  if (enr.offerType)         return 'offer'
  return 'new'
}
const STATUS_LABELS = { new: 'New', offer: 'Offer Issued', admission: 'Admission', confirmed: 'Admitted', graded: 'Graded', approved: 'Approved' }

function hasPendingAbsence(sid) {
  return absences.some(a => a.studentId === sid && a.status === 'pending')
}
const NEXT_ACTION = {
  new:       'Awaiting offer from partner',
  offer:     'Mark payment received',
  admission: 'Release certificate when ready',
  confirmed: 'Awaiting grading by partner',
  graded:    'Release certificate',
  approved:  'Complete',
}

// Add student
const showStudentDrawer = ref(false)
const sSuccess = ref('')
const sForm = reactive({ firstName:'', lastName:'', passportId:'', address:'', partner:'', programme:'', major:'', commencementDate:'', modeOfStudy:'' })
function openAddStudent() { Object.assign(sForm, { firstName:'', lastName:'', passportId:'', address:'', partner:'', programme:'', major:'', commencementDate:'', modeOfStudy:'' }); sSuccess.value=''; showStudentDrawer.value=true }

const progCodeMap = { 'Master of Business Administration':'MBA', 'Bachelor of Business Administration':'BBA', 'Master of Finance':'MF', 'Bachelor of Computer Science':'BCS', 'Master of Marketing':'MM' }
function submitStudent() {
  const seq = getNextId()
  const code = progCodeMap[sForm.programme] ?? 'GEN'
  const now = new Date()
  const sid = `IBSS.${code}.${String(now.getFullYear()).slice(2)}${String(now.getMonth()+1).padStart(2,'0')}${String(seq).padStart(4,'0')}`
  students.push({
    id: seq, studentId: sid,
    firstName: sForm.firstName, lastName: sForm.lastName,
    passportId: sForm.passportId, address: sForm.address,
    partner: sForm.partner,
    email: '', dateOfBirth: '', highestDegree: '', languageResult: '', yearsWorkExperience: 0,
    docPassport: null, docDegree: null, docLanguage: null, docCV: null,
    docsVerified: { passport: false, degree: false, language: false, cv: false },
    enrollments: [
      {
        id: nextEnrollId(),
        programme: sForm.programme, major: sForm.major,
        commencementDate: sForm.commencementDate, modeOfStudy: sForm.modeOfStudy,
        durationOfStudy: '',
        selectedPathway: null, offerType: null,
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
  sSuccess.value = `${sForm.firstName} ${sForm.lastName} (${sid})`
  setTimeout(() => { showStudentDrawer.value = false; sSuccess.value = '' }, 1800)
}

// ── Partners tab (real API) ───────────────────────────────────────────────────
const partners        = ref([])
const partnersLoading = ref(false)
const partnersError   = ref('')
const managingPartner = ref(null)
const partnerUsers    = ref([])
const partnerUsersLoading = ref(false)

// Create wizard
const showPartnerWizard = ref(false)

// Manage panel tabs
const MANAGE_TABS = [
  { k: 'profile',  label: 'Profile' },
  { k: 'users',    label: 'Users' },
  { k: 'core',     label: 'Core Programmes' },
  { k: 'custom',   label: 'Custom Programmes' },
  { k: 'students', label: 'Students' },
]
const manageTab = ref('users')

// Add user to partner
const newUserUsername = ref('')
const newUserEmail    = ref('')
const newUserPassword = ref('')
const addingUser      = ref(false)
const addUserError    = ref('')

// Edit / reset-password state
const editingUserId  = ref(null)
const editForm       = reactive({ username: '', firstName: '', lastName: '' })
const editSaving     = ref(false)
const editError      = ref('')
const resetUserId    = ref(null)
const resetPassword  = ref('')
const resettingUserId = ref(null)

async function loadPartners() {
  partnersLoading.value = true
  partnersError.value = ''
  try {
    const res = await apiClient.get('/v1/admin/school/partners')
    partners.value = res.data.items ?? []
  } catch (e) {
    partnersError.value = e.response?.data?.message ?? e.message ?? 'Failed to load partners'
  } finally {
    partnersLoading.value = false
  }
}

function openCreatePartner() {
  showPartnerWizard.value = true
}

async function onWizardCreated(partnerId) {
  await loadPartners()
  const p = partners.value.find(x => x.partnerId === partnerId)
  if (p) {
    manageTab.value = 'users'
    await openManagePartner(p)
  }
}

function onWizardClose() {
  showPartnerWizard.value = false
}

async function disablePartner(p) {
  try { await apiClient.post(`/v1/admin/school/partners/${p.partnerId}/disable`); await loadPartners() }
  catch (e) { partnersError.value = e.response?.data?.message ?? 'Failed' }
}
async function enablePartner(p) {
  try { await apiClient.post(`/v1/admin/school/partners/${p.partnerId}/enable`); await loadPartners() }
  catch (e) { partnersError.value = e.response?.data?.message ?? 'Failed' }
}

async function openManagePartner(p) {
  managingPartner.value = p
  newUserUsername.value = ''; newUserEmail.value = ''; newUserPassword.value = ''; addUserError.value = ''
  editingUserId.value = null; editError.value = ''
  resetUserId.value = null; resetPassword.value = ''
  partnerUsersLoading.value = true
  try {
    const res = await apiClient.get(`/v1/admin/school/partners/${p.partnerId}/users`)
    partnerUsers.value = res.data.items ?? []
  } catch { partnerUsers.value = [] }
  finally { partnerUsersLoading.value = false }
}

function startEditUser(u) {
  editingUserId.value = u.userId
  editForm.username  = u.username ?? ''
  editForm.firstName = u.firstName ?? ''
  editForm.lastName  = u.lastName ?? ''
  editError.value = ''
}
function cancelEditUser() {
  editingUserId.value = null
  editError.value = ''
}
async function saveEditUser() {
  const u = partnerUsers.value.find(x => x.userId === editingUserId.value)
  if (!u) return
  editSaving.value = true; editError.value = ''
  const payload = {}
  const newUsername = editForm.username.trim()
  if (newUsername && newUsername !== u.username) payload.username = newUsername
  if ((editForm.firstName ?? '') !== (u.firstName ?? '')) payload.firstName = editForm.firstName.trim()
  if ((editForm.lastName  ?? '') !== (u.lastName  ?? '')) payload.lastName  = editForm.lastName.trim()
  if (Object.keys(payload).length === 0) { editSaving.value = false; editingUserId.value = null; return }
  try {
    await apiClient.patch(`/v1/admin/school/partners/${managingPartner.value.partnerId}/users/${u.userId}`, payload)
    editingUserId.value = null
    await openManagePartner(managingPartner.value)
  } catch (e) {
    editError.value = e.response?.data ?? e.message ?? 'Failed to update user'
  } finally {
    editSaving.value = false
  }
}

async function resetUserPassword(u) {
  resettingUserId.value = u.userId
  resetUserId.value = null; resetPassword.value = ''
  try {
    const res = await apiClient.post(`/v1/admin/school/partners/${managingPartner.value.partnerId}/users/${u.userId}/reset-password`)
    resetUserId.value = u.userId
    resetPassword.value = res.data.temporaryPassword
  } catch (e) {
    addUserError.value = e.response?.data?.message ?? e.response?.data ?? 'Failed to reset password'
  } finally {
    resettingUserId.value = null
  }
}

function copyResetPassword() {
  navigator.clipboard.writeText(resetPassword.value).catch(() => {})
}

async function addUserToPartner() {
  if (!newUserUsername.value.trim()) return
  addingUser.value = true; addUserError.value = ''; newUserPassword.value = ''
  try {
    const res = await apiClient.post(`/v1/admin/school/partners/${managingPartner.value.partnerId}/users`, {
      username: newUserUsername.value.trim(),
      email: newUserEmail.value.trim() || undefined,
    })
    newUserPassword.value = res.data.temporaryPassword
    newUserEmail.value = ''
    await openManagePartner(managingPartner.value)
    await loadPartners()
  } catch (e) {
    addUserError.value = e.response?.data ?? e.message ?? 'Failed to add user'
  } finally {
    addingUser.value = false
  }
}

async function disableUser(u) {
  try {
    await apiClient.post(`/v1/admin/school/partners/${managingPartner.value.partnerId}/users/${u.userId}/disable`)
    await openManagePartner(managingPartner.value)
  } catch (e) { addUserError.value = e.response?.data?.message ?? 'Failed' }
}
async function enableUser(u) {
  try {
    await apiClient.post(`/v1/admin/school/partners/${managingPartner.value.partnerId}/users/${u.userId}/enable`)
    await openManagePartner(managingPartner.value)
  } catch (e) { addUserError.value = e.response?.data?.message ?? 'Failed' }
}

function copyPassword() {
  navigator.clipboard.writeText(newUserPassword.value).catch(() => {})
}

// Load partners when Partners tab is activated
watch(tab, (t) => { if (t === 'partners') loadPartners() })

// ── Messages tab ──────────────────────────────────────────────────────────────
function studentNameById(sid) {
  const s = students.find(s => s.studentId === sid)
  return s ? `${s.firstName} ${s.lastName}` : sid
}
function studentPartnerById(sid) {
  return students.find(s => s.studentId === sid)?.partner || '—'
}

const pendingMsgCount = computed(() => {
  const openT = tickets.filter(t => t.status === 'open').length
  const pendA = absences.filter(a => a.status === 'pending').length
  return openT + pendA
})

// Announcements
const anForm = reactive({ title: '', body: '', targetStudentId: null })
function postAnnouncement() {
  announcements.push({
    id: nextAnnouncementId(),
    title: anForm.title,
    body: anForm.body,
    targetStudentId: anForm.targetStudentId,
    createdAt: new Date().toISOString(),
  })
  Object.assign(anForm, { title: '', body: '', targetStudentId: null })
}

// Support tickets
const expandedAdminTickets = ref([])
const adminReplyTexts = reactive({})
function toggleAdminTicket(id) {
  const i = expandedAdminTickets.value.indexOf(id)
  if (i === -1) expandedAdminTickets.value.push(id)
  else expandedAdminTickets.value.splice(i, 1)
}
function adminReply(t) {
  const text = adminReplyTexts[t.id]
  if (!text) return
  t.replies.push({ from: 'admin', text, at: new Date().toISOString() })
  adminReplyTexts[t.id] = ''
}
function resolveTicket(t) { t.status = 'resolved' }

// Absences
function acknowledgeAbsence(a) { a.status = 'acknowledged' }

function logout() { auth.logout(); router.push('/login') }
</script>

<style scoped>
.page-wrapper { min-height: 100vh; background: #f2f5f9; }

/* Navbar */
.navbar { background: #003366; color: #fff; display: flex; align-items: center; justify-content: space-between; padding: 0.85rem 2rem; gap: 1rem; }
.brand-text { font-size: 1.05rem; font-weight: 700; white-space: nowrap; }
.nav-links { display: flex; gap: 0.25rem; flex: 1; padding: 0 1rem; }
.nav-link { color: rgba(255,255,255,0.75); text-decoration: none; padding: 0.35rem 0.9rem; border-radius: 5px; font-size: 0.88rem; transition: background 0.15s, color 0.15s; }
.nav-link:hover, .nav-link.router-link-active { background: rgba(255,255,255,0.15); color: #fff; }
.nav-right { display: flex; align-items: center; gap: 1rem; }
.nav-user { font-size: 0.85rem; opacity: 0.85; white-space: nowrap; }
.btn-logout { background: transparent; border: 1.5px solid rgba(255,255,255,0.55); color: #fff; padding: 0.3rem 0.85rem; border-radius: 5px; cursor: pointer; font-size: 0.82rem; }
.btn-logout:hover { background: rgba(255,255,255,0.13); }

/* Tabs */
.tab-bar { background: #fff; border-bottom: 2px solid #e8edf4; display: flex; padding: 0 2rem; }
.tab-btn { background: none; border: none; padding: 0.85rem 1.25rem; font-size: 0.9rem; font-weight: 600; color: #888; cursor: pointer; border-bottom: 3px solid transparent; margin-bottom: -2px; transition: color 0.15s, border-color 0.15s; }
.tab-btn.active { color: #003366; border-bottom-color: #003366; }
.tab-btn:hover:not(.active) { color: #333; }

/* Container */
.container { max-width: 1100px; margin: 2rem auto; padding: 0 1.5rem; }

.page-header { display: flex; align-items: flex-start; justify-content: space-between; margin-bottom: 1.25rem; }
.page-title { font-size: 1.5rem; font-weight: 700; color: #003366; }
.page-sub { font-size: 0.82rem; color: #888; margin-top: 0.2rem; }

/* Buttons */
.btn-primary { background: #003366; color: #fff; border: none; border-radius: 7px; padding: 0.6rem 1.25rem; font-size: 0.9rem; font-weight: 600; cursor: pointer; }
.btn-primary:hover { background: #0055a5; }
.btn-primary-sm { background: #003366; color: #fff; border: none; border-radius: 5px; padding: 0.38rem 0.85rem; font-size: 0.82rem; cursor: pointer; }
.btn-primary-sm:hover { background: #0055a5; }
.btn-ghost { background: none; border: 1.5px solid #e0a8a8; color: #991b1b; border-radius: 6px; padding: .42rem .85rem; font-size: .8rem; cursor: pointer; font-weight: 600; white-space: nowrap; }
.btn-ghost:hover { background: #fee2e2; }
.btn-sm { background: #e8f0f8; color: #003366; border: 1px solid #c5d8f0; border-radius: 5px; padding: 0.3rem 0.75rem; font-size: 0.8rem; cursor: pointer; white-space: nowrap; text-decoration: none; display: inline-block; }
.btn-sm:hover { background: #d0e4f5; }
.btn-sm-teal { background: #e0f5f0; color: #0d6b55; border: 1px solid #a8ddd0; border-radius: 5px; padding: 0.3rem 0.75rem; font-size: 0.8rem; cursor: pointer; white-space: nowrap; }
.btn-sm-teal:hover { background: #c0ece3; }
.btn-danger-sm { background: #fdecea; color: #c0392b; border: 1px solid #f5c0bb; border-radius: 5px; padding: 0.28rem 0.7rem; font-size: 0.78rem; cursor: pointer; }
.btn-danger-sm:hover { background: #f9d4d0; }
.btn-x { background: none; border: none; color: #aaa; cursor: pointer; font-size: 0.85rem; padding: 0 4px; }
.btn-x:hover { color: #c0392b; }
.btn-add-subj { background: #003366; color: #fff; border: none; border-radius: 5px; padding: 0.35rem 0.75rem; font-size: 0.8rem; cursor: pointer; white-space: nowrap; }
.btn-add-subj:hover { background: #0055a5; }

/* Filters */
.filters-bar { display: flex; flex-wrap: wrap; gap: 0.65rem; align-items: center; margin-bottom: 1.1rem; }
.search-wrap { position: relative; flex: 1 1 200px; min-width: 160px; max-width: 320px; display: flex; align-items: center; }
.search-icon { position: absolute; left: 0.65rem; top: 50%; transform: translateY(-50%); width: 15px; height: 15px; pointer-events: none; }
.search-input { width: 100%; padding: 0.55rem 2rem 0.55rem 2.1rem; border: 1.5px solid #d0dbe8; border-radius: 8px; font-size: 0.87rem; outline: none; box-sizing: border-box; background: #fff; color: #222; }
.search-input:focus { border-color: #0055a5; box-shadow: 0 0 0 3px rgba(0,85,165,.08); }
.search-clear { position: absolute; right: 0.5rem; top: 50%; transform: translateY(-50%); background: none; border: none; color: #aaa; cursor: pointer; font-size: 0.85rem; padding: 0.2rem 0.3rem; line-height: 1; }
.search-clear:hover { color: #555; }
.search-hint-wrap { position: relative; display: flex; align-items: center; flex-shrink: 0; }
.search-hint-icon { width: 18px; height: 18px; border-radius: 50%; background: #d0dbe8; color: #555; font-size: .72rem; font-weight: 700; display: flex; align-items: center; justify-content: center; cursor: default; user-select: none; flex-shrink: 0; }
.search-hint-wrap:hover .search-hint-tip { display: block; }
.search-hint-tip { display: none; position: absolute; top: calc(100% + 7px); left: 50%; transform: translateX(-50%); background: #1e2d3d; color: #eee; font-size: .78rem; line-height: 1.6; padding: .65rem .85rem; border-radius: 8px; white-space: nowrap; box-shadow: 0 4px 16px rgba(0,0,0,.22); z-index: 300; pointer-events: none; }
.search-hint-tip::before { content: ''; position: absolute; bottom: 100%; left: 50%; transform: translateX(-50%); border: 5px solid transparent; border-bottom-color: #1e2d3d; }
.search-hint-tip strong { color: #fff; }
.search-hint-tip code { background: rgba(255,255,255,.15); border-radius: 3px; padding: 0 4px; font-family: ui-monospace, monospace; color: #adf; }
.search-hint-tip em { color: #ccc; font-style: normal; }
.filter-select { padding: 0.52rem 0.75rem; border: 1.5px solid #d0dbe8; border-radius: 7px; font-size: 0.85rem; background: #fff; min-width: 130px; cursor: pointer; outline: none; }
.filter-select:focus { border-color: #0055a5; }
.filter-count { margin-left: auto; font-size: .8rem; color: #999; white-space: nowrap; }
/* Multi-select dropdown */
.ms-wrap { position: relative; }
.ms-btn { display: flex; align-items: center; gap: .4rem; background: #fff; border: 1.5px solid #d0dbe8; color: #333; border-radius: 7px; padding: .42rem .85rem; font-size: .85rem; cursor: pointer; white-space: nowrap; font-family: inherit; min-width: 145px; justify-content: space-between; }
.ms-btn:hover { border-color: #0055a5; background: #f7fbff; }
.ms-btn-active { border-color: #0055a5; color: #0055a5; font-weight: 700; background: #eef4fb; }
.ms-caret { font-size: .9rem; color: #888; }
.ms-dropdown { position: absolute; top: calc(100% + 4px); left: 0; min-width: 230px; background: #fff; border: 1.5px solid #d0dbe8; border-radius: 9px; box-shadow: 0 6px 22px rgba(0,0,0,.12); z-index: 200; padding: .45rem 0 .4rem; max-height: 260px; overflow-y: auto; }
.ms-item { display: flex; align-items: center; gap: .6rem; padding: .42rem .85rem; cursor: pointer; font-size: .86rem; color: #333; }
.ms-item:hover { background: #f0f5fb; }
.ms-item input[type=checkbox] { width: 15px; height: 15px; cursor: pointer; accent-color: #0055a5; flex-shrink: 0; }
.ms-clear { display: block; width: calc(100% - 1.7rem); margin: .4rem .85rem 0; background: none; border: 1px solid #e0a8a8; color: #991b1b; border-radius: 5px; padding: .3rem 0; font-size: .78rem; cursor: pointer; font-weight: 600; }
.ms-clear:hover { background: #fee2e2; }

/* Table */
.table-wrap { background: #fff; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.07); overflow: auto; }
.data-table { width: 100%; border-collapse: collapse; font-size: 0.88rem; }
.data-table th { text-align: left; padding: 0.75rem 1rem; font-size: 0.74rem; text-transform: uppercase; letter-spacing: 0.05em; color: #666; border-bottom: 2px solid #e8edf4; background: #fafbfc; white-space: nowrap; }
.data-row td { padding: 0.72rem 1rem; border-bottom: 1px solid #f0f3f7; }
.data-row:last-child td { border-bottom: none; }
.data-row:hover td { background: #f7f9fb; }
.mono { font-family: ui-monospace, monospace; font-size: 0.82rem; color: #555; }
.empty-row { text-align: center; padding: 2.5rem !important; color: #aaa; font-style: italic; }
/* Status badges */
.s-badge { display: inline-block; padding: 2px 10px; border-radius: 20px; font-size: 0.74rem; font-weight: 700; white-space: nowrap; }
.s-new       { background: #f0f3f7; color: #888; border: 1px solid #d0d7e0; }
.s-offer     { background: #e8f4fd; color: #1a6ca8; border: 1px solid #b8d9f5; }
.s-admission { background: #dbeafe; color: #1d4ed8; border: 1px solid #93c5fd; }
.s-confirmed { background: #fef3c7; color: #92400e; border: 1px solid #fcd34d; }
.s-graded    { background: #e0f5f0; color: #0d6b55; border: 1px solid #a8ddd0; }
.s-approved  { background: #d1fae5; color: #065f46; border: 1px solid #6ee7b7; }
.next-action-cell { font-size: 0.8rem; color: #666; font-style: italic; }
.btn-view-link { background: #e8f0f8; color: #003366; border: 1px solid #c5d8f0; border-radius: 5px; padding: 0.28rem 0.75rem; font-size: 0.8rem; font-weight: 600; cursor: pointer; white-space: nowrap; text-decoration: none; display: inline-block; }
.btn-view-link:hover { background: #d0e4f5; }

/* ── Student cards ─────────────────────────────────────────────────────────── */
.empty-state-card { background: #fff; border-radius: 10px; box-shadow: 0 2px 8px rgba(0,0,0,0.06); padding: 2.5rem; text-align: center; color: #aaa; font-style: italic; }
.student-card { background: #fff; border-radius: 12px; box-shadow: 0 2px 10px rgba(0,0,0,0.06); overflow: hidden; margin-bottom: 0.75rem; }
.sc-header { display: flex; align-items: center; justify-content: space-between; padding: 0.9rem 1.25rem; background: #003366; }
.sc-id-name { display: flex; align-items: center; gap: 0.55rem; flex-wrap: wrap; }
.sc-sid { font-family: ui-monospace, monospace; font-size: 0.88rem; color: #a8c8ff; font-weight: 600; }
.sc-sep { color: rgba(255,255,255,.4); }
.sc-name { font-size: 1rem; font-weight: 700; color: #fff; }
.sc-partner-chip { font-size: 0.72rem; font-weight: 700; padding: 1px 8px; background: rgba(255,255,255,.12); color: #cce4ff; border: 1px solid rgba(255,255,255,.2); border-radius: 10px; }
.sc-header-right { display: flex; align-items: center; gap: 0.75rem; flex-shrink: 0; }
.sc-summary { font-size: 0.78rem; color: rgba(255,255,255,.7); font-weight: 500; white-space: nowrap; }
.sc-chevron { font-size: 1.3rem; color: rgba(255,255,255,.65); transition: transform 0.2s; display: inline-block; line-height: 1; margin-left: .25rem; }
.sc-chevron.collapsed { transform: rotate(-90deg); }
.abs-badge { background: #fff3cd; color: #856404; border: 1px solid #fcd34d; padding: 1px 7px; border-radius: 10px; font-size: .7rem; font-weight: 700; }

/* Enrollment table inside card */
.sc-body { overflow: hidden; }
.enr-table-wrap { overflow-x: auto; }
.enr-table { width: 100%; border-collapse: collapse; min-width: 860px; }
.enr-table thead th { background: #f0f4f8; color: #555; font-size: 0.72rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.04em; padding: 0.55rem 0.9rem; text-align: left; border-bottom: 2px solid #e0e8f4; white-space: nowrap; }
.enr-row { border-bottom: 1px solid #f0f3f7; }
.enr-row:last-child { border-bottom: none; }
.enr-row td { padding: 0.7rem 0.9rem; vertical-align: top; }
.th-prog  { width: 22%; }
.th-status { width: 14%; }
.th-acad  { width: 18%; }
.th-pay   { width: 17%; }
.th-rel   { width: 16%; }
.th-act   { width: 90px; }

.td-prog {}
.prog-name-main { font-size: 0.88rem; font-weight: 700; color: #003366; }
.prog-major-sub { font-size: 0.8rem; color: #555; margin-top: 1px; margin-bottom: 0.45rem; }
.enr-doc-list { display: flex; flex-wrap: wrap; gap: 0.25rem; }
.doc-chip { font-size: 0.72rem; padding: 1px 7px; border-radius: 10px; white-space: nowrap; }
.doc-chip-on  { background: #e0f5f0; color: #0d6b55; border: 1px solid #a8ddd0; }
.doc-chip-off { background: #f0f3f7; color: #bbb;    border: 1px solid #e0e4ea; }

.td-status { vertical-align: middle; }
.status-sel-inline { width: 100%; padding: 0.4rem 0.5rem; border: 1.5px solid #ccc; border-radius: 6px; font-size: 0.82rem; background: #fff; }
.status-sel-inline:focus { border-color: #003366; outline: none; }

.ap-row { display: flex; align-items: center; gap: 0.4rem; flex-wrap: wrap; }
.ap-lbl { font-size: 0.75rem; color: #888; font-weight: 600; white-space: nowrap; }
.course-inp { display: flex; align-items: center; gap: 0.25rem; }
.inp-num-sm { width: 44px; padding: 0.28rem 0.3rem; border: 1.5px solid #ccc; border-radius: 5px; font-size: 0.82rem; text-align: center; }
.inp-num-sm:focus { border-color: #003366; outline: none; }
.num-sep { color: #888; }
.sel-sm { padding: 0.28rem 0.4rem; border: 1.5px solid #ccc; border-radius: 5px; font-size: 0.8rem; background: #fff; }
.sel-sm:focus { border-color: #003366; outline: none; }
.rel-yes { font-size: 0.76rem; color: #0d6b55; font-weight: 700; }
.rel-no  { font-size: 0.76rem; color: #bbb; }
.confirmed-chip { font-size: 0.72rem; font-weight: 700; padding: 2px 8px; background: #d1fae5; color: #065f46; border: 1px solid #6ee7b7; border-radius: 20px; }

.td-pay {}
.pay-section { display: flex; flex-direction: column; gap: 0.2rem; }

.td-rel { display: flex; flex-direction: column; }
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

.td-act { vertical-align: middle; text-align: center; }
.sc-footer { display: flex; align-items: center; justify-content: flex-end; padding: 0.65rem 1.25rem; background: #fafbfc; border-top: 1px solid #f0f3f7; }
.sc-detail-link { font-size: 0.82rem; color: #003366; font-weight: 600; text-decoration: none; }
.sc-detail-link:hover { text-decoration: underline; }

.release-cell { display: flex; gap: 0.3rem; flex-wrap: nowrap; }
.btn-rel-off { background: #f0f3f7; color: #888; border: 1px solid #d0d7e0; border-radius: 4px; padding: 0.2rem 0.55rem; font-size: 0.73rem; cursor: pointer; white-space: nowrap; }
.btn-rel-off:hover { background: #e0f5f0; border-color: #a8ddd0; color: #0d6b55; }
.btn-rel-on  { background: #e0f5f0; color: #0d6b55; border: 1px solid #a8ddd0; border-radius: 4px; padding: 0.2rem 0.55rem; font-size: 0.73rem; cursor: pointer; font-weight: 700; white-space: nowrap; }
.btn-rel-on:hover { background: #c8ede5; }

/* Inline form card */
.inline-form-card { background: #fff; border: 1.5px solid #e0e8f0; border-radius: 9px; padding: 1.25rem 1.5rem; margin-bottom: 1.2rem; }
.inline-form-card h3 { font-size: 0.95rem; font-weight: 700; color: #003366; margin: 0 0 0.9rem; }
.inline-form-actions { display: flex; gap: 0.65rem; justify-content: flex-end; margin-top: 0.9rem; }
.row-3 { display: flex; gap: 0.75rem; flex-wrap: wrap; }
.field { display: flex; flex-direction: column; gap: 0.3rem; flex: 1 1 180px; }
.field label { font-size: 0.82rem; font-weight: 600; color: #444; }
.req { color: #c0392b; }
.field input, .field select { padding: 0.58rem 0.75rem; border: 1.5px solid #ccc; border-radius: 7px; font-size: 0.9rem; font-family: inherit; outline: none; }
.field input:focus, .field select:focus { border-color: #0055a5; }

/* Programme accordion */
.prog-card { background: #fff; border-radius: 10px; box-shadow: 0 2px 8px rgba(0,0,0,0.06); margin-bottom: 0.85rem; overflow: hidden; }
.prog-header { display: flex; align-items: center; justify-content: space-between; padding: 0.9rem 1.25rem; cursor: pointer; user-select: none; }
.prog-header:hover { background: #f7f9fb; }
.prog-title { display: flex; align-items: center; gap: 0.6rem; font-size: 0.95rem; }
.prog-expand { color: #888; font-size: 0.85rem; width: 12px; }
.code-badge { background: #e8f0f8; color: #003366; border-radius: 4px; padding: 1px 7px; font-size: 0.75rem; font-weight: 700; }
.count-badge { background: #f0f3f7; color: #888; border-radius: 4px; padding: 1px 7px; font-size: 0.75rem; }
.prog-body { padding: 0.75rem 1.25rem 1rem; border-top: 1px solid #f0f3f7; }

.maj-block { border: 1px solid #e8edf4; border-radius: 7px; margin-bottom: 0.6rem; overflow: hidden; }
.maj-header { display: flex; align-items: center; justify-content: space-between; padding: 0.6rem 1rem; background: #fafbfc; }
.maj-title { display: flex; align-items: center; gap: 0.5rem; font-size: 0.88rem; font-weight: 600; cursor: pointer; }

.subjects-block { padding: 0.5rem 1rem 0.75rem; }
.subj-row { display: flex; align-items: center; gap: 0.75rem; padding: 0.3rem 0; border-bottom: 1px solid #f5f6f8; font-size: 0.85rem; }
.subj-row:last-of-type { border-bottom: none; }
.subj-name { flex: 1; }
.subj-credits { color: #888; font-size: 0.8rem; white-space: nowrap; }

.add-subj-row { display: flex; gap: 0.5rem; align-items: center; margin-top: 0.6rem; }
.subj-input { flex: 1; padding: 0.38rem 0.65rem; border: 1.5px solid #ccc; border-radius: 5px; font-size: 0.85rem; outline: none; }
.subj-input:focus { border-color: #003366; }
.subj-credits-input { width: 60px; padding: 0.38rem 0.5rem; border: 1.5px solid #ccc; border-radius: 5px; font-size: 0.85rem; outline: none; text-align: center; }
.subj-credits-input:focus { border-color: #003366; }

.add-major-row { display: flex; gap: 0.6rem; align-items: center; margin-top: 0.75rem; padding-top: 0.75rem; border-top: 1px dashed #e0e8f0; }
.maj-input { flex: 1; padding: 0.42rem 0.75rem; border: 1.5px solid #ccc; border-radius: 6px; font-size: 0.87rem; outline: none; }
.maj-input:focus { border-color: #003366; }

/* Drawers */
.overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.38); z-index: 100; }
.drawer { position: fixed; top: 0; right: 0; bottom: 0; width: 420px; max-width: 95vw; background: #fff; z-index: 101; display: flex; flex-direction: column; box-shadow: -4px 0 24px rgba(0,0,0,0.15); }
.drawer-header { display: flex; align-items: center; justify-content: space-between; padding: 1.2rem 1.5rem; border-bottom: 1.5px solid #e8edf4; flex-shrink: 0; }
.drawer-header h2 { font-size: 1.05rem; font-weight: 700; color: #003366; }
.drawer-close { background: none; border: none; font-size: 1.1rem; color: #888; cursor: pointer; }
.drawer-close:hover { color: #333; }
.drawer-form { flex: 1; overflow-y: auto; padding: 1.2rem 1.5rem; display: flex; flex-direction: column; gap: 0.9rem; }
.row-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 0.75rem; }
.hint-text { font-size: 0.8rem; color: #888; margin: 0; }
.success-msg { background: #eafaf1; border: 1.5px solid #2ecc71; border-radius: 7px; padding: 0.7rem 1rem; color: #1e8449; font-size: 0.86rem; }
.drawer-actions { display: flex; gap: 0.75rem; justify-content: flex-end; padding-top: 0.5rem; }
.btn-cancel { padding: 0.62rem 1.2rem; background: #f2f5f9; border: 1.5px solid #ccc; border-radius: 7px; font-size: 0.9rem; cursor: pointer; color: #555; }
.btn-save { padding: 0.62rem 1.4rem; background: #003366; color: #fff; border: none; border-radius: 7px; font-size: 0.9rem; font-weight: 600; cursor: pointer; }
.btn-save:hover { background: #0055a5; }

/* Modal */
.modal { position: fixed; top: 50%; left: 50%; transform: translate(-50%, -50%); width: 680px; max-width: 96vw; max-height: 88vh; background: #fff; z-index: 101; border-radius: 12px; box-shadow: 0 12px 48px rgba(0,0,0,0.22); display: flex; flex-direction: column; }
.modal-header { display: flex; align-items: flex-start; justify-content: space-between; padding: 1.2rem 1.5rem; border-bottom: 1.5px solid #e8edf4; flex-shrink: 0; }
.modal-header h2 { font-size: 1.05rem; font-weight: 700; color: #003366; }
.modal-sub { font-size: 0.82rem; color: #888; margin-top: 0.15rem; }
.modal-body { flex: 1; overflow-y: auto; padding: 1.2rem 1.5rem; }
.modal-footer { padding: 0.9rem 1.5rem; border-top: 1px solid #e8edf4; display: flex; justify-content: flex-end; flex-shrink: 0; }

.mprog-block { margin-bottom: 1.25rem; }
.mprog-title { display: flex; align-items: center; gap: 0.5rem; margin-bottom: 0.6rem; font-size: 0.9rem; }
.mmaj-row { border: 1px solid #e8edf4; border-radius: 7px; margin-bottom: 0.5rem; overflow: hidden; }
.toggle-label { display: flex; align-items: center; gap: 0.6rem; padding: 0.6rem 1rem; font-size: 0.88rem; cursor: pointer; }
.toggle-label input[type=checkbox] { width: 16px; height: 16px; cursor: pointer; }
.mmaj-actions { display: flex; align-items: center; gap: 0.5rem; padding: 0 0.75rem 0.5rem 2.6rem; }
.badge-cloned { background: #e0f5f0; color: #0d6b55; border: 1px solid #a8ddd0; border-radius: 20px; padding: 1px 9px; font-size: 0.75rem; font-weight: 600; }

.clone-editor { background: #f7fbff; border-top: 1px solid #d0e4f5; padding: 0.75rem 1rem; }
.clone-editor-header { font-size: 0.8rem; color: #555; margin-bottom: 0.6rem; }

/* Tab badge */
.tab-badge { display: inline-block; background: #e74c3c; color: #fff; border-radius: 10px; font-size: 0.7rem; font-weight: 700; padding: 1px 6px; margin-left: 5px; vertical-align: middle; }

/* Messages tab */
.msg-section { margin-bottom: 2rem; }
.section-title { font-size: 1.15rem; font-weight: 700; color: #003366; margin: 0 0 0.85rem; }
.msg-card { background: #fff; border-radius: 10px; padding: 1.25rem 1.5rem; box-shadow: 0 2px 8px rgba(0,0,0,.06); margin-bottom: 0.85rem; }
.msg-card h3 { font-size: 0.93rem; font-weight: 700; color: #333; margin: 0 0 0.85rem; }
.msg-card .field { display: flex; flex-direction: column; gap: .3rem; }
.msg-card .field label { font-size: .82rem; font-weight: 600; color: #444; }
.msg-card .field input, .msg-card .field textarea, .msg-card .field select { padding: .55rem .75rem; border: 1.5px solid #ccc; border-radius: 6px; font-size: .9rem; font-family: inherit; outline: none; }
.msg-card .field input:focus, .msg-card .field textarea:focus, .msg-card .field select:focus { border-color: #003366; }
.empty-msg { color: #aaa; font-style: italic; font-size: .88rem; padding: .5rem 0; }

/* Ticket cards */
.ticket-card { padding: 0; overflow: hidden; }
.ticket-meta { display: flex; align-items: center; gap: .75rem; padding: .85rem 1.25rem; cursor: pointer; background: #fafbfc; }
.ticket-meta:hover { background: #f0f4f8; }
.ticket-subject-admin { flex: 1; font-weight: 600; font-size: .9rem; color: #222; }
.ticket-student-admin { font-size: .82rem; color: #666; white-space: nowrap; }
.ticket-thread-admin { padding: .85rem 1.25rem; display: flex; flex-direction: column; gap: .5rem; }
.reply-adm { padding: .55rem .85rem; border-radius: 7px; font-size: .87rem; }
.reply-adm-student { background: #e8f0fe; border-left: 3px solid #4a90d9; }
.reply-adm-admin   { background: #e8f6e9; border-left: 3px solid #2d9e53; }
.reply-adm-partner { background: #fff8e6; border-left: 3px solid #e6a817; }
.reply-from-lbl { font-weight: 700; margin-right: .5rem; }
.reply-at-lbl   { font-size: .75rem; color: #888; }
.reply-adm p { margin: .3rem 0 0; }
.admin-reply-box { display: flex; flex-direction: column; gap: .5rem; margin-top: .5rem; }
.admin-reply-box textarea { padding: .5rem .7rem; border: 1.5px solid #ccc; border-radius: 6px; font-size: .88rem; resize: vertical; font-family: inherit; }
.admin-reply-actions { display: flex; gap: .5rem; align-items: center; }

/* Transitions */
.fade-enter-active, .fade-leave-active { transition: opacity 0.2s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
.slide-enter-active, .slide-leave-active { transition: transform 0.25s ease; }
.slide-enter-from, .slide-leave-to { transform: translateX(100%); }
.modal-pop-enter-active, .modal-pop-leave-active { transition: opacity 0.2s, transform 0.2s; }
.modal-pop-enter-from, .modal-pop-leave-to { opacity: 0; transform: translate(-50%, -48%) scale(0.96); }

/* ═══════ Partners tab ═══════ */
.actions-cell { display: flex; gap: 0.4rem; white-space: nowrap; }
.loading-row { color: #888; font-style: italic; padding: 1.5rem; text-align: center; }
.err-banner { background: #fef2f2; border: 1.5px solid #fca5a5; border-radius: 7px;
  padding: 0.65rem 1rem; color: #b91c1c; font-size: 0.86rem; margin-bottom: 1rem; }

.badge-enabled  { background: #d1fae5; color: #065f46; border: 1px solid #6ee7b7;
  border-radius: 20px; padding: 2px 10px; font-size: 0.74rem; font-weight: 700; }
.badge-disabled { background: #fdecea; color: #c0392b; border: 1px solid #f5c0bb;
  border-radius: 20px; padding: 2px 10px; font-size: 0.74rem; font-weight: 700; }

.btn-warn { background: #fef3c7; color: #92400e; border: 1px solid #fcd34d;
  border-radius: 5px; padding: 0.3rem 0.75rem; font-size: 0.8rem; cursor: pointer; font-weight: 600; }
.btn-warn:hover { background: #fde68a; }
.btn-ok { background: #d1fae5; color: #065f46; border: 1px solid #6ee7b7;
  border-radius: 5px; padding: 0.3rem 0.75rem; font-size: 0.8rem; cursor: pointer; font-weight: 600; }
.btn-ok:hover { background: #a7f3d0; }

/* Manage panel */
.manage-panel { background: #fff; border: 1.5px solid #dde6f0; border-radius: 10px;
  padding: 1.25rem 1.5rem; margin-top: 1rem; box-shadow: 0 2px 10px rgba(0,0,0,0.06); }
.manage-panel-header { display: flex; justify-content: space-between; align-items: center;
  border-bottom: 1.5px solid #e8edf4; padding-bottom: 0.75rem; margin-bottom: 1rem; }
.manage-panel-header h2 { font-size: 1.1rem; font-weight: 700; color: #003366; margin: 0; }
.btn-close-panel { background: none; border: 1.5px solid #d0d7e0; border-radius: 5px;
  padding: 0.3rem 0.75rem; font-size: 0.82rem; cursor: pointer; color: #666; }
.btn-close-panel:hover { background: #f0f3f7; }

.manage-section-title { font-size: 0.72rem; font-weight: 700; text-transform: uppercase;
  letter-spacing: 0.06em; color: #999; margin-bottom: 0.5rem; }

.manage-tab-bar { display: flex; gap: 0; border-bottom: 1.5px solid #e8edf4; margin-bottom: 1rem; }
.manage-tab-btn { background: none; border: none; padding: .55rem .95rem; font-size: .88rem; font-weight: 600; color: #5f6e85; cursor: pointer; border-bottom: 2.5px solid transparent; margin-bottom: -1.5px; }
.manage-tab-btn:hover { color: #0a264f; }
.manage-tab-btn.active { color: #0a264f; border-bottom-color: #0a264f; }
.manage-section { padding: 0; }

.add-user-row { display: flex; gap: 0.55rem; align-items: center; margin-top: 0.75rem;
  padding-top: 0.75rem; border-top: 1px dashed #dde6f0; }
.inp-add { flex: 1; padding: 0.5rem 0.75rem; border: 1.5px solid #ccc; border-radius: 6px;
  font-size: 0.88rem; outline: none; }
.inp-add:focus { border-color: #0055a5; }

.password-reveal { margin-top: 0.75rem; padding: 0.85rem 1rem; background: #eafaf1;
  border: 1.5px solid #2ecc71; border-radius: 8px; font-size: 0.88rem; }
.password-reveal code { background: #fff; padding: 2px 10px; border-radius: 5px;
  font-family: ui-monospace, monospace; font-size: 0.95rem; margin-left: 0.5rem;
  border: 1px solid #d0dbe8; color: #003366; font-weight: 700; }
.password-note { font-size: 0.78rem; color: #666; margin: 0.5rem 0 0; font-style: italic; }
.btn-copy { background: #003366; color: #fff; border: none; border-radius: 5px;
  padding: 0.28rem 0.75rem; font-size: 0.78rem; cursor: pointer; margin-left: 0.5rem; }
.btn-copy:hover { background: #0055a5; }

/* Create drawer feedback */
.success-box { background: #eafaf1; border: 1.5px solid #2ecc71; border-radius: 8px;
  padding: 0.85rem 1rem; font-size: 0.86rem; }
.success-box p { margin: 0.25rem 0; }
.success-box code { background: #fff; padding: 2px 8px; border-radius: 4px;
  font-family: ui-monospace, monospace; border: 1px solid #d0dbe8; color: #003366; font-weight: 700; }
.error-msg-inline { color: #b91c1c; font-size: 0.85rem; background: #fef2f2;
  border: 1px solid #fca5a5; border-radius: 6px; padding: 0.5rem 0.75rem; margin: 0; }

.form-error { color: #b91c1c; font-size: 0.82rem; margin: 0.4rem 0 0; }

.edit-row > td { background: #f8fafc; border-top: 1px dashed #dde6f0; padding: 0.65rem 0.75rem; }
.edit-row-inner { display: flex; gap: 0.55rem; align-items: center; flex-wrap: wrap; }
.reveal-row > td { background: #f8fafc; padding: 0.5rem 0.75rem; }
</style>
