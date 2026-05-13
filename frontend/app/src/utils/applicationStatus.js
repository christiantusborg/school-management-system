// Single source of truth for translating an EnrollmentStatus.Code into
// a label, a tone (used as the badge colour class), and whether the
// status calls for the student to act. Mirrors the codes seeded by
// EnrollmentStatusIds.cs on the backend.

const MAP = {
  Draft:                                { label: 'Draft',          tone: 'grey'  },
  ApplicationSubmitted:                 { label: 'Submitted',      tone: 'amber' },
  ApplicationAwaitingReviewByPartner:   { label: 'Under review',   tone: 'amber' },
  ApplicationAwaitingReviewByAdmission: { label: 'Under review',   tone: 'amber' },
  ApplicationRejectedByPartner:         { label: 'Action required', tone: 'red'  },
  ApplicationRejectedByAdmission:       { label: 'Action required', tone: 'red'  },
  AcceptOffer:                          { label: 'Offer ready',    tone: 'blue'  },
  ApplicationApprovedAdmission:         { label: 'Approved',       tone: 'green' },
  AcceptAdmission:                      { label: 'Accept admission', tone: 'blue' },
  AwaitingGradesSubmit:                 { label: 'Awaiting grades', tone: 'amber' },
  AwaitingGradesApproval:               { label: 'Awaiting grades', tone: 'amber' },
  GradesApproved:                       { label: 'Completed',      tone: 'green' },
}

export function statusBadge(code) {
  return MAP[code] ?? { label: code ?? 'Unknown', tone: 'grey' }
}

export function isReviewing(code) {
  return code === 'ApplicationSubmitted'
    || code === 'ApplicationAwaitingReviewByPartner'
    || code === 'ApplicationAwaitingReviewByAdmission'
}

// Splits a "reasons — free text" rejection note into its parts. The
// backend builds notes as: chip1, chip2, chip3 — free text. Either side
// may be empty.
export function parseRejectionNote(note) {
  if (!note) return { reasons: [], freeText: '' }
  const idx = note.indexOf(' — ')
  if (idx === -1) {
    // No em-dash separator — treat the whole thing as free text unless
    // it looks like a comma-list of chips.
    return note.includes(',')
      ? { reasons: note.split(',').map(s => s.trim()).filter(Boolean), freeText: '' }
      : { reasons: [], freeText: note }
  }
  const reasons = note.slice(0, idx).split(',').map(s => s.trim()).filter(Boolean)
  const freeText = note.slice(idx + 3).trim()
  return { reasons, freeText }
}
