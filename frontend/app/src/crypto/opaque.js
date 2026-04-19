import { ristretto255_oprf, ed25519 } from '@noble/curves/ed25519.js'
import { hkdf } from '@noble/hashes/hkdf.js'
import { sha512 } from '@noble/hashes/sha2.js'
import { toBase64, fromBase64 } from './utils.js'

const HKDF_SALT = new TextEncoder().encode('ODIN-OPAQUE')
const HKDF_INFO = new TextEncoder().encode('ed25519-key')

/**
 * Step 1 of login: blind the password for OPRF.
 * Returns { blind, blindedElement } — keep blind in memory, send blindedElement (base64) to server.
 */
export function blindPassword(password) {
  const passwordBytes = new TextEncoder().encode(password)
  const { blind, blinded } = ristretto255_oprf.oprf.blind(passwordBytes)
  return { blind, blindedElement: toBase64(blinded) }
}

/**
 * Step 2 of login: unblind the server's evaluated element, derive Ed25519 private key, sign challenge.
 * Returns base64-encoded signature to send to /v1/login/finish.
 */
export function signChallenge(password, blind, evaluatedElementB64, challengeB64) {
  const passwordBytes = new TextEncoder().encode(password)
  const evaluatedElement = fromBase64(evaluatedElementB64)
  const challenge = fromBase64(challengeB64)

  const oprfOutput = ristretto255_oprf.oprf.finalize(passwordBytes, blind, evaluatedElement)
  const ed25519Seed = hkdf(sha512, oprfOutput, HKDF_SALT, HKDF_INFO, 32)
  const signature = ed25519.sign(challenge, ed25519Seed)
  return toBase64(signature)
}
