using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Profiles.Application.Interfaces;
using Profiles.Application.Models;
using Profiles.Domain;

namespace Profiles.Application.Services
{
    public class PatientService
    {
        private readonly IPatientRepository _patientRepository;
        private readonly ILogger<PatientService> _logger;

        public PatientService(IPatientRepository patientRepository, ILogger<PatientService> logger)
        {
            _patientRepository = patientRepository;
            _logger = logger;
        }

        // US-52 и US-50: Получение списка всех пациентов или поиск по имени админом
        public async Task<IEnumerable<PatientProfile>> GetPatientsListAsync(string? name, CancellationToken cancellationToken = default)
        {
            return await _patientRepository.GetAllAsync(name, cancellationToken);
        }

        // US-51: Детальный просмотр профиля пациента
        public async Task<PatientProfile?> GetPatientByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _patientRepository.GetByIdAsync(id, cancellationToken);
        }

        // US-47: Создание карточки пациента
        public async Task<PatientProfile> CreatePatientProfileAsync(CreatePatientProfileDto dto, CancellationToken cancellationToken = default)
        {
            var profile = new PatientProfile
            {
                Id = Guid.NewGuid(),
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                MiddleName = dto.MiddleName?.Trim() ?? string.Empty,
                DateOfBirth = dto.DateOfBirth,
                PhoneNumber = dto.PhoneNumber.Trim(),
                PhotoUrl = dto.PhotoUrl ?? string.Empty,
                IsLinkedToAccount = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _patientRepository.AddAsync(profile, cancellationToken);
            return profile;
        }

        // AC-7: Привязка профиля к аккаунту (US-49)
        public async Task<bool> LinkAccountToExistingProfileAsync(Guid profileId, Guid accountId, CancellationToken cancellationToken = default)
        {
            var profile = await _patientRepository.GetByIdAsync(profileId, cancellationToken);
            if (profile == null || profile.IsLinkedToAccount) return false;

            profile.AccountId = accountId;
            profile.IsLinkedToAccount = true;
            profile.UpdatedAt = DateTime.UtcNow;

            await _patientRepository.UpdateAsync(profile, cancellationToken);
            return true;
        }

        // US-46: Обновление профиля пациента
        public async Task<bool> UpdatePatientProfileAsync(Guid id, CreatePatientProfileDto dto, CancellationToken cancellationToken = default)
        {
            var profile = await _patientRepository.GetByIdAsync(id, cancellationToken);
            if (profile == null) return false;

            profile.FirstName = dto.FirstName.Trim();
            profile.LastName = dto.LastName.Trim();
            profile.MiddleName = dto.MiddleName?.Trim() ?? string.Empty;
            profile.DateOfBirth = dto.DateOfBirth;
            profile.PhoneNumber = dto.PhoneNumber.Trim();
            profile.PhotoUrl = dto.PhotoUrl ?? string.Empty;
            profile.UpdatedAt = DateTime.UtcNow;

            await _patientRepository.UpdateAsync(profile, cancellationToken);
            return true;
        }

        // US-48: Удаление профиля пациента
        public async Task<bool> DeletePatientProfileAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var profile = await _patientRepository.GetByIdAsync(id, cancellationToken);
            if (profile == null) return false;

            await _patientRepository.DeleteAsync(profile, cancellationToken);
            return true;
        }

        // AC-4: Поиск нелинкованных профилей для авто-мэтчинга
        public async Task<IEnumerable<PatientProfile>> GetUnlinkedProfilesAsync(CancellationToken cancellationToken = default)
        {
            return await _patientRepository.GetUnlinkedProfilesAsync(cancellationToken);
        }
    }
}
