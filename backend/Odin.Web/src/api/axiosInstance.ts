import axios from 'axios'
import router from '@/router'

const api = axios.create({
  baseURL: '/v1',
  headers: {
    'Content-Type': 'application/json'
  }
})

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('odin_token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

api.interceptors.response.use(
  (response) => {
    response.data = { success: true, data: response.data }
    return response
  },
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('odin_token')
      router.push('/auth/login')
    }
    const raw = error.response?.data
      ? (typeof error.response.data === 'string' ? error.response.data : error.response.data.error ?? 'Request failed')
      : 'Request failed'
    // Strip internal command prefix e.g. "CommandName - actual message"
    const message = raw.includes(' - ') ? raw.substring(raw.indexOf(' - ') + 3) : raw
    return Promise.reject({ ...error, response: { ...error.response, data: { success: false, message } } })
  }
)

export default api
