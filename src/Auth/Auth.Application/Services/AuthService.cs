using System;
using System.Security.Cryptography;
using System.Threading;
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

        public async Task<bool> IsEmailRegisteredAsync(string email, CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(email)) return false;
            return await _accountRepository.ExistByEmailAsync(email.ToLower().Trim(), cancellationToken);
        }

        public async Task<bool> RegisterPatientAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
        {
            if (await IsEmailRegisteredAsync(request.Email, cancellationToken)) return false;

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

            await _accountRepository.AddAsync(newAccount, cancellationToken);

            var confirmationLink = $"http://localhost:api/auth/verify?token={verificationToken}";
            _logger.LogInformation($"\n===============================================\n" +
                                    $"SENDING EMAIL ON: {newAccount.Email}\n" +
                                    $"To confirm registration go to:\n{confirmationLink}\n" +
                                    $"==================================================");
            return true;
        }

        public async Task<bool> VerifyEmailAsync(string token, CancellationToken cancellationToken = default)
        {
            var account = await _accountRepository.GetByVerificationTokenAsync(token, cancellationToken);
            if (account == null || account.VerificationTokenExpires < DateTime.UtcNow) return false;

            account.IsEmailVerefied = true;
            account.VerificationToken = null;
            account.VerificationTokenExpires = null;
            account.UpdatedAt = DateTime.UtcNow;

            await _accountRepository.UpdateAsync(account, cancellationToken);
            return true;
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
        {
            var account = await _accountRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (account == null) return null;

            if(!BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash)) return null;

            var accessToken = _tokenService.GenerateAccessToken(account);
            var refreshToken = _tokenService.GenerateRefreshToken();

            account.RefreshToken = refreshToken;
            account.RefreshTokenExpires = DateTime.UtcNow.AddDays(7);
            account.UpdatedAt = DateTime.UtcNow;

            await _accountRepository.UpdateAsync(account, cancellationToken);
            
            return new AuthResponseDto { AccessToken = accessToken, RefreshToken = refreshToken };
        }

        public async Task<AuthResponseDto?> RefreshTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            var account = await _accountRepository.GetByRefreshTokenAsync(token, cancellationToken);
            if (account == null || account.RefreshTokenExpires < DateTime.UtcNow) return null;

            var newAccessToken = _tokenService.GenerateAccessToken(account);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            account.RefreshToken = newRefreshToken;
            account.RefreshTokenExpires = DateTime.UtcNow.AddDays(7);
            account.UpdatedAt = DateTime.UtcNow;

            await _accountRepository.UpdateAsync(account, cancellationToken);

            return new AuthResponseDto { AccessToken = newAccessToken, RefreshToken = newRefreshToken };
        }
        
    }
}