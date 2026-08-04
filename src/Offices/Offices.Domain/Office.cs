using System;

namespace Offices.Domain
{
    public class Office
    {
        public string Id{ get; set; } = string.Empty;

        public string PhotoUrl { get; set; } = string.Empty;

       public string City { get; set; } = string.Empty;       // F-2 (Required)
        public string Street { get; set; } = string.Empty;     // (Required)
        public string HouseNumber { get; set; } = string.Empty; // (Required)
        public string OfficeNumber { get; set; } = string.Empty;// (Required)

        
        public string Status { get; set; } = "Active";
        public string RegistryPhoneNumber { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}