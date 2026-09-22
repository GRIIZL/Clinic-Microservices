using System.Threading;
using System.Threading.Tasks;

namespace Auth.Application.Interfaces
{
    /// <summary>
    /// Абстракция отправки писем.
    /// Application-слой зависит только от неё (DIP), а не от SMTP-провайдера.
    /// </summary>
    public interface IEmailSender
    {
        /// <summary>
        /// Отправляет письмо со ссылкой подтверждения email.
        /// </summary>
        /// <param name="recipientEmail">Адрес получателя.</param>
        /// <param name="confirmationLink">Готовая ссылка подтверждения.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        Task SendVerificationEmailAsync(
            string recipientEmail,
            string confirmationLink,
            CancellationToken cancellationToken = default);
    }
}
