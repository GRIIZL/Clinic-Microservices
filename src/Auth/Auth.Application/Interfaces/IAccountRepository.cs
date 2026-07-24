using System.Threading.Tasks;
using Auth.Domain;

namespace Auth.Application.Interfaces
{
    public interface IAccountRepository
    {
        Task<Account?> GetByEmailAsync(string email);
        Task<Account?> GetByVerificationTokenAsync(string token);
        Task<Account?> GetByRefreshTokenAsync(string token);
        Task<bool> ExistByEmailAsync(string email);
        Task AddAsync(Account account);
        Task UpdateAsync(Account account);
    }
}