using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Auth.Application.Interfaces;
using Auth.Application.Models;
using Auth.Application.Services;

namespace AuthorizationAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly ITokenBlacklistService _tokenBlacklistService;

        public AuthController(AuthService authService, ITokenBlacklistService tokenBlacklistService)
        {
            _authService = authService;
            _tokenBlacklistService = tokenBlacklistService;
        }

        [HttpGet("check-email")]
        public async Task<IActionResult> CheckEmail([FromQuery] string email, CancellationToken cancellationToken)
        {
            var exists = await _authService.IsEmailRegisteredAsync(email, cancellationToken);
            return Ok(new { exists = exists });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _authService.RegisterPatientAsync(request, cancellationToken);
            if (!result)
            {
                return BadRequest(new { message = "Пользователь с таким email уже существует." });
            }

            return Ok(new { message = "Регистрация успешна. Проверьте почту для подтверждения." });
        }

        [HttpGet("verify")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(token)) return BadRequest(new { message = "Токен отсутствует." });

            var result = await _authService.VerifyEmailAsync(token, cancellationToken);
            if (!result)
            {
                return BadRequest(new { message = "Невалидный или просроченный токен." });
            }

            return Ok(new { message = "Email успешно подтвержден! Теперь вы можете войти в систему." });
        }

        [HttpPost("sign-in")]
        public async Task<IActionResult> SignIn([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var authResult = await _authService.LoginAsync(request, cancellationToken);
            if (authResult == null)
            {
                return Unauthorized(new { message = "Either an email or a password is incorrect." });
            }

            return Ok(authResult);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto
         request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var authResult = await _authService.RefreshTokenAsync(request.RefreshToken, cancellationToken);
            if (authResult == null)
            {
                return Unauthorized(new { message = "Invalid or expired refresh token." });
            }

            return Ok(authResult);
        }

        // Требует валидный (и не отозванный) токен: сам факт допуска к методу означает,
        // что middleware JWT уже проверил подпись, срок и blacklist.
        [Authorize]
        [HttpPost("sign-out")]
        public async Task<IActionResult> SignOutUser(CancellationToken cancellationToken)
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length).Trim();

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var expiry = jwtToken.ValidTo;

                await _tokenBlacklistService.BlacklistTokenAsync(token, expiry, cancellationToken);

                return Ok(new { message = "You've logged out successfully. Token invalidated." });
            }
            catch
            {
                return BadRequest(new { message = "Could not process token destruction." });
            }
        }
    }
}
