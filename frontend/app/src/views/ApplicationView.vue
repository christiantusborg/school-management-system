<template>
  <div class="app-page">
    <nav class="navbar">
      <span class="brand">IBSS Application</span>
      <div class="nav-right">
        <span class="user">{{ auth.user?.displayName }}</span>
        <button class="btn-logout" @click="logout">Log out</button>
      </div>
    </nav>

    <div class="container">
      <div v-if="loadError" class="err-banner">{{ loadError }}</div>
      <div v-else-if="!loaded" class="loading">Loading…</div>
      <template v-else-if="data">
        <header class="page-head">
          <div>
            <h1>{{ headingName }}</h1>
            <p class="sub">
              <span v-if="data.studentNumber">Student #{{ data.studentNumber }}</span>
              <span v-if="data.partner?.name"> · {{ data.partner.name }}</span>
            </p>
          </div>
          <a v-if="data.partner?.contactEmail" class="btn-ghost" :href="`mailto:${data.partner.contactEmail}`">Contact partner</a>
        </header>

        <div v-if="!data.enrollments?.length" class="empty">
          You don't have any applications yet.
        </div>

        <section v-for="enr in data.enrollments" :key="enr.enrollmentId" class="enr-card">
          <div class="enr-head">
            <div>
              <strong>{{ enr.programmeName }}</strong>
              <span class="badge-code">{{ enr.programmeCode }}</span>
              <span class="badge-specialization">{{ enr.specializationName }}</span>
            </div>
            <span class="badge-status" :class="`tone-${badgeFor(enr).tone}`">{{ badgeFor(enr).label }}</span>
          </div>

          <!-- Rejection banner -->
          <div v-if="enr.isRejected" class="reject-banner">
            <div class="reject-banner-title">Your application was sent back — please update the items below.</div>
            <div v-if="enr.rejectionSummary" class="reject-banner-meta">
              by {{ enr.rejectionSummary.byName ?? 'Reviewer' }} on {{ formatDate(enr.rejectionSummary.atUtc) }}
            </div>
            <pre v-if="enr.rejectionSummary?.note" class="reject-banner-note">{{ enr.rejectionSummary.note }}</pre>
          </div>

          <!-- Awaiting review banner -->
          <div v-else-if="isReviewing(enr.statusCode)" class="info-banner">
            Your application is being reviewed. We'll let you know as soon as there's an update.
          </div>

          <!-- Programme summary -->
          <dl class="summary">
            <div><dt>Specialization</dt><dd>{{ enr.specializationName || '—' }}</dd></div>
            <div><dt>Duration</dt><dd>{{ enr.durationOfStudyMonths ? `${enr.durationOfStudyMonths} months` : '—' }}</dd></div>
          </dl>

          <h3 class="docs-h">Documents</h3>
          <ul class="doc-list">
            <li v-for="doc in enr.requiredDocuments" :key="doc.slot" class="doc-row">
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
                <span v-else class="lock-note">Verified — locked</span>
              </div>
            </li>
          </ul>

          <!-- Per-doc rejection cards -->
          <div v-for="doc in enr.requiredDocuments.filter(d => d.isRejected && d.rejectionReasons)" :key="`r-${doc.slot}`" class="reject-card">
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

          <!-- Action footer -->
          <div class="actions">
            <template v-if="enr.canResubmit">
              <button
                class="btn-primary"
                :disabled="!canResubmit(enr) || busy"
                :title="canResubmit(enr) ? '' : 'Replace every rejected document first.'"
                @click="resubmit(enr)"
              >Resubmit application</button>
              <span v-if="!canResubmit(enr)" class="action-hint">Replace every rejected document first.</span>
            </template>
            <button
              v-else-if="enr.canAcceptOffer"
              class="btn-primary btn-accept"
              :disabled="busy"
              @click="acceptOffer(enr)"
            >Accept Offer</button>
          </div>
        </section>
      </template>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { auth } from '../store/auth.js'
import api from '../api/client.js'
import { statusBadge, isReviewing, parseRejectionNote } from '../utils/applicationStatus.js'

const router = useRouter()
const acceptedTypes = 'application/pdf,image/jpeg,image/png'

const data = ref(null)
const loaded = ref(false)
const loadError = ref('')
const busy = ref(false)

const headingName = computed(() => {
  const first = data.value?.account?.firstName
  const last = data.value?.account?.lastName
  const name = [first, last].filter(Boolean).join(' ').trim()
  return name ? `${name}'s Application` : 'Your Application'
})

function badgeFor(enr) { return statusBadge(enr.statusCode) }

function docPillTone(doc) {
  if (!doc.statusCode) return 'tone-grey'
  if (doc.statusCode === 'VerifiedByEnrolment' || doc.statusCode === 'VerifiedByPartner') return 'tone-green'
  if (doc.statusCode === 'RejectedByPartner' || doc.statusCode === 'RejectedByEnrolment') return 'tone-red'
  return 'tone-amber'
}

function canReplace(enr, doc) {
  // Locked once admission has verified, AND while the application is
  // under partner / admission review (don't swap docs out from under
  // the reviewer). Allowed only when the student is the next actor —
  // i.e. the application is rejected back to them or still a draft.
  if (doc.statusCode === 'VerifiedByEnrolment') return false
  return enr.isRejected || enr.statusCode === 'Draft'
}

function canResubmit(enr) {
  // Every required slot must be uploaded and not currently rejected.
  return enr.requiredDocuments.every(d => d.uploaded
    && d.statusCode !== 'RejectedByPartner'
    && d.statusCode !== 'RejectedByEnrolment')
}

function parsedReasons(doc) { return parseRejectionNote(doc.rejectionReasons?.note) }

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
    loadError.value = 'File is larger than 10 MB.'
    return
  }
  loadError.value = ''
  busy.value = true
  const fd = new FormData()
  fd.append('enrollmentId', enr.enrollmentId)
  fd.append('documentTypeId', doc.documentTypeId)
  fd.append('file', file)
  try {
    await api.post('/v1/student/me/documents', fd, { headers: { 'Content-Type': 'multipart/form-data' } })
    await load()
  } catch (e) {
    loadError.value = e.response?.data?.error ?? e.message ?? 'Upload failed'
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
  } catch (e) {
    loadError.value = e.response?.data?.error ?? e.message ?? 'Resubmit failed'
  } finally {
    busy.value = false
  }
}

async function acceptOffer(enr) {
  busy.value = true
  try {
    await api.post(`/v1/student/me/application/${enr.enrollmentId}/accept-offer`)
    await load()
  } catch (e) {
    loadError.value = e.response?.data?.error ?? e.message ?? 'Accept failed'
  } finally {
    busy.value = false
  }
}

function logout() {
  auth.logout()
  router.push('/login')
}

onMounted(load)
</script>

<style scoped>
.app-page { min-height: 100vh; background: #f2f5f9; }
.navbar { background: #003366; color: #fff; display: flex; justify-content: space-between; padding: 0.85rem 2rem; }
.brand { font-weight: 700; }
.nav-right { display: flex; align-items: center; gap: 1rem; }
.user { font-size: 0.88rem; opacity: 0.85; }
.btn-logout { background: rgba(255,255,255,0.12); border: 1px solid rgba(255,255,255,0.25); color: #fff; border-radius: 5px; padding: 0.3rem 0.8rem; cursor: pointer; }

.container { max-width: 820px; margin: 2rem auto; padding: 0 1.25rem; }
.page-head { display: flex; align-items: flex-start; justify-content: space-between; margin-bottom: 1.25rem; gap: 1rem; }
.page-head h1 { color: #003366; margin: 0; font-size: 1.45rem; }
.sub { color: #888; margin: 0.2rem 0 0; font-size: 0.85rem; }
.btn-ghost { color: #003366; border: 1.5px solid #d0d7e0; background: #fff; padding: 0.4rem 0.9rem; border-radius: 6px; text-decoration: none; font-size: 0.85rem; align-self: flex-start; }

.err-banner { background: #fef2f2; border: 1.5px solid #fca5a5; color: #b91c1c; padding: 0.65rem 1rem; border-radius: 7px; margin-bottom: 1rem; font-size: 0.86rem; }
.loading { color: #888; font-style: italic; padding: 2rem; text-align: center; }
.empty { color: #555; background: #fff; border-radius: 10px; padding: 1.4rem; text-align: center; box-shadow: 0 2px 8px rgba(0,0,0,0.05); }

.enr-card { background: #fff; border-radius: 10px; padding: 1.2rem 1.4rem; margin-bottom: 1.25rem; box-shadow: 0 2px 8px rgba(0,0,0,0.05); }
.enr-head { display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 0.5rem; margin-bottom: 0.75rem; }
.badge-code { background: #e8f0f8; color: #003366; border-radius: 4px; padding: 1px 8px; font-size: 0.74rem; font-weight: 700; margin-left: 0.5rem; }
.badge-specialization { background: #f0f3f7; color: #555; border-radius: 4px; padding: 1px 8px; font-size: 0.74rem; margin-left: 0.4rem; }
.badge-status { font-size: 0.74rem; font-weight: 700; padding: 3px 10px; border-radius: 4px; text-transform: uppercase; letter-spacing: 0.02em; }

.tone-grey  { background: #f0f3f7; color: #555; }
.tone-amber { background: #fff3cd; color: #856404; }
.tone-blue  { background: #cfe2ff; color: #084298; }
.tone-green { background: #d1fae5; color: #065f46; }
.tone-red   { background: #fee2e2; color: #b91c1c; }

.reject-banner {
  background: #fef2f2;
  border: 1.5px solid #fca5a5;
  border-radius: 8px;
  padding: 0.85rem 1rem;
  margin: 0.75rem 0 1rem;
}
.reject-banner-title { color: #b91c1c; font-weight: 700; font-size: 0.95rem; }
.reject-banner-meta { color: #7f1d1d; font-size: 0.8rem; margin-top: 0.15rem; }
.reject-banner-note {
  margin: 0.5rem 0 0;
  font-family: inherit; font-size: 0.86rem; color: #444;
  white-space: pre-wrap; background: #fff; border-radius: 6px;
  padding: 0.6rem 0.75rem; border: 1px solid #fbcaca;
}

.info-banner {
  background: #eef5ff; border: 1.5px solid #b6d4fe;
  color: #084298; padding: 0.65rem 0.9rem;
  border-radius: 7px; font-size: 0.86rem;
  margin: 0.5rem 0 1rem;
}

.summary { display: grid; grid-template-columns: repeat(auto-fill, minmax(170px, 1fr)); gap: 0.65rem 1.25rem; margin: 0.5rem 0 1rem; padding: 0; }
.summary div { display: flex; flex-direction: column; }
.summary dt { font-size: 0.72rem; text-transform: uppercase; letter-spacing: 0.04em; color: #888; margin: 0; }
.summary dd { font-size: 0.9rem; color: #222; margin: 0.1rem 0 0; }

.docs-h { margin: 1rem 0 0.5rem; color: #003366; font-size: 0.95rem; }

.doc-list { list-style: none; padding: 0; margin: 0; }
.doc-row { display: flex; justify-content: space-between; align-items: flex-start; padding: 0.65rem 0; border-bottom: 1px solid #f0f3f7; gap: 1rem; }
.doc-row:last-child { border-bottom: none; }
.doc-info { display: flex; gap: 0.6rem; align-items: flex-start; flex: 1; }
.doc-mark { width: 22px; height: 22px; display: inline-flex; align-items: center; justify-content: center; border-radius: 50%; font-weight: 700; flex-shrink: 0; font-size: 0.92rem; }
.mark-ok { background: #d1fae5; color: #065f46; }
.mark-bad { background: #fee2e2; color: #b91c1c; }
.mark-pending { background: #f0f3f7; color: #aaa; }
.doc-text { display: flex; flex-direction: column; gap: 0.2rem; }
.doc-meta { color: #888; font-size: 0.78rem; margin: 0; }
.doc-pill { display: inline-block; font-size: 0.7rem; font-weight: 600; padding: 2px 8px; border-radius: 4px; margin-top: 0.2rem; align-self: flex-start; }

.doc-actions { display: flex; gap: 0.4rem; align-items: center; }
.btn-upload {
  background: #003366; color: #fff; padding: 0.32rem 0.85rem;
  border-radius: 5px; font-size: 0.8rem; font-weight: 600; cursor: pointer;
}
.btn-upload input { display: none; }
.lock-note { color: #888; font-size: 0.78rem; font-style: italic; }

.reject-card {
  background: #fff7f7; border: 1px solid #fbcaca;
  border-left: 3px solid #b91c1c; border-radius: 6px;
  padding: 0.7rem 0.85rem; margin: 0.65rem 0 0;
}
.reject-card-head { display: flex; gap: 0.6rem; align-items: baseline; flex-wrap: wrap; font-size: 0.83rem; color: #7f1d1d; }
.reject-card-head strong { color: #b91c1c; }
.reject-card-date { margin-left: auto; color: #999; font-size: 0.76rem; }
.reject-chips { margin-top: 0.45rem; display: flex; flex-wrap: wrap; gap: 0.3rem; }
.reject-chip { background: #fee2e2; color: #b91c1c; border-radius: 12px; font-size: 0.74rem; padding: 2px 9px; }
.reject-free { margin: 0.5rem 0 0; color: #555; font-size: 0.84rem; white-space: pre-wrap; }

.actions { margin-top: 1rem; padding-top: 0.85rem; border-top: 1px solid #f0f3f7; display: flex; align-items: center; gap: 0.85rem; flex-wrap: wrap; }
.btn-primary {
  background: #16a34a; color: #fff; border: none;
  padding: 0.55rem 1.4rem; border-radius: 7px;
  font-weight: 700; font-size: 0.9rem; cursor: pointer;
}
.btn-primary:hover:not(:disabled) { background: #15803d; }
.btn-primary:disabled { background: #aaa; cursor: not-allowed; }
.btn-accept { background: #0055a5; }
.btn-accept:hover:not(:disabled) { background: #003d7a; }
.action-hint { color: #888; font-size: 0.82rem; }
</style>
