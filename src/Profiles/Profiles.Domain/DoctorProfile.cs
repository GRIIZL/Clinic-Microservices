using System;
using System.Globalization;

namespace Profiles.Domain
{
    public class DoctorProfile
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid AccountId { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string MiddleName{ get; set; } = string.Empty;

        public string FullName => $"{LastName} {FirstName} {MiddleName}".Trim();

        public string Email { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }

        public string PhotoUrl { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public string OfficeId{ get; set; } = string.Empty;

        public string Status { get; set; } = "At work";

        public int CareerStartYear { get; set; }

        public int Experience => DateTime.UtcNow.Year - CareerStartYear + 1;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}