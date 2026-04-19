<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useNotificationStore } from '@/stores/notification'
import { mfaApi } from '@/api/mfaApi'
import { startAuthentication } from '@simplewebauthn/browser'

const auth = useAuthStore()
const notify = useNotificationStore()
const router = useRouter()

const mode = ref<'opaque' | 'recovery'>('opaque')
const username = ref('')
const password = ref('')
const recoveryCode = ref('')
const loading = ref(false)
const error = ref('')

// MFA state
const mfaMethod = ref('')
const mfaCode = ref('')
const mfaCodeSent = ref(false)
const mfaLoading = ref(false)

async function handleLogin() {
  loading.value = true
  error.value = ''
  try {
    const result = await auth.login(username.value, password.value)
    if (result.requiresMfa) {
      if (auth.mfaAvailableMethods.length > 0) {
        mfaMethod.value = auth.mfaAvailableMethods[0]
      }
    } else if (result.success) {
      notify.success('Logged in successfully')
      router.push('/profile')
    } else {
      error.value = result.message || 'Login failed'
    }
  } catch {
    error.value = 'Invalid username or password'
  } finally {
    loading.value = false
  }
}

async function handleRecoveryLogin() {
  loading.value = true
  error.value = ''
  try {
    const result = await auth.loginWithRecoveryCode(username.value, recoveryCode.value)
    if (result.success) {
      notify.success('Logged in with recovery code')
      router.push('/profile')
    } else {
      error.value = result.message || 'Login failed'
    }
  } catch {
    error.value = 'Invalid username or recovery code'
  } finally {
    loading.value = false
  }
}

async function sendOtp() {
  if (!auth.mfaPendingId) return
  mfaLoading.value = true
  error.value = ''
  try {
    const pendingId = auth.mfaPendingId
    if (mfaMethod.value === 'email') {
      await mfaApi.emailSend({ pendingId })
    } else {
      await mfaApi.smsSend({ pendingId })
    }
    mfaCodeSent.value = true
    notify.success('Code sent — check server logs')
  } catch {
    error.value = 'Failed to send code'
  } finally {
    mfaLoading.value = false
  }
}

async function handleMfa() {
  if (!auth.mfaPendingId) return
  mfaLoading.value = true
  error.value = ''
  try {
    const pendingId = auth.mfaPendingId
    let res
    if (mfaMethod.value === 'totp') {
      res = await mfaApi.totpVerify({ pendingId, code: mfaCode.value })
    } else if (mfaMethod.value === 'email') {
      res = await mfaApi.emailVerify({ pendingId, code: mfaCode.value })
    } else if (mfaMethod.value === 'sms') {
      res = await mfaApi.smsVerify({ pendingId, code: mfaCode.value })
    } else {
      return
    }
    if (res.data.success && res.data.data) {
      await auth.completeMfa(res.data.data)
      notify.success('Logged in successfully')
      router.push('/profile')
    } else {
      error.value = res.data.message || 'Invalid code'
    }
  } catch {
    error.value = 'Verification failed'
  } finally {
    mfaLoading.value = false
  }
}

async function handleFido2() {
  if (!auth.mfaPendingId) return
  mfaLoading.value = true
  error.value = ''
  try {
    const pendingId = auth.mfaPendingId
    const initRes = await mfaApi.fido2AssertionInit({ pendingId })
    if (!initRes.data.success || !initRes.data.data) {
      error.value = initRes.data.message || 'Failed to initialize security key'
      return
    }
    const optionsJson = JSON.parse(initRes.data.data)
    const assertionResponse = await startAuthentication({ optionsJSON: optionsJson })
    const finishRes = await mfaApi.fido2AssertionFinish({ pendingId, assertionResponse })
    if (finishRes.data.success && finishRes.data.data) {
      await auth.completeMfa(finishRes.data.data)
      notify.success('Logged in successfully')
      router.push('/profile')
    } else {
      error.value = finishRes.data.message || 'Security key verification failed'
    }
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Security key verification failed'
  } finally {
    mfaLoading.value = false
  }
}

function switchMode(m: 'opaque' | 'recovery') {
  mode.value = m
  error.value = ''
}
</script>

<template>
  <!-- MFA step -->
  <form v-if="auth.mfaPendingId" @submit.prevent="handleMfa" autocomplete="off">
    <h2 class="form-title">Two-Factor Authentication</h2>
    <p class="muted">Choose a verification method.</p>

    <div v-if="error" class="error-banner">{{ error }}</div>

    <div class="method-picker">
      <button
        v-for="m in auth.mfaAvailableMethods"
        :key="m"
        type="button"
        class="method-btn"
        :class="{ active: mfaMethod === m }"
        @click="mfaMethod = m; mfaCodeSent = false; mfaCode = ''"
      >
        {{ m === 'totp' ? 'Authenticator App' : m === 'email' ? 'Email Code' : m === 'sms' ? 'SMS Code' : 'Security Key' }}
      </button>
    </div>

    <template v-if="mfaMethod === 'totp'">
      <label for="mfa-totp-code">6-digit code</label>
      <input id="mfa-totp-code" v-model="mfaCode" type="text" inputmode="numeric" maxlength="6" placeholder="000000" required />
      <button class="btn" type="submit" :disabled="mfaLoading">
        {{ mfaLoading ? 'Verifying…' : 'Verify' }}
      </button>
    </template>

    <template v-else-if="mfaMethod === 'email' || mfaMethod === 'sms'">
      <template v-if="!mfaCodeSent">
        <button class="btn" type="button" :disabled="mfaLoading" @click="sendOtp">
          {{ mfaLoading ? 'Sending…' : 'Send Code' }}
        </button>
      </template>
      <template v-else>
        <label for="mfa-otp-code">Enter the code</label>
        <input id="mfa-otp-code" v-model="mfaCode" type="text" inputmode="numeric" maxlength="6" placeholder="000000" required />
        <button class="btn" type="submit" :disabled="mfaLoading">
          {{ mfaLoading ? 'Verifying…' : 'Verify' }}
        </button>
      </template>
    </template>

    <template v-else-if="mfaMethod === 'fido2'">
      <button class="btn" type="button" :disabled="mfaLoading" @click="handleFido2">
        {{ mfaLoading ? 'Waiting for key…' : 'Use Security Key' }}
      </button>
    </template>
  </form>

  <!-- Normal login step -->
  <form v-else @submit.prevent="mode === 'opaque' ? handleLogin() : handleRecoveryLogin()" autocomplete="off">
    <h2 class="form-title">Sign in</h2>
    <p class="muted">{{ mode === 'opaque' ? 'Welcome back.' : 'Use one of your saved recovery codes.' }}</p>

    <div v-if="error" class="error-banner">{{ error }}</div>

    <label for="login-username">Username</label>
    <input id="login-username" v-model="username" type="text" placeholder="Enter username" required />

    <template v-if="mode === 'opaque'">
      <label for="login-pwd">Password</label>
      <input id="login-pwd" v-model="password" type="password" placeholder="Enter password" required />
    </template>

    <template v-else>
      <label for="login-recovery">Recovery Code</label>
      <input id="login-recovery" v-model="recoveryCode" type="text" placeholder="Paste your recovery code" required />
    </template>

    <button class="btn" type="submit" :disabled="loading">
      {{ loading ? 'Signing in…' : 'Sign in' }}
    </button>

    <p class="switch-link">
      <a href="#" @click.prevent="switchMode(mode === 'opaque' ? 'recovery' : 'opaque')">
        {{ mode === 'opaque' ? 'Use a recovery code instead' : 'Use password instead' }}
      </a>
    </p>

    <p class="switch-link">
      Don't have an account? <RouterLink to="/auth/register">Register</RouterLink>
    </p>
  </form>
</template>

<style scoped>
.form-title {
  margin: 0 0 4px;
  font-size: 20px;
  color: #e9eef7;
}

.muted {
  color: #9db0cc;
  font-size: 13px;
  margin: 0 0 4px;
}

label {
  display: block;
  margin-top: 16px;
  margin-bottom: 6px;
  color: #d5e2ff;
  font-size: 14px;
}

input[type="text"],
input[type="password"] {
  width: 100%;
  padding: 12px 14px;
  border-radius: 10px;
  border: 1px solid #2a3754;
  background: #0f1524;
  color: #e9eef7;
  font-size: 14px;
  outline: none;
  box-sizing: border-box;
  box-shadow: 0 0 0 3px rgba(0, 0, 0, 0.06) inset;
}

input:focus {
  border-color: #4f8cff;
}

.btn {
  display: block;
  width: 100%;
  margin-top: 18px;
  padding: 12px 16px;
  border-radius: 10px;
  border: none;
  background: #4f8cff;
  color: #fff;
  font-weight: 600;
  font-size: 15px;
  cursor: pointer;
  transition: background 0.15s;
}

.btn:hover:not(:disabled) { background: #3b78f5; }
.btn:disabled { opacity: 0.55; cursor: not-allowed; }

.error-banner {
  background: rgba(224, 36, 36, 0.12);
  border: 1px solid #e02424;
  color: #f87171;
  padding: 0.75rem;
  border-radius: 8px;
  margin-top: 12px;
  font-size: 13px;
}

.switch-link {
  text-align: center;
  margin-top: 1.25rem;
  font-size: 13px;
  color: #9db0cc;
}

.switch-link a {
  color: #4f8cff;
  text-decoration: none;
}

.switch-link a:hover {
  text-decoration: underline;
}

.method-picker {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-top: 16px;
}

.method-btn {
  padding: 8px 14px;
  border-radius: 8px;
  border: 1px solid #2a3754;
  background: #0f1524;
  color: #9db0cc;
  font-size: 13px;
  cursor: pointer;
  transition: all 0.15s;
}

.method-btn.active,
.method-btn:hover {
  border-color: #4f8cff;
  color: #4f8cff;
  background: rgba(79, 140, 255, 0.08);
}
</style>
