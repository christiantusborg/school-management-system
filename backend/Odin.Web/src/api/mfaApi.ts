import api from './axiosInstance'
import type {
  ApiResponse,
  LoginResponse,
  MfaTotpVerifyRequest,
  MfaOtpSendRequest,
  MfaOtpVerifyRequest,
  MfaFido2AssertionInitRequest,
  MfaFido2AssertionFinishRequest,
  MfaFido2InitResponse
} from '@/types'

export const mfaApi = {
  totpVerify: (data: MfaTotpVerifyRequest) =>
    api.post<ApiResponse<LoginResponse>>('/mfa/totp/verify', data),

  emailSend: (data: MfaOtpSendRequest) =>
    api.post<ApiResponse>('/mfa/email/send', data),

  emailVerify: (data: MfaOtpVerifyRequest) =>
    api.post<ApiResponse<LoginResponse>>('/mfa/email/verify', data),

  smsSend: (data: MfaOtpSendRequest) =>
    api.post<ApiResponse>('/mfa/sms/send', data),

  smsVerify: (data: MfaOtpVerifyRequest) =>
    api.post<ApiResponse<LoginResponse>>('/mfa/sms/verify', data),

  fido2AssertionInit: (data: MfaFido2AssertionInitRequest) =>
    api.post<ApiResponse<MfaFido2InitResponse>>('/mfa/fido2/assertion/init', data),

  fido2AssertionFinish: (data: MfaFido2AssertionFinishRequest) =>
    api.post<ApiResponse<LoginResponse>>('/mfa/fido2/assertion/finish', data),
}
