namespace Shared.Events
{
    /// <summary>
    /// Типы изменений, передаваемые в SpecializationChangedEvent.ChangeType.
    /// </summary>
    public static class SpecializationChangeTypes
    {
        /// <summary>Изменился статус самой специализации.</summary>
        public const string SpecializationStatus = "SpecializationStatus";

        /// <summary>Изменился статус конкретной медицинской услуги.</summary>
        public const string ServiceStatus = "ServiceStatus";
    }

    /// <summary>
    /// Статусы специализаций/услуг в сервисе Services.
    /// </summary>
    public static class ServiceStatuses
    {
        public const string Active = "Active";
        public const string Inactive = "Inactive";
    }

    /// <summary>
    /// Событие, публикуемое сервисом Services при изменении специализации или медицинской услуги.
    /// Appointments подписывается на него, чтобы отменять/блокировать записи к неактивным врачам.
    /// </summary>
    public class SpecializationChangedEvent
    {
        /// <summary>Идентификатор специализации, которая изменилась.</summary>
        public string SpecializationId { get; set; } = string.Empty;

        /// <summary>Название специализации.</summary>
        public string SpecializationName { get; set; } = string.Empty;

        /// <summary>Новый статус: см. ServiceStatuses.</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Предыдущий статус (для аудита и идемпотентной обработки).</summary>
        public string? OldStatus { get; set; }

        /// <summary>Тип изменения: см. SpecializationChangeTypes.</summary>
        public string ChangeType { get; set; } = string.Empty;

        /// <summary>Идентификатор услуги (если изменение касается конкретной услуги).</summary>
        public string? ServiceId { get; set; }

        /// <summary>Название услуги (если изменение касается конкретной услуги).</summary>
        public string? ServiceName { get; set; }

        /// <summary>Момент изменения (UTC).</summary>
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}
