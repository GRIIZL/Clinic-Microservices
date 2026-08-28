using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Auth.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace Auth.Infrastructure.RabbitMQ
{
    /// <summary>
    /// Реализация публикатора событий поверх RabbitMQ (паттерн Publisher).
    /// Application-слой Auth остаётся независимым от конкретного брокера.
    /// </summary>
    public class RabbitMqPublisher : IEventPublisher, IAsyncDisposable
    {
        // Общий exchange для всех микросервисов (fanout: событие уходит всем подписчикам)
        private const string ExchangeName = "clinic_events";

        private readonly ConnectionFactory _factory;
        private readonly string _exchangeName;

        // Защита от гонок при ленивой инициализации соединения
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private IConnection? _connection;
        private IChannel? _channel;
        private bool _disposed;

        public RabbitMqPublisher(IConfiguration configuration)
        {
            _factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMQHost"] ?? "localhost",
                Port = int.Parse(configuration["RabbitMQPort"] ?? "5672")
            };
            _exchangeName = ExchangeName;
        }

        /// <inheritdoc/>
        public async Task PublishAsync<T>(T eventMessage, CancellationToken cancellationToken = default) where T : class
        {
            if (eventMessage == null) throw new ArgumentNullException(nameof(eventMessage));

            var channel = await GetChannelAsync(cancellationToken);

            // Накладываем заголовок с именем типа события —
            // потребитель (Profiles) сможет идентифицировать какое событие пришло
            var properties = new BasicProperties
            {
                Type = typeof(T).Name,
                Persistent = true // durable-сообщение: переживёт рестарт брокера (если очередь объявлена durable)
            };

            var json = JsonSerializer.Serialize(eventMessage,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var body = Encoding.UTF8.GetBytes(json);

            await channel.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: string.Empty, // fanout: routing key игнорируется
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            Console.WriteLine($"--> [Auth] Published event {typeof(T).Name} to exchange '{_exchangeName}'");
        }

        // Лениво создаём соединение и объявляем exchange при первой публикации
        private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
        {
            if (_channel is { IsOpen: true } && _connection is { IsOpen: true })
                return _channel;

            await _initLock.WaitAsync(cancellationToken);
            try
            {
                if (_channel is { IsOpen: true } && _connection is { IsOpen: true })
                    return _channel;

                _connection = await _factory.CreateConnectionAsync(cancellationToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

                // Fanout-обменник: сообщение копируется во все привязанные очереди
                await _channel.ExchangeDeclareAsync(
                    exchange: _exchangeName,
                    type: ExchangeType.Fanout,
                    durable: true,
                    autoDelete: false,
                    cancellationToken: cancellationToken);

                Console.WriteLine($"--> [Auth] Connected to RabbitMQ, exchange '{_exchangeName}' declared.");
            }
            finally
            {
                _initLock.Release();
            }

            return _channel;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                if (_channel != null)
                {
                    await _channel.CloseAsync();
                    await _channel.DisposeAsync();
                }

                if (_connection != null)
                {
                    await _connection.CloseAsync();
                    await _connection.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--> [Auth] Error during disposing RabbitMQ publisher: {ex.Message}");
            }

            _initLock.Dispose();
        }
    }
}