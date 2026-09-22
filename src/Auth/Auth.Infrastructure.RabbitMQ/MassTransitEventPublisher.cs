using System.Threading;
using System.Threading.Tasks;
using Auth.Application.Interfaces;
using MassTransit;

namespace Auth.Infrastructure.RabbitMQ
{
    /// <summary>
    /// Адаптер публикации событий поверх MassTransit.
    /// Application-слой Auth не знает ни про брокер, ни про MassTransit —
    /// он зависит только от абстракции IEventPublisher (DIP).
    /// </summary>
    public class MassTransitEventPublisher : IEventPublisher
    {
        private readonly IPublishEndpoint _publishEndpoint;

        public MassTransitEventPublisher(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
        }

        /// <inheritdoc/>
        public Task PublishAsync<T>(T eventMessage, CancellationToken cancellationToken = default) where T : class
        {
            // MassTransit сам сериализует сообщение, объявляет exchange/очереди
            // и подтверждает доставку
            return _publishEndpoint.Publish(eventMessage, cancellationToken);
        }
    }
}
