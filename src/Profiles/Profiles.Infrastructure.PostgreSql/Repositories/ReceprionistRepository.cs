using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Profiles.Application.Interfaces;
using Profiles.Domain;
using Profiles.Infrastructure.PostgreSql.Data;

namespace Profiles.Infrastructure.PostgreSql.Repositories
{
    public class ReceptionistRepository : IReceptionistRepository
    {
        private readonly ProfilesDataContext _context;

        public ReceptionistRepository(ProfilesDataContext context)
        {
            _context = context;
        }

        public async Task<ReceptionistProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => await _context.Receptionists.FindAsync(id, cancellationToken);

        public async Task<IEnumerable<ReceptionistProfile>> GetAllAsync(CancellationToken cancellationToken = default) => await _context.Receptionists.OrderBy(r => r.LastName).ToListAsync(cancellationToken);

        public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) => await _context.Receptionists.AnyAsync(r => r.Email.ToLower() == email.ToLower().Trim(), cancellationToken);

        public async Task AddAsync(ReceptionistProfile receptionist, CancellationToken cancellationToken = default)
        {
            await _context.Receptionists.AddAsync(receptionist, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(ReceptionistProfile receptionist, CancellationToken cancellationToken = default)
        {
            _context.Receptionists.Update(receptionist);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(ReceptionistProfile receptionist, CancellationToken cancellationToken = default)
        {
            _context.Receptionists.Remove(receptionist);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
