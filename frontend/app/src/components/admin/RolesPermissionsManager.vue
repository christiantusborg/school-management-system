<template>
  <div class="rp">
    <div class="rp-head">
      <div>
        <h2 class="rp-title">Roles &amp; Permissions</h2>
        <p class="rp-sub">Configure exactly what each role can do and see. <strong>Super Administrator</strong> always has full
          access and cannot be changed. Every change is recorded in the history.</p>
      </div>
      <div class="rp-viewtabs">
        <button :class="{active: view==='items'}" @click="view='items'">Permissions</button>
        <button :class="{active: view==='status'}" @click="view='status'">Statuses</button>
        <button :class="{active: view==='history'}" @click="view='history'; loadAudit()">History</button>
      </div>
    </div>

    <div v-if="error" class="rp-err">{{ error }}</div>
    <div v-if="loading" class="rp-loading">Loading…</div>

    <!-- PERMISSIONS -->
    <template v-else-if="matrix && view==='items'">
      <p class="rp-legend">
        <span class="lg edit">Edit</span> full · <span class="lg view">View</span> read-only ·
        <span class="lg noacc">No&nbsp;access</span> locked · <span class="lg hidden">Hidden</span> not shown
      </p>
      <div v-for="surf in surfaces" :key="surf.name" class="rp-surface">
        <button class="rp-surfhead" @click="toggleSurf(surf.name)">
          <span class="rp-caret">{{ openSurfs.has(surf.name) ? '▾' : '▸' }}</span>
          {{ surf.name }} <span class="rp-count">{{ surf.items.length }}</span>
        </button>
        <div v-show="openSurfs.has(surf.name)" class="rp-scroll">
          <table class="rp-grid">
            <thead>
              <tr>
                <th class="rp-permcol">Item</th>
                <th v-for="r in matrix.roles" :key="r.name" :class="{'is-super': r.name===matrix.superRole}">{{ r.label }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="it in surf.items" :key="it.key">
                <td class="rp-permcol">
                  <span class="rp-permlabel">{{ it.label }}</span>
                  <span class="rp-typetag">{{ it.type }}</span>
                  <span class="rp-permkey">{{ it.key }}</span>
                </td>
                <td v-for="r in matrix.roles" :key="r.name" :class="['rp-cell', 'lvl-'+levelOf(r.name, it.key)]">
                  <span v-if="r.name===matrix.superRole" class="rp-always" title="Always full access">◆</span>
                  <select v-else class="rp-sel"
                          :value="levelOf(r.name, it.key)"
                          :disabled="savingKey===(r.name+'|'+it.key)"
                          @change="setLevel(r.name, it.key, +$event.target.value)">
                    <option v-for="o in optionsFor(it.type)" :key="o.v" :value="o.v">{{ o.l }}</option>
                  </select>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </template>

    <!-- STATUSES -->
    <template v-else-if="matrix && view==='status'">
      <p class="rp-sub" style="margin-bottom:.6rem">Which enrolment statuses each role can work with. <strong>Edit</strong> = see and act,
        <strong>View</strong> = read-only, <strong>Hidden</strong> = students in that status are filtered out for the role.</p>
      <div class="rp-scroll">
        <table class="rp-grid">
          <thead>
            <tr>
              <th class="rp-permcol">Status</th>
              <th v-for="r in matrix.roles" :key="r.name" :class="{'is-super': r.name===matrix.superRole}">{{ r.label }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="s in matrix.statuses" :key="s.id">
              <td class="rp-permcol"><span class="rp-permlabel">{{ s.name }}</span><span class="rp-permkey">level {{ s.level }}<template v-if="s.nextActionRole"> · {{ s.nextActionRole }} acts</template></span></td>
              <td v-for="r in matrix.roles" :key="r.name" :class="['rp-cell','lvl-'+statusLevelOf(r.name, s.id)]">
                <span v-if="r.name===matrix.superRole" class="rp-always">◆</span>
                <select v-else class="rp-sel"
                        :value="statusLevelOf(r.name, s.id)"
                        :disabled="savingKey===('S|'+r.name+'|'+s.id)"
                        @change="setStatusLevel(r.name, s.id, +$event.target.value)">
                  <option :value="3">Edit</option><option :value="2">View</option><option :value="0">Hidden</option>
                </select>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </template>

    <!-- HISTORY -->
    <template v-else-if="view==='history'">
      <h3 class="rp-h3">Change history</h3>
      <p v-if="!audit.length" class="rp-sub">No changes recorded yet.</p>
      <table v-else class="rp-audit">
        <thead><tr><th>When</th><th>By</th><th>Role</th><th>Item</th><th>Change</th></tr></thead>
        <tbody>
          <tr v-for="(a,i) in audit" :key="i">
            <td>{{ fmt(a.changedAt) }}</td><td>{{ a.changedBy || '—' }}</td><td>{{ a.roleName }}</td>
            <td class="mono">{{ a.permissionKey }}</td>
            <td><span class="lg" :class="lvlClass(a.oldLevel)">{{ LVL[a.oldLevel] }}</span> → <span class="lg" :class="lvlClass(a.newLevel)">{{ LVL[a.newLevel] }}</span></td>
          </tr>
        </tbody>
      </table>
    </template>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import api from '../../api/client.js'

const LVL = ['Hidden', 'No access', 'View', 'Edit']
const FIELD_OPTS = [{v:3,l:'Edit'},{v:2,l:'View'},{v:1,l:'No access'},{v:0,l:'Hidden'}]
const ACTION_OPTS = [{v:3,l:'Allow'},{v:1,l:'No access'},{v:0,l:'Hidden'}]
const optionsFor = t => (t === 'action' ? ACTION_OPTS : FIELD_OPTS)
const lvlClass = l => ['hidden','noacc','view','edit'][l] ?? 'hidden'

const matrix = ref(null)
const itemGrants = reactive({})    // role -> {key: level}
const statusGrants = reactive({})  // role -> {statusId: level}
const loading = ref(true)
const error = ref('')
const savingKey = ref('')
const view = ref('items')
const openSurfs = reactive(new Set())
const audit = ref([])

const fmt = d => d ? new Date(d).toLocaleString('en-GB', { dateStyle:'medium', timeStyle:'short' }) : ''

const surfaces = computed(() => {
  const out = []
  for (const r of matrix.value?.resources ?? []) {
    let g = out.find(s => s.name === r.surface)
    if (!g) { g = { name: r.surface, items: [] }; out.push(g) }
    g.items.push(r)
  }
  return out
})

const levelOf = (role, key) => itemGrants[role]?.[key] ?? 0
const statusLevelOf = (role, sid) => statusGrants[role]?.[sid] ?? 3  // missing = Edit
function toggleSurf(n){ openSurfs.has(n) ? openSurfs.delete(n) : openSurfs.add(n) }

async function load() {
  loading.value = true; error.value = ''
  try {
    const res = await api.get('/v1/admin/roles-permissions/matrix')
    matrix.value = res.data
    Object.keys(itemGrants).forEach(k => delete itemGrants[k])
    for (const [role, m] of Object.entries(res.data.itemGrants ?? {})) itemGrants[role] = { ...m }
    for (const r of res.data.roles ?? []) if (!itemGrants[r.name]) itemGrants[r.name] = {}
    Object.keys(statusGrants).forEach(k => delete statusGrants[k])
    for (const [role, m] of Object.entries(res.data.statusGrants ?? {})) statusGrants[role] = { ...m }
    for (const r of res.data.roles ?? []) if (!statusGrants[r.name]) statusGrants[r.name] = {}
    if (surfaces.value[0]) openSurfs.add(surfaces.value[0].name)
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to load the matrix'
  } finally { loading.value = false }
}

async function setLevel(role, key, level) {
  if (role === matrix.value.superRole) return
  const mark = role + '|' + key
  savingKey.value = mark
  const prev = itemGrants[role]?.[key] ?? 0
  itemGrants[role] = { ...(itemGrants[role] ?? {}), [key]: level }
  try {
    await api.put('/v1/admin/roles-permissions/grant', { roleName: role, permissionKey: key, accessLevel: level })
  } catch (e) {
    itemGrants[role] = { ...(itemGrants[role] ?? {}), [key]: prev }
    error.value = e.response?.data?.error ?? 'Could not save that change'
  } finally { savingKey.value = '' }
}

async function setStatusLevel(role, sid, level) {
  if (role === matrix.value.superRole) return
  const mark = 'S|' + role + '|' + sid
  savingKey.value = mark
  const prev = statusGrants[role]?.[sid] ?? 3
  statusGrants[role] = { ...(statusGrants[role] ?? {}), [sid]: level }
  try {
    await api.put('/v1/admin/roles-permissions/status-grant', { roleName: role, statusId: sid, accessLevel: level })
  } catch (e) {
    statusGrants[role] = { ...(statusGrants[role] ?? {}), [sid]: prev }
    error.value = e.response?.data?.error ?? 'Could not save that change'
  } finally { savingKey.value = '' }
}

async function loadAudit() {
  try { const res = await api.get('/v1/admin/roles-permissions/audit'); audit.value = res.data?.items ?? [] } catch { /* ignore */ }
}

onMounted(load)
</script>

<style scoped>
.rp-head { display:flex; justify-content:space-between; align-items:flex-start; gap:1rem; margin-bottom:1rem; flex-wrap:wrap; }
.rp-title { margin:0; font-size:1.15rem; color:#003366; }
.rp-sub { font-size:.82rem; color:#6b7888; margin:.25rem 0 0; max-width:74ch; }
.rp-viewtabs { display:flex; gap:.25rem; }
.rp-viewtabs button { background:#f2f5f9; border:1px solid #cfd7e3; border-radius:6px; padding:.35rem .8rem; font-size:.8rem; font-weight:600; color:#2c3e50; cursor:pointer; }
.rp-viewtabs button.active { background:#003366; color:#fff; border-color:#003366; }
.rp-err { background:#fdf3f2; border:1px solid #e2b8b5; color:#b3261e; padding:.5rem .7rem; border-radius:6px; font-size:.82rem; margin-bottom:.6rem; }
.rp-loading { color:#6b7888; padding:1rem 0; }
.rp-legend { font-size:.78rem; color:#6b7888; margin:.2rem 0 .8rem; }
.lg { display:inline-block; font-weight:700; border-radius:5px; padding:.02rem .3rem; margin:0 .1rem; font-size:.72rem; }
.lg.edit { color:#2f6f4f; background:#e7f1ec; } .lg.view { color:#2e5aa8; background:#e7eef8; }
.lg.noacc { color:#8a5a12; background:#f6eedd; } .lg.hidden { color:#8a2f3b; background:#f6e7e9; }
.rp-surface { margin-bottom:.6rem; }
.rp-surfhead { width:100%; text-align:left; background:#eef2f9; border:1px solid #dbe3f0; border-radius:8px; padding:.5rem .8rem; font-weight:700; color:#2e3b87; cursor:pointer; font-size:.9rem; }
.rp-caret { display:inline-block; width:1rem; }
.rp-count { color:#8a93a5; font-weight:600; font-size:.78rem; }
.rp-scroll { overflow-x:auto; border:1px solid #e4e7ec; border-top:none; border-radius:0 0 8px 8px; background:#fff; }
.rp-grid { border-collapse:collapse; width:100%; font-size:.82rem; }
.rp-grid th, .rp-grid td { border-bottom:1px solid #eef1f5; padding:.35rem .55rem; }
.rp-grid thead th { background:#f7f9fc; font-size:.68rem; text-transform:uppercase; letter-spacing:.04em; color:#5b636e; text-align:center; }
.rp-grid thead th.is-super { color:#003366; }
.rp-permcol { text-align:left; position:sticky; left:0; background:#fff; min-width:300px; }
.rp-grid thead th.rp-permcol { background:#f7f9fc; }
.rp-permlabel { font-weight:600; color:#1a2d4f; }
.rp-typetag { font-size:.62rem; text-transform:uppercase; letter-spacing:.04em; color:#8a93a5; border:1px solid #dbe3f0; border-radius:99px; padding:0 .35rem; margin-left:.4rem; }
.rp-permkey { display:block; font-family:ui-monospace,Menlo,Consolas,monospace; font-size:.7rem; color:#98a0ab; }
.rp-cell { text-align:center; }
.rp-cell.lvl-3 { background:#f2f9f5; } .rp-cell.lvl-2 { background:#f2f6fc; } .rp-cell.lvl-1 { background:#fbf6ec; } .rp-cell.lvl-0 { background:#fbf1f2; }
.rp-sel { font-size:.74rem; border:1px solid #cfd7e3; border-radius:5px; padding:.12rem .2rem; background:#fff; color:#2c3e50; }
.rp-always { color:#3E4DA8; font-weight:700; }
.rp-h3 { font-size:1rem; color:#003366; margin:0 0 .5rem; }
.rp-audit { width:100%; border-collapse:collapse; font-size:.8rem; }
.rp-audit th, .rp-audit td { text-align:left; padding:.4rem .6rem; border-bottom:1px solid #eef1f5; }
.rp-audit th { color:#6b7888; font-size:.72rem; text-transform:uppercase; }
.mono { font-family:ui-monospace,Menlo,Consolas,monospace; font-size:.74rem; }
</style>
