import axios from 'axios'

// Production build sets VITE_API_BASE_URL=https://ibssapi.curium.dk so the browser
// calls the backend directly. Empty baseURL during `npm run dev` keeps axios using
// relative paths, which Vite's dev proxy forwards to http://localhost:5103.
const api = axios.create({ baseURL: import.meta.env.VITE_API_BASE_URL || '' })

// Endpoints under /v1/public/* are anonymous (the student-signup wizard flow
// carries its own X-Wizard-Token header, not the admin/partner session token).
// Sending the admin token to them confuses the backend (resolves to a non-
// wizard user → 401) and — worse — would trip the response interceptor below
// and kick the logged-in admin out of their session. Opt out here.
function isPublicPath(url = '') {
  // Handle absolute URLs (when VITE_API_BASE_URL is set) as well as relative ones.
  try {
    const path = url.startsWith('http') ? new URL(url).pathname : url
    return path.startsWith('/v1/public/')
  } catch { return false }
}

api.interceptors.request.use(config => {
  if (isPublicPath(config.url)) return config
  const token = localStorage.getItem('adminToken')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

api.interceptors.response.use(
  res => res,
  err => {
    // Only the authenticated admin/partner/student endpoints should bounce to
    // login on 401. Public-wizard endpoints have their own auth and may 401 for
    // benign reasons (stale wizardToken, fresh signup, etc.).
    if (err.response?.status === 401 && !isPublicPath(err.config?.url)) {
      localStorage.removeItem('adminToken')
      window.location.hash = '#/login'
    }
    return Promise.reject(err)
  }
)

export default api
