using MongoDB.Driver;
using Services.Application.Interfaces;
using Services.Application.Services;
using Services.Infrastructure.MongoDb.Configuration;
using Services.Infrastructure.MongoDb.Repositories;
using Services.Infrastructure.RabbitMQ;

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

// Регистрация инфраструктуры NoSQL MongoDB.
// MongoClient — singleton: он потокобезопасен и сам держит пул соединений,
// поэтому одно соединение переиспользуется всеми запросами приложения.
builder.Services.AddSingleton<IMongoClient>(_ =>
    new MongoClient(builder.Configuration.GetConnectionString("MongoConnection")));

// Имена базы и коллекции вынесены в конфигурацию (секция "MongoDb"), а не зашиты в репозиторий
builder.Services.Configure<MongoDbOptions>(builder.Configuration.GetSection(MongoDbOptions.SectionName));

builder.Services.AddScoped<ISpecializationRepository, SpecializationRepository>();

// Регистрация сервиса бизнес-логики
builder.Services.AddScoped<SpezializationService>();

// Регистрация шины MassTransit (RabbitMQ)
builder.Services.AddServicesMessaging(builder.Configuration);

// Регистрация RabbitMQ Publisher (публикует события об изменении специализаций)
builder.Services.AddScoped<IEventPublisher, MassTransitEventPublisher>();

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
