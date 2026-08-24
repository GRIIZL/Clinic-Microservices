using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes; // Добавляем этот using

namespace Services.Domain
{
    public class Specialization
    {
        [BsonId]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [BsonElement("Name")]
        public string Name { get; set; } = string.Empty;
        
        [BsonElement("Status")]
        public string Status { get; set; } = "Active";

        // ХАРД-ФИКС: Явно указываем MongoDriver сериализовать это как массив документов, а не строку!
        [BsonElement("Services")]
        public List<MedicalService> Services { get; set; } = new();

        [BsonElement("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("UpdatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
