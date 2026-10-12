using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Services.Application.Interfaces;

namespace Services.IntegrationTests.Fakes
{
    /// <summary>
    /// Простейшая реализация IEventPublisher для интеграционных тестов:
    /// вместо реального RabbitMQ складывает опубликованные события в память,
    /// чтобы затем проверить их состав и содержимое.
    /// </summary>
    public class RecordingEventPublisher : IEventPublisher
    {
        private readonly List<object> _publishedEvents = new();

        public IReadOnlyList<object> PublishedEvents => _publishedEvents;

        public Task PublishAsync<T>(T eventMessage, CancellationToken cancellationToken = default) where T : class
        {
            _publishedEvents.Add(eventMessage);
            return Task.CompletedTask;
        }

        public IEnumerable<T> EventsOfType<T>() => _publishedEvents.OfType<T>();
    }
}
