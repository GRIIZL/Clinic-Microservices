using System;
using System.Threading.Tasks;

namespace Auth.Application.Interfaces
{
    public interface ITokenBlacklistService
    {
        Task BlacklistTokenAsync(string token, DateTime expiryTime);
        Task<bool> IsTokenBlacklistedAsync(string token);
    }
}