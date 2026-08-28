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
        public async Task<IEnumerable<PatientProfileResponseDto>> GetPatientsListAsync(string? name, CancellationToken cancellationToken = default)
        {
            var data = await _patientRepository.GetAllAsync(name, cancellationToken);
            return data.Select(p => new PatientProfileResponseDto
            {
                Id = p.Id,
                FirstName = p.FirstName,
                LastName = p.LastName,
                MiddleName = p.MiddleName,
                PhoneNumber = p.PhoneNumber,
                DateOfBirth = p.DateOfBirth,
                PhotoUrl = p.PhotoUrl
            });
        }

        // US-51: Детальный просмотр профиля пациента
        public async Task<PatientProfileResponseDto?> GetPatientByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var profile = await _patientRepository.GetByIdAsync(id, cancellationToken);
            if (profile == null) return null;

            return new PatientProfileResponseDto
            {
                Id = profile.Id,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                MiddleName = profile.MiddleName,
                PhoneNumber = profile.PhoneNumber,
                DateOfBirth = profile.DateOfBirth,
                PhotoUrl = profile.PhotoUrl,
                AppointmentResults = new List<string>() // В будущем подтянется из базы записей
            };
        }

        // Ключевой метод умного создания профиля (US-8 / AC-4 / AC-5 / AC-6)
        public async Task<object> CreatePatientProfileAsync(CreatePatientProfileDto dto, CancellationToken cancellationToken = default)
        {
            // 1. Извлекаем все не связанные карточки из БД (AC-4)
            var unlinkedProfiles = await _patientRepository.GetUnlinkedProfilesAsync(cancellationToken);

            foreach (var existing in unlinkedProfiles)
            {
                // Вычисляем коэффициент совпадения по правилам AC-9
                int score = 0;
                if (existing.FirstName.Equals(dto.FirstName, StringComparison.OrdinalIgnoreCase)) score += 5;
                if (existing.LastName.Equals(dto.LastName, StringComparison.OrdinalIgnoreCase)) score += 5;
                if (!string.IsNullOrEmpty(existing.MiddleName) && existing.MiddleName.Equals(dto.MiddleName, StringComparison.OrdinalIgnoreCase)) score += 5;
                if (existing.DateOfBirth.Date == dto.DateOfBirth.Date) score += 3;

                // Если совпадение >= 13 — прерываем создание и отправляем дубликат на фронтенд (AC-6)
                if (score >= 13)
                {
                    return new
                    {
                        MatchFound = true,
                        Message = "A similar profile has been found, you might have already visited one of our clinics?",
                        ExistingProfileId = existing.Id,
                        ExistingProfileData = new { existing.FirstName, existing.LastName, existing.PhoneNumber }
                    };
                }
            }

            // 2. Если совпадений нет — создаем абсолютно новый профиль (AC-5)
            var newProfile = new PatientProfile
            {
                AccountId = dto.AccountId,
                IsLinkedToAccount = dto.AccountId.HasValue, // Если пришел AccountId, сразу линкуем
                PhotoUrl = dto.PhotoUrl ?? string.Empty,
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                MiddleName = dto.MiddleName?.Trim() ?? string.Empty,
                PhoneNumber = dto.PhoneNumber.Trim(),
                DateOfBirth = dto.DateOfBirth,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _patientRepository.AddAsync(newProfile, cancellationToken);
            return new { MatchFound = false, ProfileId = newProfile.Id };
        }

        // Метод ручной привязки к найденному профилю по кнопке "Yes, it's me" (AC-7)
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
            profile.PhoneNumber = dto.PhoneNumber.Trim();
            profile.DateOfBirth = dto.DateOfBirth;
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
