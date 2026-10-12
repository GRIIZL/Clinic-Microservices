using System;
using System.Threading;
using System.Threading.Tasks;
using Auth.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace Auth.Infrastructure.Redis
{
    /// <summary>
    /// Реализация ICacheService поверх IDistributedCache (StackExchange Redis).
    /// Используется для кеша результатов проверки существования email:
    /// частые чтения из БД при регистрации заменяются одним быстрым GET в Redis.
    /// </summary>
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _cache;

        public RedisCacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            return await _cache.GetStringAsync(key, cancellationToken);
        }

        public async Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken cancellationToken = default)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            };
            await _cache.SetStringAsync(key, value, options, cancellationToken);
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            await _cache.RemoveAsync(key, cancellationToken);
        }
    }
}
