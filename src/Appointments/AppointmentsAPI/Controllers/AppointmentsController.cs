using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Appointments.Application.Models;
using Appointments.Application.Services;

namespace AppointmentsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentsController : ControllerBase
    {
        private readonly AppointmentService _appointmentService;

        public AppointmentsController(AppointmentService appointmentService)
        {
            _appointmentService = appointmentService;
        }

        // US-6 (AC-4, AC-5): Запись на прием пациентом
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAppointmentDto request, CancellationToken cancellationToken)
        {
            // Симуляция проверки авторизации (AC-4)
            // В будущем мы защитим эндпоинт клеймами [Authorize], но логика валидации готова:
            if (request.PatientId == Guid.Empty)
            {
                return Unauthorized(new { message = "Sign in to make an appointment" }); // Текст по AC-4
            }

            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _appointmentService.CreateAppointmentAsync(request, cancellationToken);
            return Ok(new { message = "Appointment has been created", appointmentId = result.Id }); // AC-5
        }

        // Получение истории записей пациента
        [HttpGet("patient/{patientId}")]
        public async Task<IActionResult> GetHistory(Guid patientId, CancellationToken cancellationToken)
        {
            var history = await _appointmentService.GetPatientHistoryAsync(patientId, cancellationToken);
            return Ok(history);
        }
    }
}
