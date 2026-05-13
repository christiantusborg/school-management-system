<template>
  <div class="rw-decision">
    <div class="rw-decision-current">
      <span class="rw-decision-lbl">Decision:</span>
      <span :class="['rw-decision-chip', 'dc-' + (d?.status || 'pending')]">
        {{ d?.status === 'approved' ? 'Approved' : d?.status === 'rejected' ? 'Rejected' : 'Pending' }}
      </span>
      <span v-if="d?.status === 'rejected' && d.reason" class="rw-decision-reason">{{ d.reason }}</span>
    </div>
    <div class="rw-decision-actions">
      <button class="rw-btn-approve" @click="onApprove">✓ Approve</button>
      <button class="rw-btn-reject" @click="rejectOpen = !rejectOpen">✕ Reject</button>
    </div>
    <div v-if="rejectOpen" class="rw-reject-panel">
      <div class="rw-reject-label">Rejection reason (required, min 10 characters)</div>
      <div class="rw-chip-row">
        <button v-for="c in chips" :key="c"
                :class="['rw-reason-chip', { active: selected.has(c) }]"
                @click="toggleChip(c)">{{ c }}</button>
      </div>
      <textarea v-model="text" rows="3" placeholder="Add a specific reason…"></textarea>
      <div class="rw-reject-footer">
        <span class="rw-reject-count" :class="{ ok: text.trim().length >= 10 }">
          {{ text.trim().length }} chars
        </span>
        <button class="rw-btn-reject-confirm" :disabled="text.trim().length < 10" @click="confirmReject">
          Confirm rejection
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'

const props = defineProps({
  sectionKey: { type: String, required: true },
  draft: { type: Object, required: true },
  chips: { type: Array, default: () => [] },
  prefillReasons: { type: Array, default: () => [] },
})
const emit = defineEmits(['approve', 'reject'])

const d = computed(() => props.draft[props.sectionKey])
const rejectOpen = ref(d.value?.status === 'rejected')
const text = ref(d.value?.reason || '')
const selected = ref(new Set())
for (const c of props.chips) {
  if (text.value.includes(c)) selected.value.add(c)
}
// Pre-tick chips that map to checklist items the reviewer left unchecked.
// Only applies when no prior reason text exists (otherwise we'd clobber
// a manual edit).
if (!d.value?.reason) {
  for (const r of props.prefillReasons) selected.value.add(r)
}

function combined() {
  const chipPart = [...selected.value].join('; ')
  const freePart = text.value.replace(chipPart, '').trim().replace(/^;\s*/, '')
  return [chipPart, freePart].filter(Boolean).join('; ').trim()
}
function toggleChip(c) {
  if (selected.value.has(c)) selected.value.delete(c)
  else selected.value.add(c)
  const next = combined()
  if (next) text.value = next
}
function confirmReject() {
  const reason = combined()
  if (reason.trim().length < 10) return
  emit('reject', reason)
}
function onApprove() {
  rejectOpen.value = false
  selected.value.clear()
  text.value = ''
  emit('approve')
}
</script>

<style scoped>
.rw-decision { margin-top: auto; padding-top: 0.85rem; border-top: 1px solid #e8edf4; }
.rw-decision-current { font-size: 0.85rem; margin-bottom: 0.55rem; display: flex; flex-wrap: wrap; gap: 0.45rem; align-items: center; }
.rw-decision-lbl { color: #666; }
.rw-decision-reason { color: #991b1b; font-size: 0.78rem; font-style: italic; width: 100%; margin-top: 0.25rem; }
.rw-decision-actions { display: flex; gap: 0.6rem; }
.rw-btn-approve { background: #0d6b55; color: #fff; border: none; border-radius: 6px; padding: 0.6rem 1.3rem; font-size: 0.88rem; font-weight: 600; cursor: pointer; }
.rw-btn-approve:hover { background: #0a5a47; }
.rw-btn-reject { background: #fff; color: #b42318; border: 1.5px solid #fda29b; border-radius: 6px; padding: 0.6rem 1.3rem; font-size: 0.88rem; font-weight: 600; cursor: pointer; }
.rw-btn-reject:hover { background: #fee4e2; }

.rw-decision-chip { font-size: 0.72rem; font-weight: 700; padding: 2px 9px; border-radius: 14px; }
.dc-pending  { background: #eef2f7; color: #888; }
.dc-approved { background: #d1fae5; color: #065f46; }
.dc-rejected { background: #fee2e2; color: #991b1b; }

.rw-reject-panel { margin-top: 0.75rem; background: #fff7f7; border: 1px solid #fecaca; border-radius: 7px; padding: 0.75rem; }
.rw-reject-label { font-size: 0.8rem; font-weight: 600; color: #991b1b; margin-bottom: 0.5rem; }
.rw-chip-row { display: flex; flex-wrap: wrap; gap: 0.35rem; margin-bottom: 0.55rem; }
.rw-reason-chip { background: #fff; border: 1px solid #fca5a5; color: #991b1b; padding: 0.25rem 0.6rem; border-radius: 14px; font-size: 0.75rem; cursor: pointer; }
.rw-reason-chip.active { background: #991b1b; color: #fff; }
.rw-reject-panel textarea { width: 100%; padding: 0.55rem 0.7rem; border: 1.5px solid #fca5a5; border-radius: 6px; font-size: 0.86rem; font-family: inherit; resize: vertical; }
.rw-reject-footer { display: flex; justify-content: space-between; align-items: center; margin-top: 0.55rem; }
.rw-reject-count { font-size: 0.76rem; color: #991b1b; }
.rw-reject-count.ok { color: #065f46; }
.rw-btn-reject-confirm { background: #991b1b; color: #fff; border: none; border-radius: 5px; padding: 0.4rem 0.85rem; font-size: 0.82rem; font-weight: 600; cursor: pointer; }
.rw-btn-reject-confirm:disabled { opacity: 0.45; cursor: default; }
</style>
