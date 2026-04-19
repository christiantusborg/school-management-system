export interface ApiResponse<T = unknown> {
  success: boolean
  message?: string
  data?: T
}

// OPAQUE Login (2-step)
export interface LoginInitRequest {
  username: string
  blindedElement: string
  deviceInfo?: string
}

export interface LoginInitResponse {
  loginId: string
  evaluatedElement: string
  challenge: string
}

export interface LoginFinishRequest {
  loginId: string
  signature: string
}

export interface LoginResponse {
  token?: string
  expiresAt?: string
  mfaPendingId?: string
  availableMethods?: string[]   // "totp" | "email" | "sms" | "fido2"
}

// MFA auth
export interface MfaTotpVerifyRequest { pendingId: string; code: string }
export interface MfaOtpSendRequest { pendingId: string }
export interface MfaOtpVerifyRequest { pendingId: string; code: string }
export interface MfaFido2AssertionInitRequest { pendingId: string }
export interface MfaFido2AssertionFinishRequest { pendingId: string; assertionResponse: unknown }

// FIDO2 init response (register or assertion)
export interface MfaFido2InitResponse { optionsJson: string }

// MFA account management
export interface MfaStatusResponse {
  enabledMethods: { method: string }[]
  fido2Credentials: { fido2CredentialId: string; label: string; createdAt: string; lastUsedAt?: string }[]
}

export interface MfaEnableInitResponse { sessionId: string }
export interface MfaEnableConfirmRequest { sessionId: string; code: string }
export interface TotpInitResponse { secret: string; qrUri: string }
export interface TotpEnableRequest { code: string }
export interface Fido2RegisterFinishRequest { label: string; attestationResponse: unknown }

// OPAQUE Register (2-step)
export interface RegisterInitRequest {
  username: string
  email: string
  blindedElement: string
  inviteCode?: string
}

export interface RegisterInitResponse {
  registrationId: string
  evaluatedElement: string
}

export interface RegisterFinalizeRequest {
  registrationId: string
  clientPublicKey: string
}

export interface RegisterResponse {
  token: string
}

export interface MeResponse {
  userId: string
  username: string
  email: string
  roles: string[]
  isEnabled: boolean
  createdAt: string
}

// OPAQUE Change Password (2-step)
export interface ChangePasswordInitRequest {
  oldBlindedElement: string
  blindedElement: string
}

export interface ChangePasswordInitResponse {
  changeId: string
  oldEvaluatedElement: string
  challenge: string
  evaluatedElement: string
}

export interface ChangePasswordFinalizeRequest {
  changeId: string
  signature: string
  clientPublicKey: string
}

export interface RecoveryCodesStatusResponse {
  remainingCount: number
}

// Profile entries — id fields match backend field names
export interface PhoneEntry { id: string; number: string; label?: string; isPrimary: boolean; isVerified: boolean }
export interface ContactEmailEntry { id: string; email: string; label?: string; isPrimary: boolean; isVerified: boolean }
export interface AddressEntry { id: string; label?: string; street?: string; city?: string; state?: string; zipCode?: string; country?: string; isPrimary: boolean }

// Basic profile (returned by GET /v1/profile)
export interface ProfileResponse {
  userProfileId: string
  firstName?: string
  lastName?: string
  avatarUrl?: string
  bio?: string
  dateOfBirth?: string
}

export interface UpdateProfileRequest {
  firstName?: string
  lastName?: string
  avatarUrl?: string
  bio?: string
  dateOfBirth?: string
}

// GetAll response wrapper (phones, emails, addresses)
export interface GetAllResponse<T> {
  items: T[]
  total: number
}

export interface AddPhoneRequest { number: string; label?: string }
export interface AddContactEmailRequest { email: string; label?: string }
export interface AddAddressRequest { label?: string; street?: string; city?: string; state?: string; zipCode?: string; country?: string }

// Admin
export interface UserListItem {
  userId: string
  username: string
  email: string
  isEnabled: boolean
  roles: string[]
  createdAt: string
}

export interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

export interface UserDetailResponse {
  userId: string
  username: string
  email: string
  isEnabled: boolean
  roles: string[]
  createdAt: string
  firstName?: string
  lastName?: string
  avatarUrl?: string
  bio?: string
  dateOfBirth?: string
}

export interface ResetPasswordResponse {
  resetToken: string
}

export interface InviteCodeResponse {
  code: string
  assignedRole: string
  expiresAt: string
}

export interface InviteCodeListItem {
  inviteCodeId: string
  code: string
  assignedRole: string
  expiresAt: string
  redeemedByUserId?: string
  createdAt: string
}

export interface InviteCodeListResponse {
  items: InviteCodeListItem[]
  total: number
}

export interface CreateInviteCodeRequest {
  assignedRole: string
  expirationDays: number
}

export interface ChangeRoleRequest {
  newRole: string
}

// Recovery code login via OPAQUE (2-step; uses /login/finish for finalize)
export interface RecoveryLoginInitRequest {
  username: string
  codeId: string
  blindedElement: string
  deviceInfo?: string
}

export interface RecoveryLoginInitResponse {
  loginId: string
  evaluatedElement: string
  challenge: string
  encryptedPrivateKey: string
  nonce: string
}

// Password-reset re-registration (2-step, initiated by admin reset-password action)
export interface PasswordResetInitRequest {
  resetToken: string
  blindedElement: string
}

export interface PasswordResetInitResponse {
  resetId: string
  evaluatedElement: string
}

export interface PasswordResetFinalizeRequest {
  resetId: string
  clientPublicKey: string
}

// KEM key pair
export interface KemKeyPairRequest {
  publicKey: string
  encryptedPrivateKey: string
  nonce: string
}

export interface KemKeyPairResponse {
  publicKey: string
  encryptedPrivateKey: string
  nonce: string
}

// Batch recovery codes setup/regeneration
export interface RecoveryCodesBatchInitRequest {
  codeIds: string[]
  blindedElements: string[]
}

export interface RecoveryCodesBatchInitResponse {
  batchId: string
  evaluatedElements: string[]
}

export interface RecoveryCodesBatchFinalizeRequest {
  batchId: string
  clientPublicKeys: string[]
  encryptedPrivateKeys: string[]
  nonces: string[]
}
