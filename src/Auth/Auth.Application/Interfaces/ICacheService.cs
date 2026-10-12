using System;
using System.Threading;
using System.Threading.Tasks;

namespace Auth.Application.Interfaces
{
    /// <summary>
    /// Абстракция кеша для Application-слоя. Позволяет бизнес-логике работать с кешем,
    /// не завися от конкретной реализации (Redis, in-memory и т.д.).
    /// </summary>
    public interface ICacheService
    {
        Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);
        Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken cancellationToken = default);
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    }
}
