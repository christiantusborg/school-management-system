import api from './axiosInstance'
import type { ApiResponse } from '@/types'

// ── Cases ────────────────────────────────────────────────────────────────────

export interface CaseItem {
  caseId: string
  name: string
  description?: string
  status: string
  priority: string
  dueDate?: string
  createdByUserId: string
}

export interface CreateCaseRequest {
  name: string
  description?: string
  priority?: string
  dueDate?: string
  levelKeyPairs: CaseLevelKeyDto[]
}

export interface CaseLevelKeyDto {
  level: number
  publicKey: string           // base64, ML-KEM-768 public key
  wrappedPrivateKey: CaseWrappedKeyDto
}

export interface CaseWrappedKeyDto {
  kemCiphertext: string       // base64, 1088 bytes
  encryptedLevelPrivKey: string // base64
  nonce: string               // base64, 12 bytes
}

export interface CreateCaseResponse {
  caseId: string
}

// ── Case Key Pairs ───────────────────────────────────────────────────────────

export interface CaseKeyPairItem {
  level: number
  name: string
  publicKey: string           // base64
}

export interface LabelOverrideItem {
  level: number
  label: string
}

export interface CaseKeyPairsResponse {
  items: CaseKeyPairItem[]
  labelOverrides: LabelOverrideItem[]
}

// ── My Level Keys ────────────────────────────────────────────────────────────

export interface CaseUserKeyItem {
  level: number
  kemCiphertext: string
  encryptedLevelPrivKey: string
  nonce: string
}

export interface MyKeysResponse {
  keys: CaseUserKeyItem[]
}

// ── Members ──────────────────────────────────────────────────────────────────

export interface CaseMemberItem {
  caseUserMemberId: string
  userId: string
  level: number
  username?: string
  email?: string
  grantedByUserId: string
  grantedAt: string
}

export interface CaseTeamMemberItem {
  caseTeamMembershipId: string
  teamId: string
  level: number
  teamName?: string
  grantedByUserId: string
  grantedAt: string
}

export interface CaseMembersResponse {
  users: CaseMemberItem[]
  teams: CaseTeamMemberItem[]
}

export interface GrantUserRequest {
  targetUserId: string
  level: number
  wrappedKeys: WrappedLevelKeyDto[]
}

export interface WrappedLevelKeyDto {
  level: number
  kemCiphertext: string
  encryptedLevelPrivKey: string
  nonce: string
}

export interface GrantTeamRequest {
  teamId: string
  level: number
  teamWrappedKeys: TeamWrappedLevelKeyDto[]
}

export interface TeamWrappedLevelKeyDto {
  level: number
  encryptedLevelPrivKey: string
  nonce: string
}

// ── Files ────────────────────────────────────────────────────────────────────

export interface CaseFileItem {
  caseFileId: string
  name: string
  contentType: string
  sizeBytes: number
  minLevel: number
  accessMode: string
  createdByUserId: string
  createdAt: string
}

export interface CreateFileRequest {
  name: string
  contentType: string
  sizeBytes: number
  storagePath: string
  minLevel: number
  accessMode?: string
  levelKeys: FileLevelKeyDto[]
}

export interface FileLevelKeyDto {
  level: number
  kemCiphertext: string
  encryptedFileKey: string
  nonce: string
}

export interface FileKeyResponse {
  level: number
  kemCiphertext: string
  encryptedFileKey: string
  nonce: string
  storagePath: string
}

// ── API ──────────────────────────────────────────────────────────────────────

export const caseApi = {
  // Cases
  listCases: (page = 1, pageSize = 50) =>
    api.get<ApiResponse<{ items: CaseItem[]; total: number }>>(`/cases?page=${page}&pageSize=${pageSize}`),

  createCase: (data: CreateCaseRequest) =>
    api.post<ApiResponse<CreateCaseResponse>>('/cases', data),

  getCase: (caseId: string) =>
    api.get<ApiResponse<CaseItem>>(`/cases/${caseId}`),

  // Key pairs (public keys for all 6 levels)
  getKeyPairs: (caseId: string) =>
    api.get<ApiResponse<CaseKeyPairsResponse>>(`/cases/${caseId}/key-pairs`),

  // My encrypted level private keys
  getMyKeys: (caseId: string) =>
    api.get<ApiResponse<MyKeysResponse>>(`/cases/${caseId}/my-keys`),

  // Members
  listMembers: (caseId: string) =>
    api.get<ApiResponse<CaseMembersResponse>>(`/cases/${caseId}/members`),

  grantUser: (caseId: string, data: GrantUserRequest) =>
    api.post<ApiResponse<{ caseUserMemberId: string }>>(`/cases/${caseId}/members/users`, data),

  revokeUser: (caseId: string, userId: string) =>
    api.delete<ApiResponse>(`/cases/${caseId}/members/users/${userId}`),

  grantTeam: (caseId: string, data: GrantTeamRequest) =>
    api.post<ApiResponse<{ caseTeamMembershipId: string }>>(`/cases/${caseId}/members/teams`, data),

  revokeTeam: (caseId: string, teamId: string) =>
    api.delete<ApiResponse>(`/cases/${caseId}/members/teams/${teamId}`),

  // Files
  listFiles: (caseId: string) =>
    api.get<ApiResponse<{ items: CaseFileItem[] }>>(`/cases/${caseId}/files`),

  createFile: (caseId: string, data: CreateFileRequest) =>
    api.post<ApiResponse<{ caseFileId: string }>>(`/cases/${caseId}/files`, data),

  getFileKey: (caseId: string, fileId: string) =>
    api.get<ApiResponse<FileKeyResponse>>(`/cases/${caseId}/files/${fileId}/key`),
}
