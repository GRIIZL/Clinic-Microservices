using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Documents.Application.Interfaces;
using Documents.Domain;
using Documents.Infrastructure.Data;

namespace Documents.Infrastructure.Repositories
{
    public class DocumentMetadataRepository : IDocumentMetadataRepository
    {
        private readonly DocumentsDataContext _context;

        public DocumentMetadataRepository(DocumentsDataContext context)
        {
            _context = context;
        }

        public async Task<DocumentMetadata?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.DocumentsMetadata.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        }

        public async Task<DocumentMetadata?> GetByEntityIdAsync(Guid entityId, CancellationToken cancellationToken = default)
        {
            return await _context.DocumentsMetadata.FirstOrDefaultAsync(d => d.RelatedEntityId == entityId, cancellationToken);
        }

        public async Task AddAsync(DocumentMetadata metadata, CancellationToken cancellationToken = default)
        {
            await _context.DocumentsMetadata.AddAsync(metadata, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
