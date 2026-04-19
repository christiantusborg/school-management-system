/**
 * Computes OPAQUE credentials + KEM key pair for a given password.
 * Used by the C# DatabaseSeeder to bootstrap seed accounts.
 *
 * Usage: node scripts/compute-opaque-credentials.mjs <password>
 * Output: JSON {
 *   oprfSeed, clientPublicKey,
 *   kemPublicKey, kemEncryptedPrivateKey, kemNonce
 * }
 */
import { ristretto255_oprf, ed25519 } from '@noble/curves/ed25519.js';
import { hkdf } from '@noble/hashes/hkdf.js';
import { sha512 } from '@noble/hashes/sha2.js';
import { ml_kem768 } from '@noble/post-quantum/ml-kem.js';
import { webcrypto } from 'crypto';

const subtle = webcrypto.subtle;

const password = process.argv[2];
if (!password) {
    console.error('Usage: node compute-opaque-credentials.mjs <password>');
    process.exit(1);
}

const enc = new TextEncoder();
const passwordBytes = enc.encode(password);

// ── OPAQUE ────────────────────────────────────────────────────────────────────
const kp = ristretto255_oprf.oprf.generateKeyPair();
const oprfSeed = kp.secretKey;
const oprfOutput = ristretto255_oprf.oprf.evaluate(oprfSeed, passwordBytes);

// Ed25519 client public key (for OPAQUE challenge verification)
const ed25519Seed = hkdf(sha512, oprfOutput, enc.encode('ODIN-OPAQUE'), enc.encode('ed25519-key'), 32);
const clientPublicKey = ed25519.getPublicKey(ed25519Seed);

// ── KEM enc key (same as frontend deriveKemEncKey) ────────────────────────────
const kemEncKey = hkdf(sha512, oprfOutput, enc.encode('ODIN-OPAQUE'), enc.encode('ml-kem-key'), 32);

// ── ML-KEM-768 key pair ───────────────────────────────────────────────────────
const { publicKey: kemPublicKey, secretKey: kemPrivateKey } = ml_kem768.keygen();

// ── Encrypt private key with AES-256-GCM ─────────────────────────────────────
const nonce = webcrypto.getRandomValues(new Uint8Array(12));
const cryptoKey = await subtle.importKey('raw', kemEncKey, { name: 'AES-GCM' }, false, ['encrypt']);
const ciphertext = await subtle.encrypt({ name: 'AES-GCM', iv: nonce }, cryptoKey, kemPrivateKey);

const toB64 = (buf) => Buffer.from(buf).toString('base64');

console.log(JSON.stringify({
    oprfSeed:             toB64(oprfSeed),
    clientPublicKey:      toB64(clientPublicKey),
    kemPublicKey:         toB64(kemPublicKey),
    kemEncryptedPrivKey:  toB64(new Uint8Array(ciphertext)),
    kemNonce:             toB64(nonce)
}));
