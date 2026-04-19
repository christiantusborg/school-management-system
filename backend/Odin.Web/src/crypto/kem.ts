import { ml_kem768 } from '@noble/post-quantum/ml-kem.js'
import { toBase64, fromBase64 } from './utils'

export { toBase64, fromBase64 }

// Generates a fresh 32-byte team key and encrypts it for a recipient's ML-KEM-768 public key.
// Returns the raw teamKey (for immediate use) plus the three fields to store per user.
export async function encryptTeamKey(
  recipientPublicKey: Uint8Array
): Promise<{ teamKey: Uint8Array; kemCiphertext: string; encryptedKey: string; nonce: string }> {
  const teamKey = crypto.getRandomValues(new Uint8Array(32))
  const { kemCiphertext, encryptedKey, nonce } = await wrapTeamKey(teamKey, recipientPublicKey)
  return { teamKey, kemCiphertext, encryptedKey, nonce }
}

// Wraps an existing team key for a recipient's ML-KEM-768 public key.
// Use this when adding a team member: you already have the raw team key, just wrap it for them.
export async function wrapTeamKey(
  teamKey: Uint8Array,
  recipientPublicKey: Uint8Array
): Promise<{ kemCiphertext: string; encryptedKey: string; nonce: string }> {
  const { cipherText, sharedSecret } = ml_kem768.encapsulate(recipientPublicKey)
  const nonce = crypto.getRandomValues(new Uint8Array(12))
  const cryptoKey = await crypto.subtle.importKey('raw', sharedSecret, { name: 'AES-GCM' }, false, ['encrypt'])
  const ciphertext = await crypto.subtle.encrypt({ name: 'AES-GCM', iv: nonce }, cryptoKey, teamKey)
  return {
    kemCiphertext: toBase64(cipherText),
    encryptedKey: toBase64(new Uint8Array(ciphertext)),
    nonce: toBase64(nonce)
  }
}

// Decrypts the team key using the user's ML-KEM-768 secret key.
export async function decryptTeamKey(
  kemCiphertextB64: string,
  encryptedKeyB64: string,
  nonceB64: string,
  secretKey: Uint8Array
): Promise<Uint8Array> {
  const sharedSecret = ml_kem768.decapsulate(fromBase64(kemCiphertextB64), secretKey)
  const cryptoKey = await crypto.subtle.importKey('raw', sharedSecret, { name: 'AES-GCM' }, false, ['decrypt'])
  const plaintext = await crypto.subtle.decrypt(
    { name: 'AES-GCM', iv: fromBase64(nonceB64) },
    cryptoKey,
    fromBase64(encryptedKeyB64)
  )
  return new Uint8Array(plaintext)
}

// Wraps a level private key for a recipient's ML-KEM-768 public key.
// Used when granting another user access to a case level — caller must hold the raw level private key.
export async function wrapLevelPrivateKey(
  levelPrivKey: Uint8Array,
  recipientPublicKey: Uint8Array
): Promise<{ kemCiphertext: string; encryptedLevelPrivKey: string; nonce: string }> {
  const { cipherText, sharedSecret } = ml_kem768.encapsulate(recipientPublicKey)
  const nonce = crypto.getRandomValues(new Uint8Array(12))
  const cryptoKey = await crypto.subtle.importKey('raw', sharedSecret, { name: 'AES-GCM' }, false, ['encrypt'])
  const ciphertext = await crypto.subtle.encrypt({ name: 'AES-GCM', iv: nonce }, cryptoKey, levelPrivKey)
  return {
    kemCiphertext: toBase64(cipherText),
    encryptedLevelPrivKey: toBase64(new Uint8Array(ciphertext)),
    nonce: toBase64(nonce)
  }
}

// Generates a fresh 32-byte file key and encrypts it for a case level's ML-KEM-768 public key.
// Call once per qualifying level when uploading a file.
export async function encryptFileKeyForLevel(
  fileKey: Uint8Array,
  levelPublicKey: Uint8Array
): Promise<{ kemCiphertext: string; encryptedFileKey: string; nonce: string }> {
  const { cipherText, sharedSecret } = ml_kem768.encapsulate(levelPublicKey)
  const nonce = crypto.getRandomValues(new Uint8Array(12))
  const cryptoKey = await crypto.subtle.importKey('raw', sharedSecret, { name: 'AES-GCM' }, false, ['encrypt'])
  const ciphertext = await crypto.subtle.encrypt({ name: 'AES-GCM', iv: nonce }, cryptoKey, fileKey)
  return {
    kemCiphertext: toBase64(cipherText),
    encryptedFileKey: toBase64(new Uint8Array(ciphertext)),
    nonce: toBase64(nonce)
  }
}

// Decrypts a level private key using the user's personal ML-KEM-768 secret key.
// Used when you need to re-wrap a level key for another user.
export async function decryptLevelPrivateKey(
  kemCiphertextB64: string,
  encryptedLevelPrivKeyB64: string,
  nonceB64: string,
  mySecretKey: Uint8Array
): Promise<Uint8Array> {
  const sharedSecret = ml_kem768.decapsulate(fromBase64(kemCiphertextB64), mySecretKey)
  const cryptoKey = await crypto.subtle.importKey('raw', sharedSecret, { name: 'AES-GCM' }, false, ['decrypt'])
  const plaintext = await crypto.subtle.decrypt(
    { name: 'AES-GCM', iv: fromBase64(nonceB64) },
    cryptoKey,
    fromBase64(encryptedLevelPrivKeyB64)
  )
  return new Uint8Array(plaintext)
}

export function generateKemKeyPair(): { publicKey: Uint8Array; secretKey: Uint8Array } {
  const seed = crypto.getRandomValues(new Uint8Array(64))
  return ml_kem768.keygen(seed)
}

export async function encryptKemPrivateKey(
  secretKey: Uint8Array,
  encKeyBytes: Uint8Array
): Promise<{ encryptedKey: string; nonce: string }> {
  const nonce = crypto.getRandomValues(new Uint8Array(12))

  const cryptoKey = await crypto.subtle.importKey(
    'raw',
    encKeyBytes,
    { name: 'AES-GCM' },
    false,
    ['encrypt']
  )

  const ciphertext = await crypto.subtle.encrypt(
    { name: 'AES-GCM', iv: nonce },
    cryptoKey,
    secretKey
  )

  return {
    encryptedKey: toBase64(new Uint8Array(ciphertext)),
    nonce: toBase64(nonce)
  }
}

export async function decryptKemPrivateKey(
  encryptedKey: string,
  nonce: string,
  encKeyBytes: Uint8Array
): Promise<Uint8Array> {
  const cryptoKey = await crypto.subtle.importKey(
    'raw',
    encKeyBytes,
    { name: 'AES-GCM' },
    false,
    ['decrypt']
  )

  const plaintext = await crypto.subtle.decrypt(
    { name: 'AES-GCM', iv: fromBase64(nonce) },
    cryptoKey,
    fromBase64(encryptedKey)
  )

  return new Uint8Array(plaintext)
}
