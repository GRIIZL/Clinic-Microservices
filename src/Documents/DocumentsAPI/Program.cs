using Microsoft.EntityFrameworkCore;
using Documents.Application.Interfaces;
using Documents.Application.Services;
using Documents.Infrastructure.Data;
using Documents.Infrastructure.Repositories;
using Documents.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();

// Подключаем PostgreSQL метаданных (Порт 5435)
builder.Services.AddDbContext<DocumentsDataContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("Documents.Infrastructure")
    ));

// Регистрация инфраструктуры S3 и SQL
builder.Services.AddScoped<IDocumentMetadataRepository, DocumentMetadataRepository>();
builder.Services.AddScoped<IFileStorageService, AmazonS3StorageService>();

// Регистрация сервисов логики и QuestPDF
builder.Services.AddScoped<PdfGeneratorService>();
builder.Services.AddScoped<DocumentBusinessService>();

var app = builder.Build();

// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }

app.UseAuthorization();
app.MapControllers();
app.Run();
