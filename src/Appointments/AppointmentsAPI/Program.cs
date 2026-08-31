using Microsoft.EntityFrameworkCore;
using Appointments.Application.Interfaces;
using Appointments.Application.Services;
using Appointments.Infrastructure.PostgreSql.Data;
using Appointments.Infrastructure.PostgreSql.Repositories;
using Appointments.Infrastructure.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ИСПРАВЛЕННЫЙ ВАРИАНТ: Явно перенаправляем сборку миграций в слой Infrastructure!
builder.Services.AddDbContext<AppointmentsDataContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("Appointments.Infrastructure.PostgreSql")
    ));

builder.Services.AddScoped<AppointmentService>();

// Регистрация слоев Clean Architecture
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();

// Регистрация обработчика событий специализаций (DIP: интерфейс → реализация)
builder.Services.AddScoped<ISpecializationEventHandlingService, SpecializationEventHandlingService>();

// Регистрация фонового сервиса RabbitMQ Consumer (BackgroundService)
builder.Services.AddHostedService<RabbitMqConsumer>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();
