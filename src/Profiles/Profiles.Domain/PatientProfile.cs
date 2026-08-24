using System;

namespace Profiles.Domain
{
    public class PatientProfile
    {
     public Guid Id { get; set; } = Guid.NewGuid();

     public Guid? AccountId { get; set; }

     public bool IsLinkedToAccount { get; set; } = false;

     public string PhotoUrl {get; set; } = string.Empty;
     public string FirstName { get; set; } = string.Empty;
     public string LastName { get; set; } = string.Empty;
     public string MiddleName { get; set; } = string.Empty;

     public string PhoneNumber { get; set; } = string.Empty;
     public DateTime DateOfBirth { get; set; }

     public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
     public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; 
    }
}