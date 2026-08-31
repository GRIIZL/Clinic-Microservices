using Microsoft.EntityFrameworkCore;
using Appointments.Domain;
using System;

namespace Appointments.Infrastructure.PostgreSql.Data
{
    public class AppointmentsDataContext : DbContext
    {
        public AppointmentsDataContext(DbContextOptions<AppointmentsDataContext> options) : base(options) { }

        public DbSet<Appointment> Appointments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Appointment>().HasKey(a => a.Id);

            // Внедряем нашу прошлую «прививку» от конфликтов UTC часовых поясов Postgres!
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }
    }
}
