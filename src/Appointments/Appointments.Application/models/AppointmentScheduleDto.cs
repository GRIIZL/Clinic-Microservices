using System;

namespace Appointments.Application.Models
{
    public class AppointmentScheduleDto
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public string PatientFullName { get; set; } = "Иванов Иван Иванович"; // В будущем свяжем по HTTP с Profiles API
        public string ServiceName { get; set; } = string.Empty;
        public string Timeslot { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public bool HasResult { get; set; } // Флаг, есть ли уже заключение по этому приему
    }
}
