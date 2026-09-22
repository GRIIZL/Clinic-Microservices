namespace Services.Infrastructure.MongoDb.Configuration
{
    /// <summary>
    /// Параметры подключения к MongoDB сервиса Services.
    /// Задаются в appsettings.json (секция "MongoDb") и могут быть переопределены
    /// переменными окружения, например MongoDb__DatabaseName=ServicesDB_Staging.
    /// </summary>
    public class MongoDbOptions
    {
        public const string SectionName = "MongoDb";

        /// <summary>Имя базы данных.</summary>
        public string DatabaseName { get; set; } = "ServicesDB";

        /// <summary>Имя коллекции со специализациями.</summary>
        public string CollectionName { get; set; } = "Specializations";
    }
}
