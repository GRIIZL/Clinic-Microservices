using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Auth.Application.Models
{
    public class RegisterRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(15, MinimumLength = 6, ErrorMessage = "Passwoed must be between 6 and 15 symbols.")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare("Password", ErrorMessage = "Dont match.")]
        public String ConfirmPassword { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;
    }
}