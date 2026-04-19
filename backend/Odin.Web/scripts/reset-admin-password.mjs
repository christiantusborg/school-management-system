/**
 * Resets the OPAQUE credentials for a seed user to a known password.
 * Usage: node scripts/reset-admin-password.mjs <username> <newPassword>
 *
 * Example: node scripts/reset-admin-password.mjs adm "Admin@123!"
 */
import { ristretto255_oprf, ed25519 } from '@noble/curves/ed25519.js'
import { hkdf } from '@noble/hashes/hkdf.js'
import { sha512 } from '@noble/hashes/sha2.js'
import { createCipheriv, randomBytes } from 'crypto'
import Database from 'better-sqlite3'
import { fileURLToPath } from 'url'
import { dirname, resolve } from 'path'

const __dirname = dirname(fileURLToPath(import.meta.url))

const username = process.argv[2]
const password = process.argv[3]

if (!username || !password) {
  console.error('Usage: node reset-admin-password.mjs <username> <password>')
  process.exit(1)
}

// AES-256-GCM key from appsettings.json
const FIELD_KEY = Buffer.from('4258c6316815cda9d2a35dc6733344b8cdefd65154a9e3ce722a99b58cc77e89', 'hex')

function encryptField(plaintextBytes) {
  const nonce = randomBytes(12)  // AesGcm.NonceByteSizes.MaxSize = 12
  const cipher = createCipheriv('aes-256-gcm', FIELD_KEY, nonce)
  const ciphertext = Buffer.concat([cipher.update(plaintextBytes), cipher.final()])
  const tag = cipher.getAuthTag()  // 16 bytes
  // Format matches C# FieldEncryption: nonce | tag | ciphertext
  return Buffer.concat([nonce, tag, ciphertext])
}

const enc = new TextEncoder()
const passwordBytes = enc.encode(password)

// Generate OPAQUE credentials
const kp = ristretto255_oprf.oprf.generateKeyPair()
const oprfSeed = kp.secretKey
const oprfOutput = ristretto255_oprf.oprf.evaluate(oprfSeed, passwordBytes)

// Ed25519 client public key
const HKDF_SALT = enc.encode('ODIN-OPAQUE')
const HKDF_INFO_ED = enc.encode('ed25519-key')
const ed25519Seed = hkdf(sha512, oprfOutput, HKDF_SALT, HKDF_INFO_ED, 32)
const clientPublicKey = ed25519.getPublicKey(ed25519Seed)

// Only OprfSeed is encrypted at rest; ClientPublicKey is stored as plain bytes
const encryptedOprfSeed = encryptField(Buffer.from(oprfSeed))
const rawClientPublicKey = Buffer.from(clientPublicKey)

// Open SQLite DB
const dbPath = resolve(__dirname, '../../SharedLibrary.Basics.Opaque.Api/odin.db')
const db = new Database(dbPath)

// Find the user
const user = db.prepare("SELECT Id FROM AspNetUsers WHERE NormalizedUserName = ?")
  .get(username.toUpperCase())

if (!user) {
  console.error(`User '${username}' not found in database.`)
  process.exit(1)
}

const userId = user.Id

// Upsert OpaqueCredentials
const existing = db.prepare("SELECT OpaqueCredentialId FROM OpaqueCredentials WHERE UserId = ?").get(userId)

if (existing) {
  db.prepare(`
    UPDATE OpaqueCredentials
    SET OprfSeed = ?, ClientPublicKey = ?
    WHERE UserId = ?
  `).run(encryptedOprfSeed, rawClientPublicKey, userId)
  console.log(`✓ Updated OPAQUE credentials for '${username}'`)
} else {
  const newId = crypto.randomUUID()
  db.prepare(`
    INSERT INTO OpaqueCredentials (OpaqueCredentialId, UserId, OprfSeed, ClientPublicKey, CreatedAt)
    VALUES (?, ?, ?, ?, datetime('now'))
  `).run(newId, userId, encryptedOprfSeed, rawClientPublicKey)
  console.log(`✓ Created OPAQUE credentials for '${username}'`)
}

db.close()
console.log(`\nUsername : ${username}`)
console.log(`Password : ${password}`)
console.log(`\nYou can now log in at http://localhost:5173`)
