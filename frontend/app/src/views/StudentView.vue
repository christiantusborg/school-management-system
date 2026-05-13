<template>
  <div class="student-portal">
    <nav class="navbar">
      <div class="navbar-brand">IBSS Student Portal — {{ displayName }}</div>
      <button class="btn-logout" @click="logout">Log Out</button>
    </nav>

    <div class="tab-content">
      <div v-if="loadError" class="err-banner">{{ loadError }}</div>
      <div v-else-if="!loaded" class="loading">Loading…</div>

      <template v-else-if="data">
        <div v-if="!data.enrollments?.length" class="empty">
          You don't have any applications yet.
        </div>

        <div v-for="enr in data.enrollments" :key="enr.enrollmentId" class="enr-card">
          <div class="enr-head">
            <div>
              <strong>{{ enr.programmeName }}</strong>
              <span class="badge-code">{{ enr.programmeCode }}</span>
              <span class="badge-specialization">{{ enr.specializationName }}</span>
            </div>
            <span class="badge-status" :class="`tone-${badgeFor(enr).tone}`">{{ badgeFor(enr).label }}</span>
          </div>

          <!-- Action banner -->
          <div v-if="enr.isRejected" class="action-banner action-bad">
            <div class="action-banner-title">{{ rejectedDocCount(enr) }} document(s) need replacement</div>
            <div v-if="enr.rejectionSummary?.byName" class="action-banner-meta">
              Returned by {{ enr.rejectionSummary.byName }} on {{ formatDate(enr.rejectionSummary.atUtc) }}
            </div>
          </div>
          <div v-else-if="enr.canAcceptOffer" class="action-banner action-blue">
            <div class="action-banner-title">Your offer is ready</div>
            <div class="action-banner-meta">Review the offer letter below, then accept to continue.</div>
            <button class="btn-primary" :disabled="busy" @click="acceptOffer(enr)">Accept Offer</button>
          </div>
          <div v-else-if="isReviewing(enr.statusCode)" class="action-banner action-info">
            Your application is being reviewed. We'll let you know as soon as there's an update.
          </div>
          <div v-else-if="enr.statusCode === 'ApplicationApprovedAdmission'" class="action-banner action-blue">
            <div class="action-banner-title">Approved by Admission</div>
            <div class="action-banner-meta">Final admission step coming soon.</div>
          </div>

          <!-- Programme summary (no Tuition) -->
          <dl class="summary">
            <div><dt>Specialization</dt><dd>{{ enr.specializationName || '—' }}</dd></div>
            <div><dt>Duration</dt><dd>{{ enr.durationOfStudyMonths ? `${enr.durationOfStudyMonths} months` : '—' }}</dd></div>
          </dl>

          <!-- Per-application documents -->
          <button type="button" class="docs-h" @click="toggleDocs(enr.enrollmentId)">
            <span class="docs-caret" :class="{ open: isDocsOpen(enr.enrollmentId) }">▶</span>
            Documents
            <span class="docs-count">({{ enr.requiredDocuments.length }})</span>
          </button>
          <template v-if="isDocsOpen(enr.enrollmentId)">
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

          <!-- Per-doc rejection cards -->
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
          </template>

          <!-- Resubmit footer (rejected applications only) -->
          <div v-if="enr.canResubmit" class="actions">
            <button
              class="btn-primary"
              :disabled="!canResubmit(enr) || busy"
              :title="canResubmit(enr) ? '' : 'Replace every rejected document first.'"
              @click="resubmit(enr)"
            >Resubmit application</button>
            <span v-if="!canResubmit(enr)" class="action-hint">Replace every rejected document first.</span>
          </div>

          <!-- Small download strip -->
          <div class="doc-strip">
            <div class="doc-mini" :class="{ disabled: !canDownloadOffer(enr) }">
              <div class="doc-mini-icon">📄</div>
              <div class="doc-mini-info">
                <div class="doc-mini-name">Offer Letter</div>
                <div class="doc-mini-sub">{{ canDownloadOffer(enr) ? 'Ready' : 'Not yet issued' }}</div>
              </div>
              <button class="btn-mini" :disabled="!canDownloadOffer(enr)" @click="downloadOffer(enr)">Download</button>
            </div>
            <div class="doc-mini" :class="{ disabled: !canDownloadAdmission(enr) }">
              <div class="doc-mini-icon">📋</div>
              <div class="doc-mini-info">
                <div class="doc-mini-name">Admission Letter</div>
                <div class="doc-mini-sub">{{ canDownloadAdmission(enr) ? 'Confirmed' : 'Available after admission' }}</div>
              </div>
              <button class="btn-mini" :disabled="!canDownloadAdmission(enr)" @click="downloadAdmission(enr)">Download</button>
            </div>
            <div class="doc-mini" :class="{ disabled: !canDownloadTranscript(enr) }">
              <div class="doc-mini-icon">📑</div>
              <div class="doc-mini-info">
                <div class="doc-mini-name">Transcript</div>
                <div class="doc-mini-sub">{{ canDownloadTranscript(enr) ? 'Ready' : 'Available after grades approved' }}</div>
              </div>
              <button class="btn-mini" :disabled="!canDownloadTranscript(enr)" @click="downloadTranscript(enr)">Download</button>
            </div>
            <div class="doc-mini" :class="{ disabled: !canDownloadCertificate(enr) }">
              <div class="doc-mini-icon">🎓</div>
              <div class="doc-mini-info">
                <div class="doc-mini-name">Certificate</div>
                <div class="doc-mini-sub">{{ canDownloadCertificate(enr) ? 'Ready' : 'Not yet available' }}</div>
              </div>
              <button class="btn-mini" :disabled="!canDownloadCertificate(enr)" @click="downloadCertificate(enr)">Download</button>
            </div>
          </div>

          <EnrollmentActivityLog :api-path="`/v1/student/me/enrollments/${enr.enrollmentId}/activity`" />
        </div>
      </template>
    </div>

    <div v-if="toast" class="toast">{{ toast }}</div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import EnrollmentActivityLog from '../components/letters/EnrollmentActivityLog.vue'
import { auth } from '../store/auth.js'
import api from '../api/client.js'
import { statusBadge, isReviewing, parseRejectionNote } from '../utils/applicationStatus.js'

const router = useRouter()
const acceptedTypes = 'application/pdf,image/jpeg,image/png'

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

// Replace is allowed only when:
//  - the doc isn't already IBSS-verified (final), AND
//  - the application is in a state where the student is the next actor.
// Locks during partner / admission review so a doc can't get swapped out
// from under the reviewer's eyes.
function canReplace(enr, doc) {
  if (doc.statusCode === 'VerifiedByEnrolment') return false
  return enr.isRejected || enr.statusCode === 'Draft'
}
function replaceLockReason(enr, doc) {
  if (doc.statusCode === 'VerifiedByEnrolment') return 'Verified — locked'
  if (isReviewing(enr.statusCode)) return 'Under review — locked'
  return 'Locked'
}

// Per-card collapse state for the Documents section. Default open.
const docsOpen = ref({})
function isDocsOpen(enrollmentId) {
  return docsOpen.value[enrollmentId] !== false
}
function toggleDocs(enrollmentId) {
  docsOpen.value[enrollmentId] = !isDocsOpen(enrollmentId)
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
  } catch (e) {
    loadError.value = e.response?.data?.error ?? e.message ?? 'Failed to load'
  } finally {
    loaded.value = true
  }
}

async function onPick(event, enr, doc) {
  const file = event.target.files?.[0]
  event.target.value = ''
  if (!file) return
  if (file.size > 10 * 1024 * 1024) {
    showToast('File is larger than 10 MB.')
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
// Accepts a "letter" object from `enr.letters.<type>` (shape:
// { studentDocumentId, fileName, uploadedAt }). Bails quietly if the
// letter hasn't been released — the button is already disabled in that
// case, but defensive guards keep the UI honest.
async function downloadLetter(letter) {
  if (!letter?.studentDocumentId) return
  try {
    const res = await api.get(
      `/v1/student/me/documents/${letter.studentDocumentId}/file`,
      { responseType: 'blob' })
    const url = URL.createObjectURL(res.data)
    // Prefer in-tab open for PDFs (browsers render natively); the
    // download attribute on a temporary anchor preserves the filename.
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

.tab-content { padding: 1.5rem 2rem; max-width: 1200px; margin: 0 auto; display: flex; flex-direction: column; gap: 0.85rem; }
.err-banner { background: #fef2f2; border: 1.5px solid #fca5a5; color: #b91c1c; padding: 0.65rem 1rem; border-radius: 7px; font-size: 0.86rem; }
.loading { color: #888; font-style: italic; padding: 2rem; text-align: center; }
.empty { color: #555; background: #fff; border-radius: 10px; padding: 1.4rem; text-align: center; box-shadow: 0 1px 4px rgba(0,0,0,.08); }

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

.docs-h {
  display: flex; align-items: center; gap: 0.4rem;
  background: none; border: none; padding: 0; cursor: pointer;
  margin: 0.85rem 0 0.4rem; color: #003366; font-size: 0.88rem; font-weight: 700;
  font-family: inherit; text-align: left;
}
.docs-h:hover { color: #0055a5; }
.docs-caret { display: inline-block; font-size: 0.65rem; transition: transform 0.15s ease; color: #5f6e85; }
.docs-caret.open { transform: rotate(90deg); }
.docs-count { color: #888; font-weight: 500; font-size: 0.78rem; }

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

.doc-strip { display: grid; grid-template-columns: repeat(3, 1fr); gap: 0.5rem; margin-top: 0.85rem; padding-top: 0.65rem; border-top: 1px solid #f0f3f7; }
.doc-mini { display: flex; align-items: center; gap: 0.45rem; padding: 0.4rem 0.6rem; border: 1px solid #e2e8f0; border-radius: 6px; background: #fafcff; }
.doc-mini.disabled { opacity: 0.55; }
.doc-mini-icon { font-size: 1.05rem; }
.doc-mini-info { flex: 1; min-width: 0; }
.doc-mini-name { font-size: 0.78rem; font-weight: 700; color: #003366; }
.doc-mini-sub { font-size: 0.68rem; color: #888; }
.btn-mini { background: #003366; color: #fff; border: none; padding: 0.22rem 0.6rem; border-radius: 5px; font-size: 0.72rem; cursor: pointer; }
.btn-mini:disabled { background: #bbb; cursor: not-allowed; }

.toast { position: fixed; bottom: 2rem; right: 2rem; background: #003366; color: #fff; padding: 0.75rem 1.4rem; border-radius: 8px; font-size: 0.9rem; box-shadow: 0 4px 16px rgba(0,0,0,.2); z-index: 9999; }
</style>
