<template>
  <div class="page-wrapper">
    <!-- Top nav -->
    <nav class="navbar">
      <div class="nav-brand">
        <span class="brand-text">IBSS Student Portal</span>
      </div>
      <div class="nav-user">
        <span>{{ auth.user?.firstName }} {{ auth.user?.lastName }} &nbsp;|&nbsp; {{ auth.user?.studentId }}</span>
        <button class="btn-logout" @click="logout">Log out</button>
      </div>
    </nav>

    <div class="container">
      <!-- Student info card -->
      <div class="info-card">
        <h2 class="section-title">Your Enrolment Information</h2>
        <div class="info-grid">
          <div class="info-item"><span class="label">First Name</span><span>{{ auth.user?.firstName }}</span></div>
          <div class="info-item"><span class="label">Last Name</span><span>{{ auth.user?.lastName }}</span></div>
          <div class="info-item"><span class="label">Passport / ID No.</span><span>{{ auth.user?.passportId }}</span></div>
          <div class="info-item info-full"><span class="label">Address</span><span>{{ auth.user?.address }}</span></div>
          <div class="info-item"><span class="label">Programme</span><span>{{ auth.user?.programme }}</span></div>
          <div class="info-item"><span class="label">Specialization</span><span>{{ auth.user?.specialization }}</span></div>
          <div class="info-item"><span class="label">Student ID</span><span>{{ auth.user?.studentId }}</span></div>
          <div class="info-item"><span class="label">Commencement Date</span><span>{{ auth.user?.commencementDate }}</span></div>
          <div class="info-item"><span class="label">Expected Completion</span><span>{{ auth.user?.expectedCompletion }}</span></div>
          <div class="info-item"><span class="label">Duration</span><span>{{ auth.user?.duration }}</span></div>
          <div class="info-item"><span class="label">Partner / Learning Center</span><span>{{ auth.user?.partner }}</span></div>
          <div class="info-item"><span class="label">Mode of Study</span><span>{{ auth.user?.modeOfStudy }}</span></div>
        </div>
      </div>

      <!-- Ask Question form -->
      <div class="question-card">
        <h2 class="section-title">Ask a Question</h2>
        <p class="card-desc">Have a question about your programme or enrolment? Submit it below and our team will get back to you.</p>

        <!-- Success banner -->
        <div v-if="submitted" class="success-banner">
          <strong>Question submitted!</strong> We have received your enquiry and will respond to
          <em>{{ form.email }}</em> within 2–3 business days.
          <button class="btn-new" @click="resetForm">Ask another question</button>
        </div>

        <form v-else @submit.prevent="submitQuestion" class="q-form">
          <div class="row-2">
            <div class="field">
              <label>Full Name <span class="req">*</span></label>
              <input v-model="form.name" type="text" required />
            </div>
            <div class="field">
              <label>Passport / ID No. <span class="req">*</span></label>
              <input v-model="form.passportId" type="text" required />
            </div>
          </div>

          <div class="field">
            <label>Email Address <span class="req">*</span></label>
            <input v-model="form.email" type="email" required />
          </div>

          <div class="field">
            <label>Address</label>
            <input v-model="form.address" type="text" />
          </div>

          <div class="row-2">
            <div class="field">
              <label>Programme</label>
              <input v-model="form.programme" type="text" readonly class="readonly" />
            </div>
            <div class="field">
              <label>Student ID</label>
              <input v-model="form.studentId" type="text" readonly class="readonly" />
            </div>
          </div>

          <div class="field">
            <label>Category <span class="req">*</span></label>
            <select v-model="form.category" required>
              <option value="">— Select a category —</option>
              <option>Academic / Curriculum</option>
              <option>Admission & Enrolment</option>
              <option>Fees & Payment</option>
              <option>Learning Center</option>
              <option>Certificates & Transcripts</option>
              <option>Other</option>
            </select>
          </div>

          <div class="field">
            <label>Your Question <span class="req">*</span></label>
            <textarea v-model="form.question" rows="5" required placeholder="Type your question here…"></textarea>
          </div>

          <div class="form-actions">
            <button type="submit" class="btn-submit">Submit Question</button>
          </div>
        </form>
      </div>

      <!-- Past questions (in-memory) -->
      <div v-if="pastQuestions.length > 0" class="history-card">
        <h2 class="section-title">Your Past Questions</h2>
        <div v-for="(q, i) in pastQuestions" :key="i" class="q-history-item">
          <div class="q-meta">
            <span class="q-cat">{{ q.category }}</span>
            <span class="q-date">{{ q.submittedAt }}</span>
          </div>
          <p class="q-text">{{ q.question }}</p>
          <span class="q-status">Pending review</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed } from 'vue'
import { useRouter } from 'vue-router'
import { auth } from '../store/auth.js'
import { mockQuestions } from '../mock/data.js'

const router = useRouter()
const submitted = ref(false)

const fullName = computed(() =>
  `${auth.user?.firstName ?? ''} ${auth.user?.lastName ?? ''}`.trim()
)

const form = reactive({
  name: fullName.value,
  passportId: auth.user?.passportId ?? '',
  email: '',
  address: auth.user?.address ?? '',
  programme: auth.user?.programme ?? '',
  studentId: auth.user?.studentId ?? '',
  category: '',
  question: '',
})

const pastQuestions = computed(() =>
  mockQuestions.filter((q) => q.studentId === auth.user?.studentId)
)

function submitQuestion() {
  mockQuestions.push({
    studentId: form.studentId,
    name: form.name,
    passportId: form.passportId,
    email: form.email,
    address: form.address,
    programme: form.programme,
    category: form.category,
    question: form.question,
    submittedAt: new Date().toLocaleString(),
  })
  submitted.value = true
}

function resetForm() {
  form.email = ''
  form.category = ''
  form.question = ''
  submitted.value = false
}

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

.brand-text {
  font-size: 1.05rem;
  font-weight: 700;
  letter-spacing: 0.02em;
}

.nav-user {
  display: flex;
  align-items: center;
  gap: 1rem;
  font-size: 0.85rem;
}

.btn-logout {
  background: transparent;
  border: 1.5px solid rgba(255,255,255,0.6);
  color: #fff;
  padding: 0.3rem 0.85rem;
  border-radius: 5px;
  cursor: pointer;
  font-size: 0.82rem;
  transition: background 0.15s;
}

.btn-logout:hover {
  background: rgba(255,255,255,0.15);
}

/* Container */
.container {
  max-width: 860px;
  margin: 2rem auto;
  padding: 0 1rem;
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

/* Cards */
.info-card,
.question-card,
.history-card {
  background: #fff;
  border-radius: 10px;
  padding: 1.75rem;
  box-shadow: 0 2px 10px rgba(0,0,0,0.07);
}

.section-title {
  font-size: 1.1rem;
  font-weight: 700;
  color: #003366;
  margin: 0 0 1.2rem;
  padding-bottom: 0.6rem;
  border-bottom: 2px solid #e8edf4;
}

.card-desc {
  color: #666;
  font-size: 0.88rem;
  margin: -0.6rem 0 1.2rem;
}

/* Info grid */
.info-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.7rem 1.5rem;
}

.info-full {
  grid-column: 1 / -1;
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
  font-size: 0.88rem;
}

.label {
  font-weight: 600;
  color: #888;
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

/* Form */
.q-form {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.row-2 {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
}

.field label {
  font-size: 0.85rem;
  font-weight: 600;
  color: #333;
}

.req {
  color: #c0392b;
}

.field input,
.field select,
.field textarea {
  padding: 0.6rem 0.85rem;
  border: 1.5px solid #ccc;
  border-radius: 7px;
  font-size: 0.9rem;
  font-family: inherit;
  outline: none;
  transition: border-color 0.2s;
  resize: vertical;
}

.field input:focus,
.field select:focus,
.field textarea:focus {
  border-color: #0055a5;
}

.readonly {
  background: #f7f9fb;
  color: #666;
  cursor: default;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
}

.btn-submit {
  padding: 0.7rem 2rem;
  background: #003366;
  color: #fff;
  border: none;
  border-radius: 7px;
  font-size: 0.95rem;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.2s;
}

.btn-submit:hover {
  background: #0055a5;
}

/* Success banner */
.success-banner {
  background: #eafaf1;
  border: 1.5px solid #2ecc71;
  border-radius: 8px;
  padding: 1.1rem 1.2rem;
  color: #1e8449;
  font-size: 0.9rem;
  display: flex;
  flex-direction: column;
  gap: 0.7rem;
}

.btn-new {
  align-self: flex-start;
  background: #27ae60;
  color: #fff;
  border: none;
  border-radius: 6px;
  padding: 0.45rem 1rem;
  cursor: pointer;
  font-size: 0.85rem;
}

/* History */
.q-history-item {
  border: 1px solid #e8edf4;
  border-radius: 8px;
  padding: 0.9rem 1rem;
  margin-bottom: 0.8rem;
}

.q-meta {
  display: flex;
  justify-content: space-between;
  font-size: 0.8rem;
  margin-bottom: 0.4rem;
}

.q-cat {
  font-weight: 700;
  color: #003366;
}

.q-date {
  color: #999;
}

.q-text {
  font-size: 0.88rem;
  color: #333;
  margin: 0 0 0.4rem;
}

.q-status {
  font-size: 0.75rem;
  background: #fff3cd;
  color: #856404;
  padding: 2px 8px;
  border-radius: 20px;
}
</style>
