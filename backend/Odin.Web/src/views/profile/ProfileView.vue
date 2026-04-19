<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useProfileStore } from '@/stores/profile'
import { useNotificationStore } from '@/stores/notification'
import { profileApi } from '@/api/profileApi'
import FormInput from '@/components/FormInput.vue'
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import type { PhoneEntry, ContactEmailEntry, AddressEntry } from '@/types'

const profileStore = useProfileStore()
const notify = useNotificationStore()

const form = ref({
  firstName: '',
  lastName: '',
  avatarUrl: '',
  bio: '',
  dateOfBirth: '',
})
const saving = ref(false)

// ── Inline edit state ──────────────────────────────────────────────────────

type EditingPhone = { id: string | null; number: string; label: string }
type EditingEmail = { id: string | null; email: string; label: string }
type EditingAddress = { id: string | null; label: string; street: string; city: string; state: string; zipCode: string; country: string }

const editingPhone = ref<EditingPhone | null>(null)
const editingEmail = ref<EditingEmail | null>(null)
const editingAddress = ref<EditingAddress | null>(null)

// ── Verify modal state ─────────────────────────────────────────────────────

type VerifyTarget = { type: 'email' | 'phone'; id: string; sessionId: string }
const verifyTarget = ref<VerifyTarget | null>(null)
const verifyCode = ref('')
const verifyBusy = ref(false)

async function startVerifyEmail(id: string) {
  try {
    const res = await profileApi.verifyEmailInit(id)
    if (res.data.success && res.data.data) {
      verifyTarget.value = { type: 'email', id, sessionId: res.data.data.sessionId }
      verifyCode.value = ''
    } else {
      notify.error(res.data.message || 'Failed to send verification code')
    }
  } catch {
    notify.error('Failed to send verification code')
  }
}

async function startVerifyPhone(id: string) {
  try {
    const res = await profileApi.verifyPhoneInit(id)
    if (res.data.success && res.data.data) {
      verifyTarget.value = { type: 'phone', id, sessionId: res.data.data.sessionId }
      verifyCode.value = ''
    } else {
      notify.error(res.data.message || 'Failed to send verification code')
    }
  } catch {
    notify.error('Failed to send verification code')
  }
}

async function submitVerifyCode() {
  if (!verifyTarget.value || !verifyCode.value) return
  verifyBusy.value = true
  try {
    const { type, sessionId } = verifyTarget.value
    const res = type === 'email'
      ? await profileApi.verifyEmailConfirm(sessionId, verifyCode.value)
      : await profileApi.verifyPhoneConfirm(sessionId, verifyCode.value)
    if (res.data.success) {
      notify.success('Verified successfully')
      verifyTarget.value = null
      await refresh()
    } else {
      notify.error(res.data.message || 'Invalid code')
    }
  } catch {
    notify.error('Invalid code')
  } finally {
    verifyBusy.value = false
  }
}

onMounted(async () => {
  await profileStore.fetchProfile()
  if (profileStore.profile) {
    const p = profileStore.profile
    form.value = {
      firstName: p.firstName || '',
      lastName: p.lastName || '',
      avatarUrl: p.avatarUrl || '',
      bio: p.bio || '',
      dateOfBirth: p.dateOfBirth?.split('T')[0] || '',
    }
  }
})

async function handleSave() {
  saving.value = true
  try {
    const result = await profileStore.updateProfile({
      ...form.value,
      dateOfBirth: form.value.dateOfBirth || undefined
    })
    if (result.success) {
      notify.success('Profile updated')
    } else {
      notify.error(result.message || 'Failed to update')
    }
  } catch {
    notify.error('Failed to update profile')
  } finally {
    saving.value = false
  }
}

async function refresh() {
  await profileStore.fetchProfile()
}

// ── Phones ─────────────────────────────────────────────────────────────────

function startAddPhone() {
  editingPhone.value = { id: null, number: '', label: '' }
}

function startEditPhone(p: PhoneEntry) {
  editingPhone.value = { id: p.id, number: p.number, label: p.label || '' }
}

async function savePhone() {
  if (!editingPhone.value) return
  const { id, number, label } = editingPhone.value
  const data = { number, label: label || undefined }
  const res = id === null
    ? await profileApi.addPhone(data)
    : await profileApi.updatePhone(id, data)
  if (res.data.success) {
    editingPhone.value = null
    await refresh()
  } else {
    notify.error(res.data.message || 'Failed to save phone')
  }
}

async function deletePhone(id: string) {
  const res = await profileApi.deletePhone(id)
  if (res.data.success) {
    await refresh()
  } else {
    notify.error(res.data.message || 'Failed to delete phone')
  }
}

async function setPrimaryPhone(id: string) {
  const res = await profileApi.setPrimaryPhone(id)
  if (res.data.success) {
    await refresh()
  } else {
    notify.error(res.data.message || 'Failed to set primary phone')
  }
}

// ── Contact Emails ──────────────────────────────────────────────────────────

function startAddEmail() {
  editingEmail.value = { id: null, email: '', label: '' }
}

function startEditEmail(e: ContactEmailEntry) {
  editingEmail.value = { id: e.id, email: e.email, label: e.label || '' }
}

async function saveEmail() {
  if (!editingEmail.value) return
  const { id, email, label } = editingEmail.value
  const data = { email, label: label || undefined }
  const res = id === null
    ? await profileApi.addEmail(data)
    : await profileApi.updateEmail(id, data)
  if (res.data.success) {
    editingEmail.value = null
    await refresh()
  } else {
    notify.error(res.data.message || 'Failed to save email')
  }
}

async function deleteEmail(id: string) {
  const res = await profileApi.deleteEmail(id)
  if (res.data.success) {
    await refresh()
  } else {
    notify.error(res.data.message || 'Failed to delete email')
  }
}

async function setPrimaryEmail(id: string) {
  const res = await profileApi.setPrimaryEmail(id)
  if (res.data.success) {
    await refresh()
  } else {
    notify.error(res.data.message || 'Failed to set primary email')
  }
}

// ── Addresses ───────────────────────────────────────────────────────────────

function startAddAddress() {
  editingAddress.value = { id: null, label: '', street: '', city: '', state: '', zipCode: '', country: '' }
}

function startEditAddress(a: AddressEntry) {
  editingAddress.value = {
    id: a.id,
    label: a.label || '',
    street: a.street || '',
    city: a.city || '',
    state: a.state || '',
    zipCode: a.zipCode || '',
    country: a.country || '',
  }
}

async function saveAddress() {
  if (!editingAddress.value) return
  const { id, label, street, city, state, zipCode, country } = editingAddress.value
  const data = {
    label: label || undefined,
    street: street || undefined,
    city: city || undefined,
    state: state || undefined,
    zipCode: zipCode || undefined,
    country: country || undefined,
  }
  const res = id === null
    ? await profileApi.addAddress(data)
    : await profileApi.updateAddress(id, data)
  if (res.data.success) {
    editingAddress.value = null
    await refresh()
  } else {
    notify.error(res.data.message || 'Failed to save address')
  }
}

async function deleteAddress(id: string) {
  const res = await profileApi.deleteAddress(id)
  if (res.data.success) {
    await refresh()
  } else {
    notify.error(res.data.message || 'Failed to delete address')
  }
}

async function setPrimaryAddress(id: string) {
  const res = await profileApi.setPrimaryAddress(id)
  if (res.data.success) {
    await refresh()
  } else {
    notify.error(res.data.message || 'Failed to set primary address')
  }
}
</script>

<template>
  <div class="page">
    <h1>Profile</h1>
    <LoadingSpinner v-if="profileStore.loading" />
    <template v-else>

      <!-- ── Personal Info ─────────────────────────────────────────────── -->
      <div class="card">
        <h2>Personal Info</h2>
        <form class="profile-form" @submit.prevent="handleSave">
          <div class="form-row">
            <FormInput label="First Name" v-model="form.firstName" />
            <FormInput label="Last Name" v-model="form.lastName" />
          </div>
          <FormInput label="Avatar URL" v-model="form.avatarUrl" />
          <div class="form-group">
            <label class="form-label">Bio</label>
            <textarea v-model="form.bio" class="form-textarea" rows="3"></textarea>
          </div>
          <FormInput label="Date of Birth" type="date" v-model="form.dateOfBirth" />
          <button type="submit" class="btn-primary" :disabled="saving">
            {{ saving ? 'Saving…' : 'Save Profile' }}
          </button>
        </form>
      </div>

      <!-- ── Phone Numbers ─────────────────────────────────────────────── -->
      <div class="card">
        <h2>Phone Numbers</h2>
        <div v-for="phone in profileStore.phones" :key="phone.id">
          <template v-if="editingPhone?.id === phone.id">
            <div class="inline-form">
              <input v-model="editingPhone.number" placeholder="Phone number" class="field" />
              <input v-model="editingPhone.label" placeholder="Label (e.g. Mobile)" class="field field-sm" />
              <button class="btn-primary-sm" @click="savePhone">Save</button>
              <button class="btn-secondary-sm" @click="editingPhone = null">Cancel</button>
            </div>
          </template>
          <template v-else>
            <div class="entry-row">
              <span class="entry-value">{{ phone.number }}</span>
              <span v-if="phone.label" class="entry-label">{{ phone.label }}</span>
              <span v-if="phone.isPrimary" class="badge-primary" title="Primary">★</span>
              <span v-if="phone.isVerified" class="badge-verified" title="Verified">✓</span>
              <span v-else class="badge-unverified" title="Unverified">!</span>
              <div class="entry-actions">
                <button v-if="!phone.isVerified" class="btn-action-verify" @click="startVerifyPhone(phone.id)">Verify</button>
                <button v-if="!phone.isPrimary" class="btn-action" @click="setPrimaryPhone(phone.id)" title="Set as primary">★</button>
                <button class="btn-action" @click="startEditPhone(phone)">Edit</button>
                <button class="btn-action-danger" @click="deletePhone(phone.id)">Delete</button>
              </div>
            </div>
          </template>
        </div>
        <template v-if="editingPhone?.id === null">
          <div class="inline-form">
            <input v-model="editingPhone.number" placeholder="Phone number" class="field" />
            <input v-model="editingPhone.label" placeholder="Label (e.g. Mobile)" class="field field-sm" />
            <button class="btn-primary-sm" @click="savePhone">Add</button>
            <button class="btn-secondary-sm" @click="editingPhone = null">Cancel</button>
          </div>
        </template>
        <button v-else class="btn-add" @click="startAddPhone">+ Add phone number</button>
      </div>

      <!-- ── Contact Emails ────────────────────────────────────────────── -->
      <div class="card">
        <h2>Contact Emails</h2>
        <div v-for="entry in profileStore.emails" :key="entry.id">
          <template v-if="editingEmail?.id === entry.id">
            <div class="inline-form">
              <input v-model="editingEmail.email" placeholder="Email address" class="field" type="email" />
              <input v-model="editingEmail.label" placeholder="Label (e.g. Work)" class="field field-sm" />
              <button class="btn-primary-sm" @click="saveEmail">Save</button>
              <button class="btn-secondary-sm" @click="editingEmail = null">Cancel</button>
            </div>
          </template>
          <template v-else>
            <div class="entry-row">
              <span class="entry-value">{{ entry.email }}</span>
              <span v-if="entry.label" class="entry-label">{{ entry.label }}</span>
              <span v-if="entry.isPrimary" class="badge-primary" title="Primary">★</span>
              <span v-if="entry.isVerified" class="badge-verified" title="Verified">✓</span>
              <span v-else class="badge-unverified" title="Unverified">!</span>
              <div class="entry-actions">
                <button v-if="!entry.isVerified" class="btn-action-verify" @click="startVerifyEmail(entry.id)">Verify</button>
                <button v-if="!entry.isPrimary" class="btn-action" @click="setPrimaryEmail(entry.id)" title="Set as primary">★</button>
                <button class="btn-action" @click="startEditEmail(entry)">Edit</button>
                <button class="btn-action-danger" @click="deleteEmail(entry.id)">Delete</button>
              </div>
            </div>
          </template>
        </div>
        <template v-if="editingEmail?.id === null">
          <div class="inline-form">
            <input v-model="editingEmail.email" placeholder="Email address" class="field" type="email" />
            <input v-model="editingEmail.label" placeholder="Label (e.g. Work)" class="field field-sm" />
            <button class="btn-primary-sm" @click="saveEmail">Add</button>
            <button class="btn-secondary-sm" @click="editingEmail = null">Cancel</button>
          </div>
        </template>
        <button v-else class="btn-add" @click="startAddEmail">+ Add contact email</button>
      </div>

      <!-- ── Addresses ─────────────────────────────────────────────────── -->
      <div class="card">
        <h2>Addresses</h2>
        <div v-for="addr in profileStore.addresses" :key="addr.id">
          <template v-if="editingAddress?.id === addr.id">
            <div class="inline-form inline-form-col">
              <div class="form-row">
                <input v-model="editingAddress.label" placeholder="Label (e.g. Home)" class="field field-sm" />
                <input v-model="editingAddress.street" placeholder="Street" class="field" />
              </div>
              <div class="form-row">
                <input v-model="editingAddress.city" placeholder="City" class="field" />
                <input v-model="editingAddress.state" placeholder="State" class="field field-sm" />
              </div>
              <div class="form-row">
                <input v-model="editingAddress.zipCode" placeholder="Zip Code" class="field field-sm" />
                <input v-model="editingAddress.country" placeholder="Country" class="field" />
              </div>
              <div>
                <button class="btn-primary-sm" @click="saveAddress">Save</button>
                <button class="btn-secondary-sm" @click="editingAddress = null">Cancel</button>
              </div>
            </div>
          </template>
          <template v-else>
            <div class="entry-row">
              <div class="entry-addr">
                <span v-if="addr.label" class="entry-label">{{ addr.label }}</span>
                <span class="entry-value">{{ [addr.street, addr.city, addr.state, addr.zipCode, addr.country].filter(Boolean).join(', ') }}</span>
              </div>
              <span v-if="addr.isPrimary" class="badge-primary" title="Primary">★</span>
              <div class="entry-actions">
                <button v-if="!addr.isPrimary" class="btn-action" @click="setPrimaryAddress(addr.id)" title="Set as primary">★</button>
                <button class="btn-action" @click="startEditAddress(addr)">Edit</button>
                <button class="btn-action-danger" @click="deleteAddress(addr.id)">Delete</button>
              </div>
            </div>
          </template>
        </div>
        <template v-if="editingAddress?.id === null">
          <div class="inline-form inline-form-col">
            <div class="form-row">
              <input v-model="editingAddress.label" placeholder="Label (e.g. Home)" class="field field-sm" />
              <input v-model="editingAddress.street" placeholder="Street" class="field" />
            </div>
            <div class="form-row">
              <input v-model="editingAddress.city" placeholder="City" class="field" />
              <input v-model="editingAddress.state" placeholder="State" class="field field-sm" />
            </div>
            <div class="form-row">
              <input v-model="editingAddress.zipCode" placeholder="Zip Code" class="field field-sm" />
              <input v-model="editingAddress.country" placeholder="Country" class="field" />
            </div>
            <div>
              <button class="btn-primary-sm" @click="saveAddress">Add</button>
              <button class="btn-secondary-sm" @click="editingAddress = null">Cancel</button>
            </div>
          </div>
        </template>
        <button v-else class="btn-add" @click="startAddAddress">+ Add address</button>
      </div>

    </template>
  </div>

  <!-- ── Verify code modal ─────────────────────────────────────────────── -->
  <Teleport to="body">
    <div v-if="verifyTarget" class="modal-backdrop" @click.self="verifyTarget = null">
      <div class="modal">
        <h3>Enter verification code</h3>
        <p class="modal-hint">
          A code was sent to your {{ verifyTarget.type === 'email' ? 'email address' : 'phone number' }}.
        </p>
        <input
          v-model="verifyCode"
          class="code-input"
          placeholder="000000"
          maxlength="6"
          inputmode="numeric"
          autocomplete="one-time-code"
          @keyup.enter="submitVerifyCode"
        />
        <div class="modal-actions">
          <button class="btn-primary-sm" :disabled="verifyBusy || verifyCode.length < 4" @click="submitVerifyCode">
            {{ verifyBusy ? 'Verifying…' : 'Confirm' }}
          </button>
          <button class="btn-secondary-sm" @click="verifyTarget = null">Cancel</button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
.page { max-width: 640px; }
.page h1 { margin-bottom: 1.5rem; }

.card {
  background: white;
  padding: 1.5rem;
  border-radius: 8px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  margin-bottom: 1.5rem;
}

.card h2 { margin-bottom: 1rem; font-size: 1.1rem; }

.profile-form { display: flex; flex-direction: column; }

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
}

.form-group { margin-bottom: 1rem; }

.form-label {
  display: block;
  margin-bottom: 0.375rem;
  font-size: 0.875rem;
  font-weight: 500;
  color: #374151;
}

.form-textarea {
  width: 100%;
  padding: 0.625rem 0.75rem;
  border: 1px solid #d1d5db;
  border-radius: 6px;
  font-size: 0.9rem;
  font-family: inherit;
  resize: vertical;
  box-sizing: border-box;
}

.form-textarea:focus {
  outline: none;
  border-color: #667eea;
  box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.15);
}

/* Entry rows */
.entry-row {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 0;
  border-bottom: 1px solid #f3f4f6;
  flex-wrap: wrap;
}

.entry-value { flex: 1; font-size: 0.9rem; }
.entry-addr { display: flex; flex-direction: column; flex: 1; }
.entry-label {
  font-size: 0.75rem;
  color: #6b7280;
  background: #f3f4f6;
  padding: 1px 6px;
  border-radius: 99px;
}

.badge-primary { color: #f59e0b; font-size: 1rem; }

.entry-actions { display: flex; gap: 0.4rem; margin-left: auto; }

/* Inline forms */
.inline-form {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 0;
  flex-wrap: wrap;
}

.inline-form-col {
  flex-direction: column;
  align-items: stretch;
  gap: 0.5rem;
}

.inline-form-col .form-row { margin-bottom: 0; }

.field {
  padding: 0.5rem 0.625rem;
  border: 1px solid #d1d5db;
  border-radius: 6px;
  font-size: 0.875rem;
  flex: 1;
  min-width: 0;
}

.field-sm { flex: 0 0 auto; width: 130px; }

.field:focus { outline: none; border-color: #667eea; }

/* Buttons */
.btn-primary {
  padding: 0.75rem 1.5rem;
  background: #667eea;
  color: white;
  border: none;
  border-radius: 6px;
  font-size: 0.9rem;
  cursor: pointer;
  margin-top: 0.5rem;
}

.btn-primary:hover { background: #5a6fd6; }
.btn-primary:disabled { opacity: 0.6; cursor: not-allowed; }

.btn-primary-sm {
  padding: 0.375rem 0.875rem;
  background: #667eea;
  color: white;
  border: none;
  border-radius: 6px;
  font-size: 0.8rem;
  cursor: pointer;
}

.btn-primary-sm:hover { background: #5a6fd6; }

.btn-secondary-sm {
  padding: 0.375rem 0.875rem;
  background: #f3f4f6;
  color: #374151;
  border: 1px solid #d1d5db;
  border-radius: 6px;
  font-size: 0.8rem;
  cursor: pointer;
}

.btn-action {
  padding: 3px 8px;
  background: #f3f4f6;
  color: #374151;
  border: 1px solid #d1d5db;
  border-radius: 4px;
  font-size: 0.78rem;
  cursor: pointer;
}

.btn-action:hover { background: #e5e7eb; }

.btn-action-danger {
  padding: 3px 8px;
  background: #fee2e2;
  color: #dc2626;
  border: 1px solid #fca5a5;
  border-radius: 4px;
  font-size: 0.78rem;
  cursor: pointer;
}

.btn-action-danger:hover { background: #fca5a5; }

.btn-add {
  margin-top: 0.75rem;
  padding: 0.375rem 0.875rem;
  background: none;
  color: #667eea;
  border: 1px dashed #a5b4fc;
  border-radius: 6px;
  font-size: 0.85rem;
  cursor: pointer;
}

.btn-add:hover { background: #eef2ff; }

.badge-verified {
  font-size: 0.75rem;
  color: #16a34a;
  background: #dcfce7;
  border: 1px solid #86efac;
  border-radius: 99px;
  padding: 1px 6px;
}

.badge-unverified {
  font-size: 0.75rem;
  font-weight: 700;
  color: #b45309;
  background: #fef3c7;
  border: 1px solid #fcd34d;
  border-radius: 99px;
  padding: 1px 6px;
}

.btn-action-verify {
  padding: 3px 8px;
  background: #eff6ff;
  color: #1d4ed8;
  border: 1px solid #93c5fd;
  border-radius: 4px;
  font-size: 0.78rem;
  cursor: pointer;
}

.btn-action-verify:hover { background: #dbeafe; }

/* Modal */
.modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.45);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 100;
}

.modal {
  background: white;
  border-radius: 10px;
  padding: 1.75rem;
  width: 320px;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.18);
}

.modal h3 { margin: 0 0 0.5rem; font-size: 1.1rem; }

.modal-hint {
  color: #6b7280;
  font-size: 0.875rem;
  margin-bottom: 1.25rem;
}

.code-input {
  width: 100%;
  padding: 0.75rem;
  font-size: 1.5rem;
  letter-spacing: 0.3em;
  text-align: center;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  box-sizing: border-box;
  margin-bottom: 1.25rem;
}

.code-input:focus {
  outline: none;
  border-color: #667eea;
  box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.15);
}

.modal-actions { display: flex; gap: 0.5rem; justify-content: flex-end; }
</style>
