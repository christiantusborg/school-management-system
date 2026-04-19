<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { adminApi } from '@/api/adminApi'
import { useAdminStore } from '@/stores/admin'
import { useNotificationStore } from '@/stores/notification'
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import ConfirmDialog from '@/components/ConfirmDialog.vue'

const route = useRoute()
const admin = useAdminStore()
const notify = useNotificationStore()

const userId = route.params.id as string
const showRoleDialog = ref(false)
const newRole = ref('User')
const resetTokenValue = ref('')
const showResetDialog = ref(false)

onMounted(() => admin.fetchUser(userId))

async function toggleEnabled() {
  if (!admin.currentUser) return
  try {
    if (admin.currentUser.isEnabled) {
      await adminApi.disableUser(userId)
      notify.success('User disabled')
    } else {
      await adminApi.enableUser(userId)
      notify.success('User enabled')
    }
    await admin.fetchUser(userId)
  } catch {
    notify.error('Action failed')
  }
}

async function handleChangeRole() {
  showRoleDialog.value = false
  try {
    await adminApi.changeRole(userId, { newRole: newRole.value })
    notify.success(`Role changed to ${newRole.value}`)
    await admin.fetchUser(userId)
  } catch {
    notify.error('Failed to change role')
  }
}

async function handleResetPassword() {
  showResetDialog.value = false
  try {
    const res = await adminApi.resetPassword(userId)
    if (res.data.success && res.data.data) {
      resetTokenValue.value = res.data.data.resetToken
      notify.success('Password reset. Give the user the reset token below.')
    }
  } catch {
    notify.error('Failed to reset password')
  }
}
</script>

<template>
  <div class="page">
    <RouterLink to="/admin/users" class="back-link">Back to Users</RouterLink>

    <LoadingSpinner v-if="admin.loading" />

    <template v-else-if="admin.currentUser">
      <h1>{{ admin.currentUser.username }}</h1>

      <div class="card">
        <div class="info-grid">
          <div><strong>Email:</strong> {{ admin.currentUser.email }}</div>
          <div><strong>Roles:</strong> {{ admin.currentUser.roles.join(', ') }}</div>
          <div>
            <strong>Status:</strong>
            <span :class="['badge', admin.currentUser.isEnabled ? 'badge-green' : 'badge-red']">
              {{ admin.currentUser.isEnabled ? 'Active' : 'Disabled' }}
            </span>
          </div>
          <div><strong>Created:</strong> {{ new Date(admin.currentUser.createdAt).toLocaleDateString() }}</div>
        </div>

        <div v-if="admin.currentUser.firstName || admin.currentUser.bio" class="profile-info">
          <h3>Profile</h3>
          <div v-if="admin.currentUser.firstName || admin.currentUser.lastName"><strong>Name:</strong> {{ admin.currentUser.firstName }} {{ admin.currentUser.lastName }}</div>
          <div v-if="admin.currentUser.bio"><strong>Bio:</strong> {{ admin.currentUser.bio }}</div>
        </div>
      </div>

      <div class="actions-card">
        <h2>Actions</h2>

        <div class="action-row">
          <button :class="admin.currentUser.isEnabled ? 'btn-warning' : 'btn-success'" @click="toggleEnabled">
            {{ admin.currentUser.isEnabled ? 'Disable User' : 'Enable User' }}
          </button>
        </div>

        <div class="action-row">
          <select v-model="newRole" class="select-input">
            <option value="User">User</option>
            <option value="Admin">Admin</option>
          </select>
          <button class="btn-primary" @click="showRoleDialog = true">Change Role</button>
        </div>

        <div class="action-row">
          <button class="btn-warning" @click="showResetDialog = true">Reset Password</button>
        </div>

        <div v-if="resetTokenValue" class="temp-password">
          <strong>Reset Token:</strong>
          <p>The user must visit the registration page and use this token to set a new password.</p>
          <code>{{ resetTokenValue }}</code>
        </div>
      </div>

      <ConfirmDialog
        :show="showRoleDialog"
        title="Change Role"
        :message="`Change role to ${newRole}?`"
        @confirm="handleChangeRole"
        @cancel="showRoleDialog = false"
      />

      <ConfirmDialog
        :show="showResetDialog"
        title="Reset Password"
        message="This will revoke all sessions and require the user to re-register with a reset token."
        @confirm="handleResetPassword"
        @cancel="showResetDialog = false"
      />
    </template>
  </div>
</template>

<style scoped>
.page { max-width: 640px; }
.page h1 { margin-bottom: 1.5rem; }

.back-link {
  display: inline-block;
  margin-bottom: 1rem;
  color: #667eea;
}

.card, .actions-card {
  background: white;
  padding: 1.5rem;
  border-radius: 8px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  margin-bottom: 1.5rem;
}

.info-grid {
  display: grid;
  gap: 0.75rem;
}

.profile-info {
  margin-top: 1.5rem;
  padding-top: 1rem;
  border-top: 1px solid #e5e7eb;
}

.profile-info h3 { margin-bottom: 0.75rem; }

.badge {
  padding: 0.2rem 0.6rem;
  border-radius: 12px;
  font-size: 0.8rem;
  font-weight: 500;
}

.badge-green { background: #dcfce7; color: #166534; }
.badge-red { background: #fee2e2; color: #991b1b; }

.actions-card h2 { margin-bottom: 1rem; }

.action-row {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 0.75rem;
}

.select-input {
  padding: 0.5rem;
  border: 1px solid #d1d5db;
  border-radius: 4px;
}

.btn-primary { padding: 0.5rem 1rem; background: #667eea; color: white; border: none; border-radius: 6px; cursor: pointer; }
.btn-warning { padding: 0.5rem 1rem; background: #f59e0b; color: white; border: none; border-radius: 6px; cursor: pointer; }
.btn-success { padding: 0.5rem 1rem; background: #22c55e; color: white; border: none; border-radius: 6px; cursor: pointer; }

.temp-password {
  margin-top: 1rem;
  padding: 1rem;
  background: #fef9c3;
  border-radius: 6px;
}

.temp-password code {
  display: block;
  margin-top: 0.5rem;
  font-size: 1rem;
  word-break: break-all;
}
</style>
