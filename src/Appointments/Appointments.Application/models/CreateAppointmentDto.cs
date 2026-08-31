using System;
using System.ComponentModel.DataAnnotations;

namespace Appointments.Application.Models
{
    public class CreateAppointmentDto
    {
        [Required]
        public Guid PatientId { get; set; }

        [Required(ErrorMessage = "Please, choose the specialization")] // Текст по US-6 F-1
        public string SpecializationId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please, choose the doctor")] // F-2
        public Guid DoctorId { get; set; }

        [Required(ErrorMessage = "Please, choose the service")] // F-3
        public string ServiceId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please, choose the office")]
        public string OfficeId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please, select the date")] // F-5
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Please, choose the timeslot")]
        public string Timeslot { get; set; } = string.Empty;
    }
}
