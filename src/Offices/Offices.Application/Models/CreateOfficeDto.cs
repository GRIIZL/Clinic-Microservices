using System.ComponentModel.DataAnnotations;

namespace Offices.Application.Models
{
    public class CreateOfficeDto
    {
        public string PhotoUrl { get; set; } = string.Empty; // Необязательное по ТЗ (F-1 Required: no)

        [Required(ErrorMessage = "City is required.")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Street is required.")]
        public string Street { get; set; } = string.Empty;

        [Required(ErrorMessage = "House number is required.")]
        public string HouseNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Office number is required.")]
        public string OfficeNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Status is required.")]
        public string Status { get; set; } = "Active";

        [Required(ErrorMessage = "Registry phone number is required.")]
        [RegularExpression(@"^\+?[1-9]\d{1,14}$", ErrorMessage = "You've entered an invalid phone number")]
        public string RegistryPhoneNumber { get; set; } = string.Empty;
    }
}
