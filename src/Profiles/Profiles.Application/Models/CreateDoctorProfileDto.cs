using System;
using System.ComponentModel.DataAnnotations;

namespace Profiles.Application.Models
{
    public class CreateDoctorProfileDto
    {
        public string? PhotoUrl { get; set; }

        [Required(ErrorMessage = "Please, enter the first name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please, enter the last name")]
        public string LastName { get; set; } = string.Empty;

        public string? MiddleName { get; set; }

        [Required(ErrorMessage = "Please, select the date")]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Please, select the mail")]
        [EmailAddress(ErrorMessage = "You've entered an invalid email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please, choose the specialization")]
        public string Specialization { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please, choose the office")] 
        public string OfficeId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please, select the year")]
        public int CareerStartYear { get; set; }

        [Required(ErrorMessage = "Status is required")] 
        public string Status { get; set; } = "At work";

    }
}