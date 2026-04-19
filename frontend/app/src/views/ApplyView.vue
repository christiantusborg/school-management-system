<template>
  <div class="apply-page">
    <header class="apply-header">
      <div class="apply-logo">IBSS</div>
      <div class="apply-org">
        <strong>International Business School of Scandinavia</strong>
        <span>Student Application Portal</span>
      </div>
    </header>

    <!-- ── Success screen ───────────────────────────────────────────────────── -->
    <div v-if="submitted" class="apply-card success-card">
      <div class="success-icon">✓</div>
      <h2 class="success-title">Application Submitted</h2>
      <p class="success-body">Thank you, <strong>{{ submittedName }}</strong>. Your application has been received.</p>
      <div class="ref-box">
        <span class="ref-label">Application Reference</span>
        <span class="ref-id">{{ submittedId }}</span>
      </div>
      <p class="success-note">Please keep this reference number. You will be contacted by your partner institution once your application has been reviewed.</p>
      <button class="btn-new" @click="resetForm">Submit Another Application</button>
    </div>

    <!-- ── Wizard ────────────────────────────────────────────────────────────── -->
    <div v-else class="apply-card">

      <!-- Step indicator -->
      <div class="step-bar">
        <div v-for="s in STEPS" :key="s.n" class="step-item" :class="{ active: step === s.n, done: step > s.n }">
          <div class="step-circle">{{ step > s.n ? '✓' : s.n }}</div>
          <span class="step-label">{{ s.label }}</span>
          <div v-if="s.n < STEPS.length" class="step-line" :class="{ done: step > s.n }" />
        </div>
      </div>

      <!-- ── Step 1 — Personal Information ── -->
      <div v-if="step === 1" class="step-body">
        <h2 class="step-title">Personal Information</h2>
        <div class="row-2">
          <div class="field"><label>First Name <span class="req">*</span></label><input v-model="form.firstName" /></div>
          <div class="field"><label>Last Name <span class="req">*</span></label><input v-model="form.lastName" /></div>
        </div>
        <div class="row-2">
          <div class="field"><label>Date of Birth <span class="req">*</span></label><input v-model="form.dateOfBirth" type="date" /></div>
          <div class="field"><label>Email Address <span class="req">*</span></label><input v-model="form.email" type="email" /></div>
        </div>
        <div class="field"><label>Passport / ID No. <span class="req">*</span></label><input v-model="form.passportId" placeholder="e.g. A12345678" /></div>
        <div class="field"><label>Full Address</label><input v-model="form.address" placeholder="Street, City, Country" /></div>
        <div class="step-nav">
          <span />
          <button class="btn-next" :disabled="!step1Valid" @click="step = 2">Next →</button>
        </div>
      </div>

      <!-- ── Step 2 — Academic & Professional Background ── -->
      <div v-else-if="step === 2" class="step-body">
        <h2 class="step-title">Academic &amp; Professional Background</h2>
        <div class="row-2">
          <div class="field"><label>Highest Degree Obtained</label><input v-model="form.highestDegree" placeholder="e.g. Bachelor of Science" /></div>
          <div class="field"><label>Language Proficiency Result</label><input v-model="form.languageResult" placeholder="e.g. IELTS 6.5" /></div>
        </div>
        <div class="field field-narrow">
          <label>Years of Work Experience</label>
          <input v-model.number="form.yearsWorkExperience" type="number" min="0" placeholder="0" />
        </div>
        <div class="step-nav">
          <button class="btn-back" @click="step = 1">← Back</button>
          <button class="btn-next" @click="step = 3">Next →</button>
        </div>
      </div>

      <!-- ── Step 3 — Programme Details ── -->
      <div v-else-if="step === 3" class="step-body">
        <h2 class="step-title">Programme Details</h2>
        <div class="field">
          <label>Partner Institution <span class="req">*</span></label>
          <select v-model="form.partner">
            <option value="">— Select partner —</option>
            <option v-for="n in partnerNames" :key="n">{{ n }}</option>
          </select>
        </div>
        <div class="row-2">
          <div class="field">
            <label>Programme <span class="req">*</span></label>
            <select v-model="form.programme">
              <option value="">— Select —</option>
              <option v-for="n in programmeNames" :key="n">{{ n }}</option>
            </select>
          </div>
          <div class="field">
            <label>Major <span class="req">*</span></label>
            <select v-model="form.major">
              <option value="">— Select —</option>
              <option v-for="n in majorNames" :key="n">{{ n }}</option>
            </select>
          </div>
        </div>
        <div class="row-2">
          <div class="field"><label>Commencement Date <span class="req">*</span></label><input v-model="form.commencementDate" type="date" /></div>
          <div class="field"><label>Duration of Study</label><input v-model="form.durationOfStudy" placeholder="e.g. 18 months" /></div>
        </div>
        <div class="field">
          <label>Mode of Study <span class="req">*</span></label>
          <select v-model="form.modeOfStudy">
            <option value="">— Select —</option>
            <option>Distance/Online self-study</option>
            <option>Blended learning</option>
            <option>On-campus</option>
          </select>
        </div>
        <div class="step-nav">
          <button class="btn-back" @click="step = 2">← Back</button>
          <button class="btn-next" :disabled="!step3Valid" @click="step = 4">Next →</button>
        </div>
      </div>

      <!-- ── Step 4 — Supporting Documents ── -->
      <div v-else-if="step === 4" class="step-body">
        <h2 class="step-title">Supporting Documents</h2>
        <p class="step-hint">Upload scanned copies or photos. Accepted: PDF, JPG, PNG. <span class="req">*</span> Passport/ID is required.</p>
        <div class="doc-grid">
          <div class="doc-item">
            <label class="doc-label">Passport / ID <span class="req">*</span></label>
            <div class="file-drop" :class="{ 'file-drop-filled': form.docPassport }">
              <input type="file" class="file-hidden" accept=".pdf,.jpg,.jpeg,.png" @change="handleFile('docPassport', $event)" />
              <span v-if="form.docPassport" class="file-chosen">{{ form.docPassport }}</span>
              <span v-else class="file-placeholder">Click to upload</span>
            </div>
          </div>
          <div class="doc-item">
            <label class="doc-label">Degree Certificate</label>
            <div class="file-drop" :class="{ 'file-drop-filled': form.docDegree }">
              <input type="file" class="file-hidden" accept=".pdf,.jpg,.jpeg,.png" @change="handleFile('docDegree', $event)" />
              <span v-if="form.docDegree" class="file-chosen">{{ form.docDegree }}</span>
              <span v-else class="file-placeholder">Click to upload</span>
            </div>
          </div>
          <div class="doc-item">
            <label class="doc-label">Language Result</label>
            <div class="file-drop" :class="{ 'file-drop-filled': form.docLanguage }">
              <input type="file" class="file-hidden" accept=".pdf,.jpg,.jpeg,.png" @change="handleFile('docLanguage', $event)" />
              <span v-if="form.docLanguage" class="file-chosen">{{ form.docLanguage }}</span>
              <span v-else class="file-placeholder">Click to upload</span>
            </div>
          </div>
          <div class="doc-item">
            <label class="doc-label">CV / Résumé</label>
            <div class="file-drop" :class="{ 'file-drop-filled': form.docCV }">
              <input type="file" class="file-hidden" accept=".pdf,.doc,.docx" @change="handleFile('docCV', $event)" />
              <span v-if="form.docCV" class="file-chosen">{{ form.docCV }}</span>
              <span v-else class="file-placeholder">Click to upload</span>
            </div>
          </div>
        </div>
        <div class="step-nav">
          <button class="btn-back" @click="step = 3">← Back</button>
          <button class="btn-next" :disabled="!form.docPassport" @click="step = 5">Next →</button>
        </div>
      </div>

      <!-- ── Step 5 — Consent ── -->
      <div v-else class="step-body">
        <h2 class="step-title">Consent &amp; Declaration</h2>
        <div class="consent-intro">
          <p>Before submitting your application, please read the following carefully and confirm your consent.</p>
        </div>

        <div class="consent-summary">
          <h4>How we use your data</h4>
          <ul>
            <li>Your personal data is collected solely to process your application for admission.</li>
            <li>Data is shared with your selected partner institution and IBSS academic staff.</li>
            <li>We do not sell or share your data with third parties for marketing purposes.</li>
            <li>Your data will be retained for up to <strong>3 years</strong> or until you request deletion.</li>
            <li>You have the right to access, correct or delete your data at any time.</li>
          </ul>
          <button class="btn-terms-link" @click="showTerms = true">Read full Terms of Service &amp; Privacy Policy →</button>
        </div>

        <div class="consent-checks">
          <label class="consent-item" :class="{ checked: consent.processing }">
            <input type="checkbox" v-model="consent.processing" />
            <span>I consent to IBSS and my selected partner institution processing my personal data for the purpose of evaluating and managing my application, in accordance with the GDPR.</span>
          </label>
          <label class="consent-item" :class="{ checked: consent.terms }">
            <input type="checkbox" v-model="consent.terms" />
            <span>I have read and agree to the <button type="button" class="inline-link" @click.prevent="showTerms = true">Terms of Service and Privacy Policy</button>. I understand I may withdraw my consent at any time by contacting <strong>privacy@ibss.edu</strong>.</span>
          </label>
          <label class="consent-item" :class="{ checked: consent.accuracy }">
            <input type="checkbox" v-model="consent.accuracy" />
            <span>I declare that the information provided in this application is true and accurate to the best of my knowledge.</span>
          </label>
        </div>

        <div class="step-nav">
          <button class="btn-back" @click="step = 4">← Back</button>
          <button class="btn-submit" :disabled="!consentValid" @click="handleSubmit">Submit Application</button>
        </div>
      </div>
    </div>

    <!-- ── Terms & Privacy Dialog ─────────────────────────────────────────────── -->
    <transition name="fade">
      <div v-if="showTerms" class="terms-overlay" @click.self="showTerms = false" />
    </transition>
    <transition name="pop">
      <div v-if="showTerms" class="terms-dialog" role="dialog" aria-modal="true" aria-label="Terms of Service and Privacy Policy">
        <div class="terms-header">
          <h2>Terms of Service &amp; Privacy Policy</h2>
          <button class="terms-close" @click="showTerms = false" aria-label="Close">✕</button>
        </div>
        <div class="terms-body">

          <section class="terms-section">
            <h3>1. Data Controller</h3>
            <p>The data controller is <strong>International Business School of Scandinavia (IBSS)</strong>. You may contact us regarding data matters at <strong>privacy@ibss.edu</strong>.</p>
          </section>

          <section class="terms-section">
            <h3>2. What Data We Collect</h3>
            <p>We collect the personal information you provide in this application form, including your name, date of birth, contact details, passport/ID number, address, academic background, and supporting documents.</p>
          </section>

          <section class="terms-section">
            <h3>3. Purpose &amp; Legal Basis</h3>
            <p>Your data is processed for the following purposes:</p>
            <ul>
              <li>Evaluating your eligibility for admission to an IBSS programme</li>
              <li>Communicating with you regarding your application</li>
              <li>Enrolling you in the programme if your application is successful</li>
            </ul>
            <p>The legal basis for processing is <strong>your explicit consent</strong> (Article 6(1)(a) GDPR), given at the point of submitting this form.</p>
          </section>

          <section class="terms-section">
            <h3>4. Who Has Access to Your Data</h3>
            <p>Your data is accessible to:</p>
            <ul>
              <li>IBSS admissions and academic staff</li>
              <li>Your selected partner institution, for the purpose of managing your enrolment</li>
            </ul>
            <p>We do not sell, rent or share your personal data with any third party for commercial or marketing purposes.</p>
          </section>

          <section class="terms-section">
            <h3>5. Data Retention</h3>
            <p>Your application data will be retained for a maximum of <strong>3 years</strong> from the date of submission, or until you request deletion — whichever is earlier. Data associated with enrolled students may be retained for up to <strong>7 years</strong> for regulatory compliance purposes.</p>
          </section>

          <section class="terms-section">
            <h3>6. Your Rights Under GDPR</h3>
            <p>You have the following rights regarding your personal data:</p>
            <ul>
              <li><strong>Right of access</strong> — You may request a copy of the data we hold about you.</li>
              <li><strong>Right to rectification</strong> — You may request that inaccurate data be corrected.</li>
              <li><strong>Right to erasure</strong> — You may request that your data be deleted ("right to be forgotten").</li>
              <li><strong>Right to data portability</strong> — You may request your data in a portable format.</li>
              <li><strong>Right to object</strong> — You may object to the processing of your data at any time.</li>
              <li><strong>Right to withdraw consent</strong> — You may withdraw your consent at any time without affecting the lawfulness of processing based on consent before withdrawal.</li>
            </ul>
            <p>To exercise any of these rights, contact us at <strong>privacy@ibss.edu</strong>.</p>
          </section>

          <section class="terms-section">
            <h3>7. Security</h3>
            <p>We apply appropriate technical and organisational measures to protect your personal data against unauthorised access, loss or disclosure.</p>
          </section>

          <section class="terms-section">
            <h3>8. Complaints</h3>
            <p>If you believe your data has been processed unlawfully, you have the right to lodge a complaint with your national data protection authority.</p>
          </section>

        </div>
        <div class="terms-footer">
          <button class="btn-accept" @click="consent.terms = true; showTerms = false">I have read and understood this policy</button>
        </div>
      </div>
    </transition>

    <footer class="apply-footer">
      &copy; {{ year }} International Business School of Scandinavia. All rights reserved. &nbsp;·&nbsp;
      <button class="footer-link" @click="showTerms = true">Privacy Policy</button>
    </footer>
  </div>
</template>

<script setup>
import { ref, reactive, computed } from 'vue'
import { students, getNextId, nextEnrollId } from '../mock/data.js'
import { getProgrammeNames, getMajorNames, getPartnerNames } from '../mock/programmes.js'

const programmeNames = getProgrammeNames()
const majorNames     = getMajorNames()
const partnerNames   = getPartnerNames()
const year           = new Date().getFullYear()

const STEPS = [
  { n: 1, label: 'Personal Info' },
  { n: 2, label: 'Background' },
  { n: 3, label: 'Programme' },
  { n: 4, label: 'Documents' },
  { n: 5, label: 'Consent' },
]

const step      = ref(1)
const showTerms = ref(false)
const submitted    = ref(false)
const submittedId  = ref('')
const submittedName = ref('')

const blankForm = () => ({
  firstName: '', lastName: '', dateOfBirth: '', email: '',
  passportId: '', address: '',
  highestDegree: '', languageResult: '', yearsWorkExperience: 0,
  partner: '', programme: '', major: '',
  commencementDate: '', durationOfStudy: '', modeOfStudy: '',
  docPassport: null, docDegree: null, docLanguage: null, docCV: null,
})

const form = reactive(blankForm())
const consent = reactive({ processing: false, terms: false, accuracy: false })

const step1Valid   = computed(() => form.firstName && form.lastName && form.dateOfBirth && form.email && form.passportId)
const step3Valid   = computed(() => form.partner && form.programme && form.major && form.commencementDate && form.modeOfStudy)
const consentValid = computed(() => consent.processing && consent.terms && consent.accuracy)

function handleFile(field, e) {
  form[field] = e.target.files[0]?.name ?? null
}

const progCodeMap = {
  'Master of Business Administration': 'MBA',
  'Bachelor of Business Administration': 'BBA',
  'Master of Finance': 'MF',
  'Bachelor of Computer Science': 'BCS',
  'Master of Marketing': 'MM',
}

function handleSubmit() {
  const seq  = getNextId()
  const code = progCodeMap[form.programme] ?? 'GEN'
  const now  = new Date()
  const sid  = `IBSS.${code}.${String(now.getFullYear()).slice(2)}${String(now.getMonth()+1).padStart(2,'0')}${String(seq).padStart(4,'0')}`

  students.push({
    id: seq, studentId: sid,
    firstName: form.firstName, lastName: form.lastName,
    dateOfBirth: form.dateOfBirth, email: form.email,
    passportId: form.passportId, address: form.address,
    partner: form.partner,
    highestDegree: form.highestDegree, languageResult: form.languageResult,
    yearsWorkExperience: form.yearsWorkExperience,
    docPassport: form.docPassport, docDegree: form.docDegree,
    docLanguage: form.docLanguage, docCV: form.docCV,
    docsVerified: { passport: false, degree: false, language: false, cv: false },
    enrollments: [{
      id: nextEnrollId(),
      programme: form.programme, major: form.major,
      commencementDate: form.commencementDate, durationOfStudy: form.durationOfStudy,
      modeOfStudy: form.modeOfStudy,
      selectedPathway: null, offerType: null,
      paymentDone: false, admissionConfirmed: false,
      missingDocsSubmitted: false, certReleased: false,
      enrollmentStatus: 'Potential applicant',
      coursesCompleted: 0, coursesRequired: 8,
      finalProjectStatus: 'not applicable',
      transcriptReleased: false,
      tuitionFeeStatus: 'unpaid', otherFeesStatus: 'not applicable',
      changeNotes: [],
    }],
  })

  submittedId.value   = sid
  submittedName.value = `${form.firstName} ${form.lastName}`
  submitted.value     = true
}

function resetForm() {
  Object.assign(form, blankForm())
  Object.assign(consent, { processing: false, terms: false, accuracy: false })
  step.value      = 1
  submitted.value = false
}
</script>

<style scoped>
.apply-page {
  min-height: 100vh;
  background: linear-gradient(135deg, #eaf1fb 0%, #f2f5f9 100%);
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 0 1rem 3rem;
}

/* Header */
.apply-header { width: 100%; max-width: 800px; display: flex; align-items: center; gap: 1rem; padding: 2rem 0 1.5rem; }
.apply-logo { font-size: 2.4rem; font-weight: 900; color: #003366; letter-spacing: -1px; line-height: 1; flex-shrink: 0; }
.apply-org { display: flex; flex-direction: column; }
.apply-org strong { font-size: 1rem; color: #003366; }
.apply-org span { font-size: 0.82rem; color: #666; margin-top: 1px; }

/* Card */
.apply-card { width: 100%; max-width: 800px; background: #fff; border-radius: 14px; box-shadow: 0 4px 28px rgba(0,0,0,0.09); padding: 2rem 2.5rem 2rem; }

/* Step bar */
.step-bar { display: flex; align-items: flex-start; justify-content: space-between; margin-bottom: 2.25rem; position: relative; }
.step-item { display: flex; flex-direction: column; align-items: center; position: relative; flex: 1; }
.step-circle {
  width: 34px; height: 34px; border-radius: 50%;
  display: flex; align-items: center; justify-content: center;
  font-size: 0.82rem; font-weight: 700;
  border: 2.5px solid #d0dbe8; background: #f2f5f9; color: #999;
  transition: all 0.2s; z-index: 1;
}
.step-item.active .step-circle { border-color: #003366; background: #003366; color: #fff; }
.step-item.done  .step-circle { border-color: #0d6b55; background: #d1fae5; color: #065f46; }
.step-label { font-size: 0.72rem; font-weight: 600; color: #aaa; margin-top: 0.4rem; white-space: nowrap; text-align: center; }
.step-item.active .step-label { color: #003366; }
.step-item.done  .step-label  { color: #0d6b55; }
.step-line { position: absolute; top: 17px; left: calc(50% + 17px); right: calc(-50% + 17px); height: 2px; background: #d0dbe8; z-index: 0; }
.step-line.done { background: #0d6b55; }

/* Step body */
.step-body { display: flex; flex-direction: column; gap: 1rem; }
.step-title { font-size: 1.15rem; font-weight: 700; color: #003366; margin: 0 0 0.25rem; }
.step-hint { font-size: 0.82rem; color: #888; margin: 0; }

/* Fields */
.row-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 0.85rem; }
.field { display: flex; flex-direction: column; gap: 0.3rem; }
.field-narrow { max-width: 200px; }
.field label { font-size: 0.82rem; font-weight: 600; color: #444; }
.req { color: #c0392b; }
.field input, .field select, .step-body input:not(.file-hidden), .step-body select {
  padding: 0.6rem 0.8rem; border: 1.5px solid #ccc; border-radius: 7px;
  font-size: 0.9rem; font-family: inherit; outline: none; background: #fff;
}
.field input:focus, .field select:focus { border-color: #003366; }

/* Navigation */
.step-nav { display: flex; justify-content: space-between; align-items: center; padding-top: 0.75rem; border-top: 1px solid #f0f3f7; margin-top: 0.5rem; }
.btn-back { padding: 0.62rem 1.25rem; background: #f2f5f9; border: 1.5px solid #ccc; border-radius: 7px; font-size: 0.9rem; cursor: pointer; color: #555; font-weight: 600; }
.btn-back:hover { border-color: #999; }
.btn-next { padding: 0.65rem 1.75rem; background: #003366; color: #fff; border: none; border-radius: 7px; font-size: 0.9rem; font-weight: 700; cursor: pointer; }
.btn-next:hover:not(:disabled) { background: #0055a5; }
.btn-next:disabled { opacity: 0.38; cursor: default; }
.btn-submit { padding: 0.65rem 2rem; background: #0d6b55; color: #fff; border: none; border-radius: 7px; font-size: 0.9rem; font-weight: 700; cursor: pointer; }
.btn-submit:hover:not(:disabled) { background: #0a5242; }
.btn-submit:disabled { opacity: 0.38; cursor: default; }

/* Documents */
.doc-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 0.85rem; }
.doc-item { display: flex; flex-direction: column; gap: 0.3rem; }
.doc-label { font-size: 0.82rem; font-weight: 600; color: #444; }
.file-drop { position: relative; border: 1.5px dashed #b0c4de; border-radius: 7px; padding: 0.75rem 1rem; background: #f7fbff; cursor: pointer; transition: border-color 0.15s, background 0.15s; overflow: hidden; min-height: 44px; display: flex; align-items: center; }
.file-drop:hover { border-color: #003366; background: #eef4fb; }
.file-drop-filled { border-style: solid; border-color: #0d6b55; background: #edfaf5; }
.file-hidden { position: absolute; inset: 0; opacity: 0; cursor: pointer; width: 100%; height: 100%; }
.file-placeholder { font-size: 0.82rem; color: #aaa; pointer-events: none; }
.file-chosen { font-size: 0.82rem; color: #0d6b55; font-weight: 600; pointer-events: none; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }

/* Consent step */
.consent-intro { font-size: 0.88rem; color: #555; background: #f7fbff; border: 1px solid #d0e4f5; border-radius: 8px; padding: 0.85rem 1.1rem; }
.consent-summary { background: #fafbfc; border: 1.5px solid #e0e8f0; border-radius: 9px; padding: 1rem 1.25rem; font-size: 0.86rem; color: #444; }
.consent-summary h4 { font-size: 0.88rem; font-weight: 700; color: #003366; margin: 0 0 0.6rem; }
.consent-summary ul { margin: 0 0 0.75rem; padding-left: 1.25rem; line-height: 1.7; }
.consent-summary li { margin-bottom: 0.2rem; }
.btn-terms-link { background: none; border: none; color: #0055a5; font-size: 0.84rem; font-weight: 600; cursor: pointer; padding: 0; text-decoration: underline; }
.btn-terms-link:hover { color: #003366; }

.consent-checks { display: flex; flex-direction: column; gap: 0.65rem; }
.consent-item {
  display: flex; align-items: flex-start; gap: 0.75rem;
  font-size: 0.86rem; color: #333; line-height: 1.55;
  padding: 0.75rem 1rem; border: 1.5px solid #e0e8f0; border-radius: 8px;
  cursor: pointer; background: #fafbfc; transition: border-color 0.15s;
}
.consent-item.checked { border-color: #0d6b55; background: #edfaf5; }
.consent-item input[type=checkbox] { width: 16px; height: 16px; flex-shrink: 0; margin-top: 2px; cursor: pointer; accent-color: #003366; }
.inline-link { background: none; border: none; color: #0055a5; font-size: inherit; font-weight: 600; cursor: pointer; padding: 0; text-decoration: underline; }
.inline-link:hover { color: #003366; }

/* Terms dialog */
.terms-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.5); z-index: 200; }
.terms-dialog {
  position: fixed; top: 50%; left: 50%; transform: translate(-50%, -50%);
  width: 680px; max-width: 96vw; max-height: 85vh;
  background: #fff; border-radius: 14px;
  box-shadow: 0 16px 64px rgba(0,0,0,0.25);
  z-index: 201; display: flex; flex-direction: column;
}
.terms-header { display: flex; align-items: center; justify-content: space-between; padding: 1.25rem 1.75rem; border-bottom: 1.5px solid #e8edf4; flex-shrink: 0; }
.terms-header h2 { font-size: 1.05rem; font-weight: 700; color: #003366; margin: 0; }
.terms-close { background: none; border: none; font-size: 1.15rem; color: #888; cursor: pointer; }
.terms-close:hover { color: #333; }
.terms-body { flex: 1; overflow-y: auto; padding: 1.5rem 1.75rem; }
.terms-section { margin-bottom: 1.5rem; }
.terms-section h3 { font-size: 0.9rem; font-weight: 700; color: #003366; margin: 0 0 0.5rem; }
.terms-section p { font-size: 0.86rem; color: #444; line-height: 1.65; margin: 0 0 0.5rem; }
.terms-section ul { font-size: 0.86rem; color: #444; line-height: 1.65; margin: 0.4rem 0 0.5rem; padding-left: 1.25rem; }
.terms-section li { margin-bottom: 0.25rem; }
.terms-footer { padding: 1rem 1.75rem; border-top: 1px solid #e8edf4; display: flex; justify-content: flex-end; flex-shrink: 0; }
.btn-accept { background: #003366; color: #fff; border: none; border-radius: 7px; padding: 0.65rem 1.75rem; font-size: 0.9rem; font-weight: 700; cursor: pointer; }
.btn-accept:hover { background: #0055a5; }

/* Success */
.success-card { display: flex; flex-direction: column; align-items: center; text-align: center; gap: 0.75rem; padding: 3rem 2.5rem; }
.success-icon { width: 60px; height: 60px; border-radius: 50%; background: #d1fae5; color: #065f46; font-size: 1.8rem; font-weight: 900; display: flex; align-items: center; justify-content: center; border: 2.5px solid #6ee7b7; }
.success-title { font-size: 1.4rem; font-weight: 700; color: #003366; margin: 0; }
.success-body { font-size: 0.95rem; color: #444; margin: 0; }
.ref-box { display: flex; flex-direction: column; align-items: center; gap: 0.3rem; background: #eaf1fb; border: 2px solid #b8d9f5; border-radius: 10px; padding: 1rem 2.5rem; margin: 0.5rem 0; }
.ref-label { font-size: 0.76rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.06em; color: #666; }
.ref-id { font-family: ui-monospace, monospace; font-size: 1.3rem; font-weight: 700; color: #003366; letter-spacing: 1px; }
.success-note { font-size: 0.82rem; color: #888; max-width: 440px; line-height: 1.55; margin: 0; }
.btn-new { margin-top: 0.5rem; background: none; border: 1.5px solid #003366; color: #003366; border-radius: 7px; padding: 0.55rem 1.5rem; font-size: 0.9rem; font-weight: 600; cursor: pointer; }
.btn-new:hover { background: #eaf1fb; }

/* Footer */
.apply-footer { margin-top: 2rem; font-size: 0.78rem; color: #aaa; text-align: center; }
.footer-link { background: none; border: none; color: #aaa; font-size: 0.78rem; cursor: pointer; text-decoration: underline; padding: 0; }
.footer-link:hover { color: #666; }

/* Transitions */
.fade-enter-active, .fade-leave-active { transition: opacity 0.2s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
.pop-enter-active, .pop-leave-active { transition: opacity 0.2s, transform 0.2s; }
.pop-enter-from, .pop-leave-to { opacity: 0; transform: translate(-50%, -47%) scale(0.96); }
</style>
