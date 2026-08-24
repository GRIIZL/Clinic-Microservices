using System;
using System.Collections.Generic;

namespace Services.Domain
{
    public class MedicalService
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty; 
        public decimal Price { get; set; } 
        public string Status { get; set; } = "Active"; 
        

        public string CategoryName { get; set; } = "Consultations"; 
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
