using System;
using System.Threading;
using System.Threading.Tasks;
using Appointments.Application.Models;

namespace Appointments.Application.Interfaces
{
    /// <summary>
    /// Сервис расчёта свободных временных слотов приёма (US-7).
    /// Выделен в отдельную ответственность (SRP): AppointmentService управляет записями,
    /// SlotService — только алгоритмом расчёта сетки доступного времени.
    /// </summary>
    public interface ISlotService
    {
        /// <summary>
        /// Возвращает список доступных временных точек старта приёма
        /// для врача, даты и категории услуги.
        /// </summary>
        Task<AvailableSlotsResponseDto> GetAvailableSlotsAsync(
            Guid doctorId,
            DateTime date,
            string categoryName,
            CancellationToken cancellationToken = default);
    }
}