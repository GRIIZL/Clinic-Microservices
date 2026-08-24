using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Profiles.Application.Interfaces;
using Profiles.Application.Models;
using Profiles.Domain;

namespace Profiles.Application.Services
{
    public class PatientService
    {
        private readonly IPatientRepository _patientRepository;

        public PatientService(IPatientRepository patientRepository)
        {
            _patientRepository = patientRepository;
        }

        public async Task<IEnumerable<PatientProfileResponseDto>> GetPatientsListAsync(string? searchName)
        {
            var data = await _patientRepository.GetAllAsync(searchName);
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
        // Ключевой метод умного создания профиля (US-8)
        public async Task<object> CreatePatientProfileAsync(CreatePatientProfileDto dto)
        {
            // 1. Извлекаем все не связанные карточки из БД (AC-4)
            var unlinkedProfiles = await _patientRepository.GetUnlinkedProfilesAsync();
            
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
                    return new { 
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
                DateOfBirth = dto.DateOfBirth
            };

            await _patientRepository.AddAsync(newProfile);
            return new { MatchFound = false, ProfileId = newProfile.Id };
        }

        // Метод ручной привязки к найденному профилю по кнопке "Yes, it's me" (AC-7)
        public async Task<bool> LinkAccountToExistingProfileAsync(Guid profileId, Guid accountId)
        {
            var profile = await _patientRepository.GetByIdAsync(profileId);
            if (profile == null || profile.IsLinkedToAccount) return false;

            profile.AccountId = accountId;
            profile.IsLinkedToAccount = true;
            profile.UpdatedAt = DateTime.UtcNow;

            await _patientRepository.UpdateAsync(profile);
            return true;
        }

        public async Task<PatientProfileResponseDto?> GetPatientByIdAsync(Guid id)
        {
            var profile = await _patientRepository.GetByIdAsync(id);
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

        public async Task<bool> UpdatePatientProfileAsync(Guid id, CreatePatientProfileDto dto)
        {
            var profile = await _patientRepository.GetByIdAsync(id);
            if (profile == null) return false;

            profile.FirstName = dto.FirstName.Trim();
            profile.LastName = dto.LastName.Trim();
            profile.MiddleName = dto.MiddleName?.Trim() ?? string.Empty;
            profile.PhoneNumber = dto.PhoneNumber.Trim();
            profile.DateOfBirth = dto.DateOfBirth;
            profile.PhotoUrl = dto.PhotoUrl ?? string.Empty;
            profile.UpdatedAt = DateTime.UtcNow;

            await _patientRepository.UpdateAsync(profile);
            return true;
        }

        public async Task<bool> DeletePatientProfileAsync(Guid id)
        {
            var profile = await _patientRepository.GetByIdAsync(id);
            if (profile == null) return false;

            await _patientRepository.DeleteAsync(profile);
            return true;
        }
    }
}
