import { defineStore } from 'pinia'
import { ref } from 'vue'
import { profileApi } from '@/api/profileApi'
import type { ProfileResponse, UpdateProfileRequest, PhoneEntry, ContactEmailEntry, AddressEntry } from '@/types'

export const useProfileStore = defineStore('profile', () => {
  const profile = ref<ProfileResponse | null>(null)
  const phones = ref<PhoneEntry[]>([])
  const emails = ref<ContactEmailEntry[]>([])
  const addresses = ref<AddressEntry[]>([])
  const loading = ref(false)

  async function fetchProfile() {
    loading.value = true
    try {
      const [profileRes, phonesRes, emailsRes, addressesRes] = await Promise.all([
        profileApi.getProfile(),
        profileApi.getPhones(),
        profileApi.getEmails(),
        profileApi.getAddresses(),
      ])
      if (profileRes.data.success && profileRes.data.data) {
        profile.value = profileRes.data.data
      }
      if (phonesRes.data.success && phonesRes.data.data) {
        phones.value = phonesRes.data.data.items
      }
      if (emailsRes.data.success && emailsRes.data.data) {
        emails.value = emailsRes.data.data.items
      }
      if (addressesRes.data.success && addressesRes.data.data) {
        addresses.value = addressesRes.data.data.items
      }
    } finally {
      loading.value = false
    }
  }

  async function updateProfile(data: UpdateProfileRequest) {
    const res = await profileApi.updateProfile(data)
    if (res.data.success) {
      await fetchProfile()
    }
    return res.data
  }

  return { profile, phones, emails, addresses, loading, fetchProfile, updateProfile }
})
