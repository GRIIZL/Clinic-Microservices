using System;
using System.Diagnostics;
using System.IO;
using Auth.Infrastructure.Redis;
using AuthorizationAPI.Services;
using Microsoft.EntityFrameworkCore;
using Auth.Application.Interfaces;
using Auth.Application.Services;
using Auth.Infrastructure.PostgreSql.Data;
using Auth.Infrastructure.PostgreSql.Repositories;

// Автоматический запуск баз Postgres и Redis в Docker
try
{
    var startInfo = new ProcessStartInfo
    {
        FileName = "docker-compose",
        Arguments = "up -d",
        WorkingDirectory = @"C:\Projects\Authorization-Service",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };
    using var process = Process.Start(startInfo);
    process?.WaitForExit();
    Console.WriteLine("[Docker Auto-Start]: Инфраструктура в Docker (Postgres + Redis) запущена.");
}
catch (Exception ex)
{
    Console.WriteLine($"[Docker Auto-Start Warning]: {ex.Message}");
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
//builder.Services.AddSwashbuckleSwaggerUi(); // Наш UI

// 1. Подключаем PostgreSQL
builder.Services.AddDbContext<DataContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Подключаем Redis (Задаем локальный порт из docker-compose)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});

// 3. Внедряем зависимости по SOLID (Интерфейс -> Реализация)
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();
builder.Services.AddScoped<AuthService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Authorization API v1"));
    
    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
        }
    });
}

// Авто-остановка контейнеров
app.Lifetime.ApplicationStopping.Register(() =>
{
    try
    {
        var stopInfo = new ProcessStartInfo
        {
            FileName = "docker-compose",
            Arguments = "down",
            WorkingDirectory = @"C:\Projects\Authorization-Service",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var stopProcess = Process.Start(stopInfo);
        stopProcess?.WaitForExit();
    }
    catch { }
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

app.Run();
