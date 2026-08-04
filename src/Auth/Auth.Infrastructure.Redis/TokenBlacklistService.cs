using System;
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

        public async Task BlacklistTokenAsync(string token, DateTime expiryTime)
        {
            var livetime = expiryTime - DateTime.UtcNow;
            if (livetime <= TimeSpan.Zero) return;

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = livetime 
            };

            await _cache.SetStringAsync(token, "true", options);
        }

        public async Task<bool> IsTokenBlacklistedAsync(string token)
        {
            var result = await _cache.GetStringAsync(token);
            return result != null;
        }
    }
}