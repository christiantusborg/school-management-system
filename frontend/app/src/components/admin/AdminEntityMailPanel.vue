<template>
  <div class="emp">
    <div class="emp-head">
      <button class="emp-btn emp-primary" @click="composeOpen = !composeOpen">✎ Send new mail</button>
      <span v-if="error" class="emp-err">{{ error }}</span>
    </div>

    <div v-if="composeOpen" class="emp-compose">
      <div class="emp-f">
        <label>From</label>
        <select v-model="compose.accountId">
          <option v-for="a in accounts" :key="a.mailAccountId" :value="a.mailAccountId">
            {{ a.displayName }} &lt;{{ a.emailAddress }}&gt;
          </option>
        </select>
      </div>
      <div class="emp-f"><label>To</label><input v-model="compose.to" /></div>
      <div class="emp-f"><label>Subject</label><input v-model="compose.subject" /></div>
      <textarea v-model="compose.body" rows="6" class="emp-body"></textarea>
      <div class="emp-actions">
        <button class="emp-btn emp-primary" :disabled="compose.busy || !compose.to.trim() || !compose.body.trim() || !compose.accountId"
                @click="send">{{ compose.busy ? 'Sending…' : 'Send' }}</button>
        <button class="emp-btn" @click="composeOpen = false">Cancel</button>
        <span v-if="compose.error" class="emp-err">{{ compose.error }}</span>
        <span v-if="sentOk" class="emp-ok">✓ Sent</span>
      </div>
      <p v-if="!accounts.length" class="emp-err">No mail accounts you have access to — ask a SuperAdministrator.</p>
    </div>

    <div v-for="m in items" :key="m.mailMessageId" class="emp-row" @click="open = open === m.mailMessageId ? null : m.mailMessageId">
      <div class="emp-row-top">
        <span class="emp-dir">{{ m.isOutbound ? '📤' : '📥' }}</span>
        <span class="emp-acct" :style="{ background: m.accountColor || '#eef3fb' }">{{ m.accountName }}</span>
        <strong class="emp-from">{{ m.isOutbound ? `→ ${m.toAddresses}` : (m.fromName || m.fromAddress) }}</strong>
        <span class="emp-date">{{ fmt(m.sentAt) }}</span>
      </div>
      <div class="emp-subject">{{ m.subject || '(no subject)' }}</div>
      <pre v-if="open === m.mailMessageId" class="emp-bodytext">{{ m.bodyText || '(no text content)' }}</pre>
    </div>
    <p v-if="!loading && !items.length" class="emp-muted">No mail linked yet.</p>
    <p v-if="loading" class="emp-muted">Loading…</p>
  </div>
</template>

<script setup>
import { ref, reactive, watch, onMounted } from 'vue'
import api from '../../api/client.js'

const props = defineProps({
  kind: { type: String, required: true },      // 'student' | 'partner'
  entityId: { type: String, required: true },
})

const items = ref([])
const accounts = ref([])
const open = ref(null)
const loading = ref(true)
const error = ref('')
const composeOpen = ref(false)
const sentOk = ref(false)
const compose = reactive({ accountId: '', to: '', subject: '', body: '', busy: false, error: '' })
const fmt = d => d ? new Date(d).toLocaleString('en-GB', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' }) : ''

async function load() {
  if (!props.entityId) return
  loading.value = true
  error.value = ''
  try {
    const [mailRes, acctRes] = await Promise.all([
      api.get(`/v1/admin/mail/for-${props.kind}/${props.entityId}`),
      api.get('/v1/admin/mail/accounts'),
    ])
    items.value = mailRes.data.items ?? []
    accounts.value = acctRes.data.items ?? []
    if (!compose.to) compose.to = mailRes.data.defaultTo ?? ''
    if (!compose.accountId) compose.accountId = accounts.value[0]?.mailAccountId ?? ''
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to load mail'
  } finally { loading.value = false }
}
async function send() {
  if (compose.busy) return
  compose.busy = true; compose.error = ''
  try {
    await api.post('/v1/admin/mail/send', {
      accountId: compose.accountId, to: compose.to.trim(),
      subject: compose.subject.trim(), body: compose.body,
    })
    compose.subject = ''; compose.body = ''
    composeOpen.value = false
    sentOk.value = true
    setTimeout(() => { sentOk.value = false }, 2500)
    await load()
  } catch (e) { compose.error = e.response?.data?.error ?? e.message ?? 'Send failed' }
  finally { compose.busy = false }
}

watch(() => props.entityId, load)
onMounted(load)
</script>

<style scoped>
.emp { padding: .2rem 0; }
.emp-head { display: flex; align-items: center; gap: .6rem; margin-bottom: .5rem; }
.emp-btn { padding: .32rem .65rem; border: 1px solid #cfd7e3; background: #fff; border-radius: 6px; font-size: .8rem; cursor: pointer; }
.emp-primary { background: #0b2e59; color: #fff; border-color: #0b2e59; }
.emp-err { color: #b42318; font-size: .78rem; }
.emp-ok { color: #1c7a4a; font-size: .8rem; font-weight: 700; }
.emp-muted { color: #5f6e85; font-size: .8rem; }
.emp-compose { background: #f6f9fd; border: 1px solid #e0e6ee; border-radius: 8px; padding: .6rem .8rem; margin-bottom: .6rem; }
.emp-f { display: flex; align-items: center; gap: .5rem; margin-bottom: .4rem; }
.emp-f label { width: 60px; font-size: .72rem; font-weight: 700; color: #5f6e85; text-transform: uppercase; }
.emp-f input, .emp-f select { flex: 1; padding: .35rem .55rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .82rem; }
.emp-body { width: 100%; border: 1px solid #cfd7e3; border-radius: 6px; padding: .5rem; font-size: .84rem; font-family: inherit; }
.emp-actions { display: flex; gap: .45rem; align-items: center; margin-top: .4rem; }
.emp-row { background: #fff; border: 1px solid #e8edf3; border-radius: 7px; padding: .45rem .65rem; margin-bottom: .35rem; cursor: pointer; }
.emp-row:hover { border-color: #a0b8d0; }
.emp-row-top { display: flex; align-items: center; gap: .45rem; }
.emp-acct { font-size: .64rem; border-radius: 8px; padding: 0 7px; color: #0b2e59; font-weight: 700; }
.emp-from { font-size: .8rem; color: #16324f; flex: 1; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.emp-date { font-size: .68rem; color: #8a93a4; }
.emp-subject { font-size: .8rem; color: #445; margin-top: .15rem; }
.emp-bodytext { white-space: pre-wrap; font-family: inherit; font-size: .82rem; background: #fbfcfe; border-top: 1px dashed #e6ebf2; margin: .4rem 0 0; padding: .5rem 0 0; }
</style>
