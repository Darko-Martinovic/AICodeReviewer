using Microsoft.EntityFrameworkCore;

namespace Sales.Reports;

public record SalesRow(string Customer, int Orders, decimal Revenue);

public class SalesReportService(SalesDbContext db)
{
    /// <summary>
    /// Sales from the first day through the last day of the range, both included
    /// </summary>
    public async Task<List<SalesRow>> ByCustomerAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var start = from.ToDateTime(TimeOnly.MinValue);
        var end = to.ToDateTime(TimeOnly.MinValue);

        var orders = await db.Orders
            .Where(o => o.PlacedAt >= start && o.PlacedAt < end)
            .ToListAsync(ct);

        var rows = new List<SalesRow>();
        foreach (var group in orders.GroupBy(o => o.CustomerId))
        {
            var customer = await db.Customers.SingleAsync(c => c.Id == group.Key, ct);
            rows.Add(new SalesRow(customer.Name, group.Count(), group.Sum(o => o.Total)));
        }
        return rows.OrderByDescending(r => r.Revenue).ToList();
    }

    public async Task<decimal> AverageOrderValueAsync(int customerId, CancellationToken ct)
    {
        var totals = await db.Orders
            .Where(o => o.CustomerId == customerId)
            .Select(o => o.Total)
            .ToListAsync(ct);

        return totals.Average();
    }
}
