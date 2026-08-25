using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Auth.Application.Interfaces;

namespace Auth.Infrastructure.Redis
{
    public class TokenBlacklistService : ITokenBlacklistService
    {
        private readonly IDistributedCache _cache;

        public TokenBlacklistService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task BlacklistTokenAsync(string token, DateTime expiryTime, CancellationToken cancellationToken = default)
        {
            var livetime = expiryTime - DateTime.UtcNow;
            if (livetime <= TimeSpan.Zero) return;

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = livetime 
            };

            await _cache.SetStringAsync(token, "true", options, cancellationToken);
        }

        public async Task<bool> IsTokenBlacklistedAsync(string token, CancellationToken cancellationToken = default)
        {
            var result = await _cache.GetStringAsync(token, cancellationToken);
            return result != null;
        }
    }
}