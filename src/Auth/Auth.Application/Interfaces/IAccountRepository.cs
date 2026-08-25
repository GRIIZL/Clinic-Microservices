using System.Threading;
using System.Threading.Tasks;
using Auth.Domain;

namespace Auth.Application.Interfaces
{
    public interface IAccountRepository
    {
        Task<Account?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<Account?> GetByVerificationTokenAsync(string token, CancellationToken cancellationToken = default);
        Task<Account?> GetByRefreshTokenAsync(string token, CancellationToken cancellationToken = default);
        Task<bool> ExistByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task AddAsync(Account account, CancellationToken cancellationToken = default);
        Task UpdateAsync(Account account, CancellationToken cancellationToken = default);
    }
}