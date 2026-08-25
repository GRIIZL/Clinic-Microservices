using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Services.Domain
{
    public class MedicalService
    {
        [BsonId] // Указываем, что это уникальный идентификатор вложенного документа
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [BsonElement("Name")]
        public string Name { get; set; } = string.Empty;

        // Важно для Decimal в MongoDB: указываем тип представления, иначе Mongo сохранит его неверно
        [BsonElement("Price")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Price { get; set; }

        [BsonElement("Status")]
        public string Status { get; set; } = "Active";

        [BsonElement("CategoryName")]
        public string CategoryName { get; set; } = "Consultations";

        [BsonElement("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("UpdatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
