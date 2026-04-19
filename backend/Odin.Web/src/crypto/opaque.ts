import { ristretto255_oprf, ed25519 } from '@noble/curves/ed25519.js'
import { hkdf } from '@noble/hashes/hkdf.js'
import { sha512 } from '@noble/hashes/sha2.js'
import { toBase64, fromBase64 } from './utils'

const HKDF_SALT = new TextEncoder().encode('ODIN-OPAQUE')
const HKDF_INFO = new TextEncoder().encode('ed25519-key')
const HKDF_INFO_KEM = new TextEncoder().encode('ml-kem-key')

/**
 * Step 1: Blind the password for OPRF.
 * Returns base64-encoded blindedElement (to send to server) and raw blind scalar (keep in memory).
 */
export function blindPassword(password: string): { blind: Uint8Array; blindedElement: string } {
  const passwordBytes = new TextEncoder().encode(password)
  const { blind, blinded } = ristretto255_oprf.oprf.blind(passwordBytes)
  return { blind, blindedElement: toBase64(blinded) }
}

/**
 * Finalize the OPRF and derive Ed25519 public key (for registration + change-password finalize).
 */
export function derivePublicKey(
  password: string,
  blind: Uint8Array,
  evaluatedElementB64: string
): string {
  const passwordBytes = new TextEncoder().encode(password)
  const evaluatedElement = fromBase64(evaluatedElementB64)

  // Finalize OPRF: unblind and compute output hash
  const oprfOutput = ristretto255_oprf.oprf.finalize(passwordBytes, blind, evaluatedElement)

  // Derive Ed25519 seed via HKDF-SHA512
  const ed25519Seed = hkdf(sha512, oprfOutput, HKDF_SALT, HKDF_INFO, 32)

  // Derive public key
  const publicKey = ed25519.getPublicKey(ed25519Seed)
  return toBase64(publicKey)
}

/**
 * Finalize the OPRF and sign a challenge (for login finish).
 */
export function signChallenge(
  password: string,
  blind: Uint8Array,
  evaluatedElementB64: string,
  challengeB64: string
): string {
  const passwordBytes = new TextEncoder().encode(password)
  const evaluatedElement = fromBase64(evaluatedElementB64)
  const challenge = fromBase64(challengeB64)

  // Finalize OPRF
  const oprfOutput = ristretto255_oprf.oprf.finalize(passwordBytes, blind, evaluatedElement)

  // Derive Ed25519 seed
  const ed25519Seed = hkdf(sha512, oprfOutput, HKDF_SALT, HKDF_INFO, 32)

  // Sign challenge
  const signature = ed25519.sign(challenge, ed25519Seed)
  return toBase64(signature)
}

/**
 * Derive a 32-byte AES-256-GCM encryption key for the ML-KEM private key.
 * HKDF-SHA512(oprfOutput, salt='ODIN-OPAQUE', info='ml-kem-key', len=32)
 */
export function deriveKemEncKey(
  password: string,
  blind: Uint8Array,
  evaluatedElementB64: string
): Uint8Array {
  const passwordBytes = new TextEncoder().encode(password)
  const evaluatedElement = fromBase64(evaluatedElementB64)

  const oprfOutput = ristretto255_oprf.oprf.finalize(passwordBytes, blind, evaluatedElement)
  return hkdf(sha512, oprfOutput, HKDF_SALT, HKDF_INFO_KEM, 32)
}
