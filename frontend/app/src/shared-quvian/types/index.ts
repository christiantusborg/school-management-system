// Minimal shim of @quvian/shared/types for the ported intake feature.
// Only what intakeApi.ts imports; the full core type surface stays in core.
export interface ApiResponse<T = unknown> {
  success: boolean
  message?: string
  error?: string
  data?: T
}
