import { defineStore } from 'pinia'
import { ref } from 'vue'

export interface Notification {
  id: number
  message: string
  type: 'success' | 'error' | 'info'
}

let nextId = 0

export const useNotificationStore = defineStore('notification', () => {
  const notifications = ref<Notification[]>([])

  function notify(message: string, type: 'success' | 'error' | 'info' = 'info') {
    const id = nextId++
    notifications.value.push({ id, message, type })
    setTimeout(() => {
      notifications.value = notifications.value.filter(n => n.id !== id)
    }, 4000)
  }

  function success(message: string) { notify(message, 'success') }
  function error(message: string) { notify(message, 'error') }

  return { notifications, notify, success, error }
})
