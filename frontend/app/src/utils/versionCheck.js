import { ref } from 'vue'

// The build id baked into THIS bundle at build time (see vite.config.js).
const CURRENT = typeof __BUILD_ID__ !== 'undefined' ? __BUILD_ID__ : 0

// True once a newer build has been detected on the server.
export const updateAvailable = ref(false)
let deployedId = null

// Fetch the deployed build id, defeating every cache layer with a no-store
// fetch plus a unique query string (the edge proxy we don't control may ignore
// headers but won't merge distinct query strings).
async function fetchDeployedId() {
  try {
    const res = await fetch(`/version.json?ts=${Date.now()}`, { cache: 'no-store' })
    if (!res.ok) return null
    const data = await res.json()
    return typeof data.buildId === 'number' ? data.buildId : null
  } catch {
    return null
  }
}

// Reload onto the freshest index.html. A query param on the document URL busts
// any cached HTML; a sessionStorage guard prevents a reload loop if the server
// still serves a stale build (then we just surface the banner instead).
function reloadToLatest(id) {
  const guardKey = 'vc-reloaded'
  if (sessionStorage.getItem(guardKey) === String(id)) {
    updateAvailable.value = true // already tried; let the user reload manually
    return
  }
  sessionStorage.setItem(guardKey, String(id))
  const url = `${location.pathname}?v=${id}${location.hash}`
  location.replace(url)
}

export async function manualReload() {
  const id = deployedId ?? (await fetchDeployedId()) ?? Date.now()
  // Force the reload regardless of the guard when the user clicks.
  sessionStorage.removeItem('vc-reloaded')
  reloadToLatest(id)
}

// Compare deployed vs running. `auto` reloads immediately (used on the very
// first check at page load); otherwise it just raises the banner so a
// mid-session user isn't interrupted while typing.
async function check(auto) {
  const id = await fetchDeployedId()
  if (!id || id === CURRENT) return
  deployedId = id
  if (auto) reloadToLatest(id)
  else updateAvailable.value = true
}

export function startVersionWatch() {
  if (!CURRENT) return // dev / no build id — nothing to compare
  // On first load, silently jump to the latest build if one is already out.
  check(true)
  // Mid-session: re-check when the tab regains focus and on a slow interval,
  // surfacing a banner rather than yanking the page.
  window.addEventListener('focus', () => check(false))
  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'visible') check(false)
  })
  setInterval(() => check(false), 5 * 60 * 1000)
}
