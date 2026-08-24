using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Profiles.Application.Interfaces;
using Profiles.Application.Models;
using Profiles.Domain;
using Microsoft.Extensions.Logging;

namespace Profiles.Application.Services
{
    public class DoctorService
    {
        private readonly IDoctorRepository _doctorRepository;
        private readonly ILogger<DoctorService> _logger;

        public DoctorService(IDoctorRepository doctorRepository, ILogger<DoctorService> logger)
        {
            _doctorRepository = doctorRepository;
            _logger = logger;
        }

        public async Task<DoctorProfile?> GetDoctorProfileAsync(Guid id)
        {
            return await _doctorRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<DoctorProfile>> GetDoctorsForPatientsAsync(DoctorQueryParametersDto parameters)
        {
            return await _doctorRepository.GetFilteredDoctorsAsync(parameters, includeAllStatuses: false);
        }

        public async Task<IEnumerable<DoctorProfile>> GetDoctorsForAdminAsync(DoctorQueryParametersDto parameters)
        {
            return await _doctorRepository.GetFilteredDoctorsAsync(parameters, includeAllStatuses: true);
        }

        public async Task<bool> ChangeDoctorStatusAsync(Guid id, ChangeDoctorStatusDto dto)
        {
            var doctor = await _doctorRepository.GetByIdAsync(id);
            if (doctor == null) return false;

            doctor.Status = dto.Status;
            doctor.UpdatedAt = DateTime.UtcNow;

            await _doctorRepository.UpdateAsync(doctor);
            return true;
        }

        public async Task<DoctorProfile> CreateDoctorAsync(DoctorProfile doctor)
        {
            doctor.CreatedAt = DateTime.UtcNow;
            doctor.UpdatedAt = DateTime.UtcNow;
            await _doctorRepository.AddAsync(doctor);
            return doctor;
        }

        public async Task<object?> CreateDoctorProfileByReceptionistAsync(CreateDoctorProfileDto dto)
        {
            if (await _doctorRepository.ExistsByEmailAsync(dto.Email))
            {
                return null;
            }

            string generatedPassword = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(6));

            var doctor = new DoctorProfile
            {
                Id = Guid.NewGuid(),
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                MiddleName = dto.MiddleName?.Trim() ?? string.Empty,
                Email = dto.Email.ToLower().Trim(),
                DateOfBirth = dto.DateOfBirth,
                Specialization = dto.Specialization,
                OfficeId = dto.OfficeId,
                CareerStartYear = dto.CareerStartYear,
                Status = dto.Status,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _doctorRepository.AddAsync(doctor);

             _logger.LogInformation($"\n==================================================\n" +
                                   $"SENDING CREDENTIALS TO DOCTOR: {doctor.Email}\n" +
                                   $"Your account has been created by receptionist.\n" +
                                   $"Login: {doctor.Email}\n" +
                                   $"Temporary Password: {generatedPassword}\n" +
                                   $"==================================================");

            return new { ProfileId = doctor.Id, TemporaryPassword = generatedPassword };
        }

        // US-18: Редактирование профиля доктора ресепшионистом
        public async Task<bool> UpdateDoctorProfileAsync(Guid id, UpdateDoctorProfileDto dto)
        {
            var doctor = await _doctorRepository.GetByIdAsync(id);
            if (doctor == null) return false;

            // Проверка из ТЗ: Дата рождения не должна быть больше текущей даты
            if (dto.DateOfBirth.Date > DateTime.UtcNow.Date) return false;

            doctor.FirstName = dto.FirstName.Trim();
            doctor.LastName = dto.LastName.Trim();
            doctor.MiddleName = dto.MiddleName?.Trim() ?? string.Empty;
            doctor.DateOfBirth = dto.DateOfBirth;
            doctor.Specialization = dto.Specialization;
            doctor.OfficeId = dto.OfficeId;
            doctor.CareerStartYear = dto.CareerStartYear;
            doctor.Status = dto.Status;
            doctor.UpdatedAt = DateTime.UtcNow;

            await _doctorRepository.UpdateAsync(doctor);
            return true;
        }

    }
}