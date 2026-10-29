namespace Shop.Pricing;

public enum CustomerTier { Standard, Silver, Gold }

public record OrderLine(string Sku, int Quantity, decimal UnitPrice);

public class OrderPricing
{
    private const decimal FreeShippingThreshold = 100m;
    private const decimal ShippingFee = 7.50m;

    public decimal Subtotal(IEnumerable<OrderLine> lines) =>
        lines.Sum(l => l.Quantity * l.UnitPrice);

    public decimal DiscountRate(CustomerTier tier, decimal subtotal)
    {
        var rate = tier switch
        {
            CustomerTier.Gold => 0.10m,
            CustomerTier.Silver => 0.05m,
            _ => 0m
        };

        // Orders of 500 or more get an extra 5 percent
        if (subtotal >= 500m)
        {
            rate += 0.05m;
        }

        return rate;
    }

    public decimal Total(IEnumerable<OrderLine> lines, CustomerTier tier)
    {
        var items = lines.ToList();
        var subtotal = Subtotal(items);
        var discounted = subtotal - subtotal * DiscountRate(tier, subtotal);
        var shipping = discounted >= FreeShippingThreshold ? 0m : ShippingFee;
        return Math.Round(discounted + shipping, 2, MidpointRounding.AwayFromZero);
    }

    public decimal AveragePricePerItem(IEnumerable<OrderLine> lines)
    {
        var items = lines.ToList();
        var count = items.Sum(l => l.Quantity);
        return count == 0 ? 0m : Subtotal(items) / count;
    }
}
