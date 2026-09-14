namespace Appointments.Domain
{
    /// <summary>
    /// Допустимые статусы записи на приём.
    /// Единый источник правды вместо строковых литералов по всему коду.
    /// </summary>
    public static class AppointmentStatuses
    {
        /// <summary>Запись создана, ожидает подтверждения.</summary>
        public const string Pending = "Pending";

        /// <summary>Запись подтверждена ресепшионистом.</summary>
        public const string Approved = "Approved";

        /// <summary>Запись отменена (пациентом/ресепшионистом/каскадно при деактивации).</summary>
        public const string Canceled = "Canceled";

        /// <summary>Приём состоялся, заключение создано.</summary>
        public const string Completed = "Completed";
    }
}