using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Util;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration;
using Documents.Application.Interfaces;

namespace Documents.Infrastructure.Storage
{
    public class AmazonS3StorageService : IFileStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        public AmazonS3StorageService(IConfiguration configuration)
        {
            _bucketName = configuration["MinIO:BucketName"] ?? "clinic-medical-reports";

            // Настраиваем S3-клиент на работу с локальным контейнером MinIO
            var config = new AmazonS3Config
            {
                ServiceURL = configuration["MinIO:ServiceUrl"] ?? "http://localhost:9000",
                ForcePathStyle = true // Обязательно для локального MinIO
            };

            var accessKey = configuration["MinIO:AccessKey"] ?? "minioadmin";
            var secretKey = configuration["MinIO:SecretKey"] ?? "InnowiseMinioSuperSecretPassword123!";

            _s3Client = new AmazonS3Client(accessKey, secretKey, config);
        }

        public async Task<string> UploadFileAsync(string fileName, Stream fileStream, string contentType, CancellationToken cancellationToken = default)
        {
            // Автоматически создаем бакет при первом запуске, если его нет
            if (!await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, _bucketName))
            {
                await _s3Client.PutBucketAsync(_bucketName, cancellationToken);
            }

            // Генерируем уникальный ключ-путь для файла в S3
            var storageKey = $"{Guid.NewGuid()}_{fileName}";

            var fileTransferUtility = new TransferUtility(_s3Client);
            
            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = fileStream,
                Key = storageKey,
                BucketName = _bucketName,
                ContentType = contentType
            };

            await fileTransferUtility.UploadAsync(uploadRequest, cancellationToken);
            return storageKey;
        }

        public async Task<Stream> DownloadFileAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            var response = await _s3Client.GetObjectAsync(_bucketName, storageKey, cancellationToken);
            return response.ResponseStream;
        }
    }
}
