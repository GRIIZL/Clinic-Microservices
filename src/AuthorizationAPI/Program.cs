using System.Diagnostics;
using AuthorizationAPI.Data;
using AuthorizationAPI.Services;
using Microsoft.EntityFrameworkCore;

// Автоматический запуск базы данных в Docker при старте приложения
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
    System.Console.WriteLine("[Docker Auto-Start]: Команда docker-compose up -d успешно отправлена.");
}
catch (Exception ex)
{
    System.Console.WriteLine($"[Docker Auto-Start Warning]: Не удалось запустить docker-compose автоматически: {ex.Message}");
}


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDbContext<DataContext>(options => 
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<AuthService>();

var app = builder.Build();

app.Lifetime.ApplicationStopping.Register(() =>
{
    try
    {
        System.Console.WriteLine("[Docker Auto-Stop]: Останавливаем базу данных в Docker...");
        var stopInfo = new ProcessStartInfo
        {
            FileName = "docker-compose",
            Arguments = "down", 
            WorkingDirectory = @"C:\Projects\Authorization-Service",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var stopProcess = Process.Start(stopInfo);
        stopProcess?.WaitForExit();
        System.Console.WriteLine("[Docker Auto-Stop]: Контейнеры успешно остановлены.");
    }
    catch (Exception ex)
    {
        System.Console.WriteLine($"[Docker Auto-Stop Warning]: Не удалось остановить Docker: {ex.Message}");
    }
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Authorization API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();
app.UseDefaultFiles(); 
app.UseStaticFiles();  
app.MapControllers();

app.Run();