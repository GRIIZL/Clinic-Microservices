using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Profiles.Domain;

namespace Profiles.Application.Interfaces
{
    public interface IReceptionistRepository
    {
        Task<ReceptionistProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<ReceptionistProfile>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task AddAsync(ReceptionistProfile receptionist, CancellationToken cancellationToken = default);
        Task UpdateAsync(ReceptionistProfile receptionist, CancellationToken cancellationToken = default);
        Task DeleteAsync(ReceptionistProfile receptionist, CancellationToken cancellationToken = default);
    }
}
