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
    public class PatientRepository : IPatientRepository
    {
        private readonly ProfilesDataContext _context;

        public PatientRepository(ProfilesDataContext context)
        {
            _context = context;
        }

        public async Task<PatientProfile?> GetByIdAsync(Guid id)
        {
            return await _context.Patients.FindAsync(id);
        }

        public async Task<IEnumerable<PatientProfile>> GetAllAsync(string? searchName)
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

            return await query.OrderBy(p => p.LastName).ToListAsync();
        }

        public async Task<IEnumerable<PatientProfile>> GetUnlinkedProfilesAsync()
        {
            // Берем только те профили, которые еще никто не залинковал на свой аккаунт (AC-4)
            return await _context.Patients.Where(p => !p.IsLinkedToAccount).ToListAsync();
        }

        public async Task AddAsync(PatientProfile profile)
        {
            await _context.Patients.AddAsync(profile);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(PatientProfile profile)
        {
            _context.Patients.Update(profile);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(PatientProfile profile)
        {
            _context.Patients.Remove(profile);
            await _context.SaveChangesAsync();
        }
    }
}
