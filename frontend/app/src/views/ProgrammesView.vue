<template>
  <div class="page-wrapper">
    <nav class="navbar">
      <span class="brand-text">IBSS Admin Portal</span>
      <div class="nav-links">
        <RouterLink to="/admin"       class="nav-link">Dashboard</RouterLink>
        <RouterLink to="/programmes"  class="nav-link">Programmes</RouterLink>
      </div>
      <div class="nav-right">
        <button class="btn-logout" @click="logout">Log out</button>
      </div>
    </nav>

    <div class="tab-bar">
      <button :class="['tab-btn', { active: tab === 'core' }]"    @click="tab = 'core'">IBSS Core</button>
      <button :class="['tab-btn', { active: tab === 'partner' }]" @click="tab = 'partner'">Partner Programmes</button>
    </div>

    <!-- ══════ IBSS CORE TAB ══════ -->
    <div v-show="tab === 'core'" class="container">
      <div class="page-header">
        <div>
          <h1 class="page-title">IBSS Core Programmes</h1>
          <p class="page-sub">{{ corePrograms.length }} programmes — changes apply to all partners</p>
        </div>
        <button class="btn-primary" @click="newProg.show = !newProg.show">+ Add Programme</button>
      </div>

      <!-- Add programme form -->
      <div v-if="newProg.show" class="inline-card">
        <h3>New Programme</h3>
        <div class="row-2">
          <div class="field"><label>Name <span class="req">*</span></label>
            <input v-model="newProg.name" placeholder="e.g. Master of Marketing" /></div>
          <div class="field" style="max-width:120px"><label>Code <span class="req">*</span></label>
            <input v-model="newProg.code" placeholder="e.g. MM" /></div>
        </div>
        <div class="inline-actions">
          <button class="btn-ghost" @click="newProg.show = false">Cancel</button>
          <button class="btn-primary" @click="addProgramme">Add</button>
        </div>
      </div>

      <!-- Programme accordion -->
      <div v-for="prog in corePrograms" :key="prog.id" class="acc-card">
        <div class="acc-header" @click="toggleProg(prog.id)">
          <div class="acc-title">
            <span class="arrow">{{ xProg === prog.id ? '▾' : '▸' }}</span>
            <strong>{{ prog.name }}</strong>
            <span class="badge-code">{{ prog.code }}</span>
            <span class="badge-count">{{ prog.specializations.length }} specialization{{ prog.specializations.length !== 1 ? 's' : '' }}</span>
          </div>
          <button class="btn-del" @click.stop="removeProgramme(prog.id)">Delete</button>
        </div>

        <div v-if="xProg === prog.id" class="acc-body">
          <div v-for="maj in prog.specializations" :key="maj.id" class="maj-block">
            <div class="maj-header">
              <div class="maj-title" @click="toggleMaj(maj.id)">
                <span class="arrow">{{ xMaj === maj.id ? '▾' : '▸' }}</span>
                {{ maj.name }}
                <span class="badge-count">{{ maj.subjects.length }} subjects</span>
              </div>
              <button class="btn-del" @click="removeSpecialization(prog, maj.id)">Remove</button>
            </div>
            <div v-if="xMaj === maj.id" class="subj-block">
              <!-- Column headers -->
              <div class="subj-col-header">
                <span class="col-code">Module Code</span>
                <span class="col-name">Module Name</span>
                <span class="col-ects">ECTS</span>
                <span class="col-del"></span>
              </div>
              <div v-for="s in maj.subjects" :key="s.id" class="subj-row">
                <span class="scode">{{ s.code || '—' }}</span>
                <span class="sname">{{ s.name }}</span>
                <span class="scr">{{ s.ects }}</span>
                <button class="btn-x" @click="removeSubject(maj, s.id)">✕</button>
              </div>
              <div class="add-row">
                <input v-model="sf[maj.id+'_code']" class="inp-code" placeholder="Code" />
                <input v-model="sf[maj.id+'_n']" class="inp-wide" placeholder="Module name" />
                <input v-model.number="sf[maj.id+'_c']" class="inp-num" type="number" min="1" placeholder="15" />
                <button class="btn-add" @click="addSubject(maj, maj.id)">+ Add</button>
              </div>
            </div>
          </div>
          <div class="add-specialization-row">
            <input v-model="mf[prog.id]" class="inp-wide" placeholder="New specialization name…" />
            <button class="btn-primary-sm" @click="addSpecialization(prog)">+ Add Specialization</button>
          </div>
        </div>
      </div>
    </div>

    <!-- ══════ PARTNER PROGRAMMES TAB ══════ -->
    <div v-show="tab === 'partner'" class="container">
      <div class="page-header">
        <div>
          <h1 class="page-title">Partner Programmes</h1>
          <p class="page-sub">Approve or reject partner programme submissions. Core access is managed per partner.</p>
        </div>
      </div>

      <div v-for="partner in partnerRecords" :key="partner.id" class="acc-card">
        <div class="acc-header" @click="togglePartner(partner.id)">
          <div class="acc-title">
            <span class="arrow">{{ xPartner === partner.id ? '▾' : '▸' }}</span>
            <strong>{{ partner.name }}</strong>
            <span class="badge-count">{{ partner.coreAccess.length }} core access</span>
            <span v-if="partnerProgClones(partner).length" class="badge-clone">
              {{ partnerProgClones(partner).length }} programme{{ partnerProgClones(partner).length !== 1 ? 's' : '' }}
            </span>
            <span v-if="pendingCount(partner)" class="badge-pending">{{ pendingCount(partner) }} pending</span>
          </div>
          <RouterLink :to="`/partners/${partner.id}`" class="btn-link" @click.stop>Config →</RouterLink>
        </div>

        <div v-if="xPartner === partner.id" class="acc-body">

          <!-- Programme clones with approval UI -->
          <template v-if="partnerProgClones(partner).length">
            <div class="view-section-title">Partner Programmes</div>
            <div v-for="clone in partnerProgClones(partner)" :key="clone.id" class="approval-card">
              <div class="approval-header">
                <div class="approval-info">
                  <strong>{{ clone.name }}</strong>
                  <span class="badge-code">{{ clone.code }}</span>
                  <span class="badge-count">{{ clone.specializations?.length }} specialization{{ clone.specializations?.length !== 1 ? 's' : '' }}</span>
                  <span :class="statusBadgeClass(clone.status)">{{ statusLabel(clone.status) }}</span>
                </div>
                <div v-if="clone.status !== 'draft'" class="approval-actions">
                  <button v-if="clone.status !== 'approved'" class="btn-approve" @click="approveClone(partner, clone.id)">Approve</button>
                  <template v-if="rejectingId === clone.id">
                    <input v-model="rejectReason" class="inp-reject" placeholder="Rejection reason…" @keyup.enter="confirmReject(partner, clone.id)" />
                    <button class="btn-confirm-reject" @click="confirmReject(partner, clone.id)">Confirm Reject</button>
                    <button class="btn-cancel-reject" @click="rejectingId = null; rejectReason = ''">Cancel</button>
                  </template>
                  <button v-else-if="clone.status !== 'rejected'" class="btn-reject" @click="startReject(clone.id)">Reject</button>
                  <button v-else class="btn-reject" @click="startReject(clone.id)">Change Rejection</button>
                </div>
              </div>
              <div v-if="clone.status === 'rejected' && clone.rejectionReason" class="rejection-note">
                Rejection reason: {{ clone.rejectionReason }}
              </div>
              <div class="view-maj-list" style="margin-top:0.4rem">
                <div v-for="m in clone.specializations" :key="m.id" class="view-maj-row">
                  {{ m.name }} <span class="badge-count">{{ m.subjects.length }} subjects</span>
                </div>
              </div>
            </div>
          </template>
          <p v-else class="empty-note">No partner programmes submitted yet.</p>

          <!-- Core Access (editable toggles) -->
          <div class="view-section-title" style="margin-top:1.25rem">Core Access</div>
          <div v-for="prog in corePrograms" :key="prog.id" class="view-prog-block">
            <div class="view-prog-name">
              <span class="badge-code">{{ prog.code }}</span> {{ prog.name }}
              <span class="badge-count">{{ enabledSpecializationCount(partner, prog) }}/{{ prog.specializations.length }} on</span>
            </div>
            <div class="view-maj-list">
              <div v-for="maj in prog.specializations" :key="maj.id" class="view-maj-row">
                <label class="toggle-wrap">
                  <input type="checkbox"
                    :checked="hasAccess(partner, prog.id, maj.id)"
                    @change="toggleAccess(partner, prog.id, maj.id)" />
                  <span>{{ maj.name }}</span>
                </label>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive } from 'vue'
import { useRouter, RouterLink } from 'vue-router'
import { auth } from '../store/auth.js'
import {
  corePrograms, partnerRecords, uid,
  approveProgramFn, rejectProgramFn,
} from '../mock/programmes.js'

const router = useRouter()
const tab = ref('core')

// ── Core tab accordion ────────────────────────────────────────────────────────
const xProg = ref(null)
const xMaj  = ref(null)
const mf    = reactive({})   // progId → new specialization name
const sf    = reactive({})   // majId_n / majId_c

function toggleProg(id) { xProg.value = xProg.value === id ? null : id; xMaj.value = null }
function toggleMaj(id)  { xMaj.value  = xMaj.value  === id ? null : id }

const newProg = reactive({ show: false, name: '', code: '' })

function addProgramme() {
  if (!newProg.name.trim() || !newProg.code.trim()) return
  corePrograms.push({ id: uid(), name: newProg.name.trim(), code: newProg.code.trim().toUpperCase(), specializations: [] })
  newProg.name = ''; newProg.code = ''; newProg.show = false
}
function removeProgramme(id) {
  const i = corePrograms.findIndex(p => p.id === id)
  if (i >= 0) corePrograms.splice(i, 1)
  if (xProg.value === id) xProg.value = null
}
function addSpecialization(prog) {
  const name = (mf[prog.id] ?? '').trim()
  if (!name) return
  prog.specializations.push({ id: uid(), name, subjects: [] })
  mf[prog.id] = ''
}
function removeSpecialization(prog, majId) {
  const i = prog.specializations.findIndex(m => m.id === majId)
  if (i >= 0) prog.specializations.splice(i, 1)
}
function addSubject(maj, majId) {
  const name = (sf[majId + '_n'] ?? '').trim()
  const code = (sf[majId + '_code'] ?? '').trim()
  const ects = Number(sf[majId + '_c']) || 15
  if (!name) return
  maj.subjects.push({ id: uid(), code, name, ects })
  sf[majId + '_code'] = ''; sf[majId + '_n'] = ''; sf[majId + '_c'] = 15
}
function removeSubject(maj, id) {
  const i = maj.subjects.findIndex(s => s.id === id)
  if (i >= 0) maj.subjects.splice(i, 1)
}

// ── Partner tab helpers ───────────────────────────────────────────────────────
const xPartner   = ref(null)
const rejectingId = ref(null)
const rejectReason = ref('')

function togglePartner(id) { xPartner.value = xPartner.value === id ? null : id }

function hasAccess(partner, progId, majId) {
  return partner.coreAccess.includes(`${progId}__${majId}`)
}

function toggleAccess(partner, progId, majId) {
  const key = `${progId}__${majId}`
  const idx = partner.coreAccess.indexOf(key)
  if (idx >= 0) partner.coreAccess.splice(idx, 1)
  else partner.coreAccess.push(key)
}

function enabledSpecializationCount(partner, prog) {
  return prog.specializations.filter(m => hasAccess(partner, prog.id, m.id)).length
}

function partnerProgClones(partner) {
  return partner.clones.filter(c => c.type === 'programme')
}

function pendingCount(partner) {
  return partnerProgClones(partner).filter(c => c.status === 'pending').length
}

function statusLabel(status) {
  return { draft: 'Draft', pending: 'Pending Approval', approved: 'Approved', rejected: 'Rejected' }[status] ?? status
}

function statusBadgeClass(status) {
  return { draft: 'badge-status-draft', pending: 'badge-status-pending', approved: 'badge-status-approved', rejected: 'badge-status-rejected' }[status] ?? 'badge-count'
}

function approveClone(partner, cloneId) {
  approveProgramFn(partner, cloneId)
}

function startReject(cloneId) {
  rejectingId.value = cloneId
  rejectReason.value = ''
}

function confirmReject(partner, cloneId) {
  if (!rejectReason.value.trim()) return
  rejectProgramFn(partner, cloneId, rejectReason.value.trim())
  rejectingId.value = null
  rejectReason.value = ''
}

function logout() { auth.logout(); router.push('/login') }
</script>

<style scoped>
.page-wrapper { min-height: 100vh; background: #f2f5f9; }

.navbar { background: #003366; color: #fff; display: flex; align-items: center; gap: 1rem; padding: 0.85rem 2rem; }
.brand-text { font-size: 1.05rem; font-weight: 700; white-space: nowrap; }
.nav-links { display: flex; gap: 0.25rem; flex: 1; padding: 0 1rem; }
.nav-link { color: rgba(255,255,255,0.75); text-decoration: none; padding: 0.35rem 0.9rem; border-radius: 5px; font-size: 0.88rem; transition: background 0.15s; }
.nav-link:hover, .nav-link.router-link-active { background: rgba(255,255,255,0.15); color: #fff; }
.nav-right { display: flex; align-items: center; gap: 1rem; }
.btn-logout { background: transparent; border: 1.5px solid rgba(255,255,255,0.55); color: #fff; padding: 0.3rem 0.85rem; border-radius: 5px; cursor: pointer; font-size: 0.82rem; }
.btn-logout:hover { background: rgba(255,255,255,0.13); }

.tab-bar { background: #fff; border-bottom: 2px solid #e8edf4; display: flex; padding: 0 2rem; }
.tab-btn { background: none; border: none; padding: 0.85rem 1.25rem; font-size: 0.9rem; font-weight: 600; color: #888; cursor: pointer; border-bottom: 3px solid transparent; margin-bottom: -2px; transition: color 0.15s, border-color 0.15s; }
.tab-btn.active { color: #003366; border-bottom-color: #003366; }

.container { max-width: 900px; margin: 2rem auto; padding: 0 1.5rem; }
.page-header { display: flex; align-items: flex-start; justify-content: space-between; margin-bottom: 1.25rem; }
.page-title { font-size: 1.5rem; font-weight: 700; color: #003366; }
.page-sub { font-size: 0.82rem; color: #888; margin-top: 0.2rem; }

.btn-primary { background: #003366; color: #fff; border: none; border-radius: 7px; padding: 0.6rem 1.25rem; font-size: 0.9rem; font-weight: 600; cursor: pointer; }
.btn-primary:hover { background: #0055a5; }
.btn-primary-sm { background: #003366; color: #fff; border: none; border-radius: 5px; padding: 0.38rem 0.85rem; font-size: 0.82rem; cursor: pointer; white-space: nowrap; }
.btn-primary-sm:hover { background: #0055a5; }
.btn-ghost { background: none; border: 1.5px solid #ccc; border-radius: 7px; padding: 0.5rem 0.9rem; font-size: 0.82rem; color: #666; cursor: pointer; }
.btn-del { background: #fdecea; color: #c0392b; border: 1px solid #f5c0bb; border-radius: 5px; padding: 0.25rem 0.65rem; font-size: 0.78rem; cursor: pointer; }
.btn-del:hover { background: #f9d4d0; }
.btn-add { background: #003366; color: #fff; border: none; border-radius: 5px; padding: 0.35rem 0.75rem; font-size: 0.8rem; cursor: pointer; white-space: nowrap; }
.btn-add:hover { background: #0055a5; }
.btn-x { background: none; border: none; color: #bbb; cursor: pointer; font-size: 0.82rem; padding: 0 3px; }
.btn-x:hover { color: #c0392b; }
.btn-link { color: #0055a5; text-decoration: none; font-size: 0.82rem; font-weight: 600; padding: 0.25rem 0.6rem; border-radius: 5px; transition: background 0.15s; white-space: nowrap; }
.btn-link:hover { background: #e8f0f8; }

.inline-card { background: #fff; border: 1.5px solid #dde6f0; border-radius: 9px; padding: 1.2rem 1.4rem; margin-bottom: 1.2rem; }
.inline-card h3 { font-size: 0.95rem; font-weight: 700; color: #003366; margin: 0 0 0.85rem; }
.inline-actions { display: flex; gap: 0.6rem; justify-content: flex-end; margin-top: 0.85rem; }
.row-2 { display: flex; gap: 0.75rem; flex-wrap: wrap; }
.field { display: flex; flex-direction: column; gap: 0.28rem; flex: 1 1 180px; }
.field label { font-size: 0.8rem; font-weight: 600; color: #444; }
.req { color: #c0392b; }
.field input { padding: 0.55rem 0.75rem; border: 1.5px solid #ccc; border-radius: 6px; font-size: 0.9rem; font-family: inherit; outline: none; }
.field input:focus { border-color: #0055a5; }

/* Accordion */
.acc-card { background: #fff; border-radius: 10px; box-shadow: 0 2px 8px rgba(0,0,0,0.06); margin-bottom: 0.85rem; overflow: hidden; }
.acc-header { display: flex; align-items: center; justify-content: space-between; padding: 0.9rem 1.25rem; cursor: pointer; user-select: none; }
.acc-header:hover { background: #f7f9fb; }
.acc-title { display: flex; align-items: center; gap: 0.55rem; font-size: 0.93rem; flex: 1; }
.arrow { color: #888; font-size: 0.82rem; width: 12px; }
.acc-body { padding: 0.75rem 1.25rem 1rem; border-top: 1px solid #f0f3f7; }

.badge-code  { background: #e8f0f8; color: #003366; border-radius: 4px; padding: 1px 7px; font-size: 0.73rem; font-weight: 700; }
.badge-count { background: #f0f3f7; color: #777; border-radius: 4px; padding: 1px 7px; font-size: 0.73rem; }
.badge-clone { background: #e0f5f0; color: #0d6b55; border-radius: 4px; padding: 1px 7px; font-size: 0.73rem; font-weight: 600; }
.badge-type  { background: #f0e8ff; color: #5b21b6; border-radius: 4px; padding: 1px 7px; font-size: 0.73rem; font-weight: 600; text-transform: capitalize; }

.maj-block { border: 1px solid #e8edf4; border-radius: 7px; margin-bottom: 0.55rem; overflow: hidden; }
.maj-header { display: flex; align-items: center; justify-content: space-between; padding: 0.55rem 0.9rem; background: #fafbfc; }
.maj-title { display: flex; align-items: center; gap: 0.45rem; font-size: 0.87rem; font-weight: 600; cursor: pointer; flex: 1; }

.subj-block { padding: 0.5rem 0.9rem 0.75rem; }

/* 3-column header */
.subj-col-header { display: flex; align-items: center; gap: 0.55rem; padding-bottom: 0.3rem; border-bottom: 1px solid #e8edf4; margin-bottom: 0.3rem; }
.subj-col-header span { font-size: 0.69rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.05em; color: #aaa; }
.col-code { width: 88px; flex-shrink: 0; }
.col-name { flex: 1; }
.col-ects { width: 46px; flex-shrink: 0; text-align: center; }
.col-del  { width: 18px; flex-shrink: 0; }

.subj-row { display: flex; align-items: center; gap: 0.55rem; padding: 0.28rem 0; border-bottom: 1px solid #f5f6f8; font-size: 0.84rem; }
.subj-row:last-of-type { border-bottom: none; }
.scode { width: 88px; flex-shrink: 0; font-family: ui-monospace, monospace; font-size: 0.78rem; color: #003366; font-weight: 600; }
.sname { flex: 1; }
.scr   { width: 46px; flex-shrink: 0; text-align: center; color: #666; font-size: 0.82rem; font-weight: 600; }

.add-row { display: flex; gap: 0.45rem; align-items: center; margin-top: 0.55rem; }
.inp-code { width: 88px; flex-shrink: 0; padding: 0.36rem 0.5rem; border: 1.5px solid #ccc; border-radius: 5px; font-size: 0.82rem; outline: none; font-family: ui-monospace, monospace; }
.inp-code:focus { border-color: #003366; }
.inp-wide { flex: 1; padding: 0.36rem 0.6rem; border: 1.5px solid #ccc; border-radius: 5px; font-size: 0.84rem; outline: none; }
.inp-wide:focus { border-color: #003366; }
.inp-num { width: 58px; padding: 0.36rem 0.45rem; border: 1.5px solid #ccc; border-radius: 5px; font-size: 0.84rem; text-align: center; outline: none; }
.inp-num:focus { border-color: #003366; }

.add-specialization-row { display: flex; gap: 0.55rem; align-items: center; margin-top: 0.7rem; padding-top: 0.7rem; border-top: 1px dashed #dde6f0; }

/* Partner programmes view */
.view-section-title { font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.06em; color: #888; font-weight: 700; margin-bottom: 0.6rem; margin-top: 0.2rem; }
.view-prog-block { margin-bottom: 0.75rem; }
.view-prog-name { font-size: 0.87rem; font-weight: 600; display: flex; align-items: center; gap: 0.5rem; margin-bottom: 0.3rem; }
.view-maj-list { padding-left: 1.2rem; }
.view-maj-row { font-size: 0.84rem; color: #444; padding: 0.18rem 0; display: flex; align-items: center; gap: 0.5rem; }
.dot-on { color: #27ae60; }
.dot-off { color: #ccc; }
.toggle-wrap { display: flex; align-items: center; gap: 0.45rem; cursor: pointer; font-size: 0.84rem; color: #333; }
.toggle-wrap input[type=checkbox] { width: 14px; height: 14px; cursor: pointer; accent-color: #003366; }
.view-clone-block { border: 1px solid #e8edf4; border-radius: 7px; padding: 0.65rem 0.9rem; margin-bottom: 0.5rem; }
.view-clone-header { display: flex; align-items: center; gap: 0.5rem; margin-bottom: 0.4rem; font-size: 0.87rem; }
.view-subj-list { display: flex; flex-wrap: wrap; gap: 0.4rem; margin-top: 0.35rem; }
.subj-pill { background: #f0f3f7; border-radius: 20px; padding: 2px 10px; font-size: 0.78rem; color: #555; }
.empty-note { font-size: 0.83rem; color: #bbb; font-style: italic; padding: 0.5rem 0; }

/* Approval UI */
.approval-card { border: 1px solid #e8edf4; border-radius: 8px; padding: 0.75rem 1rem; margin-bottom: 0.65rem; background: #fafbfc; }
.approval-header { display: flex; align-items: center; justify-content: space-between; flex-wrap: wrap; gap: 0.5rem; }
.approval-info { display: flex; align-items: center; gap: 0.5rem; flex-wrap: wrap; font-size: 0.88rem; }
.approval-actions { display: flex; align-items: center; gap: 0.45rem; flex-wrap: wrap; }
.rejection-note { font-size: 0.8rem; color: #c0392b; background: #fdecea; border-radius: 5px; padding: 0.3rem 0.7rem; margin-top: 0.45rem; }

.btn-approve { background: #d1fae5; color: #065f46; border: 1px solid #6ee7b7; border-radius: 5px; padding: 0.28rem 0.75rem; font-size: 0.8rem; font-weight: 600; cursor: pointer; }
.btn-approve:hover { background: #a7f3d0; }
.btn-reject  { background: #fdecea; color: #c0392b; border: 1px solid #f5c0bb; border-radius: 5px; padding: 0.28rem 0.75rem; font-size: 0.8rem; font-weight: 600; cursor: pointer; }
.btn-reject:hover { background: #f9d4d0; }
.btn-confirm-reject { background: #c0392b; color: #fff; border: none; border-radius: 5px; padding: 0.28rem 0.75rem; font-size: 0.8rem; cursor: pointer; }
.btn-cancel-reject  { background: none; border: 1px solid #ccc; color: #666; border-radius: 5px; padding: 0.28rem 0.6rem; font-size: 0.8rem; cursor: pointer; }
.inp-reject { padding: 0.28rem 0.6rem; border: 1.5px solid #e0a0a0; border-radius: 5px; font-size: 0.82rem; outline: none; width: 200px; }
.inp-reject:focus { border-color: #c0392b; }

.badge-pending  { background: #fff3cd; color: #856404; border-radius: 4px; padding: 1px 7px; font-size: 0.73rem; font-weight: 700; }
.badge-status-draft    { background: #f0f3f7; color: #666; border-radius: 4px; padding: 1px 7px; font-size: 0.73rem; font-weight: 600; }
.badge-status-pending  { background: #fff3cd; color: #856404; border-radius: 4px; padding: 1px 7px; font-size: 0.73rem; font-weight: 700; }
.badge-status-approved { background: #d1fae5; color: #065f46; border-radius: 4px; padding: 1px 7px; font-size: 0.73rem; font-weight: 700; }
.badge-status-rejected { background: #fdecea; color: #c0392b; border-radius: 4px; padding: 1px 7px; font-size: 0.73rem; font-weight: 700; }
</style>
