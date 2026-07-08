using System;

namespace AuthorizationAPI.Data
{
    public class Account
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get;set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsEmailVerified { get; set; } = false;
        public string Role { get;set; } = string.Empty;

        public string? VerificationToken { get; set; }
        public DateTime? VerificationTokenExpires { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}