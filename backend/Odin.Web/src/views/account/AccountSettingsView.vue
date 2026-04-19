<script setup lang="ts">
import { ref, computed } from 'vue'
import { accountApi } from '@/api/accountApi'
import { blindPassword, derivePublicKey, deriveKemEncKey, signChallenge } from '@/crypto/opaque'
import { encryptKemPrivateKey, toBase64 } from '@/crypto/kem'
import { generatePassphrase } from '@/crypto/passphrase'
import { zxcvbn, zxcvbnOptions } from '@zxcvbn-ts/core'
import { dictionary as commonDictionary, adjacencyGraphs } from '@zxcvbn-ts/language-common'
import { dictionary as enDictionary } from '@zxcvbn-ts/language-en'
import { useAuthStore } from '@/stores/auth'
import { useNotificationStore } from '@/stores/notification'
import FormInput from '@/components/FormInput.vue'
import ConfirmDialog from '@/components/ConfirmDialog.vue'

zxcvbnOptions.setOptions({ graphs: adjacencyGraphs, dictionary: { ...commonDictionary, ...enDictionary } })

const auth = useAuthStore()
const notify = useNotificationStore()

const currentPassword = ref('')
const newPassword = ref('')
const confirmNewPassword = ref('')
const changingPassword = ref(false)
const showDeleteDialog = ref(false)
const generatedPhrase = ref('')
const copied = ref(false)

const GUESSES_PER_SEC = 1e9
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
  if (!newPassword.value) return null
  const result = zxcvbn(newPassword.value)
  const seconds = Math.pow(10, result.guessesLog10) / GUESSES_PER_SEC
  const day = 86400, year = 31557600
  let bucket
  if      (seconds < day)          bucket = SCORE_BUCKETS[0]
  else if (seconds < year)         bucket = SCORE_BUCKETS[1]
  else if (seconds < 1000 * year)  bucket = SCORE_BUCKETS[2]
  else if (seconds < 10000 * year) bucket = SCORE_BUCKETS[3]
  else                             bucket = SCORE_BUCKETS[4]
  return { bucket, seconds }
})

const strength = computed(() => strengthResult.value?.bucket ?? null)
const strengthEstimate = computed(() => strengthResult.value ? '≈ ' + humanTime(strengthResult.value.seconds) : '—')

function handleGeneratePassphrase() {
  const phrase = generatePassphrase()
  newPassword.value = phrase
  confirmNewPassword.value = phrase
  generatedPhrase.value = phrase
  copied.value = false
}

async function copyPhrase() {
  await navigator.clipboard.writeText(generatedPhrase.value)
  copied.value = true
  setTimeout(() => { copied.value = false }, 2000)
}

async function handleChangePassword() {
  if (!currentPassword.value) {
    notify.error('Enter your current password')
    return
  }
  if (newPassword.value !== confirmNewPassword.value) {
    notify.error('Passwords do not match')
    return
  }

  changingPassword.value = true
  try {
    // Blind both old and new passwords
    const { blind: oldBlind, blindedElement: oldBlindedElement } = blindPassword(currentPassword.value)
    const { blind: newBlind, blindedElement: newBlindedElement } = blindPassword(newPassword.value)

    const initRes = await accountApi.changePasswordInit({
      oldBlindedElement,
      blindedElement: newBlindedElement
    })

    if (!initRes.data.success || !initRes.data.data) {
      notify.error(initRes.data.message || 'Failed to change password')
      return
    }

    const { changeId, oldEvaluatedElement, challenge, evaluatedElement } = initRes.data.data

    // Sign the challenge with the old password's derived Ed25519 key (proves current password)
    const signature = signChallenge(currentPassword.value, oldBlind, oldEvaluatedElement, challenge)

    // Derive new public key from new password
    const clientPublicKey = derivePublicKey(newPassword.value, newBlind, evaluatedElement)

    const finalizeRes = await accountApi.changePasswordFinalize({ changeId, signature, clientPublicKey })

    if (finalizeRes.data.success) {
      // Re-encrypt KEM private key with the new password's OPRF-derived key
      if (auth.kemPrivateKey && auth.kemEncKey) {
        try {
          const newEncKey = deriveKemEncKey(newPassword.value, newBlind, evaluatedElement)
          const pwEnc = await encryptKemPrivateKey(auth.kemPrivateKey, newEncKey)
          await accountApi.saveKemKeyPair({
            publicKey: toBase64(auth.kemPublicKey!),
            encryptedPrivateKey: pwEnc.encryptedKey,
            nonce: pwEnc.nonce
          })
          auth.kemEncKey = newEncKey
        } catch { /* best-effort */ }
      }
      notify.success('Password changed')
      currentPassword.value = ''
      newPassword.value = ''
      confirmNewPassword.value = ''
    } else {
      notify.error(finalizeRes.data.message || 'Current password is incorrect')
    }
  } catch (e: unknown) {
    const msg = (e as { response?: { data?: { message?: string } } })?.response?.data?.message
    notify.error(msg || 'Failed to change password')
  } finally {
    changingPassword.value = false
  }
}

async function handleDeleteAccount() {
  try {
    const res = await accountApi.deleteAccount()
    if (res.data.success) {
      notify.success('Account deleted')
      auth.logout()
    } else {
      notify.error(res.data.message || 'Failed to delete account')
    }
  } catch {
    notify.error('Failed to delete account')
  }
  showDeleteDialog.value = false
}
</script>

<template>
  <div class="page">
    <h1>Account Settings</h1>

    <div class="card">
      <h2>Change Password</h2>
      <p>Your password never leaves your device. It is used to derive cryptographic keys locally.</p>
      <form @submit.prevent="handleChangePassword">
        <FormInput label="Current Password" type="password" v-model="currentPassword" />
        <FormInput label="New Password" type="password" v-model="newPassword" />

        <button type="button" class="btn-generate" @click="handleGeneratePassphrase">Generate passphrase</button>

        <div v-if="generatedPhrase" class="phrase-display">
          <span class="phrase-text">{{ generatedPhrase }}</span>
          <button type="button" class="btn-copy" @click="copyPhrase">{{ copied ? 'Copied!' : 'Copy' }}</button>
        </div>

        <div class="meter">
          <div class="bar" :style="strength ? { width: strength.width + '%', background: strength.color } : { width: '0%' }" />
        </div>
        <div class="legend">
          <span class="tag" :class="strength?.cls ?? 'unsafe'">{{ strength?.name ?? 'Unsafe' }}</span>
          <span class="est">{{ strengthEstimate }}</span>
        </div>

        <FormInput label="Confirm New Password" type="password" v-model="confirmNewPassword" />
        <button type="submit" class="btn-primary" :disabled="changingPassword">
          {{ changingPassword ? 'Changing...' : 'Change Password' }}
        </button>
      </form>
    </div>

    <div class="card">
      <h2>Logout Everywhere</h2>
      <p>Revoke all active sessions on all devices.</p>
      <button class="btn-warning" @click="auth.logoutEverywhere()">Logout Everywhere</button>
    </div>

    <div class="card danger-zone">
      <h2>Delete Account</h2>
      <p>Permanently delete your account and all associated data.</p>
      <button class="btn-danger" @click="showDeleteDialog = true">Delete Account</button>
    </div>

    <ConfirmDialog
      :show="showDeleteDialog"
      title="Delete Account"
      message="This action is permanent. All your data will be lost."
      confirm-text="Delete"
      danger
      @confirm="handleDeleteAccount"
      @cancel="showDeleteDialog = false"
    />
  </div>
</template>

<style scoped>
.page { max-width: 640px; }
.page h1 { margin-bottom: 1.5rem; }

.card {
  background: white;
  padding: 1.5rem;
  border-radius: 8px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  margin-bottom: 1.5rem;
}

.card h2 { margin-bottom: 1rem; font-size: 1.25rem; }
.card p { color: #6b7280; margin-bottom: 1rem; }

.btn-primary {
  padding: 0.625rem 1.25rem;
  background: #667eea;
  color: white;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  margin-top: 0.75rem;
}

.btn-primary:hover { background: #5a6fd6; }
.btn-primary:disabled { opacity: 0.6; cursor: not-allowed; }

.btn-generate {
  margin-top: 0.5rem;
  padding: 6px 14px;
  border-radius: 6px;
  border: 1px solid #d1d5db;
  background: transparent;
  color: #667eea;
  font-size: 13px;
  cursor: pointer;
}
.btn-generate:hover { background: #f5f3ff; }

.phrase-display {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-top: 8px;
  padding: 10px 12px;
  background: #f9fafb;
  border: 1px solid #d1d5db;
  border-radius: 8px;
}
.phrase-text {
  flex: 1;
  font-family: monospace;
  font-size: 13px;
  color: #374151;
  word-break: break-all;
}
.btn-copy {
  flex-shrink: 0;
  padding: 4px 12px;
  border-radius: 6px;
  border: 1px solid #d1d5db;
  background: transparent;
  color: #667eea;
  font-size: 12px;
  cursor: pointer;
}
.btn-copy:hover { background: #f5f3ff; }

.meter {
  margin-top: 10px;
  height: 10px;
  border-radius: 999px;
  background: #e5e7eb;
  overflow: hidden;
}
.bar {
  height: 100%;
  width: 0%;
  transition: width 0.25s ease, background 0.25s ease;
}
.legend {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: 6px;
  font-size: 12px;
  color: #6b7280;
}
.tag {
  padding: 2px 8px;
  border-radius: 999px;
  font-size: 11px;
  border: 1px solid #d1d5db;
}
.tag.unsafe  { border-color: #e02424; color: #e02424; }
.tag.vuln    { border-color: #ef6c00; color: #ef6c00; }
.tag.decent  { border-color: #ca8a04; color: #ca8a04; }
.tag.robust  { border-color: #1e88e5; color: #1e88e5; }
.tag.green   { border-color: #2e7d32; color: #2e7d32; }
.est { font-variant-numeric: tabular-nums; }

.btn-warning {
  padding: 0.625rem 1.25rem;
  background: #f59e0b;
  color: white;
  border: none;
  border-radius: 6px;
  cursor: pointer;
}

.btn-danger {
  padding: 0.625rem 1.25rem;
  background: #ef4444;
  color: white;
  border: none;
  border-radius: 6px;
  cursor: pointer;
}

.danger-zone {
  border: 1px solid #fca5a5;
}
</style>
