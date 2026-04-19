import { reactive } from 'vue'
import api from '../api/client.js'
import { blindPassword, signChallenge } from '../crypto/opaque.js'

export const auth = reactive({
  user: null,
  error: null,
  loading: false,

  // MFA pending state
  mfaPendingId: null,
  mfaAvailableMethods: [],

  async login(username, password) {
    this.error = null
    this.loading = true
    this.mfaPendingId = null
    this.mfaAvailableMethods = []

    try {
      // Step 1: blind password and call init
      const { blind, blindedElement } = blindPassword(password)
      const initRes = await api.post('/v1/login/init', { username, blindedElement })
      const { loginId, evaluatedElement, challenge } = initRes.data

      // Step 2: sign challenge and call finish
      const signature = signChallenge(password, blind, evaluatedElement, challenge)
      const finishRes = await api.post('/v1/login/finish', { loginId, signature })
      const finishData = finishRes.data

      // MFA required
      if (finishData.mfaPendingId) {
        this.mfaPendingId = finishData.mfaPendingId
        this.mfaAvailableMethods = finishData.availableMethods ?? []
        return 'mfa'
      }

      // Success — store token and fetch user profile
      localStorage.setItem('adminToken', finishData.token)
      await this.fetchUser()
      return this._roleToRoute()
    } catch (e) {
      this.error = e.response?.data?.message ?? e.message ?? 'Login failed'
      return null
    } finally {
      this.loading = false
    }
  },

  async verifyMfaTotp(code) {
    this.error = null
    this.loading = true
    try {
      const res = await api.post('/v1/mfa/totp/verify', { pendingId: this.mfaPendingId, code })
      return this._completeMfa(res.data.token)
    } catch (e) {
      this.error = e.response?.data?.message ?? e.message ?? 'Invalid code'
      return null
    } finally {
      this.loading = false
    }
  },

  async sendMfaEmail() {
    try {
      await api.post('/v1/mfa/email/send', { pendingId: this.mfaPendingId })
    } catch (e) {
      this.error = e.response?.data?.message ?? e.message ?? 'Failed to send code'
    }
  },

  async verifyMfaEmail(code) {
    this.error = null
    this.loading = true
    try {
      const res = await api.post('/v1/mfa/email/verify', { pendingId: this.mfaPendingId, code })
      return this._completeMfa(res.data.token)
    } catch (e) {
      this.error = e.response?.data?.message ?? e.message ?? 'Invalid code'
      return null
    } finally {
      this.loading = false
    }
  },

  async _completeMfa(token) {
    localStorage.setItem('adminToken', token)
    this.mfaPendingId = null
    this.mfaAvailableMethods = []
    await this.fetchUser()
    return this._roleToRoute()
  },

  async fetchUser() {
    try {
      const res = await api.get('/v1/me')
      const data = res.data
      this.user = {
        ...data,
        displayName: data.username,
        role: data.roles?.includes('Admin') ? 'employee'
            : data.roles?.includes('Partner') ? 'partner'
            : data.roles?.includes('Student') ? 'student'
            : null,
      }
    } catch {
      this.logout()
    }
  },

  _roleToRoute() {
    if (!this.user) return null
    if (this.user.role === 'employee') return 'employee'
    if (this.user.role === 'partner')  return 'partner'
    if (this.user.role === 'student')  return 'student'
    return null
  },

  async logout() {
    try { await api.post('/v1/logout') } catch { /* ignore */ }
    this.user = null
    this.error = null
    this.mfaPendingId = null
    this.mfaAvailableMethods = []
    localStorage.removeItem('adminToken')
  },

  get isEmployee() { return this.user?.role === 'employee' },
  get isPartner()  { return this.user?.role === 'partner'  },
  get isStudent()  { return this.user?.role === 'student'  },
})

// Restore session on page load
;(async () => {
  if (localStorage.getItem('adminToken')) {
    await auth.fetchUser()
  }
})()
