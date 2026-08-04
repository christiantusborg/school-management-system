<template>
  <div class="pce">
    <div class="pce-head">
      <h4>Contacts</h4>
      <span class="muted">Named contacts by role, each with any number of contact methods.</span>
    </div>
    <p v-if="error" class="pce-err">{{ error }}</p>

    <div v-for="(c, ci) in contacts" :key="ci" class="pce-card" :class="{ 'pce-locked': isLocked(c) }">
      <div class="pce-row">
        <select v-model="c.partnerContactTypeId" class="pce-inp pce-type" :disabled="isLocked(c)">
          <option v-for="t in typesFor(c)" :key="t.partnerContactTypeId" :value="t.partnerContactTypeId">{{ t.name }}</option>
        </select>
        <input v-model="c.name" class="pce-inp pce-name" placeholder="Contact name…" :disabled="isLocked(c)" />
        <span v-if="isLocked(c)" class="pce-lock" title="Owner contacts can only be changed by the Admission Office">🔒 Admission only</span>
        <button v-else class="pce-x" title="Remove contact" @click="contacts.splice(ci, 1)">✕</button>
      </div>
      <div v-for="(m, mi) in c.methods" :key="mi" class="pce-method-row">
        <select v-model="m.contactMethodTypeId" class="pce-inp pce-method" :disabled="isLocked(c)">
          <option v-for="mt in methods" :key="mt.contactMethodTypeId" :value="mt.contactMethodTypeId">{{ mt.name }}</option>
        </select>
        <input v-model="m.value" class="pce-inp pce-value" placeholder="Address / number / handle…" :disabled="isLocked(c)" />
        <button v-if="!isLocked(c)" class="pce-x" title="Remove method" @click="c.methods.splice(mi, 1)">✕</button>
      </div>
      <button v-if="!isLocked(c)" class="pce-add" @click="addMethod(c)">+ Add contact method</button>
    </div>
    <p v-if="!contacts.length" class="muted" style="margin:.3rem 0;">No contacts yet.</p>

    <div class="pce-actions">
      <button class="pce-btn" @click="addContact">+ Add contact</button>
      <button class="pce-btn pce-btn-save" :disabled="busy" @click="save">{{ busy ? 'Saving…' : 'Save contacts' }}</button>
      <span v-if="saved" class="pce-ok">✓ Saved</span>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, watch } from 'vue'
import api from '../../api/client.js'

const props = defineProps({
  // 'admin' edits any type incl. Owner (needs partnerId); 'partner' edits
  // everything except Owner contacts, which render locked.
  mode: { type: String, default: 'admin' },
  partnerId: { type: String, default: '' },
})

const contacts = ref([])
const methods = ref([])
const types = ref([])
const busy = ref(false)
const saved = ref(false)
const error = ref('')

const contactsUrl = () => props.mode === 'admin'
  ? `/v1/admin/school/partners/${props.partnerId}/contacts`
  : '/v1/partner/profile/contacts'

function isLocked(c) {
  if (props.mode === 'admin') return false
  const t = types.value.find(x => x.partnerContactTypeId === c.partnerContactTypeId)
  return !!t?.locked
}
// Partner-editable contacts can't be switched INTO a locked type.
function typesFor(c) {
  return isLocked(c) ? types.value : types.value.filter(t => props.mode === 'admin' || !t.locked)
}

async function load() {
  if (props.mode === 'admin' && !props.partnerId) { contacts.value = []; return }
  error.value = ''
  try {
    if (props.mode === 'admin') {
      const [c, m, t] = await Promise.all([
        api.get(contactsUrl()),
        api.get('/v1/school/contact-methods/options'),
        api.get('/v1/school/contact-types/options'),
      ])
      contacts.value = normalize(c.data.items)
      methods.value = m.data.items ?? []
      types.value = (t.data.items ?? []).map(x => ({ ...x, locked: false }))
    } else {
      const [c, o] = await Promise.all([
        api.get(contactsUrl()),
        api.get('/v1/partner/profile/contact-options'),
      ])
      contacts.value = normalize(c.data.items)
      methods.value = o.data.methods ?? []
      types.value = o.data.types ?? []
    }
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to load contacts'
  }
}
function normalize(items) {
  return (items ?? []).map(c => ({
    partnerContactTypeId: c.partnerContactTypeId,
    name: c.name ?? '',
    methods: (c.methods ?? []).map(m => ({ contactMethodTypeId: m.contactMethodTypeId, value: m.value ?? '' })),
  }))
}
function addContact() {
  const firstType = types.value.find(t => props.mode === 'admin' || !t.locked)
  contacts.value.push({
    partnerContactTypeId: firstType?.partnerContactTypeId ?? '',
    name: '',
    methods: [{ contactMethodTypeId: methods.value[0]?.contactMethodTypeId ?? '', value: '' }],
  })
}
function addMethod(c) {
  c.methods.push({ contactMethodTypeId: methods.value[0]?.contactMethodTypeId ?? '', value: '' })
}
async function save() {
  if (busy.value) return
  busy.value = true
  error.value = ''
  saved.value = false
  try {
    // Partner mode: locked (Owner) contacts stay server-side; send the rest.
    const payload = contacts.value.filter(c => !isLocked(c))
    const res = await api.put(contactsUrl(), { contacts: payload })
    contacts.value = normalize(res.data.items)
    saved.value = true
    setTimeout(() => { saved.value = false }, 2500)
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to save contacts'
  } finally {
    busy.value = false
  }
}

watch(() => props.partnerId, load)
onMounted(load)
</script>

<style scoped>
.pce { background: #fff; border: 1px solid #e0e6ee; border-radius: 8px; padding: .7rem .9rem; margin-bottom: .8rem; }
.pce-head { display: flex; align-items: baseline; gap: .6rem; margin-bottom: .5rem; }
.pce-head h4 { margin: 0; font-size: .92rem; color: #0b2e59; }
.muted { font-size: .76rem; color: #5f6e85; }
.pce-err { color: #b42318; font-size: .8rem; margin: .2rem 0; }
.pce-card { border: 1px solid #e8edf3; border-radius: 7px; padding: .5rem .65rem; margin-bottom: .45rem; background: #fbfcfe; }
.pce-locked { background: #f4f6f9; }
.pce-row, .pce-method-row { display: flex; align-items: center; gap: .45rem; margin-bottom: .3rem; flex-wrap: wrap; }
.pce-method-row { padding-left: 1rem; }
.pce-inp { padding: .32rem .5rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .8rem; }
.pce-inp:disabled { background: #eef1f5; color: #667; }
.pce-type { width: 150px; }
.pce-name { flex: 1; min-width: 180px; }
.pce-method { width: 170px; }
.pce-value { flex: 1; min-width: 200px; }
.pce-x { background: none; border: none; color: #b42318; cursor: pointer; font-size: .85rem; }
.pce-lock { font-size: .72rem; color: #8a6b16; background: #fff1cc; padding: 2px 8px; border-radius: 10px; }
.pce-add { background: none; border: 1px dashed #a0b8d0; color: #0b2e59; border-radius: 5px; padding: .2rem .55rem; font-size: .74rem; cursor: pointer; margin-left: 1rem; }
.pce-actions { display: flex; align-items: center; gap: .5rem; margin-top: .5rem; }
.pce-btn { padding: .35rem .7rem; font-size: .8rem; border: 1px solid #cfd7e3; background: #fff; border-radius: 5px; cursor: pointer; }
.pce-btn-save { border-color: #1c7a4a; color: #1c7a4a; font-weight: 600; }
.pce-ok { color: #1c7a4a; font-size: .8rem; font-weight: 600; }
</style>
