using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace AuthorizationAPI.Configuration
{
    /// <summary>
    /// Вынесенная конфигурация JWT-аутентификации из Program.cs.
    /// Делает точку входа читаемой и позволяет легко менять параметры.
    /// </summary>
    public static class JwtBearerConfiguration
    {
        public static void AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSecret = configuration["Jwt:Secret"]
                ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = configuration["Jwt:Issuer"],
                        ValidAudience = configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                        ClockSkew = TimeSpan.Zero
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
                                .GetRequiredService<Auth.Application.Interfaces.ITokenBlacklistService>();

                            if (await blacklist.IsTokenBlacklistedAsync(tokenValue, context.HttpContext.RequestAborted))
                            {
                                context.Fail("Token has been revoked (logged out).");
                            }
                        }
                    };
                });
        }
    }
}
