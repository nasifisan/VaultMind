/**
 * Generates a standard UUID string, using secure browser crypto APIs
 * with a fallback helper if ran in non-secure context.
 */
export const generateGuid = (): string => {
  if (typeof crypto !== "undefined" && crypto.randomUUID) {
    return crypto.randomUUID();
  }
  // Fallback using crypto.getRandomValues (cryptographically secure)
  const bytes = new Uint8Array(16);
  crypto.getRandomValues(bytes);
  // Set version 4 bits (byte 6: 0100xxxx)
  bytes[6] = (bytes[6] & 0x0f) | 0x40;
  // Set variant bits (byte 8: 10xxxxxx)
  bytes[8] = (bytes[8] & 0x3f) | 0x80;
  const hex = Array.from(bytes, (b) => b.toString(16).padStart(2, "0")).join(
    "",
  );
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
};
