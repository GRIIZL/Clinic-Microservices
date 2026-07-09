using System;
using System.Collections.Concurrent;

namespace AuthorizationAPI.Services
{
    public class TokenBlacklistService
    {
        private readonly ConcurrentDictionary<string, DateTime> _blacklistedTokens = new();
        
        public void BlacklistToken(string token, DateTime expiryTime)
        {
            _blacklistedTokens.TryAdd(token, expiryTime);
        }

        public bool IsTokenBlacklisted(string token)
        {
            if(_blacklistedTokens.TryGetValue(token, out var expiryTIme))
            {
                if(expiryTIme < DateTime.UtcNow)
                {
                    _blacklistedTokens.TryRemove(token, out _);
                    return false;
                }
                return true;
            }
            return false;
        }
    }
}