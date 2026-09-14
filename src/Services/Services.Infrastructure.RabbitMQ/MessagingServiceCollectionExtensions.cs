using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Services.Infrastructure.RabbitMQ
{
    /// <summary>
    /// Регистрация шины сообщений Services.
    /// Детали MassTransit инкапсулированы здесь, чтобы Program.cs оставался тонким.
    /// </summary>
    public static class MessagingServiceCollectionExtensions
    {
        public static IServiceCollection AddServicesMessaging(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddMassTransit(bus =>
            {
                // Publisher-only сервис: consumer'ов нет
                bus.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(
                        configuration["RabbitMQHost"] ?? "localhost",
                        "/",
                        host =>
                        {
                            host.Username(configuration["RabbitMQUser"] ?? "guest");
                            host.Password(configuration["RabbitMQPassword"] ?? "guest");
                        });
                });
            });

            return services;
        }
    }
}