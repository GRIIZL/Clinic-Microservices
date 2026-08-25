using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Offices.Domain;

namespace Offices.Application.Interfaces
{
    public interface IOfficeRepository
    {
        Task<Office?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Office>> GetAllAsync(CancellationToken cancellationToken = default);
        Task AddAsync(Office office, CancellationToken cancellationToken = default);
        Task UpdateAsync(Office office, CancellationToken cancellationToken = default);
    }
}