using Microsoft.Extensions.Options;

namespace GatewayAPI.Configuration;

/// <summary>
/// Middleware Gateway, который:
/// 1. Проверяет путь запроса.
/// 2. Если путь входит в публичный whitelist (sign-in, register, check-email, verify, refresh) — пропускает.
/// 3. Для всех остальных маршрутов требует валидный JWT-токен.
///
/// JWT-валидация (подпись, issuer, audience, lifetime) выполняется
/// через стандартный JwtBearer middleware, который зарегистрирован выше.
/// Этот middleware только решает, нужно ли требовать авторизацию для конкретного пути.
/// </summary>
public class GatewayAuthorizationMiddleware
{
    private readonly RequestDelegate _next;

    public GatewayAuthorizationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IOptions<PublicPathsOptions> publicPaths)
    {
        var path = context.Request.Path.Value?.TrimStart('/') ?? string.Empty;

        // Проверяем, является ли путь публичным (по префиксу)
        var isPublic = publicPaths.Value.Paths.Any(p =>
            path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        if (isPublic)
        {
            await _next(context);
            return;
        }

        // Защищённый маршрут — проверяем, авторизован ли запрос
        if (context.User.Identity?.IsAuthenticated != true)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Access denied. Valid token is required."
            });
            return;
        }

        await _next(context);
    }
}

public static class GatewayAuthorizationExtensions
{
    public static IApplicationBuilder UseGatewayAuthorization(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GatewayAuthorizationMiddleware>();
    }
}
