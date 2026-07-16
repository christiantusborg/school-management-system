<template>
  <div class="es-wrap">
    <h2>Email (outbound mail)</h2>
    <p class="es-intro">
      Student-facing mail is sent through these transports. Each school can have its own
      completely separate SMTP or Gmail service; schools set to "Default" use the default
      configuration. If a school's own service fails, sending falls back to the default.
    </p>

    <div class="es-field">
      <label>Configure for</label>
      <select v-model="scope" @change="load">
        <option value="">Default (all schools)</option>
        <option v-for="s in schools" :key="s.schoolId" :value="s.schoolId">{{ s.name }}</option>
      </select>
    </div>

    <div v-if="loading" class="muted">Loading…</div>
    <div v-else-if="loadError" class="es-err">{{ loadError }}</div>
    <template v-else>
      <div class="es-field">
        <label>Transport</label>
        <select v-model="form.provider">
          <option v-if="scope" value="Default">Default (use the default settings)</option>
          <option v-else value="Brevo">Built-in SMTP (Brevo)</option>
          <option value="Gmail">Gmail (Google Workspace service account)</option>
          <option value="Smtp">Custom SMTP (e.g. SiteGround)</option>
        </select>
      </div>

      <template v-if="!scope || form.provider !== 'Default'">
        <div class="es-field">
          <label>From address</label>
          <input v-model="form.fromEmail" type="email" placeholder="admissions@ibss.edu.eu" />
        </div>
        <div class="es-field">
          <label>From name</label>
          <input v-model="form.fromName" type="text" placeholder="Admission Team" />
        </div>
      </template>

      <template v-if="form.provider === 'Gmail'">
        <div class="es-divider">Gmail service account</div>
        <div class="es-field">
          <label>Impersonated user</label>
          <input v-model="form.gmailImpersonatedUser" type="email" placeholder="admissions@ibss.edu.eu" />
        </div>
        <div class="es-field es-field-col">
          <label>
            Service-account JSON key
            <span v-if="hasServiceAccount" class="es-ok-tag">✓ configured</span>
          </label>
          <textarea v-model="form.gmailServiceAccountJson" rows="7"
            :placeholder="hasServiceAccount
              ? 'A key is already saved. Paste a new JSON key here only to replace it.'
              : 'Paste the full service-account JSON key here.'"></textarea>
          <p class="es-hint">
            Stored encrypted. The service account needs domain-wide delegation for
            <code>https://www.googleapis.com/auth/gmail.send</code>, impersonating the user above.
          </p>
        </div>
      </template>

      <template v-if="form.provider === 'Smtp'">
        <div class="es-divider">SMTP server</div>
        <div class="es-field">
          <label>Host</label>
          <input v-model="form.smtpHost" type="text" placeholder="mail.yourdomain.com (SiteGround)" />
        </div>
        <div class="es-field">
          <label>Port</label>
          <input v-model.number="form.smtpPort" type="number" placeholder="465" style="max-width:120px;" />
          <label style="min-width:70px;">Security</label>
          <select v-model="form.smtpSecurity">
            <option value="Auto">Auto</option>
            <option value="SslOnConnect">SSL/TLS (465)</option>
            <option value="StartTls">STARTTLS (587)</option>
            <option value="None">None</option>
          </select>
        </div>
        <div class="es-field">
          <label>Username</label>
          <input v-model="form.smtpUsername" type="text" placeholder="admissions@ibss.edu.eu" />
        </div>
        <div class="es-field">
          <label>Password <span v-if="hasSmtpPassword" class="es-ok-tag">✓ set</span></label>
          <input v-model="form.smtpPassword" type="password"
            :placeholder="hasSmtpPassword ? 'Saved — type to replace' : 'SMTP password'" />
        </div>
        <p class="es-hint">SiteGround mailboxes use SSL/TLS on port 465 or STARTTLS on 587. Stored encrypted.</p>
      </template>

      <div class="es-actions">
        <button class="btn-primary" :disabled="saving" @click="save">{{ saving ? 'Saving…' : 'Save settings' }}</button>
        <span v-if="saveOk" class="es-ok-tag">Saved</span>
        <span v-if="saveError" class="es-err-inline">{{ saveError }}</span>
      </div>

      <div class="es-divider">Send a test email</div>
      <div class="es-field">
        <label>To</label>
        <input v-model="testTo" type="email" placeholder="you@example.com" />
        <button class="btn-ghost" :disabled="testing || !testTo" @click="sendTest">{{ testing ? 'Sending…' : 'Send test' }}</button>
      </div>
      <p v-if="testResult" :class="testOk ? 'es-ok-tag' : 'es-err-inline'">{{ testResult }}</p>
      <p class="es-hint">
        The test uses the <strong>saved</strong> settings, save first, then test.
        <template v-if="scope"> The test goes strictly through this school's own service (no fallback).</template>
      </p>
    </template>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import apiClient from '../../api/client.js'

const loading = ref(true)
const loadError = ref('')
const saving = ref(false)
const saveOk = ref(false)
const saveError = ref('')
const hasServiceAccount = ref(false)
const hasSmtpPassword = ref(false)
const testTo = ref('')
const testing = ref(false)
const testResult = ref('')
const testOk = ref(false)

// '' = the default (global) settings; a schoolId = that school's own settings.
const scope = ref('')
const schools = ref([])

function settingsUrl() {
  return scope.value ? `/v1/admin/schools/${scope.value}/mail-settings` : '/v1/admin/mail-settings'
}

async function loadSchools() {
  try {
    const { data } = await apiClient.get('/v1/school/schools/options')
    schools.value = data.items ?? []
  } catch { /* selector just shows Default */ }
}

const form = reactive({
  provider: 'Brevo',
  fromEmail: '',
  fromName: '',
  gmailImpersonatedUser: '',
  gmailServiceAccountJson: '',
  smtpHost: '',
  smtpPort: 465,
  smtpUsername: '',
  smtpPassword: '',
  smtpSecurity: 'Auto',
})

async function load() {
  loading.value = true
  loadError.value = ''
  testResult.value = ''
  saveError.value = ''
  try {
    const { data } = await apiClient.get(settingsUrl())
    form.provider = data.provider ?? (scope.value ? 'Default' : 'Brevo')
    form.fromEmail = data.fromEmail ?? ''
    form.fromName = data.fromName ?? ''
    form.gmailImpersonatedUser = data.gmailImpersonatedUser ?? ''
    form.gmailServiceAccountJson = ''
    hasServiceAccount.value = !!data.hasServiceAccount
    form.smtpHost = data.smtpHost ?? ''
    form.smtpPort = data.smtpPort ?? 465
    form.smtpUsername = data.smtpUsername ?? ''
    form.smtpSecurity = data.smtpSecurity ?? 'Auto'
    form.smtpPassword = ''
    hasSmtpPassword.value = !!data.hasSmtpPassword
  } catch (err) {
    loadError.value = err.response?.data?.error ?? err.message ?? 'Failed to load'
  } finally {
    loading.value = false
  }
}

async function save() {
  saving.value = true
  saveOk.value = false
  saveError.value = ''
  try {
    const payload = {
      provider: form.provider,
      fromEmail: form.fromEmail,
      fromName: form.fromName,
      gmailImpersonatedUser: form.gmailImpersonatedUser,
      smtpHost: form.smtpHost,
      smtpPort: form.smtpPort,
      smtpUsername: form.smtpUsername,
      smtpSecurity: form.smtpSecurity,
    }
    // Only send secrets when the admin typed a new one.
    if (form.gmailServiceAccountJson.trim()) payload.gmailServiceAccountJson = form.gmailServiceAccountJson.trim()
    if (form.smtpPassword) payload.smtpPassword = form.smtpPassword
    const { data } = await apiClient.put(settingsUrl(), payload)
    hasServiceAccount.value = !!data.hasServiceAccount
    hasSmtpPassword.value = !!data.hasSmtpPassword
    form.gmailServiceAccountJson = ''
    form.smtpPassword = ''
    saveOk.value = true
    setTimeout(() => { saveOk.value = false }, 2500)
  } catch (err) {
    saveError.value = err.response?.data?.error ?? err.message ?? 'Save failed'
  } finally {
    saving.value = false
  }
}

async function sendTest() {
  testing.value = true
  testResult.value = ''
  try {
    const { data } = await apiClient.post(`${settingsUrl()}/test`, { to: testTo.value.trim() })
    testOk.value = true
    testResult.value = `Sent to ${data.to}. Check the inbox.`
  } catch (err) {
    testOk.value = false
    testResult.value = err.response?.data?.error ?? err.message ?? 'Test failed'
  } finally {
    testing.value = false
  }
}

onMounted(() => { loadSchools(); load() })
</script>

<style scoped>
.es-wrap { background: #fff; border: 1px solid #e6ebf2; border-radius: 9px; padding: 1.3rem 1.5rem; max-width: 720px; }
.es-wrap h2 { margin: 0 0 .3rem; font-size: 1.05rem; color: #1a2d4f; }
.es-intro { font-size: .82rem; color: #6b7888; margin: 0 0 1rem; }
.es-divider { font-size: .72rem; font-weight: 700; text-transform: uppercase; letter-spacing: .05em; color: #6b7888; border-top: 1px solid #eef2f7; padding-top: .9rem; margin: 1.1rem 0 .7rem; }
.es-field { display: flex; align-items: center; gap: .7rem; margin-bottom: .7rem; }
.es-field-col { flex-direction: column; align-items: stretch; }
.es-field > label { font-size: .82rem; font-weight: 600; color: #44506a; min-width: 150px; }
.es-field-col > label { margin-bottom: .3rem; }
.es-field input, .es-field select, .es-field textarea { flex: 1; padding: .45rem .6rem; border: 1px solid #d8dde5; border-radius: 6px; font-size: .85rem; font-family: inherit; }
.es-field textarea { width: 100%; font-family: ui-monospace, monospace; font-size: .76rem; }
.es-hint { font-size: .76rem; color: #6b7888; margin: .35rem 0 0; }
.es-hint code { background: #f1f5f9; padding: 0 .25rem; border-radius: 3px; }
.es-actions { display: flex; align-items: center; gap: .8rem; margin-top: .5rem; }
.btn-primary { border: 1px solid #1a4d8c; background: #1a4d8c; color: #fff; border-radius: 6px; padding: .5rem 1.1rem; cursor: pointer; font-weight: 600; }
.btn-primary:disabled { opacity: .6; cursor: not-allowed; }
.btn-ghost { border: 1px solid #d8dde5; background: #fff; border-radius: 6px; padding: .45rem .9rem; cursor: pointer; }
.es-ok-tag { color: #1c7a4a; font-size: .82rem; font-weight: 600; }
.es-err, .es-err-inline { color: #b42318; font-size: .85rem; }
.es-err { background: #fff0ef; border: 1px solid #f3c0bb; padding: .5rem .7rem; border-radius: 6px; }
.muted { color: #6b7888; }
</style>
