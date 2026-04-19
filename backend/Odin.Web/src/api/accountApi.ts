import api from './axiosInstance'
import type {
  ApiResponse,
  ChangePasswordInitRequest,
  ChangePasswordInitResponse,
  ChangePasswordFinalizeRequest,
  RecoveryCodesStatusResponse,
  KemKeyPairRequest,
  KemKeyPairResponse,
  RecoveryCodesBatchInitRequest,
  RecoveryCodesBatchInitResponse,
  RecoveryCodesBatchFinalizeRequest
} from '@/types'

export const accountApi = {
  // OPAQUE Change Password (2-step)
  changePasswordInit: (data: ChangePasswordInitRequest) =>
    api.post<ApiResponse<ChangePasswordInitResponse>>('/change-password/init', data),

  changePasswordFinalize: (data: ChangePasswordFinalizeRequest) =>
    api.post<ApiResponse>('/change-password/finalize', data),

  deleteAccount: () =>
    api.delete<ApiResponse>('/account'),

  // Recovery codes status
  getRecoveryCodesStatus: () =>
    api.get<ApiResponse<RecoveryCodesStatusResponse>>('/recovery-codes'),

  // Batch recovery codes setup/regeneration
  recoveryCodesInit: (data: RecoveryCodesBatchInitRequest) =>
    api.post<ApiResponse<RecoveryCodesBatchInitResponse>>('/recovery-codes/init', data),

  recoveryCodesFinalize: (data: RecoveryCodesBatchFinalizeRequest) =>
    api.post<ApiResponse>('/recovery-codes/finalize', data),

  // KEM key pair
  saveKemKeyPair: (data: KemKeyPairRequest) =>
    api.post<ApiResponse>('/account/kem-keypair', data),

  getKemKeyPair: () =>
    api.get<ApiResponse<KemKeyPairResponse>>('/account/kem-keypair')
}
