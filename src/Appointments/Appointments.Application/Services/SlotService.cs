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
            var requiredSlotsCount = ResolveRequiredSlots(categoryName);

            var response = new AvailableSlotsResponseDto
            {
                // Формат ISO-8601: не зависит от культуры сервера и удобен фронту.
                Date = date.ToString("yyyy-MM-dd"),
                // Сколько 10-минутных слотов занимает выбранная категория (AC-5, AC-6, AC-7)
                RequiredSlotsCount = requiredSlotsCount
            };

            // 1. Извлекаем из базы данных все существующие записи к этому врачу на выбранную дату
            var existingAppointments = await _repository.GetByDoctorIdAsync(doctorId, date, cancellationToken);

            // Составляем хэш-сет всех занятых точек сетки в этот день.
            // ВАЖНО: отменённые записи слот НЕ занимают — время снова доступно для записи
            var busySlots = BuildBusySlots(existingAppointments);

            // 2. Пробегаем по рабочему дню и ищем окна нужной длины
            response.AvailableStartTimes = FindFreeWindows(busySlots, requiredSlotsCount);

            return response;
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<DateTime>> GetAvailableDatesAsync(
            Guid doctorId,
            DateTime from,
            DateTime to,
            string categoryName,
            CancellationToken cancellationToken = default)
        {
            var fromDate = from.Date;
            var toDate = to.Date;
            if (toDate < fromDate) return Array.Empty<DateTime>();

            var requiredSlotsCount = ResolveRequiredSlots(categoryName);

            // Один запрос на весь диапазон вместо запроса на каждый день календаря
            var appointments = (await _repository.GetByDoctorIdRangeAsync(
                doctorId, fromDate, toDate, cancellationToken)).ToList();

            var availableDates = new List<DateTime>();
            for (var day = fromDate; day <= toDate; day = day.AddDays(1))
            {
                var busySlots = BuildBusySlots(appointments.Where(a => a.Date.Date == day));
                if (HasFreeWindow(busySlots, requiredSlotsCount))
                {
                    availableDates.Add(day);
                }
            }

            return availableDates;
        }

        /// <summary>
        /// Сколько точек сетки занимает категория услуги.
        /// Неизвестная категория получает значение по умолчанию.
        /// </summary>
        private int ResolveRequiredSlots(string categoryName) =>
            _options.CategorySlots.GetValueOrDefault(categoryName, _options.DefaultCategorySlots);

        /// <summary>
        /// Собирает занятые точки сетки по всем записям, кроме отменённых.
        /// </summary>
        private HashSet<TimeSpan> BuildBusySlots(IEnumerable<Appointment> appointments)
        {
            var step = TimeSpan.FromMinutes(_options.SlotMinutes);
            var busySlots = new HashSet<TimeSpan>();

            foreach (var appointment in appointments.Where(a => a.Status != AppointmentStatuses.Canceled))
            {
                CollectBusySlots(appointment.Timeslot, busySlots, step);
            }

            return busySlots;
        }

        /// <summary>
        /// Возвращает все точки старта, начиная с которых помещается свободное окно.
        /// </summary>
        private List<string> FindFreeWindows(HashSet<TimeSpan> busySlots, int requiredSlotsCount)
        {
            var step = TimeSpan.FromMinutes(_options.SlotMinutes);
            var workStart = new TimeSpan(_options.WorkStartHour, 0, 0);
            var workEnd = new TimeSpan(_options.WorkEndHour, 0, 0);
            var windowLength = TimeSpan.FromMinutes(requiredSlotsCount * _options.SlotMinutes);

            var startTimes = new List<string>();
            var checkTime = workStart;

            while (checkTime + windowLength <= workEnd)
            {
                if (IsWindowFree(busySlots, checkTime, windowLength, step))
                {
                    startTimes.Add(checkTime.ToString(@"hh\:mm"));
                }

                checkTime += step;
            }

            return startTimes;
        }

        /// <summary>
        /// Есть ли в дне хотя бы одно свободное окно нужной длины.
        /// </summary>
        private bool HasFreeWindow(HashSet<TimeSpan> busySlots, int requiredSlotsCount)
        {
            var step = TimeSpan.FromMinutes(_options.SlotMinutes);
            var workStart = new TimeSpan(_options.WorkStartHour, 0, 0);
            var workEnd = new TimeSpan(_options.WorkEndHour, 0, 0);
            var windowLength = TimeSpan.FromMinutes(requiredSlotsCount * _options.SlotMinutes);

            for (var checkTime = workStart; checkTime + windowLength <= workEnd; checkTime += step)
            {
                if (IsWindowFree(busySlots, checkTime, windowLength, step))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Проверяет, свободны ли все N слотов подряд, начиная с указанной минуты (AC-3, AC-4).
        /// </summary>
        private static bool IsWindowFree(
            HashSet<TimeSpan> busySlots,
            TimeSpan start,
            TimeSpan windowLength,
            TimeSpan step)
        {
            for (var offset = TimeSpan.Zero; offset < windowLength; offset += step)
            {
                if (busySlots.Contains(start + offset))
                {
                    return false;
                }
            }

            return true;
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