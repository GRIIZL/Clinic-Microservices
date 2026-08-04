using System.ComponentModel.DataAnnotations;

namespace Offices.Application.Models
{
    public class UpdateOfficeDto
    {
        public string PhotoUrl { get; set; } = string.Empty;

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
        public string RegistryPhoneNumber { get; set; } = string.Empty;
    }
}
