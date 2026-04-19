<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useNotificationStore } from '@/stores/notification'
import { caseApi } from '@/api/caseApi'
import { teamApi } from '@/api/teamApi'
import { decryptLevelPrivateKey, wrapLevelPrivateKey, encryptFileKeyForLevel, fromBase64 } from '@/crypto/kem'
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import type {
  CaseItem, CaseMemberItem, CaseTeamMemberItem,
  CaseFileItem, CaseKeyPairItem, CaseUserKeyItem
} from '@/api/caseApi'
import type { UserSearchItem } from '@/api/teamApi'

const route = useRoute()
const auth = useAuthStore()
const notify = useNotificationStore()

const caseId = route.params.id as string

// ── State ────────────────────────────────────────────────────────────────────
const activeTab = ref<'overview' | 'members' | 'files'>('overview')

const caseData = ref<CaseItem | null>(null)
const loading = ref(true)

// Members
const userMembers = ref<CaseMemberItem[]>([])
const teamMembers = ref<CaseTeamMemberItem[]>([])
const membersLoading = ref(false)

// Files
const files = ref<CaseFileItem[]>([])
const filesLoading = ref(false)

// Key pairs & my keys (loaded lazily when granting)
const keyPairs = ref<CaseKeyPairItem[]>([])
const myLevelKeys = ref<CaseUserKeyItem[]>([])
const myLevel = computed(() => {
  const me = userMembers.value.find(m => m.userId === auth.user?.userId)
  return me?.level ?? null
})

// Grant user modal
const showGrantUser = ref(false)
const grantUserForm = ref({ userId: '', level: 1, searchQuery: '' })
const searchResults = ref<UserSearchItem[]>([])
const searching = ref(false)
const granting = ref(false)

// Grant team modal
const showGrantTeam = ref(false)
const grantTeamForm = ref({ teamId: '', teamName: '', level: 1 })
const granting2 = ref(false)

// File record modal (metadata only — actual file upload out of scope)
const showAddFile = ref(false)
const fileForm = ref({ name: '', contentType: 'application/octet-stream', sizeBytes: 0, storagePath: '', minLevel: 1, accessMode: 'Hierarchical' })
const addingFile = ref(false)

// ── Load ─────────────────────────────────────────────────────────────────────

async function loadCase() {
  try {
    const res = await caseApi.getCase(caseId)
    if (res.data.success && res.data.data) caseData.value = res.data.data
  } catch { notify.error('Failed to load case') }
}

async function loadMembers() {
  membersLoading.value = true
  try {
    const res = await caseApi.listMembers(caseId)
    if (res.data.success && res.data.data) {
      userMembers.value = res.data.data.users
      teamMembers.value = res.data.data.teams
    }
  } catch { notify.error('Failed to load members') } finally { membersLoading.value = false }
}

async function loadFiles() {
  filesLoading.value = true
  try {
    const res = await caseApi.listFiles(caseId)
    if (res.data.success && res.data.data) files.value = res.data.data.items
  } catch { notify.error('Failed to load files') } finally { filesLoading.value = false }
}

async function loadKeyMaterial() {
  if (keyPairs.value.length > 0) return
  try {
    const [kpRes, myRes] = await Promise.all([
      caseApi.getKeyPairs(caseId),
      caseApi.getMyKeys(caseId)
    ])
    if (kpRes.data.success && kpRes.data.data) keyPairs.value = kpRes.data.data.items
    if (myRes.data.success && myRes.data.data) myLevelKeys.value = myRes.data.data.keys
  } catch { notify.error('Failed to load key material') }
}

onMounted(async () => {
  loading.value = true
  await Promise.all([loadCase(), loadMembers()])
  loading.value = false
})

// ── Tab switch ───────────────────────────────────────────────────────────────

async function switchTab(tab: 'overview' | 'members' | 'files') {
  activeTab.value = tab
  if (tab === 'files' && files.value.length === 0) loadFiles()
}

// ── Grant user ───────────────────────────────────────────────────────────────

async function searchUsers() {
  if (grantUserForm.value.searchQuery.length < 2) return
  searching.value = true
  try {
    const res = await teamApi.searchUsers(grantUserForm.value.searchQuery)
    if (res.data.success && res.data.data) searchResults.value = res.data.data.items
  } catch { notify.error('Search failed') } finally { searching.value = false }
}

function selectUser(u: UserSearchItem) {
  grantUserForm.value.userId = u.userId
  grantUserForm.value.searchQuery = u.username || u.email || u.userId
  searchResults.value = []
}

async function openGrantUser() {
  await loadKeyMaterial()
  grantUserForm.value = { userId: '', level: myLevel.value ?? 1, searchQuery: '' }
  showGrantUser.value = true
}

async function handleGrantUser() {
  if (!grantUserForm.value.userId) { notify.error('Select a user'); return }
  if (!auth.kemPrivateKey) { notify.error('Private key not available — please log in again'); return }

  const targetLevel = grantUserForm.value.level

  granting.value = true
  try {
    // 1. Fetch target user's ML-KEM public key
    const pkRes = await teamApi.getUserPublicKey(grantUserForm.value.userId)
    if (!pkRes.data.success || !pkRes.data.data) { notify.error('Failed to get target public key'); return }
    const targetPublicKey = fromBase64(pkRes.data.data.publicKey)

    // 2. Decrypt my level private keys for levels targetLevel..6
    const levelsToGrant = myLevelKeys.value.filter(k => k.level >= targetLevel)
    if (levelsToGrant.length === 0) { notify.error('You do not hold the required level keys'); return }

    const wrappedKeys = await Promise.all(
      levelsToGrant.map(async (k) => {
        const levelPrivKey = await decryptLevelPrivateKey(
          k.kemCiphertext, k.encryptedLevelPrivKey, k.nonce, auth.kemPrivateKey!
        )
        return wrapLevelPrivateKey(levelPrivKey, targetPublicKey)
          .then(w => ({ level: k.level, ...w }))
      })
    )

    const res = await caseApi.grantUser(caseId, {
      targetUserId: grantUserForm.value.userId,
      level: targetLevel,
      wrappedKeys
    })

    if (res.data.success) {
      notify.success('Access granted')
      showGrantUser.value = false
      await loadMembers()
    } else {
      notify.error(res.data.message || 'Failed to grant access')
    }
  } catch (e) {
    console.error(e)
    notify.error('Failed to grant access')
  } finally { granting.value = false }
}

// ── Revoke user ──────────────────────────────────────────────────────────────

async function revokeUser(userId: string) {
  if (!confirm('Revoke this user\'s access?')) return
  try {
    const res = await caseApi.revokeUser(caseId, userId)
    if (res.data.success) {
      notify.success('Access revoked')
      await loadMembers()
    } else {
      notify.error(res.data.message || 'Failed to revoke')
    }
  } catch { notify.error('Failed to revoke') }
}

// ── Grant team ───────────────────────────────────────────────────────────────

async function openGrantTeam() {
  await loadKeyMaterial()
  grantTeamForm.value = { teamId: '', teamName: '', level: myLevel.value ?? 1 }
  showGrantTeam.value = true
}

async function handleGrantTeam() {
  if (!grantTeamForm.value.teamId) { notify.error('Enter a team ID'); return }
  if (!auth.kemPrivateKey) { notify.error('Private key not available'); return }

  const targetLevel = grantTeamForm.value.level

  granting2.value = true
  try {
    // Fetch the team's symmetric key envelope
    const skRes = await teamApi.getSymmetricKey(grantTeamForm.value.teamId)
    if (!skRes.data.success || !skRes.data.data) { notify.error('Failed to get team symmetric key'); return }

    // Decrypt team symmetric key using my personal KEM private key
    const { decryptTeamKey } = await import('@/crypto/kem')
    const teamKey = await decryptTeamKey(
      skRes.data.data.kemCiphertext,
      skRes.data.data.encryptedKey,
      skRes.data.data.nonce,
      auth.kemPrivateKey!
    )

    // Decrypt my level private keys and re-encrypt with team symmetric key (AES-256-GCM)
    const levelsToGrant = myLevelKeys.value.filter(k => k.level >= targetLevel)
    if (levelsToGrant.length === 0) { notify.error('You do not hold the required level keys'); return }

    const teamWrappedKeys = await Promise.all(
      levelsToGrant.map(async (k) => {
        const levelPrivKey = await decryptLevelPrivateKey(
          k.kemCiphertext, k.encryptedLevelPrivKey, k.nonce, auth.kemPrivateKey!
        )
        const nonce = crypto.getRandomValues(new Uint8Array(12))
        const cryptoKey = await crypto.subtle.importKey('raw', teamKey, { name: 'AES-GCM' }, false, ['encrypt'])
        const ciphertext = await crypto.subtle.encrypt({ name: 'AES-GCM', iv: nonce }, cryptoKey, levelPrivKey)
        const { toBase64 } = await import('@/crypto/kem')
        return {
          level: k.level,
          encryptedLevelPrivKey: toBase64(new Uint8Array(ciphertext)),
          nonce: toBase64(nonce)
        }
      })
    )

    const res = await caseApi.grantTeam(caseId, {
      teamId: grantTeamForm.value.teamId,
      level: targetLevel,
      teamWrappedKeys
    })

    if (res.data.success) {
      notify.success('Team access granted')
      showGrantTeam.value = false
      await loadMembers()
    } else {
      notify.error(res.data.message || 'Failed to grant team access')
    }
  } catch (e) {
    console.error(e)
    notify.error('Failed to grant team access')
  } finally { granting2.value = false }
}

async function revokeTeam(teamId: string) {
  if (!confirm('Revoke this team\'s access?')) return
  try {
    const res = await caseApi.revokeTeam(caseId, teamId)
    if (res.data.success) { notify.success('Team access revoked'); await loadMembers() }
    else notify.error(res.data.message || 'Failed')
  } catch { notify.error('Failed') }
}

// ── Add file ─────────────────────────────────────────────────────────────────

async function openAddFile() {
  await loadKeyMaterial()
  fileForm.value = { name: '', contentType: 'application/octet-stream', sizeBytes: 0, storagePath: '', minLevel: myLevel.value ?? 1, accessMode: 'Hierarchical' }
  showAddFile.value = true
}

async function handleAddFile() {
  if (!fileForm.value.name.trim() || !fileForm.value.storagePath.trim()) {
    notify.error('Name and storage path are required')
    return
  }

  addingFile.value = true
  try {
    const minLevel = fileForm.value.minLevel
    const fileKey = crypto.getRandomValues(new Uint8Array(32))

    // Encrypt file key for each qualifying level (1 through minLevel)
    const qualifyingLevels = keyPairs.value.filter(kp => kp.level <= minLevel)
    const levelKeys = await Promise.all(
      qualifyingLevels.map(async (kp) => {
        const pub = fromBase64(kp.publicKey)
        const enc = await encryptFileKeyForLevel(fileKey, pub)
        return { level: kp.level, ...enc }
      })
    )

    const res = await caseApi.createFile(caseId, {
      name: fileForm.value.name.trim(),
      contentType: fileForm.value.contentType,
      sizeBytes: fileForm.value.sizeBytes,
      storagePath: fileForm.value.storagePath.trim(),
      minLevel,
      accessMode: fileForm.value.accessMode,
      levelKeys
    })

    if (res.data.success) {
      notify.success('File record added')
      showAddFile.value = false
      await loadFiles()
    } else {
      notify.error(res.data.message || 'Failed to add file')
    }
  } catch (e) {
    console.error(e)
    notify.error('Failed to add file')
  } finally { addingFile.value = false }
}
</script>

<template>
  <div class="page">
    <LoadingSpinner v-if="loading" />

    <template v-else-if="caseData">
      <div class="case-header">
        <div>
          <h1>{{ caseData.name }}</h1>
          <p v-if="caseData.description" class="sub">{{ caseData.description }}</p>
        </div>
        <div class="badges">
          <span class="badge status">{{ caseData.status }}</span>
          <span class="badge priority">{{ caseData.priority }}</span>
        </div>
      </div>

      <!-- Tabs -->
      <div class="tabs">
        <button :class="['tab', activeTab === 'overview' && 'active']" @click="switchTab('overview')">Overview</button>
        <button :class="['tab', activeTab === 'members' && 'active']" @click="switchTab('members')">Members</button>
        <button :class="['tab', activeTab === 'files' && 'active']" @click="switchTab('files')">Files</button>
      </div>

      <!-- Overview tab -->
      <div v-if="activeTab === 'overview'" class="tab-content">
        <div class="info-grid">
          <div class="info-item"><span class="info-label">Status</span><span>{{ caseData.status }}</span></div>
          <div class="info-item"><span class="info-label">Priority</span><span>{{ caseData.priority }}</span></div>
          <div class="info-item"><span class="info-label">Due Date</span><span>{{ caseData.dueDate ? new Date(caseData.dueDate).toLocaleDateString() : '—' }}</span></div>
          <div class="info-item"><span class="info-label">My Level</span><span>{{ myLevel !== null ? `Level ${myLevel}` : 'Not a member' }}</span></div>
        </div>
        <div class="level-legend">
          <h3>Access Levels</h3>
          <div v-for="n in 6" :key="n" class="level-row">
            <span class="level-num">L{{ n }}</span>
            <span class="level-name">{{ ['Lead Partner', 'Senior Partner', 'Associate', 'Paralegal', 'Secretary', 'External Consultant'][n - 1] }}</span>
            <span v-if="myLevel !== null && myLevel <= n" class="level-has">✓ You have access</span>
          </div>
        </div>
      </div>

      <!-- Members tab -->
      <div v-if="activeTab === 'members'" class="tab-content">
        <div class="section-header">
          <h2>Users</h2>
          <div v-if="myLevel !== null" class="actions">
            <button class="btn-primary-sm" @click="openGrantUser">+ Grant User</button>
            <button class="btn-secondary-sm" @click="openGrantTeam">+ Grant Team</button>
          </div>
        </div>

        <LoadingSpinner v-if="membersLoading" />

        <table v-else-if="userMembers.length > 0" class="table">
          <thead>
            <tr>
              <th>User</th>
              <th>Level</th>
              <th>Granted</th>
              <th v-if="myLevel === 1"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="m in userMembers" :key="m.caseUserMemberId">
              <td>{{ m.username || m.email || m.userId }}</td>
              <td>Level {{ m.level }}</td>
              <td>{{ new Date(m.grantedAt).toLocaleDateString() }}</td>
              <td v-if="myLevel === 1">
                <button
                  v-if="m.userId !== auth.user?.userId"
                  class="btn-danger-sm"
                  @click="revokeUser(m.userId)"
                >Revoke</button>
              </td>
            </tr>
          </tbody>
        </table>
        <p v-else class="empty-small">No user members.</p>

        <div class="section-header" style="margin-top: 2rem;">
          <h2>Teams</h2>
        </div>
        <table v-if="teamMembers.length > 0" class="table">
          <thead>
            <tr>
              <th>Team</th>
              <th>Level</th>
              <th>Granted</th>
              <th v-if="myLevel === 1"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="t in teamMembers" :key="t.caseTeamMembershipId">
              <td>{{ t.teamName || t.teamId }}</td>
              <td>Level {{ t.level }}</td>
              <td>{{ new Date(t.grantedAt).toLocaleDateString() }}</td>
              <td v-if="myLevel === 1">
                <button class="btn-danger-sm" @click="revokeTeam(t.teamId)">Revoke</button>
              </td>
            </tr>
          </tbody>
        </table>
        <p v-else class="empty-small">No team members.</p>
      </div>

      <!-- Files tab -->
      <div v-if="activeTab === 'files'" class="tab-content">
        <div class="section-header">
          <h2>Files</h2>
          <button v-if="myLevel !== null" class="btn-primary-sm" @click="openAddFile">+ Add File Record</button>
        </div>

        <LoadingSpinner v-if="filesLoading" />

        <table v-else-if="files.length > 0" class="table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Type</th>
              <th>Size</th>
              <th>Min Level</th>
              <th>Access</th>
              <th>Added</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="f in files" :key="f.caseFileId">
              <td>{{ f.name }}</td>
              <td>{{ f.contentType }}</td>
              <td>{{ f.sizeBytes > 0 ? `${Math.round(f.sizeBytes / 1024)} KB` : '—' }}</td>
              <td>Level {{ f.minLevel }}</td>
              <td>{{ f.accessMode }}</td>
              <td>{{ new Date(f.createdAt).toLocaleDateString() }}</td>
            </tr>
          </tbody>
        </table>
        <p v-else class="empty-small">No files yet.</p>
      </div>
    </template>

    <div v-else class="empty">Case not found.</div>
  </div>

  <!-- Grant user modal -->
  <Teleport to="body">
    <div v-if="showGrantUser" class="modal-backdrop" @click.self="showGrantUser = false">
      <div class="modal">
        <h3>Grant User Access</h3>
        <div class="form-group">
          <label class="form-label">Search user</label>
          <input v-model="grantUserForm.searchQuery" class="field" placeholder="Username or email" @input="searchUsers" />
          <div v-if="searchResults.length > 0" class="search-results">
            <div
              v-for="u in searchResults"
              :key="u.userId"
              class="search-result-item"
              @click="selectUser(u)"
            >{{ u.username || u.email }}</div>
          </div>
        </div>
        <div class="form-group">
          <label class="form-label">Access Level ({{ grantUserForm.level }} = {{ ['Lead Partner','Senior Partner','Associate','Paralegal','Secretary','External Consultant'][grantUserForm.level-1] }})</label>
          <input v-model.number="grantUserForm.level" type="range" min="1" :max="myLevel ?? 6" step="1" class="range" />
          <div class="range-labels">
            <span>L1 (highest)</span><span>L{{ myLevel ?? 6 }} (lowest you can grant)</span>
          </div>
        </div>
        <p class="hint">The user will receive encrypted copies of levels {{ grantUserForm.level }}–6.</p>
        <div class="modal-actions">
          <button class="btn-primary-sm" :disabled="granting || !grantUserForm.userId" @click="handleGrantUser">
            {{ granting ? 'Granting…' : 'Grant' }}
          </button>
          <button class="btn-secondary-sm" @click="showGrantUser = false">Cancel</button>
        </div>
      </div>
    </div>
  </Teleport>

  <!-- Grant team modal -->
  <Teleport to="body">
    <div v-if="showGrantTeam" class="modal-backdrop" @click.self="showGrantTeam = false">
      <div class="modal">
        <h3>Grant Team Access</h3>
        <div class="form-group">
          <label class="form-label">Team ID</label>
          <input v-model="grantTeamForm.teamId" class="field" placeholder="Paste team UUID" />
        </div>
        <div class="form-group">
          <label class="form-label">Access Level</label>
          <input v-model.number="grantTeamForm.level" type="range" min="1" :max="myLevel ?? 6" step="1" class="range" />
          <div class="range-labels">
            <span>L1</span><span>L{{ myLevel ?? 6 }}</span>
          </div>
        </div>
        <div class="modal-actions">
          <button class="btn-primary-sm" :disabled="granting2 || !grantTeamForm.teamId" @click="handleGrantTeam">
            {{ granting2 ? 'Granting…' : 'Grant' }}
          </button>
          <button class="btn-secondary-sm" @click="showGrantTeam = false">Cancel</button>
        </div>
      </div>
    </div>
  </Teleport>

  <!-- Add file modal -->
  <Teleport to="body">
    <div v-if="showAddFile" class="modal-backdrop" @click.self="showAddFile = false">
      <div class="modal modal-wide">
        <h3>Add File Record</h3>
        <div class="form-group">
          <label class="form-label">File Name *</label>
          <input v-model="fileForm.name" class="field" placeholder="e.g. contract.pdf" />
        </div>
        <div class="form-group">
          <label class="form-label">Content Type</label>
          <input v-model="fileForm.contentType" class="field" placeholder="application/pdf" />
        </div>
        <div class="form-group">
          <label class="form-label">Storage Path *</label>
          <input v-model="fileForm.storagePath" class="field" placeholder="e.g. cases/abc123/contract.enc" />
        </div>
        <div class="form-group">
          <label class="form-label">Minimum Level (1=highest, 6=lowest)</label>
          <input v-model.number="fileForm.minLevel" type="range" min="1" :max="myLevel ?? 6" step="1" class="range" />
          <span class="range-value">Level {{ fileForm.minLevel }}</span>
        </div>
        <div class="form-group">
          <label class="form-label">Access Mode</label>
          <select v-model="fileForm.accessMode" class="field">
            <option value="Hierarchical">Hierarchical (levels 1–{{ fileForm.minLevel }} can access)</option>
            <option value="Independent">Independent (only level {{ fileForm.minLevel }} can access)</option>
          </select>
        </div>
        <p class="hint">A 32-byte file key will be generated and encrypted for each qualifying level.</p>
        <div class="modal-actions">
          <button class="btn-primary-sm" :disabled="addingFile || !fileForm.name.trim() || !fileForm.storagePath.trim()" @click="handleAddFile">
            {{ addingFile ? 'Adding…' : 'Add' }}
          </button>
          <button class="btn-secondary-sm" @click="showAddFile = false">Cancel</button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
.case-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 1.5rem;
}
.case-header h1 { margin: 0 0 0.25rem; }
.sub { margin: 0; color: #6b7280; font-size: 0.9rem; }

.badges { display: flex; gap: 0.5rem; }
.badge {
  padding: 0.25rem 0.75rem;
  border-radius: 999px;
  font-size: 0.8rem;
  font-weight: 600;
}
.badge.status { background: #e0e7ff; color: #4338ca; }
.badge.priority { background: #fef3c7; color: #b45309; }

.tabs {
  display: flex;
  gap: 0;
  border-bottom: 2px solid #e5e7eb;
  margin-bottom: 1.5rem;
}
.tab {
  padding: 0.6rem 1.25rem;
  background: none;
  border: none;
  cursor: pointer;
  font-size: 0.9rem;
  color: #6b7280;
  border-bottom: 2px solid transparent;
  margin-bottom: -2px;
}
.tab.active { color: #667eea; border-bottom-color: #667eea; font-weight: 600; }

.tab-content { min-height: 200px; }

.info-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(180px, 1fr)); gap: 1rem; margin-bottom: 2rem; }
.info-item { background: white; border-radius: 8px; padding: 1rem; box-shadow: 0 1px 3px rgba(0,0,0,0.08); }
.info-label { display: block; font-size: 0.75rem; color: #9ca3af; margin-bottom: 0.25rem; text-transform: uppercase; letter-spacing: 0.05em; }

.level-legend h3 { margin: 0 0 0.75rem; font-size: 1rem; }
.level-row {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.4rem 0;
  border-bottom: 1px solid #f3f4f6;
}
.level-num { font-weight: 700; color: #667eea; width: 2rem; }
.level-name { flex: 1; font-size: 0.9rem; }
.level-has { font-size: 0.75rem; color: #059669; }

.section-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 1rem; }
.section-header h2 { margin: 0; font-size: 1.1rem; }
.actions { display: flex; gap: 0.5rem; }

.table { width: 100%; border-collapse: collapse; font-size: 0.875rem; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 1px 3px rgba(0,0,0,0.08); }
.table th { text-align: left; padding: 0.75rem 1rem; background: #f9fafb; font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.05em; color: #6b7280; }
.table td { padding: 0.75rem 1rem; border-top: 1px solid #f3f4f6; }
.table tr:hover td { background: #fafafa; }

.empty { text-align: center; padding: 4rem 0; color: #6b7280; }
.empty-small { color: #9ca3af; font-size: 0.875rem; padding: 1rem 0; }

.search-results {
  background: white;
  border: 1px solid #d1d5db;
  border-top: none;
  border-radius: 0 0 6px 6px;
  max-height: 180px;
  overflow-y: auto;
}
.search-result-item {
  padding: 0.5rem 0.75rem;
  cursor: pointer;
  font-size: 0.875rem;
}
.search-result-item:hover { background: #f3f4f6; }

.range { width: 100%; margin: 0.5rem 0; }
.range-labels { display: flex; justify-content: space-between; font-size: 0.75rem; color: #9ca3af; }
.range-value { font-size: 0.85rem; color: #374151; }

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

.modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0,0,0,0.45);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 100;
}
.modal { background: white; border-radius: 10px; padding: 1.75rem; width: 420px; box-shadow: 0 8px 32px rgba(0,0,0,0.18); }
.modal-wide { width: 520px; }
.modal h3 { margin: 0 0 1.25rem; font-size: 1.1rem; }
.modal-actions { display: flex; gap: 0.5rem; justify-content: flex-end; margin-top: 1.25rem; }

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
  padding: 0.25rem 0.75rem;
  background: #fee2e2;
  color: #dc2626;
  border: 1px solid #fca5a5;
  border-radius: 6px;
  font-size: 0.8rem;
  cursor: pointer;
}
.btn-danger-sm:hover { background: #fecaca; }
</style>
