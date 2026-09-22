using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Profiles.Infrastructure.RabbitMQ
{
    /// <summary>
    /// Регистрация шины сообщений Profiles.
    /// Детали MassTransit инкапсулированы здесь, чтобы Program.cs оставался тонким.
    /// </summary>
    public static class MessagingServiceCollectionExtensions
    {
        public static IServiceCollection AddProfilesMessaging(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddMassTransit(bus =>
            {
                // Consumer события регистрации из Auth
                bus.AddConsumer<UserRegisteredEventConsumer>();

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

                    // Собственная durable-очередь сервиса Profiles
                    cfg.ReceiveEndpoint("profiles-user-registered-events", endpoint =>
                    {
                        // Не более 1 сообщения за раз — не перегружаем БД при шквале событий
                        endpoint.PrefetchCount = 1;

                        // Повторы с паузой вместо бесконечного requeue-цикла:
                        // после исчерпания попыток сообщение уходит в _error queue (DLQ)
                        endpoint.UseMessageRetry(retry => retry.Interval(5, TimeSpan.FromSeconds(2)));

                        endpoint.ConfigureConsumer<UserRegisteredEventConsumer>(context);
                    });
                });
            });

            return services;
        }
    }
}
