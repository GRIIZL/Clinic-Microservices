using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using Xunit;

namespace Services.IntegrationTests.Fixtures
{
    /// <summary>
    /// Поднимает одноразовый контейнер MongoDB на время прогона тестов.
    /// Контейнер стартует один раз на всю коллекцию тестов (см. MongoCollectionFixture),
    /// поэтому тесты не зависят от локально запущенного docker-compose.
    /// </summary>
    public class MongoDbFixture : IAsyncLifetime
    {
        private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:7.0")
            .Build();

        public IMongoClient Client { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            await _container.StartAsync();
            Client = new MongoClient(_container.GetConnectionString());
        }

        public async Task DisposeAsync()
        {
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// Определение коллекции xUnit: одна фикстура MongoDbFixture шарится между всеми тестовыми классами,
    /// помеченными [Collection(MongoCollectionFixture.Name)].
    /// </summary>
    [CollectionDefinition(Name)]
    public class MongoCollectionFixture : ICollectionFixture<MongoDbFixture>
    {
        public const string Name = "Mongo";
    }
}
