import api from './axiosInstance'
import type { ApiResponse, ProfileResponse, UpdateProfileRequest, AddPhoneRequest, AddContactEmailRequest, AddAddressRequest, GetAllResponse, PhoneEntry, ContactEmailEntry, AddressEntry } from '@/types'

// Raw backend item shapes (ids use backend field names before normalization)
interface RawPhoneItem { userPhoneId: string; number: string; label?: string; isPrimary: boolean; isVerified: boolean }
interface RawEmailItem { userContactEmailId: string; email: string; label?: string; isPrimary: boolean; isVerified: boolean }
interface RawAddressItem { userAddressId: string; label?: string; street?: string; city?: string; state?: string; zipCode?: string; country?: string; isPrimary: boolean }

function normalizePhone(p: RawPhoneItem): PhoneEntry {
  return { id: p.userPhoneId, number: p.number, label: p.label, isPrimary: p.isPrimary, isVerified: p.isVerified }
}

function normalizeEmail(e: RawEmailItem): ContactEmailEntry {
  return { id: e.userContactEmailId, email: e.email, label: e.label, isPrimary: e.isPrimary, isVerified: e.isVerified }
}

function normalizeAddress(a: RawAddressItem): AddressEntry {
  return { id: a.userAddressId, label: a.label, street: a.street, city: a.city, state: a.state, zipCode: a.zipCode, country: a.country, isPrimary: a.isPrimary }
}

export const profileApi = {
  getProfile: () =>
    api.get<ApiResponse<ProfileResponse>>('/profile'),

  updateProfile: (data: UpdateProfileRequest) =>
    api.put<ApiResponse>('/profile', data),

  // Phones
  getPhones: async () => {
    const res = await api.get<ApiResponse<GetAllResponse<RawPhoneItem>>>('/profile/phones')
    if (res.data.data) res.data.data.items = res.data.data.items.map(normalizePhone) as never
    return res as unknown as { data: ApiResponse<GetAllResponse<PhoneEntry>> }
  },
  addPhone: (data: AddPhoneRequest) =>
    api.post<ApiResponse>('/profile/phones', data),
  updatePhone: (id: string, data: AddPhoneRequest) =>
    api.put<ApiResponse>(`/profile/phones/${id}`, data),
  deletePhone: (id: string) =>
    api.delete<ApiResponse>(`/profile/phones/${id}`),
  setPrimaryPhone: (id: string) =>
    api.post<ApiResponse>(`/profile/phones/${id}/set-primary`, {}),
  verifyPhoneInit: (id: string) =>
    api.post<ApiResponse<{ sessionId: string }>>(`/profile/phones/${id}/verify/init`, {}),
  verifyPhoneConfirm: (sessionId: string, code: string) =>
    api.post<ApiResponse>('/profile/phones/verify/confirm', { sessionId, code }),

  // Contact emails
  getEmails: async () => {
    const res = await api.get<ApiResponse<GetAllResponse<RawEmailItem>>>('/profile/emails')
    if (res.data.data) res.data.data.items = res.data.data.items.map(normalizeEmail) as never
    return res as unknown as { data: ApiResponse<GetAllResponse<ContactEmailEntry>> }
  },
  addEmail: (data: AddContactEmailRequest) =>
    api.post<ApiResponse>('/profile/emails', data),
  updateEmail: (id: string, data: AddContactEmailRequest) =>
    api.put<ApiResponse>(`/profile/emails/${id}`, data),
  deleteEmail: (id: string) =>
    api.delete<ApiResponse>(`/profile/emails/${id}`),
  setPrimaryEmail: (id: string) =>
    api.post<ApiResponse>(`/profile/emails/${id}/set-primary`, {}),
  verifyEmailInit: (id: string) =>
    api.post<ApiResponse<{ sessionId: string }>>(`/profile/emails/${id}/verify/init`, {}),
  verifyEmailConfirm: (sessionId: string, code: string) =>
    api.post<ApiResponse>('/profile/emails/verify/confirm', { sessionId, code }),

  // Addresses
  getAddresses: async () => {
    const res = await api.get<ApiResponse<GetAllResponse<RawAddressItem>>>('/profile/addresses')
    if (res.data.data) res.data.data.items = res.data.data.items.map(normalizeAddress) as never
    return res as unknown as { data: ApiResponse<GetAllResponse<AddressEntry>> }
  },
  addAddress: (data: AddAddressRequest) =>
    api.post<ApiResponse>('/profile/addresses', data),
  updateAddress: (id: string, data: AddAddressRequest) =>
    api.put<ApiResponse>(`/profile/addresses/${id}`, data),
  deleteAddress: (id: string) =>
    api.delete<ApiResponse>(`/profile/addresses/${id}`),
  setPrimaryAddress: (id: string) =>
    api.post<ApiResponse>(`/profile/addresses/${id}/set-primary`, {}),
}
