<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { accountApi } from '@/api/accountApi'
import { useAuthStore } from '@/stores/auth'
import { useNotificationStore } from '@/stores/notification'
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import ConfirmDialog from '@/components/ConfirmDialog.vue'

const auth = useAuthStore()
const notify = useNotificationStore()

const remainingCount = ref(0)
const loading = ref(true)
const newCodes = ref<string[]>([])
const showRegenerateDialog = ref(false)

async function fetchStatus() {
  loading.value = true
  try {
    const res = await accountApi.getRecoveryCodesStatus()
    if (res.data.success && res.data.data) {
      remainingCount.value = res.data.data.remainingCount
    }
  } finally {
    loading.value = false
  }
}

async function handleRegenerate() {
  showRegenerateDialog.value = false
  if (!auth.kemPrivateKey) {
    notify.error('Log in with your password to regenerate recovery codes')
    return
  }
  try {
    const codes = await auth.regenerateKemRecovery()
    if (codes === false) {
      notify.error('Log in with your password to regenerate recovery codes')
      return
    }
    newCodes.value = codes
    notify.success('Recovery codes regenerated')
    await fetchStatus()
  } catch {
    notify.error('Failed to regenerate codes')
  }
}

async function handleConfirm() {
  newCodes.value = []
  await fetchStatus()
}

onMounted(fetchStatus)
</script>

<template>
  <div class="page">
    <h1>Recovery Codes</h1>

    <LoadingSpinner v-if="loading" />

    <template v-else>
      <div v-if="remainingCount < 5 && remainingCount > 0" class="warning-banner">
        ⚠ Only {{ remainingCount }} of 8 codes remaining — regenerate soon.
      </div>
      <div v-else-if="remainingCount === 0" class="warning-banner danger">
        ⚠ No recovery codes remaining. Regenerate immediately.
      </div>

      <div class="card">
        <div class="status">
          <span class="remaining">{{ remainingCount }} of 8 codes remaining</span>
        </div>

        <div v-if="newCodes.length" class="codes-section">
          <p class="warning">Save these codes somewhere safe. You won't see them again!</p>
          <div class="codes-grid">
            <code v-for="code in newCodes" :key="code">{{ code }}</code>
          </div>
          <button class="btn-primary" @click="handleConfirm">I've saved my codes</button>
        </div>

        <div class="actions">
          <button class="btn-secondary" @click="showRegenerateDialog = true">
            Regenerate Codes
          </button>
          <p v-if="!auth.kemPrivateKey" class="muted-note">
            Log in with your password to regenerate codes.
          </p>
        </div>
      </div>

      <ConfirmDialog
        :show="showRegenerateDialog"
        title="Regenerate Recovery Codes"
        message="This will invalidate all existing recovery codes and generate new ones."
        @confirm="handleRegenerate"
        @cancel="showRegenerateDialog = false"
      />
    </template>
  </div>
</template>

<style scoped>
.page { max-width: 640px; }
.page h1 { margin-bottom: 1.5rem; }

.warning-banner {
  background: rgba(245, 158, 11, 0.1);
  border: 1px solid #f59e0b;
  color: #92400e;
  padding: 0.75rem 1rem;
  border-radius: 8px;
  margin-bottom: 1.25rem;
  font-size: 0.9rem;
  font-weight: 500;
}

.warning-banner.danger {
  background: rgba(239, 68, 68, 0.1);
  border-color: #ef4444;
  color: #7f1d1d;
}

.card {
  background: white;
  padding: 1.5rem;
  border-radius: 8px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.status {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.remaining { color: #6b7280; }

.warning {
  color: #dc2626;
  font-weight: 500;
  margin-bottom: 1rem;
}

.codes-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.5rem;
  margin-bottom: 1rem;
}

.codes-grid code {
  background: #f3f4f6;
  padding: 0.5rem;
  border-radius: 4px;
  font-size: 0.8rem;
  text-align: center;
  word-break: break-all;
}

.codes-section {
  margin-bottom: 1.5rem;
  padding: 1rem;
  border: 1px solid #fca5a5;
  border-radius: 8px;
}

.actions { margin-top: 1rem; }

.muted-note {
  margin-top: 0.5rem;
  color: #9ca3af;
  font-size: 0.875rem;
}

.btn-primary {
  padding: 0.625rem 1.25rem;
  background: #667eea;
  color: white;
  border: none;
  border-radius: 6px;
  cursor: pointer;
}

.btn-secondary {
  padding: 0.625rem 1.25rem;
  background: #e5e7eb;
  color: #374151;
  border: none;
  border-radius: 6px;
  cursor: pointer;
}
</style>
