import api from './axiosInstance'
import type { ApiResponse } from '@/types'

export interface CreateTeamRequest {
  name: string
  description?: string
  teamTypeId?: string
  kemCiphertext: string   // base64 — 1088-byte ML-KEM-768 ciphertext
  encryptedKey: string    // base64 — 48-byte AES-GCM encrypted team key
  nonce: string           // base64 — 12-byte AES-GCM nonce
}

export interface AddMemberRequest {
  userId: string
  teamRoleId?: string
  kemCiphertext: string   // base64 — team key wrapped for new member's public key
  encryptedKey: string
  nonce: string
}

export interface CreateTeamResponse {
  teamId: string
}

export const teamApi = {
  listTeams: () =>
    api.get<ApiResponse<{ items: TeamItem[]; total: number }>>('/teams?page=1&pageSize=100'),

  createTeam: (data: CreateTeamRequest) =>
    api.post<ApiResponse<CreateTeamResponse>>('/teams', data),

  getSymmetricKey: (teamId: string) =>
    api.get<ApiResponse<{ kemCiphertext: string; encryptedKey: string; nonce: string }>>(`/teams/${teamId}/symmetric-key`),

  listMembers: (teamId: string) =>
    api.get<ApiResponse<{ items: MemberItem[]; total: number }>>(`/teams/${teamId}/members`),

  addMember: (teamId: string, data: AddMemberRequest) =>
    api.post<ApiResponse>(`/teams/${teamId}/members`, data),

  removeMember: (teamId: string, userId: string) =>
    api.delete<ApiResponse>(`/teams/${teamId}/members/${userId}`),

  getUserPublicKey: (userId: string) =>
    api.get<ApiResponse<{ publicKey: string }>>(`/users/${userId}/kem-public-key`),

  updateTeam: (teamId: string, data: UpdateTeamRequest) =>
    api.put<ApiResponse>(`/teams/${teamId}`, data),

  saveSymmetricKey: (teamId: string, data: { kemCiphertext: string; encryptedKey: string; nonce: string }) =>
    api.put<ApiResponse>(`/teams/${teamId}/symmetric-key`, data),

  searchUsers: (q: string) =>
    api.get<ApiResponse<{ items: UserSearchItem[] }>>(`/users/search?q=${encodeURIComponent(q)}`),
}

export interface TeamItem {
  teamId: string
  name: string
  description?: string
  teamTypeId: string
  deletedAt?: string
}

export interface MemberItem {
  teamMemberId: string
  userId: string
  teamRoleId: string
  username?: string
  firstName?: string
  lastName?: string
  email?: string
  roleName?: string
}

export interface UserSearchItem {
  userId: string
  username: string
  firstName?: string
  lastName?: string
  email?: string
}

export interface UpdateTeamRequest {
  name: string
  description?: string
}
