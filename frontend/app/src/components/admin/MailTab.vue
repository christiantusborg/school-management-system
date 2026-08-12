<template>
  <div class="mail">
    <div class="mail-top">
      <button class="mail-btn" :disabled="syncing || !activeAccount" @click="syncNow">
        {{ syncing ? 'Syncing…' : '⟳ Sync' }}
      </button>
      <button class="mail-btn mail-btn-primary" :disabled="!accounts.length" @click="openCompose()">✎ New mail</button>
      <input v-model="search" class="mail-search" placeholder="Search subject / sender…" @keyup.enter="loadMessages" />
      <span v-if="err" class="mail-err">{{ err }}</span>
      <button v-if="auth.can('mail.accounts_manage')" class="mail-gear" title="Mail accounts (SuperAdmin)" @click="openConfig">⚙</button>
    </div>

    <div class="mail-layout">
      <!-- accounts + folders -->
      <aside class="mail-side">
        <div v-for="a in accounts" :key="a.mailAccountId" class="mail-acct">
          <div class="mail-acct-head" :style="{ borderLeftColor: a.color || '#cfd7e3' }"
               :class="{ active: activeAccount === a.mailAccountId && !activeFolder }"
               @click="pickAccount(a)">
            <span class="mail-acct-dot" :style="{ background: a.color || '#cfd7e3' }"></span>
            <div class="mail-acct-names">
              <strong>{{ a.displayName }}</strong>
              <span class="mail-acct-mail">{{ a.emailAddress }}</span>
            </div>
          </div>
          <p v-if="a.lastSyncError" class="mail-acct-err" :title="a.lastSyncError">⚠ sync error</p>
          <div v-for="f in a.folders" :key="f.mailFolderId"
               :class="['mail-folder', { active: activeFolder === f.mailFolderId }]"
               @click="pickFolder(a, f)">
            {{ folderLabel(f) }}
            <span v-if="f.unread" class="mail-unread">{{ f.unread }}</span>
          </div>
        </div>
        <p v-if="!accounts.length" class="muted" style="padding:.5rem;">
          No mail accounts you have access to.
        </p>
      </aside>

      <!-- message list -->
      <div class="mail-list">
        <div v-for="m in messages" :key="m.mailMessageId"
             :class="['mail-row', { unread: !m.isRead, active: reader?.mailMessageId === m.mailMessageId }]"
             @click="openMessage(m.mailMessageId)">
          <span class="mail-row-dot" :style="{ background: m.accountColor || '#cfd7e3' }" :title="`${m.accountName} <${m.accountEmail}>`"></span>
          <div class="mail-row-main">
            <div class="mail-row-top">
              <strong class="mail-row-from">{{ m.isOutbound ? `→ ${m.toAddresses}` : (m.fromName || m.fromAddress) }}</strong>
              <span class="mail-row-date">{{ fmtDate(m.sentAt) }}</span>
            </div>
            <div class="mail-row-sub">
              <span class="mail-row-subject">{{ m.subject || '(no subject)' }}</span>
              <span v-if="m.hasAttachments" title="Has attachments">📎</span>
            </div>
            <div class="mail-row-links">
              <span class="mail-acct-chip" :style="{ background: m.accountColor || '#eef' }">{{ m.accountName }}</span>
              <span v-for="(l, i) in m.links" :key="i" class="mail-link-chip clickable"
                    :title="l.studentId ? 'Open student' : l.crmLeadId ? 'Open lead in CRM' : 'Open partner'"
                    @click.stop="openLink(l)">
                {{ l.studentId ? `🎓 ${l.studentName || l.studentNumber || 'Student'}`
                  : l.crmLeadId ? `📈 ${l.leadName || 'Lead'}`
                  : `🤝 ${l.partnerName || 'Partner'}` }}
              </span>
              <span v-if="!m.links.length && !m.isOutbound" class="mail-link-chip unknown">unknown</span>
            </div>
          </div>
        </div>
        <p v-if="!messages.length" class="muted" style="padding:.7rem;">No mail here yet — try Sync.</p>
        <div v-if="total > messages.length" class="mail-more">
          <button class="mail-btn" @click="page++; loadMessages(true)">Load more ({{ total - messages.length }} left)</button>
        </div>
      </div>

      <!-- reader -->
      <div class="mail-reader">
        <template v-if="reader">
          <div class="mail-reader-head">
            <h3>{{ reader.subject || '(no subject)' }}</h3>
            <div class="mail-reader-meta">
              <span class="mail-acct-chip" :style="{ background: reader.accountColor || '#eef' }">
                {{ reader.accountName }} · {{ reader.accountEmail }}
              </span>
              <span><strong>From:</strong> {{ reader.fromName }} &lt;{{ reader.fromAddress }}&gt;</span>
              <span><strong>To:</strong> {{ reader.toAddresses }}</span>
              <span v-if="reader.ccAddresses"><strong>Cc:</strong> {{ reader.ccAddresses }}</span>
              <span>{{ fmtDate(reader.sentAt) }}</span>
            </div>
            <div class="mail-reader-links">
              <span v-for="(l, i) in reader.links" :key="i" class="mail-link-chip big clickable"
                    :title="l.studentId ? 'Open student' : l.crmLeadId ? 'Open lead in CRM' : 'Open partner'"
                    @click="openLink(l)">
                {{ l.studentId ? `🎓 ${l.studentNumber}` : l.crmLeadId ? `📈 ${l.leadName}` : `🤝 ${l.partnerName}` }}
              </span>
              <button v-if="!reader.links.some(l => l.crmLeadId) && !reader.isOutbound && reader.fromAddress"
                      class="mail-btn mail-btn-lead" @click="createLead">📈 Create lead from sender</button>
            </div>
            <div class="mail-reader-actions">
              <button class="mail-btn" @click="openCompose('reply')">↩ Reply</button>
              <button class="mail-btn" @click="openCompose('replyAll')">↩↩ Reply all</button>
            </div>
          </div>
          <div class="mail-atts" v-if="reader.attachments?.length">
            <a v-for="a in reader.attachments" :key="a.mailAttachmentId" class="mail-att"
               :class="{ skipped: a.skipped }" @click="a.skipped ? null : downloadAtt(a)">
              📎 {{ a.fileName }} <span class="muted">({{ fmtSize(a.sizeBytes) }}{{ a.skipped ? ', too large — not stored' : '' }})</span>
            </a>
          </div>
          <iframe v-if="reader.bodyHtml" class="mail-body-frame" sandbox="" :srcdoc="reader.bodyHtml"></iframe>
          <pre v-else class="mail-body-text">{{ reader.bodyText || '(empty message)' }}</pre>
        </template>
        <p v-else class="muted" style="padding:1rem;">Select a message to read it.</p>
      </div>
    </div>

    <!-- compose modal -->
    <Teleport to="body">
      <div v-if="compose" class="mailc-overlay" @click.self="compose = null">
        <div class="mailc-modal">
          <div class="mailc-head">
            <h3>{{ compose.replyTo ? 'Reply' : 'New mail' }}</h3>
            <button class="mailc-x" @click="compose = null">✕</button>
          </div>
          <p v-if="compose.error" class="mail-err">{{ compose.error }}</p>
          <div class="mailc-f">
            <label>From</label>
            <select v-model="compose.accountId">
              <option v-for="a in accounts" :key="a.mailAccountId" :value="a.mailAccountId">
                {{ a.displayName }} &lt;{{ a.emailAddress }}&gt;
              </option>
            </select>
            <span class="mail-acct-chip" :style="{ background: accounts.find(a => a.mailAccountId === compose.accountId)?.color || '#eef' }">
              sends via this account's real mail server
            </span>
          </div>
          <div class="mailc-f"><label>To</label><input v-model="compose.to" placeholder="a@x.com, b@y.com" /></div>
          <div class="mailc-f"><label>Cc</label><input v-model="compose.cc" /></div>
          <div class="mailc-f"><label>Subject</label><input v-model="compose.subject" /></div>
          <textarea v-model="compose.body" rows="12" class="mailc-body"></textarea>
          <div class="mailc-actions">
            <button class="mail-btn mail-btn-primary" :disabled="compose.busy || !compose.to.trim() || !compose.body.trim()" @click="sendMail">
              {{ compose.busy ? 'Sending…' : 'Send' }}
            </button>
            <button class="mail-btn" @click="compose = null">Cancel</button>
          </div>
        </div>
      </div>

      <!-- accounts config (SuperAdmin) -->
      <div v-if="config" class="mailc-overlay" @click.self="config = null">
        <div class="mailc-modal mailc-wide">
          <div class="mailc-head">
            <h3>⚙ Mail accounts</h3>
            <button class="mailc-x" @click="config = null">✕</button>
          </div>
          <p v-if="config.error" class="mail-err">{{ config.error }}</p>
          <table class="mailcfg-tbl">
            <thead><tr><th></th><th>Name</th><th>Address</th><th>Last sync</th><th>Access</th><th></th></tr></thead>
            <tbody>
              <tr v-for="a in accounts" :key="a.mailAccountId">
                <td><span class="mail-acct-dot" :style="{ background: a.color || '#cfd7e3' }"></span></td>
                <td>{{ a.displayName }}</td>
                <td>{{ a.emailAddress }}</td>
                <td class="muted">{{ a.lastSyncAt ? fmtDate(a.lastSyncAt) : '—' }}<span v-if="a.lastSyncError" title="a.lastSyncError"> ⚠</span></td>
                <td class="muted">{{ (a.accessUserIds || []).length }} user(s)</td>
                <td>
                  <button class="mail-btn" @click="editAccount(a)">Edit</button>
                  <button class="mail-btn mail-btn-danger" @click="deleteAccount(a)">Delete</button>
                </td>
              </tr>
            </tbody>
          </table>
          <button class="mail-btn" style="margin:.5rem 0;" @click="editAccount(null)">+ Add mail account</button>

          <div v-if="config.form" class="mailcfg-form">
            <h4>{{ config.form.mailAccountId ? 'Edit account' : 'New account' }}</h4>
            <div class="mailcfg-grid">
              <label>Display name<input v-model="config.form.displayName" /></label>
              <label>Email address<input v-model="config.form.emailAddress" /></label>
              <label>Label colour<input v-model="config.form.color" type="color" /></label>
              <label>Username<input v-model="config.form.username" /></label>
              <label>Password<input v-model="config.form.password" type="password" :placeholder="config.form.mailAccountId ? '(unchanged)' : ''" /></label>
              <label>IMAP host<input v-model="config.form.imapHost" placeholder="imap.example.com" /></label>
              <label>IMAP port<input v-model.number="config.form.imapPort" type="number" /></label>
              <label>IMAP SSL<input v-model="config.form.imapUseSsl" type="checkbox" /></label>
              <label>SMTP host<input v-model="config.form.smtpHost" placeholder="smtp.example.com" /></label>
              <label>SMTP port<input v-model.number="config.form.smtpPort" type="number" /></label>
              <label>SMTP StartTLS<input v-model="config.form.smtpUseSsl" type="checkbox" /></label>
              <label>Enabled<input v-model="config.form.isEnabled" type="checkbox" /></label>
            </div>
            <h4>Who can open this mailbox</h4>
            <div class="mailcfg-users">
              <label v-for="u in adminUsers" :key="u.userId" class="mailcfg-user">
                <input type="checkbox" :value="u.userId" v-model="config.form.accessUserIds" /> {{ u.name }}
              </label>
            </div>
            <div class="mailc-actions">
              <button class="mail-btn mail-btn-primary" :disabled="config.busy" @click="saveAccount">
                {{ config.busy ? 'Saving…' : 'Save account' }}
              </button>
              <button class="mail-btn" @click="config.form = null">Cancel</button>
            </div>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '../../api/client.js'
import { auth } from '../../store/auth.js'

const emit = defineEmits(['open-student', 'open-partner', 'open-lead'])
function openLink(l) {
  if (l.studentId) emit('open-student', l.studentId)
  else if (l.crmLeadId) emit('open-lead', l.crmLeadId)
  else if (l.partnerId) emit('open-partner', l.partnerId)
}
const accounts = ref([])
const isSuperAdmin = ref(false)
const activeAccount = ref('')
const activeFolder = ref('')
const messages = ref([])
const total = ref(0)
const page = ref(1)
const search = ref('')
const reader = ref(null)
const compose = ref(null)
const config = ref(null)
const adminUsers = ref([])
const syncing = ref(false)
const err = ref('')

const fmtDate = d => d ? new Date(d).toLocaleString('en-GB', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' }) : ''
const fmtSize = b => b > 1048576 ? `${(b / 1048576).toFixed(1)} MB` : `${Math.ceil(b / 1024)} KB`
function folderLabel(f) {
  const n = f.name.split(/[/.]/).pop()
  return f.isSent ? `📤 ${n}` : n.toLowerCase() === 'inbox' ? '📥 Inbox' : `📁 ${n}`
}

async function loadAccounts() {
  try {
    const res = await api.get('/v1/admin/mail/accounts')
    accounts.value = res.data.items ?? []
    isSuperAdmin.value = !!res.data.isSuperAdmin
    if (!activeAccount.value && accounts.value.length) {
      const a = accounts.value[0]
      activeAccount.value = a.mailAccountId
      const inbox = a.folders.find(f => f.name.toLowerCase() === 'inbox') ?? a.folders[0]
      activeFolder.value = inbox?.mailFolderId ?? ''
    }
  } catch (e) { err.value = e.response?.data?.error ?? e.message }
}
function pickAccount(a) { activeAccount.value = a.mailAccountId; activeFolder.value = ''; page.value = 1; loadMessages() }
function pickFolder(a, f) { activeAccount.value = a.mailAccountId; activeFolder.value = f.mailFolderId; page.value = 1; loadMessages() }

async function loadMessages(append = false) {
  try {
    const res = await api.get('/v1/admin/mail/messages', { params: {
      accountId: activeAccount.value || undefined,
      folderId: activeFolder.value || undefined,
      search: search.value || undefined,
      page: page.value, pageSize: 50,
    } })
    messages.value = append ? [...messages.value, ...(res.data.items ?? [])] : (res.data.items ?? [])
    total.value = res.data.total ?? 0
  } catch (e) { err.value = e.response?.data?.error ?? e.message }
}
async function openMessage(id) {
  try {
    reader.value = (await api.get(`/v1/admin/mail/messages/${id}`)).data
    const row = messages.value.find(m => m.mailMessageId === id)
    if (row) row.isRead = true
  } catch (e) { err.value = e.response?.data?.error ?? e.message }
}
async function syncNow() {
  if (!activeAccount.value || syncing.value) return
  syncing.value = true; err.value = ''
  try {
    await api.post(`/v1/admin/mail/accounts/${activeAccount.value}/sync`)
    await Promise.all([loadAccounts(), loadMessages()])
  } catch (e) { err.value = e.response?.data?.error ?? e.message }
  finally { syncing.value = false }
}
async function downloadAtt(a) {
  try {
    const res = await api.get(`/v1/admin/mail/attachments/${a.mailAttachmentId}`, { responseType: 'blob' })
    const url = URL.createObjectURL(res.data)
    const el = document.createElement('a')
    el.href = url; el.download = a.fileName; el.click()
    URL.revokeObjectURL(url)
  } catch (e) { err.value = 'Download failed' }
}
async function createLead() {
  if (!reader.value) return
  try {
    const res = await api.post(`/v1/admin/mail/messages/${reader.value.mailMessageId}/create-lead`)
    await openMessage(reader.value.mailMessageId)
    err.value = ''
    alert(res.data.existed ? 'Linked to an existing lead with this email.' : 'Lead created and linked — find it on the CRM board.')
  } catch (e) { err.value = e.response?.data?.error ?? e.message }
}

function openCompose(mode) {
  const r = reader.value
  const acct = r?.accountId ?? activeAccount.value ?? accounts.value[0]?.mailAccountId
  if (mode && r) {
    const cc = mode === 'replyAll' ? [r.toAddresses, r.ccAddresses].filter(Boolean).join(', ') : ''
    compose.value = {
      accountId: acct, replyTo: r.mailMessageId,
      to: r.isOutbound ? r.toAddresses : r.fromAddress,
      cc,
      subject: /^re:/i.test(r.subject ?? '') ? r.subject : `Re: ${r.subject ?? ''}`,
      body: `\n\n--- On ${fmtDate(r.sentAt)}, ${r.fromName || r.fromAddress} wrote:\n> ` +
        (r.bodyText ?? '').split('\n').slice(0, 20).join('\n> '),
      busy: false, error: '',
    }
  } else {
    compose.value = { accountId: acct, replyTo: null, to: '', cc: '', subject: '', body: '', busy: false, error: '' }
  }
}
async function sendMail() {
  const c = compose.value
  if (!c || c.busy) return
  c.busy = true; c.error = ''
  try {
    await api.post('/v1/admin/mail/send', {
      accountId: c.accountId, to: c.to, cc: c.cc || null,
      subject: c.subject, body: c.body, inReplyToMessageId: c.replyTo,
    })
    compose.value = null
    await loadMessages()
  } catch (e) { c.error = e.response?.data?.error ?? e.message } finally { c.busy = false }
}

// ── accounts config (SuperAdmin) ─────────────────────────────────────────────
async function openConfig() {
  config.value = { form: null, busy: false, error: '' }
  try {
    const res = await api.get('/v1/admin/admin-users')
    adminUsers.value = (res.data.items ?? []).map(u => ({
      userId: u.userId ?? u.id, name: u.name ?? u.userName ?? u.email }))
  } catch { adminUsers.value = [] }
}
function editAccount(a) {
  config.value.form = a ? {
    mailAccountId: a.mailAccountId, displayName: a.displayName, emailAddress: a.emailAddress,
    color: a.color || '#1058a4', username: a.username, password: '',
    imapHost: a.imapHost, imapPort: a.imapPort, imapUseSsl: a.imapUseSsl,
    smtpHost: a.smtpHost, smtpPort: a.smtpPort, smtpUseSsl: a.smtpUseSsl,
    isEnabled: a.isEnabled, accessUserIds: [...(a.accessUserIds ?? [])],
  } : {
    mailAccountId: null, displayName: '', emailAddress: '', color: '#1058a4', username: '', password: '',
    imapHost: '', imapPort: 993, imapUseSsl: true, smtpHost: '', smtpPort: 587, smtpUseSsl: true,
    isEnabled: true, accessUserIds: [],
  }
}
async function saveAccount() {
  const f = config.value.form
  if (!f || config.value.busy) return
  config.value.busy = true; config.value.error = ''
  try {
    let id = f.mailAccountId
    if (id) await api.patch(`/v1/admin/mail/accounts/${id}`, f)
    else id = (await api.post('/v1/admin/mail/accounts', f)).data.mailAccountId
    await api.put(`/v1/admin/mail/accounts/${id}/access`, { userIds: f.accessUserIds })
    config.value.form = null
    await loadAccounts()
  } catch (e) { config.value.error = e.response?.data?.error ?? e.message }
  finally { config.value.busy = false }
}
async function deleteAccount(a) {
  if (!confirm(`Remove mail account ${a.emailAddress}? Synced mail stays archived.`)) return
  try {
    await api.delete(`/v1/admin/mail/accounts/${a.mailAccountId}`)
    await loadAccounts()
  } catch (e) { config.value.error = e.response?.data?.error ?? e.message }
}

onMounted(async () => { await loadAccounts(); if (activeAccount.value) await loadMessages() })
</script>

<style scoped>
.mail { padding: .25rem 0; }
.muted { color: #5f6e85; font-size: .8rem; }
.mail-err { color: #b42318; font-size: .8rem; }
.mail-top { display: flex; align-items: center; gap: .5rem; margin-bottom: .6rem; }
.mail-btn { padding: .35rem .7rem; border: 1px solid #cfd7e3; background: #fff; border-radius: 6px; font-size: .8rem; cursor: pointer; }
.mail-btn-primary { background: #0b2e59; color: #fff; border-color: #0b2e59; }
.mail-btn-danger { border-color: #b63329; color: #b63329; }
.mail-btn-lead { border-color: #6b21a8; color: #6b21a8; font-weight: 700; }
.mail-search { flex: 1; max-width: 340px; padding: .38rem .6rem; border: 1px solid #cfd7e3; border-radius: 6px; font-size: .82rem; }
.mail-gear { margin-left: auto; border: 1px solid #cfd7e3; background: #fff; border-radius: 6px; padding: .3rem .6rem; cursor: pointer; font-size: .95rem; }

.mail-layout { display: flex; gap: .6rem; align-items: stretch; height: calc(100vh - 240px); min-height: 480px; }
.mail-side { width: 230px; flex-shrink: 0; overflow-y: auto; background: #fff; border: 1px solid #e0e6ee; border-radius: 8px; padding: .4rem; }
.mail-acct { margin-bottom: .5rem; }
.mail-acct-head { display: flex; gap: .45rem; align-items: center; padding: .4rem .5rem; border-left: 4px solid; border-radius: 5px; cursor: pointer; background: #f6f9fd; }
.mail-acct-head.active { background: #eef3fb; }
.mail-acct-dot { width: 10px; height: 10px; border-radius: 50%; flex-shrink: 0; display: inline-block; }
.mail-acct-names { display: flex; flex-direction: column; min-width: 0; }
.mail-acct-names strong { font-size: .8rem; color: #0b2e59; }
.mail-acct-mail { font-size: .68rem; color: #8a93a4; overflow: hidden; text-overflow: ellipsis; }
.mail-acct-err { color: #b42318; font-size: .68rem; margin: .1rem 0 0 .5rem; }
.mail-folder { padding: .28rem .5rem .28rem 1.4rem; font-size: .78rem; color: #445; cursor: pointer; border-radius: 5px; display: flex; justify-content: space-between; }
.mail-folder:hover { background: #f2f5f9; }
.mail-folder.active { background: #eef3fb; color: #0b2e59; font-weight: 700; }
.mail-unread { background: #0b2e59; color: #fff; border-radius: 8px; font-size: .66rem; padding: 0 6px; }

.mail-list { width: 380px; flex-shrink: 0; overflow-y: auto; background: #fff; border: 1px solid #e0e6ee; border-radius: 8px; }
.mail-row { display: flex; gap: .5rem; padding: .5rem .6rem; border-bottom: 1px solid #f0f3f7; cursor: pointer; }
.mail-row:hover { background: #f8fafd; }
.mail-row.active { background: #eef3fb; }
.mail-row.unread .mail-row-from, .mail-row.unread .mail-row-subject { font-weight: 800; }
.mail-row-dot { width: 9px; height: 9px; border-radius: 50%; margin-top: .35rem; flex-shrink: 0; }
.mail-row-main { flex: 1; min-width: 0; }
.mail-row-top { display: flex; justify-content: space-between; gap: .5rem; }
.mail-row-from { font-size: .8rem; color: #16324f; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.mail-row-date { font-size: .68rem; color: #8a93a4; flex-shrink: 0; }
.mail-row-sub { display: flex; gap: .3rem; font-size: .78rem; color: #445; }
.mail-row-subject { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.mail-row-links { display: flex; gap: .3rem; margin-top: .2rem; flex-wrap: wrap; }
.mail-acct-chip { font-size: .64rem; border-radius: 8px; padding: 0 7px; color: #0b2e59; font-weight: 700; }
.mail-link-chip { font-size: .64rem; border-radius: 8px; padding: 0 7px; background: #ecf0f6; color: #445; }
.mail-link-chip.unknown { background: #fff1cc; color: #8a6b16; }
.mail-link-chip.clickable { cursor: pointer; }
.mail-link-chip.clickable:hover { background: #dbe7f6; }
.mail-link-chip.big { font-size: .74rem; padding: 2px 9px; }

.mail-reader { flex: 1; min-width: 0; overflow-y: auto; background: #fff; border: 1px solid #e0e6ee; border-radius: 8px; display: flex; flex-direction: column; }
.mail-reader-head { padding: .7rem .9rem; border-bottom: 1px solid #e8edf3; }
.mail-reader-head h3 { margin: 0 0 .3rem; font-size: 1rem; color: #0b2e59; }
.mail-reader-meta { display: flex; flex-direction: column; gap: .1rem; font-size: .76rem; color: #445; }
.mail-reader-links { display: flex; gap: .35rem; margin-top: .4rem; flex-wrap: wrap; align-items: center; }
.mail-reader-actions { display: flex; gap: .4rem; margin-top: .5rem; }
.mail-atts { padding: .4rem .9rem; border-bottom: 1px solid #e8edf3; display: flex; gap: .6rem; flex-wrap: wrap; }
.mail-att { font-size: .78rem; color: #1058a4; cursor: pointer; }
.mail-att.skipped { color: #8a93a4; cursor: default; }
.mail-body-frame { flex: 1; border: 0; width: 100%; min-height: 300px; background: #fff; }
.mail-body-text { flex: 1; padding: .8rem .9rem; font-family: inherit; font-size: .86rem; white-space: pre-wrap; margin: 0; }
.mail-more { padding: .5rem; text-align: center; }
</style>

<style>
.mailc-overlay { position: fixed; inset: 0; background: rgba(10, 25, 47, .45); z-index: 1100; display: flex; align-items: center; justify-content: center; }
.mailc-modal { width: 720px; max-width: 96vw; max-height: 92vh; overflow-y: auto; background: #fff; border-radius: 10px; padding: 1rem 1.2rem; }
.mailc-wide { width: 980px; }
.mailc-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: .6rem; }
.mailc-head h3 { margin: 0; font-size: 1rem; color: #0b2e59; }
.mailc-x { background: none; border: none; cursor: pointer; font-size: 1rem; color: #5f6e85; }
.mailc-f { display: flex; align-items: center; gap: .5rem; margin-bottom: .45rem; }
.mailc-f label { width: 60px; font-size: .74rem; font-weight: 700; color: #5f6e85; text-transform: uppercase; }
.mailc-f input, .mailc-f select { flex: 1; padding: .38rem .55rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .84rem; }
.mailc-body { width: 100%; border: 1px solid #cfd7e3; border-radius: 6px; padding: .55rem; font-size: .86rem; font-family: inherit; }
.mailc-actions { display: flex; gap: .5rem; margin-top: .6rem; }
.mailcfg-tbl { width: 100%; border-collapse: collapse; font-size: .82rem; }
.mailcfg-tbl th { text-align: left; font-size: .66rem; text-transform: uppercase; color: #6b7888; padding: .3rem .4rem; border-bottom: 1px solid #e8edf3; }
.mailcfg-tbl td { padding: .35rem .4rem; border-bottom: 1px solid #f0f3f7; }
.mailcfg-form { border-top: 2px solid #e8edf3; margin-top: .6rem; padding-top: .6rem; }
.mailcfg-form h4 { margin: .4rem 0; font-size: .84rem; color: #0b2e59; }
.mailcfg-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: .45rem .7rem; }
.mailcfg-grid label { display: flex; flex-direction: column; gap: .15rem; font-size: .72rem; font-weight: 700; color: #5f6e85; }
.mailcfg-grid input[type="text"], .mailcfg-grid input:not([type="checkbox"]):not([type="color"]) { padding: .35rem .5rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .82rem; font-weight: 400; }
.mailcfg-users { display: flex; gap: .4rem; flex-wrap: wrap; }
.mailcfg-user { font-size: .8rem; background: #f6f9fd; border: 1px solid #e0e6ee; border-radius: 6px; padding: .25rem .55rem; }
</style>
