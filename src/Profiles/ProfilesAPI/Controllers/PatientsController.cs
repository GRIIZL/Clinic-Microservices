using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Profiles.Application.Models;
using Profiles.Application.Services;

namespace ProfilesAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientsController : ControllerBase
    {
        private readonly PatientService _patientService;

        public PatientsController(PatientService _patientService)
        {
            this._patientService = _patientService;
        }

        // US-52 и US-50: Просмотр списка всех пациентов или поиск по ФИО админом
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? name, CancellationToken cancellationToken)
        {
            var result = await _patientService.GetPatientsListAsync(name, cancellationToken);
            
            // Если админ ввел поисковый запрос и совпадений нет — возвращаем ошибку по ТЗ (US-50)
            if (!string.IsNullOrWhiteSpace(name) && !result.Any())
            {
                return NotFound(new { message = "Incorrect patient name" });
            }
            
            return Ok(result);
        }

        // US-51: Детальный просмотр профиля пациента со вкладками
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _patientService.GetPatientByIdAsync(id, cancellationToken);
            if (result == null) return NotFound(new { message = "Patient profile not found." });
            return Ok(result);
        }

        // US-47: Создание карточки с алгоритмом весов совпадения
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePatientProfileDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            
            var result = await _patientService.CreatePatientProfileAsync(request, cancellationToken);
            return Ok(result);
        }

        // Эндпоинт клика по кнопке "Yes, it's me" для связывания (AC-7)
        [HttpPost("{id}/link-account")]
        public async Task<IActionResult> LinkAccount(Guid id, [FromQuery] Guid accountId, CancellationToken cancellationToken)
        {
            var success = await _patientService.LinkAccountToExistingProfileAsync(id, accountId, cancellationToken);
            if (!success) return BadRequest(new { message = "Could not link account or profile already linked." });
            
            return Ok(new { message = "Profile linked successfully!" });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CreatePatientProfileDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var success = await _patientService.UpdatePatientProfileAsync(id, request, cancellationToken);
            if (!success) return NotFound(new { message = "Patient profile not found to update." });

            return Ok(new { message = "Patient profile updated successfully." });
        }

        // US-48: Удаление профиля пациента (с модальным подтверждением на фронтенде)
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var success = await _patientService.DeletePatientProfileAsync(id, cancellationToken);
            if (!success) return NotFound(new { message = "Patient profile not found to delete." });

            return Ok(new { message = "Patient profile has been deleted successfully." });
        }
    }
}
