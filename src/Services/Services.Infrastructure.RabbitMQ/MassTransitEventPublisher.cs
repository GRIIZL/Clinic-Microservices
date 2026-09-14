using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Services.Application.Interfaces;

namespace Services.Infrastructure.RabbitMQ
{
    /// <summary>
    /// Адаптер публикации событий поверх MassTransit.
    /// Application-слой Services не знает ни про брокер, ни про MassTransit —
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
            // MassTransit сам сериализует сообщение, создаст exchange/очереди
            // и подтвердит доставку — ручной код больше не нужен
            return _publishEndpoint.Publish(eventMessage, cancellationToken);
        }
    }
}