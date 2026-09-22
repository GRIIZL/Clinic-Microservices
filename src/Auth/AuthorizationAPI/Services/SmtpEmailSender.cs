using System;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Auth.Application.Configuration;
using Auth.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthorizationAPI.Services
{
    /// <summary>
    /// Отправка писем через SMTP. Параметры берутся из секции "Email" (appsettings/ENV).
    /// </summary>
    public class SmtpEmailSender : IEmailSender
    {
        private readonly EmailOptions _options;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendVerificationEmailAsync(
            string recipientEmail,
            string confirmationLink,
            CancellationToken cancellationToken = default)
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_options.From),
                Subject = "Подтверждение регистрации в клинике",
                Body = $"Чтобы завершить регистрацию, перейдите по ссылке:\n{confirmationLink}"
            };
            message.To.Add(recipientEmail);

            // SmtpClient помечен obsolete в .NET, но почта уходит на обычный SMTP-релей;
            // если появится внешний провайдер — реализация IEmailSender меняется, AuthService нет.
#pragma warning disable SYSLIB0014
            using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
            {
                EnableSsl = _options.UseSsl
            };
            if (!string.IsNullOrEmpty(_options.UserName))
            {
                client.Credentials = new NetworkCredential(_options.UserName, _options.Password);
            }

            await client.SendMailAsync(message, cancellationToken);
#pragma warning restore SYSLIB0014

            // Ссылку и токен в лог не пишем: токен подтверждает владение почтой,
            // его вывод в лог равняется компрометации.
            _logger.LogInformation("Verification email sent to {Email}", recipientEmail);
        }
    }
}
