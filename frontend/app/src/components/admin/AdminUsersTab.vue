<template>
  <div class="au-tab">
    <div v-if="loadError" class="err-banner">{{ loadError }}</div>

    <h2 class="page-title">Admin Users</h2>
    <p class="page-sub">{{ users.length }} admin user{{ users.length !== 1 ? 's' : '' }}</p>
    <div class="ref-bar">
      <span class="ref-lbl">Referral links — partner:</span>
      <select v-model="refPartnerSlug" class="ref-sel">
        <option value="">— choose a partner —</option>
        <option v-for="p in refPartners" :key="p.slug" :value="p.slug">{{ p.name }}</option>
      </select>
      <span class="muted" style="font-size:.76rem">🔗 Ref link copies a personal signup URL; students who apply through it are shown as "Added by" that user.</span>
    </div>


    <table v-if="users.length" class="data-table">
      <thead><tr><th>Username</th><th>Email</th><th>Level</th><th>Status</th><th>Actions</th></tr></thead>
      <tbody>
        <tr v-for="u in users" :key="u.userId" class="data-row">
          <td class="mono">{{ u.username }}<span v-if="u.isSelf" class="self-tag">you</span></td>
          <td>{{ u.email ?? '—' }}</td>
          <td>
            <select :value="u.level" :disabled="busy === u.userId" @change="changeLevel(u, $event.target.value)">
              <option v-for="lvl in levels" :key="lvl" :value="lvl" :disabled="u.isSelf && lvl !== 'SuperAdministrator'">
                {{ humanLevel(lvl) }}
              </option>
            </select>
          </td>
          <td>
            <button class="btn-pill" :disabled="u.isSelf || busy === u.userId" :class="['status', u.isEnabled ? 'on' : 'off']"
                    :title="u.isSelf ? 'You cannot disable yourself' : (u.isEnabled ? 'Click to disable' : 'Click to enable')"
                    @click="toggleEnabled(u)">
              {{ u.isEnabled ? 'Active' : 'Disabled' }}
            </button>
          </td>
          <td class="actions">
            <button v-if="u.level === 'Sales'" class="btn-sm" title="Assign which partners this Sales user can see"
                    @click="openSalesPartners(u)">🤝 Partners</button>
            <button class="btn-sm" :title="refPartnerSlug ? 'Copy this user\'s personal signup link — students who sign up through it show them as Added by' : 'Pick a partner in the Referral links selector above first'"
                    :disabled="!refPartnerSlug" @click="copyRefLink(u)">
              {{ copiedRef === u.userId ? '✓ Copied' : '🔗 Ref link' }}</button>
            <button class="btn-sm" :disabled="busy === u.userId" @click="resetPassword(u)">
              {{ busy === `reset:${u.userId}` ? 'Resetting…' : 'Reset password' }}
            </button>
            <button class="btn-sm btn-danger"
                    :disabled="u.isSelf || busy === u.userId"
                    :title="u.isSelf ? 'You cannot delete yourself' : 'Soft-delete this admin user'"
                    @click="del(u)">
              Delete
            </button>
          </td>
        </tr>
      </tbody>
    </table>

    <p v-else class="empty">No admin users yet.</p>

    <!-- Add admin -->
    <div class="add-card">
      <h3>+ Add a new admin user</h3>
      <p class="hint">Their temporary password is shown once after creation. Share it via a secure channel.</p>
      <div class="add-fields">
        <input v-model="newUser.username" class="inp" placeholder="Username" />
        <input v-model="newUser.email" class="inp" placeholder="Email (optional)" />
        <select v-model="newUser.level" class="inp">
          <option v-for="lvl in levels" :key="lvl" :value="lvl">{{ humanLevel(lvl) }}</option>
        </select>
        <button class="btn-primary" :disabled="!canAdd || busy === 'add'" @click="add">
          {{ busy === 'add' ? 'Creating…' : '+ Add admin' }}
        </button>
      </div>
      <p v-if="addError" class="err-banner">{{ addError }}</p>
      <div v-if="lastCreated" class="created-banner">
        <strong>Admin created.</strong>
        <p>Username: <code>{{ lastCreated.username }}</code> · Level: <code>{{ humanLevel(lastCreated.level) }}</code></p>
        <p>Temporary password: <code class="pw">{{ lastCreated.temporaryPassword }}</code>
          <button class="btn-tiny" @click="copy(lastCreated.temporaryPassword)">Copy</button>
        </p>
        <button class="btn-tiny" @click="lastCreated = null">Dismiss</button>
      </div>
    </div>

    <!-- Reset password result -->
    <div v-if="lastReset" class="created-banner">
      <strong>Password reset for {{ lastReset.username }}.</strong>
      <p>Temporary password: <code class="pw">{{ lastReset.temporaryPassword }}</code>
        <button class="btn-tiny" @click="copy(lastReset.temporaryPassword)">Copy</button>
      </p>
      <button class="btn-tiny" @click="lastReset = null">Dismiss</button>
    </div>
  </div>
  <Teleport to="body">
    <div v-if="spDlg.open" class="sp-backdrop" @click.self="spDlg.open = false">
      <div class="sp-dialog">
        <h3 class="sp-title">Partners for {{ spDlg.userName }}</h3>
        <p class="sp-hint">This Sales user sees only the ticked partners (plus students they added or handle).
          Administrator level required to change.</p>
        <label v-for="p in refPartners" :key="p.partnerId ?? p.slug" class="sp-check">
          <input type="checkbox" :value="p.partnerId" v-model="spDlg.partnerIds" :disabled="!canAssignSales" />
          {{ p.name }}</label>
        <p v-if="spDlg.error" class="sp-err">{{ spDlg.error }}</p>
        <div class="sp-actions">
          <button type="button" class="btn-sm" @click="spDlg.open = false">Close</button>
          <button v-if="canAssignSales" type="button" class="btn-add" :disabled="spDlg.busy" @click="saveSalesPartners">
            {{ spDlg.busy ? 'Saving…' : 'Save' }}</button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { auth } from '../../store/auth.js'
import api from '../../api/client.js'

const users = ref([])
const levels = ref(['SuperAdministrator', 'Administrator', 'Manager', 'Editor', 'Viewer', 'Sales'])

// Personal referral signup links (?ref=<userId> attributes "Added by").
const refPartners = ref([])
const refPartnerSlug = ref('')
const copiedRef = ref('')
async function loadRefPartners() {
  try {
    const res = await api.get('/v1/admin/school/partners')
    refPartners.value = (res.data.items ?? []).filter(p => p.slug)
  } catch { refPartners.value = [] }
}
loadRefPartners()
// Sales↔partner assignment dialog (Administrator+ writes).
const spDlg = reactive({ open: false, userId: '', userName: '', partnerIds: [], busy: false, error: '' })
const canAssignSales = computed(() =>
  ['SuperAdministrator', 'Administrator'].includes(auth.adminLevel))
async function openSalesPartners(u) {
  spDlg.userId = u.userId
  spDlg.userName = u.username ?? u.userName ?? ''
  spDlg.error = ''
  try {
    spDlg.partnerIds = (await api.get(`/v1/admin/sales-staff/${u.userId}/partners`)).data.partnerIds ?? []
  } catch { spDlg.partnerIds = [] }
  spDlg.open = true
}
async function saveSalesPartners() {
  if (spDlg.busy) return
  spDlg.busy = true
  spDlg.error = ''
  try {
    await api.put(`/v1/admin/sales-staff/${spDlg.userId}/partners`, { partnerIds: spDlg.partnerIds })
    spDlg.open = false
  } catch (e) {
    spDlg.error = e.response?.data?.error ?? e.message ?? 'Save failed'
  } finally {
    spDlg.busy = false
  }
}

async function copyRefLink(u) {
  const url = `${window.location.origin}/#/apply?partner=${encodeURIComponent(refPartnerSlug.value)}&ref=${encodeURIComponent(u.userId)}`
  try { await navigator.clipboard.writeText(url) } catch { prompt('Copy the link:', url) }
  copiedRef.value = u.userId
  setTimeout(() => { if (copiedRef.value === u.userId) copiedRef.value = '' }, 2000)
}
const loadError = ref('')
const busy = ref('')

const newUser = reactive({ username: '', email: '', level: 'Viewer' })
const addError = ref('')
const lastCreated = ref(null)
const lastReset = ref(null)

const canAdd = computed(() => newUser.username.trim().length > 0 && levels.value.includes(newUser.level))

function humanLevel(l) {
  switch (l) {
    case 'SuperAdministrator': return 'Super Administrator'
    case 'Administrator':      return 'Administrator'
    case 'Manager':            return 'Manager'
    case 'Editor':             return 'Editor'
    case 'Viewer':             return 'Viewer'
    default:                   return l ?? '—'
  }
}

async function loadLevels() {
  try {
    const res = await api.get('/v1/admin/admin-levels')
    if (Array.isArray(res.data?.items) && res.data.items.length) levels.value = res.data.items
  } catch { /* fall back to hardcoded list */ }
}

async function load() {
  loadError.value = ''
  try {
    const res = await api.get('/v1/admin/admin-users')
    users.value = res.data.items ?? []
  } catch (e) {
    loadError.value = e.response?.data?.error ?? e.message ?? 'Failed to load'
  }
}

async function add() {
  if (!canAdd.value) return
  addError.value = ''
  busy.value = 'add'
  try {
    const res = await api.post('/v1/admin/admin-users', {
      username: newUser.username.trim(),
      email: newUser.email.trim() || null,
      level: newUser.level,
    })
    lastCreated.value = res.data
    newUser.username = ''
    newUser.email = ''
    newUser.level = 'Viewer'
    await load()
  } catch (e) {
    addError.value = e.response?.data?.error ?? e.message ?? 'Failed'
  } finally { busy.value = '' }
}

async function changeLevel(u, level) {
  if (level === u.level) return
  busy.value = u.userId
  try {
    await api.patch(`/v1/admin/admin-users/${u.userId}`, { level })
    await load()
  } catch (e) {
    loadError.value = e.response?.data?.error ?? e.message ?? 'Failed to change level'
  } finally { busy.value = '' }
}

async function toggleEnabled(u) {
  busy.value = u.userId
  try {
    await api.patch(`/v1/admin/admin-users/${u.userId}`, { isEnabled: !u.isEnabled })
    await load()
  } catch (e) {
    loadError.value = e.response?.data?.error ?? e.message ?? 'Failed to update status'
  } finally { busy.value = '' }
}

async function resetPassword(u) {
  busy.value = `reset:${u.userId}`
  try {
    const res = await api.post(`/v1/admin/admin-users/${u.userId}/reset-password`)
    lastReset.value = res.data
  } catch (e) {
    loadError.value = e.response?.data?.error ?? e.message ?? 'Failed to reset'
  } finally { busy.value = '' }
}

async function del(u) {
  if (u.isSelf) return
  if (!confirm(`Delete admin user ${u.username}? They will be soft-deleted and unable to log in.`)) return
  busy.value = u.userId
  try {
    await api.delete(`/v1/admin/admin-users/${u.userId}`)
    await load()
  } catch (e) {
    loadError.value = e.response?.data?.error ?? e.message ?? 'Failed to delete'
  } finally { busy.value = '' }
}

function copy(text) { if (text) navigator.clipboard.writeText(text).catch(() => {}) }

onMounted(async () => { await loadLevels(); await load() })
</script>

<style scoped>
.au-tab { padding: .5rem 0; }
.page-title { color: #003366; margin: 0; }
.page-sub { color: #888; font-size: .85rem; margin: .25rem 0 1rem; }
.err-banner { background: #fef2f2; border: 1px solid #fca5a5; color: #b91c1c; padding: .5rem .8rem; border-radius: 6px; font-size: .85rem; margin: .5rem 0; }

.data-table { width: 100%; border-collapse: collapse; background: #fff; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.04); overflow: hidden; }
.data-table th { text-align: left; font-size: .72rem; color: #5f6e85; text-transform: uppercase; padding: .55rem .75rem; background: #fafbfc; border-bottom: 1px solid #e5eaf1; }
.data-row td { padding: .55rem .75rem; border-bottom: 1px solid #eef2f7; font-size: .88rem; }
.mono { font-family: monospace; font-size: .82rem; color: #0a264f; }
.self-tag { background: #e8f0f8; color: #003366; border-radius: 4px; padding: 1px 6px; font-size: .7rem; font-weight: 700; margin-left: .4rem; }

select { padding: .25rem .5rem; border: 1.5px solid #d0d7e0; border-radius: 5px; font-size: .82rem; background: #fff; }
.btn-pill { font-size: .75rem; padding: 2px 10px; border-radius: 10px; font-weight: 600; cursor: pointer; border: 1px solid transparent; }
.btn-pill:disabled { cursor: not-allowed; opacity: .65; }
.status.on { background: #d1fae5; color: #065f46; border-color: #6ee7b7; }
.status.off { background: #fef2f2; color: #b91c1c; border-color: #fca5a5; }

.actions { display: flex; gap: .35rem; flex-wrap: wrap; }
.btn-sm { background: #fff; border: 1.5px solid #d0d7e0; padding: .25rem .65rem; border-radius: 5px; font-size: .78rem; cursor: pointer; }
.btn-sm:disabled { opacity: .5; cursor: not-allowed; }
.btn-danger { background: #fef2f2; color: #b91c1c; border-color: #fca5a5; }
.btn-danger:hover:not(:disabled) { background: #fde8e8; }

.empty { padding: 1rem; background: #f6f9fd; color: #5f6e85; border-radius: 8px; text-align: center; }

.add-card { margin-top: 1.25rem; background: #fff; border: 1px solid #e8edf4; border-radius: 8px; padding: 1rem 1.25rem; }
.add-card h3 { color: #003366; margin: 0 0 .35rem; font-size: 1rem; }
.hint { color: #888; font-size: .82rem; margin: 0 0 .65rem; }
.add-fields { display: flex; gap: .55rem; align-items: center; flex-wrap: wrap; }
.inp { padding: .45rem .7rem; border: 1.5px solid #d0d7e0; border-radius: 5px; font-size: .88rem; min-width: 200px; }
.btn-primary { background: #003366; color: #fff; border: 0; padding: .45rem 1rem; border-radius: 5px; font-weight: 600; cursor: pointer; }
.btn-primary:disabled { opacity: .55; cursor: not-allowed; }

.created-banner { margin-top: 1rem; background: #ecfdf5; border: 1px solid #6ee7b7; padding: .75rem 1rem; border-radius: 6px; }
.created-banner strong { color: #065f46; }
.pw { background: #fff; padding: 2px 8px; border-radius: 4px; font-family: monospace; font-size: .82rem; }
.btn-tiny { background: #fff; border: 1px solid #d0d7e0; border-radius: 4px; padding: 2px 8px; font-size: .76rem; cursor: pointer; margin-left: .35rem; }
.ref-bar { display: flex; align-items: center; gap: .6rem; margin: .4rem 0 .8rem; flex-wrap: wrap; }
.ref-lbl { font-size: .82rem; font-weight: 600; color: #44536a; }
.ref-sel { padding: .35rem .5rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .82rem; background: #fff; }
</style>

<style>
/* Sales-partner dialog is teleported to <body> — global styles required. */
.sp-backdrop { position: fixed; inset: 0; background: rgba(15,30,55,.45); display: flex; align-items: flex-start; justify-content: center; padding: 5rem 1rem; z-index: 300; }
.sp-dialog { background: #fff; border-radius: 10px; padding: 1.1rem 1.3rem; width: 100%; max-width: 420px; box-shadow: 0 12px 40px rgba(0,0,0,.25); }
.sp-dialog .sp-title { margin: 0 0 .4rem; font-size: 1.02rem; color: #003366; }
.sp-dialog .sp-hint { font-size: .76rem; color: #6b7888; margin: 0 0 .5rem; }
.sp-dialog .sp-check { display: block; font-size: .87rem; color: #2c3e50; margin: .25rem 0; cursor: pointer; }
.sp-dialog .sp-err { color: #b3261e; font-size: .8rem; margin-top: .4rem; }
.sp-dialog .sp-actions { display: flex; justify-content: flex-end; gap: .5rem; margin-top: .8rem; }
.sp-dialog .btn-sm { background: #f2f5f9; border: 1px solid #cfd7e3; border-radius: 5px; padding: .3rem .6rem; font-size: .78rem; font-weight: 600; color: #2c3e50; cursor: pointer; }
.sp-dialog .btn-add { background: #003366; border: 1px solid #003366; color: #fff; border-radius: 5px; padding: .35rem .8rem; font-size: .8rem; font-weight: 600; cursor: pointer; }
</style>
