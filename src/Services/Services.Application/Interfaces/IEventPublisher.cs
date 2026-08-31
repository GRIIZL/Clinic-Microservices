using System.Threading;
using System.Threading.Tasks;

namespace Services.Application.Interfaces
{
    /// <summary>
    /// Интерфейс для публикации событий.
    /// Позволяет Application-слою быть независимым от конкретного брокера.
    /// </summary>
    public interface IEventPublisher
    {
        /// <summary>
        /// Публикует событие в брокер сообщений.
        /// </summary>
        /// <typeparam name="T">Тип события.</typeparam>
        /// <param name="eventMessage">Сообщение события.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        Task PublishAsync<T>(T eventMessage, CancellationToken cancellationToken = default) where T : class;
    }
}
