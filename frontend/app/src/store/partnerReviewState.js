import { reactive } from 'vue'

// Frontend-only store for partner review flow state that the backend does not
// currently persist. Resets on page refresh.
//
// - completed: set of studentNumbers whose partner review wizard has been
//   submitted. Flips the status badge from "Applying" -> "Potential Applicant".
// - subStatus: sub-status on Active enrollments ({ type, reason, setAt }).
//   Keyed by `${studentNumber}:${studentEnrollmentId}`.

export const partnerReviewState = reactive({
  completed: new Set(),
  subStatus: {},
})

export function markReviewComplete(studentNumber) {
  partnerReviewState.completed.add(studentNumber)
}

export function isReviewComplete(studentNumber) {
  return partnerReviewState.completed.has(studentNumber)
}

export function subStatusKey(studentNumber, enrollmentId) {
  return `${studentNumber}:${enrollmentId}`
}

export function setSubStatus(studentNumber, enrollmentId, type, reason) {
  partnerReviewState.subStatus[subStatusKey(studentNumber, enrollmentId)] = {
    type, reason: reason ?? '', setAt: new Date().toISOString(),
  }
}

export function getSubStatus(studentNumber, enrollmentId) {
  return partnerReviewState.subStatus[subStatusKey(studentNumber, enrollmentId)] ?? null
}

export const SUB_STATUS_OPTIONS = [
  { type: 'grade',       label: 'Grade',       requiresReason: false, color: 'blue'   },
  { type: 'cancel',      label: 'Cancel',      requiresReason: true,  color: 'gray'   },
  { type: 'dismissed',   label: 'Dismissed',   requiresReason: true,  color: 'red'    },
  { type: 'dropout',     label: 'Drop out',    requiresReason: true,  color: 'red'    },
  { type: 'deferred',    label: 'Deferred',    requiresReason: true,  color: 'amber'  },
  { type: 'transferred', label: 'Transferred', requiresReason: true,  color: 'purple' },
]

export function subStatusLabel(type) {
  return SUB_STATUS_OPTIONS.find(o => o.type === type)?.label ?? type
}
