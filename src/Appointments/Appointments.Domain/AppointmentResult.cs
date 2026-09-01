using System;

namespace Appointments.Domain
{
    public class AppointmentResult
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        // Связь один-к-одному с приемом
        public Guid AppointmentId { get; set; }

        // Поля формы ввода доктора по ТЗ US-58 (Fields description)
        public string Complaints { get; set; } = string.Empty;     // F-1 Required: yes
        public string Conclusion { get; set; } = string.Empty;     // F-2 Required: yes
        public string Recommendations { get; set; } = string.Empty; // F-3 Required: yes

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
