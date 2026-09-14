using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Appointments.Infrastructure.RabbitMQ
{
    /// <summary>
    /// Регистрация шины сообщений Appointments.
    /// Детали MassTransit инкапсулированы здесь, чтобы Program.cs оставался тонким.
    /// </summary>
    public static class MessagingServiceCollectionExtensions
    {
        public static IServiceCollection AddAppointmentsMessaging(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddMassTransit(bus =>
            {
                // Регистрируем consumer события об изменении специализации
                bus.AddConsumer<SpecializationChangedEventConsumer>();

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

                    // Собственная durable-очередь сервиса Appointments
                    cfg.ReceiveEndpoint("appointments-specialization-events", endpoint =>
                    {
                        endpoint.ConfigureConsumer<SpecializationChangedEventConsumer>(context);
                    });
                });
            });

            return services;
        }
    }
}