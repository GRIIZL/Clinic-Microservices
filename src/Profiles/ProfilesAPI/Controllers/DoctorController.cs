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
    public class DoctorsController : ControllerBase
    {
        private readonly DoctorService _doctorService;

        public DoctorsController(DoctorService doctorService)
        {
            _doctorService = doctorService;
        }

         // Эндпоинт для пациентов (US-4, US-19, US-21, US-23, US-25)
        [HttpGet("patient-view")]
        public async Task<IActionResult> GetDoctorsForPatients([FromQuery] DoctorQueryParametersDto query, CancellationToken cancellationToken)
        {
            var doctors = await _doctorService.GetDoctorsForPatientsAsync(query, cancellationToken);
            
            // Если массив пустой, возвращаем кастомную ошибку фильтрации/поиска по ТЗ
            if (!doctors.Any())
            {
                return NotFound(new { message = "There are no doctors matching this filtration or incorrect search criteria." });
            }
            
            return Ok(doctors);
        }

// Эндпоинт для администраторов и персонала (US-22, US-24, US-26, US-28)
        [HttpGet("admin-view")]
        public async Task<IActionResult> GetDoctorsForAdmin([FromQuery] DoctorQueryParametersDto query, CancellationToken cancellationToken)
        {
            var doctors = await _doctorService.GetDoctorsForAdminAsync(query, cancellationToken);
            if (!doctors.Any())
            {
                return NotFound(new { message = "There are no doctors matching this filtration." });
            }
            return Ok(doctors);
        }

        // US-20: Точечное изменение статуса доктора ресепшионистом (PATCH)
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeDoctorStatusDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _doctorService.ChangeDoctorStatusAsync(id, request, cancellationToken);
            if (!result) return NotFound(new { message = "Doctor profile not found." });

            return Ok(new { message = $"Doctor status successfully updated to '{request.Status}'." });
        }

        // US-9: Создание профиля доктора ресепшионистом
        [HttpPost]
        public async Task<IActionResult> CreateDoctor([FromBody] CreateDoctorProfileDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _doctorService.CreateDoctorProfileByReceptionistAsync(request, cancellationToken);
            if (result == null)
            {
                return BadRequest(new { message = "User with this email already exists." });
            }

            return Ok(result);
        }

                // US-17: Просмотр детального профиля доктора
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDoctorProfile(Guid id, CancellationToken cancellationToken)
        {
            var doctor = await _doctorService.GetDoctorProfileAsync(id, cancellationToken);
            if (doctor == null) return NotFound(new { message = "Doctor profile not found." });
            return Ok(doctor);
        }

        // US-18: Редактирование профиля доктора ресепшионистом
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDoctorProfile(Guid id, [FromBody] UpdateDoctorProfileDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _doctorService.UpdateDoctorProfileAsync(id, request, cancellationToken);
            if (!result) return BadRequest(new { message = "Could not update profile or invalid data fields." });

            return Ok(new { message = "Doctor profile updated successfully." });
        }

    }
}
