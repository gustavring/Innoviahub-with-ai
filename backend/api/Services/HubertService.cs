
using api.Dtos.HubertDtos;
using api.Dtos.ResourceDtos;
using api.Interfaces;

namespace api.Services;

public class HubertService
{
    private readonly IResourceRepository _resourceRepository;
    private readonly TimeService _timeService;
    private readonly AvailabilityService _availabilityService;

    public HubertService(
        IResourceRepository resourceRepository,
        TimeService timeService,
        AvailabilityService availabilityService)
    {
        _resourceRepository = resourceRepository;
        _timeService = timeService;
        _availabilityService = availabilityService;
    }

    public DateTime GetCurrentSwedishTime()
    {
        return _timeService.GetCurrentSwedishTime();
    }

    public async Task<ResourceTypeAvailabilityDto?>
        GetBookingAvailabilityAsync(HubertBookingRequestDto request)
    {
        if (request.ResourceType == null ||
            request.Date == null ||
            request.StartTime == null ||
            request.DurationMinutes == null ||
            request.DurationMinutes <= 0)
        {
            return null;
        }

        var startLocal = request.Date.Value.ToDateTime(
            request.StartTime.Value);

        var endLocal = startLocal.AddMinutes(
            request.DurationMinutes.Value);

        var startUtc = _timeService.ToUtc(startLocal);
        var endUtc = _timeService.ToUtc(endLocal);

        var availability =
            await _availabilityService.GetResourceTypeAvailabilityAsync(
                request.ResourceType.Value,
                startUtc,
                endUtc);

        Console.WriteLine(
            $"HUBERT TILLGÄNGLIGHET: " +
            $"Resurs={request.ResourceType}, " +
            $"StartUTC={startUtc}, " +
            $"SlutUTC={endUtc}, " +
            $"Totalt={availability.TotalResources}, " +
            $"Lediga={availability.AvailableResources}"
        );

        return availability;
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
