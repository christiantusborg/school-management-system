<template>
  <div class="cp-tab">
    <div v-if="loadError" class="err-banner">{{ loadError }}</div>
    <div v-if="loading" class="empty-state-card">Loading…</div>
    <div v-else-if="!partnerId" class="empty-state-card">No partner selected.</div>
    <div v-else-if="programmes.length === 0" class="empty-state-card">This partner has not created any programmes yet.</div>

    <div v-else>
      <div class="filter-bar-mini">
        <label class="filter-label">Status:</label>
        <select v-model="statusFilter" class="inp-status">
          <option value="">Any</option>
          <option value="Draft">Draft</option>
          <option value="Pending">Pending</option>
          <option value="Approved">Approved</option>
          <option value="Rejected">Rejected</option>
        </select>
      </div>

      <div v-for="c in filtered" :key="c.programmeId" class="clone-card">
        <div class="clone-row">
        <div class="clone-main clickable" @click="toggleExpand(c.programmeId)">
          <span class="caret">{{ expanded.has(c.programmeId) ? '▾' : '▸' }}</span>
          <strong>{{ c.name }}</strong>
          <span class="mono-code">{{ c.code }}</span>
          <span :class="['status-pill', `s-${c.status.toLowerCase()}`]">{{ c.status }}</span>
          <span v-if="c.isDisabledByAdmin" class="status-pill s-disabled">Disabled by Admin</span>
          <span v-else-if="c.isActive && c.status === 'Approved'" class="status-pill s-active">Active</span>
          <span v-if="c.hasEnrolments" class="status-pill s-enrolled">Enrolments</span>
        </div>

        <div class="clone-actions">
          <template v-if="c.status === 'Pending'">
            <button class="btn-sm btn-ok" :disabled="busy.has(c.programmeId)" @click="approve(c)">Approve</button>
            <button v-if="rejectingId !== c.programmeId" class="btn-sm btn-warn" @click="openReject(c)">Reject</button>
            <template v-else>
              <input v-model="rejectReason" class="inp-add" placeholder="Reason for rejection…" />
              <button class="btn-sm btn-warn" :disabled="!rejectReason.trim() || busy.has(c.programmeId)" @click="confirmReject(c)">Confirm</button>
              <button class="btn-sm" @click="cancelReject">Cancel</button>
            </template>
          </template>
          <template v-else-if="c.status === 'Rejected'">
            <span class="reject-reason">Rejected: {{ c.rejectionReason || '—' }}</span>
            <button class="btn-sm btn-ok" :disabled="busy.has(c.programmeId)" @click="approve(c)">Approve</button>
            <button class="btn-sm" :disabled="busy.has(c.programmeId)" @click="reopen(c)">Move to Pending</button>
          </template>
          <button class="btn-sm" :disabled="busy.has(c.programmeId)" @click="toggleDisable(c)">
            {{ c.isDisabledByAdmin ? 'Enable' : 'Disable' }}
          </button>
        </div>
        </div>

        <div v-if="expanded.has(c.programmeId)" class="clone-details">
          <div class="detail-row">
            <span class="detail-label">Description</span>
            <span class="detail-value">{{ c.description?.trim() || '—' }}</span>
          </div>
          <div class="detail-row">
            <span class="detail-label">Award on completion</span>
            <span class="detail-value">{{ awardName(c.awardEducationLevelId) || '—' }}</span>
          </div>
          <div class="detail-row">
            <span class="detail-label">Pathways</span>
            <span class="detail-value">{{ pathwayNames(c.pathwayIds) || '—' }}</span>
          </div>
          <div v-if="c.rejectionReason" class="detail-row">
            <span class="detail-label">Rejection reason</span>
            <span class="detail-value">{{ c.rejectionReason }}</span>
          </div>

          <div class="section-label">Specializations &amp; Subjects</div>
          <div v-if="specsFor(c.programmeId).length === 0" class="empty-note">No specializations.</div>
          <div v-for="s in specsFor(c.programmeId)" :key="s.specializationId" class="spec-card">
            <div class="spec-row" @click="toggleSpec(s.specializationId)">
              <span class="caret">{{ openSpecs.has(s.specializationId) ? '▾' : '▸' }}</span>
              <strong>{{ s.name }}</strong>
              <span class="mono-code">{{ s.code }}</span>
              <span class="muted">{{ subjectsFor(s.specializationId).length }} subjects</span>
              <span v-if="s.instructionLanguage" class="pill">{{ s.instructionLanguage }}</span>
              <span class="pill" :class="(s.offerAcceptanceMode === 'AutoAccept') ? 'pill-auto' : 'pill-student'">
                {{ s.offerAcceptanceMode === 'AutoAccept' ? 'auto-accept' : 'student-accept' }}
              </span>
            </div>
            <div v-if="openSpecs.has(s.specializationId)" class="subj-table">
              <div class="subj-header">
                <span class="col-code">Code</span>
                <span class="col-name">Subject</span>
                <span class="col-ects">ECTS</span>
              </div>
              <div v-if="subjectsFor(s.specializationId).length === 0" class="empty-note">No subjects.</div>
              <div v-for="sub in subjectsFor(s.specializationId)" :key="sub.subjectId" class="subj-row">
                <span class="col-code mono-code">{{ sub.code || '—' }}</span>
                <span class="col-name">{{ sub.name }}</span>
                <span class="col-ects">{{ sub.ects }}</span>
              </div>
            </div>
          </div>
        </div>

        <LetterButtonsRow :programme-id="c.programmeId" :programme-name="c.name" :partner-id="partnerId" />
      </div>
    </div>
  </div>
</template>

<script setup>
import { reactive, ref, computed, watch, onMounted } from 'vue'
import apiClient from '../../../api/client.js'
import LetterButtonsRow from '../../letters/LetterButtonsRow.vue'

const props = defineProps({
  partnerId: { type: String, default: '' },
  partnerName: { type: String, default: '' },
})

const programmes = ref([])
const specializations = ref([])
const subjects = ref([])
const educationLevels = ref([])
const pathways = ref([])
const loading = ref(false)
const loadError = ref('')
const statusFilter = ref('')
const rejectingId = ref(null)
const rejectReason = ref('')
const busy = ref(new Set())
const expanded = reactive(new Set()) // programmeId
const openSpecs = reactive(new Set()) // specializationId

function toggleExpand(id) {
  expanded.has(id) ? expanded.delete(id) : expanded.add(id)
}
function toggleSpec(id) {
  openSpecs.has(id) ? openSpecs.delete(id) : openSpecs.add(id)
}

function specsFor(programmeId) {
  return specializations.value.filter(s => s.programmeId === programmeId && !s.deletedAt)
}
function subjectsFor(specializationId) {
  return subjects.value.filter(s => s.specializationId === specializationId && !s.deletedAt)
}
function awardName(id) {
  if (!id) return ''
  const e = educationLevels.value.find(x => x.educationLevelId === id)
  return e?.name ?? ''
}
function pathwayNames(ids) {
  if (!ids?.length) return ''
  return ids
    .map(id => pathways.value.find(p => p.pathwayId === id)?.name)
    .filter(Boolean)
    .join(', ')
}

const filtered = computed(() =>
  statusFilter.value
    ? programmes.value.filter(p => p.status === statusFilter.value)
    : programmes.value
)

async function load() {
  if (!props.partnerId) { programmes.value = []; return }
  loading.value = true
  loadError.value = ''
  try {
    // Load programmes + their child catalogue data in parallel so the inline
    // expansion can render specializations / subjects / pathways without a
    // second roundtrip per row.
    const [progRes, specRes, subRes, pathRes, elRes] = await Promise.all([
      apiClient.get('/v1/school/programmes?ownership=partner'),
      apiClient.get('/v1/school/specializations'),
      apiClient.get('/v1/school/subjects'),
      apiClient.get('/v1/school/system-config/pathways'),
      apiClient.get('/v1/school/system-config/education-levels'),
    ])
    // The list endpoint exposes the owner under `ownerId`, not `partnerId` —
    // the previous filter never matched, so partner-owned programmes were
    // silently hidden in the admin drawer.
    programmes.value = (progRes.data.items ?? []).filter(p => p.ownerId === props.partnerId)
    specializations.value = specRes.data.items ?? []
    subjects.value        = subRes.data.items ?? []
    pathways.value        = pathRes.data.items ?? []
    educationLevels.value = elRes.data.items ?? []
  } catch (e) {
    loadError.value = e.response?.data?.message ?? e.message ?? 'Failed to load'
  } finally {
    loading.value = false
  }
}

async function approve(c) {
  if (busy.value.has(c.programmeId)) return
  busy.value.add(c.programmeId)
  try {
    await apiClient.post(`/v1/school/programmes/${c.programmeId}/approve`)
    await load()
  } catch (e) { loadError.value = e.response?.data?.error ?? e.message ?? 'Approve failed' }
  finally { busy.value.delete(c.programmeId) }
}

function openReject(c) {
  rejectingId.value = c.programmeId
  rejectReason.value = ''
}

async function confirmReject(c) {
  const reason = rejectReason.value.trim()
  if (!reason) return
  if (busy.value.has(c.programmeId)) return
  busy.value.add(c.programmeId)
  try {
    await apiClient.post(`/v1/school/programmes/${c.programmeId}/reject`, { reason })
    rejectingId.value = null
    rejectReason.value = ''
    await load()
  } catch (e) { loadError.value = e.response?.data?.error ?? e.message ?? 'Reject failed' }
  finally { busy.value.delete(c.programmeId) }
}

function cancelReject() {
  rejectingId.value = null
  rejectReason.value = ''
}

async function reopen(c) {
  if (busy.value.has(c.programmeId)) return
  busy.value.add(c.programmeId)
  try {
    await apiClient.post(`/v1/school/programmes/${c.programmeId}/reopen`)
    await load()
  } catch (e) { loadError.value = e.response?.data?.error ?? e.message ?? 'Reopen failed' }
  finally { busy.value.delete(c.programmeId) }
}

async function toggleDisable(c) {
  if (busy.value.has(c.programmeId)) return
  busy.value.add(c.programmeId)
  try {
    await apiClient.patch(`/v1/school/programmes/${c.programmeId}/admin-disable`, { disabled: !c.isDisabledByAdmin })
    await load()
  } catch (e) { loadError.value = e.response?.data?.error ?? e.message ?? 'Disable toggle failed' }
  finally { busy.value.delete(c.programmeId) }
}

watch(() => props.partnerId, load)
onMounted(load)
</script>

<style scoped>
.cp-tab { padding: .25rem 0; }
.err-banner { background: #fde7e7; color: #8a1515; padding: .55rem .8rem; border-radius: 6px; font-size: .85rem; margin-bottom: .75rem; }
.filter-bar-mini { display: flex; align-items: center; gap: .5rem; margin-bottom: .6rem; }
.filter-label { font-size: .82rem; color: #5f6e85; }
.inp-status { padding: .3rem .5rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .82rem; }
.empty-state-card { padding: 1rem; background: #f6f9fd; color: #5f6e85; border-radius: 8px; text-align: center; }
.clone-row { display: flex; justify-content: space-between; align-items: center; padding: .55rem .8rem; background: #f6f9fd; border-radius: 6px; margin-bottom: .4rem; gap: .5rem; flex-wrap: wrap; }
.clone-main { display: flex; align-items: center; gap: .5rem; }
.mono-code { font-family: monospace; font-size: .72rem; color: #6b7888; background: #fff; border: 1px solid #e1e6ed; padding: 1px 5px; border-radius: 4px; }
.status-pill { font-size: .7rem; padding: 2px 8px; border-radius: 10px; text-transform: uppercase; }
.s-draft    { background: #ecf0f6; color: #5f6e85; }
.s-pending  { background: #fff1cc; color: #8a6b16; }
.s-approved { background: #d7f0df; color: #1c7a4a; }
.s-rejected { background: #fde7e5; color: #a8241e; }
.s-active   { background: #e0f1ff; color: #1058a4; }
.s-enrolled { background: #fff2cc; color: #7a5c00; }
.s-disabled { background: #ecd4d2; color: #8a1515; }
.clone-actions { display: flex; align-items: center; gap: .4rem; }
.btn-sm { padding: .3rem .6rem; font-size: .78rem; border: 1px solid #cfd7e3; background: #fff; border-radius: 5px; cursor: pointer; }
.btn-ok { border-color: #1c7a4a; color: #1c7a4a; }
.btn-warn { border-color: #b63329; color: #b63329; }
.inp-add { padding: .35rem .6rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .82rem; min-width: 200px; }
.reject-reason { font-size: .78rem; color: #a8241e; }

.clone-card { background: #fff; border: 1px solid #e0e6ee; border-radius: 6px; margin-bottom: .5rem; overflow: hidden; }
.clickable { cursor: pointer; user-select: none; flex: 1 1 auto; }
.caret { font-size: .8rem; color: #6b7888; margin-right: .15rem; min-width: .8rem; }
.muted { font-size: .78rem; color: #5f6e85; }

.clone-details { padding: .65rem .85rem .85rem; border-top: 1px solid #e8edf3; background: #fbfcfe; }
.detail-row { display: flex; gap: .65rem; padding: .25rem 0; font-size: .82rem; }
.detail-label { color: #5f6e85; min-width: 9rem; font-weight: 600; }
.detail-value { color: #1a2d4f; flex: 1; }
.section-label { margin-top: .65rem; padding: .25rem 0; font-size: .72rem; color: #6b7888; font-weight: 700; text-transform: uppercase; letter-spacing: .04em; }
.empty-note { font-size: .8rem; color: #8a93a4; padding: .25rem 0 .5rem; font-style: italic; }

.spec-card { border: 1px solid #e8edf3; border-radius: 5px; margin-bottom: .35rem; }
.spec-row { display: flex; align-items: center; gap: .5rem; padding: .45rem .65rem; cursor: pointer; user-select: none; background: #f7fafd; }
.pill { font-size: .68rem; padding: 1px 7px; border-radius: 10px; background: #ecf0f6; color: #5f6e85; }
.pill-student { background: #eef3fb; color: #1a4d8c; }
.pill-auto { background: #eaf6ec; color: #226c3b; }
.subj-table { padding: .35rem .9rem .65rem; }
.subj-header, .subj-row { display: flex; align-items: center; padding: .25rem 0; font-size: .82rem; }
.subj-header { color: #6b7888; font-weight: 700; text-transform: uppercase; font-size: .68rem; letter-spacing: .04em; border-bottom: 1px solid #e8edf3; }
.col-code { width: 110px; flex-shrink: 0; }
.col-name { flex: 1; }
.col-ects { width: 50px; text-align: right; flex-shrink: 0; }
</style>
