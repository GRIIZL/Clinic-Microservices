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
    public class PatientRepository : IPatientRepository
    {
        private readonly ProfilesDataContext _context;

        public PatientRepository(ProfilesDataContext context)
        {
            _context = context;
        }

        public async Task<PatientProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Patients.FindAsync(id, cancellationToken);
        }

        public async Task<IEnumerable<PatientProfile>> GetAllAsync(string? searchName, CancellationToken cancellationToken = default)
        {
            var query = _context.Patients.AsQueryable();

            // Реализация US-50 (Поиск по имени пациента админом)
            if (!string.IsNullOrWhiteSpace(searchName))
            {
                var name = searchName.ToLower().Trim();
                query = query.Where(p => p.FirstName.ToLower().Contains(name) || 
                                         p.LastName.ToLower().Contains(name) || 
                                         p.MiddleName.ToLower().Contains(name));
            }

            return await query.OrderBy(p => p.LastName).ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<PatientProfile>> GetUnlinkedProfilesAsync(CancellationToken cancellationToken = default)
        {
            // Берем только те профили, которые еще никто не залинковал на свой аккаунт (AC-4)
            return await _context.Patients.Where(p => !p.IsLinkedToAccount).ToListAsync(cancellationToken);
        }

        public async Task AddAsync(PatientProfile profile, CancellationToken cancellationToken = default)
        {
            await _context.Patients.AddAsync(profile, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(PatientProfile profile, CancellationToken cancellationToken = default)
        {
            _context.Patients.Update(profile);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(PatientProfile profile, CancellationToken cancellationToken = default)
        {
            _context.Patients.Remove(profile);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
