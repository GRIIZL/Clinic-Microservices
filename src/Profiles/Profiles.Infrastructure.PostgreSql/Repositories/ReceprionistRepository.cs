using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task<ReceptionistProfile?> GetByIdAsync(Guid id) => await _context.Receptionists.FindAsync(id);

        public async Task<IEnumerable<ReceptionistProfile>> GetAllAsync() => await _context.Receptionists.OrderBy(r => r.LastName).ToListAsync();

        public async Task<bool> ExistsByEmailAsync(string email) => await _context.Receptionists.AnyAsync(r => r.Email.ToLower() == email.ToLower().Trim());

        public async Task AddAsync(ReceptionistProfile receptionist)
        {
            await _context.Receptionists.AddAsync(receptionist);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(ReceptionistProfile receptionist)
        {
            _context.Receptionists.Update(receptionist);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(ReceptionistProfile receptionist)
        {
            _context.Receptionists.Remove(receptionist);
            await _context.SaveChangesAsync();
        }
    }
}
