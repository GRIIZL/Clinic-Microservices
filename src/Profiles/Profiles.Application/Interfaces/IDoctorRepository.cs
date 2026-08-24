using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Profiles.Application.Models;
using Profiles.Domain;

namespace Profiles.Application.Interfaces
{
    public interface IDoctorRepository
    {
        Task<DoctorProfile?> GetByIdAsync(Guid id);
        Task<IEnumerable<DoctorProfile>> GetFilteredDoctorsAsync(DoctorQueryParametersDto parameters, bool includeAllStatuses);
        Task AddAsync(DoctorProfile doctor);
        Task UpdateAsync(DoctorProfile doctor);
        Task<bool> ExistsByEmailAsync(string email);
    }
}