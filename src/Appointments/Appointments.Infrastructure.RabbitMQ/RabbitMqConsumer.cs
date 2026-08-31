using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Appointments.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Events;

namespace Appointments.Infrastructure.RabbitMQ
{
    /// <summary>
    /// Фоновый подписчик (Consumer) на события брокера RabbitMQ.
    /// Ждёт события об изменении специализаций/услуг от Services API
    /// и обрабатывает их через Application-слой.
    /// 
    /// Паттерн: Consumer → интерфейс → Application-слой → Domain (репозитории)
    /// Это следует принципу DIP (Dependency Inversion Principle).
    /// </summary>
    public class RabbitMqConsumer : BackgroundService
    {
        // Exchange совпадает с тем, который объявляет Publisher
        private const string ExchangeName = "clinic_events";
        // Уникальное имя очереди для Appointments
        private const string QueueName = "appointments.specialization_events";

        private readonly ConnectionFactory _factory;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RabbitMqConsumer> _logger;

        public RabbitMqConsumer(
            IConfiguration configuration,
            IServiceScopeFactory scopeFactory,
            ILogger<RabbitMqConsumer> logger)
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
            _logger.LogInformation("--> [Appointments] RabbitMqConsumer starting...");

            IConnection? connection = null;
            IChannel? channel = null;

            try
            {
                // 1. Создаём соединение с RabbitMQ
                connection = await _factory.CreateConnectionAsync(stoppingToken);
                channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                // 2. Объявляем exchange (должен совпадать с Publisher'ом)
                await channel.ExchangeDeclareAsync(
                    exchange: ExchangeName,
                    type: ExchangeType.Fanout,
                    durable: true,        // переживёт перезапуск RabbitMQ
                    autoDelete: false,
                    cancellationToken: stoppingToken);

                // 3. Объявляем свою очередь (durable — не пропадёт при перезапуске)
                await channel.QueueDeclareAsync(
                    queue: QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: stoppingToken);

                // 4. Привязываем очередь к exchange
                await channel.QueueBindAsync(
                    queue: QueueName,
                    exchange: ExchangeName,
                    routingKey: string.Empty,
                    cancellationToken: stoppingToken);

                // 5. Ограничиваем: не больше 1 сообщения в обработке одновременно
                // Это защищает БД от перегрузки при шквале событий
                await channel.BasicQosAsync(0, 1, false, stoppingToken);

                // 6. Создаём асинхронного консьюмера
                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (_, ea) => await HandleMessageAsync(channel, ea);

                // 7. Начинаем потребление (autoAck: false — подтверждаем вручную)
                string consumerTag = await channel.BasicConsumeAsync(
                    QueueName,
                    autoAck: false,
                    consumer,
                    stoppingToken);

                _logger.LogInformation(
                    "--> [Appointments] Consuming from queue '{QueueName}' (tag {Tag}).",
                    QueueName,
                    consumerTag);

                // 8. Блокируем фоновую задачу до остановки приложения
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("--> [Appointments] RabbitMqConsumer stopping due to cancellation.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "--> [Appointments] RabbitMqConsumer failed to connect. Check that RabbitMQ is running.");
            }
            finally
            {
                // 9. Очищаем ресурсы
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
        /// Обрабатывает одно входящее сообщение:
        /// - Определяет тип события по заголовку
        /// - Делегирует бизнес-логику в Application-слой
        /// - Подтверждает обработку (ack) или возвращает в очередь (nack)
        /// </summary>
        private async Task HandleMessageAsync(IChannel channel, BasicDeliverEventArgs ea)
        {
            var eventType = ea.BasicProperties?.Type ?? string.Empty;
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());

            try
            {
                switch (eventType)
                {
                    case nameof(SpecializationChangedEvent):
                        var evt = JsonSerializer.Deserialize<SpecializationChangedEvent>(body, JsonOptions);
                        if (evt != null)
                        {
                            // Создаём scope: у нас scoped-сервисы (репозитории/EF),
                            // BackgroundService — синглтон, поэтому нужен новый scope для каждого сообщения
                            using var scope = _scopeFactory.CreateScope();
                            var handler = scope.ServiceProvider.GetRequiredService<ISpecializationEventHandlingService>();
                            await handler.HandleSpecializationChangedAsync(evt, CancellationToken.None);
                        }
                        break;

                    default:
                        _logger.LogWarning(
                            "[Appointments] Received unknown event '{EventType}', ignoring.",
                            eventType);
                        break;
                }

                // Подтверждаем обработку — сообщение удаляется из очереди
                await channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[Appointments] Error processing event '{EventType}' from RabbitMQ.",
                    eventType);

                // Не подтверждаем (nack) — сообщение вернётся в очередь и будет повторная попытка
                await channel.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        }

        // Настройки сериализации: camelCase (как в Publisher)
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}
