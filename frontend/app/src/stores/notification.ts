import { defineStore } from 'pinia'
import { ref } from 'vue'

export interface Notification {
  id: number
  message: string
  type: 'success' | 'error' | 'info'
}

let nextId = 0

// Errors stick around long enough to be read on a slow connection
// and surface even when several fire in a row; success/info clear
// quickly. Either way the user can dismiss them with the × button.
const TIMEOUT_MS: Record<Notification['type'], number> = {
  success: 4000,
  info: 4000,
  error: 12000,
}

export const useNotificationStore = defineStore('notification', () => {
  const notifications = ref<Notification[]>([])

  function dismiss(id: number) {
    notifications.value = notifications.value.filter(n => n.id !== id)
  }

  function notify(message: string, type: 'success' | 'error' | 'info' = 'info') {
    const id = nextId++
    notifications.value.push({ id, message, type })
    setTimeout(() => dismiss(id), TIMEOUT_MS[type])
  }

  function success(message: string) { notify(message, 'success') }
  function error(message: string) { notify(message, 'error') }

  return { notifications, notify, success, error, dismiss }
})
