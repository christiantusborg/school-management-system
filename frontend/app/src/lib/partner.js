// Resolves the partner slug for the current page.
// Production prefers the subdomain (`<slug>.ibss.com`); falls back to ?partner=<slug>
// for local dev (localhost) or shareable links from email.
const SUBDOMAIN_RE = /^([a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\.ibss\.com$/i
const SLUG_RE = /^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$/

export function resolvePartnerSlug(loc = window.location) {
  const host = (loc.host || '').toLowerCase()
  const m = host.match(SUBDOMAIN_RE)
  if (m) return m[1]
  // Look at search and hash, in case the slug is in a Vue Router hash query.
  const search = new URLSearchParams(loc.search || '')
  const fromQuery = (search.get('partner') || '').trim().toLowerCase()
  if (SLUG_RE.test(fromQuery)) return fromQuery
  const hash = loc.hash || ''
  const qIndex = hash.indexOf('?')
  if (qIndex >= 0) {
    const hashSearch = new URLSearchParams(hash.slice(qIndex + 1))
    const fromHash = (hashSearch.get('partner') || '').trim().toLowerCase()
    if (SLUG_RE.test(fromHash)) return fromHash
  }
  return null
}
