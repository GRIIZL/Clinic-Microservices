using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Auth.Application.Interfaces;
using Auth.Application.Models;
using Auth.Domain;

namespace Auth.Application.Services
{
    public class AuthService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IAccountRepository accountRepository,
            ITokenService tokenService,
            ILogger<AuthService> logger)
        {
            _accountRepository = accountRepository;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<bool> IsEmailRegisteredAsync(string email)
        {
            if(string.IsNullOrWhiteSpace(email)) return false;
            return await _accountRepository.ExistByEmailAsync(email.ToLower().Trim());
        }

        public async Task<bool> RegisterPatientAsync(RegisterRequestDto request)
        {
            if (await IsEmailRegisteredAsync(request.Email)) return false;

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            string verificationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

            var newAccount = new Account
            {
                Email = request.Email.ToLower().Trim(),
                PasswordHash = passwordHash,
                PhoneNumber = request.PhoneNumber ?? string.Empty,
                Role = "Patient",
                VerificationToken = verificationToken,
                VerificationTokenExpires = DateTime.UtcNow.AddHours(24)
            };

            await _accountRepository.AddAsync(newAccount);

            var confirmationLink = $"http://localhost:api/auth/verify?token={verificationToken}";
            _logger.LogInformation($"\n===============================================\n" +
                                    $"SENDING EMAIL ON: {newAccount.Email}\n" +
                                    $"To confirm registration go to:\n{confirmationLink}\n" +
                                    $"==================================================");
            return true;
        }

        public async Task<bool> VerifyEmailAsync(string token)
        {
            var account = await _accountRepository.GetByVerificationTokenAsync(token);
            if (account == null || account.VerificationTokenExpires < DateTime.UtcNow) return false;

            account.IsEmailVerefied = true;
            account.VerificationToken = null;
            account.VerificationTokenExpires = null;
            account.UpdatedAt = DateTime.UtcNow;

            await _accountRepository.UpdateAsync(account);
            return true;
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto request)
        {
            var account = await _accountRepository.GetByEmailAsync(request.Email);
            if (account == null) return null;

            if(!BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash)) return null;

            var accessToken = _tokenService.GenerateAccessToken(account);
            var refreshToken = _tokenService.GenerateRefreshToken();

            account.RefreshToken = refreshToken;
            account.RefreshTokenExpires = DateTime.UtcNow.AddDays(7);
            account.UpdatedAt = DateTime.UtcNow;

            await _accountRepository.UpdateAsync(account);
            
            return new AuthResponseDto { AccessToken = accessToken, RefreshToken = refreshToken };
        }

        public async Task<AuthResponseDto?> RefreshTokenAsync(string token)
        {
            var account = await _accountRepository.GetByRefreshTokenAsync(token);
            if (account == null || account.RefreshTokenExpires < DateTime.UtcNow) return null;

            var newAccessToken = _tokenService.GenerateAccessToken(account);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            account.RefreshToken = newRefreshToken;
            account.RefreshTokenExpires = DateTime.UtcNow.AddDays(7);
            account.UpdatedAt = DateTime.UtcNow;

            await _accountRepository.UpdateAsync(account);

            return new AuthResponseDto { AccessToken = newAccessToken, RefreshToken = newRefreshToken };
        }
        
    }
}