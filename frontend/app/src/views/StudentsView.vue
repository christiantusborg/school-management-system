<template>
  <div class="page-wrapper">
    <!-- Navbar -->
    <nav class="navbar">
      <span class="brand-text">IBSS Admin Portal</span>
      <div class="nav-right">
        <span class="nav-user">{{ auth.user?.displayName }}</span>
        <button class="btn-logout" @click="logout">Log out</button>
      </div>
    </nav>

    <div class="container">
      <div class="page-header">
        <div>
          <h1 class="page-title">Students</h1>
          <p class="page-sub">{{ filtered.length }} of {{ students.length }} records</p>
        </div>
        <button class="btn-add" @click="openAdd">+ Add Student</button>
      </div>

      <!-- Filters bar -->
      <div class="filters-bar">
        <div class="search-wrap">
          <svg class="search-icon" viewBox="0 0 20 20" fill="none">
            <circle cx="9" cy="9" r="6" stroke="#888" stroke-width="1.6"/>
            <path d="M13.5 13.5 17 17" stroke="#888" stroke-width="1.6" stroke-linecap="round"/>
          </svg>
          <input
            v-model="search"
            class="search-input"
            type="text"
            placeholder="Search by student name…"
          />
          <button v-if="search" class="search-clear" @click="search = ''">✕</button>
        </div>

        <select v-model="filterPartner" class="filter-select">
          <option value="">All Partners</option>
          <option v-for="p in partners" :key="p">{{ p }}</option>
        </select>

        <select v-model="filterProgramme" class="filter-select">
          <option value="">All Programmes</option>
          <option v-for="p in programmes" :key="p">{{ p }}</option>
        </select>

        <select v-model="filterMajor" class="filter-select">
          <option value="">All Majors</option>
          <option v-for="m in majors" :key="m">{{ m }}</option>
        </select>

        <button
          v-if="search || filterPartner || filterProgramme || filterMajor"
          class="btn-clear-all"
          @click="clearFilters"
        >Clear filters</button>
      </div>

      <!-- Students table -->
      <div class="table-wrap">
        <table class="students-table">
          <thead>
            <tr>
              <th>Student ID</th>
              <th>Name</th>
              <th>Passport / ID</th>
              <th>Partner</th>
              <th>Programme</th>
              <th>Major</th>
              <th>Commenced</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="filtered.length === 0">
              <td colspan="7" class="empty-row">No students match the current filters.</td>
            </tr>
            <tr v-for="s in filtered" :key="s.id" class="data-row">
              <td class="mono">{{ s.studentId }}</td>
              <td>{{ s.firstName }} {{ s.lastName }}</td>
              <td class="mono">{{ s.passportId }}</td>
              <td>{{ s.partner }}</td>
              <td>{{ s.programme }}</td>
              <td>{{ s.major }}</td>
              <td>{{ formatDate(s.commencementDate) }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- ── Add Student Drawer ──────────────────────────────────────────────── -->
    <transition name="fade">
      <div v-if="showDrawer" class="drawer-overlay" @click.self="closeDrawer" />
    </transition>

    <transition name="slide">
      <div v-if="showDrawer" class="drawer">
        <div class="drawer-header">
          <h2>Add New Student</h2>
          <button class="drawer-close" @click="closeDrawer">✕</button>
        </div>

        <form @submit.prevent="submitStudent" class="drawer-form">
          <div class="row-2">
            <div class="field">
              <label>First Name <span class="req">*</span></label>
              <input v-model="form.firstName" type="text" required />
            </div>
            <div class="field">
              <label>Last Name <span class="req">*</span></label>
              <input v-model="form.lastName" type="text" required />
            </div>
          </div>

          <div class="field">
            <label>Passport / ID No. <span class="req">*</span></label>
            <input v-model="form.passportId" type="text" required placeholder="e.g. 800203-10-5685" />
          </div>

          <div class="field">
            <label>Address</label>
            <input v-model="form.address" type="text" placeholder="Full address" />
          </div>

          <div class="field">
            <label>Partner <span class="req">*</span></label>
            <select v-model="form.partner" required>
              <option value="">— Select partner —</option>
              <option v-for="p in partners" :key="p">{{ p }}</option>
            </select>
          </div>

          <div class="field">
            <label>Programme <span class="req">*</span></label>
            <select v-model="form.programme" required>
              <option value="">— Select programme —</option>
              <option v-for="p in programmes" :key="p">{{ p }}</option>
            </select>
          </div>

          <div class="field">
            <label>Major <span class="req">*</span></label>
            <select v-model="form.major" required>
              <option value="">— Select major —</option>
              <option v-for="m in majors" :key="m">{{ m }}</option>
            </select>
          </div>

          <div class="field">
            <label>Commencement Date <span class="req">*</span></label>
            <input v-model="form.commencementDate" type="date" required />
          </div>

          <div class="field">
            <label>Mode of Study <span class="req">*</span></label>
            <select v-model="form.modeOfStudy" required>
              <option value="">— Select mode —</option>
              <option>Distance/Online self-study</option>
              <option>Blended learning</option>
              <option>On-campus</option>
            </select>
          </div>

          <div v-if="addSuccess" class="success-msg">
            Student <strong>{{ addSuccess }}</strong> added successfully!
          </div>

          <div class="drawer-actions">
            <button type="button" class="btn-cancel" @click="closeDrawer">Cancel</button>
            <button type="submit" class="btn-save">Save Student</button>
          </div>
        </form>
      </div>
    </transition>
  </div>
</template>

<script setup>
import { ref, reactive, computed } from 'vue'
import { useRouter } from 'vue-router'
import { auth } from '../store/auth.js'
import { students, partners, programmes, majors, getNextId } from '../mock/data.js'

const router = useRouter()

// ── Filters ───────────────────────────────────────────────────────────────────
const search = ref('')
const filterPartner = ref('')
const filterProgramme = ref('')
const filterMajor = ref('')

function fuzzyMatch(haystack, needle) {
  if (!needle) return true
  const h = haystack.toLowerCase()
  const n = needle.toLowerCase().trim()
  // simple: every character of needle appears in order in haystack
  let hi = 0
  for (const ch of n) {
    const pos = h.indexOf(ch, hi)
    if (pos === -1) return false
    hi = pos + 1
  }
  return true
}

const filtered = computed(() => {
  return students.filter((s) => {
    const fullName = `${s.firstName} ${s.lastName}`
    if (!fuzzyMatch(fullName, search.value)) return false
    if (filterPartner.value && s.partner !== filterPartner.value) return false
    if (filterProgramme.value && s.programme !== filterProgramme.value) return false
    if (filterMajor.value && s.major !== filterMajor.value) return false
    return true
  })
})

function clearFilters() {
  search.value = ''
  filterPartner.value = ''
  filterProgramme.value = ''
  filterMajor.value = ''
}

// ── Date formatting ───────────────────────────────────────────────────────────
function formatDate(d) {
  if (!d) return '—'
  const dt = new Date(d)
  return dt.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })
}

// ── Add student drawer ────────────────────────────────────────────────────────
const showDrawer = ref(false)
const addSuccess = ref('')

const emptyForm = () => ({
  firstName: '',
  lastName: '',
  passportId: '',
  address: '',
  partner: '',
  programme: '',
  major: '',
  commencementDate: '',
  modeOfStudy: '',
})

const form = reactive(emptyForm())

function openAdd() {
  Object.assign(form, emptyForm())
  addSuccess.value = ''
  showDrawer.value = true
}

function closeDrawer() {
  showDrawer.value = false
}

function generateStudentId(programme, seq) {
  const map = {
    'Master of Business Administration': 'MBA',
    'Bachelor of Business Administration': 'BBA',
    'Master of Finance': 'MF',
    'Bachelor of Computer Science': 'BCS',
    'Master of Marketing': 'MM',
  }
  const code = map[programme] ?? 'GEN'
  const now = new Date()
  const yy = String(now.getFullYear()).slice(2)
  const mm = String(now.getMonth() + 1).padStart(2, '0')
  return `IBSS.${code}.${yy}${mm}${String(seq).padStart(4, '0')}`
}

function submitStudent() {
  const seq = getNextId()
  const newStudent = {
    id: seq,
    studentId: generateStudentId(form.programme, seq),
    firstName: form.firstName,
    lastName: form.lastName,
    passportId: form.passportId,
    address: form.address,
    partner: form.partner,
    programme: form.programme,
    major: form.major,
    commencementDate: form.commencementDate,
    modeOfStudy: form.modeOfStudy,
  }
  students.push(newStudent)
  addSuccess.value = `${form.firstName} ${form.lastName} (${newStudent.studentId})`
  setTimeout(() => {
    showDrawer.value = false
    addSuccess.value = ''
  }, 1800)
}

// ── Logout ────────────────────────────────────────────────────────────────────
function logout() {
  auth.logout()
  router.push('/login')
}
</script>

<style scoped>
.page-wrapper {
  min-height: 100vh;
  background: #f2f5f9;
}

/* Navbar */
.navbar {
  background: #003366;
  color: #fff;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.85rem 2rem;
}
.brand-text { font-size: 1.05rem; font-weight: 700; }
.nav-right { display: flex; align-items: center; gap: 1rem; }
.nav-user { font-size: 0.85rem; opacity: 0.85; }
.btn-logout {
  background: transparent;
  border: 1.5px solid rgba(255,255,255,0.55);
  color: #fff;
  padding: 0.3rem 0.85rem;
  border-radius: 5px;
  cursor: pointer;
  font-size: 0.82rem;
}
.btn-logout:hover { background: rgba(255,255,255,0.13); }

/* Container */
.container {
  max-width: 1100px;
  margin: 2rem auto;
  padding: 0 1.5rem;
}

/* Page header */
.page-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  margin-bottom: 1.25rem;
}
.page-title { font-size: 1.5rem; font-weight: 700; color: #003366; }
.page-sub { font-size: 0.82rem; color: #888; margin-top: 0.2rem; }

.btn-add {
  background: #003366;
  color: #fff;
  border: none;
  border-radius: 7px;
  padding: 0.6rem 1.25rem;
  font-size: 0.9rem;
  font-weight: 600;
  cursor: pointer;
  white-space: nowrap;
  transition: background 0.18s;
}
.btn-add:hover { background: #0055a5; }

/* Filters bar */
.filters-bar {
  display: flex;
  flex-wrap: wrap;
  gap: 0.65rem;
  align-items: center;
  margin-bottom: 1.1rem;
}

.search-wrap {
  position: relative;
  flex: 1 1 220px;
  min-width: 180px;
}
.search-icon {
  position: absolute;
  left: 0.65rem;
  top: 50%;
  transform: translateY(-50%);
  width: 16px;
  height: 16px;
  pointer-events: none;
}
.search-input {
  width: 100%;
  padding: 0.58rem 2rem 0.58rem 2.1rem;
  border: 1.5px solid #ccc;
  border-radius: 7px;
  font-size: 0.88rem;
  outline: none;
  box-sizing: border-box;
  transition: border-color 0.2s;
}
.search-input:focus { border-color: #0055a5; }
.search-clear {
  position: absolute;
  right: 0.5rem;
  top: 50%;
  transform: translateY(-50%);
  background: none;
  border: none;
  color: #aaa;
  cursor: pointer;
  font-size: 0.85rem;
  line-height: 1;
  padding: 0;
}

.filter-select {
  padding: 0.55rem 0.8rem;
  border: 1.5px solid #ccc;
  border-radius: 7px;
  font-size: 0.86rem;
  outline: none;
  background: #fff;
  min-width: 140px;
  cursor: pointer;
  transition: border-color 0.2s;
}
.filter-select:focus { border-color: #0055a5; }

.btn-clear-all {
  background: none;
  border: 1.5px solid #ccc;
  border-radius: 7px;
  padding: 0.52rem 0.9rem;
  font-size: 0.82rem;
  color: #666;
  cursor: pointer;
}
.btn-clear-all:hover { border-color: #999; color: #333; }

/* Table */
.table-wrap {
  background: #fff;
  border-radius: 10px;
  box-shadow: 0 2px 10px rgba(0,0,0,0.07);
  overflow: auto;
}
.students-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.88rem;
}
.students-table th {
  text-align: left;
  padding: 0.75rem 1rem;
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: #666;
  border-bottom: 2px solid #e8edf4;
  white-space: nowrap;
  background: #fafbfc;
}
.data-row td {
  padding: 0.75rem 1rem;
  border-bottom: 1px solid #f0f3f7;
  color: #222;
}
.data-row:last-child td { border-bottom: none; }
.data-row:hover td { background: #f7f9fb; }
.mono { font-family: ui-monospace, monospace; font-size: 0.82rem; color: #555; }
.empty-row {
  text-align: center;
  padding: 2.5rem !important;
  color: #aaa;
  font-style: italic;
}

/* Drawer overlay */
.drawer-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0,0,0,0.35);
  z-index: 100;
}

/* Drawer panel */
.drawer {
  position: fixed;
  top: 0;
  right: 0;
  bottom: 0;
  width: 420px;
  max-width: 95vw;
  background: #fff;
  z-index: 101;
  display: flex;
  flex-direction: column;
  box-shadow: -4px 0 24px rgba(0,0,0,0.15);
}

.drawer-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1.25rem 1.5rem;
  border-bottom: 1.5px solid #e8edf4;
  flex-shrink: 0;
}
.drawer-header h2 {
  font-size: 1.05rem;
  font-weight: 700;
  color: #003366;
}
.drawer-close {
  background: none;
  border: none;
  font-size: 1.1rem;
  color: #888;
  cursor: pointer;
  line-height: 1;
  padding: 0.2rem;
}
.drawer-close:hover { color: #333; }

.drawer-form {
  flex: 1;
  overflow-y: auto;
  padding: 1.25rem 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 0.9rem;
}

.row-2 {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.75rem;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
}
.field label {
  font-size: 0.82rem;
  font-weight: 600;
  color: #444;
}
.req { color: #c0392b; }
.field input,
.field select {
  padding: 0.6rem 0.8rem;
  border: 1.5px solid #ccc;
  border-radius: 7px;
  font-size: 0.9rem;
  font-family: inherit;
  outline: none;
  transition: border-color 0.2s;
}
.field input:focus,
.field select:focus { border-color: #0055a5; }

.success-msg {
  background: #eafaf1;
  border: 1.5px solid #2ecc71;
  border-radius: 7px;
  padding: 0.75rem 1rem;
  color: #1e8449;
  font-size: 0.86rem;
}

.drawer-actions {
  display: flex;
  gap: 0.75rem;
  justify-content: flex-end;
  padding-top: 0.5rem;
  flex-shrink: 0;
}
.btn-cancel {
  padding: 0.65rem 1.2rem;
  background: #f2f5f9;
  border: 1.5px solid #ccc;
  border-radius: 7px;
  font-size: 0.9rem;
  cursor: pointer;
  color: #555;
}
.btn-save {
  padding: 0.65rem 1.4rem;
  background: #003366;
  color: #fff;
  border: none;
  border-radius: 7px;
  font-size: 0.9rem;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.18s;
}
.btn-save:hover { background: #0055a5; }

/* Transitions */
.fade-enter-active, .fade-leave-active { transition: opacity 0.22s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }

.slide-enter-active, .slide-leave-active { transition: transform 0.25s ease; }
.slide-enter-from, .slide-leave-to { transform: translateX(100%); }
</style>
