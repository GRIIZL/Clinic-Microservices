using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Appointments.Application.Interfaces;
using Appointments.Domain;
using Microsoft.Extensions.Logging;
using Shared.Events;

namespace Appointments.Application.Services
{
    /// <summary>
    /// Обрабатывает события от Services API:
    /// - Специализация стала Inactive → отменяет все активные записи этой специализации
    /// - Услуга стала Inactive → отменяет активные записи, ссылающиеся на эту услугу
    /// </summary>
    public class SpecializationEventHandlingService : ISpecializationEventHandlingService
    {
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly ILogger<SpecializationEventHandlingService> _logger;

        public SpecializationEventHandlingService(
            IAppointmentRepository appointmentRepository,
            ILogger<SpecializationEventHandlingService> logger)
        {
            _appointmentRepository = appointmentRepository;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task HandleSpecializationChangedAsync(SpecializationChangedEvent evt, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "[Appointments] Received event: Specialization '{SpecializationId}' ({Status}), ChangeType: {ChangeType}",
                evt.SpecializationId,
                evt.Status,
                evt.ChangeType);

            // Реагируем только на деактивацию — остальные изменения не требуют действий
            if (evt.Status != ServiceStatuses.Inactive)
            {
                _logger.LogInformation(
                    "[Appointments] Specialization '{SpecializationId}' is now {Status}. No action needed.",
                    evt.SpecializationId,
                    evt.Status);
                return;
            }

            switch (evt.ChangeType)
            {
                case SpecializationChangeTypes.SpecializationStatus:
                    await CancelAndLogAsync(
                        loadAppointments: ct => _appointmentRepository.GetActiveBySpecializationIdAsync(evt.SpecializationId, ct),
                        scopeDescription: $"specialization '{evt.SpecializationId}'",
                        cancellationToken);
                    break;

                case SpecializationChangeTypes.ServiceStatus when !string.IsNullOrEmpty(evt.ServiceId):
                    await CancelAndLogAsync(
                        loadAppointments: ct => _appointmentRepository.GetActiveByServiceIdAsync(evt.ServiceId!, ct),
                        scopeDescription: $"service '{evt.ServiceId}'",
                        cancellationToken);
                    break;

                default:
                    _logger.LogWarning(
                        "[Appointments] Unknown event combination: Status={Status}, ChangeType={ChangeType}",
                        evt.Status,
                        evt.ChangeType);
                    break;
            }
        }

        /// <summary>
        /// Единая логика каскадной отмены: загружает активные записи по переданному фильтру,
        /// переводит их в статус Canceled и логирует результат.
        /// Устраняет дублирование между сценариями специализации и услуги.
        /// </summary>
        private async Task CancelAndLogAsync(
            Func<CancellationToken, Task<IEnumerable<Appointment>>> loadAppointments,
            string scopeDescription,
            CancellationToken cancellationToken)
        {
            try
            {
                var appointmentsToCancel = (await loadAppointments(cancellationToken)).ToList();

                if (appointmentsToCancel.Count == 0)
                {
                    _logger.LogInformation(
                        "[Appointments] No active appointments found for {Scope}.",
                        scopeDescription);
                    return;
                }

                foreach (var appointment in appointmentsToCancel)
                {
                    appointment.Status = AppointmentStatuses.Canceled;
                    appointment.UpdatedAt = DateTime.UtcNow;
                    await _appointmentRepository.UpdateAsync(appointment, cancellationToken);
                }

                _logger.LogInformation(
                    "[Appointments] Cancelled {Count} appointments for {Scope}.",
                    appointmentsToCancel.Count,
                    scopeDescription);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[Appointments] Error canceling appointments for {Scope}.",
                    scopeDescription);

                // Пробрасываем наверх: MassTransit вернёт сообщение в очередь (retry)
                throw;
            }
        }
    }
}
