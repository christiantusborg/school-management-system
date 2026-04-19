import api from './axiosInstance'
import type {
  ApiResponse,
  MfaStatusResponse,
  TotpInitResponse,
  TotpEnableRequest,
  Fido2RegisterFinishRequest,
  MfaEnableInitResponse,
  MfaEnableConfirmRequest,
  MfaFido2InitResponse
} from '@/types'

export const accountMfaApi = {
  getStatus: () =>
    api.get<ApiResponse<MfaStatusResponse>>('/account/mfa/status'),

  totpInit: () =>
    api.post<ApiResponse<TotpInitResponse>>('/mfa/totp/enable/init', {}),

  totpEnable: (data: TotpEnableRequest) =>
    api.post<ApiResponse>('/mfa/totp/enable/confirm', data),

  totpDelete: () =>
    api.delete<ApiResponse>('/mfa/totp'),

  emailEnableInit: () =>
    api.post<ApiResponse<MfaEnableInitResponse>>('/mfa/email/enable/init', {}),

  emailEnableConfirm: (data: MfaEnableConfirmRequest) =>
    api.post<ApiResponse>('/mfa/email/enable/confirm', data),

  emailDelete: () =>
    api.delete<ApiResponse>('/mfa/email'),

  smsEnableInit: () =>
    api.post<ApiResponse<MfaEnableInitResponse>>('/mfa/sms/enable/init', {}),

  smsEnableConfirm: (data: MfaEnableConfirmRequest) =>
    api.post<ApiResponse>('/mfa/sms/enable/confirm', data),

  smsDelete: () =>
    api.delete<ApiResponse>('/mfa/sms'),

  fido2RegisterInit: () =>
    api.post<ApiResponse<MfaFido2InitResponse>>('/mfa/fido2/register/init', {}),

  fido2RegisterFinish: (data: Fido2RegisterFinishRequest) =>
    api.post<ApiResponse>('/mfa/fido2/register/finish', data),

  fido2DeleteCredential: (id: string) =>
    api.delete<ApiResponse>(`/mfa/fido2/${id}`),
}
