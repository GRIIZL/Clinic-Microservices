using System.ComponentModel.DataAnnotations;

namespace Auth.Application.Models
{
    public class RefreshTokenRequestDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
