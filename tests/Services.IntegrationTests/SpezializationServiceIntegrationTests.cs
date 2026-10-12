using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Services.Application.Models;
using Services.Application.Services;
using Services.Domain;
using Services.Infrastructure.MongoDb.Configuration;
using Services.Infrastructure.MongoDb.Repositories;
using Services.IntegrationTests.Fakes;
using Services.IntegrationTests.Fixtures;
using Shared.Events;
using Xunit;

namespace Services.IntegrationTests
{
    /// <summary>
    /// Сквозные интеграционные тесты: SpezializationService работает поверх настоящего
    /// SpecializationRepository и реальной MongoDB. Публикация событий подменена
    /// на RecordingEventPublisher, чтобы не поднимать RabbitMQ.
    /// </summary>
    [Collection(MongoCollectionFixture.Name)]
    public class SpezializationServiceIntegrationTests
    {
        private readonly MongoDbFixture _fixture;

        public SpezializationServiceIntegrationTests(MongoDbFixture fixture)
        {
            _fixture = fixture;
        }

        private (SpezializationService Service, SpecializationRepository Repository, RecordingEventPublisher Publisher)
            CreateSut()
        {
            var options = Options.Create(new MongoDbOptions
            {
                DatabaseName = $"ServicesIT_{Guid.NewGuid():N}",
                CollectionName = "Specializations"
            });

            var repository = new SpecializationRepository(_fixture.Client, options);
            var publisher = new RecordingEventPublisher();
            return (new SpezializationService(repository, publisher), repository, publisher);
        }

        [Fact]
        public async Task CreateSpecializationAsync_PersistsAggregate_ReadableFromRepository()
        {
            var (service, repository, _) = CreateSut();

            var created = await service.CreateSpecializationAsync(new CreateSpecializationDto
            {
                Name = "  Cardiology  ",
                Status = ServiceStatuses.Active,
                Services = new List<CreateServiceDto>
                {
                    new() { Name = "  Consultation  ", Price = 100m, CategoryName = "Consultations", Status = ServiceStatuses.Active },
                    new() { Name = "ECG", Price = 200m, CategoryName = "Diagnostics", Status = ServiceStatuses.Active }
                }
            });

            var stored = await repository.GetByIdAsync(created.Id);

            Assert.NotNull(stored);
            Assert.Equal("Cardiology", stored!.Name);
            Assert.Equal(2, stored.Services.Count);
            Assert.Equal("Consultation", stored.Services[0].Name);
            Assert.Equal(200m, stored.Services[1].Price);
        }

        [Fact]
        public async Task AddServiceToSpecializationAsync_AppendsServiceInMongo()
        {
            var (service, repository, _) = CreateSut();
            var created = await service.CreateSpecializationAsync(new CreateSpecializationDto
            {
                Name = "Cardiology",
                Status = ServiceStatuses.Active
            });

            var added = await service.AddServiceToSpecializationAsync(created.Id, new CreateMedicalServiceDto
            {
                Name = "MRI",
                Price = 500m,
                CategoryName = "Diagnostics",
                Status = ServiceStatuses.Active
            });

            var stored = await repository.GetByIdAsync(created.Id);
            Assert.True(added);
            Assert.NotNull(stored);
            Assert.Single(stored!.Services);
            Assert.Equal("MRI", stored.Services.Single().Name);
            Assert.Equal(500m, stored.Services.Single().Price);
        }

        [Fact]
        public async Task UpdateServiceInSpecializationAsync_PersistsChanges_AndPublishesEvent()
        {
            var (service, repository, publisher) = CreateSut();
            var created = await service.CreateSpecializationAsync(new CreateSpecializationDto
            {
                Name = "Cardiology",
                Status = ServiceStatuses.Active,
                Services = new List<CreateServiceDto>
                {
                    new() { Name = "Consultation", Price = 100m, CategoryName = "Consultations", Status = ServiceStatuses.Active }
                }
            });
            var serviceId = created.Services.Single().Id;

            var updated = await service.UpdateServiceInSpecializationAsync(created.Id, serviceId, new UpdateMedicalServiceDto
            {
                Name = "  Consultation (adult)  ",
                Price = 150m,
                CategoryName = "Diagnostics",
                Status = ServiceStatuses.Inactive
            });

            var stored = await repository.GetByIdAsync(created.Id);
            Assert.True(updated);
            Assert.NotNull(stored);
            var storedService = stored!.Services.Single();
            Assert.Equal("Consultation (adult)", storedService.Name);
            Assert.Equal(150m, storedService.Price);
            Assert.Equal("Diagnostics", storedService.CategoryName);
            Assert.Equal(ServiceStatuses.Inactive, storedService.Status);

            var published = Assert.Single(publisher.EventsOfType<SpecializationChangedEvent>());
            Assert.Equal(SpecializationChangeTypes.ServiceStatus, published.ChangeType);
            Assert.Equal(ServiceStatuses.Active, published.OldStatus);
            Assert.Equal(ServiceStatuses.Inactive, published.Status);
            Assert.Equal(serviceId, published.ServiceId);
        }

        [Fact]
        public async Task ChangeStatusAsync_Inactive_DeactivatesServicesInMongo_AndPublishesEvent()
        {
            var (service, repository, publisher) = CreateSut();
            var created = await service.CreateSpecializationAsync(new CreateSpecializationDto
            {
                Name = "Cardiology",
                Status = ServiceStatuses.Active,
                Services = new List<CreateServiceDto>
                {
                    new() { Name = "Consultation", Price = 100m, CategoryName = "Consultations", Status = ServiceStatuses.Active },
                    new() { Name = "ECG", Price = 200m, CategoryName = "Diagnostics", Status = ServiceStatuses.Active }
                }
            });

            var changed = await service.ChangeStatusAsync(created.Id, new ChangeSpecializationStatusDto
            {
                Status = ServiceStatuses.Inactive
            });

            var stored = await repository.GetByIdAsync(created.Id);
            Assert.True(changed);
            Assert.NotNull(stored);
            Assert.Equal(ServiceStatuses.Inactive, stored!.Status);
            Assert.All(stored.Services, s => Assert.Equal(ServiceStatuses.Inactive, s.Status));

            var published = Assert.Single(publisher.EventsOfType<SpecializationChangedEvent>());
            Assert.Equal(SpecializationChangeTypes.SpecializationStatus, published.ChangeType);
            Assert.Equal(ServiceStatuses.Inactive, published.Status);
        }

        [Fact]
        public async Task UpdateSpecializationAsync_PersistsNewName()
        {
            var (service, repository, _) = CreateSut();
            var created = await service.CreateSpecializationAsync(new CreateSpecializationDto
            {
                Name = "Cardiology",
                Status = ServiceStatuses.Active
            });

            var updated = await service.UpdateSpecializationAsync(created.Id, new UpdateSpecializationDto
            {
                Name = "  Neurology  "
            });

            var stored = await repository.GetByIdAsync(created.Id);
            Assert.True(updated);
            Assert.Equal("Neurology", stored!.Name);
        }

        [Fact]
        public async Task GetServiceByIdAsync_ReturnsPersistedService()
        {
            var (service, _, _) = CreateSut();
            var created = await service.CreateSpecializationAsync(new CreateSpecializationDto
            {
                Name = "Cardiology",
                Status = ServiceStatuses.Active,
                Services = new List<CreateServiceDto>
                {
                    new() { Name = "ECG", Price = 200m, CategoryName = "Diagnostics", Status = ServiceStatuses.Active }
                }
            });
            var serviceId = created.Services.Single().Id;

            var loaded = await service.GetServiceByIdAsync(created.Id, serviceId);

            Assert.NotNull(loaded);
            Assert.Equal("ECG", loaded!.Name);
            Assert.Equal(200m, loaded.Price);
        }
    }
}
