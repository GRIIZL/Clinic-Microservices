using System;
using System.Text;
using System.Threading.Tasks;
using Auth.Infrastructure.Redis;
using Auth.Infrastructure.RabbitMQ;
using AuthorizationAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Auth.Application.Configuration;
using Auth.Application.Interfaces;
using Auth.Application.Services;
using Auth.Infrastructure.PostgreSql.Data;
using Auth.Infrastructure.PostgreSql.Repositories;

// Инфраструктура (Postgres, Redis, RabbitMQ, MailHog, MinIO) поднимается вручную:
// docker compose up -d. Автозапуск и авто-остановка контейнеров из приложения убраны —
// сервис не должен управлять чужим жизненным циклом.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
//builder.Services.AddSwashbuckleSwaggerUi(); // Наш UI

// 1. Подключаем PostgreSQL
builder.Services.AddDbContext<DataContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Подключаем Redis (строка подключения берётся из конфигурации, секция ConnectionStrings:Redis)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis")
        ?? throw new InvalidOperationException("Connection string 'Redis' is not configured.");
});

// 3. Внедряем зависимости по SOLID (Интерфейс -> Реализация)
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();
// Кеш на Redis (IDistributedCache уже зарегистрирован выше через AddStackExchangeRedisCache)
builder.Services.AddScoped<Auth.Application.Interfaces.ICacheService, Auth.Infrastructure.Redis.RedisCacheService>();
// Шина сообщений: MassTransit + RabbitMQ (топология, повторы и соединения на стороне библиотеки)
builder.Services.AddAuthMessaging(builder.Configuration);
// Публикатор событий: адаптер над MassTransit IPublishEndpoint
builder.Services.AddScoped<IEventPublisher, MassTransitEventPublisher>();
// Отправка писем: SMTP-настройки берутся из секции "Email" (appsettings / user-secrets).
// Письма уходят через MailKit на SMTP-сервер; локально это MailHog.
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.AddSingleton<IEmailSender, MailKitEmailSender>();
builder.Services.AddScoped<AuthService>();

// 4. JWT Bearer аутентификация: токен валидируется стандартно, а через OnTokenValidated
// дополнительно проверяется blacklist в Redis. Это делает отозванные при sign-out токены
// реально невалидными до истечения их срока.
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero // без запаса: срок действия = ровно 15 минут из TokenService
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var rawToken = context.SecurityToken as Microsoft.IdentityModel.JsonWebTokens.JsonWebToken;
                var tokenValue = rawToken?.EncodedToken;
                if (string.IsNullOrEmpty(tokenValue))
                {
                    context.Fail("Token could not be resolved for blacklist check.");
                    return;
                }

                var blacklist = context.HttpContext.RequestServices
                    .GetRequiredService<ITokenBlacklistService>();

                if (await blacklist.IsTokenBlacklistedAsync(tokenValue, context.HttpContext.RequestAborted))
                {
                    context.Fail("Token has been revoked (logged out).");
                }
            }
        };
    });

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

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

app.Run();
