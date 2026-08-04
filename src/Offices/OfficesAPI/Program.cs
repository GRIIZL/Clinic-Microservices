using MongoDB.Driver;
using System.Diagnostics;
using Offices.Application.Interfaces;
using Offices.Application.Services;
using Offices.Infrastructure.MongoDB;

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
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); // Используем стандартный Swagger для удобства

// 1. Регистрируем официальный клиент NoSQL MongoDB в DI
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoConnection");
builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));

// 2. Связываем интерфейсы и реализации Чистой Архитектуры Офисов
builder.Services.AddScoped<IOfficeRepository, OfficeRepository>();
builder.Services.AddScoped<OfficeService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();
app.MapControllers();

app.Run();
