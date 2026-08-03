// Study-duration helpers — the DB stores a VALUE plus a UNIT ('Month' or
// 'Day'), preserving what was entered. Conversion to days happens only when
// comparing against the programme's month range, using the real calendar
// from the commencement date (30.44-day average without one).
export const AVG_MONTH_DAYS = 30.44

export function monthsToDays(commencement, months) {
  const m = Number(months)
  if (!m || m < 1) return null
  if (commencement) {
    const c = new Date(String(commencement).slice(0, 10))
    const end = new Date(c)
    end.setMonth(end.getMonth() + m)
    return Math.round((end - c) / 86400000)
  }
  return Math.round(m * AVG_MONTH_DAYS)
}

export function isDayUnit(unit) {
  return String(unit || '').toLowerCase().startsWith('day')
}

/** Day equivalent of a stored (value, unit) pair; null when no value. */
export function durationToDays(commencement, value, unit) {
  const v = Number(value)
  if (!v || v < 1) return null
  return isDayUnit(unit) ? Math.round(v) : monthsToDays(commencement, Math.round(v))
}

export function displayDuration(value, unit) {
  const v = Number(value)
  if (!v) return ''
  return isDayUnit(unit)
    ? `${v} day${v === 1 ? '' : 's'}`
    : `${v} month${v === 1 ? '' : 's'}`
}
