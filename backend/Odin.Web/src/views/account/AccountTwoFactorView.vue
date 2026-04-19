<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { accountMfaApi } from '@/api/accountMfaApi'
import { useNotificationStore } from '@/stores/notification'
import { startRegistration } from '@simplewebauthn/browser'
import QRCode from 'qrcode'
import type { MfaStatusResponse, TotpInitResponse } from '@/types'

const notify = useNotificationStore()

const status = ref<MfaStatusResponse | null>(null)
const loading = ref(false)

// TOTP state
const totpSetup = ref<TotpInitResponse | null>(null)
const totpQrDataUrl = ref('')
const totpCode = ref('')
const totpLoading = ref(false)

// Email enable OTP state
const emailEnableSessionId = ref<string | null>(null)
const emailEnableCode = ref('')

// SMS state
const smsLoading = ref(false)

// SMS enable OTP state
const smsEnableSessionId = ref<string | null>(null)
const smsEnableCode = ref('')

// FIDO2 state
const fido2Label = ref('')
const fido2Loading = ref(false)

async function loadStatus() {
  loading.value = true
  try {
    const res = await accountMfaApi.getStatus()
    if (res.data.success && res.data.data) status.value = res.data.data
  } finally {
    loading.value = false
  }
}

function isEnabled(method: string) {
  return status.value?.enabledMethods.some(m => m.method === method) ?? false
}

// ── TOTP ───────────────────────────────────────────────────────────────────

async function initTotp() {
  totpLoading.value = true
  try {
    const res = await accountMfaApi.totpInit()
    if (res.data.success && res.data.data) {
      totpSetup.value = res.data.data
      totpQrDataUrl.value = await QRCode.toDataURL(res.data.data.qrUri)
    }
  } catch {
    notify.error('Failed to initialize TOTP')
  } finally {
    totpLoading.value = false
  }
}

async function enableTotp() {
  if (!totpSetup.value) return
  totpLoading.value = true
  try {
    const res = await accountMfaApi.totpEnable({ code: totpCode.value })
    if (res.data.success) {
      notify.success('Authenticator app enabled')
      totpSetup.value = null
      totpCode.value = ''
      await loadStatus()
    } else {
      notify.error(res.data.message || 'Invalid code')
    }
  } finally {
    totpLoading.value = false
  }
}

async function disableTotp() {
  totpLoading.value = true
  try {
    const res = await accountMfaApi.totpDelete()
    if (res.data.success) {
      notify.success('Authenticator app disabled')
      await loadStatus()
    } else {
      notify.error(res.data.message || 'Failed to disable TOTP')
    }
  } finally {
    totpLoading.value = false
  }
}

// ── Email ─────────────────────────────────────────────────────────────────

async function enableEmail() {
  try {
    const res = await accountMfaApi.emailEnableInit()
    if (res.data.success && res.data.data) {
      emailEnableSessionId.value = res.data.data.sessionId
      emailEnableCode.value = ''
    } else {
      notify.error(res.data.message || 'Failed to start email enable')
    }
  } catch {
    notify.error('Failed to start email enable')
  }
}

async function confirmEmailEnable() {
  if (!emailEnableSessionId.value) return
  try {
    const res = await accountMfaApi.emailEnableConfirm({ sessionId: emailEnableSessionId.value, code: emailEnableCode.value })
    if (res.data.success) {
      notify.success('Email codes enabled')
      emailEnableSessionId.value = null
      emailEnableCode.value = ''
      await loadStatus()
    } else {
      notify.error(res.data.message || 'Invalid code')
    }
  } catch {
    notify.error('Failed to confirm email enable')
  }
}

async function disableEmail() {
  try {
    const res = await accountMfaApi.emailDelete()
    if (res.data.success) {
      notify.success('Email codes disabled')
      await loadStatus()
    } else {
      notify.error(res.data.message || 'Failed to disable email codes')
    }
  } catch {
    notify.error('Failed to disable email codes')
  }
}

// ── SMS ───────────────────────────────────────────────────────────────────

async function enableSms() {
  smsLoading.value = true
  try {
    const res = await accountMfaApi.smsEnableInit()
    if (res.data.success && res.data.data) {
      smsEnableSessionId.value = res.data.data.sessionId
      smsEnableCode.value = ''
    } else {
      notify.error(res.data.message || 'Failed to start SMS enable')
    }
  } finally {
    smsLoading.value = false
  }
}

async function confirmSmsEnable() {
  if (!smsEnableSessionId.value) return
  smsLoading.value = true
  try {
    const res = await accountMfaApi.smsEnableConfirm({ sessionId: smsEnableSessionId.value, code: smsEnableCode.value })
    if (res.data.success) {
      notify.success('SMS codes enabled')
      smsEnableSessionId.value = null
      smsEnableCode.value = ''
      await loadStatus()
    } else {
      notify.error(res.data.message || 'Invalid code')
    }
  } finally {
    smsLoading.value = false
  }
}

async function disableSms() {
  smsLoading.value = true
  try {
    const res = await accountMfaApi.smsDelete()
    if (res.data.success) {
      notify.success('SMS codes disabled')
      await loadStatus()
    } else {
      notify.error(res.data.message || 'Failed to disable SMS codes')
    }
  } finally {
    smsLoading.value = false
  }
}

// ── FIDO2 ─────────────────────────────────────────────────────────────────

async function addSecurityKey() {
  if (!fido2Label.value.trim()) {
    notify.error('Enter a label for this key')
    return
  }
  fido2Loading.value = true
  try {
    const initRes = await accountMfaApi.fido2RegisterInit()
    if (!initRes.data.success || !initRes.data.data) {
      notify.error(initRes.data.message || 'Failed to initialize key registration')
      return
    }
    const optionsJson = JSON.parse(initRes.data.data.optionsJson)
    const attestationResponse = await startRegistration({ optionsJSON: optionsJson })
    const finishRes = await accountMfaApi.fido2RegisterFinish({
      label: fido2Label.value,
      attestationResponse
    })
    if (finishRes.data.success) {
      notify.success('Security key registered')
      fido2Label.value = ''
      await loadStatus()
    } else {
      notify.error(finishRes.data.message || 'Key registration failed')
    }
  } catch (e: unknown) {
    notify.error(e instanceof Error ? e.message : 'Key registration failed')
  } finally {
    fido2Loading.value = false
  }
}

async function deleteCredential(id: string) {
  fido2Loading.value = true
  try {
    const res = await accountMfaApi.fido2DeleteCredential(id)
    if (res.data.success) {
      notify.success('Security key removed')
      await loadStatus()
    } else {
      notify.error(res.data.message || 'Failed to remove key')
    }
  } finally {
    fido2Loading.value = false
  }
}

onMounted(loadStatus)
</script>

<template>
  <div class="page">
    <h1>Two-Factor Authentication</h1>

    <div v-if="loading" class="loading">Loading…</div>

    <template v-else-if="status">
      <!-- Status summary -->
      <div class="card">
        <h2>Status</h2>
        <p v-if="status.enabledMethods.length === 0" class="muted">No 2FA methods enabled. Your account is protected by password only.</p>
        <ul v-else class="method-list">
          <li v-for="m in status.enabledMethods" :key="m.method">
            <span class="badge">{{ m.method === 'totp' ? 'Authenticator App' : m.method === 'email' ? 'Email Codes' : m.method === 'sms' ? 'SMS Codes' : 'Security Keys' }}</span>
          </li>
        </ul>
      </div>

      <!-- TOTP -->
      <div class="card">
        <h2>Authenticator App (TOTP)</h2>
        <p class="muted">Use an app like Google Authenticator, Authy, or 1Password.</p>

        <template v-if="isEnabled('totp')">
          <button class="btn-danger" :disabled="totpLoading" @click="disableTotp">
            {{ totpLoading ? 'Disabling…' : 'Disable Authenticator App' }}
          </button>
        </template>
        <template v-else-if="totpSetup">
          <div class="qr-container">
            <img v-if="totpQrDataUrl" :src="totpQrDataUrl" alt="TOTP QR Code" class="qr-code" />
          </div>
          <p class="secret-code">Manual entry: <code>{{ totpSetup.secret }}</code></p>
          <label for="totp-code">Verification code</label>
          <input id="totp-code" v-model="totpCode" type="text" inputmode="numeric" maxlength="6" placeholder="000000" />
          <button class="btn-primary" :disabled="totpLoading || !totpCode" @click="enableTotp">
            {{ totpLoading ? 'Enabling…' : 'Enable' }}
          </button>
          <button class="btn-secondary" @click="totpSetup = null">Cancel</button>
        </template>
        <template v-else>
          <button class="btn-primary" :disabled="totpLoading" @click="initTotp">
            {{ totpLoading ? 'Loading…' : 'Enable Authenticator App' }}
          </button>
        </template>
      </div>

      <!-- Email -->
      <div class="card">
        <h2>Email Codes</h2>
        <p class="muted">Receive a one-time code via email when you sign in.</p>
        <template v-if="isEnabled('email')">
          <button class="btn-danger" @click="disableEmail">Disable Email Codes</button>
        </template>
        <template v-else-if="emailEnableSessionId">
          <p class="muted">A verification code was sent to your primary email. Enter it below to confirm.</p>
          <label for="email-enable-code">Verification code</label>
          <input id="email-enable-code" v-model="emailEnableCode" type="text" inputmode="numeric" maxlength="6" placeholder="000000" />
          <button class="btn-primary" :disabled="!emailEnableCode" @click="confirmEmailEnable">Confirm</button>
          <button class="btn-secondary" @click="emailEnableSessionId = null">Cancel</button>
        </template>
        <template v-else>
          <button class="btn-primary" @click="enableEmail">Enable Email Codes</button>
        </template>
      </div>

      <!-- SMS -->
      <div class="card">
        <h2>SMS Codes</h2>
        <p class="muted">Receive a one-time code via SMS when you sign in. (Dummy — logs to server console.) Requires a primary phone number set on your profile.</p>
        <template v-if="isEnabled('sms')">
          <button class="btn-danger" :disabled="smsLoading" @click="disableSms">
            {{ smsLoading ? 'Disabling…' : 'Disable SMS Codes' }}
          </button>
        </template>
        <template v-else-if="smsEnableSessionId">
          <p class="muted">A verification code was sent to your primary phone. Enter it below to confirm.</p>
          <label for="sms-enable-code">Verification code</label>
          <input id="sms-enable-code" v-model="smsEnableCode" type="text" inputmode="numeric" maxlength="6" placeholder="000000" />
          <button class="btn-primary" :disabled="smsLoading || !smsEnableCode" @click="confirmSmsEnable">
            {{ smsLoading ? 'Confirming…' : 'Confirm' }}
          </button>
          <button class="btn-secondary" @click="smsEnableSessionId = null">Cancel</button>
        </template>
        <template v-else>
          <button class="btn-primary" :disabled="smsLoading" @click="enableSms">
            {{ smsLoading ? 'Enabling…' : 'Enable SMS Codes' }}
          </button>
        </template>
      </div>

      <!-- FIDO2 -->
      <div class="card">
        <h2>Security Keys</h2>
        <p class="muted">Use a hardware key (YubiKey, NFC), Touch ID, or Windows Hello.</p>

        <div v-if="status.fido2Credentials.length > 0" class="key-list">
          <div v-for="cred in status.fido2Credentials" :key="cred.fido2CredentialId" class="key-item">
            <div>
              <strong>{{ cred.label }}</strong>
              <span class="key-meta"> — Added {{ new Date(cred.createdAt).toLocaleDateString() }}</span>
              <span v-if="cred.lastUsedAt" class="key-meta">, Last used {{ new Date(cred.lastUsedAt).toLocaleDateString() }}</span>
            </div>
            <button class="btn-danger-sm" :disabled="fido2Loading" @click="deleteCredential(cred.fido2CredentialId)">Remove</button>
          </div>
        </div>
        <p v-else class="muted">No security keys registered.</p>

        <div class="add-key-form">
          <label for="key-label">Key label</label>
          <input id="key-label" v-model="fido2Label" type="text" placeholder="e.g. YubiKey 5" />
          <button class="btn-primary" :disabled="fido2Loading" @click="addSecurityKey">
            {{ fido2Loading ? 'Registering…' : 'Add Security Key' }}
          </button>
        </div>
      </div>
    </template>
  </div>
</template>

<style scoped>
.page { max-width: 640px; }
.page h1 { margin-bottom: 1.5rem; }
.loading { color: #6b7280; }

.card {
  background: white;
  padding: 1.5rem;
  border-radius: 8px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  margin-bottom: 1.5rem;
}

.card h2 { margin-bottom: 0.5rem; font-size: 1.15rem; }
.muted { color: #6b7280; font-size: 0.875rem; margin-bottom: 1rem; }

.method-list {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.badge {
  display: inline-block;
  padding: 4px 10px;
  background: #e0f2fe;
  color: #0369a1;
  border-radius: 99px;
  font-size: 0.8rem;
  font-weight: 500;
}

.qr-container {
  display: flex;
  justify-content: center;
  margin: 1rem 0;
}

.qr-code {
  width: 180px;
  height: 180px;
  border: 4px solid white;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.15);
}

.secret-code {
  font-size: 0.8rem;
  color: #6b7280;
  margin-bottom: 1rem;
  word-break: break-all;
}

.secret-code code {
  background: #f3f4f6;
  padding: 2px 6px;
  border-radius: 4px;
  font-family: monospace;
}

label {
  display: block;
  margin-bottom: 6px;
  font-size: 0.875rem;
  color: #374151;
}

input[type="text"],
input[type="tel"] {
  width: 100%;
  padding: 10px 12px;
  border-radius: 6px;
  border: 1px solid #d1d5db;
  font-size: 0.875rem;
  box-sizing: border-box;
  margin-bottom: 0.75rem;
}

input:focus { outline: none; border-color: #667eea; }

.btn-primary {
  display: inline-block;
  padding: 0.625rem 1.25rem;
  background: #667eea;
  color: white;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  font-size: 0.875rem;
  margin-right: 0.5rem;
}

.btn-primary:hover:not(:disabled) { background: #5a6fd6; }
.btn-primary:disabled { opacity: 0.6; cursor: not-allowed; }

.btn-secondary {
  display: inline-block;
  padding: 0.625rem 1.25rem;
  background: #f3f4f6;
  color: #374151;
  border: 1px solid #d1d5db;
  border-radius: 6px;
  cursor: pointer;
  font-size: 0.875rem;
}

.btn-danger {
  display: inline-block;
  padding: 0.625rem 1.25rem;
  background: #ef4444;
  color: white;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  font-size: 0.875rem;
}

.btn-danger:hover:not(:disabled) { background: #dc2626; }
.btn-danger:disabled { opacity: 0.6; cursor: not-allowed; }

.btn-danger-sm {
  padding: 4px 10px;
  background: #fee2e2;
  color: #dc2626;
  border: 1px solid #fca5a5;
  border-radius: 4px;
  cursor: pointer;
  font-size: 0.8rem;
}

.btn-danger-sm:hover:not(:disabled) { background: #fca5a5; }

.key-list {
  margin-bottom: 1rem;
}

.key-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.625rem 0;
  border-bottom: 1px solid #f3f4f6;
}

.key-meta {
  font-size: 0.8rem;
  color: #9ca3af;
}

.add-key-form {
  margin-top: 1rem;
  padding-top: 1rem;
  border-top: 1px solid #f3f4f6;
}
</style>
