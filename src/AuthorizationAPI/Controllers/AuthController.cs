using AuthorizationAPI.Models;
using AuthorizationAPI.Services;
using Microsoft.AspNetCore.Mvc;

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
    }
}