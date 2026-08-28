using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Profiles.Application.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Events;

namespace Profiles.Infrastructure.RabbitMQ
{
    /// <summary>
    /// Фоновый подписчик (Consumer) на события шины RabbitMQ.
    /// Живёт в Infrastructure: принимает события из брокера и передаёт бизнес-логике
    /// через абстракцию IPatientEventHandlingService (принцип DIP).
    /// </summary>
    public class RabbitMqConsumer : BackgroundService
    {
        // Должен совпадать с exchange, который объявляет Publisher в Auth.Infrastructure.RabbitMQ
        private const string ExchangeName = "clinic_events";
        private const string QueueName = "profiles.user_registered";

        private readonly ConnectionFactory _factory;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RabbitMqConsumer> _logger;

        public RabbitMqConsumer(IConfiguration configuration, IServiceScopeFactory scopeFactory, ILogger<RabbitMqConsumer> logger)
        {
            _factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMQHost"] ?? "localhost",
                Port = int.Parse(configuration["RabbitMQPort"] ?? "5672"),
                // Автоматически переподключаемся при обрыве соединения
                AutomaticRecoveryEnabled = true
            };
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("--> [Profiles] RabbitMqConsumer starting...");

            IConnection? connection = null;
            IChannel? channel = null;

            try
            {
                connection = await _factory.CreateConnectionAsync(stoppingToken);
                channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                // Общий exchange (совпадает с Auth Publisher'ом)
                await channel.ExchangeDeclareAsync(
                    exchange: ExchangeName,
                    type: ExchangeType.Fanout,
                    durable: true,
                    autoDelete: false,
                    cancellationToken: stoppingToken);

                // Собственная durable-очередь: при падении сервиса события не теряются
                await channel.QueueDeclareAsync(
                    queue: QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: stoppingToken);

                // Привязываем очередь к exchange (fanout — routing key не используется)
                await channel.QueueBindAsync(
                    queue: QueueName,
                    exchange: ExchangeName,
                    routingKey: string.Empty,
                    cancellationToken: stoppingToken);

                // Отдаём не более 1 сообщения за раз — не перегружаем БД при шквале событий
                await channel.BasicQosAsync(0, 1, false, stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (_, ea) => await HandleMessageAsync(channel, ea);

                string consumerTag = await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer, stoppingToken);

                _logger.LogInformation("--> [Profiles] Consuming from queue '{QueueName}' (tag {Tag}).", QueueName, consumerTag);

                // Блокируем фоновую задачу до остановки приложения
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("--> [Profiles] RabbitMqConsumer stopping due to cancellation.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "--> [Profiles] RabbitMqConsumer failed to connect. Check that RabbitMQ is running.");
            }
            finally
            {
                if (channel != null)
                {
                    await channel.CloseAsync();
                    await channel.DisposeAsync();
                }

                if (connection != null)
                {
                    await connection.CloseAsync();
                    await connection.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// Обрабатывает одно входящее сообщение: определяет тип события по заголовку
        /// и делегирует бизнес-логику в Application-слой.
        /// </summary>
        private async Task HandleMessageAsync(IChannel channel, BasicDeliverEventArgs ea)
        {
            var eventType = ea.BasicProperties?.Type ?? string.Empty;
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());

            try
            {
                switch (eventType)
                {
                    case nameof(UserRegisteredEvent):
                        var registered = JsonSerializer.Deserialize<UserRegisteredEvent>(body, JsonOptions);
                        if (registered != null)
                        {
                            // Создаём scope: у нас скоуп-сервисы (репозитории/EF), BackgroundService — синглтон
                            using var scope = _scopeFactory.CreateScope();
                            var handler = scope.ServiceProvider.GetRequiredService<IPatientEventHandlingService>();
                            await handler.HandleUserRegisteredAsync(registered, CancellationToken.None);
                        }
                        break;

                    default:
                        _logger.LogWarning("Получено неизвестное событие '{EventType}', игнорируем.", eventType);
                        break;
                }

                // Подтверждаем обработку (ack) — сообщение удаляется из очереди
                await channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке события '{EventType}' из RabbitMQ.", eventType);
                // Не подтверждаем (nack) — сообщение вернётся в очередь и будет повторная попытка (redelivery)
                await channel.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        }

        // Анонимная сериалзация: те же правила (camelCase), что и в Publisher
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}