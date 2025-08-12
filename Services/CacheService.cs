using AICodeReviewer.Models;
using AICodeReviewer.Services.Interfaces;
using System.Collections.Concurrent;
using System.Text.Json;

namespace AICodeReviewer.Services
{
    /// <summary>
    /// In-memory cache service for code review results
    /// </summary>
    public class CacheService : ICacheService
    {
        private readonly ConcurrentDictionary<string, CachedReviewResult> _commitCache = new();
        private readonly ConcurrentDictionary<int, CachedReviewResult> _pullRequestCache = new();
        private readonly TimeSpan _cacheExpirationTime;

        public CacheService(IConfiguration configuration)
        {
            // Get cache expiration from config (default 24 hours)
            var expirationHours = configuration.GetValue<int>("Cache:ExpirationHours", 24);
            _cacheExpirationTime = TimeSpan.FromHours(expirationHours);
        }

        public Task<CodeReviewResult?> GetCommitReviewAsync(string commitSha)
        {
            if (_commitCache.TryGetValue(commitSha, out var cached))
            {
                if (cached.ExpiresAt > DateTime.UtcNow)
                {
                    return Task.FromResult<CodeReviewResult?>(cached.Result);
                }

                // Remove expired entry
                _commitCache.TryRemove(commitSha, out _);
            }

            return Task.FromResult<CodeReviewResult?>(null);
        }

        public Task SetCommitReviewAsync(string commitSha, CodeReviewResult result)
        {
            var cached = new CachedReviewResult
            {
                Result = result,
                CachedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(_cacheExpirationTime)
            };

            _commitCache.AddOrUpdate(commitSha, cached, (key, oldValue) => cached);
            return Task.CompletedTask;
        }

        public Task<CodeReviewResult?> GetPullRequestReviewAsync(int pullRequestNumber)
        {
            if (_pullRequestCache.TryGetValue(pullRequestNumber, out var cached))
            {
                if (cached.ExpiresAt > DateTime.UtcNow)
                {
                    return Task.FromResult<CodeReviewResult?>(cached.Result);
                }

                // Remove expired entry
                _pullRequestCache.TryRemove(pullRequestNumber, out _);
            }

            return Task.FromResult<CodeReviewResult?>(null);
        }

        public Task SetPullRequestReviewAsync(int pullRequestNumber, CodeReviewResult result)
        {
            var cached = new CachedReviewResult
            {
                Result = result,
                CachedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(_cacheExpirationTime)
            };

            _pullRequestCache.AddOrUpdate(pullRequestNumber, cached, (key, oldValue) => cached);
            return Task.CompletedTask;
        }

        public Task<bool> HasCommitReviewAsync(string commitSha)
        {
            if (_commitCache.TryGetValue(commitSha, out var cached))
            {
                if (cached.ExpiresAt > DateTime.UtcNow)
                {
                    return Task.FromResult(true);
                }

                // Remove expired entry
                _commitCache.TryRemove(commitSha, out _);
            }

            return Task.FromResult(false);
        }

        public Task<bool> HasPullRequestReviewAsync(int pullRequestNumber)
        {
            if (_pullRequestCache.TryGetValue(pullRequestNumber, out var cached))
            {
                if (cached.ExpiresAt > DateTime.UtcNow)
                {
                    return Task.FromResult(true);
                }

                // Remove expired entry
                _pullRequestCache.TryRemove(pullRequestNumber, out _);
            }

            return Task.FromResult(false);
        }

        public Task ClearAllAsync()
        {
            _commitCache.Clear();
            _pullRequestCache.Clear();
            return Task.CompletedTask;
        }

        public Task ClearCommitReviewAsync(string commitSha)
        {
            _commitCache.TryRemove(commitSha, out _);
            return Task.CompletedTask;
        }

        public Task ClearPullRequestReviewAsync(int pullRequestNumber)
        {
            _pullRequestCache.TryRemove(pullRequestNumber, out _);
            return Task.CompletedTask;
        }

        private class CachedReviewResult
        {
            public CodeReviewResult Result { get; set; } = null!;
            public DateTime CachedAt { get; set; }
            public DateTime ExpiresAt { get; set; }
        }
    }
}
