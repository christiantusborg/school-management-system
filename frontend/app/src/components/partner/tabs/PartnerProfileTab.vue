<template>
  <div class="profile-tab">
    <div v-if="loading" class="loading-row">Loading…</div>
    <div v-else-if="error" class="err-banner">{{ error }}</div>
    <template v-else-if="profile">
      <div v-if="!editing" class="view-grid">
        <div class="grid-row"><span>Name</span><strong>{{ profile.name }}</strong></div>
        <div class="grid-row">
          <span>Slug</span>
          <strong>
            <code class="slug-code">{{ profile.slug }}</code>
            <span class="slug-url">student signup URL: <code>{{ signupUrl }}</code></span>
          </strong>
        </div>
        <div class="grid-row"><span>Contact</span><strong>{{ contactSummary || '—' }}</strong></div>
        <div class="grid-row"><span>Address</span><strong>{{ addressSummary || '—' }}</strong></div>
        <div class="grid-row"><span>Website</span><strong>{{ profile.website || '—' }}</strong></div>
        <div class="grid-row"><span>Registration no.</span><strong>{{ profile.registrationNumber || '—' }}</strong></div>
        <div class="grid-row"><span>Tax ID</span><strong>{{ profile.taxId || '—' }}</strong></div>
        <div class="grid-row"><span>Tier</span><strong>{{ profile.tier || '—' }}</strong></div>
        <div class="grid-row"><span>Contract</span><strong>{{ contractSummary || '—' }}</strong></div>
        <div v-if="profile.internalNotes" class="grid-row"><span>Internal notes</span><strong class="multiline">{{ profile.internalNotes }}</strong></div>

        <div class="actions"><button class="btn-primary-sm" @click="startEdit">Edit</button></div>
      </div>

      <div v-else class="edit-form">
        <div class="row-2">
          <div class="field"><label>Name</label><input v-model="form.name" /></div>
          <div class="field">
            <label>Slug (student signup URL)</label>
            <input v-model="form.slug" placeholder="e.g. bloom-business-school" />
            <p v-if="slugError" class="field-error">{{ slugError }}</p>
            <p v-else class="field-hint">2–40 chars · lowercase letters, digits, hyphens · must be unique</p>
          </div>
        </div>
        <div class="row-2">
          <div class="field"><label>Contact person name</label><input v-model="form.contactPersonName" /></div>
          <div class="field"><label>Title</label><input v-model="form.contactPersonTitle" /></div>
        </div>
        <div class="row-2">
          <div class="field"><label>Contact email</label><input v-model="form.contactPersonEmail" /></div>
          <div class="field"><label>Contact phone</label><input v-model="form.contactPersonPhone" /></div>
        </div>
        <div class="field"><label>Address line 1</label><input v-model="form.addressLine1" /></div>
        <div class="field"><label>Address line 2</label><input v-model="form.addressLine2" /></div>
        <div class="row-3">
          <div class="field"><label>City</label><input v-model="form.city" /></div>
          <div class="field"><label>State / Region</label><input v-model="form.stateRegion" /></div>
          <div class="field"><label>Postal code</label><input v-model="form.postalCode" /></div>
        </div>
        <div class="field"><label>Country</label><input v-model="form.country" /></div>
        <div class="row-2">
          <div class="field"><label>Website</label><input v-model="form.website" /></div>
          <div class="field"><label>Registration number</label><input v-model="form.registrationNumber" /></div>
        </div>
        <div class="row-2">
          <div class="field"><label>Tax ID</label><input v-model="form.taxId" /></div>
          <div class="field"><label>Tier</label>
            <select v-model="form.tier">
              <option value="">—</option>
              <option>Gold</option><option>Silver</option><option>Bronze</option><option>Standard</option>
            </select>
          </div>
        </div>
        <div class="row-2">
          <div class="field"><label>Contract start</label><input v-model="form.contractStart" type="date" /></div>
          <div class="field"><label>Contract end</label><input v-model="form.contractEnd" type="date" /></div>
        </div>
        <div class="field"><label>Internal notes</label><textarea v-model="form.internalNotes" rows="2" /></div>

        <div v-if="saveError" class="err-banner">{{ saveError }}</div>
        <div class="actions">
          <button class="btn-sm" @click="editing = false">Cancel</button>
          <button class="btn-primary-sm" :disabled="saving || !!slugError" @click="save">{{ saving ? 'Saving…' : 'Save' }}</button>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted, watch } from 'vue'
import apiClient from '../../../api/client.js'

const props = defineProps({ partnerId: { type: String, required: true } })
const emit = defineEmits(['updated'])

const profile = ref(null)
const loading = ref(false)
const error = ref('')

const editing = ref(false)
const saving = ref(false)
const saveError = ref('')
const form = reactive({
  name: '', slug: '',
  contactPersonName: '', contactPersonTitle: '', contactPersonEmail: '', contactPersonPhone: '',
  addressLine1: '', addressLine2: '', city: '', stateRegion: '', postalCode: '', country: '',
  website: '', registrationNumber: '', taxId: '',
  contractStart: '', contractEnd: '', tier: '', internalNotes: '',
})

// Student signup links use `?partner=<slug>` — surface the URL so changing the
// slug clearly shows what students will need to type.
const SLUG_RE = /^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$/
const signupUrl = computed(() => profile.value?.slug
  ? `${window.location.origin}/#/apply?partner=${profile.value.slug}`
  : '—')
const slugError = computed(() => {
  const s = form.slug?.trim()
  if (!s) return 'Slug is required'
  if (s.length < 2 || s.length > 40) return 'Must be 2–40 characters'
  if (!SLUG_RE.test(s)) return 'Only lowercase letters, digits, and hyphens'
  return ''
})

const contactSummary = computed(() => {
  if (!profile.value) return ''
  const parts = []
  if (profile.value.contactPersonName) parts.push(profile.value.contactPersonName)
  if (profile.value.contactPersonTitle) parts.push(`(${profile.value.contactPersonTitle})`)
  const extras = [profile.value.contactPersonEmail, profile.value.contactPersonPhone].filter(Boolean).join(' · ')
  if (extras) parts.push('— ' + extras)
  return parts.join(' ')
})
const addressSummary = computed(() => {
  if (!profile.value) return ''
  return [profile.value.addressLine1, profile.value.addressLine2, profile.value.city, profile.value.stateRegion, profile.value.postalCode, profile.value.country].filter(Boolean).join(', ')
})
const contractSummary = computed(() => {
  if (!profile.value) return ''
  const fmt = d => d ? new Date(d).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }) : ''
  return [fmt(profile.value.contractStart), fmt(profile.value.contractEnd)].filter(Boolean).join(' → ')
})

async function load() {
  loading.value = true
  error.value = ''
  try {
    const res = await apiClient.get(`/v1/admin/school/partners/${props.partnerId}`)
    profile.value = res.data
  } catch (e) {
    error.value = e.response?.data?.message ?? e.message ?? 'Failed to load partner'
  } finally {
    loading.value = false
  }
}

function startEdit() {
  const p = profile.value
  Object.assign(form, {
    name: p.name ?? '',
    slug: p.slug ?? '',
    contactPersonName: p.contactPersonName ?? '',
    contactPersonTitle: p.contactPersonTitle ?? '',
    contactPersonEmail: p.contactPersonEmail ?? '',
    contactPersonPhone: p.contactPersonPhone ?? '',
    addressLine1: p.addressLine1 ?? '',
    addressLine2: p.addressLine2 ?? '',
    city: p.city ?? '',
    stateRegion: p.stateRegion ?? '',
    postalCode: p.postalCode ?? '',
    country: p.country ?? '',
    website: p.website ?? '',
    registrationNumber: p.registrationNumber ?? '',
    taxId: p.taxId ?? '',
    contractStart: p.contractStart ? p.contractStart.slice(0, 10) : '',
    contractEnd: p.contractEnd ? p.contractEnd.slice(0, 10) : '',
    tier: p.tier ?? '',
    internalNotes: p.internalNotes ?? '',
  })
  saveError.value = ''
  editing.value = true
}

async function save() {
  if (slugError.value) { saveError.value = slugError.value; return }
  saving.value = true
  saveError.value = ''
  try {
    const body = { ...form, slug: form.slug.trim().toLowerCase() }
    if (!body.contractStart) delete body.contractStart
    if (!body.contractEnd) delete body.contractEnd
    await apiClient.patch(`/v1/admin/school/partners/${props.partnerId}`, body)
    editing.value = false
    await load()
    emit('updated')
  } catch (e) {
    const err = e.response?.data?.error ?? e.response?.data ?? e.message ?? 'Failed to save'
    saveError.value = err === 'slug_not_unique' ? 'That slug is already taken by another partner.'
                    : err === 'invalid_slug'    ? 'Slug format is invalid.'
                    : err
  } finally {
    saving.value = false
  }
}

onMounted(load)
watch(() => props.partnerId, load)
</script>

<style scoped>
.profile-tab { padding: .25rem 0; }
.view-grid { display: flex; flex-direction: column; gap: .35rem; }
.grid-row { display: grid; grid-template-columns: 180px 1fr; padding: .5rem .75rem; background: #f6f9fd; border-radius: 6px; font-size: .9rem; }
.grid-row span { color: #5f6e85; }
.grid-row strong { color: #0a264f; }
.multiline { white-space: pre-wrap; }
.actions { display: flex; gap: .5rem; justify-content: flex-end; margin-top: .75rem; }
.row-2 { display: grid; grid-template-columns: 1fr 1fr; gap: .6rem; }
.row-3 { display: grid; grid-template-columns: 1.4fr 1fr 1fr; gap: .6rem; }
.field { display: flex; flex-direction: column; margin-bottom: .55rem; }
.field label { font-size: .78rem; color: #5f6e85; margin-bottom: .2rem; }
.field input, .field select, .field textarea { padding: .5rem .65rem; border: 1px solid #cfd7e3; border-radius: 6px; font-size: .9rem; font-family: inherit; }
.err-banner { background: #fde7e5; color: #a8241e; padding: .55rem .8rem; border-radius: 6px; font-size: .88rem; margin: .5rem 0; }
.loading-row { padding: 1rem; color: #5f6e85; font-size: .9rem; }
.slug-code { background: #eef2f9; padding: 1px 8px; border-radius: 4px; font-family: ui-monospace, monospace; font-size: .85rem; }
.slug-url { display: block; color: #5f6e85; font-size: .78rem; font-weight: 400; margin-top: .2rem; }
.slug-url code { font-family: ui-monospace, monospace; }
.field-hint  { font-size: .72rem; color: #5f6e85; margin: .2rem 0 0; }
.field-error { font-size: .72rem; color: #b91c1c; margin: .2rem 0 0; }
</style>
