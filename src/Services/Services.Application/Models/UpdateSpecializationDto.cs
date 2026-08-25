using System.ComponentModel.DataAnnotations;

namespace Services.Application.Models
{
    public class UpdateSpecializationDto
    {
        [Required(ErrorMessage = "Please, enter the name")]
        public string Name { get; set; } = string.Empty;
    }
}
