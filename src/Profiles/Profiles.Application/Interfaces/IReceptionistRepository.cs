using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Profiles.Domain;

namespace Profiles.Application.Interfaces
{
    public interface IReceptionistRepository
    {
        Task<ReceptionistProfile?> GetByIdAsync(Guid id);
        Task<IEnumerable<ReceptionistProfile>> GetAllAsync();
        Task<bool> ExistsByEmailAsync(string email);
        Task AddAsync(ReceptionistProfile receptionist);
        Task UpdateAsync(ReceptionistProfile receptionist);
        Task DeleteAsync(ReceptionistProfile receptionist);
    }
}
