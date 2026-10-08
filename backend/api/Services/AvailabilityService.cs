using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.ResourceDtos;
using api.Interfaces;
using api.Enums;

namespace api.Services
{
    public class AvailabilityService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IResourceRepository _resourceRepository;

        public AvailabilityService(IBookingRepository bookingRepository, IResourceRepository resourceRepository)
        {
            _bookingRepository = bookingRepository;
            _resourceRepository = resourceRepository;
        }

        public async Task<ResourceAvailabilityDto?> GetResourceAvailabilityAsync(int resourceId, DateTime startTime, DateTime endTime)
        {
            var resource = await _resourceRepository.GetResourceAsync(resourceId);

            if (resource == null)
            {
                return null;
            }

            var isAvailable = await _bookingRepository.IsResourceAvailableAsync(startTime, endTime, resourceId);

            return new ResourceAvailabilityDto
            {
                ResourceId = resourceId,
                IsAvailable = isAvailable
            };
        }

        public async Task<ResourceTypeAvailabilityDto> GetResourceTypeAvailabilityAsync(ResourceType type, DateTime startTime, DateTime endTime)
        {
            var resources = await _resourceRepository.GetByTypeAsync(type);

            var availableResources = 0;

            foreach (var resource in resources)
            {
                var isAvailable = await _bookingRepository.IsResourceAvailableAsync(startTime, endTime, resource.ResourceId);
                if (isAvailable)
                {
                    availableResources++;
                }
            }
            return new ResourceTypeAvailabilityDto
            {
                ResourceType = type,
                TotalResources = resources.Count,
                AvailableResources = availableResources
            };
        }
    }
}