import { defineStore } from 'pinia'
import { ref } from 'vue'
import { adminApi } from '@/api/adminApi'
import type { UserListItem, UserDetailResponse, InviteCodeListItem, PagedResult } from '@/types'

export const useAdminStore = defineStore('admin', () => {
  const users = ref<PagedResult<UserListItem> | null>(null)
  const currentUser = ref<UserDetailResponse | null>(null)
  const inviteCodes = ref<InviteCodeListItem[]>([])
  const loading = ref(false)

  async function fetchUsers(page = 1, pageSize = 20, search?: string) {
    loading.value = true
    try {
      const res = await adminApi.listUsers(page, pageSize, search)
      if (res.data.success && res.data.data) {
        users.value = res.data.data
      }
    } finally {
      loading.value = false
    }
  }

  async function fetchUser(id: string) {
    loading.value = true
    try {
      const res = await adminApi.getUser(id)
      if (res.data.success && res.data.data) {
        currentUser.value = res.data.data
      }
    } finally {
      loading.value = false
    }
  }

  async function fetchInviteCodes() {
    const res = await adminApi.listInviteCodes()
    if (res.data.success && res.data.data) {
      inviteCodes.value = res.data.data.items
    }
  }

  return { users, currentUser, inviteCodes, loading, fetchUsers, fetchUser, fetchInviteCodes }
})
