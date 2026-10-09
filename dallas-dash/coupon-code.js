/**
 * Coupon codes — one module shared by the game and the coupon service.
 *
 * Shape: KFC-7K4Q-9M2P-XW3R-H
 *   12 random symbols (60 bits from a CSPRNG) + 1 check symbol.
 *
 * The alphabet drops the look-alikes I, O, 0 and 1, which cause most misreads
 * when a code is typed from a screenshot. The check symbol catches every
 * single-symbol typo and 30 in 31 adjacent swaps, so the app can reject a
 * mistyped code instantly, before it ever reaches the database.
 *
 * Uniqueness and unguessability come from the 60 random bits (1.15e18 codes):
 * guessing a live code is hopeless, and the database's UNIQUE index is the
 * final guarantee. A code is never derived from the account id or a counter.
 */

export const ALPHABET = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
export const PREFIX = "KFC";
const BODY = 12;

function randomSymbols(n) {
  const bytes = new Uint8Array(n);
  globalThis.crypto.getRandomValues(bytes);
  let out = "";
  // 256 is a multiple of 32, so masking to 5 bits has no modulo bias
  for (let i = 0; i < n; i += 1) out += ALPHABET[bytes[i] & 31];
  return out;
}

/**
 * Weighted mod-32 checksum. Every weight is odd, so it is invertible mod 32 and
 * any single changed symbol always changes the check symbol.
 */
export function checkSymbol(body) {
  let sum = 0;
  for (let i = 0; i < body.length; i += 1) {
    const v = ALPHABET.indexOf(body[i]);
    if (v < 0) return null;
    sum += v * (2 * i + 1);
  }
  return ALPHABET[sum % 32];
}

export function generateCode() {
  const body = randomSymbols(BODY);
  return format(body + checkSymbol(body));
}

function format(raw) {
  return `${PREFIX}-${raw.slice(0, 4)}-${raw.slice(4, 8)}-${raw.slice(8, 12)}-${raw.slice(12)}`;
}

/**
 * Normalise what a customer typed: case, spaces, dashes and the look-alike
 * glyphs (O→0 is not in the alphabet, so O and 0 both fail cleanly).
 * Returns the canonical code, or null if it cannot be a real code.
 */
export function normalizeCode(input) {
  if (typeof input !== "string") return null;
  let s = input.toUpperCase().replace(/[\s-]/g, "");
  if (s.startsWith(PREFIX)) s = s.slice(PREFIX.length);
  if (s.length !== BODY + 1) return null;
  for (const ch of s) if (ALPHABET.indexOf(ch) < 0) return null;
  if (checkSymbol(s.slice(0, BODY)) !== s[BODY]) return null;
  return format(s);
}
