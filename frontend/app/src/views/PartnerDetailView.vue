<template>
  <div class="page-wrapper">
    <nav class="navbar">
      <span class="brand-text">IBSS Admin Portal</span>
      <div class="nav-links">
        <RouterLink to="/admin"      class="nav-link">Dashboard</RouterLink>
        <RouterLink to="/programmes" class="nav-link">Programmes</RouterLink>
      </div>
      <div class="nav-right">
        <button class="btn-logout" @click="logout">Log out</button>
      </div>
    </nav>

    <!-- Not found -->
    <div v-if="!partner" class="container">
      <p class="not-found">Partner not found. <RouterLink to="/admin">← Back</RouterLink></p>
    </div>

    <template v-else>
      <div class="breadcrumb">
        <RouterLink to="/admin" class="bc-link">Partners</RouterLink>
        <span class="bc-sep">/</span>
        <span class="bc-current">{{ partner.name }}</span>
      </div>

      <div class="tab-bar">
        <button :class="['tab-btn', { active: tab === 'info' }]"       @click="tab = 'info'">Info</button>
        <button :class="['tab-btn', { active: tab === 'programmes' }]" @click="tab = 'programmes'">Partner Programmes</button>
      </div>

      <!-- ═══ INFO TAB ═══ -->
      <div v-show="tab === 'info'" class="container">
        <div class="page-header">
          <h1 class="page-title">{{ partner.name }}</h1>
        </div>
        <div class="info-card">
          <div class="info-row"><span class="info-label">Partner Name</span><span>{{ partner.name }}</span></div>
          <div class="info-row"><span class="info-label">Username</span><span class="mono">{{ partner.username }}</span></div>
          <div class="info-row"><span class="info-label">Role</span><span>Partner</span></div>
          <div class="info-row"><span class="info-label">Core Majors Enabled</span><span>{{ partner.coreAccess.length }}</span></div>
          <div class="info-row"><span class="info-label">Clones / Custom</span><span>{{ partner.clones.length }}</span></div>
        </div>
        <div class="info-card" style="margin-top:1rem">
          <h3 class="card-subtitle">Change Password</h3>
          <div class="row-2">
            <div class="field"><label>New Password</label><input v-model="newPw" type="password" placeholder="Enter new password" /></div>
          </div>
          <div class="inline-actions">
            <button class="btn-primary" @click="changePw" :disabled="!newPw.trim()">Update Password</button>
          </div>
          <p v-if="pwMsg" class="success-inline">{{ pwMsg }}</p>
        </div>
      </div>

      <!-- ═══ PROGRAMMES TAB ═══ -->
      <div v-show="tab === 'programmes'" class="container wide">
        <div class="page-header">
          <div>
            <h1 class="page-title">Programme Access</h1>
            <p class="page-sub">Toggle which IBSS core majors this partner can access. Partners manage their own programme clones from their portal.</p>
          </div>
        </div>

        <h2 class="section-heading">IBSS Core Access</h2>

        <div v-for="prog in corePrograms" :key="prog.id" class="acc-card">
          <div class="acc-header" @click="toggleProg(prog.id)">
            <div class="acc-title">
              <span class="arrow">{{ xProg === prog.id ? '▾' : '▸' }}</span>
              <strong>{{ prog.name }}</strong>
              <span class="badge-code">{{ prog.code }}</span>
              <span class="badge-count">{{ enabledMajorCount(prog) }}/{{ prog.majors.length }} majors on</span>
            </div>
          </div>

          <div v-if="xProg === prog.id" class="acc-body">
            <!-- Core majors — toggle access only -->
            <div v-for="maj in prog.majors" :key="maj.id" class="maj-row-prog">
              <label class="toggle-wrap">
                <input type="checkbox"
                  :checked="hasAccess(prog.id, maj.id)"
                  @change="toggleAccess(prog.id, maj.id)" />
                <span class="toggle-name">{{ maj.name }}</span>
                <span class="core-tag">core</span>
              </label>
            </div>
          </div>
        </div>

        <!-- Partner's own programme clones (read-only) -->
        <h2 class="section-heading" style="margin-top:1.5rem">Partner Programmes</h2>
        <template v-if="partnerProgClones.length">
          <div v-for="clone in partnerProgClones" :key="clone.id" class="acc-card">
            <div class="acc-header" style="cursor:default">
              <div class="acc-title">
                <strong>{{ clone.name }}</strong>
                <span class="badge-code">{{ clone.code }}</span>
                <span class="badge-count">{{ clone.majors?.length }} major{{ clone.majors?.length !== 1 ? 's' : '' }}</span>
                <span :class="statusBadgeClass(clone.status)">{{ statusLabel(clone.status) }}</span>
              </div>
            </div>
          </div>
        </template>
        <p v-else class="empty-note" style="padding:0.5rem 0">No partner programmes yet — partner creates them from their portal.</p>
      </div>
    </template>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRoute, useRouter, RouterLink } from 'vue-router'
import { auth } from '../store/auth.js'
import { corePrograms, partnerRecords } from '../mock/programmes.js'

const route  = useRoute()
const router = useRouter()

const partner = computed(() => partnerRecords.find(p => p.id === route.params.id) ?? null)

const tab = ref('info')

// ── Info tab ──────────────────────────────────────────────────────────────────
const newPw  = ref('')
const pwMsg  = ref('')
function changePw() {
  if (!partner.value || !newPw.value.trim()) return
  partner.value.password = newPw.value.trim()
  newPw.value = ''
  pwMsg.value = 'Password updated.'
  setTimeout(() => pwMsg.value = '', 2000)
}

// ── Programmes tab ────────────────────────────────────────────────────────────
const xProg = ref(null)
function toggleProg(id) { xProg.value = xProg.value === id ? null : id }

// ── Access helpers ────────────────────────────────────────────────────────────
function hasAccess(progId, majId) {
  return partner.value?.coreAccess.includes(`${progId}__${majId}`) ?? false
}
function toggleAccess(progId, majId) {
  const p = partner.value; if (!p) return
  const key = `${progId}__${majId}`
  const idx = p.coreAccess.indexOf(key)
  if (idx >= 0) p.coreAccess.splice(idx, 1)
  else p.coreAccess.push(key)
}
function enabledMajorCount(prog) {
  return prog.majors.filter(m => hasAccess(prog.id, m.id)).length
}

// ── Partner programme clones (read-only view) ─────────────────────────────────
const partnerProgClones = computed(() =>
  partner.value?.clones.filter(c => c.type === 'programme') ?? []
)

function statusLabel(status) {
  return { draft: 'Draft', pending: 'Pending Approval', approved: 'Approved', rejected: 'Rejected' }[status] ?? status
}
function statusBadgeClass(status) {
  return { draft: 'badge-status-draft', pending: 'badge-status-pending', approved: 'badge-status-approved', rejected: 'badge-status-rejected' }[status] ?? 'badge-count'
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
.nav-right { display: flex; align-items: center; }
.btn-logout { background: transparent; border: 1.5px solid rgba(255,255,255,0.55); color: #fff; padding: 0.3rem 0.85rem; border-radius: 5px; cursor: pointer; font-size: 0.82rem; }
.btn-logout:hover { background: rgba(255,255,255,0.13); }

.breadcrumb { padding: 0.75rem 2rem; font-size: 0.84rem; color: #888; display: flex; align-items: center; gap: 0.4rem; background: #fff; border-bottom: 1px solid #e8edf4; }
.bc-link { color: #0055a5; text-decoration: none; }
.bc-link:hover { text-decoration: underline; }
.bc-sep { color: #ccc; }
.bc-current { color: #333; font-weight: 600; }

.tab-bar { background: #fff; border-bottom: 2px solid #e8edf4; display: flex; padding: 0 2rem; }
.tab-btn { background: none; border: none; padding: 0.85rem 1.25rem; font-size: 0.9rem; font-weight: 600; color: #888; cursor: pointer; border-bottom: 3px solid transparent; margin-bottom: -2px; transition: color 0.15s, border-color 0.15s; }
.tab-btn.active { color: #003366; border-bottom-color: #003366; }

.container { max-width: 860px; margin: 2rem auto; padding: 0 1.5rem; }
.container.wide { max-width: 960px; }
.page-header { display: flex; align-items: flex-start; justify-content: space-between; margin-bottom: 1.25rem; }
.page-title { font-size: 1.4rem; font-weight: 700; color: #003366; }
.page-sub { font-size: 0.82rem; color: #888; margin-top: 0.2rem; }
.not-found { color: #888; font-style: italic; padding: 2rem 0; }

.section-heading { font-size: 1rem; font-weight: 700; color: #555; margin: 0 0 0.85rem; }

/* Info tab */
.info-card { background: #fff; border-radius: 10px; box-shadow: 0 2px 8px rgba(0,0,0,0.06); padding: 1.4rem 1.5rem; }
.info-row { display: flex; gap: 1rem; padding: 0.5rem 0; border-bottom: 1px solid #f0f3f7; font-size: 0.88rem; }
.info-row:last-of-type { border-bottom: none; }
.info-label { font-weight: 600; color: #888; width: 180px; flex-shrink: 0; font-size: 0.8rem; text-transform: uppercase; letter-spacing: 0.04em; padding-top: 1px; }
.mono { font-family: ui-monospace, monospace; font-size: 0.84rem; }
.card-subtitle { font-size: 0.95rem; font-weight: 700; color: #003366; margin: 0 0 0.85rem; }
.inline-actions { display: flex; gap: 0.6rem; justify-content: flex-end; margin-top: 0.85rem; }
.success-inline { color: #1e8449; font-size: 0.84rem; margin: 0.5rem 0 0; text-align: right; }
.row-2 { display: flex; gap: 0.75rem; }
.field { display: flex; flex-direction: column; gap: 0.28rem; flex: 1 1 200px; }
.field label { font-size: 0.8rem; font-weight: 600; color: #444; }
.field input { padding: 0.55rem 0.75rem; border: 1.5px solid #ccc; border-radius: 6px; font-size: 0.9rem; font-family: inherit; outline: none; }
.field input:focus { border-color: #0055a5; }

/* Accordion */
.acc-card { background: #fff; border-radius: 10px; box-shadow: 0 2px 8px rgba(0,0,0,0.06); margin-bottom: 0.75rem; overflow: hidden; }
.acc-header { display: flex; align-items: center; justify-content: space-between; padding: 0.85rem 1.2rem; cursor: pointer; user-select: none; gap: 0.75rem; }
.acc-header:hover { background: #f7f9fb; }
.acc-title { display: flex; align-items: center; gap: 0.5rem; font-size: 0.9rem; flex: 1; flex-wrap: wrap; }
.arrow { color: #888; font-size: 0.82rem; width: 12px; }
.acc-body { padding: 0.5rem 1.2rem 1rem; border-top: 1px solid #f0f3f7; }

/* Badges */
.badge-code   { background: #e8f0f8; color: #003366; border-radius: 4px; padding: 1px 7px; font-size: 0.72rem; font-weight: 700; }
.badge-count  { background: #f0f3f7; color: #777; border-radius: 4px; padding: 1px 7px; font-size: 0.72rem; }
.badge-cloned { background: #e0f5f0; color: #0d6b55; border-radius: 4px; padding: 1px 7px; font-size: 0.72rem; font-weight: 600; }

/* Core major row */
.core-maj-section { margin-bottom: 0.2rem; }
.maj-row-prog { display: flex; align-items: center; justify-content: space-between; padding: 0.52rem 0.5rem; border-bottom: 1px solid #f5f6f8; }
.toggle-wrap { display: flex; align-items: center; gap: 0.5rem; cursor: pointer; font-size: 0.88rem; }
.toggle-wrap input[type=checkbox] { width: 15px; height: 15px; cursor: pointer; }
.toggle-name { color: #333; font-weight: 500; }
.core-tag { background: #f0f3f7; color: #999; border-radius: 3px; padding: 0px 5px; font-size: 0.67rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.03em; }

/* Clone rows (indented under their parent major) */
.clone-indent { margin-left: 1.8rem; margin-bottom: 0.15rem; }
.clone-row-header { display: flex; align-items: center; gap: 0.45rem; padding: 0.42rem 0.6rem; border-radius: 6px; cursor: pointer; user-select: none; background: #fafbfc; border: 1px solid #e8edf4; margin-top: 0.25rem; }
.clone-row-header:hover { background: #f0f5fa; }
.arrow-sm { color: #aaa; font-size: 0.72rem; width: 10px; flex-shrink: 0; }
.clone-row-name { font-size: 0.86rem; color: #333; flex: 1; font-weight: 500; }
.clone-row-body { padding: 0.55rem 0.75rem 0.65rem; background: #f7f9fc; border: 1px solid #e8edf4; border-top: none; border-radius: 0 0 6px 6px; }

.badge-custom-tag { background: #fff0e0; color: #b45309; border-radius: 3px; padding: 1px 6px; font-size: 0.67rem; font-weight: 700; text-transform: uppercase; flex-shrink: 0; }
.rename-input { flex: 1; padding: 0.18rem 0.45rem; border: 1.5px solid #0055a5; border-radius: 4px; font-size: 0.86rem; font-family: inherit; outline: none; min-width: 0; background: #fff; }
.btn-icon { background: none; border: none; color: #bbb; cursor: pointer; font-size: 0.9rem; padding: 0 2px; flex-shrink: 0; line-height: 1; }
.btn-icon:hover { color: #003366; }

/* Custom major divider + rows */
.custom-maj-divider { font-size: 0.72rem; text-transform: uppercase; letter-spacing: 0.06em; color: #aaa; font-weight: 700; margin: 1rem 0 0.3rem 0.3rem; }
.custom-maj-row-wrap { margin-bottom: 0.25rem; }
.empty-note { font-size: 0.82rem; color: #bbb; font-style: italic; margin: 0.2rem 0 0.4rem; }

/* Add custom major row */
.add-custom-row { display: flex; gap: 0.5rem; align-items: center; margin-top: 0.85rem; padding-top: 0.75rem; border-top: 1px dashed #dde6f0; }

/* Subject editor */
.subj-row { display: flex; align-items: center; gap: 0.7rem; padding: 0.26rem 0; border-bottom: 1px solid #edf0f4; font-size: 0.83rem; }
.subj-row:last-of-type { border-bottom: none; }
.sname { flex: 1; }
.scr { color: #999; font-size: 0.77rem; }
.add-row { display: flex; gap: 0.42rem; align-items: center; margin-top: 0.5rem; }

/* Inputs */
.inp-wide { flex: 1; padding: 0.34rem 0.6rem; border: 1.5px solid #ccc; border-radius: 5px; font-size: 0.84rem; outline: none; font-family: inherit; }
.inp-wide:focus { border-color: #003366; }
.inp-num { width: 58px; padding: 0.34rem 0.45rem; border: 1.5px solid #ccc; border-radius: 5px; font-size: 0.84rem; text-align: center; outline: none; }
.inp-num:focus { border-color: #003366; }

/* Buttons */
.btn-primary { background: #003366; color: #fff; border: none; border-radius: 7px; padding: 0.55rem 1.2rem; font-size: 0.88rem; font-weight: 600; cursor: pointer; }
.btn-primary:hover:not(:disabled) { background: #0055a5; }
.btn-primary:disabled { opacity: 0.5; cursor: default; }
.btn-primary-sm { background: #003366; color: #fff; border: none; border-radius: 5px; padding: 0.36rem 0.85rem; font-size: 0.82rem; cursor: pointer; white-space: nowrap; }
.btn-primary-sm:hover { background: #0055a5; }
.btn-sm { background: #e8f0f8; color: #003366; border: 1px solid #c5d8f0; border-radius: 5px; padding: 0.26rem 0.65rem; font-size: 0.78rem; cursor: pointer; white-space: nowrap; }
.btn-sm:hover { background: #d0e4f5; }
.btn-add { background: #003366; color: #fff; border: none; border-radius: 5px; padding: 0.33rem 0.7rem; font-size: 0.8rem; cursor: pointer; white-space: nowrap; }
.btn-add:hover { background: #0055a5; }
.btn-del-xs { background: #fdecea; color: #c0392b; border: 1px solid #f5c0bb; border-radius: 4px; padding: 0.18rem 0.5rem; font-size: 0.73rem; cursor: pointer; white-space: nowrap; flex-shrink: 0; }
.btn-del-xs:hover { background: #f9d4d0; }
.btn-x { background: none; border: none; color: #bbb; cursor: pointer; font-size: 0.82rem; padding: 0 3px; flex-shrink: 0; }
.btn-x:hover { color: #c0392b; }

/* Status badges for partner programme clones */
.badge-status-draft    { background: #f0f3f7; color: #666; border-radius: 4px; padding: 1px 7px; font-size: 0.72rem; font-weight: 600; }
.badge-status-pending  { background: #fff3cd; color: #856404; border-radius: 4px; padding: 1px 7px; font-size: 0.72rem; font-weight: 700; }
.badge-status-approved { background: #d1fae5; color: #065f46; border-radius: 4px; padding: 1px 7px; font-size: 0.72rem; font-weight: 700; }
.badge-status-rejected { background: #fdecea; color: #c0392b; border-radius: 4px; padding: 1px 7px; font-size: 0.72rem; font-weight: 700; }
</style>
