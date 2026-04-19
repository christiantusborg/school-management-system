import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authApi } from '@/api/authApi'
import { accountApi } from '@/api/accountApi'
import { blindPassword, derivePublicKey, signChallenge, deriveKemEncKey } from '@/crypto/opaque'
import { generateKemKeyPair, encryptKemPrivateKey, decryptKemPrivateKey, toBase64, fromBase64 } from '@/crypto/kem'
import type { ApiResponse, LoginResponse, MeResponse } from '@/types'
import router from '@/router'

const SESSION_KEM_PRIVATE = 'odin_kem_priv'
const SESSION_KEM_PUBLIC  = 'odin_kem_pub'

function saveKemToSession(priv: Uint8Array, pub: Uint8Array) {
  sessionStorage.setItem(SESSION_KEM_PRIVATE, toBase64(priv))
  sessionStorage.setItem(SESSION_KEM_PUBLIC,  toBase64(pub))
}

function loadKemFromSession(): { priv: Uint8Array; pub: Uint8Array } | null {
  const priv = sessionStorage.getItem(SESSION_KEM_PRIVATE)
  const pub  = sessionStorage.getItem(SESSION_KEM_PUBLIC)
  if (!priv || !pub) return null
  return { priv: fromBase64(priv), pub: fromBase64(pub) }
}

function clearKemSession() {
  sessionStorage.removeItem(SESSION_KEM_PRIVATE)
  sessionStorage.removeItem(SESSION_KEM_PUBLIC)
}

export const useAuthStore = defineStore('auth', () => {
  const token = ref(localStorage.getItem('odin_token') || '')
  const user = ref<MeResponse | null>(null)

  // Restore KEM keys from sessionStorage (survives page refresh within same tab)
  const _sessionKem = loadKemFromSession()
  const kemPublicKey  = ref<Uint8Array | null>(_sessionKem?.pub  ?? null)
  const kemPrivateKey = ref<Uint8Array | null>(_sessionKem?.priv ?? null)
  const kemEncKey = ref<Uint8Array | null>(null)      // OPRF-derived key, kept for the session

  // MFA pending state
  const mfaPendingId = ref<string | null>(null)
  const mfaAvailableMethods = ref<string[]>([])
  const _pendingKemData = ref<{ encKey: Uint8Array; secretKey: Uint8Array | null; publicKey: Uint8Array | null } | null>(null)

  const isAuthenticated = computed(() => !!token.value)
  const isAdmin = computed(() => user.value?.roles.includes('Admin') ?? false)

  // ── helpers ─────────────────────────────────────────────────────────────

  function clearKem() {
    kemPublicKey.value = null
    kemPrivateKey.value = null
    kemEncKey.value = null
    mfaPendingId.value = null
    mfaAvailableMethods.value = []
    _pendingKemData.value = null
    clearKemSession()
  }

  /** Returns true if the key was successfully loaded. */
  async function loadKemFromServer(encKey: Uint8Array): Promise<boolean> {
    const kemRes = await accountApi.getKemKeyPair()
    if (!kemRes.data.success || !kemRes.data.data) {
      console.warn('[KEM] No key pair on server')
      return false
    }
    const { publicKey, encryptedPrivateKey, nonce } = kemRes.data.data
    const pub  = fromBase64(publicKey)
    try {
      const priv = await decryptKemPrivateKey(encryptedPrivateKey, nonce, encKey)
      kemPublicKey.value  = pub
      kemPrivateKey.value = priv
      kemEncKey.value     = encKey
      saveKemToSession(priv, pub)
      return true
    } catch (e) {
      console.error('[KEM] Private key decryption failed (wrong encKey or corrupted key):', e)
      return false
    }
  }

  async function buildAndSaveKemKeyPair(secretKey: Uint8Array, publicKey: Uint8Array, encKey: Uint8Array) {
    const pwEnc = await encryptKemPrivateKey(secretKey, encKey)
    await accountApi.saveKemKeyPair({
      publicKey: toBase64(publicKey),
      encryptedPrivateKey: pwEnc.encryptedKey,
      nonce: pwEnc.nonce
    })
    kemPublicKey.value  = publicKey
    kemPrivateKey.value = secretKey
    kemEncKey.value     = encKey
    saveKemToSession(secretKey, publicKey)
  }

  async function setupRecoveryCodes(privKey: Uint8Array): Promise<string[]> {
    const COUNT = 8
    const codes = Array.from({ length: COUNT }, () => {
      const codeId = Array.from(crypto.getRandomValues(new Uint8Array(4)))
        .map(b => b.toString(16).padStart(2, '0')).join('')  // 8 hex chars
      const secret = toBase64(crypto.getRandomValues(new Uint8Array(16)))  // 24 chars
      return `${codeId}-${secret}`
    })

    const blindResults = codes.map(c => blindPassword(c))
    const codeIds = codes.map(c => c.substring(0, 8))

    const initRes = await accountApi.recoveryCodesInit({
      codeIds,
      blindedElements: blindResults.map(r => r.blindedElement)
    })
    console.log('[setupRecoveryCodes] init response:', initRes.data)
    if (!initRes.data.success || !initRes.data.data) throw new Error('Recovery codes init failed')

    const { batchId, evaluatedElements } = initRes.data.data

    const clientPublicKeys = codes.map((c, i) => derivePublicKey(c, blindResults[i].blind, evaluatedElements[i]))

    const encrypted = await Promise.all(codes.map((c, i) => {
      const encKey = deriveKemEncKey(c, blindResults[i].blind, evaluatedElements[i])
      return encryptKemPrivateKey(privKey, encKey)
    }))

    await accountApi.recoveryCodesFinalize({
      batchId,
      clientPublicKeys,
      encryptedPrivateKeys: encrypted.map(e => e.encryptedKey),
      nonces: encrypted.map(e => e.nonce)
    })

    return codes
  }

  // ── complete login after MFA ───────────────────────────────────────────────

  async function completeMfa(loginResponse: LoginResponse) {
    if (!loginResponse.token) return
    token.value = loginResponse.token
    localStorage.setItem('odin_token', token.value)
    await fetchUser()
    if (_pendingKemData.value) {
      const { encKey, secretKey, publicKey } = _pendingKemData.value
      try {
        const loaded = await loadKemFromServer(encKey)
        if (!loaded) {
          if (secretKey && publicKey) {
            await buildAndSaveKemKeyPair(secretKey, publicKey, encKey)
          } else {
            console.warn('[KEM] Generating fresh key pair after failed MFA load')
            const kp = generateKemKeyPair()
            await buildAndSaveKemKeyPair(kp.secretKey, kp.publicKey, encKey)
          }
        }
      } catch (e) {
        console.error('[KEM] Unexpected error during MFA key setup:', e)
      }
      _pendingKemData.value = null
    }
    mfaPendingId.value = null
    mfaAvailableMethods.value = []
  }

  // ── login ────────────────────────────────────────────────────────────────

  async function login(username: string, password: string, deviceInfo?: string): Promise<ApiResponse<LoginResponse> & { requiresMfa?: boolean }> {
    const { blind, blindedElement } = blindPassword(password)
    const initRes = await authApi.loginInit({ username, blindedElement, deviceInfo })

    if (!initRes.data.success || !initRes.data.data)
      return { success: false, message: initRes.data.message || 'Login failed' }

    const { loginId, evaluatedElement, challenge } = initRes.data.data
    const encKey = deriveKemEncKey(password, blind, evaluatedElement)

    const signature = signChallenge(password, blind, evaluatedElement, challenge)
    const finishRes = await authApi.loginFinish({ loginId, signature })

    if (finishRes.data.success && finishRes.data.data) {
      const data = finishRes.data.data
      if (data.mfaPendingId) {
        mfaPendingId.value = data.mfaPendingId
        mfaAvailableMethods.value = data.availableMethods ?? []
        _pendingKemData.value = { encKey, secretKey: null, publicKey: null }
        return { success: true, requiresMfa: true, data }
      }
      token.value = data.token!
      localStorage.setItem('odin_token', token.value)
      await fetchUser()
      try {
        const loaded = await loadKemFromServer(encKey)
        if (!loaded) {
          // Key missing or corrupted — generate a fresh key pair so the user can still work
          console.warn('[KEM] Generating fresh key pair after failed load')
          const { publicKey, secretKey } = generateKemKeyPair()
          await buildAndSaveKemKeyPair(secretKey, publicKey, encKey)
        }
      } catch (e) {
        console.error('[KEM] Unexpected error during key setup:', e)
      }
    }
    return finishRes.data
  }

  // ── register ─────────────────────────────────────────────────────────────

  async function register(
    username: string,
    email: string,
    password: string,
    inviteCode?: string
  ): Promise<{ success: boolean; message?: string; recoveryCodes?: string[] }> {
    const { blind, blindedElement } = blindPassword(password)

    const initRes = await authApi.registerInit({ username, email, blindedElement, inviteCode })

    if (!initRes.data.success || !initRes.data.data)
      return { success: false, message: initRes.data.message || 'Registration failed' }

    const { registrationId, evaluatedElement } = initRes.data.data
    const encKey = deriveKemEncKey(password, blind, evaluatedElement)
    const clientPublicKey = derivePublicKey(password, blind, evaluatedElement)

    const { publicKey, secretKey } = generateKemKeyPair()

    const finalizeRes = await authApi.registerFinalize({ registrationId, clientPublicKey })

    if (finalizeRes.data.success && finalizeRes.data.data) {
      token.value = finalizeRes.data.data.token
      localStorage.setItem('odin_token', token.value)
      await fetchUser()
      try {
        await buildAndSaveKemKeyPair(secretKey, publicKey, encKey)
        const codes = await setupRecoveryCodes(secretKey)
        return { success: true, recoveryCodes: codes }
      } catch (e) {
        console.error('[register] post-finalize setup failed:', e)
        return { success: true, recoveryCodes: [] }
      }
    }
    return { success: false, message: finalizeRes.data.message || 'Registration failed' }
  }

  // ── fetchUser ────────────────────────────────────────────────────────────

  async function fetchUser() {
    try {
      const res = await authApi.getMe()
      if (res.data.success && res.data.data) user.value = res.data.data
    } catch { logout() }
  }

  // ── logout ───────────────────────────────────────────────────────────────

  async function logout() {
    try { if (token.value) await authApi.logout() } catch { /* ignore */ }
    token.value = ''
    user.value = null
    clearKem()
    localStorage.removeItem('odin_token')
    router.push('/auth/login')
  }

  async function logoutEverywhere() {
    try { await authApi.logoutEverywhere() } catch { /* ignore */ }
    token.value = ''
    user.value = null
    clearKem()
    localStorage.removeItem('odin_token')
    router.push('/auth/login')
  }

  // ── recovery login ───────────────────────────────────────────────────────

  async function loginWithRecoveryCode(
    username: string,
    fullCode: string,
    deviceInfo?: string
  ): Promise<ApiResponse<LoginResponse>> {
    const codeId = fullCode.substring(0, 8)
    const { blind, blindedElement } = blindPassword(fullCode)
    const initRes = await authApi.loginRecoveryInit({ username, codeId, blindedElement, deviceInfo })

    if (!initRes.data.success || !initRes.data.data)
      return { success: false, message: initRes.data.message || 'Login failed' }

    const { loginId, evaluatedElement, challenge, encryptedPrivateKey, nonce } = initRes.data.data
    const encKey = deriveKemEncKey(fullCode, blind, evaluatedElement)
    const signature = signChallenge(fullCode, blind, evaluatedElement, challenge)

    const finishRes = await authApi.loginFinish({ loginId, signature })

    if (finishRes.data.success && finishRes.data.data) {
      token.value = finishRes.data.data.token!
      localStorage.setItem('odin_token', token.value)
      await fetchUser()
      try {
        const priv = await decryptKemPrivateKey(encryptedPrivateKey, nonce, encKey)
        kemPrivateKey.value = priv
        const kemRes = await accountApi.getKemKeyPair()
        if (kemRes.data.success && kemRes.data.data) {
          const pub = fromBase64(kemRes.data.data.publicKey)
          kemPublicKey.value = pub
          saveKemToSession(priv, pub)
        }
        // kemEncKey stays null — no password OPRF available after recovery login
      } catch { /* best-effort */ }
    }
    return finishRes.data
  }

  // ── password reset ───────────────────────────────────────────────────────

  async function resetPassword(
    resetToken: string,
    password: string
  ): Promise<{ success: boolean; message?: string; recoveryCodes?: string[] }> {
    const { blind, blindedElement } = blindPassword(password)

    const initRes = await authApi.resetInit({ resetToken, blindedElement })
    if (!initRes.data.success || !initRes.data.data)
      return { success: false, message: initRes.data.message || 'Password reset failed' }

    const { resetId, evaluatedElement } = initRes.data.data
    const encKey = deriveKemEncKey(password, blind, evaluatedElement)
    const clientPublicKey = derivePublicKey(password, blind, evaluatedElement)

    // New keypair — old private key unrecoverable after admin reset
    const { publicKey, secretKey } = generateKemKeyPair()

    const finalizeRes = await authApi.resetFinalize({ resetId, clientPublicKey })

    if (finalizeRes.data.success && finalizeRes.data.data) {
      token.value = finalizeRes.data.data.token
      localStorage.setItem('odin_token', token.value)
      await fetchUser()
      try {
        await buildAndSaveKemKeyPair(secretKey, publicKey, encKey)
        const codes = await setupRecoveryCodes(secretKey)
        return { success: true, recoveryCodes: codes }
      } catch {
        return { success: true, recoveryCodes: [] }
      }
    }
    return { success: false, message: finalizeRes.data.message || 'Password reset failed' }
  }

  // ── regenerate recovery codes ─────────────────────────────────────────────

  async function regenerateKemRecovery(): Promise<string[] | false> {
    if (!kemPrivateKey.value) return false
    try {
      const codes = await setupRecoveryCodes(kemPrivateKey.value)
      return codes
    } catch {
      return false
    }
  }

  return {
    token, user, isAuthenticated, isAdmin,
    kemPublicKey, kemPrivateKey, kemEncKey,
    mfaPendingId, mfaAvailableMethods,
    login, register, fetchUser, logout, logoutEverywhere,
    loginWithRecoveryCode, resetPassword, regenerateKemRecovery,
    completeMfa
  }
})
