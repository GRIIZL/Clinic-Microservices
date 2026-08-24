using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Services.Application.Models;
using Services.Application.Services;

namespace ServicesAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SpecializationsController : ControllerBase
    {
        private readonly ServicesService _servicesService;

        public SpecializationsController(ServicesService servicesService)
        {
            _servicesService = servicesService;
        }

        // US-44: Получить детальную информацию об одной услуге
        [HttpGet("{specializationId}/services/{serviceId}")]
        public async Task<IActionResult> GetServiceById(string specializationId, string serviceId, CancellationToken cancellationToken)
        {
            var service = await _servicesService.GetServiceByIdAsync(specializationId, serviceId, cancellationToken);
            if (service == null) return NotFound(new { message = "Service not found in this specialization." });
            
            return Ok(service);
        }

        // US-43 и US-42: Изменить данные или переключить статус услуги
        [HttpPut("{specializationId}/services/{serviceId}")]
        public async Task<IActionResult> UpdateService(string specializationId, string serviceId, [FromBody] UpdateMedicalServiceDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var success = await _servicesService.UpdateServiceInSpecializationAsync(specializationId, serviceId, request, cancellationToken);
            if (!success) return NotFound(new { message = "Specialization or Service not found to update." });

            return Ok(new { message = "Medical service parameters updated successfully." });
        }

        // US-41: Создание новой услуги внутри конкретной специализации
        [HttpPost("{id}/services")]
        public async Task<IActionResult> CreateService(string id, [FromBody] CreateMedicalServiceDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var success = await _servicesService.AddServiceToSpecializationAsync(id, request, cancellationToken);
            if (!success) return NotFound(new { message = "Specialization not found to bind service" });

            return Ok(new { message = "Service successfully added to specialization document." });
        }

        // US-40: Получение списка всех специализаций для таблицы ресепшиониста
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _servicesService.GetSpecializationsListAsync(cancellationToken);
            return Ok(result);
        }

        // US-39: Получение детальной карточки специализации со списком услуг
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
        {
            var result = await _servicesService.GetSpecializationByIdAsync(id, cancellationToken);
            if (result == null) return NotFound(new { message = "Specialization not found" });
            return Ok(result);
        }

        // US-38: Редактирование специализации
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateSpecializationDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var success = await _servicesService.UpdateSpecializationAsync(id, request, cancellationToken);
            if (!success) return NotFound(new { message = "Specialization not found" });

            return Ok(new { message = "Specialization updated successfully" });
        }

        // US-37: Изменение статуса специализации (PATCH)
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ChangeStatus(string id, [FromBody] ChangeSpecializationStatusDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var success = await _servicesService.ChangeStatusAsync(id, request, cancellationToken);
            if (!success) return NotFound(new { message = "Specialization not found" });

            return Ok(new { message = $"Status updated to {request.Status} along with inner services." });
        }
    }
}
