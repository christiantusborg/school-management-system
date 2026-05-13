<template>
  <div class="cpw-wrap">
    <div class="cpw-card">
      <div class="cpw-head">
        <h1>Change Password</h1>
        <router-link class="cpw-back" to="/partner">← Back</router-link>
      </div>

      <p class="cpw-hint">
        Enter your current password and pick a new one. We verify the current
        password before changing it — your typed value never leaves your
        browser as plaintext (we use the same OPAQUE flow as login).
      </p>

      <form @submit.prevent="submit" class="cpw-form">
        <div class="field">
          <label>Current password</label>
          <input v-model="currentPassword" type="password" autocomplete="current-password" required />
        </div>

        <div class="field">
          <label>New password</label>
          <input v-model="newPassword" type="password" autocomplete="new-password" required minlength="8" />
        </div>

        <div class="field">
          <label>Confirm new password</label>
          <input v-model="confirmPassword" type="password" autocomplete="new-password" required />
        </div>

        <p v-if="formError" class="cpw-err">{{ formError }}</p>
        <p v-if="success" class="cpw-ok">✓ Password updated successfully. Use it next time you log in.</p>

        <div class="cpw-actions">
          <button type="submit" class="btn-primary" :disabled="!canSubmit || submitting">
            {{ submitting ? 'Updating…' : 'Change password' }}
          </button>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import api from '../api/client.js'
import { blindPassword, signChallenge, deriveClientPublicKey } from '../crypto/opaque.js'

const currentPassword = ref('')
const newPassword = ref('')
const confirmPassword = ref('')
const submitting = ref(false)
const formError = ref('')
const success = ref(false)

const canSubmit = computed(() =>
  currentPassword.value.length > 0
  && newPassword.value.length >= 8
  && newPassword.value === confirmPassword.value)

async function submit() {
  formError.value = ''
  success.value = false
  if (!canSubmit.value) return

  if (newPassword.value === currentPassword.value) {
    formError.value = 'New password must differ from the current one.'
    return
  }

  submitting.value = true
  try {
    // Blind both passwords client-side: old for verification, new for setup.
    const oldB = blindPassword(currentPassword.value)
    const newB = blindPassword(newPassword.value)

    // Init: server evaluates both OPRFs, stashes new derivation in transient
    // state, returns a challenge to verify the old password.
    const initRes = await api.post('/v1/change-password/init', {
      oldBlindedElement: oldB.blindedElement,
      blindedElement: newB.blindedElement,
    })
    const { changeId, oldEvaluatedElement, challenge, evaluatedElement } = initRes.data

    // Sign challenge with the OLD password — proves we know it.
    const signature = signChallenge(currentPassword.value, oldB.blind, oldEvaluatedElement, challenge)
    // Derive NEW public key from the NEW password's OPRF output.
    const clientPublicKey = deriveClientPublicKey(newPassword.value, newB.blind, evaluatedElement)

    await api.post('/v1/change-password/finalize', { changeId, signature, clientPublicKey })

    success.value = true
    currentPassword.value = ''
    newPassword.value = ''
    confirmPassword.value = ''
  } catch (e) {
    formError.value = e.response?.data?.message ?? e.response?.data ?? e.message ?? 'Failed to change password'
  } finally {
    submitting.value = false
  }
}
</script>

<style scoped>
.cpw-wrap { min-height: 100vh; background: #f0f4f8; display: flex; align-items: flex-start; justify-content: center; padding: 3rem 1rem; }
.cpw-card { background: #fff; border-radius: 10px; box-shadow: 0 4px 16px rgba(0,0,0,.08); padding: 1.75rem 2rem; width: 480px; max-width: 100%; }
.cpw-head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 1rem; }
.cpw-head h1 { margin: 0; color: #003366; font-size: 1.2rem; }
.cpw-back { color: #0a5aad; text-decoration: none; font-size: .9rem; }
.cpw-back:hover { text-decoration: underline; }
.cpw-hint { color: #5f6e85; font-size: .86rem; line-height: 1.5; margin: 0 0 1.25rem; }
.cpw-form { display: flex; flex-direction: column; gap: .85rem; }
.field { display: flex; flex-direction: column; }
.field label { font-size: .8rem; color: #5f6e85; margin-bottom: .25rem; font-weight: 600; }
.field input { padding: .55rem .75rem; border: 1.5px solid #ccc; border-radius: 6px; font-size: .92rem; outline: none; }
.field input:focus { border-color: #0055a5; }
.cpw-err { color: #b91c1c; background: #fef2f2; border: 1px solid #fca5a5; padding: .5rem .75rem; border-radius: 6px; font-size: .85rem; margin: 0; }
.cpw-ok  { color: #065f46; background: #ecfdf5; border: 1px solid #6ee7b7; padding: .5rem .75rem; border-radius: 6px; font-size: .85rem; margin: 0; }
.cpw-actions { display: flex; justify-content: flex-end; margin-top: .5rem; }
.btn-primary { background: #0055a5; color: #fff; border: 0; padding: .6rem 1.2rem; border-radius: 6px; font-weight: 700; cursor: pointer; font-size: .92rem; }
.btn-primary:disabled { background: #aaa; cursor: not-allowed; }
.btn-primary:hover:not(:disabled) { background: #003d7a; }
</style>
