using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks; // Добавлено для Task
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using Services.Application.Interfaces;

namespace Services.Infrastructure.MongoDb.Messaging
{
    // Вместо IDisposable для современной асинхронной библиотеки лучше использовать IAsyncDisposable
    public class MessageBusClient : IMessageBusClient, IAsyncDisposable
    {
        private IConnection? _connection;
        private IChannel? _channel;
        private readonly ConnectionFactory _factory;
        private const string ExchangeName = "trigger_exchange";
        private readonly Task _initializationTask;

        public MessageBusClient(IConfiguration configuration)
        {
            _factory = new ConnectionFactory() 
            { 
                HostName = configuration["RabbitMQHost"] ?? "localhost",
                Port = int.Parse(configuration["RabbitMQPort"] ?? "5672")
            };

            // Запускаем асинхронную инициализацию безопасно прямо из конструктора
            _initializationTask = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                _connection = await _factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();
                
                await _channel.ExchangeDeclareAsync(
                    exchange: ExchangeName, 
                    type: ExchangeType.Fanout
                );
                
                Console.WriteLine("--> Connected to RabbitMQ and declared Exchange successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--> Could not connect to RabbitMQ: {ex.Message}");
            }
        }

        // Внимание: сигнатура метода теперь асинхронная и возвращает Task
        public async Task PublishSpecializationStatusChanged(string specializationId, string newStatus)
        {
            // Ожидаем завершения инициализации, если она еще идет
            await _initializationTask;

            if (_connection != null && _connection.IsOpen && _channel != null)
            {
                var messagePayload = new 
                { 
                    SpecializationId = specializationId, 
                    Status = newStatus, 
                    Event = "SpecializationStatusChanged" 
                };
                
                var message = JsonSerializer.Serialize(messagePayload);
                var body = Encoding.UTF8.GetBytes(message);

                // В v7+ используется BasicPublishAsync, принимающий ReadOnlyMemory<byte>
                await _channel.BasicPublishAsync(
                    exchange: ExchangeName, 
                    routingKey: "", 
                    mandatory: false,
                    basicProperties: new BasicProperties(), // Создаем пустые свойства по умолчанию
                    body: body
                );
                
                Console.WriteLine($"--> RabbitMQ: Sent event {newStatus} for Specialization {specializationId}");
            }
            else
            {
                Console.WriteLine("--> RabbitMQ connection is closed, cannot publish message.");
            }
        }

        // Асинхронное освобождение ресурсов
        public async ValueTask DisposeAsync()
        {
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
                Console.WriteLine($"--> Error during disposing RabbitMQ client: {ex.Message}");
            }
        }
    }
}
