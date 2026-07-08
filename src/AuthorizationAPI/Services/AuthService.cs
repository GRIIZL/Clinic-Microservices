using AuthorizationAPI.Data;
using AuthorizationAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Numerics;
using System.Security.Cryptography;

namespace AuthorizationAPI.Services
{
    public class AuthService
    {
        private readonly DataContext _context;
        private readonly ILogger<AuthService> _logger;

        public AuthService(DataContext context, ILogger<AuthService> logger)
        {
            _context = context;
            _logger = logger; 
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

            var confirmationLink = $"https://localhost:5001/api/auth/verefy?token={verificationToken}";
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
    }
}