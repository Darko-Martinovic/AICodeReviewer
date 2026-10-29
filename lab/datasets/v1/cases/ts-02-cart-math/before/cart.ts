export interface CartItem {
  sku: string;
  unitPriceCents: number;
  quantity: number;
}

export interface Coupon {
  code: string;
  percentOff: number;
  minimumCents: number;
}

export function subtotalCents(items: CartItem[]): number {
  return items.reduce((sum, item) => sum + item.unitPriceCents * item.quantity, 0);
}

export function applyCoupon(subtotal: number, coupon: Coupon | undefined): number {
  if (!coupon || subtotal < coupon.minimumCents) {
    return subtotal;
  }
  const discount = Math.round((subtotal * coupon.percentOff) / 100);
  return Math.max(0, subtotal - discount);
}

export function taxCents(amountCents: number, ratePercent: number): number {
  return Math.round((amountCents * ratePercent) / 100);
}

export function parseQuantity(input: string): number {
  const quantity = Number(input);
  if (!Number.isInteger(quantity) || quantity < 1 || quantity > 99) {
    throw new RangeError(`Quantity must be between 1 and 99, got "${input}"`);
  }
  return quantity;
}

export function formatCents(cents: number, currency = "EUR", locale = "en-IE"): string {
  return new Intl.NumberFormat(locale, { style: "currency", currency }).format(cents / 100);
}
