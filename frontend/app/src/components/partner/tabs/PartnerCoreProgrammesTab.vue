<template>
  <div class="cp-tab">
    <p class="hint-text">Toggle specializations to grant or revoke this partner's access to IBSS core programmes. A partner can further disable individual specializations from their own portal.</p>

    <div v-if="loading" class="loading-row">Loading…</div>
    <div v-else-if="error" class="err-banner">{{ error }}</div>
    <div v-else-if="programmes.length === 0" class="empty-state-card">No core programmes configured yet.</div>

    <div v-else class="prog-list">
      <div v-for="p in programmes" :key="p.programmeId" class="prog-card">
        <div class="prog-head" @click="toggleOpen(p.programmeId)">
          <div>
            <strong>{{ p.name }}</strong>
            <span class="mono-code">{{ p.code }}</span>
          </div>
          <div>
            <span class="prog-count">{{ grantedInProg(p.programmeId) }} / {{ specializationsByProg(p.programmeId).length }}</span>
            <span class="caret">{{ openProgs.has(p.programmeId) ? '▾' : '▸' }}</span>
          </div>
        </div>
        <div v-if="openProgs.has(p.programmeId)" class="specialization-list">
          <div v-for="m in specializationsByProg(p.programmeId)" :key="m.specializationId" class="specialization-row">
            <label>
              <input type="checkbox" :checked="isGranted(m.specializationId)" :disabled="busy.has(m.specializationId)" @change="onToggle(m, $event.target.checked)" />
              <span>{{ m.name }}</span>
            </label>
            <span v-if="partnerDisabled(m.specializationId)" class="pill pill-muted" title="Partner has disabled this specialization from their portal">Partner-disabled</span>
          </div>
          <p v-if="specializationsByProg(p.programmeId).length === 0" class="empty-note">No specializations defined.</p>
          <LetterButtonsRow :programme-id="p.programmeId" :programme-name="p.name" :partner-id="partnerId" />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, watch } from 'vue'
import apiClient from '../../../api/client.js'
import LetterButtonsRow from '../../letters/LetterButtonsRow.vue'

const props = defineProps({ partnerId: { type: String, required: true } })

const programmes = ref([])
const specializations = ref([])
const access = ref([])
const loading = ref(false)
const error = ref('')
const openProgs = reactive(new Set())
const busy = reactive(new Set())

function specializationsByProg(progId) { return specializations.value.filter(m => m.programmeId === progId && !m.deletedAt) }
function isGranted(specializationId) { return access.value.some(a => a.specializationId === specializationId) }
function partnerDisabled(specializationId) { return access.value.some(a => a.specializationId === specializationId && a.disabledByPartner) }
function grantedInProg(progId) { return specializationsByProg(progId).filter(m => isGranted(m.specializationId)).length }
function toggleOpen(progId) { openProgs.has(progId) ? openProgs.delete(progId) : openProgs.add(progId) }

async function onToggle(specialization, grant) {
  if (busy.has(specialization.specializationId)) return
  busy.add(specialization.specializationId)
  try {
    if (grant) {
      await apiClient.post(`/v1/admin/school/partners/${props.partnerId}/programme-access`, { specializationIds: [specialization.specializationId] })
      access.value.push({ programmeId: specialization.programmeId, specializationId: specialization.specializationId, disabledByPartner: false })
    } else {
      await apiClient.delete(`/v1/admin/school/partners/${props.partnerId}/programme-access/${specialization.specializationId}`)
      access.value = access.value.filter(a => a.specializationId !== specialization.specializationId)
    }
  } catch (e) {
    error.value = e.response?.data ?? e.message ?? 'Operation failed'
  } finally {
    busy.delete(specialization.specializationId)
  }
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    const [pRes, mRes, aRes] = await Promise.all([
      apiClient.get('/v1/school/programmes?ownership=core'),
      apiClient.get('/v1/school/specializations'),
      apiClient.get(`/v1/admin/school/partners/${props.partnerId}/programme-access`),
    ])
    programmes.value = (pRes.data.items ?? []).filter(p => !p.deletedAt)
    specializations.value = mRes.data.items ?? []
    access.value = aRes.data.items ?? []
    programmes.value.forEach(p => openProgs.add(p.programmeId))
  } catch (e) {
    error.value = e.response?.data?.message ?? e.message ?? 'Failed to load'
  } finally {
    loading.value = false
  }
}

onMounted(load)
watch(() => props.partnerId, load)
</script>

<style scoped>
.cp-tab { padding: .25rem 0; }
.hint-text { font-size: .85rem; color: #5f6e85; margin: 0 0 .75rem; }
.prog-list { display: flex; flex-direction: column; gap: .55rem; }
.prog-card { border: 1px solid #e0e6ee; border-radius: 8px; overflow: hidden; }
.prog-head { display: flex; justify-content: space-between; align-items: center; padding: .65rem .85rem; cursor: pointer; background: #f6f9fd; }
.prog-head > div { display: flex; align-items: center; gap: .5rem; }
.mono-code { font-family: monospace; font-size: .78rem; color: #6b7888; background: #fff; border: 1px solid #e1e6ed; padding: 1px 6px; border-radius: 4px; }
.prog-count { font-size: .78rem; color: #5f6e85; }
.caret { font-size: .85rem; color: #6b7888; }
.specialization-list { padding: .5rem .9rem; display: flex; flex-direction: column; gap: .3rem; border-top: 1px solid #e8edf3; }
.specialization-row { display: flex; justify-content: space-between; align-items: center; font-size: .9rem; }
.specialization-row label { display: flex; gap: .5rem; align-items: center; cursor: pointer; }
.pill { font-size: .7rem; padding: 1px 7px; border-radius: 10px; }
.pill-muted { background: #ecf0f6; color: #5f6e85; }
.empty-note { font-size: .82rem; color: #8a93a4; margin: 0; }
.loading-row { padding: 1rem; color: #5f6e85; font-size: .9rem; }
.err-banner { background: #fde7e5; color: #a8241e; padding: .55rem .8rem; border-radius: 6px; margin: .4rem 0; font-size: .88rem; }
.empty-state-card { padding: 1rem; background: #f6f9fd; color: #5f6e85; border-radius: 8px; text-align: center; }
</style>
