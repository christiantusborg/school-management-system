<template>
  <div class="crm">
    <div class="crm-subnav">
      <button v-for="t in subTabs" :key="t.id"
              :class="['crm-sub-btn', { active: sub === t.id }]" @click="switchSub(t.id)">{{ t.label }}</button>
      <select v-if="sub === 'board' && board.pipelines.length > 1" v-model="pipelineId" class="crm-pipe-sel" @change="loadBoard">
        <option v-for="p in board.pipelines" :key="p.crmPipelineId" :value="p.crmPipelineId">{{ p.name }}</option>
      </select>
      <button v-if="sub === 'board'" class="crm-btn crm-btn-primary" @click="openNewLead">+ New lead</button>
    </div>
    <p v-if="err" class="err-banner">{{ err }}</p>

    <!-- ══ BOARD ══ -->
    <div v-if="sub === 'board'" class="crm-board">
      <div v-for="st in board.stages" :key="st.crmStageId" class="crm-col"
           @dragover.prevent @drop="onDrop(st)">
        <div class="crm-col-head" :style="{ borderTopColor: st.color || '#cfd7e3' }">
          <strong>{{ st.name }}</strong>
          <span class="crm-col-count">{{ leadsFor(st.crmStageId).length }}</span>
          <span v-if="st.stageType === 1" class="crm-col-tag won">WON</span>
          <span v-else-if="st.stageType === 2" class="crm-col-tag lost">LOST</span>
        </div>
        <div v-for="l in leadsFor(st.crmStageId)" :key="l.crmLeadId"
             class="crm-card" :class="{ rotting: l.rotting, converted: l.status === 1 }"
             draggable="true" @dragstart="dragLeadId = l.crmLeadId" @click="openLead(l.crmLeadId)">
          <div class="crm-card-top">
            <strong class="crm-card-name">{{ l.name }}</strong>
            <span v-if="l.hot" class="crm-hot" title="Hot lead">🔥</span>
          </div>
          <div class="crm-card-meta">
            <span v-if="l.valueBand != null" class="crm-band">{{ BANDS[l.valueBand] }}</span>
            <span class="crm-score" :title="`Score ${l.score}`">{{ l.score }}</span>
            <span v-if="l.rotting" class="crm-rot" title="Past the stage SLA">⏰</span>
          </div>
          <div class="crm-card-sub">
            <span>{{ l.assignedToName || 'Unassigned' }}</span>
            <span v-if="l.nextDueAt" :class="{ 'crm-due-over': new Date(l.nextDueAt) < new Date() }">
              ⏱ {{ (l.nextDueAt || '').slice(0, 10) }}
            </span>
          </div>
        </div>
      </div>
    </div>

    <!-- ══ MY DAY ══ -->
    <div v-else-if="sub === 'myday'" class="crm-myday">
      <div class="crm-kpis">
        <button v-for="k in KPIS" :key="k.id" :class="['crm-kpi', { active: mydayBucket === k.id }]"
                @click="mydayBucket = k.id">
          <span class="crm-kpi-n">{{ myday.counts?.[k.id] ?? 0 }}</span>
          <span class="crm-kpi-l">{{ k.label }}</span>
        </button>
      </div>
      <div class="crm-bucket-list">
        <div v-for="l in (myday[mydayBucket] ?? [])" :key="l.crmLeadId" class="crm-bucket-row" @click="openLead(l.crmLeadId)">
          <strong>{{ l.name }}</strong>
          <span class="muted">{{ l.stageName }}</span>
          <span class="crm-score">{{ l.score }}</span>
        </div>
        <p v-if="!(myday[mydayBucket] ?? []).length" class="muted" style="padding:.6rem;">Nothing here. 🎉</p>
      </div>
    </div>

    <!-- ══ NEXT ACTIONS ══ -->
    <div v-else-if="sub === 'next'" class="crm-next">
      <div v-for="a in nextActions" :key="a.crmActivityId" class="crm-next-row" :class="{ over: a.overdue }">
        <span class="crm-next-icon">{{ KIND_ICONS[a.kind] }}</span>
        <span class="crm-next-lead" @click="openLead(a.crmLeadId)">{{ a.leadName }}</span>
        <span class="crm-next-body">{{ a.body || KIND_LABELS[a.kind] }}</span>
        <span class="crm-next-due">{{ (a.dueAt || '').slice(0, 16).replace('T', ' ') }}</span>
        <button class="crm-btn" @click="completeAction(a)">✓ Done</button>
      </div>
      <p v-if="!nextActions.length" class="muted" style="padding:.6rem;">No open follow-ups.</p>
    </div>

    <!-- ══ SOURCE REPORT ══ -->
    <div v-else-if="sub === 'report'" class="crm-report">
      <table class="crm-tbl">
        <thead><tr><th>Source</th><th>Total</th><th>Converted</th><th>Lost</th><th>Open</th><th>Conversion %</th><th>Win rate %</th></tr></thead>
        <tbody>
          <tr v-for="r in report" :key="r.sourceId ?? 'none'">
            <td>{{ r.sourceName }}</td><td>{{ r.total }}</td><td>{{ r.converted }}</td>
            <td>{{ r.lost }}</td><td>{{ r.open }}</td><td>{{ r.conversionPct }}%</td><td>{{ r.winRatePct }}%</td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- ══ IMPORT ══ -->
    <div v-else-if="sub === 'import'" class="crm-import">
      <p class="muted">Paste CSV rows: <code>name,email,phone,country</code> (header row optional).</p>
      <textarea v-model="importText" rows="10" class="crm-import-text" placeholder="Jane Doe,jane@x.com,+4512345678,Denmark"></textarea>
      <div class="crm-import-row">
        <select v-model="importSourceId" class="crm-pipe-sel">
          <option value="">— source —</option>
          <option v-for="s in options.sources" :key="s.crmLeadSourceId" :value="s.crmLeadSourceId">{{ s.name }}</option>
        </select>
        <button class="crm-btn" :disabled="importBusy" @click="runImport(false)">Dry run</button>
        <button class="crm-btn crm-btn-primary" :disabled="importBusy || !importPlan" @click="runImport(true)">Import</button>
        <span v-if="importPlan" class="muted">
          {{ importPlan.importable }} importable · {{ importPlan.duplicates }} duplicates · {{ importPlan.noName }} without name
          <strong v-if="importPlan.imported != null"> — imported {{ importPlan.imported }} ✓</strong>
        </span>
      </div>
    </div>

    <!-- ══ SETTINGS ══ -->
    <div v-else-if="sub === 'settings'" class="crm-settings">
      <CrmSettings @changed="loadBoard" />
    </div>

    <!-- ══ LEAD DRAWER ══ -->
    <Teleport to="body">
      <div v-if="drawer" class="crm-overlay" @click.self="drawer = null">
        <div class="crm-drawer">
          <div class="crm-drawer-head">
            <div>
              <h3>{{ drawer.lead?.name || 'New lead' }}
                <span v-if="drawer.lead?.hot" title="Hot lead">🔥</span>
                <span v-if="drawer.lead?.status === 1" class="crm-col-tag won">CONVERTED</span>
                <span v-else-if="drawer.lead?.status === 2" class="crm-col-tag lost">LOST</span>
              </h3>
              <p v-if="drawer.convertedStudentNumber" class="muted">Student: {{ drawer.convertedStudentNumber }}</p>
            </div>
            <button class="crm-x" @click="drawer = null">✕</button>
          </div>
          <p v-if="drawer.error" class="err-banner">{{ drawer.error }}</p>

          <div class="crm-drawer-body">
            <div class="crm-form">
              <div class="crm-f"><label>Name *</label><input v-model="drawer.form.name" /></div>
              <div class="crm-f"><label>Email</label><input v-model="drawer.form.email" @blur="checkDuplicate" />
                <p v-if="drawer.dupWarn" class="crm-dup">⚠ {{ drawer.dupWarn }}</p>
              </div>
              <div class="crm-f"><label>Phone</label><input v-model="drawer.form.phone" /></div>
              <div class="crm-f"><label>Country</label><input v-model="drawer.form.country" /></div>
              <div class="crm-f"><label>Source</label>
                <select v-model="drawer.form.sourceId"><option :value="null">—</option>
                  <option v-for="s in options.sources" :key="s.crmLeadSourceId" :value="s.crmLeadSourceId">{{ s.name }}</option>
                </select>
              </div>
              <div class="crm-f"><label>Partner</label>
                <select v-model="drawer.form.partnerId"><option :value="null">—</option>
                  <option v-for="p in options.partners" :key="p.partnerId" :value="p.partnerId">{{ p.name }}</option>
                </select>
              </div>
              <div class="crm-f"><label>Interested programme</label>
                <select v-model="drawer.form.programmeId"><option :value="null">—</option>
                  <option v-for="p in options.programmes" :key="p.programmeId" :value="p.programmeId">{{ p.name }} ({{ p.code }})</option>
                </select>
              </div>
              <div class="crm-f"><label>Expected value</label>
                <select v-model="drawer.form.valueBand"><option :value="null">—</option>
                  <option :value="0">&lt; $1k</option><option :value="1">$1–5k</option><option :value="2">$5k+</option>
                </select>
              </div>
              <div class="crm-f"><label>Assigned to</label>
                <select v-model="drawer.form.assignedToUserId"><option :value="null">— unassigned —</option>
                  <option v-for="u in salesStaff" :key="u.userId" :value="u.userId">{{ u.name }}</option>
                </select>
              </div>
              <div class="crm-f crm-f-wide"><label>Notes</label><textarea v-model="drawer.form.note" rows="3"></textarea></div>
              <div class="crm-drawer-actions">
                <button class="crm-btn crm-btn-primary" :disabled="drawer.busy || !drawer.form.name?.trim()" @click="saveLead">
                  {{ drawer.busy ? 'Saving…' : 'Save' }}
                </button>
                <button v-if="drawer.lead && drawer.lead.status !== 1" class="crm-btn crm-btn-convert"
                        :disabled="!drawer.form.partnerId" :title="drawer.form.partnerId ? '' : 'Pick a partner first'"
                        @click="convertLead">🎓 Convert to student</button>
                <button v-if="drawer.lead" class="crm-btn crm-btn-danger" @click="deleteLead">Delete</button>
              </div>
            </div>

            <div v-if="drawer.lead" class="crm-timeline">
              <div v-if="drawer.emails?.length" class="crm-emails">
                <h4>Emails ({{ drawer.emails.length }})</h4>
                <div v-for="e in drawer.emails" :key="e.mailMessageId" class="crm-email-row">
                  <span class="mail-acct-chip" :style="{ background: e.accountColor || '#eef' }">{{ e.accountName }}</span>
                  <span class="crm-email-dir">{{ e.isOutbound ? '📤' : '📥' }}</span>
                  <span class="crm-email-subject" :title="e.snippet || ''">{{ e.subject || '(no subject)' }}</span>
                  <span class="crm-email-date">{{ (e.sentAt || '').slice(0, 10) }}</span>
                </div>
              </div>
              <h4>Activity</h4>
              <div class="crm-compose">
                <select v-model.number="compose.kind">
                  <option :value="0">📝 Note</option><option :value="1">📞 Call</option><option :value="2">✉ Email</option>
                  <option :value="3">💬 WhatsApp</option><option :value="4">🤝 Meeting</option><option :value="5">📋 Task</option>
                </select>
                <input v-model="compose.body" placeholder="What happened / what to do…" @keyup.enter="addActivity" />
                <label class="crm-follow">Follow-up <input type="datetime-local" v-model="compose.dueAt" /></label>
                <button class="crm-btn" :disabled="compose.busy" @click="addActivity">Log</button>
              </div>
              <div v-for="a in drawer.activities" :key="a.crmActivityId" class="crm-act" :class="{ sys: a.kind === 6 }">
                <span class="crm-act-icon">{{ KIND_ICONS[a.kind] }}</span>
                <div class="crm-act-main">
                  <span>{{ a.body }}</span>
                  <span class="crm-act-meta">{{ (a.occurredAt || '').slice(0, 16).replace('T', ' ') }} · {{ a.actorName || 'system' }}</span>
                </div>
                <template v-if="a.dueAt">
                  <span v-if="!a.completedAt" class="crm-act-due" :class="{ over: new Date(a.dueAt) < new Date() }">
                    due {{ (a.dueAt || '').slice(0, 16).replace('T', ' ') }}
                    <button class="crm-mini" @click="setActStatus(a, true)">✓</button>
                  </span>
                  <span v-else class="crm-act-done">✓ done</span>
                </template>
              </div>
            </div>
          </div>
        </div>
      </div>
      <div v-if="convertUrl" class="crm-overlay" @click.self="closeConvert">
        <div class="crm-wizard-modal">
          <div class="crm-drawer-head">
            <h3>Convert to student</h3>
            <button class="crm-x" @click="closeConvert">✕</button>
          </div>
          <iframe :src="convertUrl" class="crm-wizard-frame"></iframe>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, watch } from 'vue'
import api from '../../api/client.js'
import CrmSettings from './CrmSettings.vue'
import { auth } from '../../store/auth.js'

const BANDS = ['<$1k', '$1–5k', '$5k+']
const KIND_ICONS = ['📝', '📞', '✉', '💬', '🤝', '📋', '⚙']
const KIND_LABELS = ['Note', 'Call', 'Email', 'WhatsApp', 'Meeting', 'Task', 'System']
const KPIS = [
  { id: 'overdue', label: 'Overdue' }, { id: 'noNextAction', label: 'No next action' },
  { id: 'rotting', label: 'Rotting' }, { id: 'stale', label: 'Stale 14d+' },
  { id: 'hot', label: 'Hot' }, { id: 'unassigned', label: 'Unassigned' },
]
const isAdministrator = ['SuperAdministrator', 'Administrator'].includes(auth.adminLevel)
const subTabs = [
  { id: 'board', label: 'Board' },
  { id: 'myday', label: 'My Day' },
  { id: 'next', label: 'Next Actions' },
  { id: 'report', label: 'Sources' },
  { id: 'import', label: 'Import' },
  ...(isAdministrator ? [{ id: 'settings', label: 'Settings' }] : []),
]

const props = defineProps({ openLeadId: { type: String, default: '' } })
watch(() => props.openLeadId, id => { if (id) openLead(id) }, { immediate: true })
const sub = ref('board')
const err = ref('')
const pipelineId = ref('')
const board = reactive({ pipelines: [], stages: [], leads: [] })
const options = reactive({ partners: [], programmes: [], sources: [] })
const salesStaff = ref([])
const myday = reactive({ counts: null })
const mydayBucket = ref('overdue')
const nextActions = ref([])
const report = ref([])
const dragLeadId = ref('')
const drawer = ref(null)
const compose = reactive({ kind: 0, body: '', dueAt: '', busy: false })
const convertUrl = ref('')
const importText = ref('')
const importSourceId = ref('')
const importPlan = ref(null)
const importBusy = ref(false)

function leadsFor(stageId) { return board.leads.filter(l => l.stageId === stageId) }

function switchSub(id) {
  sub.value = id
  if (id === 'myday') loadMyDay()
  if (id === 'next') loadNext()
  if (id === 'report') loadReport()
}

async function loadBoard() {
  try {
    const res = await api.get('/v1/admin/crm/board', { params: pipelineId.value ? { pipelineId: pipelineId.value } : {} })
    board.pipelines = res.data.pipelines ?? []
    board.stages = res.data.stages ?? []
    board.leads = res.data.leads ?? []
    pipelineId.value = res.data.activePipelineId ?? ''
  } catch (e) { err.value = e.response?.data?.error ?? e.message }
}
async function loadOptions() {
  try {
    const [o, s] = await Promise.all([
      api.get('/v1/admin/crm/options'),
      api.get('/v1/admin/sales-staff').catch(() => ({ data: { items: [] } })),
    ])
    Object.assign(options, o.data)
    salesStaff.value = (s.data.items ?? []).map(u => ({ userId: u.userId, name: u.name ?? u.userName }))
  } catch { /* board still works */ }
}
async function loadMyDay() {
  try { Object.assign(myday, (await api.get('/v1/admin/crm/my-day')).data) }
  catch (e) { err.value = e.response?.data?.error ?? e.message }
}
async function loadNext() {
  try { nextActions.value = (await api.get('/v1/admin/crm/next-actions')).data.items ?? [] }
  catch (e) { err.value = e.response?.data?.error ?? e.message }
}
async function loadReport() {
  try { report.value = (await api.get('/v1/admin/crm/source-report')).data.items ?? [] }
  catch (e) { err.value = e.response?.data?.error ?? e.message }
}

async function onDrop(stage) {
  const id = dragLeadId.value
  dragLeadId.value = ''
  if (!id) return
  const lead = board.leads.find(l => l.crmLeadId === id)
  if (!lead || lead.stageId === stage.crmStageId) return
  let lostReason = null
  if (stage.stageType === 2) lostReason = prompt('Lost reason (optional):') ?? null
  try {
    await api.patch(`/v1/admin/crm/leads/${id}/stage`, { stageId: stage.crmStageId, lostReason })
    await loadBoard()
  } catch (e) { err.value = e.response?.data?.error ?? e.message }
}

function blankForm() {
  return { name: '', email: '', phone: '', country: '', note: '', sourceId: null,
    partnerId: null, programmeId: null, valueBand: null, assignedToUserId: null }
}
function openNewLead() {
  drawer.value = { lead: null, activities: [], form: blankForm(), busy: false, error: '', dupWarn: '' }
}
async function openLead(id) {
  drawer.value = { lead: null, activities: [], form: blankForm(), busy: false, error: '', dupWarn: '' }
  try {
    const res = await api.get(`/v1/admin/crm/leads/${id}`)
    drawer.value.lead = res.data.lead
    drawer.value.convertedStudentNumber = res.data.convertedStudentNumber
    drawer.value.activities = res.data.activities ?? []
    const l = res.data.lead
    drawer.value.form = { name: l.name, email: l.email, phone: l.phone, country: l.country, note: l.note,
      sourceId: l.sourceId, partnerId: l.partnerId, programmeId: l.programmeId,
      valueBand: l.valueBand, assignedToUserId: l.assignedToUserId }
    api.get(`/v1/admin/crm/leads/${id}/emails`)
      .then(r => { if (drawer.value?.lead?.crmLeadId === id) drawer.value.emails = r.data.items ?? [] })
      .catch(() => {})
  } catch (e) { drawer.value.error = e.response?.data?.error ?? e.message }
}
async function checkDuplicate() {
  drawer.value.dupWarn = ''
  const email = drawer.value.form.email?.trim()
  if (!email) return
  try {
    const res = await api.get('/v1/admin/crm/leads/check-email', { params: { email } })
    const hits = []
    for (const l of res.data.leads ?? []) if (l.crmLeadId !== drawer.value.lead?.crmLeadId) hits.push(`lead "${l.name}"`)
    for (const s of res.data.students ?? []) hits.push(`student ${s.studentNumber}`)
    if (hits.length) drawer.value.dupWarn = `This email already exists on ${hits.join(', ')}. You can still save.`
  } catch { /* non-blocking */ }
}
async function saveLead() {
  const d = drawer.value
  if (!d || d.busy) return
  d.busy = true; d.error = ''
  try {
    const body = { pipelineId: pipelineId.value || null, ...d.form }
    if (d.lead) await api.patch(`/v1/admin/crm/leads/${d.lead.crmLeadId}`, body)
    else await api.post('/v1/admin/crm/leads', body)
    drawer.value = null
    await loadBoard()
  } catch (e) { d.error = e.response?.data?.error ?? e.message } finally { d.busy = false }
}
async function deleteLead() {
  const d = drawer.value
  if (!d?.lead || !confirm(`Delete lead "${d.lead.name}"?`)) return
  try {
    await api.delete(`/v1/admin/crm/leads/${d.lead.crmLeadId}`)
    drawer.value = null
    await loadBoard()
  } catch (e) { d.error = e.response?.data?.error ?? e.message }
}
async function addActivity() {
  const d = drawer.value
  if (!d?.lead || compose.busy || (!compose.body.trim() && !compose.dueAt)) return
  compose.busy = true
  try {
    await api.post(`/v1/admin/crm/leads/${d.lead.crmLeadId}/activities`, {
      kind: compose.kind, body: compose.body.trim() || null,
      dueAt: compose.dueAt ? new Date(compose.dueAt).toISOString() : null,
    })
    compose.body = ''; compose.dueAt = ''
    await openLead(d.lead.crmLeadId)
  } catch (e) { d.error = e.response?.data?.error ?? e.message } finally { compose.busy = false }
}
async function setActStatus(a, completed) {
  try {
    await api.patch(`/v1/admin/crm/activities/${a.crmActivityId}/status`, { completed })
    if (drawer.value?.lead) await openLead(drawer.value.lead.crmLeadId)
  } catch (e) { err.value = e.response?.data?.error ?? e.message }
}
async function completeAction(a) {
  try {
    await api.patch(`/v1/admin/crm/activities/${a.crmActivityId}/status`, { completed: true })
    await loadNext()
  } catch (e) { err.value = e.response?.data?.error ?? e.message }
}

// ── Convert to student: prefilled wizard iframe attributed to this user ──────
async function convertLead() {
  const d = drawer.value
  if (!d?.lead || !d.form.partnerId) return
  try {
    await saveLeadSilently()
    const partner = options.partners.find(p => p.partnerId === d.form.partnerId)
    if (!partner) { d.error = 'Pick a partner first.'; return }
    const ticket = (await api.post('/v1/admin/students/actor-ticket')).data.ticket
    const [first, ...rest] = (d.form.name ?? '').trim().split(/\s+/)
    const q = new URLSearchParams({ partner: partner.slug, actor: ticket, crmLead: d.lead.crmLeadId })
    if (first) q.set('pf_first', first)
    if (rest.length) q.set('pf_last', rest.join(' '))
    if (d.form.email) q.set('pf_email', d.form.email)
    convertUrl.value = `/#/apply?${q.toString()}`
  } catch (e) { d.error = e.response?.data?.error ?? e.message }
}
async function saveLeadSilently() {
  const d = drawer.value
  if (!d?.lead) return
  await api.patch(`/v1/admin/crm/leads/${d.lead.crmLeadId}`, { ...d.form })
}
async function closeConvert() {
  convertUrl.value = ''
  if (drawer.value?.lead) await openLead(drawer.value.lead.crmLeadId)
  await loadBoard()
}

// ── CSV import ───────────────────────────────────────────────────────────────
function parseCsv() {
  const lines = importText.value.split('\n').map(l => l.trim()).filter(Boolean)
  const rows = []
  for (const line of lines) {
    const parts = line.split(',').map(p => p.trim())
    if (/^name\b/i.test(parts[0] ?? '')) continue // header row
    rows.push({ name: parts[0] || '', email: parts[1] || '', phone: parts[2] || '', country: parts[3] || '' })
  }
  return rows
}
async function runImport(commit) {
  if (importBusy.value) return
  importBusy.value = true
  try {
    const res = await api.post('/v1/admin/crm/import', {
      pipelineId: pipelineId.value, sourceId: importSourceId.value || null,
      commit, rows: parseCsv(),
    })
    importPlan.value = res.data
    if (commit) await loadBoard()
  } catch (e) { err.value = e.response?.data?.error ?? e.message } finally { importBusy.value = false }
}

onMounted(async () => { await Promise.all([loadBoard(), loadOptions()]) })
</script>

<style scoped>
.crm { padding: .25rem 0; }
.err-banner { background: #fde7e7; color: #8a1515; padding: .5rem .8rem; border-radius: 6px; font-size: .84rem; }
.muted { color: #5f6e85; font-size: .8rem; }
.crm-subnav { display: flex; align-items: center; gap: .4rem; margin-bottom: .7rem; flex-wrap: wrap; }
.crm-sub-btn { padding: .4rem .85rem; border: 1px solid #cfd7e3; background: #fff; border-radius: 6px; font-size: .84rem; cursor: pointer; }
.crm-sub-btn.active { background: #0b2e59; color: #fff; border-color: #0b2e59; }
.crm-pipe-sel { padding: .35rem .55rem; border: 1px solid #cfd7e3; border-radius: 6px; font-size: .82rem; margin-left: auto; }
.crm-btn { padding: .35rem .7rem; border: 1px solid #cfd7e3; background: #fff; border-radius: 6px; font-size: .8rem; cursor: pointer; }
.crm-btn-primary { background: #0b2e59; color: #fff; border-color: #0b2e59; }
.crm-btn-convert { border-color: #1c7a4a; color: #1c7a4a; font-weight: 700; }
.crm-btn-danger { border-color: #b63329; color: #b63329; }

.crm-board { display: flex; gap: .6rem; overflow-x: auto; align-items: flex-start; padding-bottom: .8rem; }
.crm-col { min-width: 230px; width: 230px; background: #f2f5f9; border-radius: 8px; padding: .4rem; flex-shrink: 0; }
.crm-col-head { display: flex; align-items: center; gap: .4rem; padding: .35rem .45rem; border-top: 3px solid; border-radius: 4px 4px 0 0; background: #fff; margin-bottom: .4rem; font-size: .82rem; }
.crm-col-count { margin-left: auto; background: #ecf0f6; border-radius: 9px; padding: 0 7px; font-size: .72rem; color: #5f6e85; }
.crm-col-tag { font-size: .6rem; font-weight: 800; padding: 1px 5px; border-radius: 6px; }
.crm-col-tag.won { background: #d7f0df; color: #1c7a4a; }
.crm-col-tag.lost { background: #fde7e5; color: #a8241e; }
.crm-card { background: #fff; border: 1px solid #e0e6ee; border-left: 3px solid #e0e6ee; border-radius: 6px; padding: .45rem .55rem; margin-bottom: .4rem; cursor: pointer; }
.crm-card:hover { border-color: #a0b8d0; }
.crm-card.rotting { border-left-color: #d92d20; background: #fff8f7; }
.crm-card.converted { opacity: .75; }
.crm-card-top { display: flex; align-items: center; gap: .3rem; }
.crm-card-name { font-size: .82rem; color: #0b2e59; }
.crm-hot { margin-left: auto; }
.crm-card-meta { display: flex; align-items: center; gap: .35rem; margin-top: .2rem; }
.crm-band { font-size: .68rem; background: #eef3fb; color: #1a4d8c; border-radius: 8px; padding: 0 6px; }
.crm-score { font-size: .68rem; background: #ecf0f6; color: #5f6e85; border-radius: 8px; padding: 0 6px; font-weight: 700; }
.crm-rot { font-size: .72rem; }
.crm-card-sub { display: flex; justify-content: space-between; font-size: .7rem; color: #8a93a4; margin-top: .25rem; }
.crm-due-over { color: #d92d20; font-weight: 700; }

.crm-kpis { display: flex; gap: .5rem; flex-wrap: wrap; margin-bottom: .7rem; }
.crm-kpi { display: flex; flex-direction: column; align-items: center; min-width: 108px; padding: .55rem .7rem; background: #fff; border: 1.5px solid #e0e6ee; border-radius: 8px; cursor: pointer; }
.crm-kpi.active { border-color: #0b2e59; background: #eef3fb; }
.crm-kpi-n { font-size: 1.3rem; font-weight: 800; color: #0b2e59; }
.crm-kpi-l { font-size: .72rem; color: #5f6e85; }
.crm-bucket-row { display: flex; gap: .7rem; align-items: center; background: #fff; border: 1px solid #e8edf3; border-radius: 6px; padding: .45rem .65rem; margin-bottom: .3rem; cursor: pointer; font-size: .84rem; }
.crm-bucket-row .crm-score { margin-left: auto; }

.crm-next-row { display: flex; align-items: center; gap: .6rem; background: #fff; border: 1px solid #e8edf3; border-radius: 6px; padding: .45rem .65rem; margin-bottom: .3rem; font-size: .84rem; }
.crm-next-row.over { border-left: 3px solid #d92d20; }
.crm-next-lead { font-weight: 700; color: #0b2e59; cursor: pointer; }
.crm-next-body { flex: 1; color: #445; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.crm-next-due { font-size: .76rem; color: #8a6b16; }

.crm-tbl { width: 100%; border-collapse: collapse; font-size: .84rem; background: #fff; }
.crm-tbl th { text-align: left; font-size: .68rem; text-transform: uppercase; color: #6b7888; padding: .4rem .6rem; border-bottom: 1px solid #e8edf3; }
.crm-tbl td { padding: .45rem .6rem; border-bottom: 1px solid #f0f3f7; }

.crm-import-text { width: 100%; border: 1px solid #cfd7e3; border-radius: 6px; padding: .5rem; font-family: monospace; font-size: .8rem; }
.crm-import-row { display: flex; align-items: center; gap: .5rem; margin-top: .5rem; flex-wrap: wrap; }
</style>

<style>
/* Teleported drawer — global styles (scoped can't reach the teleport target) */
.crm-overlay { position: fixed; inset: 0; background: rgba(10, 25, 47, .45); z-index: 1000; display: flex; justify-content: flex-end; }
.crm-drawer { width: 860px; max-width: 96vw; height: 100vh; background: #fff; box-shadow: -8px 0 30px rgba(0,0,0,.25); display: flex; flex-direction: column; }
.crm-drawer-head { display: flex; justify-content: space-between; align-items: center; padding: .9rem 1.1rem; border-bottom: 1px solid #e8edf3; }
.crm-drawer-head h3 { margin: 0; font-size: 1.05rem; color: #0b2e59; }
.crm-x { background: none; border: none; font-size: 1rem; cursor: pointer; color: #5f6e85; }
.crm-drawer-body { flex: 1; overflow-y: auto; padding: .9rem 1.1rem; display: flex; gap: 1.1rem; }
.crm-form { width: 320px; flex-shrink: 0; }
.crm-f { margin-bottom: .5rem; display: flex; flex-direction: column; gap: .15rem; }
.crm-f label { font-size: .72rem; font-weight: 700; color: #5f6e85; text-transform: uppercase; letter-spacing: .03em; }
.crm-f input, .crm-f select, .crm-f textarea { padding: .38rem .5rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .84rem; }
.crm-dup { color: #8a6b16; background: #fff1cc; border-radius: 5px; padding: .25rem .45rem; font-size: .74rem; margin: .2rem 0 0; }
.crm-drawer-actions { display: flex; gap: .45rem; margin-top: .6rem; flex-wrap: wrap; }
.crm-timeline { flex: 1; min-width: 0; }
.crm-timeline h4 { margin: 0 0 .4rem; font-size: .9rem; color: #0b2e59; }
.crm-compose { display: flex; gap: .35rem; align-items: center; flex-wrap: wrap; margin-bottom: .6rem; }
.crm-compose select, .crm-compose input { padding: .35rem .5rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .8rem; }
.crm-compose input[type="text"], .crm-compose input:not([type]) { flex: 1; min-width: 160px; }
.crm-follow { font-size: .72rem; color: #5f6e85; display: flex; align-items: center; gap: .25rem; }
.crm-act { display: flex; gap: .5rem; align-items: flex-start; padding: .4rem 0; border-bottom: 1px dashed #edf1f6; font-size: .82rem; }
.crm-act.sys { opacity: .65; }
.crm-act-main { flex: 1; display: flex; flex-direction: column; gap: .1rem; }
.crm-act-meta { font-size: .7rem; color: #8a93a4; }
.crm-act-due { font-size: .74rem; color: #8a6b16; white-space: nowrap; }
.crm-act-due.over { color: #d92d20; font-weight: 700; }
.crm-act-done { font-size: .74rem; color: #1c7a4a; }
.crm-mini { border: 1px solid #1c7a4a; color: #1c7a4a; background: #fff; border-radius: 4px; cursor: pointer; font-size: .7rem; padding: 0 5px; }
.crm-wizard-modal { margin: auto; width: 1100px; max-width: 96vw; height: 90vh; background: #fff; border-radius: 10px; display: flex; flex-direction: column; overflow: hidden; }
.crm-wizard-frame { flex: 1; border: 0; }
.crm-emails { margin-bottom: .6rem; }
.crm-emails h4 { margin: 0 0 .3rem; font-size: .84rem; color: #0b2e59; }
.crm-email-row { display: flex; gap: .4rem; align-items: center; font-size: .78rem; padding: .2rem 0; border-bottom: 1px dashed #edf1f6; }
.crm-email-subject { flex: 1; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.crm-email-date { color: #8a93a4; font-size: .7rem; }
.mail-acct-chip { font-size: .64rem; border-radius: 8px; padding: 0 7px; color: #0b2e59; font-weight: 700; }
</style>
