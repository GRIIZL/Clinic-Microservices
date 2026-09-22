using System;
using System.Diagnostics;
using System.IO;
using Profiles.Infrastructure.PostgreSql;
using Profiles.Infrastructure.RabbitMQ;
using ProfilesAPI.Controllers;
using Profiles.Application.Services;
using Microsoft.EntityFrameworkCore;
using Profiles.Application.Interfaces;
using Profiles.Infrastructure.PostgreSql.Data;
using Profiles.Infrastructure.PostgreSql.Repositories;

// «Прививка» от конфликтов UTC часовых поясов Postgres — один раз при старте приложения,
// а не при каждом создании модели DbContext
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();


builder.Services.AddDbContext<ProfilesDataContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
builder.Services.AddScoped<IReceptionistRepository, ReceptionistRepository>(); 
builder.Services.AddScoped<PatientService>();
builder.Services.AddScoped<DoctorService>();
builder.Services.AddScoped<ReceptionistService>();
builder.Services.AddScoped<IPatientEventHandlingService, PatientEventHandlingService>();

// Шина MassTransit (RabbitMQ): consumer события UserRegisteredEvent + retry/DLQ
builder.Services.AddProfilesMessaging(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();
app.UseDefaultFiles(); // Ищет index.html в wwwroot по умолчанию
app.UseStaticFiles();  // Разрешает серверу отдавать HTML/JS файлы
app.Run();
