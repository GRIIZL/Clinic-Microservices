using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Services.Application.Interfaces;
using Services.Domain;

namespace Services.Infrastructure.MongoDb.Repositories
{
    public class SpecializationRepository : ISpecializationRepository
    {
        private readonly IMongoCollection<Specialization> _collection;

        public SpecializationRepository(IConfiguration configuration)
        {
            // Подключаемся к Mongo (база ServicesDB)
            var client = new MongoClient(configuration.GetConnectionString("MongoConnection"));
            var database = client.GetDatabase("ServicesDB");
            _collection = database.GetCollection<Specialization>("Specializations");
        }

        public async Task<Specialization?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            return await _collection.Find(s => s.Id == id).FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IEnumerable<Specialization>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _collection.Find(_ => true).ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Specialization specialization, CancellationToken cancellationToken = default)
        {
            await _collection.InsertOneAsync(specialization, null, cancellationToken);
        }

        public async Task UpdateAsync(Specialization specialization, CancellationToken cancellationToken = default)
        {
            await _collection.ReplaceOneAsync(s => s.Id == specialization.Id, specialization, cancellationToken:cancellationToken);
        }
    }
}
