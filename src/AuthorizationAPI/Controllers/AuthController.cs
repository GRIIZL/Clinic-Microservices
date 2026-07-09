using AuthorizationAPI.Models;
using AuthorizationAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using System.IdentityModel.Tokens.Jwt;

namespace AuthorizationAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpGet("check-email")]
        public async Task<IActionResult> CheckEmail([FromQuery] string email)
        {
            var exists = await _authService.IsEmailRegisteredAsync(email);
            return Ok(new {exists = exists});
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.RegisterPatientAsync(request);
            return Ok(new { message = "Success registration. Check your email to confirm."});
        }

        [HttpGet("verify")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token)) return BadRequest(new {message = "Token doesn't exist."});

            var result = await _authService.VerifyEmailAsync(token);
            if (!result)
            {
                return BadRequest(new { message = "Invalid or expired token."});
            }

            return Ok(new { message = "Email verification succeed! Now you enter the system."});
        }

        [HttpPost("sign-in")]
        public async Task<IActionResult> SignIn([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var authResult = await _authService.LoginAsync(request);
            if (authResult == null)
            {
               return Unauthorized(new { message = "Either an email or a password is incorrect." });
            }

            return Ok(authResult);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var authResult = await _authService.RefreshTokenAsync(request.RefreshToken);
            if (authResult == null)
            {
                return Unauthorized(new { message = "Invalid or expired refresh token." });
            }

            return Ok(authResult);
        }

        [HttpPost("sign-out")]
        public IActionResult SignOutUser()
        {
            var authHeader = Request.Headers["Authorization"].ToString();

            if(string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer"))
            {
                return BadRequest(new {message = "Token missing or malformed."});
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var expiry = jwtToken.ValidTo;

                var blacklistService = HttpContext.RequestServices.GetRequiredService<TokenBlacklistService>();
                blacklistService.BlacklistToken(token, expiry);

                return Ok(new {message = "You've logged out successfully. Token invalidated."});
            }
            catch
            {
                return BadRequest(new { message = "Could not process token destruction."});
            }
        }
    }
}