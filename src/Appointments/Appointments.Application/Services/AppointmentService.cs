using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Appointments.Application.Interfaces;
using Appointments.Application.Models;
using Appointments.Domain;

namespace Appointments.Application.Services
{
    public class AppointmentService
    {
        private readonly IAppointmentRepository _repository;

        public AppointmentService(IAppointmentRepository repository)
        {
            _repository = repository;
        }

        // US-6 (AC-5): Создание записи на прием
        public async Task<Appointment> CreateAppointmentAsync(CreateAppointmentDto dto, CancellationToken cancellationToken = default)
        {
            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = dto.PatientId,
                SpecializationId = dto.SpecializationId,
                DoctorId = dto.DoctorId,
                ServiceId = dto.ServiceId,
                OfficeId = dto.OfficeId,
                Date = dto.Date.Date, // Сохраняем только чистую дату без времени
                Timeslot = dto.Timeslot,
                Status = "Pending", // Начальный статус по умолчанию
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(appointment, cancellationToken);
            return appointment;
        }

        public async Task<IEnumerable<Appointment>> GetPatientHistoryAsync(Guid patientId, CancellationToken cancellationToken = default)
        {
            return await _repository.GetByPatientIdAsync(patientId, cancellationToken);
        }
    }
}
