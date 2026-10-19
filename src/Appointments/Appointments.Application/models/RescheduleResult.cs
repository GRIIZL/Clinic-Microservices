namespace Appointments.Application.Models
{
    /// <summary>
    /// Результат попытки переноса записи (US-66).
    /// Контроллер переводит его в соответствующий HTTP-статус.
    /// </summary>
    public enum RescheduleResult
    {
        /// <summary>Запись перенесена.</summary>
        Success = 0,

        /// <summary>Запись с таким ID не найдена.</summary>
        NotFound = 1,

        /// <summary>Переносить можно только записи в статусе Pending (AC-1).</summary>
        NotReschedulable = 2,

        /// <summary>Выбранный слот уже занят другой записью этого врача.</summary>
        SlotTaken = 3,

        /// <summary>Строка слота не соответствует формату "HH:mm - HH:mm".</summary>
        InvalidTimeslot = 4
    }
}
