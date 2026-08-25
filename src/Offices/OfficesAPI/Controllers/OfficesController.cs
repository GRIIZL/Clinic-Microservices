using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Offices.Application.Models;
using Offices.Application.Services;

namespace OfficesAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OfficesController : ControllerBase
    {
        private readonly OfficeService _officeService;

        public OfficesController(OfficeService officeService)
        {
            _officeService = officeService;
        }

        // US-29 / US-30: Просмотр списка всех офисов клиники
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var offices = await _officeService.GetAllOfficesAsync(cancellationToken);
            return Ok(offices);
        }

        // Просмотр детальной информации об одном офисе
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
        {
            var office = await _officeService.GetOfficeByIdAsync(id, cancellationToken);
            if (office == null) return NotFound(new { message = "Office not found." });
            return Ok(office);
        }

        // US-31: Создание нового офиса ресепшионистом
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOfficeDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var createdOffice = await _officeService.CreateOfficeAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = createdOffice.Id }, createdOffice);
        }

        // US-30 (AC-3): Редактирование существующего офиса
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateOfficeDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _officeService.UpdateOfficeAsync(id, request, cancellationToken);
            if (!result) return NotFound(new { message = "Office not found to update." });

            return Ok(new { message = "Office updated successfully." });
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ChangeStatus(string id, [FromBody] ChangeOfficeStatusDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _officeService.ChangeStatusAsync(id, request, cancellationToken);
            if (!result) return NotFound(new { message = "Office not found." });

            return Ok(new { message = $"Office status successfully changed to '{request.Status}'." });
        }
    }
}
