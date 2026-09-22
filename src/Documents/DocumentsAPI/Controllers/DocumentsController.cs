using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Documents.Application.Services;

namespace DocumentsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly DocumentBusinessService _documentService;

        public DocumentsController(DocumentBusinessService documentService)
        {
            _documentService = documentService;
        }

        // Эндпоинт генерации и сохранения PDF в MinIO
        [HttpPost("generate-report/{appointmentId}")]
        public async Task<IActionResult> GenerateReport(Guid appointmentId, [FromQuery] string patientName, [FromQuery] string complaints, [FromQuery] string conclusion, [FromQuery] string recommendations, CancellationToken cancellationToken)
        {
            var metadata = await _documentService.GenerateAndUploadReportAsync(appointmentId, patientName, complaints, conclusion, recommendations, cancellationToken);
            return Ok(metadata);
        }

        // Эндпоинт скачивания оригинального PDF потока из MinIO по ID приема (US-62)
        [HttpGet("download-report/{appointmentId}")]
        public async Task<IActionResult> DownloadReport(Guid appointmentId, CancellationToken cancellationToken)
        {
            var metadata = await _documentService.GetMetadataByEntityIdAsync(appointmentId, cancellationToken);
            if (metadata == null) return NotFound(new { message = "Document metadata not found." });

            var fileStream = await _documentService.DownloadDocumentStreamAsync(metadata.StorageKey, cancellationToken);
            return File(fileStream, metadata.ContentType, metadata.FileName);
        }
    }
}
