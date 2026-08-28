using Services.Application.Interfaces;
using Services.Application.Services;
using Services.Infrastructure.MongoDb.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Добавляем поддержку контроллеров (Решает прошлую ошибку со службами!)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Настройка CORS политик, чтобы фронтенд мог обращаться к порту 5230 без блокировок браузера
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// Регистрация инфраструктуры NoSQL MongoDB
builder.Services.AddScoped<ISpecializationRepository, SpecializationRepository>();

// Регистрация сервиса бизнес-логики
builder.Services.AddScoped<SpezializationService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthorization();

app.UseDefaultFiles(); // Ищет index.html в wwwroot по умолчанию
app.UseStaticFiles();  // Разрешает серверу отдавать HTML/JS файлы

// Маппим эндпоинты контроллеров сервиса услуг
app.MapControllers();

app.Run();
