using System.ComponentModel.DataAnnotations;

namespace Profiles.Application.Models
{
public class ChangeDoctorStatusDto
    {
        [Required(ErrorMessage = "Status field is required.")]
        // Регулярное выражение строго ограничивает ввод разрешенными статусами из ТЗ (Fields description)
        [RegularExpression("^(At work|On vacation|Sick day|Sick leave|Self-isolation|Leave without pay|Dismissed)$", 
            ErrorMessage = "Invalid status value.")]
        public string Status { get; set; } = "At work";
    } 
}