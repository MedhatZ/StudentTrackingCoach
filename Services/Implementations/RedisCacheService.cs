using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using StudentTrackingCoach.Services.Interfaces;

namespace StudentTrackingCoach.Services.Implementations
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _distributedCache;
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        public RedisCacheService(IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache;
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            var cached = await _distributedCache.GetStringAsync(key, cancellationToken);
            if (string.IsNullOrWhiteSpace(cached))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(cached, _jsonOptions);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
        {
            var payload = JsonSerializer.Serialize(value, _jsonOptions);
            await _distributedCache.SetStringAsync(
                key,
                payload,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = ttl
                },
                cancellationToken);
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
        }

        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            var value = await _distributedCache.GetAsync(key, cancellationToken);
            return value != null;
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl, CancellationToken cancellationToken = default)
        {
            var existing = await GetAsync<T>(key, cancellationToken);
            if (existing != null)
            {
                return existing;
            }

            var created = await factory();
            await SetAsync(key, created, ttl, cancellationToken);
            return created;
        }
    }
}
