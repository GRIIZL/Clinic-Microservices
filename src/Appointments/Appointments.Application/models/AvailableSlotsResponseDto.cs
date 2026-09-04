using System.Collections.Generic;

namespace Appointments.Application.Models
{
    public class AvailableSlotsResponseDto
    {
        public string Date { get; set; } = string.Empty;
        // Сколько 10-минутных слотов занимает выбранная категория услуги
        // (нужно фронту, чтобы вычислить время окончания без дублирования бизнес-правила)
        public int RequiredSlotsCount { get; set; }
        // Список доступных для клика временных точек старта приема
        public List<string> AvailableStartTimes { get; set; } = new();
    }
}
