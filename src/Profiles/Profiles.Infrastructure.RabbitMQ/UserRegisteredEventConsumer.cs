using System.Threading.Tasks;
using MassTransit;
using Profiles.Application.Interfaces;
using Shared.Events;

namespace Profiles.Infrastructure.RabbitMQ
{
    /// <summary>
    /// Consumer события регистрации пользователя из Auth (MassTransit).
    /// MassTransit сам управляет подключением, retry, ack/nack и созданием scope
    /// для scoped-зависимостей — ручной BackgroundService больше не нужен.
    /// </summary>
    public class UserRegisteredEventConsumer : IConsumer<UserRegisteredEvent>
    {
        private readonly IPatientEventHandlingService _handler;

        public UserRegisteredEventConsumer(IPatientEventHandlingService handler)
        {
            _handler = handler;
        }

        public Task Consume(ConsumeContext<UserRegisteredEvent> context)
        {
            // Если обработка бросит исключение, MassTransit повторит доставку согласно
            // настройкам retry, а после исчерпания попыток отправит сообщение в _error queue
            return _handler.HandleUserRegisteredAsync(context.Message, context.CancellationToken);
        }
    }
}
