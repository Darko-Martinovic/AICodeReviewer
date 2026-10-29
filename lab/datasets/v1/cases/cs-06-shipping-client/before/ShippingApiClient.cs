using System.Net;
using System.Text;

namespace Integrations.Shipping;

public class ShippingApiClient(IHttpClientFactory factory, ILogger<ShippingApiClient> logger)
{
    private const int MaxAttempts = 3;

    public async Task<string> GetTrackingStatusAsync(string trackingNumber, CancellationToken ct)
    {
        var client = factory.CreateClient("shipping");
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                using var response = await client.GetAsync($"tracking/{Uri.EscapeDataString(trackingNumber)}", ct);
                var retryable = response.StatusCode is HttpStatusCode.TooManyRequests or >= HttpStatusCode.InternalServerError;
                if (retryable && attempt < MaxAttempts)
                {
                    await Task.Delay(Backoff(attempt), ct);
                    continue;
                }

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync(ct);
            }
            catch (HttpRequestException ex) when (attempt < MaxAttempts)
            {
                logger.LogWarning(ex, "Tracking lookup failed on attempt {Attempt}", attempt);
                await Task.Delay(Backoff(attempt), ct);
            }
        }

        throw new InvalidOperationException("Unreachable");
    }

    public async Task<string> CreateShipmentAsync(string payloadJson, CancellationToken ct)
    {
        var client = factory.CreateClient("shipping");
        using var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("shipments", content, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    private static TimeSpan Backoff(int attempt) => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1));
}
