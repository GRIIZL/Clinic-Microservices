using System.ComponentModel.DataAnnotations;

namespace Appointments.Application.Models
{
    public class UpdateAppointmentResultDto
    {
        [Required(ErrorMessage = "Please, enter the complaints")] // Валидация F-1
        public string Complaints { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please, enter the conclusion")] // Валидация F-2
        public string Conclusion { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please, enter the recommendations")] // Валидация F-3
        public string Recommendations { get; set; } = string.Empty;
    }
}
