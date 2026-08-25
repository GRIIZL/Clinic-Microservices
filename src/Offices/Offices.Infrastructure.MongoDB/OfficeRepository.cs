using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Offices.Application.Interfaces;
using Offices.Domain;

namespace Offices.Infrastructure.MongoDB
{
    public class OfficeRepository : IOfficeRepository
    {
        private readonly IMongoCollection<MongoOfficeMapping> _collection;

        public OfficeRepository(IMongoClient mongoClient)
        {
            // Подключаемся к базе данных 'ClinicOfficesDB' и коллекции 'Offices'
            var database = mongoClient.GetDatabase("ClinicOfficesDB");
            _collection = database.GetCollection<MongoOfficeMapping>("Offices");
        }

        public async Task<Office?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var mapping = await _collection.Find(o => o.Id == id).FirstOrDefaultAsync(cancellationToken);
            return mapping?.ToDomain();
        }

        public async Task<IEnumerable<Office>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var mappings = await _collection.Find(_ => true).ToListAsync(cancellationToken);
            var offices = new List<Office>();
            foreach (var m in mappings) offices.Add(m.ToDomain());
            return offices;
        }

        public async Task AddAsync(Office office, CancellationToken cancellationToken = default)
        {
            var mapping = MongoOfficeMapping.FromDomain(office);
            await _collection.InsertOneAsync(mapping, null, cancellationToken);
        }

        public async Task UpdateAsync(Office office, CancellationToken cancellationToken = default)
        {
            var mapping = MongoOfficeMapping.FromDomain(office);
            await _collection.ReplaceOneAsync(o => o.Id == office.Id, mapping, cancellationToken:cancellationToken);
        }
    }

    // Вспомогательный внутренний класс для корректного маппинга строкового ID в ObjectId базы MongoDB
    internal class MongoOfficeMapping
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; } = string.Empty;

        public string PhotoUrl { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string HouseNumber { get; set; } = string.Empty;
        public string? OfficeNumber { get; set; }

        public string Status { get; set; } = string.Empty;
        public string RegistryPhoneNumber { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public static MongoOfficeMapping FromDomain(Office office) => new()
        {
            Id = office.Id,
            PhotoUrl = office.PhotoUrl,
            City = office.City,
            Street = office.Street,
            HouseNumber = office.HouseNumber,
            OfficeNumber = office.OfficeNumber,
            Status = office.Status,
            RegistryPhoneNumber = office.RegistryPhoneNumber,
            CreatedAt = office.CreatedAt,
            UpdatedAt = office.UpdatedAt
        };

        public Office ToDomain() => new()
        {
            Id = this.Id,
            PhotoUrl = this.PhotoUrl,
            City = this.City,
            Street = this.Street,
            HouseNumber = this.HouseNumber,
            OfficeNumber = this.OfficeNumber ?? string.Empty,
            Status = this.Status,
            RegistryPhoneNumber = this.RegistryPhoneNumber,
            CreatedAt = this.CreatedAt,
            UpdatedAt = this.UpdatedAt
        };
    }
}