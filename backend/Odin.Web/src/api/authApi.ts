import api from './axiosInstance'
import type {
  ApiResponse,
  LoginInitRequest,
  LoginInitResponse,
  LoginFinishRequest,
  LoginResponse,
  RegisterInitRequest,
  RegisterInitResponse,
  RegisterFinalizeRequest,
  RegisterResponse,
  MeResponse,
  RecoveryLoginInitRequest,
  RecoveryLoginInitResponse,
  PasswordResetInitRequest,
  PasswordResetInitResponse,
  PasswordResetFinalizeRequest
} from '@/types'

export const authApi = {
  // OPAQUE Login (2-step)
  loginInit: (data: LoginInitRequest) =>
    api.post<ApiResponse<LoginInitResponse>>('/login/init', data),

  loginFinish: (data: LoginFinishRequest) =>
    api.post<ApiResponse<LoginResponse>>('/login/finish', data),

  // Recovery code login via OPAQUE (init step; finish uses loginFinish)
  loginRecoveryInit: (data: RecoveryLoginInitRequest) =>
    api.post<ApiResponse<RecoveryLoginInitResponse>>('/login/recovery/init', data),

  // OPAQUE Register (2-step)
  registerInit: (data: RegisterInitRequest) =>
    api.post<ApiResponse<RegisterInitResponse>>('/register/init', data),

  registerFinalize: (data: RegisterFinalizeRequest) =>
    api.post<ApiResponse<RegisterResponse>>('/register/finalize', data),

  // Password reset re-registration (2-step)
  resetInit: (data: PasswordResetInitRequest) =>
    api.post<ApiResponse<PasswordResetInitResponse>>('/password-reset/init', data),

  resetFinalize: (data: PasswordResetFinalizeRequest) =>
    api.post<ApiResponse<RegisterResponse>>('/password-reset/finalize', data),

  logout: () =>
    api.post<ApiResponse>('/logout'),

  logoutEverywhere: () =>
    api.post<ApiResponse>('/logout-everywhere'),

  getMe: () =>
    api.get<ApiResponse<MeResponse>>('/me')
}
