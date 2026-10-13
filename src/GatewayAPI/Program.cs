using GatewayAPI.Configuration;
using Microsoft.AspNetCore.HttpOverrides;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;

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
            .AllowAnyMethod();
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
// Middleware Pipeline
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

await app.UseOcelot();

app.Run();
