using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Appointments.Domain;

namespace Appointments.Application.Interfaces
{
    public interface IAppointmentRepository
    {
        Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Appointment>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Appointment>> GetAllAsync(CancellationToken cancellationToken = default);
        Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default);
        Task UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default);
    }
}
