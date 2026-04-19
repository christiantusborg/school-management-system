<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useNotificationStore } from '@/stores/notification'
import { generatePassphrase } from '@/crypto/passphrase'
import { zxcvbn, zxcvbnOptions } from '@zxcvbn-ts/core'
import { dictionary as commonDictionary, adjacencyGraphs } from '@zxcvbn-ts/language-common'
import { dictionary as enDictionary } from '@zxcvbn-ts/language-en'

zxcvbnOptions.setOptions({
  graphs: adjacencyGraphs,
  dictionary: { ...commonDictionary, ...enDictionary }
})

const auth = useAuthStore()
const notify = useNotificationStore()
const router = useRouter()
const route = useRoute()

const username = ref('')
const email = ref('')
const password = ref('')
const confirmPassword = ref('')
const inviteCode = ref('')
const resetToken = ref('')
const loading = ref(false)
const error = ref('')
const recoveryCodes = ref<string[]>([])
const showCodes = ref(false)

const isResetFlow = computed(() => !!resetToken.value)
const generatedPhrase = ref('')
const copied = ref(false)

onMounted(() => {
  if (route.query.resetToken) {
    resetToken.value = route.query.resetToken as string
  }
})

// Offline cracking rate for OPAQUE's Ristretto255 OPRF (elliptic curve scalar mult).
// MD5 clusters reach ~1e11-1e15/sec; Ristretto255 is ~1e5-1e6/sec per GPU.
// 10-year projection for a 100-GPU cluster: ~1e9/sec.
const GUESSES_PER_SEC = 1e9

// Password strength via zxcvbn (dictionary + pattern aware)
// zxcvbn scores 0-4; we map to the same 5 visual buckets.
const SCORE_BUCKETS = [
  { name: 'Unsafe',     color: '#e02424', width: 15,  cls: 'unsafe' },
  { name: 'Vulnerable', color: '#ef6c00', width: 35,  cls: 'vuln'   },
  { name: 'Decent',     color: '#f2c037', width: 60,  cls: 'decent' },
  { name: 'Robust',     color: '#1e88e5', width: 80,  cls: 'robust' },
  { name: 'Green',      color: '#2e7d32', width: 100, cls: 'green'  },
]

function humanTime(seconds: number): string {
  if (!isFinite(seconds) || seconds <= 0) return 'instantly'
  const units: [string, number][] = [
    ['sec', 60], ['min', 60], ['h', 24], ['days', 7],
    ['weeks', 4.34524], ['months', 12], ['years', 1000],
    ['k years', 1000], ['m years', 1000], ['bn years', 1000], ['tn years', 1e99]
  ]
  let val = seconds, i = 0
  while (i < units.length - 1 && val >= units[i][1]) { val /= units[i][1]; i++ }
  return (val < 10 ? val.toFixed(1) : Math.round(val)) + ' ' + units[i][0]
}

const strengthResult = computed(() => {
  if (!password.value) return null
  const result = zxcvbn(password.value)
  // Convert guess count to seconds at OPAQUE-specific cracking rate
  const seconds = Math.pow(10, result.guessesLog10) / GUESSES_PER_SEC
  const day = 86400, year = 31557600
  let bucket
  if      (seconds < day)          bucket = SCORE_BUCKETS[0] // Unsafe
  else if (seconds < year)         bucket = SCORE_BUCKETS[1] // Vulnerable
  else if (seconds < 1000 * year)  bucket = SCORE_BUCKETS[2] // Decent
  else if (seconds < 10000 * year) bucket = SCORE_BUCKETS[3] // Robust
  else                             bucket = SCORE_BUCKETS[4] // Green
  return { bucket, seconds }
})

const strength = computed(() => strengthResult.value?.bucket ?? null)

const strengthEstimate = computed(() => {
  if (!strengthResult.value) return '—'
  return '≈ ' + humanTime(strengthResult.value.seconds)
})

function handleGeneratePassphrase() {
  const phrase = generatePassphrase()
  password.value = phrase
  confirmPassword.value = phrase
  generatedPhrase.value = phrase
  copied.value = false
}

async function copyPhrase() {
  await navigator.clipboard.writeText(generatedPhrase.value)
  copied.value = true
  setTimeout(() => { copied.value = false }, 2000)
}

async function handleSubmit() {
  if (password.value !== confirmPassword.value) {
    error.value = 'Passwords do not match'
    return
  }

  loading.value = true
  error.value = ''
  try {
    if (isResetFlow.value) {
      const result = await auth.resetPassword(resetToken.value, password.value)
      if (result.success) {
        recoveryCodes.value = result.recoveryCodes ?? []
        showCodes.value = true
        notify.success('Password reset successfully')
      } else {
        error.value = result.message || 'Password reset failed'
      }
    } else {
      const result = await auth.register(
        username.value,
        email.value,
        password.value,
        inviteCode.value || undefined
      )
      if (result.success) {
        recoveryCodes.value = result.recoveryCodes ?? []
        showCodes.value = true
        notify.success('Account created successfully')
      } else {
        error.value = result.message || 'Registration failed'
      }
    }
  } catch (e: unknown) {
    const err = e as { response?: { data?: { message?: string } } }
    error.value = err.response?.data?.message || 'Operation failed'
  } finally {
    loading.value = false
  }
}

function proceed() {
  router.push('/profile')
}
</script>

<template>
  <!-- Recovery codes screen after registration -->
  <div v-if="showCodes" class="recovery-codes-display">
    <h2>Recovery Codes</h2>
    <p class="warning">Save all of these somewhere safe. You won't see them again!</p>

    <h3 class="section-label">Recovery Codes</h3>
    <div class="codes-grid">
      <code v-for="code in recoveryCodes" :key="code">{{ code }}</code>
    </div>

    <button class="btn" @click="proceed">I've saved my codes</button>
  </div>

  <!-- Registration / reset form -->
  <form v-else @submit.prevent="handleSubmit" autocomplete="off">
    <h2 class="form-title">{{ isResetFlow ? 'Reset Password' : 'Create account' }}</h2>
    <p v-if="isResetFlow" class="muted">Choose a new password. A new set of recovery codes will be issued.</p>

    <div v-if="error" class="error-banner">{{ error }}</div>

    <div v-if="isResetFlow" class="reset-notice">
      Re-registering with admin reset token.
    </div>

    <template v-if="!isResetFlow">
      <label for="reg-username">Username</label>
      <input id="reg-username" v-model="username" type="text" placeholder="Choose a username" required />

      <label for="reg-email">Email <span class="muted-inline">(stored privately on server)</span></label>
      <input id="reg-email" v-model="email" type="email" placeholder="you@example.com" required />
    </template>

    <div class="row">
      <div>
        <label for="reg-pwd">Password</label>
        <input id="reg-pwd" v-model="password" type="password" placeholder="Enter a long passphrase…" required />
      </div>
      <div>
        <label for="reg-pwd2">Confirm password</label>
        <input id="reg-pwd2" v-model="confirmPassword" type="password" placeholder="Re-enter password" required />
      </div>
    </div>

    <button type="button" class="btn-generate" @click="handleGeneratePassphrase">
      Generate passphrase
    </button>

    <div v-if="generatedPhrase" class="phrase-display">
      <span class="phrase-text">{{ generatedPhrase }}</span>
      <button type="button" class="btn-copy" @click="copyPhrase">
        {{ copied ? 'Copied!' : 'Copy' }}
      </button>
    </div>

    <!-- Strength meter -->
    <div class="meter" aria-hidden="true">
      <div
        class="bar"
        :style="strength ? { width: strength.width + '%', background: strength.color } : { width: '0%' }"
      />
    </div>
    <div class="legend">
      <span class="tag" :class="strength?.cls ?? 'unsafe'">{{ strength?.name ?? 'Unsafe' }}</span>
      <span class="est">{{ strengthEstimate }}</span>
    </div>

    <div class="hint">
      <details>
        <summary><strong>How to reach <span style="color:#2e7d32">green</span> (hard to crack)</strong></summary>
        <p class="hint-note">Strength is estimated against offline OPAQUE cracking (~1 billion Ristretto255 ops/sec, 10-year projection). Dictionary attacks and common patterns are penalised.</p>
        <ul>
          <li><strong>Length first:</strong> aim for <b>14+ characters</b> (16+ is better).</li>
          <li><strong>Randomness beats rules:</strong> predictable substitutions (<code>@</code> for <code>a</code>, <code>3</code> for <code>e</code>, <code>0</code> for <code>o</code>) are in every cracker's rulebook and gain you nothing. Pick words or characters that aren't connected to you.</li>
          <li><strong>Use a passphrase:</strong> 4–5 <em>randomly chosen</em> uncommon words + separators (e.g. "maple-orbit-satin-harbor-42").</li>
          <li><strong>Avoid patterns:</strong> no keyboard runs, dates, names, or anything guessable about you.</li>
          <li><strong>Unique per site:</strong> never reuse; store with a password manager.</li>
        </ul>
      </details>
    </div>

    <label v-if="!isResetFlow" for="reg-invite">Invite Code <span class="muted-inline">(optional)</span></label>
    <input v-if="!isResetFlow" id="reg-invite" v-model="inviteCode" type="text" placeholder="Enter invite code" />

    <button class="btn" type="submit" :disabled="loading">
      {{ loading ? 'Processing…' : (isResetFlow ? 'Reset Password' : 'Create account') }}
    </button>

    <p v-if="!isResetFlow" class="switch-link">
      Already have an account? <RouterLink to="/auth/login">Sign In</RouterLink>
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

.muted-inline {
  color: #9db0cc;
  font-size: 12px;
  font-weight: normal;
}

label {
  display: block;
  margin-top: 16px;
  margin-bottom: 6px;
  color: #d5e2ff;
  font-size: 14px;
}

input[type="text"],
input[type="email"],
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

.row {
  display: grid;
  gap: 14px;
  grid-template-columns: 1fr 1fr;
}

.meter {
  margin-top: 10px;
  height: 14px;
  border-radius: 999px;
  background: #1a2336;
  position: relative;
  overflow: hidden;
}

.bar {
  height: 100%;
  width: 0%;
  background: #e02424;
  transition: width 0.25s ease, background 0.25s ease;
}

.legend {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: 8px;
  font-size: 13px;
  color: #b7c6e3;
}

.tag {
  padding: 0.2rem 0.5rem;
  border-radius: 999px;
  background: #0f1524;
  border: 1px solid #27324a;
  font-size: 12px;
}

.tag.unsafe  { border-color: #e02424; color: #fff; }
.tag.vuln    { border-color: #ef6c00; color: #fff; }
.tag.decent  { border-color: #f2c037; color: #111; }
.tag.robust  { border-color: #1e88e5; color: #fff; }
.tag.green   { border-color: #2e7d32; color: #fff; }

.est {
  font-variant-numeric: tabular-nums;
}

.hint {
  margin-top: 18px;
  background: #0f1524;
  border: 1px solid #27324a;
  border-radius: 12px;
  padding: 12px 14px;
  font-size: 13px;
  color: #b7c6e3;
}

.hint-note {
  margin: 10px 0 0;
  color: #9db0cc;
  font-size: 12px;
}

.hint ul {
  margin: 8px 0 0;
  padding-left: 18px;
}

.hint li {
  margin-top: 6px;
}

details summary {
  cursor: pointer;
  list-style: none;
}

details summary::-webkit-details-marker {
  display: none;
}

.btn-generate {
  margin-top: 10px;
  padding: 7px 14px;
  border-radius: 8px;
  border: 1px solid #2a3754;
  background: transparent;
  color: #4f8cff;
  font-size: 13px;
  cursor: pointer;
  transition: background 0.15s, border-color 0.15s;
}

.btn-generate:hover {
  background: rgba(79, 140, 255, 0.08);
  border-color: #4f8cff;
}

.phrase-display {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-top: 8px;
  padding: 10px 12px;
  background: #0f1524;
  border: 1px solid #2a3754;
  border-radius: 8px;
}

.phrase-text {
  flex: 1;
  font-family: monospace;
  font-size: 13px;
  color: #b7c6e3;
  word-break: break-all;
}

.btn-copy {
  flex-shrink: 0;
  padding: 4px 12px;
  border-radius: 6px;
  border: 1px solid #2a3754;
  background: transparent;
  color: #4f8cff;
  font-size: 12px;
  cursor: pointer;
  transition: background 0.15s;
}

.btn-copy:hover {
  background: rgba(79, 140, 255, 0.08);
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

.reset-notice {
  background: rgba(242, 192, 55, 0.1);
  border: 1px solid #f2c037;
  color: #f2c037;
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
}

/* Recovery codes display */
.recovery-codes-display {
  text-align: center;
  color: #e9eef7;
}

.recovery-codes-display h2 {
  margin-bottom: 0.75rem;
}

.warning {
  color: #f87171;
  font-weight: 500;
  margin-bottom: 1rem;
  font-size: 14px;
}

.codes-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.5rem;
  margin-bottom: 1.5rem;
}

.codes-grid code {
  background: #0f1524;
  border: 1px solid #2a3754;
  padding: 0.5rem;
  border-radius: 6px;
  font-size: 0.85rem;
  color: #b7c6e3;
}

.section-label {
  font-size: 14px;
  color: #9db0cc;
  font-weight: 600;
  margin: 1.25rem 0 0.5rem;
  text-align: left;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.kem-note {
  font-size: 12px;
  color: #9db0cc;
  margin: 0 0 0.5rem;
  text-align: left;
}

.kem-code-box {
  background: #0f1524;
  border: 1px solid #4f8cff44;
  border-radius: 8px;
  padding: 0.75rem 1rem;
  margin-bottom: 1.5rem;
  word-break: break-all;
}

.kem-code-box code {
  font-size: 0.8rem;
  color: #7eb3ff;
  font-family: monospace;
}
</style>
