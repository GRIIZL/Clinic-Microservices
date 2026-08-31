using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Services.Application.Interfaces;
using Services.Application.Models;
using Services.Domain;
using Shared.Events;

namespace Services.Application.Services
{
public class SpezializationService
    {
        private readonly ISpecializationRepository _repository;
        private readonly IEventPublisher _eventPublisher;

        public SpezializationService(ISpecializationRepository repository, IEventPublisher eventPublisher)
        {
            _repository = repository;
            _eventPublisher = eventPublisher;
        }

        // US-44: Получение информации о конкретной услуге внутри специализации
        public async Task<MedicalService?> GetServiceByIdAsync(string specializationId, string serviceId, CancellationToken cancellationToken = default)
        {
            var specialization = await _repository.GetByIdAsync(specializationId, cancellationToken);
            if (specialization == null) return null;

            return specialization.Services.FirstOrDefault(s => s.Id == serviceId);
        }

// US-43 и US-42: Полное обновление услуги (включая имя, цену, категорию и статус)
        public async Task<bool> UpdateServiceInSpecializationAsync(
            string specializationId, 
            string serviceId, 
            UpdateMedicalServiceDto dto, 
            CancellationToken cancellationToken = default)
        {
            var specialization = await _repository.GetByIdAsync(specializationId, cancellationToken);
            if (specialization == null) return false;

            var service = specialization.Services.FirstOrDefault(s => s.Id == serviceId);
            if (service == null) return false;

            // Обновляем поля по ТЗ
            service.Name = dto.Name.Trim();
            service.Price = dto.Price;
            service.CategoryName = dto.CategoryName;
            service.Status = dto.Status; // US-42: Смена статуса (Active/Inactive)
            service.UpdatedAt = DateTime.UtcNow;

            specialization.UpdatedAt = DateTime.UtcNow;

            // Перезаписываем обновленный агрегат в MongoDB
            await _repository.UpdateAsync(specialization, cancellationToken);

            // Публикуем событие об изменении услуги (только если статус изменился)
            if (service.Status != dto.Status)
            {
                await _eventPublisher.PublishAsync(new SpecializationChangedEvent
                {
                    SpecializationId = specializationId,
                    SpecializationName = specialization.Name,
                    Status = dto.Status,
                    ChangeType = "ServiceStatus",
                    ServiceId = serviceId,
                    ServiceName = dto.Name,
                    ChangedAt = DateTime.UtcNow
                }, cancellationToken);
            }

            return true;
        }

        // US-40: Получение списка всех специализаций для админки/ресепшена
        public async Task<IEnumerable<Specialization>> GetSpecializationsListAsync(CancellationToken cancellationToken = default)
        {
            return await _repository.GetAllAsync(cancellationToken);
        }

        // US-39: Детальная информация о специализации
        public async Task<Specialization?> GetSpecializationByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            return await _repository.GetByIdAsync(id, cancellationToken);
        }

        // US-38: Редактирование имени специализации
        public async Task<bool> UpdateSpecializationAsync(string id, UpdateSpecializationDto dto, CancellationToken cancellationToken = default)
        {
            var specialization = await _repository.GetByIdAsync(id, cancellationToken);
            if (specialization == null) return false;

            specialization.Name = dto.Name.Trim();
            specialization.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(specialization, cancellationToken);
            return true;
        }

        // US-41: Создание и добавление новой услуги внутрь специализации
        public async Task<bool> AddServiceToSpecializationAsync(string specializationId, CreateMedicalServiceDto dto, CancellationToken cancellationToken = default)
        {
            var specialization = await _repository.GetByIdAsync(specializationId, cancellationToken);
            if (specialization == null) return false;

            var newService = new MedicalService
            {
                Name = dto.Name.Trim(),
                Price = dto.Price,
                CategoryName = dto.CategoryName,
                Status = dto.Status
            };

            specialization.Services.Add(newService);
            specialization.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(specialization, cancellationToken);
            return true;
        }

// Обновляем старый метод изменения статуса US-37
        public async Task<bool> ChangeStatusAsync(string id, ChangeSpecializationStatusDto dto, CancellationToken cancellationToken = default)
        {
            var specialization = await _repository.GetByIdAsync(id, cancellationToken);
            if (specialization == null) return false;

            specialization.Status = dto.Status;
            specialization.UpdatedAt = DateTime.UtcNow;

            if (dto.Status == "Inactive")
            {
                foreach (var service in specialization.Services)
                {
                    service.Status = "Inactive";
                }
            }

            await _repository.UpdateAsync(specialization, cancellationToken);

            // Публикуем событие об изменении специализации
            await _eventPublisher.PublishAsync(new SpecializationChangedEvent
            {
                SpecializationId = id,
                SpecializationName = specialization.Name,
                Status = dto.Status,
                ChangeType = "SpecializationStatus",
                ChangedAt = DateTime.UtcNow
            }, cancellationToken);

            return true;
        }

        public async Task<Specialization> CreateSpecializationAsync(CreateSpecializationDto dto, CancellationToken cancellationToken = default)
        {
            var spec = new Specialization
            {
                Name = dto.Name.Trim(),
                Status = dto.Status,
                Services = dto.Services.Select(s => new MedicalService
                {
                    Name = s.Name.Trim(),
                    Price = s.Price,
                    Status = s.Status,
                    CategoryName = s.CategoryName
                }).ToList()
            };

            await _repository.AddAsync(spec, cancellationToken);
            return spec;
        }
    }
}
