using api.Dtos.HubertDtos;
using api.Interfaces;

namespace api.Services;

public class HubertService
{
    private readonly IResourceRepository _resourceRepository;

    public HubertService(IResourceRepository resourceRepository)
    {
        _resourceRepository = resourceRepository;
    }

    public async Task<List<HubertResourceDto>> GetResourcesAsync()
    {
        var resources = await _resourceRepository.GetAllAsync();

        return resources.Select(resource => new HubertResourceDto
        {
            ResourceId = resource.ResourceId,
            ResourceType = resource.ResourceType.ToString()
        }).ToList();
    }
}