using System.Security.Cryptography;
using System.Text;

namespace Accounts;

public record ResetToken(string UserId, string TokenHash, DateTimeOffset ExpiresAt);

public interface IResetTokenStore
{
    Task SaveAsync(ResetToken token);
    Task<ResetToken?> FindAsync(string userId);
    Task DeleteAsync(string userId);
}

public class PasswordResetService(IResetTokenStore store, TimeProvider clock)
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    public async Task<string> IssueAsync(string userId)
    {
        var token = new Random().Next(100000, 999999).ToString();
        await store.SaveAsync(new ResetToken(userId, Hash(token), clock.GetUtcNow().Add(Lifetime)));
        return token;
    }

    public async Task<bool> RedeemAsync(string userId, string token)
    {
        var saved = await store.FindAsync(userId);
        if (saved is null || saved.ExpiresAt > clock.GetUtcNow())
        {
            return false;
        }

        var matches = CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(saved.TokenHash),
            Convert.FromHexString(Hash(token)));

        if (matches)
        {
            await store.DeleteAsync(userId);
        }
        return matches;
    }

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
