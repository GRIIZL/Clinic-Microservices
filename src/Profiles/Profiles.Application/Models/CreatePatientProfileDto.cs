using System;
using System.ComponentModel.DataAnnotations;

namespace Profiles.Application.Models
{
    public class CreatePatientProfileDto
    {
        public Guid? AccountId { get; set; } // Передаем, если профиль создается залогиненным юзером
        public string? PhotoUrl { get; set; }

        [Required(ErrorMessage = "Please, enter the first name")] // Текст по F-2
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please, enter the last name")] // Текст по F-3
        public string LastName { get; set; } = string.Empty;

        public string? MiddleName { get; set; }

        [Required(ErrorMessage = "Please, enter the phone number")] // Текст по F-5
        [Phone(ErrorMessage = "You've entered an invalid phone number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please, select the date")] // Текст по F-6
        public DateTime DateOfBirth { get; set; }
    }
}
