using System;
using System.ComponentModel.DataAnnotations;

namespace Appointments.Application.Models
{
    /// <summary>
    /// US-66: данные для переноса существующей записи.
    /// По AC-3 редактируются только врач, дата и временной слот —
    /// специализация, услуга и офис остаются прежними.
    /// </summary>
    public class RescheduleAppointmentDto
    {
        [Required(ErrorMessage = "Please, choose the doctor")] // F-2
        public Guid DoctorId { get; set; }

        [Required(ErrorMessage = "Please, select the date")] // F-5
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Please, select the time slot")] // F-6
        public string Timeslot { get; set; } = string.Empty;
    }
}
