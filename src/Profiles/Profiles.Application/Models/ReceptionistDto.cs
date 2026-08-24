using System;
using System.ComponentModel.DataAnnotations;

namespace Profiles.Application.Models
{
    public class ReceptionistDto
    {
        public string? PhotoUrl { get; set; }

        [Required(ErrorMessage = "Please, enter the first name")] // F-2
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please, enter the last name")] // F-3
        public string LastName { get; set; } = string.Empty;

        public string? MiddleName { get; set; }

        [Required(ErrorMessage = "Please, enter the email")] // F-4
        [EmailAddress(ErrorMessage = "You've entered an invalid email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please, choose the office")] // F-5
        public string OfficeId { get; set; } = string.Empty;
    }
}
