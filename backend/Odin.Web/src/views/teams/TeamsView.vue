<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useNotificationStore } from '@/stores/notification'
import { teamApi } from '@/api/teamApi'
import { encryptTeamKey } from '@/crypto/kem'
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import type { TeamItem } from '@/api/teamApi'

const router = useRouter()
const auth = useAuthStore()
const notify = useNotificationStore()

const teams = ref<TeamItem[]>([])
const loading = ref(false)
const creating = ref(false)
const showCreate = ref(false)

const form = ref({ name: '', description: '' })

// Edit
const showEdit = ref(false)
const editingTeam = ref<TeamItem | null>(null)
const editForm = ref({ name: '', description: '' })
const saving = ref(false)

async function fetchTeams() {
  loading.value = true
  try {
    const res = await teamApi.listTeams()
    if (res.data.success && res.data.data) teams.value = res.data.data.items
  } catch {
    notify.error('Failed to load teams')
  } finally {
    loading.value = false
  }
}

async function handleCreate() {
  if (!form.value.name.trim()) return
  if (!auth.kemPublicKey) { notify.error('Key not available — please log in again'); return }

  creating.value = true
  try {
    const { teamKey: _, kemCiphertext, encryptedKey, nonce } = await encryptTeamKey(auth.kemPublicKey)
    const res = await teamApi.createTeam({
      name: form.value.name.trim(),
      description: form.value.description.trim() || undefined,
      kemCiphertext,
      encryptedKey,
      nonce
    })
    if (res.data.success && res.data.data) {
      notify.success('Team created')
      showCreate.value = false
      form.value = { name: '', description: '' }
      router.push(`/teams/${res.data.data.teamId}`)
    } else {
      notify.error(res.data.message || 'Failed to create team')
    }
  } catch {
    notify.error('Failed to create team')
  } finally {
    creating.value = false
  }
}

function openEdit(team: TeamItem, event: Event) {
  event.stopPropagation()
  editingTeam.value = team
  editForm.value = { name: team.name, description: team.description || '' }
  showEdit.value = true
}

async function handleEdit() {
  if (!editingTeam.value || !editForm.value.name.trim()) return
  saving.value = true
  try {
    const res = await teamApi.updateTeam(editingTeam.value.teamId, {
      name: editForm.value.name.trim(),
      description: editForm.value.description.trim() || undefined
    })
    if (res.data.success) {
      notify.success('Team updated')
      showEdit.value = false
      await fetchTeams()
    } else {
      notify.error(res.data.message || 'Failed to update team')
    }
  } catch {
    notify.error('Failed to update team')
  } finally {
    saving.value = false
  }
}

onMounted(fetchTeams)
</script>

<template>
  <div class="page">
    <div class="page-header">
      <h1>Teams</h1>
      <button class="btn-primary" @click="showCreate = true">+ New Team</button>
    </div>

    <LoadingSpinner v-if="loading" />

    <div v-else-if="teams.length === 0" class="empty">
      <p>You are not a member of any team yet.</p>
      <button class="btn-primary" @click="showCreate = true">Create your first team</button>
    </div>

    <div v-else class="team-grid">
      <div
        v-for="team in teams"
        :key="team.teamId"
        class="team-card"
        @click="router.push(`/teams/${team.teamId}`)"
      >
        <div class="team-card-top">
          <div class="team-card-name">{{ team.name }}</div>
          <button class="btn-edit" title="Edit team" @click="openEdit(team, $event)">✎</button>
        </div>
        <div v-if="team.description" class="team-card-desc">{{ team.description }}</div>
      </div>
    </div>
  </div>

  <!-- Create modal -->
  <Teleport to="body">
    <div v-if="showCreate" class="modal-backdrop" @click.self="showCreate = false">
      <div class="modal">
        <h3>Create Team</h3>
        <div class="form-group">
          <label class="form-label">Name</label>
          <input v-model="form.name" class="field" placeholder="Team name" @keyup.enter="handleCreate" />
        </div>
        <div class="form-group">
          <label class="form-label">Description</label>
          <textarea v-model="form.description" class="field field-textarea" rows="3" placeholder="Optional description"></textarea>
        </div>
        <div class="modal-actions">
          <button class="btn-primary-sm" :disabled="creating || !form.name.trim()" @click="handleCreate">
            {{ creating ? 'Creating…' : 'Create' }}
          </button>
          <button class="btn-secondary-sm" @click="showCreate = false">Cancel</button>
        </div>
      </div>
    </div>
  </Teleport>

  <!-- Edit modal -->
  <Teleport to="body">
    <div v-if="showEdit" class="modal-backdrop" @click.self="showEdit = false">
      <div class="modal">
        <h3>Edit Team</h3>
        <div class="form-group">
          <label class="form-label">Name</label>
          <input v-model="editForm.name" class="field" placeholder="Team name" @keyup.enter="handleEdit" />
        </div>
        <div class="form-group">
          <label class="form-label">Description</label>
          <textarea v-model="editForm.description" class="field field-textarea" rows="3" placeholder="Optional description"></textarea>
        </div>
        <div class="modal-actions">
          <button class="btn-primary-sm" :disabled="saving || !editForm.name.trim()" @click="handleEdit">
            {{ saving ? 'Saving…' : 'Save' }}
          </button>
          <button class="btn-secondary-sm" @click="showEdit = false">Cancel</button>
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

.team-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
  gap: 1rem;
}

.team-card {
  background: white;
  border-radius: 8px;
  padding: 1.25rem 1.5rem;
  box-shadow: 0 1px 3px rgba(0,0,0,0.1);
  cursor: pointer;
  transition: box-shadow 0.15s, transform 0.15s;
}

.team-card:hover {
  box-shadow: 0 4px 12px rgba(0,0,0,0.12);
  transform: translateY(-1px);
}

.team-card-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 0.25rem;
}

.team-card-name { font-weight: 600; font-size: 1rem; }
.team-card-desc { font-size: 0.85rem; color: #6b7280; }

.btn-edit {
  background: none;
  border: none;
  color: #9ca3af;
  cursor: pointer;
  font-size: 1rem;
  padding: 0.1rem 0.3rem;
  border-radius: 4px;
  line-height: 1;
}

.btn-edit:hover { color: #667eea; background: #eef2ff; }

.empty {
  text-align: center;
  padding: 4rem 0;
  color: #6b7280;
}

.empty p { margin-bottom: 1rem; }

.form-group { margin-bottom: 1rem; }

.form-label {
  display: block;
  margin-bottom: 0.375rem;
  font-size: 0.875rem;
  font-weight: 500;
  color: #374151;
}

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

.modal {
  background: white;
  border-radius: 10px;
  padding: 1.75rem;
  width: 380px;
  box-shadow: 0 8px 32px rgba(0,0,0,0.18);
}

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
