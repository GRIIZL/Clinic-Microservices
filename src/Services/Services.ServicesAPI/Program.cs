using Services.Application.Interfaces;
using Services.Application.Services;
using Services.Infrastructure.MongoDb.Messaging;
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
builder.Services.AddScoped<ServicesService>();

// Регистрация клиента шины сообщений RabbitMQ (Task #28) как Синглтон
builder.Services.AddSingleton<IMessageBusClient, MessageBusClient>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthorization();

// Маппим эндпоинты контроллеров сервиса услуг
app.MapControllers();

app.Run();
