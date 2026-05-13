<template>
  <div class="verify-page">
    <div class="verify-card">
      <h1>Email confirmation</h1>
      <p v-if="state === 'verifying'" class="status">Confirming…</p>
      <p v-else-if="state === 'ok'" class="status ok">Confirmed — taking you back to your application…</p>
      <div v-else class="status err">
        <p><strong>That link didn't work.</strong></p>
        <p>{{ message }}</p>
        <p class="hint">Confirmation links expire after 30 days. Try logging in if you've already finished your application.</p>
        <RouterLink class="btn-primary" to="/login">Go to login</RouterLink>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute, useRouter, RouterLink } from 'vue-router'
import { auth } from '../store/auth.js'
import api from '../api/client.js'

const route = useRoute()
const router = useRouter()
const state = ref('verifying')
const message = ref('')

onMounted(async () => {
  const userId = route.query.userId
  const token = route.query.token
  if (!userId || !token) {
    state.value = 'err'
    message.value = 'Missing verification details in the link.'
    return
  }
  try {
    const res = await api.post('/v1/public/student-signup/verify-email', { userId, token })
    const { wizardToken, regularToken, redirect } = res.data
    if (wizardToken) localStorage.setItem('wizardToken', wizardToken)
    if (redirect === 'dashboard' && regularToken) {
      localStorage.setItem('adminToken', regularToken)
      await auth.fetchUser()
      state.value = 'ok'
      setTimeout(() => router.push('/student/application'), 500)
    } else {
      state.value = 'ok'
      setTimeout(() => router.push('/apply'), 500)
    }
  } catch (e) {
    state.value = 'err'
    message.value = e.response?.status === 410
      ? 'This link has expired or already been used too long ago.'
      : (e.response?.data?.error ?? e.message ?? 'Verification failed.')
  }
})
</script>

<style scoped>
.verify-page { min-height: 100vh; background: #f2f5f9; display: flex; align-items: center; justify-content: center; padding: 2rem; }
.verify-card { background: #fff; border-radius: 10px; padding: 2rem; box-shadow: 0 2px 12px rgba(0,0,0,0.06); max-width: 460px; width: 100%; text-align: center; }
h1 { color: #003366; margin: 0 0 1rem; font-size: 1.4rem; }
.status { color: #555; }
.status.ok { color: #065f46; font-weight: 600; }
.status.err { color: #b91c1c; }
.hint { color: #888; font-size: 0.85rem; margin: 0.6rem 0 1rem; }
.btn-primary { display: inline-block; background: #003366; color: #fff; padding: 0.55rem 1.1rem; border-radius: 6px; text-decoration: none; font-weight: 600; }
</style>
