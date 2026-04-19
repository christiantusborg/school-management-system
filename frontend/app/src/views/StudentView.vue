<template>
  <div class="student-portal">
    <!-- Navbar -->
    <nav class="navbar">
      <div class="navbar-brand">IBSS Student Portal — {{ auth.user?.displayName }}</div>
      <button class="btn-logout" @click="logout">Log Out</button>
    </nav>

    <!-- Tab bar -->
    <div class="tab-bar">
      <button
        v-for="tab in tabs" :key="tab.id"
        class="tab-btn"
        :class="{ active: activeTab === tab.id }"
        @click="activeTab = tab.id"
      >{{ tab.label }}</button>
    </div>

    <div class="tab-content">

      <!-- ── Dashboard ─────────────────────────────────────────────── -->
      <div v-if="activeTab === 'dashboard'" class="tab-pane">
        <div class="card status-card">
          <h2>Your Status</h2>
          <div class="status-row">
            <span :class="['badge', 'badge-' + myStatus]">{{ STATUS_LABELS[myStatus] }}</span>
            <span class="status-desc">{{ STATUS_DESCS[myStatus] }}</span>
          </div>
          <div class="status-meta">
            <span><strong>Programme:</strong> {{ enrollment.programme }}</span>
            <span><strong>Major:</strong> {{ enrollment.major }}</span>
            <span><strong>Student ID:</strong> {{ student.studentId }}</span>
          </div>
        </div>

        <div class="card" v-if="latestAnnouncement">
          <h3>Latest Notice</h3>
          <div class="notice-item">
            <div class="notice-title">{{ latestAnnouncement.title }}</div>
            <div class="notice-body">{{ latestAnnouncement.body }}</div>
            <div class="notice-date">{{ fmtDate(latestAnnouncement.createdAt) }}</div>
          </div>
        </div>

        <div class="card quick-links" v-if="openTickets.length || pendingAbsences.length">
          <h3>Pending Actions</h3>
          <ul>
            <li v-if="openTickets.length">
              <a href="#" @click.prevent="activeTab='support'">
                You have {{ openTickets.length }} open support ticket{{ openTickets.length > 1 ? 's' : '' }}
              </a>
            </li>
            <li v-if="pendingAbsences.length">
              <a href="#" @click.prevent="activeTab='absence'">
                {{ pendingAbsences.length }} absence report{{ pendingAbsences.length > 1 ? 's' : '' }} awaiting acknowledgement
              </a>
            </li>
          </ul>
        </div>
      </div>

      <!-- ── Academic ──────────────────────────────────────────────── -->
      <div v-if="activeTab === 'academic'" class="tab-pane">
        <div class="card">
          <h2>Academic Results</h2>
          <template v-if="isGraded(student.studentId)">
            <table class="tbl">
              <thead>
                <tr>
                  <th>Subject Code</th><th>Subject Name</th><th>Credits</th>
                  <th>Grade</th><th>Grade Points</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="row in myGrades.subjects" :key="row.code">
                  <td>{{ row.code }}</td><td>{{ row.name }}</td><td>{{ row.credits }}</td>
                  <td><span :class="gradeClass(row.grade)">{{ row.grade }}</span></td>
                  <td>{{ row.points }}</td>
                </tr>
              </tbody>
              <tfoot>
                <tr>
                  <td colspan="4" style="text-align:right;font-weight:700">Cumulative GPA</td>
                  <td style="font-weight:700">{{ myGpa }}</td>
                </tr>
              </tfoot>
            </table>
          </template>
          <div v-else class="empty-state">Grades are not yet available. Check back after your assessment period.</div>
        </div>
      </div>

      <!-- ── Documents ─────────────────────────────────────────────── -->
      <div v-if="activeTab === 'documents'" class="tab-pane">
        <div class="card">
          <h2>Documents</h2>
          <div class="doc-grid">
            <!-- Offer Letter -->
            <div class="doc-card" :class="{ disabled: !enrollment.offerType }">
              <div class="doc-icon">📄</div>
              <div class="doc-info">
                <div class="doc-name">Offer Letter</div>
                <div class="doc-sub" v-if="enrollment.offerType">{{ OFFER_LABELS[enrollment.offerType] || enrollment.offerType }}</div>
                <div class="doc-sub" v-else>Not yet issued</div>
              </div>
              <button class="btn-dl" :disabled="!enrollment.offerType" @click="downloadOffer">Download</button>
            </div>
            <!-- Admission Letter -->
            <div class="doc-card" :class="{ disabled: !enrollment.paymentDone }">
              <div class="doc-icon">📋</div>
              <div class="doc-info">
                <div class="doc-name">Admission Letter</div>
                <div class="doc-sub" v-if="enrollment.paymentDone">Payment confirmed</div>
                <div class="doc-sub" v-else>Available after payment</div>
              </div>
              <button class="btn-dl" :disabled="!enrollment.paymentDone" @click="downloadAdmission">Download</button>
            </div>
            <!-- Certificate -->
            <div class="doc-card" :class="{ disabled: !enrollment.certReleased }">
              <div class="doc-icon">🎓</div>
              <div class="doc-info">
                <div class="doc-name">Certificate</div>
                <div class="doc-sub" v-if="enrollment.certReleased">Ready for download</div>
                <div class="doc-sub" v-else>Not yet available</div>
              </div>
              <button class="btn-dl" :disabled="!enrollment.certReleased" @click="downloadCert">Download</button>
            </div>
          </div>

          <div class="divider"></div>
          <h3>Upload / Replace Documents</h3>
          <p class="help-text">Upload any missing or updated documents below. Accepted formats: PDF, JPG, PNG.</p>
          <div class="upload-slots">
            <div v-for="slot in docSlots" :key="slot.field" class="upload-slot">
              <div class="slot-label">{{ slot.label }}</div>
              <div class="slot-status" v-if="student[slot.field]">
                <span class="file-chip">{{ student[slot.field] }}</span>
              </div>
              <div class="slot-status" v-else><span class="missing-chip">Not uploaded</span></div>
              <label class="btn-upload">
                {{ student[slot.field] ? 'Replace' : 'Upload' }}
                <input type="file" accept=".pdf,.jpg,.jpeg,.png" @change="handleDocUpload(slot.field, $event)" hidden />
              </label>
            </div>
          </div>
        </div>
      </div>

      <!-- ── Profile ───────────────────────────────────────────────── -->
      <div v-if="activeTab === 'profile'" class="tab-pane">
        <div class="card">
          <h2>My Profile</h2>
          <div class="profile-grid">
            <div class="profile-ro">
              <label>Full Name</label>
              <div>{{ student.firstName }} {{ student.lastName }}</div>
            </div>
            <div class="profile-ro">
              <label>Student ID</label>
              <div>{{ student.studentId }}</div>
            </div>
            <div class="profile-ro">
              <label>Date of Birth</label>
              <div>{{ student.dateOfBirth }}</div>
            </div>
            <div class="profile-ro">
              <label>Passport / ID No.</label>
              <div>{{ student.passportId }}</div>
            </div>
            <div class="profile-ro">
              <label>Programme</label>
              <div>{{ enrollment.programme }}</div>
            </div>
            <div class="profile-ro">
              <label>Major</label>
              <div>{{ enrollment.major }}</div>
            </div>
            <div class="profile-ro">
              <label>Partner</label>
              <div>{{ student.partner }}</div>
            </div>
          </div>
          <div class="divider"></div>
          <h3>Editable Details</h3>
          <div class="profile-edit-grid">
            <div class="field">
              <label>Email</label>
              <input v-model="editEmail" type="email" />
            </div>
            <div class="field">
              <label>Phone</label>
              <input v-model="editPhone" type="tel" placeholder="+60 12 345 6789" />
            </div>
            <div class="field full-width">
              <label>Address</label>
              <textarea v-model="editAddress" rows="3"></textarea>
            </div>
          </div>
          <div class="profile-actions">
            <button class="btn-save" @click="saveProfile">Save Changes</button>
            <span v-if="profileSaved" class="saved-msg">Changes saved!</span>
          </div>
        </div>
      </div>

      <!-- ── Report Absence ─────────────────────────────────────────── -->
      <div v-if="activeTab === 'absence'" class="tab-pane">
        <div class="card">
          <h2>Report an Absence</h2>
          <div class="absence-form">
            <div class="field">
              <label>Date of Absence</label>
              <input v-model="absForm.date" type="date" :max="today" />
            </div>
            <div class="field">
              <label>Type</label>
              <select v-model="absForm.type">
                <option value="">— select —</option>
                <option>Sick</option>
                <option>Personal</option>
                <option>Family Emergency</option>
                <option>Other</option>
              </select>
            </div>
            <div class="field">
              <label>Reason</label>
              <textarea v-model="absForm.reason" rows="3" placeholder="Brief description…"></textarea>
            </div>
            <div class="field">
              <label>Supporting Document <span class="optional">(optional — e.g. doctor's note)</span></label>
              <div v-if="absForm.docFilename" class="file-chip">{{ absForm.docFilename }}</div>
              <label class="btn-upload">
                {{ absForm.docFilename ? 'Replace' : 'Attach file' }}
                <input type="file" accept=".pdf,.jpg,.jpeg,.png" @change="handleAbsDoc" hidden />
              </label>
            </div>
            <button class="btn-save" :disabled="!absForm.date || !absForm.type" @click="submitAbsence">Submit Report</button>
            <span v-if="absSaved" class="saved-msg">Absence reported!</span>
          </div>
        </div>

        <div class="card" v-if="myAbsences.length">
          <h3>Previous Absence Reports</h3>
          <table class="tbl">
            <thead>
              <tr><th>Date</th><th>Type</th><th>Reason</th><th>Status</th></tr>
            </thead>
            <tbody>
              <tr v-for="a in myAbsences" :key="a.id">
                <td>{{ a.date }}</td>
                <td>{{ a.type }}</td>
                <td>{{ a.reason || '—' }}</td>
                <td>
                  <span :class="['badge', a.status === 'acknowledged' ? 'badge-confirmed' : 'badge-new']">
                    {{ a.status === 'acknowledged' ? 'Acknowledged' : 'Pending' }}
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- ── Support ───────────────────────────────────────────────── -->
      <div v-if="activeTab === 'support'" class="tab-pane">
        <div class="card">
          <h2>Raise a Support Ticket</h2>
          <div class="ticket-form">
            <div class="field">
              <label>Subject</label>
              <input v-model="ticketForm.subject" type="text" placeholder="Brief subject…" />
            </div>
            <div class="field">
              <label>Message</label>
              <textarea v-model="ticketForm.message" rows="4" placeholder="Describe your issue…"></textarea>
            </div>
            <button class="btn-save" :disabled="!ticketForm.subject || !ticketForm.message" @click="submitTicket">Submit Ticket</button>
            <span v-if="ticketSaved" class="saved-msg">Ticket submitted!</span>
          </div>
        </div>

        <div class="card" v-if="myTickets.length">
          <h3>My Tickets</h3>
          <div v-for="t in myTickets" :key="t.id" class="ticket-item">
            <div class="ticket-header" @click="toggleTicket(t.id)">
              <span class="ticket-subject">{{ t.subject }}</span>
              <span :class="['badge', t.status === 'resolved' ? 'badge-approved' : 'badge-offer']">
                {{ t.status === 'resolved' ? 'Resolved' : 'Open' }}
              </span>
              <span class="ticket-date">{{ fmtDate(t.createdAt) }}</span>
              <span class="ticket-toggle">{{ expandedTickets.includes(t.id) ? '▲' : '▼' }}</span>
            </div>
            <div v-if="expandedTickets.includes(t.id)" class="ticket-thread">
              <div v-for="(reply, i) in t.replies" :key="i" :class="['reply', 'reply-' + reply.from]">
                <span class="reply-from">{{ replyFromLabel(reply.from) }}</span>
                <span class="reply-at">{{ fmtDate(reply.at) }}</span>
                <p>{{ reply.text }}</p>
              </div>
              <div v-if="t.status !== 'resolved'" class="reply-box">
                <textarea v-model="replyTexts[t.id]" rows="2" placeholder="Add a follow-up…"></textarea>
                <button class="btn-save btn-sm" :disabled="!replyTexts[t.id]" @click="addReply(t)">Send</button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- ── Notices ───────────────────────────────────────────────── -->
      <div v-if="activeTab === 'notices'" class="tab-pane">
        <div class="card">
          <h2>Notices &amp; Announcements</h2>
          <div v-if="myAnnouncements.length === 0" class="empty-state">No notices at this time.</div>
          <div v-for="a in myAnnouncements" :key="a.id" class="notice-item">
            <div class="notice-title">{{ a.title }}</div>
            <div class="notice-body">{{ a.body }}</div>
            <div class="notice-date">{{ fmtDate(a.createdAt) }}</div>
          </div>
        </div>
      </div>

    </div>

    <!-- Toast -->
    <div v-if="toast" class="toast">{{ toast }}</div>
  </div>
</template>

<script setup>
import { ref, computed, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { auth } from '../store/auth.js'
import { students } from '../mock/data.js'
import { absences, nextAbsenceId } from '../mock/absences.js'
import { tickets, nextTicketId } from '../mock/tickets.js'
import { announcements } from '../mock/announcements.js'
import { gradesStore, isGraded } from '../store/grades.js'

const router = useRouter()

// find the live student record so reactive updates from admin are reflected
const student    = computed(() => students.find(s => s.studentId === auth.user?.studentId) || auth.user)
const enrollment = computed(() => student.value?.enrollments?.[0] ?? {})

const tabs = [
  { id: 'dashboard', label: 'Dashboard' },
  { id: 'academic',  label: 'Academic'  },
  { id: 'documents', label: 'Documents' },
  { id: 'profile',   label: 'Profile'   },
  { id: 'absence',   label: 'Report Absence' },
  { id: 'support',   label: 'Support'   },
  { id: 'notices',   label: 'Notices'   },
]
const activeTab = ref('dashboard')

// ── Status helpers ────────────────────────────────────────────────────────────
const STATUS_LABELS = {
  new:       'New',
  offer:     'Offer Issued',
  admission: 'Admission',
  confirmed: 'Admitted',
  graded:    'Graded',
  approved:  'Approved',
}
const STATUS_DESCS = {
  new:       'Your application is under review.',
  offer:     'You have received an offer letter.',
  admission: 'Your payment has been received — admission is in progress.',
  confirmed: 'You are admitted — welcome to IBSS!',
  graded:    'Your grades are ready. Please check the Academic tab.',
  approved:  'Your certificate is available for download.',
}
const OFFER_LABELS = {
  standard:  'Standard Offer',
  conditional: 'Conditional Offer',
  unconditional: 'Unconditional Offer',
}

const myStatus = computed(() => {
  const s   = student.value
  const enr = enrollment.value
  if (!s) return 'new'
  if (enr.certReleased)      return 'approved'
  if (isGraded(s.studentId)) return 'graded'
  if (enr.admissionConfirmed) return 'confirmed'
  if (enr.paymentDone)       return 'admission'
  if (enr.offerType)         return 'offer'
  return 'new'
})

// ── Grades ────────────────────────────────────────────────────────────────────
const myGrades = computed(() => gradesStore[student.value?.studentId] || { subjects: [] })
const myGpa    = computed(() => {
  const subs = myGrades.value.subjects
  if (!subs.length) return '—'
  const total  = subs.reduce((a, s) => a + s.credits * s.points, 0)
  const credits = subs.reduce((a, s) => a + s.credits, 0)
  return credits ? (total / credits).toFixed(2) : '—'
})
function gradeClass(g) {
  if (['A','A+','A-'].includes(g)) return 'grade-a'
  if (['B','B+','B-'].includes(g)) return 'grade-b'
  if (['C','C+','C-'].includes(g)) return 'grade-c'
  return 'grade-f'
}

// ── Documents ─────────────────────────────────────────────────────────────────
const docSlots = [
  { field: 'docPassport', label: 'Passport / National ID' },
  { field: 'docDegree',   label: 'Degree Certificate'     },
  { field: 'docLanguage', label: 'Language Result'         },
  { field: 'docCV',       label: 'CV / Résumé'             },
]
function handleDocUpload(field, e) {
  const file = e.target.files[0]
  if (!file) return
  const live = students.find(s => s.studentId === auth.user?.studentId)
  if (live) live[field] = file.name
  showToast(`${file.name} uploaded.`)
}
function downloadOffer() {
  const s = student.value
  const win = window.open('', '_blank')
  win.document.write(`<html><body style="font-family:sans-serif;padding:2rem">
    <h1>Offer Letter</h1>
    <p>Dear ${s.firstName} ${s.lastName},</p>
    <p>We are pleased to offer you a place on the <strong>${s.programme}</strong> programme.</p>
    <p>Offer type: <strong>${OFFER_LABELS[s.offerType] || s.offerType}</strong></p>
    <br/><p>IBSS Admissions</p>
  </body></html>`)
  win.document.close()
  win.onload = () => win.print()
}
function downloadAdmission() {
  const s = student.value
  const win = window.open('', '_blank')
  win.document.write(`<html><body style="font-family:sans-serif;padding:2rem">
    <h1>Admission Letter</h1>
    <p>Dear ${s.firstName} ${s.lastName},</p>
    <p>Your payment has been received and you are now admitted to the <strong>${s.programme}</strong> programme.</p>
    <p>Student ID: <strong>${s.studentId}</strong></p>
    <br/><p>IBSS Admissions</p>
  </body></html>`)
  win.document.close()
  win.onload = () => win.print()
}
function downloadCert() {
  const s = student.value
  const win = window.open('', '_blank')
  win.document.write(`<html><body style="font-family:sans-serif;padding:2rem;text-align:center">
    <h1>Certificate of Completion</h1>
    <p>This is to certify that</p>
    <h2>${s.firstName} ${s.lastName}</h2>
    <p>has successfully completed the</p>
    <h3>${s.programme}</h3>
    <p>International Business School of Scandinavia</p>
  </body></html>`)
  win.document.close()
  win.onload = () => win.print()
}

// ── Profile ───────────────────────────────────────────────────────────────────
const editEmail   = ref(student.value?.email || '')
const editPhone   = ref(student.value?.phone || '')
const editAddress = ref(student.value?.address || '')
const profileSaved = ref(false)
function saveProfile() {
  const live = students.find(s => s.studentId === auth.user?.studentId)
  if (live) {
    live.email   = editEmail.value
    live.phone   = editPhone.value
    live.address = editAddress.value
  }
  profileSaved.value = true
  setTimeout(() => { profileSaved.value = false }, 2500)
  showToast('Profile updated.')
}

// ── Absences ──────────────────────────────────────────────────────────────────
const today = new Date().toISOString().split('T')[0]
const absForm = reactive({ date: today, type: '', reason: '', docFilename: '' })
const absSaved = ref(false)
const myAbsences = computed(() => absences.filter(a => a.studentId === student.value?.studentId).slice().reverse())
const pendingAbsences = computed(() => myAbsences.value.filter(a => a.status === 'pending'))

function handleAbsDoc(e) {
  absForm.docFilename = e.target.files[0]?.name || ''
}
function submitAbsence() {
  absences.push({
    id: nextAbsenceId(),
    studentId: student.value.studentId,
    date: absForm.date,
    type: absForm.type,
    reason: absForm.reason,
    docFilename: absForm.docFilename,
    status: 'pending',
    submittedAt: new Date().toISOString(),
  })
  Object.assign(absForm, { date: today, type: '', reason: '', docFilename: '' })
  absSaved.value = true
  setTimeout(() => { absSaved.value = false }, 2500)
  showToast('Absence report submitted.')
}

// ── Support tickets ───────────────────────────────────────────────────────────
const ticketForm  = reactive({ subject: '', message: '' })
const ticketSaved = ref(false)
const myTickets   = computed(() => tickets.filter(t => t.studentId === student.value?.studentId).slice().reverse())
const openTickets = computed(() => myTickets.value.filter(t => t.status === 'open'))
const expandedTickets = ref([])
const replyTexts  = reactive({})

function toggleTicket(id) {
  const i = expandedTickets.value.indexOf(id)
  if (i === -1) expandedTickets.value.push(id)
  else expandedTickets.value.splice(i, 1)
}
function submitTicket() {
  const id = nextTicketId()
  tickets.push({
    id,
    studentId: student.value.studentId,
    subject: ticketForm.subject,
    message: ticketForm.message,
    status: 'open',
    createdAt: new Date().toISOString(),
    replies: [{ from: 'student', text: ticketForm.message, at: new Date().toISOString() }],
  })
  Object.assign(ticketForm, { subject: '', message: '' })
  ticketSaved.value = true
  setTimeout(() => { ticketSaved.value = false }, 2500)
  expandedTickets.value.push(id)
  showToast('Ticket submitted.')
}
function addReply(t) {
  const text = replyTexts[t.id]
  if (!text) return
  t.replies.push({ from: 'student', text, at: new Date().toISOString() })
  replyTexts[t.id] = ''
  showToast('Reply sent.')
}
function replyFromLabel(from) {
  return from === 'student' ? 'You' : from === 'admin' ? 'IBSS Admin' : 'Partner'
}

// ── Announcements ─────────────────────────────────────────────────────────────
const myAnnouncements = computed(() =>
  announcements
    .filter(a => a.targetStudentId === null || a.targetStudentId === student.value?.studentId)
    .slice().reverse()
)
const latestAnnouncement = computed(() => myAnnouncements.value[0] || null)

// ── Helpers ───────────────────────────────────────────────────────────────────
function fmtDate(iso) {
  if (!iso) return ''
  return new Date(iso).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })
}

const toast = ref('')
function showToast(msg) {
  toast.value = msg
  setTimeout(() => { toast.value = '' }, 3000)
}

function logout() {
  auth.logout()
  router.push('/login')
}
</script>

<style scoped>
.student-portal { min-height: 100vh; background: #f0f4f8; font-family: sans-serif; }

/* Navbar */
.navbar {
  background: #003366; color: #fff;
  display: flex; align-items: center; justify-content: space-between;
  padding: 0.85rem 1.5rem;
}
.navbar-brand { font-weight: 700; font-size: 1rem; }
.btn-logout {
  background: transparent; border: 1.5px solid rgba(255,255,255,.5);
  color: #fff; padding: 0.35rem 1rem; border-radius: 5px; cursor: pointer; font-size: 0.85rem;
}
.btn-logout:hover { background: rgba(255,255,255,.15); }

/* Tab bar */
.tab-bar {
  display: flex; gap: 0; background: #fff;
  border-bottom: 2px solid #dde3ea;
  overflow-x: auto;
}
.tab-btn {
  padding: 0.75rem 1.25rem; border: none; background: transparent;
  cursor: pointer; font-size: 0.88rem; color: #555; white-space: nowrap;
  border-bottom: 3px solid transparent; margin-bottom: -2px;
  transition: color .2s, border-color .2s;
}
.tab-btn.active { color: #003366; font-weight: 700; border-bottom-color: #003366; }
.tab-btn:hover:not(.active) { color: #003366; background: #f7f9fc; }

/* Content */
.tab-content { padding: 1.5rem; max-width: 960px; margin: 0 auto; }
.tab-pane { display: flex; flex-direction: column; gap: 1.25rem; }
.card {
  background: #fff; border-radius: 10px; padding: 1.5rem;
  box-shadow: 0 1px 4px rgba(0,0,0,.08);
}
.card h2 { margin: 0 0 1.1rem; font-size: 1.1rem; color: #003366; }
.card h3 { margin: 0 0 0.9rem; font-size: 0.95rem; color: #444; }
.divider { border: none; border-top: 1px solid #eee; margin: 1.25rem 0; }

/* Status card */
.status-card .status-row { display: flex; align-items: center; gap: 1rem; margin-bottom: 0.75rem; }
.status-desc { color: #555; font-size: 0.9rem; }
.status-meta { display: flex; flex-wrap: wrap; gap: 1.5rem; font-size: 0.85rem; color: #666; }

/* Badges */
.badge { padding: 0.25rem 0.65rem; border-radius: 12px; font-size: 0.78rem; font-weight: 600; }
.badge-new       { background: #e9ecef; color: #555; }
.badge-offer     { background: #fff3cd; color: #856404; }
.badge-admission { background: #cfe2ff; color: #084298; }
.badge-confirmed { background: #d1e7dd; color: #0a3622; }
.badge-graded    { background: #d4edda; color: #155724; }
.badge-approved  { background: #d1f0e0; color: #0a5c36; }

/* Tables */
.tbl { width: 100%; border-collapse: collapse; font-size: 0.88rem; }
.tbl th { background: #f8f9fa; color: #444; font-weight: 600; padding: 0.55rem 0.75rem; text-align: left; border-bottom: 2px solid #dee2e6; }
.tbl td { padding: 0.55rem 0.75rem; border-bottom: 1px solid #f0f0f0; color: #333; }
.tbl tbody tr:hover { background: #fafcff; }
.grade-a { color: #0a5c36; font-weight: 700; }
.grade-b { color: #084298; font-weight: 600; }
.grade-c { color: #856404; font-weight: 600; }
.grade-f { color: #842029; font-weight: 600; }

/* Doc cards */
.doc-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(230px, 1fr)); gap: 1rem; margin-bottom: 0.5rem; }
.doc-card {
  border: 1.5px solid #dde3ea; border-radius: 8px; padding: 1rem;
  display: flex; flex-direction: column; align-items: center; gap: 0.5rem; text-align: center;
}
.doc-card.disabled { opacity: .5; }
.doc-icon { font-size: 2rem; }
.doc-name { font-weight: 600; font-size: 0.9rem; color: #333; }
.doc-sub  { font-size: 0.78rem; color: #888; }
.btn-dl {
  margin-top: 0.25rem; padding: 0.4rem 1rem; font-size: 0.82rem;
  background: #003366; color: #fff; border: none; border-radius: 5px; cursor: pointer;
}
.btn-dl:disabled { background: #aaa; cursor: not-allowed; }

/* Upload slots */
.upload-slots { display: flex; flex-direction: column; gap: 0.75rem; }
.upload-slot {
  display: flex; align-items: center; gap: 0.75rem; flex-wrap: wrap;
  padding: 0.65rem 0.75rem; background: #f8f9fa; border-radius: 7px;
}
.slot-label { font-weight: 600; font-size: 0.85rem; min-width: 180px; color: #333; }
.file-chip { background: #d1e7dd; color: #0a5c36; padding: 0.2rem 0.6rem; border-radius: 10px; font-size: 0.78rem; }
.missing-chip { background: #f8d7da; color: #842029; padding: 0.2rem 0.6rem; border-radius: 10px; font-size: 0.78rem; }
.btn-upload {
  padding: 0.35rem 0.9rem; background: #0055a5; color: #fff;
  border-radius: 5px; font-size: 0.82rem; cursor: pointer;
}

/* Profile */
.profile-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(220px, 1fr)); gap: 0.85rem; }
.profile-ro label { display: block; font-size: 0.75rem; color: #888; text-transform: uppercase; letter-spacing: .04em; margin-bottom: 0.2rem; }
.profile-ro > div { font-size: 0.9rem; color: #222; font-weight: 500; }
.profile-edit-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 0.85rem; }
.full-width { grid-column: 1 / -1; }
.field { display: flex; flex-direction: column; gap: 0.3rem; }
.field label { font-size: 0.83rem; font-weight: 600; color: #444; }
.field input, .field select, .field textarea {
  padding: 0.55rem 0.75rem; border: 1.5px solid #ccc; border-radius: 6px; font-size: 0.9rem;
}
.field input:focus, .field select:focus, .field textarea:focus { border-color: #003366; outline: none; }
.profile-actions { display: flex; align-items: center; gap: 1rem; margin-top: 1rem; }

/* Absence form */
.absence-form { display: flex; flex-direction: column; gap: 0.85rem; max-width: 500px; }

/* Ticket */
.ticket-form { display: flex; flex-direction: column; gap: 0.85rem; max-width: 560px; }
.ticket-item { border: 1.5px solid #dde3ea; border-radius: 8px; margin-bottom: 0.75rem; overflow: hidden; }
.ticket-header {
  display: flex; align-items: center; gap: 0.75rem; padding: 0.75rem 1rem;
  cursor: pointer; background: #f8f9fa;
}
.ticket-header:hover { background: #eef2f7; }
.ticket-subject { flex: 1; font-weight: 600; font-size: 0.9rem; color: #222; }
.ticket-date { font-size: 0.78rem; color: #888; }
.ticket-toggle { color: #666; font-size: 0.75rem; }
.ticket-thread { padding: 0.75rem 1rem; display: flex; flex-direction: column; gap: 0.6rem; }
.reply {
  padding: 0.6rem 0.8rem; border-radius: 7px; font-size: 0.87rem;
}
.reply-student { background: #e8f0fe; border-left: 3px solid #4a90d9; }
.reply-admin   { background: #e8f6e9; border-left: 3px solid #2d9e53; }
.reply-partner { background: #fff8e6; border-left: 3px solid #e6a817; }
.reply-from { font-weight: 700; margin-right: 0.5rem; }
.reply-at { font-size: 0.75rem; color: #888; }
.reply p { margin: 0.3rem 0 0; }
.reply-box { display: flex; gap: 0.5rem; align-items: flex-end; margin-top: 0.5rem; }
.reply-box textarea { flex: 1; padding: 0.5rem 0.7rem; border: 1.5px solid #ccc; border-radius: 6px; font-size: 0.88rem; resize: vertical; }

/* Notices */
.notice-item { border-left: 3px solid #003366; padding: 0.6rem 0.9rem; margin-bottom: 0.75rem; background: #f7f9fc; border-radius: 0 6px 6px 0; }
.notice-title { font-weight: 700; color: #003366; font-size: 0.92rem; }
.notice-body  { color: #444; font-size: 0.88rem; margin: 0.3rem 0; }
.notice-date  { font-size: 0.75rem; color: #888; }

/* Common buttons */
.btn-save {
  padding: 0.55rem 1.4rem; background: #003366; color: #fff;
  border: none; border-radius: 6px; font-size: 0.9rem; cursor: pointer; font-weight: 600;
}
.btn-save:hover:not(:disabled) { background: #0055a5; }
.btn-save:disabled { background: #aaa; cursor: not-allowed; }
.btn-sm { padding: 0.4rem 0.9rem; font-size: 0.82rem; }
.saved-msg { font-size: 0.85rem; color: #2d9e53; font-weight: 600; }
.optional { font-weight: normal; color: #888; font-size: 0.8rem; }
.empty-state { color: #888; font-style: italic; font-size: 0.9rem; padding: 1rem 0; }
.help-text { font-size: 0.83rem; color: #666; margin: 0 0 0.75rem; }

/* Quick links */
.quick-links ul { margin: 0; padding: 0 0 0 1.1rem; }
.quick-links li { margin-bottom: 0.4rem; }
.quick-links a { color: #003366; font-size: 0.9rem; }

/* Toast */
.toast {
  position: fixed; bottom: 2rem; right: 2rem;
  background: #003366; color: #fff;
  padding: 0.75rem 1.4rem; border-radius: 8px;
  font-size: 0.9rem; box-shadow: 0 4px 16px rgba(0,0,0,.2);
  animation: fadein .3s ease;
  z-index: 9999;
}
@keyframes fadein { from { opacity: 0; transform: translateY(8px); } to { opacity: 1; transform: none; } }
</style>
