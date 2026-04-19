<template>
  <transition name="fade"><div class="cpw-overlay" @click.self="$emit('close')" /></transition>
  <transition name="pop">
    <div class="cpw-dialog" role="dialog" aria-modal="true" aria-label="Create Partner">
      <div class="cpw-header">
        <h2>Create New Partner</h2>
        <button class="cpw-close" @click="$emit('close')">✕</button>
      </div>

      <!-- Step bar -->
      <div class="step-bar">
        <div v-for="s in STEPS" :key="s.n" class="step-item" :class="{ active: step === s.n, done: step > s.n }">
          <div class="step-circle">{{ step > s.n ? '✓' : s.n }}</div>
          <span class="step-label">{{ s.label }}</span>
          <div v-if="s.n < STEPS.length" class="step-line" :class="{ done: step > s.n }" />
        </div>
      </div>

      <!-- Step 1: Profile -->
      <div v-if="step === 1" class="step-body">
        <h3 class="section-title">Organisation</h3>
        <div class="field"><label>Partner Name <span class="req">*</span></label>
          <input v-model="form.name" placeholder="e.g. Copenhagen Business School" />
        </div>
        <div class="row-2">
          <div class="field"><label>Website</label><input v-model="form.website" placeholder="https://…" /></div>
          <div class="field"><label>Registration Number</label><input v-model="form.registrationNumber" /></div>
        </div>
        <div class="field"><label>Tax / VAT ID</label><input v-model="form.taxId" /></div>

        <h3 class="section-title">Contact person</h3>
        <div class="row-2">
          <div class="field"><label>Name</label><input v-model="form.contactPersonName" /></div>
          <div class="field"><label>Title</label><input v-model="form.contactPersonTitle" placeholder="e.g. Director of Admissions" /></div>
        </div>
        <div class="row-2">
          <div class="field"><label>Email</label><input v-model="form.contactPersonEmail" type="email" /></div>
          <div class="field"><label>Phone</label><input v-model="form.contactPersonPhone" /></div>
        </div>

        <h3 class="section-title">Address</h3>
        <div class="field"><label>Address line 1</label><input v-model="form.addressLine1" /></div>
        <div class="field"><label>Address line 2</label><input v-model="form.addressLine2" /></div>
        <div class="row-3">
          <div class="field"><label>City</label><input v-model="form.city" /></div>
          <div class="field"><label>State / Region</label><input v-model="form.stateRegion" /></div>
          <div class="field"><label>Postal code</label><input v-model="form.postalCode" /></div>
        </div>
        <div class="field"><label>Country</label><input v-model="form.country" /></div>

        <h3 class="section-title">Partnership metadata</h3>
        <div class="row-2">
          <div class="field"><label>Contract start</label><input v-model="form.contractStart" type="date" /></div>
          <div class="field"><label>Contract end</label><input v-model="form.contractEnd" type="date" /></div>
        </div>
        <div class="field"><label>Tier</label>
          <select v-model="form.tier">
            <option value="">—</option>
            <option>Gold</option>
            <option>Silver</option>
            <option>Bronze</option>
            <option>Standard</option>
          </select>
        </div>
        <div class="field"><label>Internal notes</label>
          <textarea v-model="form.internalNotes" rows="2" placeholder="Only visible to IBSS admins" />
        </div>

        <div class="step-nav">
          <span />
          <button class="btn-next" :disabled="!step1Valid" @click="step = 2">Next →</button>
        </div>
      </div>

      <!-- Step 2: Core Programmes -->
      <div v-else-if="step === 2" class="step-body">
        <p class="step-hint">Select which IBSS Core Programmes and Majors this partner can enrol students into. You can change this later.</p>

        <div v-if="programmesLoading" class="loading-row">Loading programmes…</div>
        <div v-else-if="programmesError" class="err-banner">{{ programmesError }}</div>
        <div v-else-if="programmes.length === 0" class="empty-state-card">No core programmes configured yet.</div>

        <div v-else class="prog-list">
          <div v-for="p in programmes" :key="p.programmeId" class="prog-card">
            <div class="prog-head" @click="toggleProg(p.programmeId)">
              <div>
                <strong>{{ p.name }}</strong>
                <span class="mono-code">{{ p.code }}</span>
              </div>
              <div>
                <span class="prog-count">{{ selectedInProg(p.programmeId) }} / {{ majorsByProg(p.programmeId).length }}</span>
                <button class="btn-xs" @click.stop="toggleAllInProg(p.programmeId)">
                  {{ allSelectedInProg(p.programmeId) ? 'Clear' : 'Select all' }}
                </button>
                <span class="caret">{{ openProgs.has(p.programmeId) ? '▾' : '▸' }}</span>
              </div>
            </div>
            <div v-if="openProgs.has(p.programmeId)" class="major-list">
              <label v-for="m in majorsByProg(p.programmeId)" :key="m.majorId" class="major-row">
                <input type="checkbox" :checked="selectedMajorIds.has(m.majorId)" @change="toggleMajor(m.majorId)" />
                <span>{{ m.name }}</span>
              </label>
              <p v-if="majorsByProg(p.programmeId).length === 0" class="empty-note">No majors defined for this programme.</p>
            </div>
          </div>
        </div>

        <div class="step-nav">
          <button class="btn-back" @click="step = 1">← Back</button>
          <button class="btn-next" @click="step = 3">Next →</button>
        </div>
      </div>

      <!-- Step 3: Users -->
      <div v-else-if="step === 3" class="step-body">
        <p class="step-hint">Create at least one partner user. Each user gets a temporary password shown after creation.</p>

        <div v-for="(u, i) in form.users" :key="i" class="user-row">
          <div class="field">
            <label>Username <span v-if="i === 0" class="req">*</span></label>
            <input v-model="u.username" placeholder="e.g. cbs_admin" />
          </div>
          <div class="field">
            <label>Email (optional)</label>
            <input v-model="u.email" type="email" />
          </div>
          <button v-if="form.users.length > 1" class="btn-icon-x" @click="form.users.splice(i, 1)" title="Remove">✕</button>
        </div>

        <button class="btn-link" @click="form.users.push({ username: '', email: '' })">+ Add another user</button>

        <div class="step-nav">
          <button class="btn-back" @click="step = 2">← Back</button>
          <button class="btn-next" :disabled="!step3Valid" @click="step = 4">Next →</button>
        </div>
      </div>

      <!-- Step 4: Review -->
      <div v-else-if="step === 4" class="step-body">
        <h3 class="section-title">Review &amp; Create</h3>

        <div class="review-grid">
          <div class="review-row"><span>Name</span><strong>{{ form.name }}</strong></div>
          <div class="review-row" v-if="form.contactPersonName"><span>Contact</span><strong>{{ form.contactPersonName }}<span v-if="form.contactPersonTitle"> ({{ form.contactPersonTitle }})</span></strong></div>
          <div class="review-row" v-if="form.contactPersonEmail || form.contactPersonPhone"><span>Contact email / phone</span><strong>{{ [form.contactPersonEmail, form.contactPersonPhone].filter(Boolean).join(' · ') || '—' }}</strong></div>
          <div class="review-row" v-if="addressSummary"><span>Address</span><strong>{{ addressSummary }}</strong></div>
          <div class="review-row" v-if="form.website"><span>Website</span><strong>{{ form.website }}</strong></div>
          <div class="review-row" v-if="form.registrationNumber"><span>Reg. no.</span><strong>{{ form.registrationNumber }}</strong></div>
          <div class="review-row" v-if="form.taxId"><span>Tax ID</span><strong>{{ form.taxId }}</strong></div>
          <div class="review-row" v-if="form.tier"><span>Tier</span><strong>{{ form.tier }}</strong></div>
          <div class="review-row" v-if="form.contractStart || form.contractEnd"><span>Contract</span><strong>{{ [form.contractStart, form.contractEnd].filter(Boolean).join(' → ') || '—' }}</strong></div>

          <div class="review-row"><span>Core programme access</span>
            <strong v-if="selectedMajorIds.size === 0">None (can grant later)</strong>
            <strong v-else>{{ selectedMajorIds.size }} major{{ selectedMajorIds.size === 1 ? '' : 's' }} across {{ selectedProgCount }} programme{{ selectedProgCount === 1 ? '' : 's' }}</strong>
          </div>
          <div class="review-row"><span>Users</span>
            <strong>{{ form.users.filter(u => u.username.trim()).map(u => u.username.trim()).join(', ') }}</strong>
          </div>
        </div>

        <div v-if="submitError" class="err-banner">{{ submitError }}</div>

        <!-- Generated passwords -->
        <div v-if="createdPasswords.length" class="password-reveal-block">
          <h4>Partner created ✓</h4>
          <p class="step-hint">Save these temporary passwords — they won't be shown again.</p>
          <div v-for="c in createdPasswords" :key="c.username" class="password-reveal">
            <span><strong>{{ c.username }}</strong></span>
            <code>{{ c.password }}</code>
            <button class="btn-copy" @click="copy(c.password)">Copy</button>
          </div>
        </div>

        <div class="step-nav">
          <button v-if="!createdPasswords.length" class="btn-back" @click="step = 3">← Back</button>
          <span v-else />
          <button v-if="!createdPasswords.length" class="btn-submit" :disabled="submitting" @click="handleSubmit">
            {{ submitting ? 'Creating…' : 'Create Partner' }}
          </button>
          <button v-else class="btn-submit" @click="$emit('close', createdPartnerId)">Done</button>
        </div>
      </div>
    </div>
  </transition>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import apiClient from '../../api/client.js'

const emit = defineEmits(['close', 'created'])

const STEPS = [
  { n: 1, label: 'Profile' },
  { n: 2, label: 'Core Programmes' },
  { n: 3, label: 'Users' },
  { n: 4, label: 'Review' },
]

const step = ref(1)

const form = reactive({
  name: '',
  contactPersonName: '', contactPersonTitle: '', contactPersonEmail: '', contactPersonPhone: '',
  addressLine1: '', addressLine2: '', city: '', stateRegion: '', postalCode: '', country: '',
  website: '', registrationNumber: '', taxId: '',
  contractStart: '', contractEnd: '', tier: '', internalNotes: '',
  users: [{ username: '', email: '' }],
})

// Core programmes
const programmes = ref([])
const majors = ref([])
const programmesLoading = ref(false)
const programmesError = ref('')
const openProgs = reactive(new Set())
const selectedMajorIds = reactive(new Set())

function majorsByProg(progId) { return majors.value.filter(m => m.programmeId === progId && !m.deletedAt) }
function selectedInProg(progId) { return majorsByProg(progId).filter(m => selectedMajorIds.has(m.majorId)).length }
function allSelectedInProg(progId) {
  const ms = majorsByProg(progId)
  return ms.length > 0 && ms.every(m => selectedMajorIds.has(m.majorId))
}
function toggleProg(progId) { openProgs.has(progId) ? openProgs.delete(progId) : openProgs.add(progId) }
function toggleMajor(majorId) { selectedMajorIds.has(majorId) ? selectedMajorIds.delete(majorId) : selectedMajorIds.add(majorId) }
function toggleAllInProg(progId) {
  const ms = majorsByProg(progId)
  if (allSelectedInProg(progId)) ms.forEach(m => selectedMajorIds.delete(m.majorId))
  else ms.forEach(m => selectedMajorIds.add(m.majorId))
}

const selectedProgCount = computed(() => {
  const progs = new Set()
  majors.value.forEach(m => { if (selectedMajorIds.has(m.majorId)) progs.add(m.programmeId) })
  return progs.size
})

const addressSummary = computed(() => {
  return [form.addressLine1, form.addressLine2, form.city, form.stateRegion, form.postalCode, form.country]
    .filter(Boolean).join(', ')
})

const step1Valid = computed(() => form.name.trim().length > 0)
const step3Valid = computed(() => (form.users[0]?.username ?? '').trim().length > 0)

// Submit state
const submitting = ref(false)
const submitError = ref('')
const createdPartnerId = ref(null)
const createdPasswords = ref([])

function nn(v) { const t = (v ?? '').trim(); return t === '' ? undefined : t }

async function handleSubmit() {
  submitting.value = true
  submitError.value = ''
  createdPasswords.value = []
  try {
    const firstUser = form.users[0]
    const body = {
      name: form.name.trim(),
      username: firstUser.username.trim(),
      email: nn(firstUser.email),
      contactPersonName:  nn(form.contactPersonName),
      contactPersonTitle: nn(form.contactPersonTitle),
      contactPersonEmail: nn(form.contactPersonEmail),
      contactPersonPhone: nn(form.contactPersonPhone),
      addressLine1: nn(form.addressLine1),
      addressLine2: nn(form.addressLine2),
      city:         nn(form.city),
      stateRegion:  nn(form.stateRegion),
      postalCode:   nn(form.postalCode),
      country:      nn(form.country),
      website:            nn(form.website),
      registrationNumber: nn(form.registrationNumber),
      taxId:              nn(form.taxId),
      contractStart: form.contractStart || undefined,
      contractEnd:   form.contractEnd || undefined,
      tier:          nn(form.tier),
      internalNotes: nn(form.internalNotes),
    }
    const res = await apiClient.post('/v1/admin/school/partners', body)
    const partnerId = res.data.partnerId
    createdPartnerId.value = partnerId
    createdPasswords.value.push({ username: res.data.username, password: res.data.temporaryPassword })

    // Additional users
    for (let i = 1; i < form.users.length; i++) {
      const u = form.users[i]
      if (!u.username.trim()) continue
      try {
        const r = await apiClient.post(`/v1/admin/school/partners/${partnerId}/users`, {
          username: u.username.trim(),
          email: nn(u.email),
        })
        createdPasswords.value.push({ username: u.username.trim(), password: r.data.temporaryPassword })
      } catch (e) {
        submitError.value = `User "${u.username}" could not be added: ${e.response?.data ?? e.message}`
      }
    }

    // Programme access
    if (selectedMajorIds.size > 0) {
      try {
        await apiClient.post(`/v1/admin/school/partners/${partnerId}/programme-access`, {
          majorIds: [...selectedMajorIds],
        })
      } catch (e) {
        submitError.value = `Partner created but programme access failed: ${e.response?.data ?? e.message}`
      }
    }

    emit('created', partnerId)
  } catch (e) {
    submitError.value = e.response?.data ?? e.message ?? 'Failed to create partner'
  } finally {
    submitting.value = false
  }
}

function copy(v) { navigator.clipboard.writeText(v).catch(() => {}) }

async function loadProgrammesAndMajors() {
  programmesLoading.value = true
  programmesError.value = ''
  try {
    const [pRes, mRes] = await Promise.all([
      apiClient.get('/v1/school/programmes?ownership=core'),
      apiClient.get('/v1/school/majors'),
    ])
    programmes.value = (pRes.data.items ?? []).filter(p => !p.deletedAt)
    majors.value = mRes.data.items ?? []
    programmes.value.forEach(p => openProgs.add(p.programmeId))
  } catch (e) {
    programmesError.value = e.response?.data?.message ?? e.message ?? 'Failed to load programmes'
  } finally {
    programmesLoading.value = false
  }
}

onMounted(loadProgrammesAndMajors)
</script>

<style scoped>
.cpw-overlay {
  position: fixed; inset: 0; background: rgba(12, 25, 48, 0.5); z-index: 2000;
}
.cpw-dialog {
  position: fixed; top: 50%; left: 50%; transform: translate(-50%, -50%);
  width: min(760px, 94vw); max-height: 92vh; overflow-y: auto;
  background: #fff; border-radius: 14px; box-shadow: 0 20px 60px rgba(0,0,0,.25);
  z-index: 2001; display: flex; flex-direction: column;
}
.cpw-header { display: flex; align-items: center; justify-content: space-between; padding: 1.1rem 1.5rem; border-bottom: 1px solid #e5eaf1; }
.cpw-header h2 { margin: 0; font-size: 1.3rem; color: #0a264f; }
.cpw-close { background: none; border: none; font-size: 1.2rem; cursor: pointer; color: #6b7888; }

/* Step bar */
.step-bar { display: flex; justify-content: space-between; align-items: center; padding: 1rem 1.5rem 0; }
.step-item { display: flex; align-items: center; gap: .5rem; flex: 1; }
.step-item:last-child { flex: 0 0 auto; }
.step-circle { width: 28px; height: 28px; border-radius: 50%; background: #dbe3ec; color: #5f6e85; font-weight: 700; font-size: .85rem; display: flex; align-items: center; justify-content: center; flex-shrink: 0; }
.step-item.active .step-circle { background: #0a264f; color: #fff; }
.step-item.done .step-circle { background: #1c7a4a; color: #fff; }
.step-label { font-size: .82rem; color: #5f6e85; white-space: nowrap; }
.step-item.active .step-label { color: #0a264f; font-weight: 600; }
.step-line { flex: 1; height: 2px; background: #dbe3ec; margin: 0 .4rem; }
.step-line.done { background: #1c7a4a; }

.step-body { padding: 1.25rem 1.5rem 1.5rem; }
.step-hint { font-size: .88rem; color: #5f6e85; margin: 0 0 1rem; }
.section-title { font-size: .95rem; color: #0a264f; margin: 1rem 0 .5rem; text-transform: uppercase; letter-spacing: .04em; font-weight: 700; }
.section-title:first-child { margin-top: 0; }

.field { display: flex; flex-direction: column; margin-bottom: .65rem; }
.field label { font-size: .78rem; color: #5f6e85; margin-bottom: .2rem; }
.field input, .field select, .field textarea {
  padding: .55rem .7rem; border: 1px solid #cfd7e3; border-radius: 7px; font-size: .92rem; font-family: inherit;
}
.field textarea { resize: vertical; }
.req { color: #c4362f; }

.row-2 { display: grid; grid-template-columns: 1fr 1fr; gap: .7rem; }
.row-3 { display: grid; grid-template-columns: 1.4fr 1fr 1fr; gap: .7rem; }

.step-nav { display: flex; justify-content: space-between; align-items: center; margin-top: 1.25rem; }
.btn-back, .btn-next, .btn-submit {
  padding: .55rem 1.1rem; border-radius: 7px; font-weight: 600; cursor: pointer; border: none; font-size: .92rem;
}
.btn-back { background: #ecf0f6; color: #3b4a63; }
.btn-next, .btn-submit { background: #0a264f; color: #fff; }
.btn-next:disabled, .btn-submit:disabled { opacity: .45; cursor: not-allowed; }

/* Programmes */
.prog-list { display: flex; flex-direction: column; gap: .55rem; }
.prog-card { border: 1px solid #e0e6ee; border-radius: 8px; overflow: hidden; }
.prog-head { display: flex; justify-content: space-between; align-items: center; padding: .7rem .9rem; cursor: pointer; background: #f6f9fd; }
.prog-head > div { display: flex; align-items: center; gap: .5rem; }
.mono-code { font-family: monospace; font-size: .78rem; color: #6b7888; background: #fff; border: 1px solid #e1e6ed; padding: 1px 6px; border-radius: 4px; }
.prog-count { font-size: .78rem; color: #5f6e85; }
.caret { font-size: .85rem; color: #6b7888; }
.btn-xs { padding: 2px 8px; font-size: .75rem; border: 1px solid #cfd7e3; background: #fff; border-radius: 5px; cursor: pointer; }
.major-list { padding: .5rem .9rem; display: flex; flex-direction: column; gap: .3rem; border-top: 1px solid #e8edf3; }
.major-row { display: flex; align-items: center; gap: .5rem; font-size: .9rem; cursor: pointer; }
.empty-note { font-size: .82rem; color: #8a93a4; margin: 0; }
.loading-row { padding: 1rem; color: #5f6e85; font-size: .9rem; }
.err-banner { background: #fde7e5; color: #a8241e; padding: .6rem .9rem; border-radius: 6px; margin: .4rem 0; font-size: .88rem; }
.empty-state-card { padding: 1rem; background: #f6f9fd; color: #5f6e85; border-radius: 8px; text-align: center; }

/* Users */
.user-row { display: grid; grid-template-columns: 1fr 1fr auto; gap: .7rem; align-items: end; margin-bottom: .4rem; }
.btn-icon-x { padding: .5rem .7rem; border: none; background: #fbe7e5; color: #a8241e; border-radius: 6px; cursor: pointer; }
.btn-link { background: none; border: none; color: #0a5aad; font-weight: 600; cursor: pointer; padding: .3rem 0; }

/* Review */
.review-grid { display: flex; flex-direction: column; gap: .4rem; margin-top: .5rem; }
.review-row { display: flex; justify-content: space-between; padding: .55rem .75rem; background: #f6f9fd; border-radius: 6px; font-size: .9rem; }
.review-row span { color: #5f6e85; }
.review-row strong { color: #0a264f; text-align: right; }

.password-reveal-block { margin-top: 1rem; padding: 1rem; background: #e7f4ec; border-radius: 8px; }
.password-reveal-block h4 { margin: 0 0 .5rem; color: #1c7a4a; }
.password-reveal { display: flex; align-items: center; gap: .6rem; margin: .3rem 0; background: #fff; padding: .5rem .75rem; border-radius: 6px; }
.password-reveal code { font-family: monospace; color: #0a264f; }
.btn-copy { margin-left: auto; padding: 2px 10px; font-size: .78rem; border: 1px solid #0a264f; background: #fff; color: #0a264f; border-radius: 5px; cursor: pointer; }

.fade-enter-active, .fade-leave-active { transition: opacity .15s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
.pop-enter-active, .pop-leave-active { transition: all .2s cubic-bezier(.18,.9,.32,1.1); }
.pop-enter-from, .pop-leave-to { opacity: 0; transform: translate(-50%, -48%) scale(.96); }
</style>
