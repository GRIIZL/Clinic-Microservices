using System.ComponentModel.DataAnnotations;

namespace Services.Application.Models
{
    public class CreateMedicalServiceDto
    {
        [Required(ErrorMessage = "Please, enter the name")] // F-1 в Fields description
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please, enter the price")] // F-2
        [Range(0.01, 1000000)]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Please, choose the service category")] // F-3
        [RegularExpression("^(Consultations|Diagnostics|Analyses)$")]
        public string CategoryName { get; set; } = "Consultations";

        [Required] // F-4
        public string Status { get; set; } = "Active";
    }
}
