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
    public class ReceptionistsController : ControllerBase
    {
        private readonly ReceptionistService _service;

        public ReceptionistsController(ReceptionistService service)
        {
            _service = service;
        }

        // US-57: Просмотр списка всех ресепшионистов клиники
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _service.GetListAsync(cancellationToken);
            return Ok(result);
        }

        // US-56: Просмотр детального профиля ресепшиониста
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _service.GetByIdAsync(id, cancellationToken);
            if (result == null) return NotFound(new { message = "Receptionist profile not found." });
            return Ok(result);
        }

        // US-53: Создание профиля ресепшиониста (с автогенерацией пароля по AC-5)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ReceptionistDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _service.CreateAsync(request, cancellationToken);
            if (result == null) return BadRequest(new { message = "User with this email already exists." });

            return Ok(result);
        }

        // US-55: Редактирование профиля
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] ReceptionistDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var success = await _service.UpdateAsync(id, request, cancellationToken);
            if (!success) return NotFound(new { message = "Receptionist profile not found to update." });

            return Ok(new { message = "Receptionist profile updated successfully." });
        }

        // US-54: Удаление профиля (с модальным подтверждением на фронтенде)
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var success = await _service.DeleteAsync(id, cancellationToken);
            if (!success) return NotFound(new { message = "Receptionist profile not found to delete." });

            return Ok(new { message = "Receptionist profile has been deleted successfully." });
        }
    }
}