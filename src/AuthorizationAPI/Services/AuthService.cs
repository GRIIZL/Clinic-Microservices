using AuthorizationAPI.Data;
using AuthorizationAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Numerics;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthorizationAPI.Services
{
    public class AuthService
    {
        private readonly DataContext _context;
        private readonly ILogger<AuthService> _logger;
        private readonly IConfiguration _configuration;

        public AuthService(DataContext context, ILogger<AuthService> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger; 
            _configuration = configuration;
        }

        public async Task<bool> IsEmailRegisteredAsync(string email)
        {
            if(string.IsNullOrWhiteSpace(email)) return false;
            return await _context.Accounts.AnyAsync(a => a.Email.ToLower() == email.Trim().ToLower());
        }

        public async Task<bool> RegisterPatientAsync(RegisterRequestDto request)
        {
            if(await IsEmailRegisteredAsync(request.Email))
            {
                return false;
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            string verificationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

            var newAccount = new Account
            {
                Id = Guid.NewGuid(),
                Email = request.Email.ToLower().Trim(),
                PasswordHash = passwordHash,
                PhoneNumber = request.PhoneNumber ?? string.Empty,
                IsEmailVerified = false,
                Role = "Patient",
                VerificationToken = verificationToken,
                VerificationTokenExpires = DateTime.UtcNow.AddHours(24),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow

            };

            _context.Accounts.Add(newAccount);
            await _context.SaveChangesAsync();

            var confirmationLink = $"http://localhost:5176/api/auth/verify?token={verificationToken}";
            _logger.LogInformation($"\n==================================================\n" +
                                   $"SENDING EMAIL ON: {newAccount.Email}\n" +
                                   $"To confirm registration go to:\n{confirmationLink}\n" +
                                   $"==================================================");

            return true;                                   
        }

        public async Task<bool> VerifyEmailAsync(string token)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.VerificationToken == token);

            if (account == null || account.VerificationTokenExpires < DateTime.UtcNow)
            {
                return false;
            }

            account.IsEmailVerified = true;
            account.VerificationToken = null;
            account.VerificationTokenExpires = null;
            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto request)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Email.ToLower() == request.Email.ToLower().Trim());

            if (account == null) return null;
  
            if (!BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash))
            {
                return null;
            }

            var accessToken = GenerateJwtToken(account);
            var refreshToken = GenerateRefreshToken();

            account.RefreshToken = refreshToken;
            account.RefreshTokenExpires = DateTime.UtcNow.AddDays(7);
            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new AuthResponseDto { AccessToken = accessToken, RefreshToken = refreshToken };
        }

        public async Task<AuthResponseDto?> RefreshTokenAsync(string token)
        {
            // Ищем аккаунт, у которого совпадает рефреш-токен
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.RefreshToken == token);

            // Проверяем: существует ли токен и не просрочен ли он
            if (account == null || account.RefreshTokenExpires < DateTime.UtcNow)
            {
                return null; // Токен невалиден или просрочен
            }

            // Если всё ок — выпускаем новую пару токенов (Паттерн ротации рефреш-токенов)
            var newAccessToken = GenerateJwtToken(account);
            var newRefreshToken = GenerateRefreshToken();

            // Перезаписываем токен в базе данных ради безопасности
            account.RefreshToken = newRefreshToken;
            account.RefreshTokenExpires = DateTime.UtcNow.AddDays(7);
            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new AuthResponseDto { AccessToken = newAccessToken, RefreshToken = newRefreshToken };
        }

        private string GenerateRefreshToken()
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(64));
        }

        private string GenerateJwtToken(Account account)
        {
            var secretKey = _configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret missing");
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
                new Claim(ClaimTypes.Email, account.Email),
                new Claim(ClaimTypes.Role, account.Role)
            };

            var token = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(2), 
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwt = tokenHandler.CreateToken(token);

            return tokenHandler.WriteToken(jwt);
        }
    }
}