import { WORDLIST } from './wordlist'

/**
 * Generate a cryptographically random passphrase using the EFF large wordlist.
 * Uses crypto.getRandomValues — no Node.js dependencies, safe in the browser.
 *
 * @param wordCount  Number of words (default 5)
 * @param separator  Separator between words (default '-')
 */
export function generatePassphrase(wordCount = 5, separator = '-'): string {
  const words: string[] = []
  const listSize = WORDLIST.length // 7776

  for (let i = 0; i < wordCount; i++) {
    // Rejection sampling to avoid modulo bias
    const needed = Math.ceil(Math.log2(listSize) / 8) + 1 // extra byte for safety
    let index: number
    const max = Math.floor(0x100000000 / listSize) * listSize
    do {
      const buf = crypto.getRandomValues(new Uint32Array(1))
      index = buf[0]
    } while (index >= max)
    words.push(WORDLIST[index % listSize])
  }

  // Append a random 2-digit number (10–99) for extra entropy
  const num = (crypto.getRandomValues(new Uint8Array(1))[0] % 90) + 10

  return [...words, String(num)].join(separator)
}
