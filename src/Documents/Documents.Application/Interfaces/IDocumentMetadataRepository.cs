using System;
using System.Threading;
using System.Threading.Tasks;
using Documents.Domain;

namespace Documents.Application.Interfaces
{
    public interface IDocumentMetadataRepository
    {
        Task<DocumentMetadata?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<DocumentMetadata?> GetByEntityIdAsync(Guid entityId, CancellationToken cancellationToken = default);
        Task AddAsync(DocumentMetadata metadata, CancellationToken cancellationToken = default);
    }
}
