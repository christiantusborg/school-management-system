<script setup lang="ts">
import { ref, onMounted, watch, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useNotificationStore } from '@/stores/notification'
import { teamApi } from '@/api/teamApi'
import { encryptTeamKey, decryptTeamKey, wrapTeamKey, fromBase64 } from '@/crypto/kem'
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import type { MemberItem, UserSearchItem } from '@/api/teamApi'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const notify = useNotificationStore()

const teamId = route.params.id as string

const teamName = ref('')
const members = ref<MemberItem[]>([])
const loading = ref(false)

// Add member modal
const showAdd = ref(false)
const adding = ref(false)

const TEAM_ROLES = [
  { id: '76A48B85-535E-12C7-EF67-2A9570474B1D', name: 'Member' },
  { id: '51C27B7B-E12E-DCFE-D76B-DC68926D4F7B', name: 'Administrator' },
]
const selectedRoleId = ref(TEAM_ROLES[0].id)

// User search
const searchQuery = ref('')
const searchResults = ref<UserSearchItem[]>([])
const searching = ref(false)
const selectedUser = ref<UserSearchItem | null>(null)

// Remove confirm
const removingUserId = ref<string | null>(null)

let searchTimeout: ReturnType<typeof setTimeout> | null = null

watch(searchQuery, (val) => {
  selectedUser.value = null
  if (searchTimeout) clearTimeout(searchTimeout)
  if (!val.trim()) { searchResults.value = []; return }
  searchTimeout = setTimeout(() => doSearch(val.trim()), 300)
})

async function doSearch(q: string) {
  console.log('[doSearch] query:', q)
  searching.value = true
  try {
    const res = await teamApi.searchUsers(q)
    console.log('[doSearch] response:', res.data)
    if (res.data.success && res.data.data) {
      searchResults.value = res.data.data.items
      console.log('[doSearch] results count:', res.data.data.items.length)
    } else {
      console.warn('[doSearch] failed:', res.data)
    }
  } catch (e) {
    console.error('[doSearch] error:', e)
    searchResults.value = []
  } finally {
    searching.value = false
  }
}

function selectUser(user: UserSearchItem) {
  console.log('[selectUser]', user)
  selectedUser.value = user
  searchQuery.value = user.username
  searchResults.value = []
}

function openAdd() {
  console.log('[openAdd] teamId:', teamId, 'kemPrivateKey available:', !!auth.kemPrivateKey)
  searchQuery.value = ''
  searchResults.value = []
  selectedUser.value = null
  selectedRoleId.value = TEAM_ROLES[0].id
  showAdd.value = true
}

async function fetchTeam() {
  console.log('[fetchTeam] teamId:', teamId)
  loading.value = true
  try {
    const res = await teamApi.listMembers(teamId)
    console.log('[fetchTeam] listMembers response:', res.data)
    if (res.data.success && res.data.data) {
      members.value = res.data.data.items
    }
    const teamsRes = await teamApi.listTeams()
    if (teamsRes.data.success && teamsRes.data.data) {
      const found = teamsRes.data.data.items.find(t => t.teamId === teamId)
      if (found) teamName.value = found.name
    }
  } catch (e) {
    console.error('[fetchTeam] error:', e)
    notify.error('Failed to load team')
  } finally {
    loading.value = false
  }
}

async function handleAddMember() {
  console.log('[handleAddMember] START — selectedUser:', selectedUser.value, 'roleId:', selectedRoleId.value)
  console.log('[handleAddMember] kemPrivateKey available:', !!auth.kemPrivateKey, 'length:', auth.kemPrivateKey?.length)
  if (!selectedUser.value) { console.warn('[handleAddMember] no user selected'); return }
  if (!auth.kemPrivateKey) { notify.error('Key not available — please log in again'); return }

  adding.value = true
  try {
    // Step 1: fetch OR generate admin's team symmetric key
    // Axios throws on 4xx, so we catch the 404 here and treat it as "no key yet"
    console.log('[handleAddMember] STEP 1 — fetching team symmetric key for teamId:', teamId)
    let teamKey: Uint8Array

    let existingKeyData: { kemCiphertext: string; encryptedKey: string; nonce: string } | null = null
    try {
      const keyRes = await teamApi.getSymmetricKey(teamId)
      console.log('[handleAddMember] STEP 1 response — status:', keyRes.status, 'data:', keyRes.data)
      if (keyRes.data.success && keyRes.data.data) {
        existingKeyData = keyRes.data.data
      }
    } catch (e: any) {
      const status = e?.response?.status
      console.warn('[handleAddMember] STEP 1 — getSymmetricKey threw, status:', status, e?.response?.data)
      if (status !== 404 && status !== 400) {
        // Unexpected error — bail out
        console.error('[handleAddMember] STEP 1 UNEXPECTED ERROR')
        notify.error('Could not fetch team key'); return
      }
      // 404/400 = no key for this user yet, will generate below
    }

    if (!existingKeyData) {
      // No key found — generate a fresh team key for this admin
      console.warn('[handleAddMember] STEP 1 — no existing key, generating fresh team key')
      const fresh = await encryptTeamKey(auth.kemPublicKey!)
      console.log('[handleAddMember] STEP 1 — saving fresh team key to backend')
      const saveRes = await teamApi.saveSymmetricKey(teamId, {
        kemCiphertext: fresh.kemCiphertext,
        encryptedKey: fresh.encryptedKey,
        nonce: fresh.nonce
      })
      console.log('[handleAddMember] STEP 1 save response:', saveRes.data)
      if (!saveRes.data.success) {
        console.error('[handleAddMember] STEP 1 FAILED — could not save fresh team key')
        notify.error('Could not save team key'); return
      }
      teamKey = fresh.teamKey
      console.log('[handleAddMember] STEP 1 OK — fresh teamKey length:', teamKey.length)
    } else {
      const { kemCiphertext, encryptedKey, nonce } = existingKeyData
      console.log('[handleAddMember] STEP 1 OK — kemCiphertext length:', kemCiphertext?.length, 'encryptedKey length:', encryptedKey?.length, 'nonce length:', nonce?.length)

      // Step 2: decrypt team key using admin's private key
      console.log('[handleAddMember] STEP 2 — decrypting team key')
      try {
        teamKey = await decryptTeamKey(kemCiphertext, encryptedKey, nonce, auth.kemPrivateKey)
        console.log('[handleAddMember] STEP 2 OK — teamKey length:', teamKey.length)
      } catch (e) {
        // Decryption failed (stale key from old KEM pair) — generate a fresh key
        console.warn('[handleAddMember] STEP 2 — decryption failed, regenerating team key:', e)
        const fresh = await encryptTeamKey(auth.kemPublicKey!)
        const saveRes = await teamApi.saveSymmetricKey(teamId, {
          kemCiphertext: fresh.kemCiphertext,
          encryptedKey: fresh.encryptedKey,
          nonce: fresh.nonce
        })
        console.log('[handleAddMember] STEP 2 regen save response:', saveRes.data)
        if (!saveRes.data.success) {
          console.error('[handleAddMember] STEP 2 FAILED — could not save regenerated key')
          notify.error('Could not regenerate team key'); return
        }
        teamKey = fresh.teamKey
        console.log('[handleAddMember] STEP 2 OK (regen) — fresh teamKey length:', teamKey.length)
      }
    }

    // Step 3: fetch new member's public key
    console.log('[handleAddMember] STEP 3 — fetching public key for userId:', selectedUser.value.userId)
    const pubKeyRes = await teamApi.getUserPublicKey(selectedUser.value.userId)
    console.log('[handleAddMember] STEP 3 response — status:', pubKeyRes.status, 'data:', pubKeyRes.data)
    if (!pubKeyRes.data.success || !pubKeyRes.data.data) {
      console.error('[handleAddMember] STEP 3 FAILED — user has no KEM public key')
      notify.error('User not found or has no key'); return
    }
    const newMemberPublicKey = fromBase64(pubKeyRes.data.data.publicKey)
    console.log('[handleAddMember] STEP 3 OK — publicKey length:', newMemberPublicKey.length)

    // Step 4: wrap team key for new member
    console.log('[handleAddMember] STEP 4 — wrapping team key for new member')
    const wrapped = await wrapTeamKey(teamKey, newMemberPublicKey)
    console.log('[handleAddMember] STEP 4 OK — wrapped kemCiphertext length:', wrapped.kemCiphertext.length)

    // Step 5: add member
    const payload = {
      userId: selectedUser.value.userId,
      teamRoleId: selectedRoleId.value,
      kemCiphertext: wrapped.kemCiphertext,
      encryptedKey: wrapped.encryptedKey,
      nonce: wrapped.nonce
    }
    console.log('[handleAddMember] STEP 5 — posting addMember payload:', { ...payload, kemCiphertext: payload.kemCiphertext.substring(0, 20) + '…' })
    const res = await teamApi.addMember(teamId, payload)
    console.log('[handleAddMember] STEP 5 response — status:', res.status, 'data:', res.data)

    if (res.data.success) {
      console.log('[handleAddMember] SUCCESS')
      notify.success('Member added')
      showAdd.value = false
      selectedUser.value = null
      searchQuery.value = ''
      await fetchTeam()
    } else {
      console.error('[handleAddMember] backend rejected:', res.data.message)
      notify.error(res.data.message || 'Failed to add member')
    }
  } catch (e) {
    console.error('[handleAddMember] CAUGHT EXCEPTION:', e)
    notify.error('Failed to add member')
  } finally {
    adding.value = false
  }
}

async function handleRemove(userId: string) {
  removingUserId.value = userId
  try {
    const res = await teamApi.removeMember(teamId, userId)
    if (res.data.success) {
      notify.success('Member removed')
      await fetchTeam()
    } else {
      notify.error(res.data.message || 'Failed to remove member')
    }
  } catch {
    notify.error('Failed to remove member')
  } finally {
    removingUserId.value = null
  }
}

function displayName(u: { firstName?: string; lastName?: string; username?: string; userId?: string }) {
  const parts = [u.firstName, u.lastName].filter(Boolean)
  const full = parts.length ? parts.join(' ') : null
  const uname = u.username || u.userId || ''
  return full ? `${full} (${uname})` : uname
}

const currentUserId = auth.user?.userId

const ADMIN_ROLE_ID = '51C27B7B-E12E-DCFE-D76B-DC68926D4F7B'

const isAdmin = computed(() =>
  members.value.some(
    m => m.userId === currentUserId &&
         m.teamRoleId?.toUpperCase() === ADMIN_ROLE_ID.toUpperCase()
  )
)

onMounted(fetchTeam)
</script>

<template>
  <div class="page">
    <div class="page-header">
      <button class="btn-back" @click="router.push('/teams')">← Teams</button>
      <h1>{{ teamName || 'Team' }}</h1>
    </div>

    <LoadingSpinner v-if="loading" />

    <template v-else>
      <div class="card">
        <div class="card-header">
          <h2>Members</h2>
          <button v-if="isAdmin" class="btn-primary-sm" @click="openAdd">+ Add Member</button>
        </div>

        <div v-if="members.length === 0" class="empty-members">No members found.</div>

        <table v-else class="data-table">
          <thead>
            <tr>
              <th>User</th>
              <th>Role</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="member in members" :key="member.teamMemberId">
              <td class="user-cell">
                <span class="user-name">{{ displayName(member) }}</span>
                <span v-if="member.email" class="user-email">{{ member.email }}</span>
                <span class="user-id">{{ member.userId }}</span>
                <span v-if="member.userId === currentUserId" class="badge-you">you</span>
              </td>
              <td>
                <span class="badge-role">{{ member.roleName || member.teamRoleId }}</span>
                <span v-if="member.roleName" class="role-id">{{ member.teamRoleId }}</span>
              </td>
              <td>
                <button
                  v-if="isAdmin && member.userId !== currentUserId"
                  class="btn-danger-sm"
                  :disabled="removingUserId === member.userId"
                  @click="handleRemove(member.userId)"
                >
                  {{ removingUserId === member.userId ? 'Removing…' : 'Remove' }}
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </template>
  </div>

  <!-- Add member modal -->
  <Teleport to="body">
    <div v-if="showAdd" class="modal-backdrop" @click.self="showAdd = false">
      <div class="modal">
        <h3>Add Member</h3>
        <p class="modal-hint">
          Search for a user by username. Their copy of the team key will be encrypted with their public key.
        </p>
        <div class="form-group search-group">
          <label class="form-label">Search User</label>
          <div class="search-wrap">
            <input
              v-model="searchQuery"
              class="field"
              placeholder="Type a username…"
              autocomplete="off"
            />
            <span v-if="searching" class="search-spinner">⟳</span>
          </div>
          <div v-if="searchResults.length > 0" class="search-results">
            <button
              v-for="user in searchResults"
              :key="user.userId"
              class="search-result-item"
              @click="selectUser(user)"
            >
              <span class="result-name">{{ displayName(user) }}</span>
              <span v-if="user.email" class="result-email">{{ user.email }}</span>
              <span class="result-id">{{ user.userId }}</span>
            </button>
          </div>
          <div v-if="selectedUser" class="selected-user">
            <span class="selected-label">Selected:</span>
            <span class="selected-name">{{ displayName(selectedUser) }}</span>
            <span v-if="selectedUser.email" class="selected-email">{{ selectedUser.email }}</span>
          </div>
        </div>
        <div class="form-group">
          <label class="form-label">Role</label>
          <select v-model="selectedRoleId" class="field">
            <option v-for="role in TEAM_ROLES" :key="role.id" :value="role.id">{{ role.name }}</option>
          </select>
        </div>
        <div class="modal-actions">
          <button
            class="btn-primary-sm"
            :disabled="adding || !selectedUser"
            @click="handleAddMember"
          >
            {{ adding ? 'Adding…' : 'Add Member' }}
          </button>
          <button class="btn-secondary-sm" @click="showAdd = false">Cancel</button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
.page-header {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.page-header h1 { margin: 0; }

.btn-back {
  background: none;
  border: none;
  color: #667eea;
  cursor: pointer;
  font-size: 0.9rem;
  padding: 0;
}

.btn-back:hover { text-decoration: underline; }

.card {
  background: white;
  border-radius: 8px;
  box-shadow: 0 1px 3px rgba(0,0,0,0.1);
  overflow: hidden;
}

.card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1.25rem 1.5rem;
  border-bottom: 1px solid #e5e7eb;
}

.card-header h2 { margin: 0; font-size: 1rem; }

.data-table {
  width: 100%;
  border-collapse: collapse;
}

.data-table th,
.data-table td {
  padding: 0.75rem 1.5rem;
  text-align: left;
  border-bottom: 1px solid #f3f4f6;
}

.data-table th {
  background: #f9fafb;
  font-size: 0.8rem;
  font-weight: 600;
  color: #6b7280;
  text-transform: uppercase;
}

.user-cell {
  display: flex;
  flex-direction: column;
  gap: 0.1rem;
}

.user-name { font-weight: 500; font-size: 0.9rem; color: #111827; }
.user-email { font-size: 0.78rem; color: #6b7280; }
.user-id { font-family: monospace; font-size: 0.72rem; color: #d1d5db; }

.role-id { display: block; font-family: monospace; font-size: 0.72rem; color: #9ca3af; margin-top: 0.1rem; }

.badge-you {
  display: inline-block;
  padding: 1px 7px;
  background: #eef2ff;
  color: #4338ca;
  border-radius: 99px;
  font-size: 0.72rem;
  font-weight: 500;
}

.badge-role {
  display: inline-block;
  padding: 2px 8px;
  background: #f3f4f6;
  color: #374151;
  border-radius: 99px;
  font-size: 0.78rem;
}

.empty-members {
  padding: 2rem 1.5rem;
  color: #9ca3af;
  font-size: 0.9rem;
}

.form-group { margin-bottom: 1rem; }

.search-group { position: relative; }

.form-label {
  display: block;
  margin-bottom: 0.375rem;
  font-size: 0.875rem;
  font-weight: 500;
  color: #374151;
}

.search-wrap { position: relative; }

.field {
  width: 100%;
  padding: 0.5rem 0.75rem;
  border: 1px solid #d1d5db;
  border-radius: 6px;
  font-size: 0.875rem;
  box-sizing: border-box;
}

.field:focus { outline: none; border-color: #667eea; box-shadow: 0 0 0 3px rgba(102,126,234,0.15); }
select.field { appearance: auto; background: white; }

.search-spinner {
  position: absolute;
  right: 0.6rem;
  top: 50%;
  transform: translateY(-50%);
  color: #9ca3af;
  font-size: 1rem;
  animation: spin 1s linear infinite;
}

@keyframes spin { to { transform: translateY(-50%) rotate(360deg); } }

.search-results {
  border: 1px solid #d1d5db;
  border-top: none;
  border-radius: 0 0 6px 6px;
  background: white;
  max-height: 180px;
  overflow-y: auto;
  box-shadow: 0 4px 8px rgba(0,0,0,0.08);
}

.search-result-item {
  display: flex;
  flex-direction: column;
  padding: 0.5rem 0.75rem;
  width: 100%;
  text-align: left;
  background: none;
  border: none;
  border-bottom: 1px solid #f3f4f6;
  cursor: pointer;
}

.search-result-item:last-child { border-bottom: none; }
.search-result-item:hover { background: #f9fafb; }

.result-name { font-size: 0.875rem; font-weight: 500; color: #111827; }
.result-email { font-size: 0.78rem; color: #6b7280; }
.result-id { font-size: 0.72rem; font-family: monospace; color: #d1d5db; }

.selected-user {
  margin-top: 0.5rem;
  padding: 0.4rem 0.6rem;
  background: #eef2ff;
  border-radius: 6px;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.82rem;
}

.selected-label { color: #6b7280; }
.selected-name { font-weight: 600; color: #4338ca; }
.selected-email { font-size: 0.78rem; color: #6b7280; }

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
  width: 420px;
  box-shadow: 0 8px 32px rgba(0,0,0,0.18);
}

.modal h3 { margin: 0 0 0.5rem; font-size: 1.1rem; }

.modal-hint {
  font-size: 0.85rem;
  color: #6b7280;
  margin-bottom: 1.25rem;
  line-height: 1.5;
}

.modal-actions { display: flex; gap: 0.5rem; justify-content: flex-end; margin-top: 0.5rem; }

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

.btn-danger-sm {
  padding: 0.3rem 0.7rem;
  background: #fee2e2;
  color: #dc2626;
  border: 1px solid #fca5a5;
  border-radius: 4px;
  font-size: 0.8rem;
  cursor: pointer;
}

.btn-danger-sm:hover { background: #fca5a5; }
.btn-danger-sm:disabled { opacity: 0.6; cursor: not-allowed; }
</style>
