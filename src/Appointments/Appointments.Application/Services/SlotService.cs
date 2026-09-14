using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Appointments.Application.Configuration;
using Appointments.Application.Interfaces;
using Appointments.Application.Models;
using Appointments.Domain;
using Microsoft.Extensions.Options;

namespace Appointments.Application.Services
{
    /// <summary>
    /// Алгоритм расчёта свободных временных слотов (US-7).
    /// Все бизнес-параметры (шаг сетки, рабочие часы, длительность по категориям)
    /// приходят из конфигурации через IOptions — никакой магии в коде.
    /// </summary>
    public class SlotService : ISlotService
    {
        private readonly IAppointmentRepository _repository;
        private readonly AppointmentSlotsOptions _options;

        public SlotService(IAppointmentRepository repository, IOptions<AppointmentSlotsOptions> options)
        {
            _repository = repository;
            _options = options.Value;
        }

        /// <inheritdoc/>
        public async Task<AvailableSlotsResponseDto> GetAvailableSlotsAsync(
            Guid doctorId,
            DateTime date,
            string categoryName,
            CancellationToken cancellationToken = default)
        {
            var response = new AvailableSlotsResponseDto
            {
                Date = date.ToShortDateString(),
                // Сколько 10-минутных слотов занимает выбранная категория (AC-5, AC-6, AC-7)
                RequiredSlotsCount = _options.CategorySlots.GetValueOrDefault(
                    categoryName, _options.DefaultCategorySlots)
            };

            var step = TimeSpan.FromMinutes(_options.SlotMinutes);
            var workStart = new TimeSpan(_options.WorkStartHour, 0, 0);
            var workEnd = new TimeSpan(_options.WorkEndHour, 0, 0);

            // 1. Извлекаем из базы данных все существующие записи к этому врачу на выбранную дату
            var existingAppointments = await _repository.GetByDoctorIdAsync(doctorId, date, cancellationToken);

            // Составляем хэш-сет всех занятых точек сетки в этот день.
            // ВАЖНО: отменённые записи слот НЕ занимают — время снова доступно для записи
            var busySlots = new HashSet<TimeSpan>();
            foreach (var appointment in existingAppointments.Where(a => a.Status != AppointmentStatuses.Canceled))
            {
                CollectBusySlots(appointment.Timeslot, busySlots, step);
            }

            // 2. Пробегаем по рабочему дню и ищем окна нужной длины
            var windowLength = TimeSpan.FromMinutes(response.RequiredSlotsCount * _options.SlotMinutes);

            var checkTime = workStart;
            while (checkTime + windowLength <= workEnd)
            {
                // Проверяем, свободны ли все N слотов подряд, начиная с этой минуты (AC-3, AC-4)
                var isWindowFree = true;
                for (var offset = TimeSpan.Zero; offset < windowLength; offset += step)
                {
                    if (busySlots.Contains(checkTime + offset))
                    {
                        isWindowFree = false;
                        break;
                    }
                }

                if (isWindowFree)
                {
                    response.AvailableStartTimes.Add(checkTime.ToString(@"hh\:mm"));
                }

                checkTime += step;
            }

            return response;
        }

        /// <summary>
        /// Парсит строку слота вида "10:30 - 11:00" и помечает все точки сетки внутри неё занятыми.
        /// Нераспарсенные слоты игнорируются (не блокируют время).
        /// </summary>
        private void CollectBusySlots(string timeslot, HashSet<TimeSpan> busySlots, TimeSpan step)
        {
            var parts = timeslot.Split('-');
            if (parts.Length != 2) return;
            if (!TimeSpan.TryParse(parts[0].Trim(), out var startTime)) return;
            if (!TimeSpan.TryParse(parts[1].Trim(), out var endTime)) return;

            var current = startTime;
            while (current < endTime)
            {
                busySlots.Add(current);
                current += step;
            }
        }
    }
}