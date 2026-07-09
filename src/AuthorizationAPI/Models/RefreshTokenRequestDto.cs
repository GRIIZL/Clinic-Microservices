using System.ComponentModel.DataAnnotations;

namespace AuthorizationAPI.Models
{
    public class RefreshTokenRequestDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}