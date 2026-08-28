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
    public class DoctorService
    {
        private readonly IDoctorRepository _doctorRepository;
        private readonly ILogger<DoctorService> _logger;

        public DoctorService(IDoctorRepository doctorRepository, ILogger<DoctorService> logger)
        {
            _doctorRepository = doctorRepository;
            _logger = logger;
        }

        // US-4 / US-19 / US-21 / US-25: Получение списка докторов для ПАЦИЕНТА (фильтр: At work)
        public async Task<IEnumerable<DoctorProfile>> GetDoctorsForPatientsAsync(DoctorQueryParametersDto query, CancellationToken cancellationToken = default)
        {
            return await _doctorRepository.GetFilteredDoctorsAsync(query, includeAllStatuses: false, cancellationToken);
        }

        // US-22 / US-24 / US-26: Получение списка докторов для АДМИНА (все статусы)
        public async Task<IEnumerable<DoctorProfile>> GetDoctorsForAdminAsync(DoctorQueryParametersDto query, CancellationToken cancellationToken = default)
        {
            return await _doctorRepository.GetFilteredDoctorsAsync(query, includeAllStatuses: true, cancellationToken);
        }

// US-9: Создание профиля доктора ресепшионистом (AC-5: автогенерация пароля)
        public async Task<object?> CreateDoctorProfileByReceptionistAsync(CreateDoctorProfileDto dto, CancellationToken cancellationToken = default)
        {
            if (await _doctorRepository.ExistsByEmailAsync(dto.Email, cancellationToken)) return null;

            // AC-5: Генерируем случайный временный пароль для доктора
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
                PhotoUrl = dto.PhotoUrl ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _doctorRepository.AddAsync(doctor, cancellationToken);

            // AC-4: Симулируем отправку письма с логином и сгенерированным паролем
            _logger.LogInformation($"\n==================================================\n" +
                                   $"SENDING CREDENTIALS TO DOCTOR: {doctor.Email}\n" +
                                   $"Your account has been created by receptionist.\n" +
                                   $"Login: {doctor.Email}\n" +
                                   $"Temporary Password: {generatedPassword}\n" +
                                   $"==================================================");

            return new { ProfileId = doctor.Id, TemporaryPassword = generatedPassword };
        }

        // US-17: Просмотр детального профиля доктора
        public async Task<DoctorProfile?> GetDoctorProfileAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _doctorRepository.GetByIdAsync(id, cancellationToken);
        }

        // US-18: Редактирование профиля доктора ресепшионистом
        public async Task<bool> UpdateDoctorProfileAsync(Guid id, UpdateDoctorProfileDto dto, CancellationToken cancellationToken = default)
        {
            var doctor = await _doctorRepository.GetByIdAsync(id, cancellationToken);
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
            doctor.PhotoUrl = dto.PhotoUrl ?? string.Empty;
            doctor.UpdatedAt = DateTime.UtcNow;

            await _doctorRepository.UpdateAsync(doctor, cancellationToken);
            return true;
        }

        // US-20: Точечное изменение статуса доктора ресепшионистом (PATCH)
        public async Task<bool> ChangeDoctorStatusAsync(Guid id, ChangeDoctorStatusDto dto, CancellationToken cancellationToken = default)
        {
            var doctor = await _doctorRepository.GetByIdAsync(id, cancellationToken);
            if (doctor == null) return false;

            doctor.Status = dto.Status;
            doctor.UpdatedAt = DateTime.UtcNow;

            await _doctorRepository.UpdateAsync(doctor, cancellationToken);
            return true;
        }
    }
}