using System;
using System.Threading;
using System.Threading.Tasks;

namespace Auth.Application.Interfaces
{
    public interface ITokenBlacklistService
    {
        Task BlacklistTokenAsync(string token, DateTime expiryTime, CancellationToken cancellationToken = default);
        Task<bool> IsTokenBlacklistedAsync(string token, CancellationToken cancellationToken = default);
    }
}