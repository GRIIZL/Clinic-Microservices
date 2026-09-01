using System;
using System.Collections.Generic;
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
    /// - Если специализация стала Inactive → отменяет все активные записи к врачам этой специализации
    /// - Если услуга стала Inactive → блокирует её в расписании
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

            // Сценарий 1: Специализация стала неактивной
            if (evt.Status == "Inactive" && evt.ChangeType == "SpecializationStatus")
            {
                await HandleSpecializationInactiveAsync(evt, cancellationToken);
                return;
            }

            // Сценарий 2: Конкретная услуга стала неактивной
            if (evt.Status == "Inactive" && evt.ChangeType == "ServiceStatus" && !string.IsNullOrEmpty(evt.ServiceId))
            {
                await HandleServiceInactiveAsync(evt, cancellationToken);
                return;
            }

            // Сценарий 3: Статус изменился на Active — можно разблокировать
            if (evt.Status == "Active")
            {
                _logger.LogInformation(
                    "[Appointments] Specialization '{SpecializationId}' is now Active. No action needed.",
                    evt.SpecializationId);
                return;
            }

            _logger.LogWarning(
                "[Appointments] Unknown event combination: Status={Status}, ChangeType={ChangeType}",
                evt.Status,
                evt.ChangeType);
        }

        /// <summary>
        /// Сценарий: специализация стала Inactive.
        /// Отменяем ВСЕ активные записи к врачам этой специализации.
        /// Это закрывает требование ТЗ: "при неактивной специализации — каскадная отмена записей".
        /// </summary>
        private async Task HandleSpecializationInactiveAsync(SpecializationChangedEvent evt, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[Appointments] Specialization '{SpecializationId}' ({SpecializationName}) is now INACTIVE. Canceling all active appointments...",
                evt.SpecializationId,
                evt.SpecializationName);

            try
            {
                // Фильтрация происходит на стороне БД (см. репозиторий), а не в памяти
                var activeAppointments = await _appointmentRepository.GetActiveBySpecializationIdAsync(evt.SpecializationId, cancellationToken);
                var appointmentsToCancel = activeAppointments.ToList();

                if (appointmentsToCancel.Count == 0)
                {
                    _logger.LogInformation(
                        "[Appointments] No active appointments found for specialization '{SpecializationId}'.",
                        evt.SpecializationId);
                    return;
                }

                foreach (var appointment in appointmentsToCancel)
                {
                    appointment.Status = "Canceled";
                    appointment.UpdatedAt = DateTime.UtcNow;
                    await _appointmentRepository.UpdateAsync(appointment, cancellationToken);
                }

                _logger.LogInformation(
                    "[Appointments] Cancelled {Count} appointments for specialization '{SpecializationId}'.",
                    appointmentsToCancel.Count,
                    evt.SpecializationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[Appointments] Error canceling appointments for specialization '{SpecializationId}'.",
                    evt.SpecializationId);
                throw; // пробрасываем наверх: consumer сделает nack, и сообщение вернётся в очередь (retry)
            }
        }

        /// <summary>
        /// Сценарий: конкретная услуга стала Inactive.
        /// Отменяем активные записи, ссылающиеся на эту услугу.
        /// </summary>
        private async Task HandleServiceInactiveAsync(SpecializationChangedEvent evt, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[Appointments] Service '{ServiceName}' (Id: {ServiceId}) is now INACTIVE. Canceling related appointments...",
                evt.ServiceName,
                evt.ServiceId);

            try
            {
                var activeAppointments = await _appointmentRepository.GetActiveByServiceIdAsync(evt.ServiceId!, cancellationToken);
                var appointmentsToCancel = activeAppointments.ToList();

                if (appointmentsToCancel.Count == 0)
                {
                    _logger.LogInformation(
                        "[Appointments] No active appointments found for service '{ServiceId}'.",
                        evt.ServiceId);
                    return;
                }

                foreach (var appointment in appointmentsToCancel)
                {
                    appointment.Status = "Canceled";
                    appointment.UpdatedAt = DateTime.UtcNow;
                    await _appointmentRepository.UpdateAsync(appointment, cancellationToken);
                }

                _logger.LogInformation(
                    "[Appointments] Cancelled {Count} appointments for service '{ServiceId}'.",
                    appointmentsToCancel.Count,
                    evt.ServiceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[Appointments] Error canceling appointments for service '{ServiceId}'.",
                    evt.ServiceId);
                throw;
            }
        }
    }
}
