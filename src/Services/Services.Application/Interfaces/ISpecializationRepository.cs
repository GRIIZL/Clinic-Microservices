using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Services.Domain;

namespace Services.Application.Interfaces
{
    public interface ISpecializationRepository
    {
        Task<Specialization?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Specialization>> GetAllAsync(CancellationToken cancellationToken = default);
        Task AddAsync(Specialization specialization, CancellationToken cancellationToken = default);
        Task UpdateAsync(Specialization specialization, CancellationToken cancellationToken = default);
    }
}
