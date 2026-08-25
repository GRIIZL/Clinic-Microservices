using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Profiles.Domain;

namespace Profiles.Application.Interfaces
{
    public interface IPatientRepository
    {
        Task<PatientProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<PatientProfile>> GetAllAsync(string? seatchName, CancellationToken cancellationToken = default);
        // Ищем только не связанные с аккаунтами профили для мэтчинга (AC-4)
        Task<IEnumerable<PatientProfile>> GetUnlinkedProfilesAsync(CancellationToken cancellationToken = default);
        Task AddAsync(PatientProfile profile, CancellationToken cancellationToken = default);
        Task UpdateAsync(PatientProfile profile, CancellationToken cancellationToken = default);
        Task DeleteAsync(PatientProfile profile, CancellationToken cancellationToken = default);
    }
}
