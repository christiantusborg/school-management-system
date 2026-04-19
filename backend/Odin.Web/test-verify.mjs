import { ed25519, RistrettoPoint } from '@noble/curves/ed25519'
import { sha512 } from '@noble/hashes/sha512'
import { sha256 } from '@noble/hashes/sha256'
import { hkdf } from '@noble/hashes/hkdf'

const BASE = 'http://localhost:5103'

function toB64(buf) { return Buffer.from(buf).toString('base64') }
function fromB64(s) { return Buffer.from(s, 'base64') }

// OPAQUE blind
function blindPassword(password) {
  const blind = ed25519.utils.randomPrivateKey()
  const H = RistrettoPoint.hashToCurve(new TextEncoder().encode(password))
  const blindedPt = H.multiply(ed25519.utils.mod(BigInt('0x' + Buffer.from(blind).toString('hex')), ed25519.CURVE.n))
  return { blind, blindedElement: toB64(blindedPt.toRawBytes()) }
}

function unblind(evaluated, blind) {
  const blindInv = ed25519.utils.mod(
    ed25519.utils.invert(BigInt('0x' + Buffer.from(blind).toString('hex')), ed25519.CURVE.n),
    ed25519.CURVE.n
  )
  return RistrettoPoint.fromHex(Buffer.from(evaluated, 'base64').toString('hex')).multiply(blindInv)
}

function derivePrivKey(password, blind, evaluatedB64) {
  const unblinded = unblind(evaluatedB64, blind)
  const opaqueKey = hkdf(sha256, unblinded.toRawBytes(), new TextEncoder().encode(password), new TextEncoder().encode('opaque-key'), 32)
  return ed25519.utils.mod(BigInt('0x' + Buffer.from(opaqueKey).toString('hex')), ed25519.CURVE.n)
}

async function login(username, password) {
  const { blind, blindedElement } = blindPassword(password)
  const initRes = await fetch(`${BASE}/v1/login/init`, {
    method: 'POST',
    headers: {'Content-Type': 'application/json'},
    body: JSON.stringify({ username, blindedElement })
  })
  const init = await initRes.json()
  
  const privKey = derivePrivKey(password, blind, init.evaluatedElement)
  const privKeyBytes = Buffer.from(privKey.toString(16).padStart(64, '0'), 'hex')
  const challenge = fromB64(init.challenge)
  const sig = ed25519.sign(challenge, privKeyBytes)
  
  const finishRes = await fetch(`${BASE}/v1/login/finish`, {
    method: 'POST',
    headers: {'Content-Type': 'application/json'},
    body: JSON.stringify({ loginId: init.loginId, signature: toB64(sig) })
  })
  const finish = await finishRes.json()
  return finish.token
}

const token = await login('c1', 'o12FP17wGIf4FhoNGTXLzVqiU5opkMo')
console.log('Token:', token ? 'obtained' : 'FAILED', token?.substring(0,20))

if (!token) process.exit(1)

// Get emails
const emailsRes = await fetch(`${BASE}/v1/profile/emails`, {
  headers: { 'Authorization': `Bearer ${token}` }
})
const emails = await emailsRes.json()
console.log('Emails response:', JSON.stringify(emails).substring(0, 200))

// Test verify init for first email
const emailId = '0C027B7A-6971-4ACC-B9DC-C8064EBBEF1E'
const verifyInitRes = await fetch(`${BASE}/v1/profile/emails/${emailId}/verify/init`, {
  method: 'POST',
  headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' }
})
const verifyInit = await verifyInitRes.json()
console.log('Verify init status:', verifyInitRes.status)
console.log('Verify init response:', JSON.stringify(verifyInit).substring(0, 200))
