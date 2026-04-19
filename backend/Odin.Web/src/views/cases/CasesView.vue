<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useNotificationStore } from '@/stores/notification'
import { caseApi } from '@/api/caseApi'
import { generateKemKeyPair, wrapLevelPrivateKey, toBase64 } from '@/crypto/kem'
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import type { CaseItem } from '@/api/caseApi'

const router = useRouter()
const auth = useAuthStore()
const notify = useNotificationStore()

const cases = ref<CaseItem[]>([])
const loading = ref(false)
const creating = ref(false)
const showCreate = ref(false)

const form = ref({ name: '', description: '', priority: 'Medium' })

const LEVELS = 6

async function fetchCases() {
  loading.value = true
  try {
    const res = await caseApi.listCases()
    if (res.data.success && res.data.data) cases.value = res.data.data.items
  } catch {
    notify.error('Failed to load cases')
  } finally {
    loading.value = false
  }
}

async function handleCreate() {
  if (!form.value.name.trim()) return
  if (!auth.kemPublicKey) { notify.error('Encryption key not available — please log in again'); return }

  creating.value = true
  try {
    // Generate 6 ML-KEM-768 key pairs (one per access level)
    const levelKeyPairs = await Promise.all(
      Array.from({ length: LEVELS }, (_, i) => i + 1).map(async (level) => {
        const { publicKey, secretKey } = generateKemKeyPair()
        const wrapped = await wrapLevelPrivateKey(secretKey, auth.kemPublicKey!)
        return {
          level,
          publicKey: toBase64(publicKey),
          wrappedPrivateKey: {
            kemCiphertext: wrapped.kemCiphertext,
            encryptedLevelPrivKey: wrapped.encryptedLevelPrivKey,
            nonce: wrapped.nonce
          }
        }
      })
    )

    const res = await caseApi.createCase({
      name: form.value.name.trim(),
      description: form.value.description.trim() || undefined,
      priority: form.value.priority,
      levelKeyPairs
    })

    if (res.data.success && res.data.data) {
      notify.success('Case created')
      showCreate.value = false
      form.value = { name: '', description: '', priority: 'Medium' }
      router.push(`/cases/${res.data.data.caseId}`)
    } else {
      notify.error(res.data.message || 'Failed to create case')
    }
  } catch (e) {
    console.error(e)
    notify.error('Failed to create case')
  } finally {
    creating.value = false
  }
}

function priorityColor(p: string) {
  if (p === 'Critical') return '#dc2626'
  if (p === 'High') return '#f59e0b'
  if (p === 'Medium') return '#3b82f6'
  return '#6b7280'
}

onMounted(fetchCases)
</script>

<template>
  <div class="page">
    <div class="page-header">
      <h1>Cases</h1>
      <button class="btn-primary" @click="showCreate = true">+ New Case</button>
    </div>

    <LoadingSpinner v-if="loading" />

    <div v-else-if="cases.length === 0" class="empty">
      <p>No cases yet.</p>
      <button class="btn-primary" @click="showCreate = true">Create your first case</button>
    </div>

    <div v-else class="case-grid">
      <div
        v-for="c in cases"
        :key="c.caseId"
        class="case-card"
        @click="router.push(`/cases/${c.caseId}`)"
      >
        <div class="case-card-top">
          <span class="case-name">{{ c.name }}</span>
          <span class="priority-badge" :style="{ color: priorityColor(c.priority) }">{{ c.priority }}</span>
        </div>
        <div v-if="c.description" class="case-desc">{{ c.description }}</div>
        <div class="case-meta">
          <span class="status-badge">{{ c.status }}</span>
          <span v-if="c.dueDate" class="due-date">Due {{ new Date(c.dueDate).toLocaleDateString() }}</span>
        </div>
      </div>
    </div>
  </div>

  <!-- Create modal -->
  <Teleport to="body">
    <div v-if="showCreate" class="modal-backdrop" @click.self="showCreate = false">
      <div class="modal">
        <h3>New Case</h3>
        <div class="form-group">
          <label class="form-label">Name *</label>
          <input v-model="form.name" class="field" placeholder="Case name" @keyup.enter="handleCreate" />
        </div>
        <div class="form-group">
          <label class="form-label">Description</label>
          <textarea v-model="form.description" class="field field-textarea" rows="3" placeholder="Optional description"></textarea>
        </div>
        <div class="form-group">
          <label class="form-label">Priority</label>
          <select v-model="form.priority" class="field">
            <option value="Low">Low</option>
            <option value="Medium">Medium</option>
            <option value="High">High</option>
            <option value="Critical">Critical</option>
          </select>
        </div>
        <p class="hint">6 encryption key pairs will be generated client-side.</p>
        <div class="modal-actions">
          <button class="btn-primary-sm" :disabled="creating || !form.name.trim()" @click="handleCreate">
            {{ creating ? 'Creating…' : 'Create' }}
          </button>
          <button class="btn-secondary-sm" @click="showCreate = false">Cancel</button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 1.5rem;
}
.page-header h1 { margin: 0; }

.case-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 1rem;
}

.case-card {
  background: white;
  border-radius: 8px;
  padding: 1.25rem 1.5rem;
  box-shadow: 0 1px 3px rgba(0,0,0,0.1);
  cursor: pointer;
  transition: box-shadow 0.15s, transform 0.15s;
}
.case-card:hover { box-shadow: 0 4px 12px rgba(0,0,0,0.12); transform: translateY(-1px); }

.case-card-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 0.3rem;
}

.case-name { font-weight: 600; font-size: 1rem; }
.priority-badge { font-size: 0.75rem; font-weight: 600; }
.case-desc { font-size: 0.85rem; color: #6b7280; margin-bottom: 0.5rem; }

.case-meta { display: flex; align-items: center; gap: 0.75rem; margin-top: 0.5rem; }
.status-badge {
  font-size: 0.75rem;
  padding: 0.15rem 0.5rem;
  background: #e0e7ff;
  color: #4338ca;
  border-radius: 999px;
  font-weight: 500;
}
.due-date { font-size: 0.75rem; color: #6b7280; }

.empty { text-align: center; padding: 4rem 0; color: #6b7280; }
.empty p { margin-bottom: 1rem; }

.hint { font-size: 0.8rem; color: #9ca3af; margin: 0 0 1rem; }

.form-group { margin-bottom: 1rem; }
.form-label { display: block; margin-bottom: 0.375rem; font-size: 0.875rem; font-weight: 500; color: #374151; }

.field {
  width: 100%;
  padding: 0.5rem 0.75rem;
  border: 1px solid #d1d5db;
  border-radius: 6px;
  font-size: 0.875rem;
  box-sizing: border-box;
}
.field:focus { outline: none; border-color: #667eea; box-shadow: 0 0 0 3px rgba(102,126,234,0.15); }
.field-textarea { font-family: inherit; resize: vertical; }

.modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0,0,0,0.45);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 100;
}
.modal { background: white; border-radius: 10px; padding: 1.75rem; width: 400px; box-shadow: 0 8px 32px rgba(0,0,0,0.18); }
.modal h3 { margin: 0 0 1.25rem; font-size: 1.1rem; }
.modal-actions { display: flex; gap: 0.5rem; justify-content: flex-end; margin-top: 1.25rem; }

.btn-primary {
  padding: 0.5rem 1rem;
  background: #667eea;
  color: white;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  font-size: 0.9rem;
}
.btn-primary:hover { background: #5a6fd6; }
.btn-primary:disabled { opacity: 0.6; cursor: not-allowed; }

.btn-primary-sm {
  padding: 0.375rem 0.875rem;
  background: #667eea;
  color: white;
  border: none;
  border-radius: 6px;
  font-size: 0.85rem;
  cursor: pointer;
}
.btn-primary-sm:hover { background: #5a6fd6; }
.btn-primary-sm:disabled { opacity: 0.6; cursor: not-allowed; }

.btn-secondary-sm {
  padding: 0.375rem 0.875rem;
  background: #f3f4f6;
  color: #374151;
  border: 1px solid #d1d5db;
  border-radius: 6px;
  font-size: 0.85rem;
  cursor: pointer;
}
</style>
