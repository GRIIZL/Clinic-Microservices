using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Auth.Application.Interfaces;
using Auth.Domain;
using Auth.Infrastructure.PostgreSql.Data;

namespace Auth.Infrastructure.PostgreSql.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly DataContext _context;

        public AccountRepository(DataContext context)
        {
            _context = context;
        }

        public async Task<Account?> GetByEmailAsync(string email)
        {
            return await _context.Accounts
                .FirstOrDefaultAsync(a => a.Email.ToLower() == email.ToLower().Trim());
        }

        public async Task<Account?> GetByVerificationTokenAsync(string token)
        {
            return await _context.Accounts
                .FirstOrDefaultAsync(a => a.VerificationToken == token);
        }

        public async Task<Account?> GetByRefreshTokenAsync(string token)
        {
            return await _context.Accounts
                .FirstOrDefaultAsync(a => a.RefreshToken == token);
        }

        public async Task<bool> ExistByEmailAsync(string email)
        {
            return await _context.Accounts
                .AnyAsync(a => a.Email.ToLower() == email.ToLower().Trim());
        }

        public async Task AddAsync(Account account)
        {
            await _context.Accounts.AddAsync(account);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Account account)
        {
            _context.Accounts.Update(account);
            await _context.SaveChangesAsync();
        }
    }
}