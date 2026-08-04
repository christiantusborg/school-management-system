<template>
  <div class="crms">
    <p v-if="err" class="err-banner">{{ err }}</p>

    <div class="crms-section">
      <h4>Pipelines &amp; stages</h4>
      <div v-for="p in cfg.pipelines" :key="p.crmPipelineId" class="crms-pipe">
        <div class="crms-pipe-head">
          <strong>{{ p.name }}</strong>
          <span v-if="p.isDefault" class="crms-def">default</span>
          <button v-else class="crms-btn" @click="makeDefault(p)">Make default</button>
          <button class="crms-btn crms-danger" @click="deletePipeline(p)">Delete</button>
        </div>
        <table class="crms-tbl">
          <thead><tr><th>Stage</th><th>Colour</th><th>Type</th><th>SLA hours</th><th></th></tr></thead>
          <tbody>
            <tr v-for="(st, i) in stagesFor(p.crmPipelineId)" :key="i">
              <td><input v-model="st.name" class="crms-inp" /></td>
              <td><input v-model="st.color" type="color" /></td>
              <td>
                <select v-model.number="st.stageType" class="crms-inp">
                  <option :value="0">Open</option><option :value="1">Won</option><option :value="2">Lost</option>
                </select>
              </td>
              <td><input v-model.number="st.slaHours" type="number" min="1" class="crms-inp crms-sla" placeholder="—" /></td>
              <td><button class="crms-btn crms-danger" @click="removeStage(p.crmPipelineId, i)">✕</button></td>
            </tr>
          </tbody>
        </table>
        <div class="crms-row">
          <button class="crms-btn" @click="addStage(p.crmPipelineId)">+ Add stage</button>
          <button class="crms-btn crms-primary" :disabled="busy" @click="saveStages(p)">Save stages</button>
        </div>
      </div>
      <div class="crms-row">
        <input v-model="newPipelineName" class="crms-inp" placeholder="New pipeline name…" />
        <button class="crms-btn" :disabled="!newPipelineName.trim() || busy" @click="createPipeline">+ Add pipeline</button>
      </div>
    </div>

    <div class="crms-section">
      <h4>Lead sources</h4>
      <div class="crms-chiprow">
        <span v-for="s in cfg.sources" :key="s.crmLeadSourceId" class="crms-chip">
          {{ s.name }} <button class="crms-chip-x" @click="deleteSource(s)">✕</button>
        </span>
      </div>
      <div class="crms-row">
        <input v-model="newSourceName" class="crms-inp" placeholder="New source…" />
        <button class="crms-btn" :disabled="!newSourceName.trim() || busy" @click="createSource">+ Add source</button>
      </div>
    </div>

    <div class="crms-section">
      <h4>Auto-assignment</h4>
      <label class="crms-radio"><input type="radio" :value="0" v-model.number="assignment.strategy" /> Manual (no auto-assign)</label>
      <label class="crms-radio"><input type="radio" :value="1" v-model.number="assignment.strategy" /> Round-robin over this pool:</label>
      <div class="crms-chiprow">
        <label v-for="u in salesStaff" :key="u.userId" class="crms-pool">
          <input type="checkbox" :value="u.userId" v-model="assignment.memberUserIds" /> {{ u.name }}
        </label>
      </div>
      <button class="crms-btn crms-primary" :disabled="busy" @click="saveAssignment">Save assignment</button>
      <span v-if="savedOk" class="crms-ok">✓ Saved</span>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import api from '../../api/client.js'

const emit = defineEmits(['changed'])
const cfg = reactive({ pipelines: [], stages: [], sources: [] })
const assignment = reactive({ strategy: 0, memberUserIds: [] })
const salesStaff = ref([])
const newPipelineName = ref('')
const newSourceName = ref('')
const busy = ref(false)
const savedOk = ref(false)
const err = ref('')

function stagesFor(pid) { return cfg.stages.filter(s => s.pipelineId === pid) }
function addStage(pid) { cfg.stages.push({ crmStageId: null, pipelineId: pid, name: '', color: '#1058a4', stageType: 0, slaHours: null }) }
function removeStage(pid, idx) {
  const mine = stagesFor(pid)
  const target = mine[idx]
  cfg.stages.splice(cfg.stages.indexOf(target), 1)
}

async function load() {
  try {
    const [c, s] = await Promise.all([
      api.get('/v1/admin/crm/settings'),
      api.get('/v1/admin/sales-staff').catch(() => ({ data: { items: [] } })),
    ])
    cfg.pipelines = c.data.pipelines ?? []
    cfg.stages = c.data.stages ?? []
    cfg.sources = c.data.sources ?? []
    assignment.strategy = c.data.assignment?.strategy ?? 0
    assignment.memberUserIds = [...(c.data.assignment?.memberUserIds ?? [])]
    salesStaff.value = (s.data.items ?? []).map(u => ({ userId: u.userId, name: u.name ?? u.userName }))
  } catch (e) { err.value = e.response?.data?.error ?? e.message }
}
async function run(fn) {
  if (busy.value) return
  busy.value = true; err.value = ''; savedOk.value = false
  try { await fn(); savedOk.value = true; setTimeout(() => { savedOk.value = false }, 2000); await load(); emit('changed') }
  catch (e) { err.value = e.response?.data?.error ?? e.message }
  finally { busy.value = false }
}
const createPipeline = () => run(async () => { await api.post('/v1/admin/crm/settings/pipelines', { name: newPipelineName.value.trim() }); newPipelineName.value = '' })
const makeDefault = p => run(() => api.patch(`/v1/admin/crm/settings/pipelines/${p.crmPipelineId}`, { isDefault: true }))
const deletePipeline = p => { if (confirm(`Delete pipeline "${p.name}"?`)) run(() => api.delete(`/v1/admin/crm/settings/pipelines/${p.crmPipelineId}`)) }
const saveStages = p => run(() => api.put(`/v1/admin/crm/settings/pipelines/${p.crmPipelineId}/stages`, {
  stages: stagesFor(p.crmPipelineId).map(s => ({ stageId: s.crmStageId, name: s.name, color: s.color, stageType: s.stageType, slaHours: s.slaHours || null })),
}))
const createSource = () => run(async () => { await api.post('/v1/admin/crm/settings/sources', { name: newSourceName.value.trim() }); newSourceName.value = '' })
const deleteSource = s => { if (confirm(`Remove source "${s.name}"?`)) run(() => api.delete(`/v1/admin/crm/settings/sources/${s.crmLeadSourceId}`)) }
const saveAssignment = () => run(() => api.put('/v1/admin/crm/settings/assignment', { strategy: assignment.strategy, memberUserIds: assignment.memberUserIds }))

onMounted(load)
</script>

<style scoped>
.crms { max-width: 900px; }
.err-banner { background: #fde7e7; color: #8a1515; padding: .5rem .8rem; border-radius: 6px; font-size: .84rem; }
.crms-section { background: #fff; border: 1px solid #e0e6ee; border-radius: 8px; padding: .8rem 1rem; margin-bottom: .8rem; }
.crms-section h4 { margin: 0 0 .6rem; font-size: .92rem; color: #0b2e59; }
.crms-pipe { border: 1px solid #e8edf3; border-radius: 7px; padding: .55rem .7rem; margin-bottom: .6rem; }
.crms-pipe-head { display: flex; align-items: center; gap: .5rem; margin-bottom: .4rem; }
.crms-def { font-size: .68rem; background: #d7f0df; color: #1c7a4a; border-radius: 8px; padding: 1px 7px; font-weight: 700; }
.crms-tbl { width: 100%; border-collapse: collapse; font-size: .82rem; }
.crms-tbl th { text-align: left; font-size: .66rem; text-transform: uppercase; color: #6b7888; padding: .25rem .35rem; }
.crms-tbl td { padding: .2rem .35rem; }
.crms-inp { padding: .32rem .5rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .82rem; width: 100%; }
.crms-sla { width: 90px; }
.crms-row { display: flex; align-items: center; gap: .5rem; margin-top: .5rem; flex-wrap: wrap; }
.crms-row .crms-inp { width: 240px; }
.crms-btn { padding: .32rem .65rem; border: 1px solid #cfd7e3; background: #fff; border-radius: 5px; font-size: .78rem; cursor: pointer; }
.crms-primary { background: #0b2e59; color: #fff; border-color: #0b2e59; }
.crms-danger { border-color: #b63329; color: #b63329; }
.crms-chiprow { display: flex; gap: .4rem; flex-wrap: wrap; margin-bottom: .4rem; }
.crms-chip { background: #eef3fb; color: #1a4d8c; border-radius: 10px; padding: .2rem .6rem; font-size: .78rem; }
.crms-chip-x { border: none; background: none; color: #b63329; cursor: pointer; font-size: .7rem; }
.crms-radio { display: block; font-size: .84rem; margin-bottom: .3rem; }
.crms-pool { font-size: .82rem; background: #f6f9fd; border: 1px solid #e0e6ee; border-radius: 6px; padding: .25rem .55rem; }
.crms-ok { color: #1c7a4a; font-size: .8rem; font-weight: 700; margin-left: .5rem; }
</style>
