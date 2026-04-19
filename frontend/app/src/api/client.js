import axios from 'axios'

const api = axios.create({})

api.interceptors.request.use(config => {
  const token = localStorage.getItem('adminToken')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

api.interceptors.response.use(
  res => res,
  err => {
    if (err.response?.status === 401) {
      localStorage.removeItem('adminToken')
      window.location.hash = '#/login'
    }
    return Promise.reject(err)
  }
)

export default api
