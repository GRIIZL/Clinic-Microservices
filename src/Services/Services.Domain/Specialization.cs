using System;
using System.Collections.Generic;

namespace Services.Domain
{
    public class Specialization
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty; 
        public string Status { get; set; } = "Active"; 
        public List<MedicalService> Services { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
