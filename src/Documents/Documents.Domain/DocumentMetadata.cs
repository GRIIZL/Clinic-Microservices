using System;

namespace Documents.Domain
{
    public class DocumentMetadata
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FileName { get; set; } = string.Empty; // Например, "MedicalReport.pdf"
        public string ContentType { get; set; } = "application/pdf";
        public long FileSize { get; set; } // Размер файла в байтах
        
        // Уникальный ключ-путь файла внутри бакета S3 хранилища
        public string StorageKey { get; set; } = string.Empty; 
        
        // Внешний ID сущности, к которой привязан файл (например, AppointmentId)
        public Guid RelatedEntityId { get; set; } 

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
