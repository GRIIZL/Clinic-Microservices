using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Profiles.Infrastructure.PostgreSql.Data;
using Microsoft.EntityFrameworkCore;
using Profiles.Application.Interfaces;
using Profiles.Application.Models;
using Profiles.Domain;

namespace Profiles.Infrastructure.PostgreSql.Repositories
{
    public class DoctorRepository : IDoctorRepository
    {
        private readonly ProfilesDataContext _context;

        public DoctorRepository(ProfilesDataContext context)
        {
            _context = context;
        }

        public async Task<DoctorProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Doctors.FindAsync(id, cancellationToken);
        }

        // Динамическая сборка SQL-запроса по критерию AC-9 (фильтрация несколькими полями)
        public async Task<IEnumerable<DoctorProfile>> GetFilteredDoctorsAsync(DoctorQueryParametersDto parameters, bool includeAllStatuses, CancellationToken cancellationToken = default)
        {
            var query = _context.Doctors.AsQueryable();

            // Если запрос идет от ПАЦИЕНТА (US-4 / US-19 / US-21 / US-25), фильтруем строго по "At work"
            // Если запрос от АДМИНА/РЕСЕПШЕНА (US-22 / US-24 / US-26), выводим все статусы
            if (!includeAllStatuses)
            {
                query = query.Where(d => d.Status == "At work");
            }

            // US-25 / US-26: Поиск по ФИО доктора
            if (!string.IsNullOrWhiteSpace(parameters.Name))
            {
                var searchName = parameters.Name.ToLower().Trim();
                query = query.Where(d => d.FirstName.ToLower().Contains(searchName) || 
                                         d.LastName.ToLower().Contains(searchName) || 
                                         d.MiddleName.ToLower().Contains(searchName));
            }

            // US-19 / US-24: Фильтрация по специализации
            if (!string.IsNullOrWhiteSpace(parameters.Specialization))
            {
                query = query.Where(d => d.Specialization.ToLower() == parameters.Specialization.ToLower().Trim());
            }

            // US-21 / US-22 / US-23: Фильтрация по строковому ID офиса из MongoDB
            if (!string.IsNullOrWhiteSpace(parameters.OfficeId))
            {
                query = query.Where(d => d.OfficeId == parameters.OfficeId.Trim());
            }

            return await query.OrderBy(d => d.LastName).ToListAsync(cancellationToken);
        }


        public async Task AddAsync(DoctorProfile doctor, CancellationToken cancellationToken = default)
        {
            await _context.Doctors.AddAsync(doctor, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(DoctorProfile doctor, CancellationToken cancellationToken = default)
        {
            _context.Doctors.Update(doctor);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            // В домене DoctorProfile у нас изначально не было поля Email, давай добавим его
            // или будем проверять уникальность через связь с Auth.
            // Но так как ProfilesDB изолирована, мы добавим колонку Email прямо в сущность доктора!
            return await _context.Doctors.AnyAsync(d => d.Email.ToLower() == email.ToLower().Trim(), cancellationToken);
        }
    }
}
