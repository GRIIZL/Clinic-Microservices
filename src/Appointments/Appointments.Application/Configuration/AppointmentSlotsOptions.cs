using System.Collections.Generic;

namespace Appointments.Application.Configuration
{
    /// <summary>
    /// Настройки расчёта свободных слотов приёма (US-7).
    /// Значения задаются в appsettings.json (секция "AppointmentSlots")
    /// и могут быть переопределены переменными окружения, например:
    /// AppointmentSlots__SlotMinutes=15 (двойное подчёркивание = вложенность).
    /// </summary>
    public class AppointmentSlotsOptions
    {
        public const string SectionName = "AppointmentSlots";

        /// <summary>Шаг сетки слотов в минутах (AC-1: 10 минут).</summary>
        public int SlotMinutes { get; set; } = 10;

        /// <summary>Час начала рабочего дня врача.</summary>
        public int WorkStartHour { get; set; } = 9;

        /// <summary>Час окончания рабочего дня врача.</summary>
        public int WorkEndHour { get; set; } = 17;

        /// <summary>Длительность приёма в слотах по категориям услуг (AC-5, AC-6, AC-7).</summary>
        public Dictionary<string, int> CategorySlots { get; set; } = new()
        {
            ["Analyses"] = 1,      // 10 минут
            ["Consultations"] = 2, // 20 минут
            ["Diagnostics"] = 3    // 30 минут
        };

        /// <summary>Длительность приёма в слотах для неизвестной категории.</summary>
        public int DefaultCategorySlots { get; set; } = 2;
    }
}