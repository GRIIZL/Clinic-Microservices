namespace Shared.Events
{
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

        /// <summary>Новый статус специализации: "Active" или "Inactive".</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Тип изменения: "SpecializationStatus" или "ServiceStatus".</summary>
        public string ChangeType { get; set; } = string.Empty;

        /// <summary>Идентификатор услуги (если изменение касается конкретной услуги).</summary>
        public string? ServiceId { get; set; }

        /// <summary>Название услуги (если изменение касается конкретной услуги).</summary>
        public string? ServiceName { get; set; }

        /// <summary>Момент изменения (UTC).</summary>
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}
