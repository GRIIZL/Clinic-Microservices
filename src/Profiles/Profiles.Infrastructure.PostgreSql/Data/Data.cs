using Microsoft.EntityFrameworkCore;
using Profiles.Domain;

namespace Profiles.Infrastructure.PostgreSql.Data
{
    public class ProfilesDataContext : DbContext
    {
        public ProfilesDataContext(DbContextOptions<ProfilesDataContext> options) : base(options) { }

        public DbSet<DoctorProfile> Doctors { get; set; }
        public DbSet<PatientProfile> Patients { get; set; }
        public DbSet<ReceptionistProfile> Receptionists { get; set; }


       protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<DoctorProfile>().HasKey(d => d.Id);
            modelBuilder.Entity<PatientProfile>().HasKey(p => p.Id);

            // ФИКС ДЛЯ 500 ОШИБКИ POSTGRESQL: 
            // Принудительно отключаем проблемы несовместимости DateTime часовых поясов
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }
    }
}