const AMOUNT = /^(\d{1,9})(?:[.,](\d{1,2}))?$/;

/** Parses "12", "12.5" or "12,50" into cents; returns null for anything else. */
export function parseCents(input: string): number | null {
  const match = AMOUNT.exec(input.trim());
  if (!match) {
    return null;
  }
  const whole = Number(match[1]);
  const fraction = Number((match[2] ?? "0").padEnd(2, "0"));
  return whole * 100 + fraction;
}

export function formatCents(cents: number, currency: string, locale: string): string {
  if (!Number.isSafeInteger(cents)) {
    throw new RangeError(`Amount must be a whole number of cents, got ${cents}`);
  }
  return new Intl.NumberFormat(locale, { style: "currency", currency }).format(cents / 100);
}

/** Splits cents into parts that differ by at most one cent and add up to the total. */
export function splitCents(total: number, parts: number): number[] {
  if (!Number.isSafeInteger(total) || !Number.isInteger(parts) || parts < 1) {
    throw new RangeError("Total must be whole cents and parts a positive integer");
  }
  const base = Math.floor(total / parts);
  const remainder = total - base * parts;
  return Array.from({ length: parts }, (_, i) => base + (i < remainder ? 1 : 0));
}
