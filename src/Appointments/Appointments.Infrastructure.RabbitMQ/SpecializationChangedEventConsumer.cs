using System.Threading.Tasks;
using Appointments.Application.Interfaces;
using MassTransit;
using Shared.Events;

namespace Appointments.Infrastructure.RabbitMQ
{
    /// <summary>
    /// Consumer события об изменении специализации/услуги (MassTransit).
    /// MassTransit сам управляет подключением, retry, ack/nack и созданием scope
    /// для scoped-зависимостей — ручной BackgroundService больше не нужен.
    /// </summary>
    public class SpecializationChangedEventConsumer : IConsumer<SpecializationChangedEvent>
    {
        private readonly ISpecializationEventHandlingService _handler;

        public SpecializationChangedEventConsumer(ISpecializationEventHandlingService handler)
        {
            _handler = handler;
        }

        public Task Consume(ConsumeContext<SpecializationChangedEvent> context)
        {
            // Если обработка бросит исключение, MassTransit вернёт сообщение в очередь (retry)
            return _handler.HandleSpecializationChangedAsync(context.Message, context.CancellationToken);
        }
    }
}