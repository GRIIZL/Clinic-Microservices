using System.ComponentModel.DataAnnotations;

namespace Services.Application.Models
{
    public class ChangeSpecializationStatusDto
    {
        [Required]
        [RegularExpression("^(Active|Inactive)$")]
        public string Status { get; set; } = "Active";
    }
}
