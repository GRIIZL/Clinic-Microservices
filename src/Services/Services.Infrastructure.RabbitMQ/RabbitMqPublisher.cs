using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using Services.Application.Interfaces;

namespace Services.Infrastructure.RabbitMQ
{
    /// <summary>
    /// Реализация публикатора событий поверх RabbitMQ (паттерн Publisher).
    /// Публикует события об изменении специализаций и услуг.
    /// </summary>
    public class RabbitMqPublisher : IEventPublisher, IAsyncDisposable
    {
        private const string ExchangeName = "clinic_events";

        private readonly ConnectionFactory _factory;
        private readonly string _exchangeName;

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

            var properties = new BasicProperties
            {
                Type = typeof(T).Name,
                Persistent = true
            };

            var json = JsonSerializer.Serialize(eventMessage,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var body = Encoding.UTF8.GetBytes(json);

            await channel.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: string.Empty,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            Console.WriteLine($"--> [Services] Published event {typeof(T).Name} to exchange '{_exchangeName}'");
        }

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

                await _channel.ExchangeDeclareAsync(
                    exchange: _exchangeName,
                    type: ExchangeType.Fanout,
                    durable: true,
                    autoDelete: false,
                    cancellationToken: cancellationToken);

                Console.WriteLine($"--> [Services] Connected to RabbitMQ, exchange '{_exchangeName}' declared.");
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
                Console.WriteLine($"--> [Services] Error during disposing RabbitMQ publisher: {ex.Message}");
            }

            _initLock.Dispose();
        }
    }
}
