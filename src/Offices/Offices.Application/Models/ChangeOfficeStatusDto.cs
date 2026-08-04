using System.ComponentModel.DataAnnotations;

namespace Offices.Application.Models
{
    public class ChangeOfficeStatusDto
    {
        [Required(ErrorMessage = "Status is required.")]
        [RegularExpression("^(Active|Inactive)$", ErrorMessage = "Status must be either 'Active' or 'Inactive'.")]
        public string Status { get; set; } = "Active";
    }
}