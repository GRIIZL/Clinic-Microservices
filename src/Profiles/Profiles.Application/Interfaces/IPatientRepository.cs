using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Profiles.Domain;

namespace Profiles.Application.Interfaces
{
    public interface IPatientRepository
    {
        Task<PatientProfile?> GetByIdAsync(Guid id);
        Task<IEnumerable<PatientProfile>> GetAllAsync(string? seatchName);
        // Ищем только не связанные с аккаунтами профили для мэтчинга (AC-4)
        Task<IEnumerable<PatientProfile>> GetUnlinkedProfilesAsync();
        Task AddAsync(PatientProfile profile);
        Task UpdateAsync(PatientProfile profile);
        Task DeleteAsync(PatientProfile profile);
    }
}
