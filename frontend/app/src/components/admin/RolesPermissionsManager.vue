<template>
  <div class="rp">
    <div class="rp-head">
      <div>
        <h2 class="rp-title">Roles &amp; Permissions</h2>
        <p class="rp-sub">Configure what each admin role can do. <strong>Super Administrator</strong> always has full
          access and cannot be changed. Tick a box to grant a permission; every change is recorded below.</p>
      </div>
      <button class="btn-ghost" @click="showHistory = !showHistory; if (showHistory) loadAudit()">
        {{ showHistory ? 'Hide history' : 'View change history' }}
      </button>
    </div>

    <div v-if="error" class="rp-err">{{ error }}</div>
    <div v-if="loading" class="rp-loading">Loading…</div>

    <div v-else-if="matrix" class="rp-scroll">
      <table class="rp-grid">
        <thead>
          <tr>
            <th class="rp-permcol">Permission</th>
            <th v-for="r in matrix.roles" :key="r.name" class="rp-rolecol" :class="{ 'is-super': r.name === matrix.superRole }">
              <span class="rp-rolelabel">{{ r.label }}</span>
              <span v-if="r.name === matrix.superRole" class="rp-lock" title="Always full access">🔒</span>
            </th>
          </tr>
        </thead>
        <tbody>
          <template v-for="grp in areas" :key="grp.area">
            <tr class="rp-arearow"><td :colspan="matrix.roles.length + 1">{{ grp.area }}</td></tr>
            <tr v-for="p in grp.perms" :key="p.key">
              <td class="rp-permcol">
                <span class="rp-permlabel">{{ p.label }}</span>
                <span class="rp-permkey">{{ p.key }}</span>
                <span v-if="p.description" class="rp-permdesc">{{ p.description }}</span>
              </td>
              <td v-for="r in matrix.roles" :key="r.name" class="rp-cell" :class="{ 'is-super': r.name === matrix.superRole }">
                <span v-if="r.name === matrix.superRole" class="rp-always" title="Always granted">◆</span>
                <input v-else type="checkbox"
                       :checked="isGranted(r.name, p.key)"
                       :disabled="savingKey === (r.name + '|' + p.key)"
                       @change="toggle(r, p, $event.target.checked)" />
              </td>
            </tr>
          </template>
        </tbody>
      </table>
    </div>

    <div v-if="showHistory" class="rp-history">
      <h3 class="rp-h3">Change history</h3>
      <p v-if="!audit.length" class="rp-sub">No changes recorded yet.</p>
      <table v-else class="rp-audit">
        <thead><tr><th>When</th><th>By</th><th>Role</th><th>Permission</th><th>Change</th></tr></thead>
        <tbody>
          <tr v-for="(a, i) in audit" :key="i">
            <td>{{ fmt(a.changedAt) }}</td>
            <td>{{ a.changedBy || '—' }}</td>
            <td>{{ a.roleName }}</td>
            <td class="mono">{{ a.permissionKey }}</td>
            <td>
              <span :class="a.newValue ? 'chg-grant' : 'chg-revoke'">
                {{ a.oldValue ? 'granted' : 'not granted' }} → {{ a.newValue ? 'granted' : 'not granted' }}
              </span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import api from '../../api/client.js'

const matrix = ref(null)
const grants = reactive({})     // roleName -> Set-like array of granted keys
const loading = ref(true)
const error = ref('')
const savingKey = ref('')
const showHistory = ref(false)
const audit = ref([])

const fmt = d => d ? new Date(d).toLocaleString('en-GB', { dateStyle: 'medium', timeStyle: 'short' }) : ''

const areas = computed(() => {
  const out = []
  for (const p of matrix.value?.permissions ?? []) {
    let g = out.find(a => a.area === p.area)
    if (!g) { g = { area: p.area, perms: [] }; out.push(g) }
    g.perms.push(p)
  }
  return out
})

function isGranted(roleName, key) {
  return (grants[roleName] ?? []).includes(key)
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    const res = await api.get('/v1/admin/roles-permissions/matrix')
    matrix.value = res.data
    Object.keys(grants).forEach(k => delete grants[k])
    for (const [role, keys] of Object.entries(res.data.grants ?? {})) grants[role] = [...keys]
    for (const r of res.data.roles ?? []) if (!grants[r.name]) grants[r.name] = []
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to load the matrix'
  } finally {
    loading.value = false
  }
}

async function toggle(role, perm, value) {
  if (role.name === matrix.value.superRole) return
  const mark = role.name + '|' + perm.key
  savingKey.value = mark
  // Optimistic update.
  const set = new Set(grants[role.name] ?? [])
  value ? set.add(perm.key) : set.delete(perm.key)
  grants[role.name] = [...set]
  try {
    await api.put('/v1/admin/roles-permissions/grant', { roleName: role.name, permissionKey: perm.key, allowed: value })
    if (showHistory.value) loadAudit()
  } catch (e) {
    // Revert on failure.
    const back = new Set(grants[role.name] ?? [])
    value ? back.delete(perm.key) : back.add(perm.key)
    grants[role.name] = [...back]
    error.value = e.response?.data?.error ?? 'Could not save that change'
  } finally {
    savingKey.value = ''
  }
}

async function loadAudit() {
  try {
    const res = await api.get('/v1/admin/roles-permissions/audit')
    audit.value = res.data?.items ?? []
  } catch { /* ignore */ }
}

onMounted(load)
</script>

<style scoped>
.rp { }
.rp-head { display:flex; justify-content:space-between; align-items:flex-start; gap:1rem; margin-bottom:1rem; }
.rp-title { margin:0; font-size:1.15rem; color:#003366; }
.rp-sub { font-size:.82rem; color:#6b7888; margin:.25rem 0 0; max-width:70ch; }
.btn-ghost { background:#f2f5f9; border:1px solid #cfd7e3; border-radius:6px; padding:.4rem .8rem; font-size:.8rem; font-weight:600; color:#2c3e50; cursor:pointer; white-space:nowrap; }
.btn-ghost:hover { background:#e8eef6; }
.rp-err { background:#fdf3f2; border:1px solid #e2b8b5; color:#b3261e; padding:.5rem .7rem; border-radius:6px; font-size:.82rem; margin-bottom:.6rem; }
.rp-loading { color:#6b7888; padding:1rem 0; }

.rp-scroll { overflow-x:auto; border:1px solid #e4e7ec; border-radius:10px; background:#fff; }
.rp-grid { border-collapse:collapse; width:100%; font-size:.84rem; }
.rp-grid th, .rp-grid td { border-bottom:1px solid #eef1f5; }
.rp-grid thead th { position:sticky; top:0; background:#f7f9fc; z-index:2; padding:.55rem .6rem; font-size:.72rem; text-transform:uppercase; letter-spacing:.04em; color:#5b636e; }
.rp-rolecol { text-align:center; min-width:96px; }
.rp-rolecol.is-super { color:#003366; }
.rp-rolelabel { display:block; font-weight:700; }
.rp-lock { font-size:.7rem; }
.rp-permcol { text-align:left; position:sticky; left:0; background:#fff; z-index:1; min-width:280px; padding:.5rem .7rem; }
.rp-grid thead th.rp-permcol { z-index:3; background:#f7f9fc; }
.rp-permlabel { display:block; font-weight:600; color:#1a2d4f; }
.rp-permkey { display:block; font-family:ui-monospace,Menlo,Consolas,monospace; font-size:.72rem; color:#98a0ab; }
.rp-permdesc { display:block; font-size:.74rem; color:#6b7888; margin-top:.1rem; }
.rp-arearow td { background:#eef2f9; color:#2e3b87; font-weight:700; font-size:.72rem; text-transform:uppercase; letter-spacing:.05em; padding:.4rem .7rem; position:sticky; left:0; }
.rp-cell { text-align:center; padding:.45rem .5rem; }
.rp-cell.is-super { background:#f7f9fc; }
.rp-cell input { width:16px; height:16px; cursor:pointer; accent-color:#3E4DA8; }
.rp-always { color:#3E4DA8; font-weight:700; }

.rp-history { margin-top:1.5rem; }
.rp-h3 { font-size:1rem; color:#003366; margin:0 0 .5rem; }
.rp-audit { width:100%; border-collapse:collapse; font-size:.8rem; }
.rp-audit th, .rp-audit td { text-align:left; padding:.4rem .6rem; border-bottom:1px solid #eef1f5; }
.rp-audit th { color:#6b7888; font-size:.72rem; text-transform:uppercase; letter-spacing:.03em; }
.mono { font-family:ui-monospace,Menlo,Consolas,monospace; font-size:.76rem; }
.chg-grant { color:#2f6f4f; font-weight:600; }
.chg-revoke { color:#b3261e; font-weight:600; }
</style>
