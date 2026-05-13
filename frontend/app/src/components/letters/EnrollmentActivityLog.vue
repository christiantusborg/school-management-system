<template>
  <div class="activity-log">
    <details class="activity-wrap" :open="defaultOpen">
      <summary class="activity-summary">
        <span class="activity-icon">📋</span>
        <span class="activity-title">Activity</span>
        <span v-if="!loading && !loadError" class="activity-count">{{ items.length }}</span>
      </summary>
      <div class="activity-body">
        <p v-if="loading" class="muted">Loading…</p>
        <p v-else-if="loadError" class="err-banner">{{ loadError }}</p>
        <p v-else-if="!items.length" class="muted">No activity yet.</p>
        <ol v-else class="timeline">
          <li v-for="entry in items" :key="entry.id" class="t-row" :class="`t-${roleClass(entry.actorRole)}`"
              :title="entry.action">
            <span class="t-marker">{{ iconFor(entry) }}</span>
            <div class="t-content">
              <div class="t-head">
                <strong class="t-actor">{{ entry.actorName }}</strong>
                <span class="t-role" :class="`role-${roleClass(entry.actorRole)}`">{{ entry.actorRole }}</span>
              </div>
              <div class="t-action">{{ entry.action }}</div>
            </div>
            <div class="t-time">
              <div class="t-date">{{ formatFullDate(entry.atUtc) }}</div>
              <div class="t-rel">{{ formatRelative(entry.atUtc) }}</div>
            </div>
          </li>
        </ol>
      </div>
    </details>
  </div>
</template>

<script setup>
import { ref, watch } from 'vue'
import api from '../../api/client.js'

const props = defineProps({
  apiPath: { type: String, required: true },
  defaultOpen: { type: Boolean, default: false },
})

const items = ref([])
const loading = ref(false)
const loadError = ref('')

async function load() {
  if (!props.apiPath) return
  loading.value = true
  loadError.value = ''
  try {
    const res = await api.get(props.apiPath)
    items.value = res.data.items ?? []
  } catch (e) {
    loadError.value = e.response?.data?.error ?? e.message ?? 'Failed to load activity'
  } finally {
    loading.value = false
  }
}

defineExpose({ refresh: load })

watch(() => props.apiPath, load, { immediate: true })

function roleClass(role) {
  const r = (role || '').toLowerCase()
  if (r === 'admin') return 'admin'
  if (r === 'partner') return 'partner'
  if (r === 'student') return 'student'
  return 'system'
}

function iconFor(entry) {
  // Document events get an icon mirroring the doc state. Status events
  // use a workflow icon. Falls back to a neutral dot.
  const code = entry.statusCode || ''
  if (entry.source === 'document') {
    if (code === 'Submitted')           return '⤴'
    if (code === 'VerifiedByPartner')   return '✓'
    if (code === 'VerifiedByEnrolment') return '✓✓'
    if (code === 'RejectedByPartner' || code === 'RejectedByEnrolment') return '✕'
    return '📎'
  }
  if (code === 'GradesApproved')                       return '🎓'
  if (code === 'AwaitingGradesApproval')               return '📨'
  if (code === 'AwaitingGradesSubmit')                 return '✎'
  if (code === 'AcceptOffer')                          return '🟣'
  if (code === 'ApplicationAwaitingReviewByAdmission') return '🔵'
  if (code === 'ApplicationAwaitingReviewByPartner')   return '🟡'
  if (code === 'ApplicationRejectedByPartner' || code === 'ApplicationRejectedByAdmission') return '✕'
  return '•'
}

function formatRelative(iso) {
  if (!iso) return ''
  const ms = new Date().getTime() - new Date(iso).getTime()
  const sec = Math.floor(ms / 1000)
  if (sec < 60) return 'just now'
  const min = Math.floor(sec / 60)
  if (min < 60) return `${min} min ago`
  const hr = Math.floor(min / 60)
  if (hr < 24) return `${hr} h ago`
  const d = Math.floor(hr / 24)
  if (d < 30) return `${d} day${d === 1 ? '' : 's'} ago`
  return new Date(iso).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })
}
function formatFullDate(iso) {
  if (!iso) return ''
  return new Date(iso).toLocaleString('en-GB', {
    day: '2-digit', month: 'short', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  })
}
</script>

<style scoped>
.activity-log { margin-top: .85rem; }
.activity-wrap { background: #fff; border: 1px solid #e6ebf2; border-radius: 8px; }
.activity-summary { padding: .65rem .85rem; cursor: pointer; display: flex; align-items: center; gap: .5rem; font-weight: 600; color: #1a2d4f; user-select: none; }
.activity-summary:hover { background: #f6f9fc; }
.activity-icon { font-size: 1rem; }
.activity-title { font-size: .92rem; }
.activity-count { margin-left: auto; background: #e8edf4; color: #4a5a72; font-size: .72rem; font-weight: 700; padding: .1rem .5rem; border-radius: 10px; }
.activity-body { padding: .25rem .85rem .85rem .85rem; border-top: 1px solid #eef2f7; }
.muted { color: #6b7888; font-size: .85rem; padding: .5rem 0; margin: 0; }
.err-banner { background: #fef2f2; border: 1px solid #fca5a5; color: #b91c1c; padding: .55rem .75rem; border-radius: 6px; font-size: .82rem; margin: .5rem 0 0 0; }

.timeline { list-style: none; padding: 0; margin: .5rem 0 0 0; }
/* Fixed-height rows so the timeline reads as a uniform list. Long action
   text is truncated with ellipsis; full text shows on hover via the title
   attribute. */
.t-row { display: flex; gap: .65rem; padding: .5rem .25rem; border-bottom: 1px dashed #eef2f7; align-items: center; height: 56px; box-sizing: border-box; }
.t-row:last-child { border-bottom: none; }
.t-marker { display: inline-flex; align-items: center; justify-content: center; width: 26px; height: 26px; border-radius: 50%; background: #f0f4f9; flex-shrink: 0; font-size: .85rem; }
.t-student .t-marker { background: #fff7e6; }
.t-partner .t-marker { background: #eaf6ec; }
.t-admin   .t-marker { background: #eef3fb; }
.t-system  .t-marker { background: #f4f4f4; color: #6b7888; }

.t-content { flex: 1; min-width: 0; display: flex; flex-direction: column; justify-content: center; }
.t-head { display: flex; align-items: center; gap: .5rem; margin-bottom: .15rem; min-width: 0; }
.t-actor { font-size: .88rem; color: #1a2d4f; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.t-role { font-size: .65rem; font-weight: 700; padding: .08rem .5rem; border-radius: 10px; text-transform: uppercase; letter-spacing: .04em; flex-shrink: 0; }
.role-student { background: #fff7e6; color: #b66a00; }
.role-partner { background: #eaf6ec; color: #1c7a4a; }
.role-admin   { background: #eef3fb; color: #1a4d8c; }
.role-system  { background: #f4f4f4; color: #6b7888; }
.t-action { font-size: .85rem; color: #344a6c; line-height: 1.3; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.t-time { flex-shrink: 0; text-align: right; line-height: 1.25; }
.t-date { font-size: .72rem; color: #344a6c; font-weight: 600; white-space: nowrap; }
.t-rel { font-size: .68rem; color: #6b7888; white-space: nowrap; }
</style>
