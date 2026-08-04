using System.Collections.Generic;
using System.Threading.Tasks;
using Offices.Domain;

namespace Offices.Application.Interfaces
{
    public interface IOfficeRepository
    {
        Task<Office?> GetByIdAsync(string id);
        Task<IEnumerable<Office>> GetAllAsync();
        Task AddAsync(Office office);
        Task UpdateAsync(Office office);
    }
}