using System.ComponentModel.DataAnnotations;

namespace Services.Application.Models
{
    public class UpdateMedicalServiceDto
    {
        [Required(ErrorMessage = "Please, enter the name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please, enter the price")]
        [Range(0.01, 1000000, ErrorMessage = "You've entered an invalid price")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Please, choose the service category")]
        public string CategoryName { get; set; } = "Consultations";

        [Required]
        public string Status { get; set; } = "Active";
    }
}
