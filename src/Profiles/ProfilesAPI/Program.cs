using System;
using System.Diagnostics;
using System.IO;
using Profiles.Infrastructure.PostgreSql;
using ProfilesAPI.Controllers;
using Profiles.Application.Services;
using Microsoft.EntityFrameworkCore;
using Profiles.Application.Interfaces;
using Profiles.Infrastructure.PostgreSql.Data;
using Profiles.Infrastructure.PostgreSql.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ProfilesDataContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
builder.Services.AddScoped<IReceptionistRepository, ReceptionistRepository>(); 
builder.Services.AddScoped<PatientService>();
builder.Services.AddScoped<DoctorService>();
builder.Services.AddScoped<ReceptionistService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


app.Run();
