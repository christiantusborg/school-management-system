<template>
  <div class="pu-tab">
    <div v-if="loadError" class="err-banner">{{ loadError }}</div>

    <h2 class="page-title">My Users</h2>
    <p class="page-sub">{{ users.length }} user{{ users.length !== 1 ? 's' : '' }} in your organisation</p>

    <table v-if="users.length" class="data-table">
      <thead><tr><th>Username</th><th>Name</th><th>Email</th><th>Status</th><th>Actions</th></tr></thead>
      <tbody>
        <tr v-for="u in users" :key="u.userId" class="data-row">
          <td class="mono">{{ u.username }}<span v-if="u.isSelf" class="self-tag">you</span></td>
          <td>{{ u.firstName ?? '—' }} {{ u.lastName ?? '' }}</td>
          <td>{{ u.email ?? '—' }}</td>
          <td>
            <span :class="['status', u.isEnabled ? 'on' : 'off']">{{ u.isEnabled ? 'Active' : 'Disabled' }}</span>
          </td>
          <td class="actions">
            <button class="btn-sm" :disabled="busy === u.userId" @click="resetPassword(u)">
              {{ busy === `reset:${u.userId}` ? 'Resetting…' : 'Reset password' }}
            </button>
            <button class="btn-sm btn-danger"
                    :disabled="u.isSelf || busy === u.userId"
                    :title="u.isSelf ? 'You cannot delete yourself' : 'Soft-delete this user'"
                    @click="del(u)">
              Delete
            </button>
          </td>
        </tr>
      </tbody>
    </table>

    <p v-else class="empty">No users yet — add the first one below.</p>

    <!-- Add user -->
    <div class="add-card">
      <h3>+ Add a new user</h3>
      <p class="hint">Their temporary password will be shown once after creation. Store it safely and share it with them.</p>
      <div class="add-fields">
        <input v-model="newUser.username" class="inp" placeholder="Username" />
        <input v-model="newUser.email" class="inp" placeholder="Email (optional)" />
        <button class="btn-primary" :disabled="!canAdd || busy === 'add'" @click="add">
          {{ busy === 'add' ? 'Creating…' : '+ Add user' }}
        </button>
      </div>
      <p v-if="addError" class="err-banner">{{ addError }}</p>
      <div v-if="lastCreated" class="created-banner">
        <strong>User created.</strong>
        <p>Username: <code>{{ lastCreated.username }}</code></p>
        <p>Temporary password: <code class="pw">{{ lastCreated.temporaryPassword }}</code>
          <button class="btn-tiny" @click="copyPassword">Copy</button>
        </p>
        <p class="hint">This password is shown only once. After they log in they should reset it themselves.</p>
        <button class="btn-tiny" @click="lastCreated = null">Dismiss</button>
      </div>
    </div>

    <!-- Reset password result -->
    <div v-if="lastReset" class="created-banner">
      <strong>Password reset for {{ lastReset.username }}.</strong>
      <p>Temporary password: <code class="pw">{{ lastReset.temporaryPassword }}</code>
        <button class="btn-tiny" @click="copyResetPassword">Copy</button>
      </p>
      <p class="hint">Share with the user via a secure channel. It is shown only once.</p>
      <button class="btn-tiny" @click="lastReset = null">Dismiss</button>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import api from '../../../api/client.js'

const users = ref([])
const loadError = ref('')
const busy = ref('')

const newUser = reactive({ username: '', email: '' })
const addError = ref('')
const lastCreated = ref(null)
const lastReset = ref(null)

const canAdd = computed(() => newUser.username.trim().length > 0)

async function load() {
  loadError.value = ''
  try {
    const res = await api.get('/v1/partner/my-users')
    users.value = res.data.items ?? []
  } catch (e) {
    loadError.value = e.response?.data?.error ?? e.message ?? 'Failed to load'
  }
}

async function add() {
  if (!canAdd.value) return
  addError.value = ''
  busy.value = 'add'
  try {
    const res = await api.post('/v1/partner/my-users', {
      username: newUser.username.trim(),
      email: newUser.email.trim() || null,
    })
    lastCreated.value = res.data
    newUser.username = ''
    newUser.email = ''
    await load()
  } catch (e) {
    addError.value = e.response?.data?.error ?? e.message ?? 'Failed'
  } finally { busy.value = '' }
}

async function resetPassword(u) {
  busy.value = `reset:${u.userId}`
  try {
    const res = await api.post(`/v1/partner/my-users/${u.userId}/reset-password`)
    lastReset.value = res.data
  } catch (e) {
    loadError.value = e.response?.data?.error ?? e.message ?? 'Failed to reset'
  } finally { busy.value = '' }
}

async function del(u) {
  if (u.isSelf) return
  if (!confirm(`Delete user ${u.username}? They will be soft-deleted and can no longer log in.`)) return
  busy.value = u.userId
  try {
    await api.delete(`/v1/partner/my-users/${u.userId}`)
    await load()
  } catch (e) {
    loadError.value = e.response?.data?.error ?? e.message ?? 'Failed to delete'
  } finally { busy.value = '' }
}

function copyPassword() { if (lastCreated.value) navigator.clipboard.writeText(lastCreated.value.temporaryPassword).catch(() => {}) }
function copyResetPassword() { if (lastReset.value) navigator.clipboard.writeText(lastReset.value.temporaryPassword).catch(() => {}) }

onMounted(load)
</script>

<style scoped>
.pu-tab { padding: .5rem 0; }
.page-title { color: #003366; margin: 0; }
.page-sub { color: #888; font-size: .85rem; margin: .25rem 0 1rem; }
.err-banner { background: #fef2f2; border: 1px solid #fca5a5; color: #b91c1c; padding: .5rem .8rem; border-radius: 6px; font-size: .85rem; margin: .5rem 0; }

.data-table { width: 100%; border-collapse: collapse; background: #fff; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.04); overflow: hidden; }
.data-table th { text-align: left; font-size: .72rem; color: #5f6e85; text-transform: uppercase; padding: .55rem .75rem; background: #fafbfc; border-bottom: 1px solid #e5eaf1; }
.data-row td { padding: .55rem .75rem; border-bottom: 1px solid #eef2f7; font-size: .88rem; }
.mono { font-family: monospace; font-size: .82rem; color: #0a264f; }
.self-tag { background: #e8f0f8; color: #003366; border-radius: 4px; padding: 1px 6px; font-size: .7rem; font-weight: 700; margin-left: .4rem; }
.status { font-size: .75rem; padding: 2px 8px; border-radius: 10px; font-weight: 600; }
.status.on { background: #d1fae5; color: #065f46; }
.status.off { background: #fef2f2; color: #b91c1c; }
.actions { display: flex; gap: .35rem; flex-wrap: wrap; }
.btn-sm { background: #fff; border: 1.5px solid #d0d7e0; padding: .25rem .65rem; border-radius: 5px; font-size: .78rem; cursor: pointer; }
.btn-sm:disabled { opacity: .5; cursor: not-allowed; }
.btn-danger { background: #fef2f2; color: #b91c1c; border-color: #fca5a5; }
.btn-danger:hover:not(:disabled) { background: #fde8e8; }

.empty { padding: 1rem; background: #f6f9fd; color: #5f6e85; border-radius: 8px; text-align: center; }

.add-card { margin-top: 1.25rem; background: #fff; border: 1px solid #e8edf4; border-radius: 8px; padding: 1rem 1.25rem; }
.add-card h3 { color: #003366; margin: 0 0 .35rem; font-size: 1rem; }
.hint { color: #888; font-size: .82rem; margin: 0 0 .65rem; }
.add-fields { display: flex; gap: .55rem; align-items: center; flex-wrap: wrap; }
.inp { padding: .45rem .7rem; border: 1.5px solid #d0d7e0; border-radius: 5px; font-size: .88rem; min-width: 200px; }
.btn-primary { background: #003366; color: #fff; border: 0; padding: .45rem 1rem; border-radius: 5px; font-weight: 600; cursor: pointer; }
.btn-primary:disabled { opacity: .55; cursor: not-allowed; }

.created-banner { margin-top: 1rem; background: #ecfdf5; border: 1px solid #6ee7b7; padding: .75rem 1rem; border-radius: 6px; }
.created-banner strong { color: #065f46; }
.pw { background: #fff; padding: 2px 8px; border-radius: 4px; font-family: monospace; font-size: .82rem; }
.btn-tiny { background: #fff; border: 1px solid #d0d7e0; border-radius: 4px; padding: 2px 8px; font-size: .76rem; cursor: pointer; margin-left: .35rem; }
</style>
