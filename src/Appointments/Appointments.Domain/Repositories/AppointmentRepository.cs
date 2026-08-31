using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Appointments.Application.Interfaces;
using Appointments.Domain;
using Appointments.Infrastructure.PostgreSql.Data;

namespace Appointments.Infrastructure.PostgreSql.Repositories
{
    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly AppointmentsDataContext _context;

        public AppointmentRepository(AppointmentsDataContext context)
        {
            _context = context;
        }

        public async Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Appointments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<Appointment>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default)
        {
            return await _context.Appointments
                .Where(a => a.PatientId == patientId)
                .OrderByDescending(a => a.Date)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Appointment>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Appointments
                .OrderByDescending(a => a.Date)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default)
        {
            await _context.Appointments.AddAsync(appointment, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default)
        {
            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
