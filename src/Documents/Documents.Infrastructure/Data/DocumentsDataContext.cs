using Microsoft.EntityFrameworkCore;
using Documents.Domain;
using System;

namespace Documents.Infrastructure.Data
{
    public class DocumentsDataContext : DbContext
    {
        public DocumentsDataContext(DbContextOptions<DocumentsDataContext> options) : base(options) { }

        public DbSet<DocumentMetadata> DocumentsMetadata { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<DocumentMetadata>().HasKey(d => d.Id);
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }
    }
}
