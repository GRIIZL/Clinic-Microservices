using System;

namespace Profiles.Domain
{
    public class ReceptionistProfile
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AccountId { get; set; } // Связь с логином из Auth
        
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        
        // Внешний строковый ключ офиса из MongoDB (F-5)
        public string OfficeId { get; set; } = string.Empty; 
        public string PhotoUrl { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
   