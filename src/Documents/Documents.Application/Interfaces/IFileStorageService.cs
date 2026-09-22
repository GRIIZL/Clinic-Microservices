using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Documents.Application.Interfaces
{
    public interface IFileStorageService
    {
        // Метод принимает поток файла и отправляет его в MinIO
        Task<string> UploadFileAsync(string fileName, Stream fileStream, string contentType, CancellationToken cancellationToken = default);
        
        // Метод скачивает файл обратно из MinIO в виде потока байт
        Task<Stream> DownloadFileAsync(string storageKey, CancellationToken cancellationToken = default);
    }
}
