using System;
using System.ComponentModel.DataAnnotations;

namespace Profiles.Application.Models
{
    public class UpdateDoctorProfileDto
    {
        public string? PhotoUrl { get; set; } // Required: no (F-1)

        [Required(ErrorMessage = "Please, enter the first name")] // F-2
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please, enter the last name")] // F-3
        public string LastName { get; set; } = string.Empty;

        public string? MiddleName { get; set; } // Required: no (F-4)

        [Required(ErrorMessage = "Please, select the date")] // F-5
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Please, choose the specialization")] // F-7
        public string Specialization { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please, choose the office")] // F-8
        public string OfficeId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please, select the year")] // F-9
        public int CareerStartYear { get; set; }

        [Required(ErrorMessage = "Status is required")] // F-10
        public string Status { get; set; } = "At work";
    }
}
