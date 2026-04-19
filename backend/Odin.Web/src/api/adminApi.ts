import api from './axiosInstance'
import type {
  ApiResponse, PagedResult, UserListItem, UserDetailResponse,
  ResetPasswordResponse, InviteCodeResponse, InviteCodeListResponse,
  CreateInviteCodeRequest, ChangeRoleRequest
} from '@/types'

export const adminApi = {
  listUsers: (page = 1, pageSize = 20, search?: string) =>
    api.get<ApiResponse<PagedResult<UserListItem>>>('/admin/users', {
      params: { page, pageSize, search }
    }),

  getUser: (id: string) =>
    api.get<ApiResponse<UserDetailResponse>>(`/admin/users/${id}`),

  enableUser: (id: string) =>
    api.post<ApiResponse>(`/admin/users/${id}/enable`),

  disableUser: (id: string) =>
    api.post<ApiResponse>(`/admin/users/${id}/disable`),

  changeRole: (id: string, data: ChangeRoleRequest) =>
    api.post<ApiResponse>(`/admin/users/${id}/change-role`, data),

  resetPassword: (id: string) =>
    api.post<ApiResponse<ResetPasswordResponse>>(`/admin/users/${id}/reset-password`),

  createInviteCode: (data: CreateInviteCodeRequest) =>
    api.post<ApiResponse<InviteCodeResponse>>('/admin/invite-codes', data),

  listInviteCodes: () =>
    api.get<ApiResponse<InviteCodeListResponse>>('/admin/invite-codes')
}
