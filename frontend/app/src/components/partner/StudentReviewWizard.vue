<template>
  <transition name="fade"><div class="rw-overlay" @click.self="$emit('close')" /></transition>
  <transition name="slide">
    <div class="rw-modal">
      <div class="rw-header">
        <div>
          <h2>Review Application</h2>
          <p class="rw-sub">{{ student.firstName }} {{ student.lastName }} · {{ student.studentId }}</p>
        </div>
        <button class="rw-close" @click="$emit('close')">✕</button>
      </div>

      <!-- Step bar -->
      <div class="rw-stepbar">
        <div v-for="(s, i) in STEPS" :key="s.key"
             :class="['rw-step-chip', stepClass(i + 1)]"
             :title="chipTitle(s.key)"
             @click="i + 1 < step ? goStep(i + 1) : null">
          <span class="rw-step-num">{{ stepIcon(i + 1, s.key) }}</span>
          <span class="rw-step-label">{{ s.label }}</span>
        </div>
      </div>

      <div class="rw-body">
        <!-- ═══════ Step 1: Passport & Identity ═══════ -->
        <div v-if="step === 1" class="rw-step-content">
          <div class="rw-three">
            <div class="rw-checklist-pane">
              <h4 class="rw-section-heading">What to verify</h4>
              <p class="rw-hint">Tick each item once you've confirmed it on the scan.</p>
              <label v-for="r in CHECKLIST.passport" :key="r.id" class="rw-check-item">
                <input type="checkbox" :checked="checklists.passport.has(r.id)" @change="toggleCheck('passport', r.id)" />
                <span :class="{ ticked: checklists.passport.has(r.id) }">{{ r.name }}</span>
              </label>
              <p v-if="!CHECKLIST.passport.length" class="rw-hint" style="margin-top:.4rem">No verification requirements configured for this document type.</p>

              <div v-if="!allChecked('passport') && CHECKLIST.passport.length" class="rw-reject-box">
                <div class="rw-reject-label">Rejection reason (required, min 10 characters)</div>
                <div class="rw-chip-row">
                  <button v-for="r in uncheckedRequirements('passport')" :key="r.id"
                          :class="['rw-reason-chip', { active: rejectChipsSelected.passport.has(reqLabelForChip(r)) }]"
                          @click="toggleRejectChip('passport', reqLabelForChip(r))">
                    {{ reqLabelForChip(r) }}
                  </button>
                </div>
                <textarea v-model="rejectText.passport" rows="3" placeholder="Add a specific reason or override the chips above…"></textarea>
                <div class="rw-reject-meta">
                  <span :class="{ ok: (rejectText.passport || '').trim().length >= 10 }">
                    {{ (rejectText.passport || '').trim().length }} chars
                  </span>
                </div>
              </div>

              <button v-if="!allChecked('passport') && CHECKLIST.passport.length"
                      class="rw-btn-reject-next" :disabled="!canReject('passport')" @click="rejectAndNext('passport')">
                ✕ Reject &amp; Next
              </button>
              <button v-if="allChecked('passport')"
                      class="rw-btn-approve-next" @click="approveAndNext('passport')">
                ✓ Approve &amp; Next
              </button>
            </div>
            <div class="rw-preview-pane">
              <h4 class="rw-section-heading">Passport / ID document</h4>
              <div v-if="rejectionInfo('passport')" class="rw-reject-banner">
                <strong>Rejected by {{ rejectionInfo('passport').by }}</strong>
                <span v-if="rejectionInfo('passport').reason"> — {{ rejectionInfo('passport').reason }}</span>
              </div>
              <DocPreview :filename="student.docPassport" :student-id="student.studentGuid" :document-id="student.docIds?.passport" />
            </div>
            <div class="rw-data-pane">
              <h4 class="rw-section-heading">Applicant identity data</h4>
              <p class="rw-hint">Confirm the data matches the scan.</p>
              <div class="rw-readonly-field"><label>First name</label><div>{{ student.firstName || '—' }}</div></div>
              <div class="rw-readonly-field"><label>Last name</label><div>{{ student.lastName || '—' }}</div></div>
              <div class="rw-readonly-field"><label>Date of birth</label><div>{{ student.dateOfBirth || '—' }}</div></div>
              <div class="rw-readonly-field"><label>Passport / ID no.</label><div>{{ student.passportId || '—' }}</div></div>
              <div class="rw-readonly-field"><label>Address</label><div>{{ student.address || '—' }}</div></div>
            </div>
          </div>
        </div>

        <!-- ═══════ Step 2: Degree ═══════ -->
        <div v-else-if="step === 2" class="rw-step-content">
          <div class="rw-three">
            <div class="rw-checklist-pane">
              <h4 class="rw-section-heading">What to verify</h4>
              <p class="rw-hint">Tick each item once you've confirmed it.</p>
              <label v-for="r in CHECKLIST.degree" :key="r.id" class="rw-check-item">
                <input type="checkbox" :checked="checklists.degree.has(r.id)" @change="toggleCheck('degree', r.id)" />
                <span :class="{ ticked: checklists.degree.has(r.id) }">{{ r.name }}</span>
              </label>
              <p v-if="!CHECKLIST.degree.length" class="rw-hint" style="margin-top:.4rem">No verification requirements configured for this document type.</p>

              <div v-if="!allChecked('degree') && CHECKLIST.degree.length" class="rw-reject-box">
                <div class="rw-reject-label">Rejection reason (required, min 10 characters)</div>
                <div class="rw-chip-row">
                  <button v-for="r in uncheckedRequirements('degree')" :key="r.id"
                          :class="['rw-reason-chip', { active: rejectChipsSelected.degree.has(reqLabelForChip(r)) }]"
                          @click="toggleRejectChip('degree', reqLabelForChip(r))">
                    {{ reqLabelForChip(r) }}
                  </button>
                </div>
                <textarea v-model="rejectText.degree" rows="3" placeholder="Add a specific reason or override the chips above…"></textarea>
                <div class="rw-reject-meta">
                  <span :class="{ ok: (rejectText.degree || '').trim().length >= 10 }">
                    {{ (rejectText.degree || '').trim().length }} chars
                  </span>
                </div>
              </div>

              <button v-if="!allChecked('degree') && CHECKLIST.degree.length"
                      class="rw-btn-reject-next" :disabled="!canReject('degree')" @click="rejectAndNext('degree')">
                ✕ Reject &amp; Next
              </button>
              <button v-if="allChecked('degree')"
                      class="rw-btn-approve-next" @click="approveAndNext('degree')">
                ✓ Approve &amp; Next
              </button>
            </div>
            <div class="rw-preview-pane">
              <h4 class="rw-section-heading">Degree certificate</h4>
              <div v-if="rejectionInfo('degree')" class="rw-reject-banner">
                <strong>Rejected by {{ rejectionInfo('degree').by }}</strong>
                <span v-if="rejectionInfo('degree').reason"> — {{ rejectionInfo('degree').reason }}</span>
              </div>
              <DocPreview :filename="student.docDegree" :student-id="student.studentGuid" :document-id="student.docIds?.degree" />
            </div>
            <div class="rw-data-pane">
              <h4 class="rw-section-heading">Academic background</h4>
              <p class="rw-hint">Confirm the certificate supports the declared qualification.</p>
              <div class="rw-readonly-field"><label>Highest degree</label><div>{{ student.highestDegree || '—' }}</div></div>
            </div>
          </div>
        </div>

        <!-- ═══════ Step 3: Language ═══════ -->
        <div v-else-if="step === 3" class="rw-step-content">
          <div class="rw-three">
            <div class="rw-checklist-pane">
              <h4 class="rw-section-heading">What to verify</h4>
              <p class="rw-hint">Tick each item once you've confirmed it.</p>
              <label v-for="r in CHECKLIST.language" :key="r.id" class="rw-check-item">
                <input type="checkbox" :checked="checklists.language.has(r.id)" @change="toggleCheck('language', r.id)" />
                <span :class="{ ticked: checklists.language.has(r.id) }">{{ r.name }}</span>
              </label>
              <p v-if="!CHECKLIST.language.length" class="rw-hint" style="margin-top:.4rem">No verification requirements configured for this document type.</p>

              <div v-if="!allChecked('language') && CHECKLIST.language.length" class="rw-reject-box">
                <div class="rw-reject-label">Rejection reason (required, min 10 characters)</div>
                <div class="rw-chip-row">
                  <button v-for="r in uncheckedRequirements('language')" :key="r.id"
                          :class="['rw-reason-chip', { active: rejectChipsSelected.language.has(reqLabelForChip(r)) }]"
                          @click="toggleRejectChip('language', reqLabelForChip(r))">
                    {{ reqLabelForChip(r) }}
                  </button>
                </div>
                <textarea v-model="rejectText.language" rows="3" placeholder="Add a specific reason or override the chips above…"></textarea>
                <div class="rw-reject-meta">
                  <span :class="{ ok: (rejectText.language || '').trim().length >= 10 }">
                    {{ (rejectText.language || '').trim().length }} chars
                  </span>
                </div>
              </div>

              <button v-if="!allChecked('language') && CHECKLIST.language.length"
                      class="rw-btn-reject-next" :disabled="!canReject('language')" @click="rejectAndNext('language')">
                ✕ Reject &amp; Next
              </button>
              <button v-if="allChecked('language')"
                      class="rw-btn-approve-next" @click="approveAndNext('language')">
                ✓ Approve &amp; Next
              </button>
            </div>
            <div class="rw-preview-pane">
              <h4 class="rw-section-heading">Language test result</h4>
              <div v-if="rejectionInfo('language')" class="rw-reject-banner">
                <strong>Rejected by {{ rejectionInfo('language').by }}</strong>
                <span v-if="rejectionInfo('language').reason"> — {{ rejectionInfo('language').reason }}</span>
              </div>
              <DocPreview :filename="student.docLanguage" :student-id="student.studentGuid" :document-id="student.docIds?.language" />
            </div>
            <div class="rw-data-pane">
              <h4 class="rw-section-heading">Language proficiency</h4>
              <p class="rw-hint">Confirm the certificate matches the declared score and is still valid.</p>
              <div class="rw-readonly-field"><label>Language result</label><div>{{ student.languageResult || '—' }}</div></div>
            </div>
          </div>
        </div>

        <!-- ═══════ Step 4: CV ═══════ -->
        <div v-else-if="step === 4" class="rw-step-content">
          <div class="rw-three">
            <div class="rw-checklist-pane">
              <h4 class="rw-section-heading">What to verify</h4>
              <p class="rw-hint">Tick each item once you've confirmed it.</p>
              <label v-for="r in CHECKLIST.cv" :key="r.id" class="rw-check-item">
                <input type="checkbox" :checked="checklists.cv.has(r.id)" @change="toggleCheck('cv', r.id)" />
                <span :class="{ ticked: checklists.cv.has(r.id) }">{{ r.name }}</span>
              </label>
              <p v-if="!CHECKLIST.cv.length" class="rw-hint" style="margin-top:.4rem">No verification requirements configured for this document type.</p>

              <div v-if="!allChecked('cv') && CHECKLIST.cv.length" class="rw-reject-box">
                <div class="rw-reject-label">Rejection reason (required, min 10 characters)</div>
                <div class="rw-chip-row">
                  <button v-for="r in uncheckedRequirements('cv')" :key="r.id"
                          :class="['rw-reason-chip', { active: rejectChipsSelected.cv.has(reqLabelForChip(r)) }]"
                          @click="toggleRejectChip('cv', reqLabelForChip(r))">
                    {{ reqLabelForChip(r) }}
                  </button>
                </div>
                <textarea v-model="rejectText.cv" rows="3" placeholder="Add a specific reason or override the chips above…"></textarea>
                <div class="rw-reject-meta">
                  <span :class="{ ok: (rejectText.cv || '').trim().length >= 10 }">
                    {{ (rejectText.cv || '').trim().length }} chars
                  </span>
                </div>
              </div>

              <button v-if="!allChecked('cv') && CHECKLIST.cv.length"
                      class="rw-btn-reject-next" :disabled="!canReject('cv')" @click="rejectAndNext('cv')">
                ✕ Reject &amp; Next
              </button>
              <button v-if="allChecked('cv')"
                      class="rw-btn-approve-next" @click="approveAndNext('cv')">
                ✓ Approve &amp; Next
              </button>
            </div>
            <div class="rw-preview-pane">
              <h4 class="rw-section-heading">CV / Résumé</h4>
              <div v-if="rejectionInfo('cv')" class="rw-reject-banner">
                <strong>Rejected by {{ rejectionInfo('cv').by }}</strong>
                <span v-if="rejectionInfo('cv').reason"> — {{ rejectionInfo('cv').reason }}</span>
              </div>
              <DocPreview :filename="student.docCV" :student-id="student.studentGuid" :document-id="student.docIds?.cv" />
            </div>
            <div class="rw-data-pane">
              <h4 class="rw-section-heading">Work experience</h4>
              <p class="rw-hint">Confirm the CV supports the declared experience.</p>
              <div class="rw-readonly-field"><label>Years of work experience</label><div>{{ student.yearsWorkExperience ?? '—' }}</div></div>
            </div>
          </div>
        </div>

        <!-- ═══════ Step 5: Programme ═══════ -->
        <div v-else-if="step === 5" class="rw-step-content">
          <h4 class="rw-section-heading">Programme selection</h4>
          <p class="rw-hint">Confirm the chosen programme and pathway suit the applicant's profile.</p>
          <div v-if="!enrollment" class="rw-empty">No enrolment found on this student.</div>
          <div v-else class="rw-prog-card">
            <div class="rw-readonly-field"><label>Programme</label><div>{{ enrollment.programme || '—' }}</div></div>
            <div class="rw-readonly-field"><label>Specialization</label><div>{{ enrollment.specialization || '—' }}</div></div>
            <div class="rw-readonly-field"><label>Mode of study</label><div>{{ enrollment.modeOfStudy || '—' }}</div></div>
            <div class="rw-readonly-field"><label>Pathway</label><div>{{ enrollment.selectedPathway || '—' }}</div></div>
          </div>
          <DecisionPanel sectionKey="programme" :draft="draft"
            :chips="REASON_CHIPS.programme" @approve="approve('programme')" @reject="reject('programme', $event)" />
        </div>

        <!-- ═══════ Step 6: Enrolment & Status ═══════ -->
        <div v-else-if="step === 6" class="rw-step-content">
          <h4 class="rw-section-heading">Enrolment &amp; status</h4>
          <p class="rw-hint">Set the commencement date and study duration for this enrolment.</p>
          <div class="rw-field"><label>Commencement date <span class="rw-req">*</span></label>
            <input type="date" v-model="enrolmentDraft.commencementDate" />
          </div>
          <div class="rw-field"><label>Duration (months) <span class="rw-req">*</span></label>
            <input type="number" :min="durationMin" :max="durationMax"
                   v-model.number="enrolmentDraft.durationMonths" />
            <span class="rw-note">
              Programme allows {{ durationMin }}–{{ durationMax }} months.
              Installment plan (next step) will be capped at {{ maxInstallmentMonths }} months.
            </span>
            <p v-if="durationError" class="rw-err">{{ durationError }}</p>
          </div>
        </div>

        <!-- ═══════ Step 7: Contract confirmation ═══════ -->
        <div v-else-if="step === 7" class="rw-step-content">
          <h4 class="rw-section-heading">Final review &amp; contract attestation</h4>
          <p class="rw-hint">Review every decision below and confirm against the IBSS partnership contract.</p>
          <div class="rw-summary">
            <div v-for="s in STEPS.slice(0, 5)" :key="s.key" class="rw-summary-row">
              <span class="rw-summary-label">{{ s.label }}</span>
              <span :class="['rw-decision-chip', 'dc-' + draft[s.key].status]">
                {{ decisionLabel(draft[s.key].status) }}
              </span>
              <span v-if="draft[s.key].status === 'rejected'" class="rw-summary-reason">
                {{ draft[s.key].reason }}
              </span>
            </div>
            <div class="rw-summary-row">
              <span class="rw-summary-label">Commencement</span>
              <span>{{ enrolmentDraft.commencementDate || '—' }} · {{ enrolmentDraft.durationMonths || '—' }} months</span>
            </div>
          </div>
          <label class="rw-attest">
            <input type="checkbox" v-model="contractAccepted" />
            <span>I confirm this application complies with the terms of the IBSS partnership agreement, the data has been verified against the submitted documents, and I am authorised to act on behalf of the partner institution.</span>
          </label>
          <div v-if="validationErrors.length" class="rw-err-list">
            <div class="rw-err-title">Cannot submit yet:</div>
            <ul><li v-for="e in validationErrors" :key="e">{{ e }}</li></ul>
          </div>
        </div>
      </div>

      <!-- Footer -->
      <div class="rw-footer">
        <button class="rw-btn-ghost" @click="$emit('close')">Cancel</button>
        <div class="rw-nav-right">
          <span v-if="submitError" class="rw-submit-err">{{ submitError }}</span>
          <button v-if="step > 1" class="rw-btn-back" @click="step = step - 1">← Back</button>
          <button v-if="step < STEPS.length" class="rw-btn-next" :disabled="!canAdvance" @click="step = step + 1">Next →</button>
          <button v-else
                  :class="['rw-btn-submit', anyRejected ? 'rw-btn-submit-reject' : 'rw-btn-submit-approve']"
                  :disabled="!canSubmit || submitting" @click="submit">
            {{ submitting ? 'Submitting…' : (anyRejected ? 'Submit & Reject' : 'Submit & Approve') }}
          </button>
        </div>
      </div>
    </div>
  </transition>
</template>

<script setup>
import { ref, reactive, computed } from 'vue'
import DocPreview from './DocPreview.vue'
import DecisionPanel from './DecisionPanel.vue'
import { markReviewComplete } from '../../store/partnerReviewState.js'
import api from '../../api/client.js'

const props = defineProps({
  student: { type: Object, required: true },
  // Endpoint to POST the review to. Defaults to the partner path; the admin
  // students tab can pass the admin-side equivalent when admin steps into
  // the partner queue.
  reviewEndpoint: { type: Function, default: (studentGuid, enrollmentId) =>
    `/v1/partner/my-students/${studentGuid}/enrollments/${enrollmentId}/review` },
})
const emit = defineEmits(['close', 'submitted'])

const STEPS = [
  { key: 'passport',  label: 'Passport' },
  { key: 'degree',    label: 'Degree' },
  { key: 'language',  label: 'Language' },
  { key: 'cv',        label: 'CV' },
  { key: 'programme', label: 'Programme' },
  { key: 'enrolment', label: 'Enrolment' },
  // Payment step removed — out of scope for this round. Tuition + plan
  // can be wired back in once payments are an actual flow.
  { key: 'confirm',   label: 'Confirm' },
]

// Programme step has no DocumentType — its reject reasons stay hardcoded
// here. Document steps source their reasons from the requirements list
// each document carries (see CHECKLIST below).
const REASON_CHIPS = {
  programme: ['Mismatched qualifications', 'Prerequisite missing'],
}

// Per-document checklist comes from the API: each document carries an array
// of `{ id, name, rejectionLabel }` requirements (DocumentTypeVerifyRequirement
// rows). CHECKLIST is a computed view keyed on the per-doc-step keys.
const CHECKLIST = computed(() => ({
  passport: props.student.docMeta?.passport?.requirements ?? [],
  degree:   props.student.docMeta?.degree?.requirements ?? [],
  language: props.student.docMeta?.language?.requirements ?? [],
  cv:       props.student.docMeta?.cv?.requirements ?? [],
}))

// Canonical document labels used in API payload (also shown in the rejection
// note appended to the enrolment).
const DOC_LABEL = {
  passport: 'Passport / ID',
  degree:   'Degree certificate',
  language: 'Language test result',
  cv:       'CV / Résumé',
}

const step = ref(1)

const draft = reactive(JSON.parse(JSON.stringify(props.student.partnerReview ?? {
  passport: { status: 'pending', reason: '' },
  degree:   { status: 'pending', reason: '' },
  language: { status: 'pending', reason: '' },
  cv:       { status: 'pending', reason: '' },
  programme:{ status: 'pending', reason: '' },
})))

// Per-doc checklist state, keyed on requirement Guid so renaming a
// requirement label doesn't desync the local set. Pre-tick everything for
// a doc that is already approved so a returning reviewer can re-Approve
// without re-ticking.
const checklists = reactive({
  passport: new Set(),
  degree:   new Set(),
  language: new Set(),
  cv:       new Set(),
})
for (const key of ['passport','degree','language','cv']) {
  if (draft[key]?.status === 'approved') {
    for (const r of CHECKLIST.value[key]) checklists[key].add(r.id)
  }
}

// Reject-box state per doc step: chips are pre-selected from unchecked
// requirements; textarea text is the canonical reason that gets submitted.
// `rejectChips[key]` mirrors which chips are currently in `rejectText[key]`,
// for the toggle-chip behaviour.
const rejectText = reactive({ passport: '', degree: '', language: '', cv: '' })
const rejectChipsSelected = reactive({
  passport: new Set(), degree: new Set(), language: new Set(), cv: new Set(),
})

function reqLabelForChip(req) { return req.rejectionLabel || req.name }

/// Whenever the checklist for `key` changes (and the section's draft isn't
/// committed yet), re-seed the reject box from the unchecked items so the
/// chips reflect the current state. Skips if the user has already typed
/// into the textarea OR explicitly toggled chips.
function syncRejectBox(key) {
  if (draft[key]?.status === 'rejected') return
  const unchecked = CHECKLIST.value[key].filter(r => !checklists[key].has(r.id))
  const labels = unchecked.map(reqLabelForChip)
  rejectChipsSelected[key] = new Set(labels)
  rejectText[key] = labels.join('; ')
}

function toggleCheck(key, requirementId) {
  const s = checklists[key]
  if (s.has(requirementId)) s.delete(requirementId)
  else s.add(requirementId)
  // Editing the checklist invalidates a prior decision so the reviewer must
  // explicitly Approve / Reject again.
  if (draft[key]?.status !== 'pending') {
    draft[key].status = 'pending'
    draft[key].reason = ''
  }
  syncRejectBox(key)
}

function toggleRejectChip(key, label) {
  const sel = rejectChipsSelected[key]
  if (sel.has(label)) {
    sel.delete(label)
    // Strip the label from the textarea's chip-derived part.
    rejectText[key] = rebuildRejectText(key)
  } else {
    sel.add(label)
    rejectText[key] = rebuildRejectText(key)
  }
}

function rebuildRejectText(key) {
  // Selected chips, joined; preserve whatever free text the user typed in
  // addition (anything in the textarea that isn't a recognised chip label).
  const selected = [...rejectChipsSelected[key]]
  const known = new Set(CHECKLIST.value[key].map(reqLabelForChip))
  const freeBits = (rejectText[key] || '')
    .split(/;\s*/)
    .map(s => s.trim())
    .filter(s => s && !known.has(s) && !selected.includes(s))
  return [...selected, ...freeBits].filter(Boolean).join('; ')
}

function allChecked(key) {
  const items = CHECKLIST.value[key]
  return items.length > 0 && items.every(r => checklists[key].has(r.id))
}
function uncheckedRequirements(key) {
  return CHECKLIST.value[key].filter(r => !checklists[key].has(r.id))
}
function canReject(key) {
  return !allChecked(key) && (rejectText[key] || '').trim().length >= 10
}

const STEP_KEYS = ['passport', 'degree', 'language', 'cv']

/// Returns { by, reason } when the document for `key` is currently in a
/// Rejected* state (per the backend's CurrentStatusId), otherwise null.
function rejectionInfo(key) {
  const m = props.student.docMeta?.[key]
  if (!m) return null
  if (m.status !== 'RejectedByPartner' && m.status !== 'RejectedByEnrolment') return null
  return {
    by: m.lastChangedByName || (m.status === 'RejectedByEnrolment' ? 'Admission Office' : 'Partner'),
    reason: m.lastChangeReason || '',
  }
}

function rejectAndNext(key) {
  if (!canReject(key)) return
  draft[key].status = 'rejected'
  draft[key].reason = (rejectText[key] || '').trim()
  advanceFrom(key)
}

function advanceFrom(key) {
  const idx = STEP_KEYS.indexOf(key)
  for (let n = idx + 1; n < STEP_KEYS.length; n++) {
    if (draft[STEP_KEYS[n]].status === 'pending') {
      step.value = n + 1
      return
    }
  }
  step.value = 5
}

function approveAndNext(key) {
  if (!allChecked(key)) return
  draft[key].status = 'approved'
  draft[key].reason = ''
  // Find next pending document step; fall back to step 5 (Programme) if all
  // four document steps are decided.
  const idx = STEP_KEYS.indexOf(key)
  for (let n = idx + 1; n < STEP_KEYS.length; n++) {
    if (draft[STEP_KEYS[n]].status === 'pending') {
      step.value = n + 1
      return
    }
  }
  step.value = 5
}

const enrollment = computed(() => props.student.enrollments?.[0] ?? null)

// Duration is partner-set within the programme's min/max range. The draft
// pre-fills with the existing approvedDurationMonths (if any) falling back
// to the specialisation's default. Total tuition still comes from the
// specialisation (server-owned).
const enrolmentDraft = reactive({
  commencementDate: enrollment.value?.commencementDate?.slice(0, 10) ?? '',
  durationMonths: enrollment.value?.durationMonths ?? 0,
})

const durationMin = computed(() => enrollment.value?.programmeMinDurationMonths || 1)
const durationMax = computed(() => enrollment.value?.programmeMaxDurationMonths || 999)
const durationError = computed(() => {
  const v = enrolmentDraft.durationMonths
  if (!v) return ''
  if (v < durationMin.value) return `Minimum is ${durationMin.value} months for this programme.`
  if (v > durationMax.value) return `Maximum is ${durationMax.value} months for this programme.`
  return ''
})

const paymentDraft = reactive({
  type: enrollment.value?.paymentPlan?.type ?? '',
  months: enrollment.value?.paymentPlan?.months ?? null,
  tuitionFee: enrollment.value?.tuitionFeeUsd ?? 0,
})

const contractAccepted = ref(false)

// ── Derived validation ───────────────────────────────────────────────────────
const maxInstallmentMonths = computed(() =>
  Math.max(2, (enrolmentDraft.durationMonths || 0) - 2)
)

const monthlyAmount = computed(() => {
  if (paymentDraft.type !== 'installments') return 0
  const m = paymentDraft.months || 0
  if (m <= 0) return 0
  return (paymentDraft.tuitionFee || 0) / m
})
const monthlyAmountDisplay = computed(() =>
  monthlyAmount.value ? monthlyAmount.value.toFixed(2) : '—'
)

const installmentError = computed(() => {
  if (paymentDraft.type !== 'installments') return ''
  const m = paymentDraft.months || 0
  if (m < 2) return 'At least 2 installments required.'
  if (m > maxInstallmentMonths.value) return `Maximum ${maxInstallmentMonths.value} months (duration − 2).`
  return ''
})

function canAdvanceFromStep(n) {
  if (n >= 1 && n <= 5) {
    const key = STEPS[n - 1].key
    return draft[key].status !== 'pending'
  }
  if (n === 6) return !!enrolmentDraft.commencementDate
    && enrolmentDraft.durationMonths >= durationMin.value
    && enrolmentDraft.durationMonths <= durationMax.value
  return true
}
const canAdvance = computed(() => canAdvanceFromStep(step.value))

const validationErrors = computed(() => {
  const errs = []
  for (const s of STEPS.slice(0, 5)) {
    if (draft[s.key].status === 'pending') errs.push(`${s.label}: no decision recorded.`)
    if (draft[s.key].status === 'rejected' && (draft[s.key].reason?.trim().length ?? 0) < 10)
      errs.push(`${s.label}: rejection reason must be at least 10 characters.`)
  }
  if (!enrolmentDraft.commencementDate) errs.push('Commencement date is required.')
  if (!enrolmentDraft.durationMonths || enrolmentDraft.durationMonths < 3)
    errs.push('Specialization is missing a study duration — fix it on the specialization before reviewing.')
  if (!contractAccepted.value) errs.push('You must attest to the IBSS partnership contract.')
  return errs
})
const canSubmit = computed(() => validationErrors.value.length === 0)

const anyRejected = computed(() =>
  ['passport', 'degree', 'language', 'cv', 'programme']
    .some(k => draft[k]?.status === 'rejected')
)

// ── Decisions ────────────────────────────────────────────────────────────────
function approve(key) {
  draft[key].status = 'approved'
  draft[key].reason = ''
}
function reject(key, reason) {
  draft[key].status = 'rejected'
  draft[key].reason = reason
}

function stepClass(n) {
  if (n === step.value) return 'rw-step-current'
  if (n < step.value) {
    if (n <= 5) {
      const st = draft[STEPS[n - 1].key].status
      if (st === 'approved') return 'rw-step-ok'
      if (st === 'rejected') return 'rw-step-bad'
    }
    return 'rw-step-done'
  }
  return 'rw-step-pending'
}
function stepIcon(n, key) {
  if (n <= 5) {
    const st = draft[key]?.status
    if (st === 'approved') return '✓'
    if (st === 'rejected') return '✕'
  }
  if (n < step.value) return '●'
  return String(n)
}
function chipTitle(key) {
  if (!draft[key]) return ''
  if (draft[key].status === 'rejected' && draft[key].reason) return `Rejected: ${draft[key].reason}`
  return ''
}
function goStep(n) { step.value = n }
function decisionLabel(st) {
  if (st === 'approved') return 'Approved'
  if (st === 'rejected') return 'Rejected'
  return 'Pending'
}

const submitting = ref(false)
const submitError = ref('')

async function submit() {
  if (!canSubmit.value || submitting.value) return
  submitError.value = ''
  submitting.value = true

  const s = props.student
  const e = enrollment.value
  if (!s.studentGuid || !e?.id) {
    submitError.value = 'Missing student or enrolment id — close and re-open the review.'
    submitting.value = false
    return
  }

  // Map per-doc decisions to backend payload, attaching the StudentDocument
  // GUID resolved by document type. Documents that have no upload are
  // skipped silently — the backend will simply not stamp anything.
  const documents = ['passport','degree','language','cv']
    .filter(key => s.docIds?.[key])
    .map(key => ({
      studentDocumentId: s.docIds[key],
      documentLabel: DOC_LABEL[key],
      decision: draft[key].status,
      reasons: draft[key].status === 'rejected' && draft[key].reason
        ? [draft[key].reason]
        : [],
      freeTextReason: null,
    }))

  try {
    await api.post(
      props.reviewEndpoint(s.studentGuid, e.id),
      {
        documents,
        enrolment: {
          commencementDate: enrolmentDraft.commencementDate || null,
          durationMonths: enrolmentDraft.durationMonths || null,
        },
        // Payment block deliberately omitted — payments aren't a flow yet.
      }
    )
  } catch (err) {
    submitError.value = err.response?.data?.error ?? err.message ?? 'Failed to submit review'
    submitting.value = false
    return
  }

  // Mirror local mock-state expectations for the rest of the partner UI.
  const now = new Date().toISOString()
  for (const key of ['passport','degree','language','cv','programme']) {
    s.partnerReview[key] = { ...draft[key] }
  }
  s.partnerReview.completedAt = now
  s.docsVerified = s.docsVerified ?? {}
  s.docsVerified.passport = draft.passport.status === 'approved'
  s.docsVerified.degree   = draft.degree.status === 'approved'
  s.docsVerified.language = draft.language.status === 'approved'
  s.docsVerified.cv       = draft.cv.status === 'approved'
  if (e) {
    e.commencementDate = enrolmentDraft.commencementDate
    e.durationMonths = enrolmentDraft.durationMonths
  }
  markReviewComplete(s.studentId)
  submitting.value = false
  emit('submitted', s)
  emit('close')
}
</script>

<style scoped>
.rw-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.45); z-index: 400; }
.rw-modal { position: fixed; top: 2vh; left: 50%; transform: translateX(-50%); width: 1080px; max-width: 96vw; height: 96vh; background: #fff; border-radius: 10px; z-index: 401; display: flex; flex-direction: column; box-shadow: 0 12px 40px rgba(0,0,0,0.25); overflow: hidden; }

.rw-header { display: flex; justify-content: space-between; align-items: flex-start; padding: 1rem 1.25rem; border-bottom: 1.5px solid #e8edf4; }
.rw-header h2 { margin: 0; color: #003366; font-size: 1.05rem; }
.rw-sub { margin: 0.2rem 0 0; color: #888; font-size: 0.82rem; }
.rw-close { background: none; border: none; font-size: 1.2rem; color: #888; cursor: pointer; }

.rw-stepbar { display: flex; gap: 0.3rem; padding: 0.65rem 1.25rem; background: #f7f9fb; border-bottom: 1px solid #e8edf4; overflow-x: auto; }
.rw-step-chip { display: flex; align-items: center; gap: 0.4rem; padding: 0.35rem 0.65rem; border-radius: 18px; background: #eef2f7; color: #5f6e85; font-size: 0.78rem; white-space: nowrap; }
.rw-step-num { font-weight: 700; }
.rw-step-current { background: #003366; color: #fff; }
.rw-step-ok { background: #d1fae5; color: #065f46; cursor: pointer; }
.rw-step-bad { background: #fee2e2; color: #991b1b; cursor: pointer; }
.rw-step-done { background: #e8f0f8; color: #003366; cursor: pointer; }
.rw-step-pending { background: #eef2f7; color: #999; }

.rw-body { flex: 1; overflow-y: auto; padding: 1.25rem 1.5rem; }
.rw-step-content { display: flex; flex-direction: column; gap: 0.85rem; }
.rw-section-heading { font-size: 0.82rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.05em; color: #003366; margin: 0 0 0.35rem; border-bottom: 1.5px solid #e8edf4; padding-bottom: 0.35rem; }
.rw-hint { font-size: 0.85rem; color: #666; margin: 0 0 0.6rem; }

.rw-split { display: grid; grid-template-columns: 1.3fr 1fr; gap: 1.25rem; min-height: 500px; }
.rw-three { display: grid; grid-template-columns: 0.85fr 1.4fr 1fr; gap: 1rem; min-height: 500px; }
.rw-preview-pane { display: flex; flex-direction: column; min-width: 0; }
.rw-data-pane { display: flex; flex-direction: column; }
.rw-checklist-pane { display: flex; flex-direction: column; gap: 0.4rem; padding-right: 0.4rem; }
.rw-check-item { display: flex; align-items: flex-start; gap: 0.5rem; font-size: 0.86rem; line-height: 1.35; padding: 0.35rem 0; cursor: pointer; }
.rw-check-item input { margin-top: 3px; flex-shrink: 0; }
.rw-check-item span.ticked { color: #065f46; text-decoration: line-through; text-decoration-color: rgba(6,95,70,0.4); }
.rw-btn-approve-next { margin-top: auto; background: #0d6b55; color: #fff; border: none; border-radius: 6px; padding: 0.6rem 0.9rem; font-size: 0.88rem; font-weight: 700; cursor: pointer; }
.rw-btn-approve-next:hover { background: #0a5a47; }
.rw-btn-approve-next:disabled { background: #b8c2cc; cursor: default; }
.rw-btn-reject-next { margin-top: 0.4rem; background: #b91c1c; color: #fff; border: none; border-radius: 6px; padding: 0.55rem 0.85rem; font-size: 0.85rem; font-weight: 700; cursor: pointer; }
.rw-btn-reject-next:hover { background: #991b1b; }
.rw-btn-reject-next:disabled { background: #f4a8a8; cursor: default; }
.rw-reject-box { margin-top: auto; background: #fff5f5; border: 1px solid #fca5a5; border-radius: 8px; padding: 0.65rem 0.75rem; display: flex; flex-direction: column; gap: 0.45rem; }
.rw-reject-box .rw-reject-label { font-size: 0.78rem; font-weight: 700; color: #991b1b; text-transform: uppercase; letter-spacing: 0.04em; }
.rw-reject-box .rw-chip-row { display: flex; flex-wrap: wrap; gap: 0.3rem; }
.rw-reject-box .rw-reason-chip { background: #fee2e2; color: #7f1d1d; border: 1px solid #fca5a5; border-radius: 14px; padding: 0.18rem 0.65rem; font-size: 0.78rem; cursor: pointer; }
.rw-reject-box .rw-reason-chip.active { background: #b91c1c; color: #fff; border-color: #b91c1c; }
.rw-reject-box textarea { width: 100%; border: 1.5px solid #fca5a5; border-radius: 6px; padding: 0.4rem 0.55rem; font-size: 0.84rem; font-family: inherit; resize: vertical; }
.rw-reject-box textarea:focus { outline: none; border-color: #b91c1c; }
.rw-reject-meta { display: flex; justify-content: flex-end; font-size: 0.74rem; color: #b91c1c; }
.rw-reject-meta .ok { color: #065f46; }
.rw-submit-err { color: #991b1b; font-size: 0.82rem; padding-right: 0.65rem; }
.rw-reject-banner { background: #fef2f2; border: 1px solid #fca5a5; color: #991b1b; padding: 0.45rem 0.65rem; border-radius: 6px; font-size: 0.82rem; margin-bottom: 0.5rem; }

.rw-readonly-field { margin-bottom: 0.65rem; }
.rw-readonly-field label { font-size: 0.7rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.04em; color: #666; display: block; margin-bottom: 0.15rem; }
.rw-readonly-field > div { font-size: 0.92rem; color: #222; padding: 0.45rem 0.65rem; background: #f7f9fb; border: 1px solid #e8edf4; border-radius: 5px; }

.rw-field { display: flex; flex-direction: column; gap: 0.3rem; margin-bottom: 0.75rem; }
.rw-field-narrow { max-width: 260px; }
.rw-field label { font-size: 0.82rem; font-weight: 600; color: #444; }
.rw-field input { padding: 0.55rem 0.7rem; border: 1.5px solid #ccc; border-radius: 6px; font-size: 0.88rem; }
.rw-field input:focus { outline: none; border-color: #003366; }
.rw-note { font-size: 0.75rem; color: #888; font-style: italic; }
.rw-req { color: #e53e3e; }

.rw-decision-chip { font-size: 0.72rem; font-weight: 700; padding: 2px 9px; border-radius: 14px; }
.dc-pending  { background: #eef2f7; color: #888; }
.dc-approved { background: #d1fae5; color: #065f46; }
.dc-rejected { background: #fee2e2; color: #991b1b; }

.rw-prog-card { display: grid; grid-template-columns: 1fr 1fr; gap: 0.75rem; padding: 0.75rem; background: #f7f9fb; border: 1px solid #e8edf4; border-radius: 7px; }

.rw-plan-list { display: flex; flex-direction: column; gap: 0.75rem; }
.rw-plan-item { display: flex; gap: 0.75rem; padding: 0.9rem 1rem; border: 1.5px solid #d0d7e0; border-radius: 8px; cursor: pointer; background: #fff; }
.rw-plan-item.active { border-color: #003366; background: #f0f6ff; }
.rw-plan-item input { margin-top: 3px; }
.rw-plan-desc { margin: 0.2rem 0 0; font-size: 0.8rem; color: #666; }
.rw-installment-config { margin-top: 0.65rem; display: flex; flex-direction: column; gap: 0.55rem; }
.rw-monthly-display { display: flex; align-items: center; gap: 0.6rem; padding: 0.55rem 0.75rem; background: #e8f0f8; border-radius: 6px; }
.rw-monthly-lbl { font-size: 0.78rem; color: #003366; font-weight: 600; }
.rw-monthly-amt { color: #003366; font-size: 1rem; }
.rw-err { color: #991b1b; font-size: 0.8rem; margin: 0; }

.rw-summary { background: #f7f9fb; border: 1px solid #e8edf4; border-radius: 7px; padding: 0.75rem 1rem; margin-bottom: 0.9rem; }
.rw-summary-row { display: flex; align-items: flex-start; gap: 0.6rem; padding: 0.35rem 0; border-bottom: 1px dashed #e8edf4; font-size: 0.87rem; flex-wrap: wrap; }
.rw-summary-row:last-child { border-bottom: none; }
.rw-summary-label { font-weight: 600; color: #444; min-width: 140px; }
.rw-summary-reason { color: #991b1b; font-size: 0.8rem; font-style: italic; width: 100%; padding-left: 140px; }

.rw-attest { display: flex; align-items: flex-start; gap: 0.6rem; padding: 0.9rem 1rem; background: #fffbeb; border: 1.5px solid #fde68a; border-radius: 8px; font-size: 0.88rem; line-height: 1.5; color: #333; }
.rw-attest input { margin-top: 3px; flex-shrink: 0; }

.rw-err-list { margin-top: 0.75rem; background: #fef2f2; border: 1px solid #fca5a5; border-radius: 7px; padding: 0.6rem 0.85rem; }
.rw-err-title { font-weight: 700; color: #991b1b; font-size: 0.85rem; margin-bottom: 0.35rem; }
.rw-err-list ul { margin: 0; padding-left: 1.1rem; color: #991b1b; font-size: 0.82rem; }

.rw-empty { padding: 2rem; text-align: center; color: #888; }

.rw-footer { display: flex; justify-content: space-between; align-items: center; padding: 0.85rem 1.25rem; border-top: 1.5px solid #e8edf4; background: #fafbfc; }
.rw-nav-right { display: flex; gap: 0.55rem; }
.rw-btn-ghost { background: #fff; border: 1.5px solid #ccc; border-radius: 6px; padding: 0.55rem 1.1rem; font-size: 0.86rem; cursor: pointer; color: #555; }
.rw-btn-back { background: #fff; border: 1.5px solid #003366; color: #003366; border-radius: 6px; padding: 0.55rem 1.1rem; font-size: 0.86rem; font-weight: 600; cursor: pointer; }
.rw-btn-next, .rw-btn-submit { background: #003366; border: none; color: #fff; border-radius: 6px; padding: 0.55rem 1.3rem; font-size: 0.86rem; font-weight: 600; cursor: pointer; }
.rw-btn-next:hover, .rw-btn-submit:hover { background: #0055a5; }
.rw-btn-next:disabled, .rw-btn-submit:disabled { opacity: 0.45; cursor: default; background: #003366; }
.rw-btn-submit { background: #0d6b55; }
.rw-btn-submit:hover { background: #0a5a47; }
.rw-btn-submit-approve { background: #0d6b55; }
.rw-btn-submit-approve:hover { background: #0a5a47; }
.rw-btn-submit-reject { background: #b91c1c; }
.rw-btn-submit-reject:hover { background: #991b1b; }

.fade-enter-active, .fade-leave-active { transition: opacity 0.18s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
.slide-enter-active, .slide-leave-active { transition: transform 0.2s ease, opacity 0.2s; }
.slide-enter-from, .slide-leave-to { transform: translate(-50%, -12px); opacity: 0; }
</style>
