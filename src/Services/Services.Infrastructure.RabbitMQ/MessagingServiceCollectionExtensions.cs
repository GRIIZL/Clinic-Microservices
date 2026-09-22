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
                    var host = configuration["RabbitMQHost"] ?? "localhost";
                    var port = int.TryParse(configuration["RabbitMQPort"], out var parsedPort) ? parsedPort : 5672;

                    cfg.Host(
                        new Uri($"rabbitmq://{host}:{port}/"),
                        mqHost =>
                        {
                            mqHost.Username(configuration["RabbitMQUser"] ?? "guest");
                            mqHost.Password(configuration["RabbitMQPassword"] ?? "guest");
                        });
                });
            });

            return services;
        }
    }
}