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

        // US-7: Алгоритмический расчет свободных временных слотов с учетом категорий услуг
        public async Task<AvailableSlotsResponseDto> GetAvailableSlotsAsync(
            Guid doctorId, 
            DateTime date, 
            string categoryName, 
            CancellationToken cancellationToken = default)
        {
            // Бизнес-правила сетки приёма (AC-1, AC-5, AC-6, AC-7)
            const int SlotMinutes = 10;                       // Шаг сетки 10 минут
            const int WorkStartHour = 9;                      // Рабочий день: с 09:00
            const int WorkEndHour = 17;                       // ...до 17:00

            var response = new AvailableSlotsResponseDto 
            { 
                Date = date.ToShortDateString() 
            };

            // Сколько слотов подряд нужно для нашей услуги
            response.RequiredSlotsCount = categoryName switch
            {
                "Analyses" => 1,      // 10 минут (AC-5)
                "Consultations" => 2, // 20 минут (AC-6)
                "Diagnostics" => 3,   // 30 минут (AC-7)
                _ => 2                // Дефолт 2 слота
            };

            // 1. Извлекаем из базы данных все существующие записи к этому врачу на выбранную дату
            var existingAppointments = await _repository.GetByDoctorIdAsync(doctorId, date, cancellationToken);
            
            // Составляем хэш-сет всех занятых 10-минутных точек времени в этот день.
            // ВАЖНО: отменённые записи слот НЕ занимают — время снова доступно для записи
            var busySlots = new HashSet<TimeSpan>();
            foreach (var app in existingAppointments.Where(a => a.Status != "Canceled"))
            {
                // Парсим строку слота вида "10:30 - 11:00" или "09:00 - 09:20"
                var parts = app.Timeslot.Split('-');
                if (parts.Length == 2 && TimeSpan.TryParse(parts[0].Trim(), out var startTime) 
                                      && TimeSpan.TryParse(parts[1].Trim(), out var endTime))
                {
                    // Маркируем все 10-минутные интервалы внутри этой записи как занятые
                    var current = startTime;
                    while (current < endTime)
                    {
                        busySlots.Add(current);
                        current = current.Add(TimeSpan.FromMinutes(SlotMinutes));
                    }
                }
            }

            // 2. Пробегаем по рабочему дню и ищем окна нужной длины
            var workStart = new TimeSpan(WorkStartHour, 0, 0);
            var workEnd = new TimeSpan(WorkEndHour, 0, 0);
            var step = TimeSpan.FromMinutes(SlotMinutes);

            var checkTime = workStart;
            while (checkTime.Add(TimeSpan.FromMinutes(response.RequiredSlotsCount * SlotMinutes)) <= workEnd)
            {
                bool isWindowFree = true;
                
                // Проверяем, свободны ли все N слотов подряд, начиная с этой минуты
                for (int i = 0; i < response.RequiredSlotsCount; i++)
                {
                    var slotToCheck = checkTime.Add(TimeSpan.FromMinutes(i * SlotMinutes));
                    if (busySlots.Contains(slotToCheck))
                    {
                        isWindowFree = false;
                        break;
                    }
                }

                // Если все слоты подряд свободны — эта точка времени доступна для записи! (AC-3, AC-4)
                if (isWindowFree)
                {
                    response.AvailableStartTimes.Add(checkTime.ToString(@"hh\:mm"));
                }

                checkTime = checkTime.Add(step); // Сдвигаемся на 10 минут вперед
            }

            return response;
        }
    }
}
