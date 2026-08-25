using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Profiles.Application.Models;
using Profiles.Domain;

namespace Profiles.Application.Interfaces
{
    public interface IDoctorRepository
    {
        Task<DoctorProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<DoctorProfile>> GetFilteredDoctorsAsync(DoctorQueryParametersDto parameters, bool includeAllStatuses, CancellationToken cancellationToken = default);
        Task AddAsync(DoctorProfile doctor, CancellationToken cancellationToken = default);
        Task UpdateAsync(DoctorProfile doctor, CancellationToken cancellationToken = default);
        Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    }
}