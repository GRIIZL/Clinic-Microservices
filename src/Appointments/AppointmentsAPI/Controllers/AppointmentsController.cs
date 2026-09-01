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

                // US-15: Удаление (отмена) записи на прием ресепшионистом
        [HttpDelete("{id}")]
        public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
        {
            var success = await _appointmentService.CancelAppointmentAsync(id, cancellationToken);
            if (!success) return NotFound(new { message = "Appointment not found." });
            return Ok(new { message = "Appointment has been permanently deleted." }); // AC-1
        }

        // US-10: Просмотр расписания конкретного врача на день
        [HttpGet("doctor/{doctorId}")]
        public async Task<IActionResult> GetDoctorSchedule(Guid doctorId, [FromQuery] DateTime date, CancellationToken cancellationToken)
        {
            var result = await _appointmentService.GetDoctorScheduleAsync(doctorId, date, cancellationToken);
            return Ok(result);
        }

        // US-58: Создание медицинского заключения доктором по итогам приема
        [HttpPost("results")]
        public async Task<IActionResult> CreateResult([FromBody] CreateAppointmentResultDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var success = await _appointmentService.CreateResultAsync(request, cancellationToken);
            if (!success) return BadRequest(new { message = "Could not create medical result. Invalid appointment ID." });

            return Ok(new { message = "Medical result has been successfully created." });
        }

                // US-60 / US-61: Просмотр медицинского заключения по ID приема
        [HttpGet("{appointmentId}/result")]
        public async Task<IActionResult> GetResult(Guid appointmentId, CancellationToken cancellationToken)
        {
            var result = await _appointmentService.GetResultDetailsAsync(appointmentId, cancellationToken);
            if (result == null) return NotFound(new { message = "Medical result not found for this appointment." });
            return Ok(result);
        }

        // US-59: Редактирование медицинского заключения доктором
        [HttpPut("{appointmentId}/result")]
        public async Task<IActionResult> UpdateResult(Guid appointmentId, [FromBody] UpdateAppointmentResultDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var success = await _appointmentService.UpdateResultAsync(appointmentId, request, cancellationToken);
            if (!success) return NotFound(new { message = "Medical result not found to update." });

            return Ok(new { message = "Medical result updated successfully." });
        }

        // US-62: Скачивание медицинского заключения пациентом в формате PDF
        [HttpGet("{appointmentId}/result/download")]
        public async Task<IActionResult> DownloadResultPdf(Guid appointmentId, CancellationToken cancellationToken)
        {
            var fileBytes = await _appointmentService.GenerateAppointmentResultPdfAsync(appointmentId, cancellationToken);
            if (fileBytes.Length == 0) return NotFound(new { message = "Document parameters not found." });

            // Возвращаем файл в браузер с правильным mime-типом (application/pdf) по AC-2
            return File(fileBytes, "application/pdf", $"MedicalReport_{appointmentId}.pdf");
        }
    }
}
