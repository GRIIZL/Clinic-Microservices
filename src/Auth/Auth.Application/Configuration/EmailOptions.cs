namespace Auth.Application.Configuration
{
    /// <summary>
    /// Настройки отправки писем и ссылки подтверждения email.
    /// Задаются в appsettings.json (секция "Email") и могут быть переопределены
    /// переменными окружения, например Email__SmtpHost=smtp.example.com.
    /// </summary>
    public class EmailOptions
    {
        public const string SectionName = "Email";

        /// <summary>
        /// Шаблон ссылки подтверждения, где {0} — токен.
        /// Основание (схема, хост, порт) не зашито в код, чтобы не зависеть от окружения.
        /// </summary>
        public string ConfirmationUrlTemplate { get; set; } = "http://localhost:5176/api/auth/verify?token={0}";

        /// <summary>SMTP-сервер для отправки писем.</summary>
        public string SmtpHost { get; set; } = "localhost";

        /// <summary>Порт SMTP-сервера.</summary>
        public int SmtpPort { get; set; } = 25;

        /// <summary>Использовать TLS при соединении с SMTP-сервером.</summary>
        public bool UseSsl { get; set; }

        /// <summary>Адрес отправителя письма.</summary>
        public string From { get; set; } = "noreply@clinic.local";

        /// <summary>Логин SMTP. Пусто — анонимная отправка (локальный relaying-сервер).</summary>
        public string? UserName { get; set; }

        /// <summary>Пароль SMTP.</summary>
        public string? Password { get; set; }
    }
}
