using System;
using System.Collections.Generic;

namespace Profiles.Application.Models
{
    public class PatientProfileResponseDto
    {
        // Вкладка 1: Персональные данные
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string PhotoUrl { get; set; } = string.Empty;

        // Вкладка 2: Результаты приемов (Пока пустой список, задел под Appointments API)
        public List<string> AppointmentResults { get; set; } = new();
    }
}
