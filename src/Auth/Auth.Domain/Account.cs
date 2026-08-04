using System;

namespace Auth.Domain
{
    public class Account
    {
        public Guid Id{ get; set; } = Guid.NewGuid();
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsEmailVerefied { get; set; } = false;
        public string Role{ get; set; } = string.Empty;

        public string? VerificationToken { get; set; }
        public DateTime? VerificationTokenExpires { get; set; }

        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpires { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}