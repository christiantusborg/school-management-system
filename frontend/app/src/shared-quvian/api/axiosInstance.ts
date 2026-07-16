// Shim replacing the QuVian core shared axios instance. Delegates to the
// IBSS api client (bearer-token interceptor, VITE_API_BASE_URL) and prefixes
// the '/v1' that core's client carried in its baseURL, so the copied
// intakeApi.ts works unchanged.
import client from '../../api/client.js'

const prefix = (url: string) => `/v1${url}`

export default {
  get:    (url: string, config?: object) => client.get(prefix(url), config),
  post:   (url: string, body?: unknown, config?: object) => client.post(prefix(url), body, config),
  put:    (url: string, body?: unknown, config?: object) => client.put(prefix(url), body, config),
  patch:  (url: string, body?: unknown, config?: object) => client.patch(prefix(url), body, config),
  delete: (url: string, config?: object) => client.delete(prefix(url), config),
}
