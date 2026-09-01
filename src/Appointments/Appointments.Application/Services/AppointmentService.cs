using System;
using System.Collections.Generic;
using System.Linq;
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

        // US-15: Удаление (отмена) записи ресепшионистом
        public async Task<bool> CancelAppointmentAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var appointment = await _repository.GetByIdAsync(id, cancellationToken);
            if (appointment == null) return false;

            await _repository.DeleteAsync(appointment, cancellationToken);
            return true;
        }

        // US-10: Получение расписания доктора на выбранную дату с сортировкой по времени слота (AC-3)
        public async Task<IEnumerable<AppointmentScheduleDto>> GetDoctorScheduleAsync(Guid doctorId, DateTime date, CancellationToken cancellationToken = default)
        {
            var appointments = await _repository.GetByDoctorIdAsync(doctorId, date, cancellationToken);
            
            var schedule = new List<AppointmentScheduleDto>();
            foreach (var a in appointments)
            {
                schedule.Add(new AppointmentScheduleDto
                {
                    Id = a.Id,
                    PatientId = a.PatientId,
                    Timeslot = a.Timeslot,
                    Status = a.Status,
                    Date = a.Date,
                    HasResult = await _repository.HasResultAsync(a.Id, cancellationToken)
                });
            }

            // Сортировка по времени слота (по возрастанию - AC-3)
            return schedule.OrderBy(s => s.Timeslot).ToList();
        }

        // US-58: Добавление медицинского заключения доктором
        public async Task<bool> CreateResultAsync(CreateAppointmentResultDto dto, CancellationToken cancellationToken = default)
        {
            var appointment = await _repository.GetByIdAsync(dto.AppointmentId, cancellationToken);
            if (appointment == null) return false;

            var result = new AppointmentResult
            {
                AppointmentId = dto.AppointmentId,
                Complaints = dto.Complaints.Trim(),
                Conclusion = dto.Conclusion.Trim(),
                Recommendations = dto.Recommendations.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddResultAsync(result, cancellationToken);
            
            // Меняем статус приема на "Completed" по завершению
            appointment.Status = "Completed";
            await _repository.UpdateAsync(appointment, cancellationToken);
            
            return true;
        }

        // US-60 / US-61: Получение детального медицинского заключения
        public async Task<AppointmentResult?> GetResultDetailsAsync(Guid appointmentId, CancellationToken cancellationToken = default)
        {
            return await _repository.GetResultByAppointmentIdAsync(appointmentId, cancellationToken);
        }

        // US-59: Редактирование заключения доктором
        public async Task<bool> UpdateResultAsync(Guid appointmentId, UpdateAppointmentResultDto dto, CancellationToken cancellationToken = default)
        {
            var result = await _repository.GetResultByAppointmentIdAsync(appointmentId, cancellationToken);
            if (result == null) return false;

            result.Complaints = dto.Complaints.Trim();
            result.Conclusion = dto.Conclusion.Trim();
            result.Recommendations = dto.Recommendations.Trim();

            await _repository.UpdateResultAsync(result, cancellationToken);
            return true;
        }

        // US-62: Скачивание медицинского результата в PDF-формате (генерация документа делегирована SimplePdfGenerator)
        public async Task<byte[]> GenerateAppointmentResultPdfAsync(Guid appointmentId, CancellationToken cancellationToken = default)
        {
            var app = await _repository.GetByIdAsync(appointmentId, cancellationToken);
            var res = await _repository.GetResultByAppointmentIdAsync(appointmentId, cancellationToken);

            if (app == null || res == null) return Array.Empty<byte>();

            // Формируем структуру документа по требованиям US-62 / AC-3
            return SimplePdfGenerator.Generate(
                "INNOWISE CLINIC - MEDICAL REPORT",
                new List<(string, string)>
                {
                    ("Date of Appointment", app.Date.ToShortDateString()),
                    ("Timeslot", app.Timeslot),
                    ("Status", app.Status),
                    ("Complaints", res.Complaints),
                    ("Conclusion", res.Conclusion),
                    ("Recommendations", res.Recommendations)
                });
        }

    }
}
