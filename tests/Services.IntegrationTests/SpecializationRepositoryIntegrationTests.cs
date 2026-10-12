using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Services.Domain;
using Services.Infrastructure.MongoDb.Configuration;
using Services.Infrastructure.MongoDb.Repositories;
using Services.IntegrationTests.Fixtures;
using Xunit;

namespace Services.IntegrationTests
{
    /// <summary>
    /// Интеграционные тесты SpecializationRepository против реальной MongoDB (Testcontainers).
    /// Проверяют то, что не покрывают unit-тесты: фактическую запись/чтение документов,
    /// сериализацию decimal как Decimal128 и сохранение вложенного массива услуг.
    /// </summary>
    [Collection(MongoCollectionFixture.Name)]
    public class SpecializationRepositoryIntegrationTests
    {
        private readonly MongoDbFixture _fixture;

        public SpecializationRepositoryIntegrationTests(MongoDbFixture fixture)
        {
            _fixture = fixture;
        }

        private SpecializationRepository CreateRepository(out string databaseName)
        {
            databaseName = $"ServicesIT_{Guid.NewGuid():N}";
            var options = Options.Create(new MongoDbOptions
            {
                DatabaseName = databaseName,
                CollectionName = "Specializations"
            });

            return new SpecializationRepository(_fixture.Client, options);
        }

        private static Specialization CreateSpecialization(string name = "Cardiology")
        {
            return new Specialization
            {
                Name = name,
                Status = "Active",
                Services = new List<MedicalService>
                {
                    new()
                    {
                        Name = "Consultation",
                        Price = 199.99m,
                        CategoryName = "Consultations",
                        Status = "Active"
                    }
                }
            };
        }

        [Fact]
        public async Task AddAsync_Then_GetByIdAsync_RoundTripsWholeAggregate()
        {
            var repository = CreateRepository(out _);
            var specialization = CreateSpecialization();

            await repository.AddAsync(specialization);
            var loaded = await repository.GetByIdAsync(specialization.Id);

            Assert.NotNull(loaded);
            Assert.Equal(specialization.Id, loaded!.Id);
            Assert.Equal("Cardiology", loaded.Name);
            Assert.Single(loaded.Services);

            var service = loaded.Services.Single();
            Assert.Equal("Consultation", service.Name);
            Assert.Equal(199.99m, service.Price);
            Assert.Equal("Consultations", service.CategoryName);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenDocumentMissing()
        {
            var repository = CreateRepository(out _);

            var loaded = await repository.GetByIdAsync(Guid.NewGuid().ToString());

            Assert.Null(loaded);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllInsertedDocuments()
        {
            var repository = CreateRepository(out _);
            await repository.AddAsync(CreateSpecialization("Cardiology"));
            await repository.AddAsync(CreateSpecialization("Neurology"));

            var all = (await repository.GetAllAsync()).ToList();

            Assert.Equal(2, all.Count);
            Assert.Contains(all, s => s.Name == "Cardiology");
            Assert.Contains(all, s => s.Name == "Neurology");
        }

        [Fact]
        public async Task UpdateAsync_ReplacesExistingDocument()
        {
            var repository = CreateRepository(out _);
            var specialization = CreateSpecialization();
            await repository.AddAsync(specialization);

            specialization.Name = "Updated Cardiology";
            specialization.Status = "Inactive";
            specialization.Services[0].Price = 250m;
            await repository.UpdateAsync(specialization);

            var loaded = await repository.GetByIdAsync(specialization.Id);
            Assert.NotNull(loaded);
            Assert.Equal("Updated Cardiology", loaded!.Name);
            Assert.Equal("Inactive", loaded.Status);
            Assert.Equal(250m, loaded.Services.Single().Price);

            var all = await repository.GetAllAsync();
            Assert.Single(all);
        }

        [Fact]
        public async Task UpdateAsync_DoesNotInsert_WhenDocumentMissing()
        {
            var repository = CreateRepository(out _);
            var missing = CreateSpecialization();

            await repository.UpdateAsync(missing);

            var all = await repository.GetAllAsync();
            Assert.Empty(all);
        }

        [Fact]
        public async Task AddAsync_StoresPriceAsDecimal128_AndServicesAsDocumentArray()
        {
            var repository = CreateRepository(out var databaseName);
            await repository.AddAsync(CreateSpecialization());

            // Читаем «сырой» BSON-документ, чтобы убедиться в физическом представлении полей в MongoDB.
            var rawCollection = _fixture.Client
                .GetDatabase(databaseName)
                .GetCollection<BsonDocument>("Specializations");
            var raw = await rawCollection.Find(FilterDefinition<BsonDocument>.Empty).FirstAsync();

            Assert.Equal(BsonType.Array, raw["Services"].BsonType);
            var firstService = raw["Services"].AsBsonArray[0].AsBsonDocument;
            Assert.Equal(BsonType.Decimal128, firstService["Price"].BsonType);
            Assert.Equal(199.99m, firstService["Price"].AsDecimal);
        }
    }
}
