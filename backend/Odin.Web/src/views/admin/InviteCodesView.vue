<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { adminApi } from '@/api/adminApi'
import { useAdminStore } from '@/stores/admin'
import { useNotificationStore } from '@/stores/notification'
import LoadingSpinner from '@/components/LoadingSpinner.vue'

const admin = useAdminStore()
const notify = useNotificationStore()

const assignedRole = ref('User')
const expirationDays = ref(7)
const creating = ref(false)
const lastCreatedCode = ref('')

onMounted(() => admin.fetchInviteCodes())

async function handleCreate() {
  creating.value = true
  try {
    const res = await adminApi.createInviteCode({
      assignedRole: assignedRole.value,
      expirationDays: expirationDays.value
    })
    if (res.data.success && res.data.data) {
      lastCreatedCode.value = res.data.data.code
      notify.success('Invite code created')
      await admin.fetchInviteCodes()
    }
  } catch {
    notify.error('Failed to create invite code')
  } finally {
    creating.value = false
  }
}
</script>

<template>
  <div class="page">
    <h1>Invite Codes</h1>

    <div class="card">
      <h2>Create Invite Code</h2>
      <div class="create-form">
        <div class="form-row">
          <div>
            <label class="form-label">Assigned Role</label>
            <select v-model="assignedRole" class="select-input">
              <option value="User">User</option>
              <option value="Admin">Admin</option>
            </select>
          </div>
          <div>
            <label class="form-label">Expiration (days)</label>
            <input v-model.number="expirationDays" type="number" min="1" class="number-input" />
          </div>
        </div>
        <button class="btn-primary" :disabled="creating" @click="handleCreate">
          {{ creating ? 'Creating...' : 'Generate Code' }}
        </button>
      </div>
      <div v-if="lastCreatedCode" class="new-code">
        <strong>New Code:</strong>
        <code>{{ lastCreatedCode }}</code>
      </div>
    </div>

    <LoadingSpinner v-if="admin.loading" />

    <table v-else class="data-table">
      <thead>
        <tr>
          <th>Code</th>
          <th>Role</th>
          <th>Expires</th>
          <th>Status</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="code in admin.inviteCodes" :key="code.inviteCodeId">
          <td><code>{{ code.code }}</code></td>
          <td>{{ code.assignedRole }}</td>
          <td>{{ new Date(code.expiresAt).toLocaleDateString() }}</td>
          <td>
            <span :class="['badge', code.redeemedByUserId ? 'badge-gray' : new Date(code.expiresAt) > new Date() ? 'badge-green' : 'badge-red']">
              {{ code.redeemedByUserId ? 'Redeemed' : new Date(code.expiresAt) > new Date() ? 'Active' : 'Expired' }}
            </span>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.page h1 { margin-bottom: 1.5rem; }

.card {
  background: white;
  padding: 1.5rem;
  border-radius: 8px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  margin-bottom: 1.5rem;
}

.card h2 { margin-bottom: 1rem; font-size: 1.25rem; }

.create-form .form-row {
  display: flex;
  gap: 1rem;
  margin-bottom: 1rem;
}

.form-label {
  display: block;
  margin-bottom: 0.375rem;
  font-size: 0.85rem;
  font-weight: 500;
  color: #374151;
}

.select-input, .number-input {
  padding: 0.5rem 0.75rem;
  border: 1px solid #d1d5db;
  border-radius: 6px;
  font-size: 0.9rem;
}

.number-input { width: 100px; }

.btn-primary {
  padding: 0.625rem 1.25rem;
  background: #667eea;
  color: white;
  border: none;
  border-radius: 6px;
  cursor: pointer;
}

.btn-primary:disabled { opacity: 0.6; cursor: not-allowed; }

.new-code {
  margin-top: 1rem;
  padding: 0.75rem;
  background: #ecfdf5;
  border-radius: 6px;
}

.new-code code {
  display: block;
  margin-top: 0.25rem;
  font-size: 0.95rem;
  word-break: break-all;
}

.data-table {
  width: 100%;
  background: white;
  border-radius: 8px;
  overflow: hidden;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  border-collapse: collapse;
}

.data-table th,
.data-table td {
  padding: 0.75rem 1rem;
  text-align: left;
  border-bottom: 1px solid #e5e7eb;
}

.data-table th {
  background: #f9fafb;
  font-weight: 600;
  font-size: 0.85rem;
  color: #6b7280;
  text-transform: uppercase;
}

.data-table code {
  font-size: 0.8rem;
  background: #f3f4f6;
  padding: 0.15rem 0.4rem;
  border-radius: 3px;
}

.badge {
  padding: 0.2rem 0.6rem;
  border-radius: 12px;
  font-size: 0.8rem;
  font-weight: 500;
}

.badge-green { background: #dcfce7; color: #166534; }
.badge-red { background: #fee2e2; color: #991b1b; }
.badge-gray { background: #f3f4f6; color: #6b7280; }
</style>
