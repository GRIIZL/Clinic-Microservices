using GatewayAPI.Configuration;
using Microsoft.AspNetCore.HttpOverrides;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;
using Shared.Security;

var builder = WebApplication.CreateBuilder(args);

// Ocelot JSON-конфиг
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// ============================================
// CORS
// ============================================
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("GatewayPolicy", policy =>
    {
        policy
            .WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // обязательно: фронт будет слать Authorization-заголовок
    });
});

// ============================================
// Forwarded Headers (если шлюз работает за reverse-proxy)
// ============================================
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// ============================================
// JWT Bearer — единая точка валидации токенов.
// Все бизнес-сервисы (Appointments, Profiles, Offices, Services, Documents)
// не знают о JWT — они получают уже авторизованный контекст от Gateway.
// ============================================
builder.Services.AddSharedJwtAuthentication(builder.Configuration);

// ============================================
// Публичные пути — маршруты, которые НЕ требуют авторизации.
// Настройка из секции "PublicPaths" в appsettings.json.
// ============================================
builder.Services.Configure<PublicPathsOptions>(
    builder.Configuration.GetSection(PublicPathsOptions.SectionName));

// ============================================
// Ocelot + Polly
// ============================================
builder.Services.AddOcelot(builder.Configuration)
    .AddPolly()
    .AddDelegatingHandler<PollyResilienceHandler>();

// ============================================
// Swagger (для Gateway UI)
// ============================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Clinic API Gateway", Version = "v1" });
});

var app = builder.Build();

// ============================================
// Middleware Pipeline (порядок важен!)
// ============================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1.json", "Clinic API Gateway v1");
    });
}

app.UseForwardedHeaders();
app.UseCors("GatewayPolicy");

// Запрос ID для трейсинга
app.Use(async (context, next) =>
{
    var requestId = context.Request.Headers["X-Request-Id"].FirstOrDefault()
        ?? Guid.NewGuid().ToString("N")[..8];
    context.TraceIdentifier = requestId;
    context.Response.Headers.Append("X-Request-Id", requestId);
    await next();
});

// 1. JWT-валидация — проверяет подпись, issuer, audience, lifetime.
//    Если токен невалиден — сразу 401.
app.UseAuthentication();

// 2. Точечная авторизация — решает, нужно ли требовать аутентификацию
//    для конкретного маршрута (публичные vs защищённые).
app.UseGatewayAuthorization();

// 3. Ocelot — маршрутизирует запрос к бизнес-сервисам.
//    На этом этапе контекст уже авторизован (или запрос был публичным).
await app.UseOcelot();

app.Run();
