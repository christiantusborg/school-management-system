<template>
  <transition name="rw-fade">
    <div class="rw-overlay" v-if="true">
      <div class="rw-modal">
        <!-- Header -->
        <div class="rw-header">
          <div>
            <h2>Admission Review</h2>
            <p class="rw-sub">{{ student.firstName }} {{ student.lastName }} · {{ student.studentId }}</p>
          </div>
          <button class="rw-close" @click="$emit('close')">✕</button>
        </div>

        <!-- Body -->
        <div class="rw-body">
          <div class="rw-notice">
            <span class="rw-notice-icon">ℹ️</span>
            <span><strong>Document approval is only required by the partner.</strong>
              Admission auto-approves all documents on submit. The list below is shown for
              reference only.</span>
          </div>

          <ul class="rw-doc-list" v-if="availableDocKeys.length">
            <li v-for="key in availableDocKeys" :key="key" class="rw-doc-row">
              <span class="rw-doc-tick tick-on">✓</span>
              <span class="rw-doc-info">
                <strong>{{ DOC_LABEL[key] }}</strong>
                <small v-if="docFileName(key)">{{ docFileName(key) }}</small>
              </span>
              <span class="rw-doc-state">AUTO-APPROVE</span>
            </li>
          </ul>

          <label class="rw-attest rw-attest-payment">
            <input type="checkbox" v-model="paymentReceived" />
            <span><strong>Payment received.</strong> The student's tuition payment has been confirmed.</span>
          </label>

          <p v-if="submitError" class="rw-submit-err">{{ submitError }}</p>
        </div>

        <!-- Footer -->
        <div class="rw-footer">
          <button class="rw-btn-ghost" @click="$emit('close')">Cancel</button>
          <button class="rw-btn-submit rw-btn-submit-approve"
                  :disabled="!paymentReceived || submitting" @click="submit">
            {{ submitting ? 'Submitting…' : 'Submit & Approve' }}
          </button>
        </div>
      </div>
    </div>
  </transition>
</template>

<script setup>
import { ref, computed } from 'vue'
import api from '../../api/client.js'

const props = defineProps({
  student: { type: Object, required: true },
})
const emit = defineEmits(['close', 'submitted'])

const DOC_LABEL = {
  passport: 'Passport / ID',
  degree:   'Degree certificate',
  language: 'Language test result',
  cv:       'CV / Résumé',
}
const DOC_KEYS = ['passport', 'degree', 'language', 'cv']

// Per user requirement (2026-05-08): document approval is the partner's job;
// Admission auto-approves on submit. The earlier per-doc-checkbox UI was
// removed. A future flow may reintroduce explicit Admission approval per
// programme — see project memory `project_admission_doc_approval_future_flow`.
const availableDocKeys = computed(() =>
  DOC_KEYS.filter(k => props.student?.docIds?.[k]))

function docFileName(key) {
  return props.student?.docMeta?.[key]?.fileName ?? null
}

const paymentReceived = ref(false)
const submitting = ref(false)
const submitError = ref('')

async function submit() {
  if (!paymentReceived.value || submitting.value) return
  submitError.value = ''
  submitting.value = true

  const s = props.student
  const e = s.enrollments?.[0]
  if (!s.studentGuid || !e?.id) {
    submitError.value = 'Missing student or enrolment id — close and re-open the review.'
    submitting.value = false
    return
  }

  // Auto-approve every uploaded doc — Admission doesn't review documents
  // today. Backend still accepts per-doc decisions, so when the future
  // explicit-approval flow lands the request shape is unchanged.
  const documents = availableDocKeys.value.map(key => ({
    studentDocumentId: s.docIds[key],
    documentLabel: DOC_LABEL[key],
    decision: 'approved',
    reasons: [],
    freeTextReason: null,
  }))

  try {
    await api.post(
      `/v1/admin/students/${s.studentGuid}/enrollments/${e.id}/review`,
      { documents }
    )
  } catch (err) {
    submitError.value = err.response?.data?.error ?? err.message ?? 'Failed to submit review'
    submitting.value = false
    return
  }

  submitting.value = false
  emit('submitted', s)
  emit('close')
}
</script>

<style scoped>
.rw-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.45); z-index: 400; }
.rw-modal {
  position: fixed; top: 50%; left: 50%; transform: translate(-50%, -50%);
  width: 600px; max-width: 96vw; max-height: 92vh; background: #fff; border-radius: 10px;
  z-index: 401; display: flex; flex-direction: column;
  box-shadow: 0 12px 40px rgba(0,0,0,0.25); overflow: hidden;
}

.rw-header { display: flex; justify-content: space-between; align-items: flex-start; padding: 1rem 1.25rem; border-bottom: 1.5px solid #e8edf4; }
.rw-header h2 { margin: 0; color: #003366; font-size: 1.05rem; }
.rw-sub { margin: 0.2rem 0 0; color: #888; font-size: 0.82rem; }
.rw-close { background: none; border: none; font-size: 1.2rem; color: #888; cursor: pointer; }

.rw-body { padding: 1.1rem 1.25rem; flex: 1; overflow-y: auto; }

.rw-notice { display: flex; align-items: flex-start; gap: 0.55rem; padding: 0.7rem 0.85rem; background: #eef3fb; border: 1.5px solid #b9d5f0; border-left: 3px solid #1a4d8c; border-radius: 7px; color: #1a2d4f; font-size: 0.85rem; line-height: 1.5; margin-bottom: 1rem; }
.rw-notice-icon { font-size: 1rem; line-height: 1; flex-shrink: 0; }

.rw-doc-list { list-style: none; padding: 0; margin: 0 0 1rem; display: flex; flex-direction: column; gap: 0.4rem; }
.rw-doc-row { background: #f7faf7; border: 1.5px solid #c8e6c9; border-left: 3px solid #16a34a; border-radius: 7px; padding: 0.5rem 0.75rem; display: flex; align-items: center; gap: 0.7rem; }
.rw-doc-tick { display: inline-flex; align-items: center; justify-content: center; width: 22px; height: 22px; border-radius: 50%; font-size: 0.78rem; font-weight: 700; flex-shrink: 0; }
.rw-doc-tick.tick-on { background: #d1fae5; color: #065f46; }
.rw-doc-info { flex: 1; display: flex; flex-direction: column; min-width: 0; }
.rw-doc-info strong { font-size: 0.88rem; color: #1a2d4f; }
.rw-doc-info small { font-size: 0.72rem; color: #6b7888; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.rw-doc-state { font-size: 0.68rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.04em; padding: 0.15rem 0.55rem; border-radius: 10px; background: #d1fae5; color: #065f46; }

.rw-attest { display: flex; align-items: flex-start; gap: 0.6rem; padding: 0.9rem 1rem; border-radius: 8px; font-size: 0.9rem; line-height: 1.5; color: #333; }
.rw-attest input { margin-top: 3px; flex-shrink: 0; }
.rw-attest-payment { background: #ecfdf5; border: 1.5px solid #6ee7b7; color: #065f46; }

.rw-submit-err { color: #b91c1c; font-size: 0.85rem; margin: 0.8rem 0 0; }

.rw-footer { display: flex; justify-content: space-between; padding: 0.85rem 1.25rem; border-top: 1.5px solid #e8edf4; background: #fafbfc; }
.rw-btn-ghost { background: #fff; border: 1.5px solid #d0d7e0; color: #5f6e85; padding: 0.5rem 1.1rem; border-radius: 7px; cursor: pointer; font-size: 0.86rem; }
.rw-btn-submit { padding: 0.5rem 1.4rem; border-radius: 7px; font-weight: 700; font-size: 0.88rem; cursor: pointer; border: none; color: #fff; }
.rw-btn-submit-approve { background: #16a34a; }
.rw-btn-submit-approve:hover:not(:disabled) { background: #15803d; }
.rw-btn-submit:disabled { background: #aaa; cursor: not-allowed; }

.rw-fade-enter-active, .rw-fade-leave-active { transition: opacity 0.18s ease; }
.rw-fade-enter-from, .rw-fade-leave-to { opacity: 0; }
</style>
