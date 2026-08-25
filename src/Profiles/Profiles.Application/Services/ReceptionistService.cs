using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Profiles.Application.Interfaces;
using Profiles.Application.Models;
using Profiles.Domain;

namespace Profiles.Application.Services
{
    public class ReceptionistService
    {
        private readonly IReceptionistRepository _repository;
        private readonly ILogger<ReceptionistService> _logger;

        public ReceptionistService(IReceptionistRepository repository, ILogger<ReceptionistService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IEnumerable<ReceptionistProfile>> GetListAsync(CancellationToken cancellationToken = default) => await _repository.GetAllAsync(cancellationToken);

        public async Task<ReceptionistProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => await _repository.GetByIdAsync(id, cancellationToken);

        // Логика создания и генерации кредов по US-53 (AC-4, AC-5)
        public async Task<object?> CreateAsync(ReceptionistDto dto, CancellationToken cancellationToken = default)
        {
            if (await _repository.ExistsByEmailAsync(dto.Email, cancellationToken)) return null; // Email занят по ТЗ

            // AC-5: Генерируем случайный безопасный пароль
            string generatedPassword = Convert.ToHexString(RandomNumberGenerator.GetBytes(6));

            var receptionist = new ReceptionistProfile
            {
                Id = Guid.NewGuid(),
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                MiddleName = dto.MiddleName?.Trim() ?? string.Empty,
                Email = dto.Email.ToLower().Trim(),
                OfficeId = dto.OfficeId,
                PhotoUrl = dto.PhotoUrl ?? string.Empty
            };

            await _repository.AddAsync(receptionist, cancellationToken);

            // AC-4: Симулируем отправку письма с логином и сгенерированным паролем
            _logger.LogInformation($"\n==================================================\n" +
                                   $"SENDING CREDENTIALS TO RECEPTIONIST: {receptionist.Email}\n" +
                                   $"Your workplace profile has been created successfully.\n" +
                                   $"Login: {receptionist.Email}\n" +
                                   $"Temporary Password: {generatedPassword}\n" +
                                   $"==================================================");

            return new { ProfileId = receptionist.Id, TemporaryPassword = generatedPassword };
        }

        public async Task<bool> UpdateAsync(Guid id, ReceptionistDto dto, CancellationToken cancellationToken = default)
        {
            var receptionist = await _repository.GetByIdAsync(id, cancellationToken);
            if (receptionist == null) return false;

            receptionist.FirstName = dto.FirstName.Trim();
            receptionist.LastName = dto.LastName.Trim();
            receptionist.MiddleName = dto.MiddleName?.Trim() ?? string.Empty;
            receptionist.OfficeId = dto.OfficeId;
            receptionist.PhotoUrl = dto.PhotoUrl ?? string.Empty;
            receptionist.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(receptionist, cancellationToken);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var receptionist = await _repository.GetByIdAsync(id, cancellationToken);
            if (receptionist == null) return false;

            await _repository.DeleteAsync(receptionist, cancellationToken);
            return true;
        }
    }
}
