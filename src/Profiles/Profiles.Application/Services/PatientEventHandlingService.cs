using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Profiles.Application.Interfaces;
using Profiles.Domain;
using Shared.Events;

namespace Profiles.Application.Services
{
    /// <summary>
    /// Реализация обработки событий из Auth в контексте профилей пациентов.
    /// </summary>
    public class PatientEventHandlingService : IPatientEventHandlingService
    {
        private readonly IPatientRepository _patientRepository;
        private readonly ILogger<PatientEventHandlingService> _logger;

        public PatientEventHandlingService(
            IPatientRepository patientRepository,
            ILogger<PatientEventHandlingService> logger)
        {
            _patientRepository = patientRepository;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task HandleUserRegisteredAsync(UserRegisteredEvent registeredEvent, CancellationToken cancellationToken = default)
        {
            // В PatientProfile нет поля Email, поэтому идентифицируем пользователя по UserId (AccountId).
            // Проверяем, не существует ли уже профиль, связанный с этим аккаунтом (идемпотентность —
            // повторная доставка события RabbitMQ не должна создавать дубликат).
            var existing = await FindByAccountIdAsync(registeredEvent.UserId, cancellationToken);
            if (existing != null)
            {
                _logger.LogInformation("Профиль для аккаунта {UserId} уже существует, пропускаем.", registeredEvent.UserId);
                return;
            }

            // Создаём «лёгкий» профиль пациента: профиль сразу связан с аккаунтом,
            // ФИО и остальные персональные данные будут заполнены позже (обычно через форму профиля).
            var profile = new PatientProfile
            {
                Id = Guid.NewGuid(),
                AccountId = registeredEvent.UserId,
                IsLinkedToAccount = true,
                PhoneNumber = registeredEvent.PhoneNumber ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _patientRepository.AddAsync(profile, cancellationToken);

            _logger.LogInformation(
                "Создан профиль пациента {ProfileId} для аккаунта {UserId} (email: {Email})",
                profile.Id,
                registeredEvent.UserId,
                registeredEvent.Email);
        }

        // Вспомогательный поиск профиля, связанного с аккаунтом.
        // Для малого объёма данных это допустимо; при росте — добавить прямой lookup по AccountId в репозитории.
        private async Task<PatientProfile?> FindByAccountIdAsync(Guid accountId, CancellationToken cancellationToken)
        {
            var unlinked = await _patientRepository.GetUnlinkedProfilesAsync(cancellationToken);
            foreach (var profile in unlinked)
            {
                if (profile.AccountId == accountId)
                    return profile;
            }
            return null;
        }
    }
}