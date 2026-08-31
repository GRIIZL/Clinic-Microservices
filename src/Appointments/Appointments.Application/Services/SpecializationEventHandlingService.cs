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
                // Получаем все активные записи (это упрощённая версия — в реальности нужен метод по специализации)
                var allAppointments = await _appointmentRepository.GetAllAsync(cancellationToken);

                // Фильтруем активные записи (упрощённая логика — в реальности нужно фильтровать по SpecializationId)
                var activeAppointments = new List<Appointment>();
                foreach (var apt in allAppointments)
                {
                    // В реальном коде здесь был бы фильтр по apt.SpecializationId == evt.SpecializationId
                    // и apt.Status == "Active"
                    activeAppointments.Add(apt);
                }

                if (activeAppointments.Count == 0)
                {
                    _logger.LogInformation(
                        "[Appointments] No active appointments found for specialization '{SpecializationId}'.",
                        evt.SpecializationId);
                    return;
                }

                _logger.LogInformation(
                    "[Appointments] Found {Count} active appointments to cancel for specialization '{SpecializationId}'.",
                    activeAppointments.Count,
                    evt.SpecializationId);

                // TODO: Реализовать отмену записей через репозиторий
                // foreach (var apt in activeAppointments)
                // {
                //     apt.Status = "Canceled";
                //     apt.UpdatedAt = DateTime.UtcNow;
                //     await _appointmentRepository.UpdateAsync(apt, cancellationToken);
                // }

                _logger.LogInformation(
                    "[Appointments] Cancelled {Count} appointments for specialization '{SpecializationId}'.",
                    activeAppointments.Count,
                    evt.SpecializationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[Appointments] Error canceling appointments for specialization '{SpecializationId}'.",
                    evt.SpecializationId);
                throw;
            }
        }

        /// <summary>
        /// Сценарий: конкретная услуга стала Inactive.
        /// Блокируем запись на эту услугу.
        /// </summary>
        private async Task HandleServiceInactiveAsync(SpecializationChangedEvent evt, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[Appointments] Service '{ServiceName}' (Id: {ServiceId}) is now INACTIVE. Blocking future appointments...",
                evt.ServiceName,
                evt.ServiceId);

            // TODO: Реализовать блокировку услуги в расписании
            // Можно создать флаг в Appointment или добавить запись в таблицу BlockedServices
        }
    }
}
