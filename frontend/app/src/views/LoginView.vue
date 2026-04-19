<template>
  <div class="login-wrapper">
    <div class="login-card">

      <!-- Logo / Brand -->
      <div class="logo-area">
        <img src="/ibss-logo.jpg" alt="IBSS Logo" class="logo-img" />
        <p class="subtitle">Student &amp; Staff Portal</p>
      </div>

      <!-- ── Step 1: Username + Password ── -->
      <form v-if="!auth.mfaPendingId" @submit.prevent="handleLogin" class="login-form">
        <div class="field">
          <label for="username">Username</label>
          <input
            id="username"
            v-model="username"
            type="text"
            placeholder="Enter your username"
            autocomplete="username"
            :disabled="auth.loading"
            required
          />
        </div>

        <div class="field">
          <label for="password">Password</label>
          <div class="password-wrap">
            <input
              id="password"
              v-model="password"
              :type="showPassword ? 'text' : 'password'"
              placeholder="Enter your password"
              autocomplete="current-password"
              :disabled="auth.loading"
              required
            />
            <button type="button" class="toggle-pw" @click="showPassword = !showPassword" tabindex="-1">
              {{ showPassword ? '🙈' : '👁' }}
            </button>
          </div>
        </div>

        <p v-if="auth.error" class="error-msg">{{ auth.error }}</p>

        <button type="submit" class="btn-login" :disabled="auth.loading">
          <span v-if="auth.loading" class="spinner"></span>
          <span>{{ auth.loading ? 'Authenticating…' : 'Log In' }}</span>
        </button>
      </form>

      <!-- ── Step 2: MFA ── -->
      <div v-else class="mfa-step">
        <div class="mfa-header">
          <div class="mfa-icon">🔐</div>
          <h2>Two-Factor Authentication</h2>
          <p class="mfa-sub">Choose your verification method</p>
        </div>

        <!-- Method selector (tabs) -->
        <div v-if="auth.mfaAvailableMethods.length > 1" class="mfa-tabs">
          <button
            v-for="m in auth.mfaAvailableMethods"
            :key="m"
            :class="['mfa-tab', { active: selectedMethod === m }]"
            type="button"
            @click="selectMethod(m)"
          >{{ methodLabel(m) }}</button>
        </div>

        <!-- TOTP -->
        <form v-if="selectedMethod === 'totp'" @submit.prevent="verifyTotp" class="mfa-form">
          <p class="mfa-hint">Enter the 6-digit code from your authenticator app.</p>
          <input
            v-model="mfaCode"
            type="text"
            inputmode="numeric"
            pattern="[0-9]{6}"
            maxlength="6"
            placeholder="000000"
            class="mfa-code-input"
            autocomplete="one-time-code"
            :disabled="auth.loading"
            autofocus
          />
          <p v-if="auth.error" class="error-msg">{{ auth.error }}</p>
          <button type="submit" class="btn-login" :disabled="auth.loading || mfaCode.length < 6">
            <span v-if="auth.loading" class="spinner"></span>
            <span>{{ auth.loading ? 'Verifying…' : 'Verify' }}</span>
          </button>
        </form>

        <!-- Email OTP -->
        <form v-else-if="selectedMethod === 'email'" @submit.prevent="verifyEmail" class="mfa-form">
          <p class="mfa-hint">
            {{ emailSent ? 'A code has been sent to your email address.' : 'Click below to receive a code by email.' }}
          </p>
          <button
            v-if="!emailSent"
            type="button"
            class="btn-send-code"
            :disabled="auth.loading"
            @click="sendEmail"
          >
            <span v-if="auth.loading" class="spinner"></span>
            <span>{{ auth.loading ? 'Sending…' : 'Send Code' }}</span>
          </button>
          <template v-else>
            <input
              v-model="mfaCode"
              type="text"
              inputmode="numeric"
              maxlength="8"
              placeholder="Enter code"
              class="mfa-code-input"
              autocomplete="one-time-code"
              :disabled="auth.loading"
              autofocus
            />
            <button type="button" class="btn-resend" @click="sendEmail" :disabled="auth.loading">
              Resend code
            </button>
            <p v-if="auth.error" class="error-msg">{{ auth.error }}</p>
            <button type="submit" class="btn-login" :disabled="auth.loading || !mfaCode">
              <span v-if="auth.loading" class="spinner"></span>
              <span>{{ auth.loading ? 'Verifying…' : 'Verify' }}</span>
            </button>
          </template>
        </form>

        <!-- Back link -->
        <button type="button" class="btn-back" @click="cancelMfa">← Back to login</button>
      </div>

      <div class="version-tag">{{ version }}</div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { auth } from '../store/auth.js'

const router  = useRouter()
const username     = ref('')
const password     = ref('')
const showPassword = ref(false)
const mfaCode      = ref('')
const emailSent    = ref(false)
const selectedMethod = ref('')
const version = __APP_VERSION__

onMounted(() => {
  // Auto-select first available MFA method if arriving mid-session
  if (auth.mfaAvailableMethods.length) {
    selectedMethod.value = auth.mfaAvailableMethods[0]
  }
})

async function handleLogin() {
  const result = await auth.login(username.value, password.value)
  if (result === 'mfa') {
    selectedMethod.value = auth.mfaAvailableMethods[0] ?? 'totp'
    if (selectedMethod.value === 'email') sendEmail()
  } else {
    navigate(result)
  }
}

async function verifyTotp() {
  const result = await auth.verifyMfaTotp(mfaCode.value)
  navigate(result)
}

async function sendEmail() {
  emailSent.value = false
  await auth.sendMfaEmail()
  emailSent.value = true
  mfaCode.value = ''
}

async function verifyEmail() {
  const result = await auth.verifyMfaEmail(mfaCode.value)
  navigate(result)
}

function selectMethod(m) {
  selectedMethod.value = m
  mfaCode.value = ''
  emailSent.value = false
  auth.error = null
}

function cancelMfa() {
  auth.mfaPendingId = null
  auth.mfaAvailableMethods = []
  auth.error = null
  mfaCode.value = ''
  emailSent.value = false
  password.value = ''
}

function navigate(role) {
  if (!role) return
  if (role === 'employee') router.push('/admin')
  else if (role === 'partner') router.push('/partner')
  else if (role === 'student') router.push('/student')
}

function methodLabel(m) {
  return { totp: 'Authenticator', email: 'Email', sms: 'SMS', fido2: 'Security Key' }[m] ?? m
}
</script>

<style scoped>
.login-wrapper {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #003366 0%, #0055a5 100%);
  padding: 1rem;
}

.login-card {
  background: #fff;
  border-radius: 14px;
  padding: 2.5rem 2.25rem;
  width: 100%;
  max-width: 420px;
  box-shadow: 0 12px 48px rgba(0, 0, 0, 0.25);
}

/* ── Logo area ── */
.logo-area {
  text-align: center;
  margin-bottom: 2rem;
}

.logo-img {
  width: 160px;
  height: auto;
  display: block;
  margin: 0 auto 0.75rem;
}

.subtitle {
  font-size: 0.82rem;
  color: #888;
  margin: 0;
  font-weight: 500;
  letter-spacing: 0.02em;
}

/* ── Form ── */
.login-form,
.mfa-form {
  display: flex;
  flex-direction: column;
  gap: 1.1rem;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

.field label {
  font-size: 0.82rem;
  font-weight: 600;
  color: #444;
}

.field input,
.mfa-code-input {
  padding: 0.68rem 0.9rem;
  border: 1.5px solid #d0d7e0;
  border-radius: 8px;
  font-size: 0.95rem;
  font-family: inherit;
  outline: none;
  transition: border-color 0.15s, box-shadow 0.15s;
  background: #fafbfc;
}

.field input:focus,
.mfa-code-input:focus {
  border-color: #0055a5;
  box-shadow: 0 0 0 3px rgba(0, 85, 165, 0.1);
  background: #fff;
}

.field input:disabled,
.mfa-code-input:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.password-wrap {
  position: relative;
  display: flex;
  align-items: center;
}

.password-wrap input {
  flex: 1;
  padding-right: 2.5rem;
}

.toggle-pw {
  position: absolute;
  right: 0.6rem;
  background: none;
  border: none;
  cursor: pointer;
  font-size: 1rem;
  padding: 0.2rem;
  line-height: 1;
  opacity: 0.5;
}

.toggle-pw:hover { opacity: 0.9; }

/* ── Buttons ── */
.btn-login {
  margin-top: 0.4rem;
  padding: 0.78rem;
  background: #003366;
  color: #fff;
  border: none;
  border-radius: 8px;
  font-size: 0.97rem;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  transition: background 0.15s;
  font-family: inherit;
}

.btn-login:hover:not(:disabled) { background: #0055a5; }
.btn-login:disabled { opacity: 0.65; cursor: not-allowed; }

.btn-send-code {
  padding: 0.68rem;
  background: #e8f0f8;
  color: #003366;
  border: 1.5px solid #b8d0e8;
  border-radius: 8px;
  font-size: 0.9rem;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  font-family: inherit;
  transition: background 0.15s;
}

.btn-send-code:hover:not(:disabled) { background: #d0e4f5; }
.btn-send-code:disabled { opacity: 0.6; cursor: not-allowed; }

.btn-resend {
  background: none;
  border: none;
  color: #0055a5;
  font-size: 0.82rem;
  cursor: pointer;
  padding: 0;
  text-decoration: underline;
  font-family: inherit;
  text-align: left;
}

.btn-back {
  display: block;
  margin-top: 1.25rem;
  background: none;
  border: none;
  color: #888;
  font-size: 0.82rem;
  cursor: pointer;
  padding: 0;
  font-family: inherit;
  width: 100%;
  text-align: center;
}

.btn-back:hover { color: #003366; }

/* ── Spinner ── */
.spinner {
  width: 16px;
  height: 16px;
  border: 2px solid rgba(255,255,255,0.4);
  border-top-color: #fff;
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
  display: inline-block;
  flex-shrink: 0;
}

.btn-send-code .spinner {
  border-color: rgba(0,51,102,0.3);
  border-top-color: #003366;
}

@keyframes spin { to { transform: rotate(360deg); } }

/* ── Error ── */
.error-msg {
  color: #c0392b;
  font-size: 0.83rem;
  margin: 0;
  background: #fef2f2;
  border: 1px solid #fca5a5;
  border-radius: 6px;
  padding: 0.5rem 0.75rem;
}

/* ── MFA step ── */
.mfa-step {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.mfa-header {
  text-align: center;
  margin-bottom: 0.25rem;
}

.mfa-icon {
  font-size: 2rem;
  margin-bottom: 0.5rem;
}

.mfa-header h2 {
  font-size: 1.15rem;
  font-weight: 700;
  color: #003366;
  margin: 0 0 0.2rem;
}

.mfa-sub {
  font-size: 0.82rem;
  color: #888;
  margin: 0;
}

.mfa-tabs {
  display: flex;
  gap: 0.4rem;
  border-bottom: 2px solid #e8edf4;
  padding-bottom: 0;
}

.mfa-tab {
  padding: 0.45rem 0.9rem;
  background: none;
  border: none;
  border-bottom: 2px solid transparent;
  margin-bottom: -2px;
  font-size: 0.85rem;
  font-weight: 600;
  color: #888;
  cursor: pointer;
  font-family: inherit;
  transition: color 0.15s, border-color 0.15s;
}

.mfa-tab.active { color: #003366; border-bottom-color: #003366; }
.mfa-tab:hover:not(.active) { color: #333; }

.mfa-hint {
  font-size: 0.84rem;
  color: #555;
  margin: 0;
  line-height: 1.5;
}

.mfa-code-input {
  font-size: 1.5rem;
  text-align: center;
  letter-spacing: 0.35em;
  font-family: ui-monospace, monospace;
  padding: 0.75rem;
}

/* ── Version ── */
.version-tag {
  margin-top: 1.5rem;
  text-align: center;
  font-size: 0.7rem;
  color: #ccc;
  font-family: ui-monospace, monospace;
}
</style>
