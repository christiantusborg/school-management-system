<template>
  <div class="page-wrapper">
    <nav class="navbar">
      <span class="brand-text">IBSS Admin Portal</span>
      <div class="nav-links">
        <RouterLink to="/admin"          class="nav-link">Dashboard</RouterLink>
        <RouterLink to="/admin/academic" class="nav-link">Academic</RouterLink>
        <RouterLink to="/admin/config"   class="nav-link">System Config</RouterLink>
      </div>
      <div class="nav-right">
        <span class="nav-user">{{ auth.user?.displayName }}</span>
        <button class="btn-logout" @click="logout">Log out</button>
      </div>
    </nav>

    <div class="container">

      <!-- Header -->
      <div class="page-header">
        <div>
          <h1 class="page-title">IBSS Core Programmes</h1>
          <p class="page-sub" v-if="!loading">
            {{ programmes.length }} programme{{ programmes.length !== 1 ? 's' : '' }}
          </p>
        </div>
        <div class="header-actions">
          <button
            :class="['btn-toggle-deleted', { active: showDeleted }]"
            @click="toggleShowDeleted"
            :disabled="loadingDeleted"
          >
            {{ showDeleted ? '✕ Hide Deleted' : '🗑 Show Deleted' }}
          </button>
          <button class="btn-primary" @click="showAddProg = !showAddProg">+ Add Programme</button>
        </div>
      </div>

      <div v-if="loadError" class="err-banner">{{ loadError }}</div>

      <!-- Add Programme form -->
      <div v-if="showAddProg" class="add-prog-card">
        <div class="add-prog-fields">
          <div class="field">
            <label>Programme Name <span class="req">*</span></label>
            <input v-model="newProg.name" placeholder="e.g. Master of Marketing" @keyup.enter="addProgramme" />
          </div>
          <div class="field field-code">
            <label>Code <span class="req">*</span></label>
            <input v-model="newProg.code" placeholder="MM" @keyup.enter="addProgramme" />
          </div>
        </div>
        <div class="field" style="margin-top:0.85rem">
          <label>Pathways</label>
          <div class="pathway-grid">
            <label v-for="p in pathways" :key="p.pathwayId" class="pathway-row">
              <input type="checkbox"
                     :checked="newProg.pathwayIds.includes(p.pathwayId)"
                     @change="toggleNewPathway(p.pathwayId)" />
              {{ p.name }}
            </label>
            <p v-if="!pathways.length" class="empty-note">No pathways defined.</p>
          </div>
        </div>
        <p v-if="newProg.error" class="form-error">{{ newProg.error }}</p>
        <div class="add-prog-actions">
          <button class="btn-ghost" @click="cancelAddProg">Cancel</button>
          <button class="btn-primary" :disabled="newProg.saving" @click="addProgramme">
            {{ newProg.saving ? 'Saving…' : 'Add Programme' }}
          </button>
        </div>
      </div>

      <div v-if="loading" class="loading-state">Loading…</div>

      <!-- ── ACTIVE PROGRAMMES ── -->
      <div v-for="prog in programmes" :key="prog.programmeId" class="prog-card">
        <div class="prog-header">
          <div class="prog-header-left">
            <strong class="prog-name">{{ prog.name }}</strong>
            <span class="badge-code">{{ prog.code }}</span>
            <span class="badge-count">{{ majorsFor(prog.programmeId).length }} major{{ majorsFor(prog.programmeId).length !== 1 ? 's' : '' }}</span>
          </div>
          <div class="prog-header-right">
            <button class="btn-del" @click="softDeleteProgramme(prog)">Delete</button>
            <button class="btn-collapse" @click="toggleProg(prog.programmeId)">
              {{ xProg === prog.programmeId ? '▲ Collapse' : '▼ Expand' }}
            </button>
          </div>
        </div>

        <div v-if="xProg === prog.programmeId" class="prog-body">
          <div class="section-label section-label-toggle" @click="togglePathwaySection(prog.programmeId)">
            <span class="section-arrow">{{ xPathway === prog.programmeId ? '▾' : '▸' }}</span>
            PATHWAYS
            <span class="badge-count">{{ (progPathways[prog.programmeId] ?? []).length }} selected</span>
          </div>
          <div v-if="xPathway === prog.programmeId" class="pathway-edit-block">
            <div class="pathway-grid">
              <label v-for="p in pathways" :key="p.pathwayId" class="pathway-row">
                <input type="checkbox"
                       :checked="(progPathways[prog.programmeId] ?? []).includes(p.pathwayId)"
                       @change="togglePathwayForProg(prog, p.pathwayId)" />
                {{ p.name }}
              </label>
              <p v-if="!pathways.length" class="empty-note">No pathways defined.</p>
            </div>
            <div v-if="pathwayBusy[prog.programmeId]" class="form-error-inline">Saving…</div>
            <div v-if="pathwayErr[prog.programmeId]" class="form-error">{{ pathwayErr[prog.programmeId] }}</div>
          </div>

          <div class="section-label">MAJORS &amp; SUBJECTS</div>

          <div v-for="maj in majorsFor(prog.programmeId)" :key="maj.majorId" class="maj-card">
            <div class="maj-row" @click="toggleMaj(maj.majorId)">
              <span class="maj-arrow">{{ xMaj === maj.majorId ? '▼' : '▶' }}</span>
              <strong class="maj-name">{{ maj.name }}</strong>
              <span class="maj-count">{{ subjectsFor(maj.majorId).length }} subject{{ subjectsFor(maj.majorId).length !== 1 ? 's' : '' }}</span>
              <button class="btn-del-sm" @click.stop="softDeleteMajor(maj)">Remove</button>
            </div>

            <div v-if="xMaj === maj.majorId" class="subj-table">
              <div class="subj-col-header">
                <span class="col-code">Module Code</span>
                <span class="col-name">Module Name</span>
                <span class="col-ects">ECTS</span>
                <span class="col-act"></span>
              </div>
              <div v-if="subjectsFor(maj.majorId).length === 0" class="subj-empty">No modules yet.</div>
              <div v-for="s in subjectsFor(maj.majorId)" :key="s.subjectId" class="subj-row">
                <span class="col-code scode">{{ s.code || '—' }}</span>
                <span class="col-name">{{ s.name }}</span>
                <span class="col-ects ects-val">{{ s.ects }}</span>
                <span class="col-act"><button class="btn-x" @click="softDeleteSubject(s)">✕</button></span>
              </div>
              <div class="add-subj-row">
                <input v-model="sf[maj.majorId + '_code']" class="inp-code col-code" placeholder="Code" />
                <input v-model="sf[maj.majorId + '_n']" class="inp-name col-name" placeholder="Module name" @keyup.enter="addSubject(maj.majorId)" />
                <input v-model.number="sf[maj.majorId + '_e']" class="inp-ects col-ects" type="number" min="1" placeholder="15" />
                <span class="col-act"><button class="btn-add" @click="addSubject(maj.majorId)">+ Add</button></span>
              </div>
              <p v-if="sf[maj.majorId + '_err']" class="form-error">{{ sf[maj.majorId + '_err'] }}</p>
            </div>
          </div>

          <div class="add-maj-row">
            <input v-model="mf[prog.programmeId]" class="inp-maj" placeholder="New major name…" @keyup.enter="addMajor(prog.programmeId)" />
            <button class="btn-primary-sm" @click="addMajor(prog.programmeId)">+ Add Major</button>
            <span v-if="mfErr[prog.programmeId]" class="form-error-inline">{{ mfErr[prog.programmeId] }}</span>
          </div>
        </div>
      </div>

      <div v-if="!loading && programmes.length === 0" class="empty-state">
        No programmes yet. Click <strong>+ Add Programme</strong> above.
      </div>

      <!-- ── DELETED SECTION ── -->
      <template v-if="showDeleted">
        <div class="deleted-section-header">
          <span>🗑 Soft-Deleted Items</span>
          <span v-if="loadingDeleted" class="deleting-hint">Loading…</span>
          <span v-else class="deleting-hint">Permanent delete cascades — deleting a programme removes all its majors and subjects.</span>
        </div>

        <!-- ── Group 1: Deleted Programmes + their deleted children ── -->
        <template v-for="prog in deletedProgrammes" :key="prog.programmeId">
          <!-- Deleted programme -->
          <div class="del-card">
            <div class="del-card-header">
              <div class="del-card-title">
                <span class="del-icon">📁</span>
                <strong>{{ prog.name }}</strong>
                <span class="badge-code">{{ prog.code }}</span>
                <span class="del-date">Deleted {{ fmtDate(prog.deletedAt) }}</span>
              </div>
              <div class="del-card-actions">
                <button class="btn-restore" :disabled="!!deleting[prog.programmeId]" @click="restoreProgramme(prog)">
                  {{ deleting[prog.programmeId] === 'restore' ? 'Restoring…' : 'Restore' }}
                </button>
                <button class="btn-perm-del" :disabled="!!deleting[prog.programmeId]" @click="permanentDeleteProgramme(prog)">
                  {{ deleting[prog.programmeId] === 'delete' ? 'Deleting…' : 'Permanent Delete' }}
                </button>
              </div>
            </div>
          </div>

          <!-- Deleted majors under this deleted programme -->
          <template v-for="maj in deletedMajors.filter(m => m.programmeId === prog.programmeId)" :key="maj.majorId">
            <div class="del-card del-card-child">
              <div class="del-card-header">
                <div class="del-card-title">
                  <span class="del-icon">📂</span>
                  <strong>{{ maj.name }}</strong>
                  <span class="del-date">Deleted {{ fmtDate(maj.deletedAt) }}</span>
                </div>
                <div class="del-card-actions">
                  <button class="btn-restore" disabled title="Restore the parent programme first">Restore</button>
                  <button class="btn-perm-del" :disabled="!!deleting[maj.majorId]" @click="permanentDeleteMajor(maj)">
                    {{ deleting[maj.majorId] === 'delete' ? 'Deleting…' : 'Permanent Delete' }}
                  </button>
                </div>
              </div>
            </div>

            <!-- Deleted subjects under this deleted major -->
            <div v-for="subj in deletedSubjects.filter(s => s.majorId === maj.majorId)" :key="subj.subjectId" class="del-card del-card-grandchild">
              <div class="del-card-header">
                <div class="del-card-title">
                  <span class="del-icon">📄</span>
                  <span class="scode">{{ subj.code || '—' }}</span>
                  <strong>{{ subj.name }}</strong>
                  <span class="del-prog-hint">{{ subj.ects }} ECTS</span>
                  <span class="del-date">Deleted {{ fmtDate(subj.deletedAt) }}</span>
                </div>
                <div class="del-card-actions">
                  <button class="btn-restore" disabled title="Restore the parent major first">Restore</button>
                  <button class="btn-perm-del" :disabled="!!deleting[subj.subjectId]" @click="permanentDeleteSubject(subj)">
                    {{ deleting[subj.subjectId] === 'delete' ? 'Deleting…' : 'Permanent Delete' }}
                  </button>
                </div>
              </div>
            </div>
          </template>

          <!-- Deleted subjects whose major is active but programme is deleted -->
          <template v-for="subj in deletedSubjects.filter(s => !deletedMajors.find(m => m.majorId === s.majorId) && majorProgrammeId(s.majorId) === prog.programmeId)" :key="subj.subjectId">
            <div class="del-card del-card-child">
              <div class="del-breadcrumb">
                <span class="bc-item bc-active">📂 {{ majorNameFor(subj.majorId) }}</span>
              </div>
              <div class="del-card-header">
                <div class="del-card-title">
                  <span class="del-icon">📄</span>
                  <span class="scode">{{ subj.code || '—' }}</span>
                  <strong>{{ subj.name }}</strong>
                  <span class="del-prog-hint">{{ subj.ects }} ECTS</span>
                  <span class="del-date">Deleted {{ fmtDate(subj.deletedAt) }}</span>
                </div>
                <div class="del-card-actions">
                  <button class="btn-restore" disabled title="Restore the parent programme first">Restore</button>
                  <button class="btn-perm-del" :disabled="!!deleting[subj.subjectId]" @click="permanentDeleteSubject(subj)">
                    {{ deleting[subj.subjectId] === 'delete' ? 'Deleting…' : 'Permanent Delete' }}
                  </button>
                </div>
              </div>
            </div>
          </template>
        </template>

        <!-- ── Group 2: Deleted Majors whose Programme is ACTIVE ── -->
        <template v-for="maj in deletedMajors.filter(m => !deletedProgrammes.find(p => p.programmeId === m.programmeId))" :key="maj.majorId">
          <!-- Grayed parent context -->
          <div class="del-breadcrumb-row">
            <span class="bc-item bc-active">📁 {{ progNameFor(maj.programmeId) }}</span>
          </div>
          <!-- Deleted major -->
          <div class="del-card del-card-child">
            <div class="del-card-header">
              <div class="del-card-title">
                <span class="del-icon">📂</span>
                <strong>{{ maj.name }}</strong>
                <span class="del-date">Deleted {{ fmtDate(maj.deletedAt) }}</span>
              </div>
              <div class="del-card-actions">
                <button class="btn-restore" :disabled="!!deleting[maj.majorId]" @click="restoreMajor(maj)">
                  {{ deleting[maj.majorId] === 'restore' ? 'Restoring…' : 'Restore' }}
                </button>
                <button class="btn-perm-del" :disabled="!!deleting[maj.majorId]" @click="permanentDeleteMajor(maj)">
                  {{ deleting[maj.majorId] === 'delete' ? 'Deleting…' : 'Permanent Delete' }}
                </button>
              </div>
            </div>
          </div>

          <!-- Deleted subjects under this deleted major -->
          <div v-for="subj in deletedSubjects.filter(s => s.majorId === maj.majorId)" :key="subj.subjectId" class="del-card del-card-grandchild">
            <div class="del-card-header">
              <div class="del-card-title">
                <span class="del-icon">📄</span>
                <span class="scode">{{ subj.code || '—' }}</span>
                <strong>{{ subj.name }}</strong>
                <span class="del-prog-hint">{{ subj.ects }} ECTS</span>
                <span class="del-date">Deleted {{ fmtDate(subj.deletedAt) }}</span>
              </div>
              <div class="del-card-actions">
                <button class="btn-restore" disabled title="Restore the parent major first">Restore</button>
                <button class="btn-perm-del" :disabled="!!deleting[subj.subjectId]" @click="permanentDeleteSubject(subj)">
                  {{ deleting[subj.subjectId] === 'delete' ? 'Deleting…' : 'Permanent Delete' }}
                </button>
              </div>
            </div>
          </div>
        </template>

        <!-- ── Group 3: Deleted Subjects whose Major AND Programme are both ACTIVE ── -->
        <template v-for="subj in deletedSubjects.filter(s => !deletedMajors.find(m => m.majorId === s.majorId) && !deletedProgrammes.find(p => p.programmeId === majorProgrammeId(s.majorId)))" :key="subj.subjectId">
          <div class="del-breadcrumb-row">
            <span class="bc-item bc-active">📁 {{ progNameFor(majorProgrammeId(subj.majorId)) }}</span>
            <span class="bc-sep">›</span>
            <span class="bc-item bc-active">📂 {{ majorNameFor(subj.majorId) }}</span>
          </div>
          <div class="del-card del-card-grandchild">
            <div class="del-card-header">
              <div class="del-card-title">
                <span class="del-icon">📄</span>
                <span class="scode">{{ subj.code || '—' }}</span>
                <strong>{{ subj.name }}</strong>
                <span class="del-prog-hint">{{ subj.ects }} ECTS</span>
                <span class="del-date">Deleted {{ fmtDate(subj.deletedAt) }}</span>
              </div>
              <div class="del-card-actions">
                <button class="btn-restore" :disabled="!!deleting[subj.subjectId]" @click="restoreSubject(subj)">
                  {{ deleting[subj.subjectId] === 'restore' ? 'Restoring…' : 'Restore' }}
                </button>
                <button class="btn-perm-del" :disabled="!!deleting[subj.subjectId]" @click="permanentDeleteSubject(subj)">
                  {{ deleting[subj.subjectId] === 'delete' ? 'Deleting…' : 'Permanent Delete' }}
                </button>
              </div>
            </div>
          </div>
        </template>

        <div v-if="!loadingDeleted && deletedProgrammes.length === 0 && deletedMajors.length === 0 && deletedSubjects.length === 0"
             class="del-empty">No soft-deleted items.</div>
      </template>

    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { auth } from '../store/auth.js'
import api from '../api/client.js'

const router = useRouter()
function logout() { auth.logout(); router.push('/login') }

// ── Active data ────────────────────────────────────────────────────────────────
const loading    = ref(true)
const loadError  = ref('')
const programmes = ref([])
const majors     = ref([])
const subjects   = ref([])

// ── Deleted data ───────────────────────────────────────────────────────────────
const showDeleted      = ref(false)
const loadingDeleted   = ref(false)
const deletedProgrammes = ref([])
const deletedMajors     = ref([])
const deletedSubjects   = ref([])
const deleting = reactive({}) // id → bool

// ── Accordion ──────────────────────────────────────────────────────────────────
const xProg = ref(null)
const xMaj  = ref(null)
const xPathway = ref(null)

function togglePathwaySection(id) { xPathway.value = xPathway.value === id ? null : id }

// ── Forms ──────────────────────────────────────────────────────────────────────
const showAddProg = ref(false)
const newProg = reactive({ name: '', code: '', saving: false, error: '', pathwayIds: [] })

// ── Pathways ───────────────────────────────────────────────────────────────────
const pathways = ref([])
const progPathways = reactive({})  // programmeId → number[]
const pathwayBusy = reactive({})   // programmeId → bool
const pathwayErr = reactive({})    // programmeId → string
const mf    = reactive({})
const mfErr = reactive({})
const sf    = reactive({})

// ── Load active ────────────────────────────────────────────────────────────────
async function loadAll() {
  loading.value = true
  loadError.value = ''
  try {
    const [pRes, mRes, sRes, pathRes] = await Promise.all([
      api.get('/v1/school/programmes?ownership=core'),
      api.get('/v1/school/majors'),
      api.get('/v1/school/subjects'),
      api.get('/v1/school/system-config/pathways'),
    ])
    programmes.value = pRes.data.items ?? []
    majors.value     = mRes.data.items ?? []
    subjects.value   = sRes.data.items ?? []
    pathways.value   = pathRes.data.items ?? []
    for (const p of programmes.value) {
      progPathways[p.programmeId] = p.pathwayIds ?? []
    }
  } catch (e) {
    loadError.value = e.response?.data?.message ?? e.message ?? 'Failed to load'
  } finally {
    loading.value = false
  }
}

// ── Load deleted ───────────────────────────────────────────────────────────────
async function loadDeleted() {
  loadingDeleted.value = true
  try {
    const [pRes, mRes, sRes] = await Promise.all([
      api.get('/v1/school/programmes?deleted=true'),
      api.get('/v1/school/majors?deleted=true'),
      api.get('/v1/school/subjects?deleted=true'),
    ])
    deletedProgrammes.value = pRes.data.items ?? []
    deletedMajors.value     = mRes.data.items ?? []
    deletedSubjects.value   = sRes.data.items ?? []
  } catch (e) {
    loadError.value = e.response?.data?.message ?? e.message ?? 'Failed to load deleted items'
  } finally {
    loadingDeleted.value = false
  }
}

function toggleShowDeleted() {
  showDeleted.value = !showDeleted.value
  if (showDeleted.value) loadDeleted()
}

onMounted(loadAll)

// ── Helpers ────────────────────────────────────────────────────────────────────
const majorsFor   = progId  => majors.value.filter(m => m.programmeId === progId)
const subjectsFor = majorId => subjects.value.filter(s => s.majorId === majorId)
const progNameFor = progId  => {
  return programmes.value.find(p => p.programmeId === progId)?.name
      ?? deletedProgrammes.value.find(p => p.programmeId === progId)?.name
      ?? '(unknown)'
}
const majorNameFor = majorId => {
  return majors.value.find(m => m.majorId === majorId)?.name
      ?? deletedMajors.value.find(m => m.majorId === majorId)?.name
      ?? '(unknown)'
}
const majorProgrammeId = majorId => {
  return majors.value.find(m => m.majorId === majorId)?.programmeId
      ?? deletedMajors.value.find(m => m.majorId === majorId)?.programmeId
      ?? null
}

function toggleProg(id) { xProg.value = xProg.value === id ? null : id; xMaj.value = null }
function toggleMaj(id)  { xMaj.value  = xMaj.value  === id ? null : id }
function fmtDate(d)     { return d ? new Date(d).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }) : '—' }

// ── Active CRUD ────────────────────────────────────────────────────────────────
function cancelAddProg() {
  showAddProg.value = false
  newProg.name = ''; newProg.code = ''; newProg.error = ''
  newProg.pathwayIds = []
}

function toggleNewPathway(id) {
  const idx = newProg.pathwayIds.indexOf(id)
  if (idx >= 0) newProg.pathwayIds.splice(idx, 1)
  else newProg.pathwayIds.push(id)
}

async function addProgramme() {
  if (!newProg.name.trim() || !newProg.code.trim()) { newProg.error = 'Name and Code are required.'; return }
  newProg.saving = true; newProg.error = ''
  try {
    const res = await api.post('/v1/school/programmes', {
      name: newProg.name.trim(),
      code: newProg.code.trim().toUpperCase(),
      pathwayIds: [...newProg.pathwayIds],
    })
    const created = await api.get(`/v1/school/programmes/${res.data.programmeId}`)
    programmes.value.push(created.data)
    progPathways[created.data.programmeId] = created.data.pathwayIds ?? []
    newProg.name = ''; newProg.code = ''; newProg.pathwayIds = []
    showAddProg.value = false
  } catch (e) {
    newProg.error = e.response?.data?.message ?? e.message ?? 'Failed to save'
  } finally {
    newProg.saving = false
  }
}

async function togglePathwayForProg(prog, pathwayId) {
  const id = prog.programmeId
  const current = progPathways[id] ? [...progPathways[id]] : []
  const idx = current.indexOf(pathwayId)
  if (idx >= 0) current.splice(idx, 1)
  else current.push(pathwayId)

  pathwayBusy[id] = true
  pathwayErr[id] = ''
  try {
    await api.put(`/v1/school/programmes/${id}`, {
      name: prog.name,
      code: prog.code,
      pathwayIds: current,
    })
    progPathways[id] = current
  } catch (e) {
    pathwayErr[id] = e.response?.data?.message ?? e.message ?? 'Failed to save pathways'
  } finally {
    pathwayBusy[id] = false
  }
}

async function softDeleteProgramme(prog) {
  try {
    await api.delete(`/v1/school/programmes/${prog.programmeId}`)
    programmes.value = programmes.value.filter(p => p.programmeId !== prog.programmeId)
    if (xProg.value === prog.programmeId) xProg.value = null
    if (showDeleted.value) await loadDeleted()
  } catch (e) { loadError.value = e.response?.data?.message ?? e.message ?? 'Delete failed' }
}

async function addMajor(progId) {
  const name = (mf[progId] ?? '').trim()
  if (!name) { mfErr[progId] = 'Major name is required.'; return }
  mfErr[progId] = ''
  try {
    const res = await api.post('/v1/school/majors', { programmeId: progId, name })
    const created = await api.get(`/v1/school/majors/${res.data.majorId}`)
    majors.value.push(created.data)
    mf[progId] = ''
  } catch (e) { mfErr[progId] = e.response?.data?.message ?? e.message ?? 'Failed to save' }
}

async function softDeleteMajor(maj) {
  try {
    await api.delete(`/v1/school/majors/${maj.majorId}`)
    majors.value = majors.value.filter(m => m.majorId !== maj.majorId)
    if (xMaj.value === maj.majorId) xMaj.value = null
    if (showDeleted.value) await loadDeleted()
  } catch (e) { loadError.value = e.response?.data?.message ?? e.message ?? 'Delete failed' }
}

async function addSubject(majorId) {
  const errKey = majorId + '_err'
  const name = (sf[majorId + '_n'] ?? '').trim()
  const code = (sf[majorId + '_code'] ?? '').trim()
  const ects = Number(sf[majorId + '_e']) || 15
  if (!name) { sf[errKey] = 'Module name is required.'; return }
  sf[errKey] = ''
  try {
    const res = await api.post('/v1/school/subjects', { majorId, code, name, ects })
    const created = await api.get(`/v1/school/subjects/${res.data.subjectId}`)
    subjects.value.push(created.data)
    sf[majorId + '_code'] = ''; sf[majorId + '_n'] = ''; sf[majorId + '_e'] = 15
  } catch (e) { sf[errKey] = e.response?.data?.message ?? e.message ?? 'Failed to save' }
}

async function softDeleteSubject(subj) {
  try {
    await api.delete(`/v1/school/subjects/${subj.subjectId}`)
    subjects.value = subjects.value.filter(s => s.subjectId !== subj.subjectId)
    if (showDeleted.value) await loadDeleted()
  } catch (e) { loadError.value = e.response?.data?.message ?? e.message ?? 'Delete failed' }
}

// ── Restore ────────────────────────────────────────────────────────────────────
async function restoreSubject(subj) {
  deleting[subj.subjectId] = 'restore'
  try {
    await api.post(`/v1/school/subjects/${subj.subjectId}/restore`)
    deletedSubjects.value = deletedSubjects.value.filter(s => s.subjectId !== subj.subjectId)
    await loadAll()
  } catch (e) { loadError.value = e.response?.data?.message ?? e.message ?? 'Restore failed' }
  finally { delete deleting[subj.subjectId] }
}

async function restoreMajor(maj) {
  deleting[maj.majorId] = 'restore'
  try {
    await api.post(`/v1/school/majors/${maj.majorId}/restore`)
    deletedMajors.value = deletedMajors.value.filter(m => m.majorId !== maj.majorId)
    await loadAll()
  } catch (e) { loadError.value = e.response?.data?.message ?? e.message ?? 'Restore failed' }
  finally { delete deleting[maj.majorId] }
}

async function restoreProgramme(prog) {
  deleting[prog.programmeId] = 'restore'
  try {
    await api.post(`/v1/school/programmes/${prog.programmeId}/restore`)
    deletedProgrammes.value = deletedProgrammes.value.filter(p => p.programmeId !== prog.programmeId)
    await loadAll()
  } catch (e) { loadError.value = e.response?.data?.message ?? e.message ?? 'Restore failed' }
  finally { delete deleting[prog.programmeId] }
}

// ── Permanent delete with cascade ──────────────────────────────────────────────
async function permanentDeleteSubject(subj) {
  deleting[subj.subjectId] = 'delete'
  try {
    await api.delete(`/v1/school/subjects/${subj.subjectId}/permanent`)
    deletedSubjects.value = deletedSubjects.value.filter(s => s.subjectId !== subj.subjectId)
  } catch (e) { loadError.value = e.response?.data?.message ?? e.message ?? 'Permanent delete failed' }
  finally { delete deleting[subj.subjectId] }
}

async function permanentDeleteMajor(maj) {
  deleting[maj.majorId] = 'delete'
  try {
    const allSubjects = [...subjects.value, ...deletedSubjects.value].filter(s => s.majorId === maj.majorId)
    for (const s of allSubjects) {
      await api.delete(`/v1/school/subjects/${s.subjectId}/permanent`)
    }
    await api.delete(`/v1/school/majors/${maj.majorId}/permanent`)
    subjects.value        = subjects.value.filter(s => s.majorId !== maj.majorId)
    deletedSubjects.value = deletedSubjects.value.filter(s => s.majorId !== maj.majorId)
    deletedMajors.value   = deletedMajors.value.filter(m => m.majorId !== maj.majorId)
  } catch (e) { loadError.value = e.response?.data?.message ?? e.message ?? 'Permanent delete failed' }
  finally { delete deleting[maj.majorId] }
}

async function permanentDeleteProgramme(prog) {
  deleting[prog.programmeId] = 'delete'
  try {
    const allMajors = [...majors.value, ...deletedMajors.value].filter(m => m.programmeId === prog.programmeId)
    for (const maj of allMajors) {
      const allSubjects = [...subjects.value, ...deletedSubjects.value].filter(s => s.majorId === maj.majorId)
      for (const s of allSubjects) {
        await api.delete(`/v1/school/subjects/${s.subjectId}/permanent`)
      }
      await api.delete(`/v1/school/majors/${maj.majorId}/permanent`)
    }
    await api.delete(`/v1/school/programmes/${prog.programmeId}/permanent`)
    const progId = prog.programmeId
    const majorIds = allMajors.map(m => m.majorId)
    subjects.value          = subjects.value.filter(s => !majorIds.includes(s.majorId))
    deletedSubjects.value   = deletedSubjects.value.filter(s => !majorIds.includes(s.majorId))
    majors.value            = majors.value.filter(m => m.programmeId !== progId)
    deletedMajors.value     = deletedMajors.value.filter(m => m.programmeId !== progId)
    programmes.value        = programmes.value.filter(p => p.programmeId !== progId)
    deletedProgrammes.value = deletedProgrammes.value.filter(p => p.programmeId !== progId)
  } catch (e) { loadError.value = e.response?.data?.message ?? e.message ?? 'Permanent delete failed' }
  finally { delete deleting[prog.programmeId] }
}
</script>

<style scoped>
.page-wrapper { min-height: 100vh; background: #f2f5f9; }

.navbar { background: #003366; color: #fff; display: flex; align-items: center; justify-content: space-between; padding: 0.85rem 2rem; gap: 1rem; }
.brand-text { font-size: 1.05rem; font-weight: 700; white-space: nowrap; }
.nav-links { display: flex; gap: 0.25rem; flex: 1; padding: 0 1rem; }
.nav-link { color: rgba(255,255,255,0.75); text-decoration: none; padding: 0.35rem 0.9rem; border-radius: 5px; font-size: 0.88rem; transition: background 0.15s, color 0.15s; }
.nav-link:hover, .nav-link.router-link-active { background: rgba(255,255,255,0.15); color: #fff; }
.nav-right { display: flex; align-items: center; gap: 1rem; }
.nav-user { font-size: 0.88rem; opacity: 0.85; }
.btn-logout { background: rgba(255,255,255,0.12); border: 1px solid rgba(255,255,255,0.25); color: #fff; border-radius: 5px; padding: 0.3rem 0.8rem; font-size: 0.83rem; cursor: pointer; }
.btn-logout:hover { background: rgba(255,255,255,0.22); }

.container { max-width: 980px; margin: 2rem auto; padding: 0 1.5rem; }
.page-header { display: flex; align-items: flex-start; justify-content: space-between; margin-bottom: 1.25rem; }
.page-title { font-size: 1.5rem; font-weight: 700; color: #003366; margin: 0; }
.page-sub { font-size: 0.82rem; color: #888; margin: 0.2rem 0 0; }
.header-actions { display: flex; gap: 0.6rem; align-items: center; }

.loading-state { color: #888; font-style: italic; padding: 2rem; text-align: center; }
.empty-state { background: #fff; border-radius: 10px; padding: 2.5rem; text-align: center; color: #aaa; box-shadow: 0 2px 8px rgba(0,0,0,0.06); }
.err-banner { background: #fef2f2; border: 1.5px solid #fca5a5; border-radius: 7px; padding: 0.65rem 1rem; color: #b91c1c; font-size: 0.86rem; margin-bottom: 1rem; }

/* Buttons */
.btn-primary { background: #003366; color: #fff; border: none; border-radius: 7px; padding: 0.6rem 1.25rem; font-size: 0.9rem; font-weight: 600; cursor: pointer; }
.btn-primary:hover:not(:disabled) { background: #0055a5; }
.btn-primary:disabled { opacity: 0.6; cursor: not-allowed; }
.btn-primary-sm { background: #003366; color: #fff; border: none; border-radius: 5px; padding: 0.38rem 0.85rem; font-size: 0.82rem; cursor: pointer; white-space: nowrap; }
.btn-primary-sm:hover { background: #0055a5; }
.btn-ghost { background: none; border: 1.5px solid #ccc; border-radius: 7px; padding: 0.5rem 0.9rem; font-size: 0.82rem; color: #666; cursor: pointer; }
.btn-del { background: #fdecea; color: #c0392b; border: 1px solid #f5c0bb; border-radius: 5px; padding: 0.25rem 0.65rem; font-size: 0.78rem; cursor: pointer; }
.btn-del:hover { background: #f9d4d0; }
.btn-del-sm { background: #fdecea; color: #c0392b; border: 1px solid #f5c0bb; border-radius: 4px; padding: 0.18rem 0.55rem; font-size: 0.74rem; cursor: pointer; flex-shrink: 0; }
.btn-del-sm:hover { background: #f9d4d0; }
.btn-collapse { background: #f0f3f7; color: #555; border: 1px solid #d8e2ee; border-radius: 5px; padding: 0.28rem 0.75rem; font-size: 0.8rem; cursor: pointer; white-space: nowrap; }
.btn-collapse:hover { background: #e0eaf5; }
.btn-add { background: #003366; color: #fff; border: none; border-radius: 5px; padding: 0.32rem 0.7rem; font-size: 0.79rem; cursor: pointer; white-space: nowrap; }
.btn-add:hover { background: #0055a5; }
.btn-x { background: none; border: none; color: #ccc; cursor: pointer; font-size: 0.82rem; padding: 0; line-height: 1; }
.btn-x:hover { color: #c0392b; }
.btn-restore { background: #d1fae5; color: #065f46; border: 1px solid #6ee7b7; border-radius: 5px; padding: 0.3rem 0.75rem; font-size: 0.8rem; font-weight: 600; cursor: pointer; white-space: nowrap; }
.btn-restore:hover:not(:disabled) { background: #a7f3d0; }
.btn-restore:disabled { opacity: 0.6; cursor: not-allowed; }
.btn-perm-del { background: #c0392b; color: #fff; border: none; border-radius: 5px; padding: 0.3rem 0.75rem; font-size: 0.8rem; font-weight: 600; cursor: pointer; white-space: nowrap; }
.btn-perm-del:hover:not(:disabled) { background: #992d22; }
.btn-perm-del:disabled { opacity: 0.6; cursor: not-allowed; }
.btn-toggle-deleted { background: #fff; border: 1.5px solid #d0dbe8; color: #555; border-radius: 7px; padding: 0.5rem 1rem; font-size: 0.86rem; cursor: pointer; white-space: nowrap; transition: background 0.15s, border-color 0.15s; }
.btn-toggle-deleted:hover:not(:disabled) { background: #f0f4f8; border-color: #a0b0c8; }
.btn-toggle-deleted.active { background: #fef2f2; border-color: #f5c0bb; color: #b91c1c; }
.btn-toggle-deleted:disabled { opacity: 0.6; cursor: not-allowed; }

/* Add programme card */
.add-prog-card { background: #fff; border: 1.5px solid #dde6f0; border-radius: 10px; padding: 1.2rem 1.5rem; margin-bottom: 1.25rem; box-shadow: 0 2px 8px rgba(0,0,0,0.05); }
.add-prog-fields { display: flex; gap: 0.75rem; flex-wrap: wrap; }
.field { display: flex; flex-direction: column; gap: 0.28rem; flex: 1 1 200px; }
.field-code { flex: 0 0 120px !important; }
.field label { font-size: 0.8rem; font-weight: 600; color: #444; }
.req { color: #c0392b; }
.field input { padding: 0.55rem 0.75rem; border: 1.5px solid #ccc; border-radius: 6px; font-size: 0.9rem; font-family: inherit; outline: none; }
.field input:focus { border-color: #0055a5; }
.add-prog-actions { display: flex; gap: 0.6rem; justify-content: flex-end; margin-top: 0.85rem; }

/* Programme card */
.prog-card { background: #fff; border: 1.5px solid #e0e8f0; border-radius: 10px; margin-bottom: 1rem; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.05); }
.prog-header { display: flex; align-items: center; justify-content: space-between; padding: 1rem 1.4rem; }
.prog-header-left { display: flex; align-items: center; gap: 0.6rem; flex: 1; flex-wrap: wrap; }
.prog-name { font-size: 1rem; font-weight: 700; color: #1a2d4f; }
.prog-header-right { display: flex; align-items: center; gap: 0.5rem; flex-shrink: 0; }
.badge-code  { background: #e8f0f8; color: #003366; border-radius: 4px; padding: 2px 8px; font-size: 0.75rem; font-weight: 700; }
.badge-count { background: #f0f3f7; color: #777; border-radius: 4px; padding: 2px 8px; font-size: 0.75rem; }
.prog-body { border-top: 1.5px solid #e8edf4; padding: 0 0 0.75rem; }
.section-label { font-size: 0.69rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.08em; color: #999; padding: 0.75rem 1.4rem 0.4rem; }

/* Major */
.maj-card { border-top: 1px solid #edf1f7; }
.maj-card:first-of-type { border-top: none; }
.maj-row { display: flex; align-items: center; gap: 0.6rem; padding: 0.6rem 1.4rem; cursor: pointer; user-select: none; }
.maj-row:hover { background: #f7f9fb; }
.maj-arrow { font-size: 0.7rem; color: #888; width: 10px; flex-shrink: 0; }
.maj-name  { font-size: 0.9rem; font-weight: 700; color: #1a2d4f; flex: 1; }
.maj-count { font-size: 0.78rem; color: #999; white-space: nowrap; margin-left: auto; }

/* Subjects */
.subj-table { padding: 0 1.4rem 0.75rem; }
.subj-col-header { display: flex; align-items: center; padding: 0.4rem 0; border-bottom: 1px solid #e8edf4; }
.subj-col-header span { font-size: 0.69rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.06em; color: #aaa; }
.subj-empty { font-size: 0.82rem; color: #bbb; font-style: italic; padding: 0.5rem 0; }
.subj-row { display: flex; align-items: center; padding: 0.45rem 0; border-bottom: 1px solid #f3f5f8; font-size: 0.87rem; }
.subj-row:last-of-type { border-bottom: none; }
.col-code { width: 110px; flex-shrink: 0; padding-right: 0.75rem; }
.col-name { flex: 1; padding-right: 0.75rem; }
.col-ects { width: 56px; flex-shrink: 0; text-align: right; padding-right: 0.75rem; }
.col-act  { width: 28px; flex-shrink: 0; text-align: center; }
.scode { font-family: ui-monospace, monospace; font-size: 0.8rem; color: #0055a5; font-weight: 700; }
.ects-val { font-weight: 700; color: #333; }
.add-subj-row { display: flex; align-items: center; padding: 0.45rem 0 0; border-top: 1px dashed #e0e8f0; margin-top: 0.35rem; }
.inp-code { padding: 0.32rem 0.5rem; border: 1.5px solid #d0dbe8; border-radius: 5px; font-size: 0.82rem; outline: none; font-family: ui-monospace, monospace; box-sizing: border-box; width: 110px; flex-shrink: 0; padding-right: 0.75rem; }
.inp-code:focus { border-color: #003366; }
.inp-name { flex: 1; padding: 0.32rem 0.6rem; border: 1.5px solid #d0dbe8; border-radius: 5px; font-size: 0.84rem; outline: none; margin-right: 0.75rem; }
.inp-name:focus { border-color: #003366; }
.inp-ects { width: 56px; flex-shrink: 0; padding: 0.32rem 0.4rem; border: 1.5px solid #d0dbe8; border-radius: 5px; font-size: 0.84rem; text-align: right; outline: none; margin-right: 0.75rem; }
.inp-ects:focus { border-color: #003366; }
.add-maj-row { display: flex; align-items: center; gap: 0.55rem; margin: 0.65rem 1.4rem 0; padding-top: 0.65rem; border-top: 1px dashed #dde6f0; }
.inp-maj { flex: 1; padding: 0.42rem 0.75rem; border: 1.5px solid #ccc; border-radius: 6px; font-size: 0.87rem; outline: none; }
.inp-maj:focus { border-color: #003366; }

/* Deleted section */
.deleted-section-header {
  display: flex; align-items: center; gap: 1rem;
  font-size: 0.82rem; font-weight: 700; color: #c0392b;
  margin: 1.75rem 0 0.75rem; padding-bottom: 0.5rem;
  border-bottom: 2px dashed #fca5a5;
}
.deleting-hint { font-weight: 400; color: #888; font-size: 0.78rem; }

.del-card { background: #fff; border: 1.5px solid #f5c0bb; border-radius: 8px; margin-bottom: 0.35rem; overflow: hidden; }
.del-card-child      { margin-left: 1.5rem; border-color: #fde8e8; }
.del-card-grandchild { margin-left: 3rem; border-color: #fef2f2; }
.del-card-header { display: flex; align-items: center; justify-content: space-between; padding: 0.7rem 1rem; }
.del-card-title { display: flex; align-items: center; gap: 0.5rem; flex: 1; flex-wrap: wrap; font-size: 0.87rem; }
.del-icon { font-size: 0.95rem; flex-shrink: 0; }
.del-date { font-size: 0.75rem; color: #999; margin-left: 0.25rem; }
.del-prog-hint { font-size: 0.78rem; color: #888; }
.del-card-actions { display: flex; gap: 0.4rem; flex-shrink: 0; margin-left: 1rem; }
.del-empty { color: #bbb; font-style: italic; font-size: 0.85rem; text-align: center; padding: 1rem 0; }

/* Breadcrumb rows showing grayed-out active parents */
.del-breadcrumb-row { display: flex; align-items: center; gap: 0.4rem; padding: 0.5rem 0 0.2rem 0.25rem; margin-top: 0.75rem; }
.del-breadcrumb { display: flex; align-items: center; gap: 0.4rem; padding: 0.3rem 1rem 0; }
.bc-item { font-size: 0.78rem; color: #aaa; font-style: italic; }
.bc-active { color: #aaa; }
.bc-sep { font-size: 0.78rem; color: #ccc; }

.form-error { color: #b91c1c; font-size: 0.82rem; margin: 0.3rem 0 0; }
.form-error-inline { color: #b91c1c; font-size: 0.82rem; }

.section-label-toggle {
  display: flex; align-items: center; gap: 0.5rem;
  cursor: pointer; user-select: none;
}
.section-label-toggle:hover { background: #f7f9fb; }
.section-arrow { color: #888; font-size: 0.78rem; width: 10px; flex-shrink: 0; }

.pathway-edit-block { padding: 0 1.4rem 0.75rem; }
.pathway-grid {
  display: flex; flex-direction: column; gap: 0.3rem;
  border: 1px solid #e8edf4; border-radius: 6px; padding: 0.55rem 0.75rem;
  max-height: 240px; overflow: auto; background: #fafbfc;
}
.pathway-row { display: flex; align-items: center; gap: 0.5rem; font-size: 0.85rem; cursor: pointer; }
.pathway-row input[type=checkbox] { width: 14px; height: 14px; accent-color: #003366; }
.empty-note { color: #aaa; font-style: italic; font-size: 0.83rem; margin: 0; }
</style>
