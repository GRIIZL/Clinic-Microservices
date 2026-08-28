using System.Threading;
using System.Threading.Tasks;

namespace Auth.Application.Interfaces
{
    /// <summary>
    /// Абстракция для публикации событий в шину сообщений (RabbitMQ).
    /// Application-слой зависит только от этой абстракции (принцип DIP),
    /// а не от конкретной реализации инфраструктуры.
    /// </summary>
    public interface IEventPublisher
    {
        /// <summary>
        /// Публикует событие в шину сообщений.
        /// </summary>
        /// <typeparam name="T">Тип события (сериализуется в JSON).</typeparam>
        /// <param name="eventMessage">Объект события.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        Task PublishAsync<T>(T eventMessage, CancellationToken cancellationToken = default) where T : class;
    }
}