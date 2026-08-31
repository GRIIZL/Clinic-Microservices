using System.Threading;
using System.Threading.Tasks;
using Shared.Events;

namespace Appointments.Application.Interfaces
{
    /// <summary>
    /// Интерфейс обработки событий изменения специализаций/услуг.
    /// Application-слой Appointments остаётся независимым от RabbitMQ.
    /// </summary>
    public interface ISpecializationEventHandlingService
    {
        /// <summary>
        /// Обрабатывает событие об изменении специализации.
        /// Если статус стал Inactive — отменяет все активные записи к врачам этой специализации.
        /// </summary>
        Task HandleSpecializationChangedAsync(SpecializationChangedEvent evt, CancellationToken cancellationToken = default);
    }
}
