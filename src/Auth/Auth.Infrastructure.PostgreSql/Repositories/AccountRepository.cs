using System.Threading;
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

        public async Task<Account?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _context.Accounts
                .FirstOrDefaultAsync(a => a.Email.ToLower() == email.ToLower().Trim(), cancellationToken);
        }

        public async Task<Account?> GetByVerificationTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            return await _context.Accounts
                .FirstOrDefaultAsync(a => a.VerificationToken == token, cancellationToken);
        }

        public async Task<Account?> GetByRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            return await _context.Accounts
                .FirstOrDefaultAsync(a => a.RefreshToken == token, cancellationToken);
        }

        public async Task<bool> ExistByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _context.Accounts
                .AnyAsync(a => a.Email.ToLower() == email.ToLower().Trim(), cancellationToken);
        }

        public async Task AddAsync(Account account, CancellationToken cancellationToken = default)
        {
            await _context.Accounts.AddAsync(account, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(Account account, CancellationToken cancellationToken = default)
        {
            _context.Accounts.Update(account);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}