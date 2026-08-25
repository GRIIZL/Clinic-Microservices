using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Services.Application.Models
{
    public class CreateSpecializationDto
    {
        [Required(ErrorMessage = "Please, enter the name")] // Текст ошибки по ТЗ (F-1 Behavior)
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Status is required")] // F-2
        public string Status { get; set; } = "Active";

        // Список услуг, добавляемых сразу при создании специализации (US-36 / AC-4)
        public List<CreateServiceDto> Services { get; set; } = new();
    }

    public class CreateServiceDto
    {
        [Required(ErrorMessage = "Please, enter the service name")]
        public string Name { get; set; } = string.Empty;

        [Range(0.01, 100000, ErrorMessage = "Price must be greater than zero")]
        public decimal Price { get; set; }

        public string Status { get; set; } = "Active";

        [Required(ErrorMessage = "Please, choose category")] // Consultations, Diagnostics, Analyses
        public string CategoryName { get; set; } = "Consultations";
    }
}
