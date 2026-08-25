using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Offices.Application.Interfaces;
using Offices.Application.Models;
using Offices.Domain;

namespace Offices.Application.Services
{
    public class OfficeService
    {
        private readonly IOfficeRepository _officeRepository;

        public OfficeService(IOfficeRepository officeRepository)
        {
            _officeRepository = officeRepository;
        }

        public async Task<IEnumerable<Office>> GetAllOfficesAsync(CancellationToken cancellationToken = default)
        {
            return await _officeRepository.GetAllAsync(cancellationToken);
        }

        public async Task<Office?> GetOfficeByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            return await _officeRepository.GetByIdAsync(id, cancellationToken);
        }

        public async Task<Office> CreateOfficeAsync(CreateOfficeDto dto, CancellationToken cancellationToken = default)
        {
            var office = new Office
            {
                Id = Guid.NewGuid().ToString(),
                PhotoUrl = dto.PhotoUrl ?? string.Empty,
                City = dto.City.Trim(),
                Street = dto.Street.Trim(),
                HouseNumber = dto.HouseNumber.Trim(),
                OfficeNumber = dto.OfficeNumber?.Trim() ?? string.Empty, // Защита от null (F-5)
                Status = dto.Status,
                RegistryPhoneNumber = dto.RegistryPhoneNumber.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _officeRepository.AddAsync(office, cancellationToken);
            return office;
        }

        public async Task<bool> UpdateOfficeAsync(string id, UpdateOfficeDto dto, CancellationToken cancellationToken = default)
        {
            var existingOffice = await _officeRepository.GetByIdAsync(id, cancellationToken);
            if (existingOffice == null) return false;

            existingOffice.PhotoUrl = dto.PhotoUrl ?? string.Empty;
            existingOffice.City = dto.City.Trim();
            existingOffice.Street = dto.Street.Trim();
            existingOffice.HouseNumber = dto.HouseNumber.Trim();
            existingOffice.OfficeNumber = dto.OfficeNumber?.Trim() ?? string.Empty;
            existingOffice.Status = dto.Status;
            existingOffice.RegistryPhoneNumber = dto.RegistryPhoneNumber.Trim();
            existingOffice.UpdatedAt = DateTime.UtcNow;

            await _officeRepository.UpdateAsync(existingOffice, cancellationToken);
            return true;
        }

        public async Task<bool> ChangeStatusAsync(string id, ChangeOfficeStatusDto dto, CancellationToken cancellationToken = default)
        {
            var office = await _officeRepository.GetByIdAsync(id, cancellationToken);
            if (office == null) return false;

            office.Status = dto.Status;
            office.UpdatedAt = DateTime.UtcNow;

            await _officeRepository.UpdateAsync(office, cancellationToken);

            //TODO:     _messageBus.Publish(new OfficeDeactivatedEvent { OfficeId = id });

            return true;
        }
    }
}
