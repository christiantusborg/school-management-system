import { blindPassword, signChallenge } from './src/crypto/opaque'

const BASE = 'http://localhost:5103'

async function run() {
  const password = 'o12FP17wGIf4FhoNGTXLzVqiU5opkMo'
  const { blind, blindedElement } = blindPassword(password)
  
  const initRes = await fetch(`${BASE}/v1/login/init`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username: 'c1', blindedElement })
  })
  const init = await initRes.json() as any
  console.log('Init:', JSON.stringify(init).substring(0, 100))
  
  const sig = signChallenge(password, blind, init.evaluatedElement, init.challenge)
  const finishRes = await fetch(`${BASE}/v1/login/finish`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ loginId: init.loginId, signature: sig })
  })
  const finish = await finishRes.json() as any
  console.log('Finish status:', finishRes.status, JSON.stringify(finish).substring(0, 200))
  
  const token = finish.token
  if (!token) { console.log('No token!'); return }
  
  const emailId = '0C027B7A-6971-4ACC-B9DC-C8064EBBEF1E'
  const verRes = await fetch(`${BASE}/v1/profile/emails/${emailId}/verify/init`, {
    method: 'POST',
    headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' }
  })
  const ver = await verRes.json()
  console.log('Verify status:', verRes.status)
  console.log('Verify response:', JSON.stringify(ver))
}

run().catch(console.error)
