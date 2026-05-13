import { ristretto255_oprf, ed25519 } from '@noble/curves/ed25519.js'
import { hkdf } from '@noble/hashes/hkdf.js'
import { sha512 } from '@noble/hashes/sha2.js'
import { createCipheriv, randomBytes, randomUUID } from 'crypto'
import pkg from 'pg'
const { Client } = pkg

const username = process.argv[2]
const password = process.argv[3]
if (!username || !password) { console.error('usage: node reset-pg.mjs <username> <password>'); process.exit(1) }

const FIELD_KEY = Buffer.from('4258c6316815cda9d2a35dc6733344b8cdefd65154a9e3ce722a99b58cc77e89', 'hex')
function encryptField(plaintextBytes) {
  const nonce = randomBytes(12)
  const cipher = createCipheriv('aes-256-gcm', FIELD_KEY, nonce)
  const ciphertext = Buffer.concat([cipher.update(plaintextBytes), cipher.final()])
  const tag = cipher.getAuthTag()
  return Buffer.concat([nonce, tag, ciphertext])
}

const enc = new TextEncoder()
const passwordBytes = enc.encode(password)
const kp = ristretto255_oprf.oprf.generateKeyPair()
const oprfSeed = kp.secretKey
const oprfOutput = ristretto255_oprf.oprf.evaluate(oprfSeed, passwordBytes)
const ed25519Seed = hkdf(sha512, oprfOutput, enc.encode('ODIN-OPAQUE'), enc.encode('ed25519-key'), 32)
const clientPublicKey = ed25519.getPublicKey(ed25519Seed)

const encryptedOprfSeed = encryptField(Buffer.from(oprfSeed))
const rawClientPublicKey = Buffer.from(clientPublicKey)

const client = new Client({ connectionString: 'postgres://school_app_user:Z7%24qL9%40pY2%23wK5%2Ax@192.168.1.201:5432/school_management_db' })
await client.connect()

const u = await client.query('SELECT "Id" FROM "AspNetUsers" WHERE "NormalizedUserName" = $1', [username.toUpperCase()])
if (u.rowCount === 0) { console.error(`user '${username}' not found`); process.exit(1) }
const userId = u.rows[0].Id

const ex = await client.query('SELECT "OpaqueCredentialId" FROM "OpaqueCredentials" WHERE "UserId" = $1', [userId])
if (ex.rowCount > 0) {
  await client.query('UPDATE "OpaqueCredentials" SET "OprfSeed"=$1, "ClientPublicKey"=$2 WHERE "UserId"=$3',
    [encryptedOprfSeed, rawClientPublicKey, userId])
  console.log(`updated creds for ${username}`)
} else {
  await client.query('INSERT INTO "OpaqueCredentials" ("OpaqueCredentialId","UserId","OprfSeed","ClientPublicKey","CreatedAt","TenantId") VALUES ($1,$2,$3,$4, now() at time zone \'utc\', \'00000000-0000-0000-0000-000000000000\'::uuid)',
    [randomUUID(), userId, encryptedOprfSeed, rawClientPublicKey])
  console.log(`inserted creds for ${username}`)
}
await client.end()
