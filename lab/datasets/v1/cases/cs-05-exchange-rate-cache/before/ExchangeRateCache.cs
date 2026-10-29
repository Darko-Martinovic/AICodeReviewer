using System.Collections.Concurrent;
using System.Net.Http.Json;

namespace Payments.Rates;

public record ExchangeRate(string From, string To, decimal Rate, DateTimeOffset FetchedAt);

public class ExchangeRateCache(HttpClient http, TimeProvider clock)
{
    private static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(10);
    private readonly ConcurrentDictionary<string, ExchangeRate> _rates = new();

    public async Task<decimal> ConvertAsync(decimal amount, string from, string to, CancellationToken ct)
    {
        if (from == to)
        {
            return amount;
        }

        var rate = await GetRateAsync(from, to, ct);
        return Math.Round(amount * rate.Rate, 2, MidpointRounding.ToEven);
    }

    public async Task<ExchangeRate> GetRateAsync(string from, string to, CancellationToken ct)
    {
        var key = $"{from}:{to}";
        if (_rates.TryGetValue(key, out var cached) && clock.GetUtcNow() - cached.FetchedAt < MaxAge)
        {
            return cached;
        }

        var response = await http.GetFromJsonAsync<RateResponse>(
            $"rates?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}", ct)
            ?? throw new InvalidOperationException($"No rate for {key}");
        var rate = new ExchangeRate(from, to, response.Rate, clock.GetUtcNow());
        _rates[key] = rate;
        return rate;
    }

    private record RateResponse(decimal Rate);
}
