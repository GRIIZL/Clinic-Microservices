using System.Threading;
using System.Threading.Tasks;
using Shared.Events;

namespace Profiles.Application.Interfaces
{
    /// <summary>
    /// Обработчик бизнес-событий из Auth, которые касаются профилей пациентов.
    /// Application-слой владеет бизнес-логикой реакции на событие;
    /// Infrastructure (RabbitMqConsumer) лишь доставляет сырое событие сюда.
    /// </summary>
    public interface IPatientEventHandlingService
    {
        /// <summary>
        /// Реакция на событие о регистрации пользователя в Auth:
        /// создаёт профиль пациента и связывает его с accountId.
        /// </summary>
        Task HandleUserRegisteredAsync(UserRegisteredEvent registeredEvent, CancellationToken cancellationToken = default);
    }
}