using api.Dtos.HubertDtos;
using api.Dtos.ResourceDtos;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.SignalR;
using api.Hubs;

namespace api.Services;

public class HubertService
{
    private readonly IResourceRepository _resourceRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly TimeService _timeService;
    private readonly AvailabilityService _availabilityService;
    private readonly IHubContext<BookingHub> _hubContext;

    public HubertService(
        IResourceRepository resourceRepository,
        IBookingRepository bookingRepository,
        TimeService timeService,
        AvailabilityService availabilityService,
        IHubContext<BookingHub> hubContext)
    {
        _resourceRepository = resourceRepository;
        _bookingRepository = bookingRepository;
        _timeService = timeService;
        _availabilityService = availabilityService;
        _hubContext = hubContext;
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

    public async Task<List<Booking>> GetMyBookingsAsync(string userId, DateOnly fromDate, DateOnly toDate)
    {
        if (string.IsNullOrWhiteSpace(userId) ||
            toDate < fromDate)
        {
            return new List<Booking>();
        }

        var fromLocal = fromDate.ToDateTime(TimeOnly.MinValue);
        var toLocalExclusive = toDate.AddDays(1)
            .ToDateTime(TimeOnly.MinValue);

        var fromUtc = _timeService.ToUtc(fromLocal);
        var toUtcExclusive = _timeService.ToUtc(toLocalExclusive);

        var bookings = await _bookingRepository.GetByUserIdAsync(userId);

        return bookings
            .Where(b =>
                b.StartTime < toUtcExclusive &&
                b.EndTime > fromUtc)
            .OrderBy(b => b.StartTime)
            .ToList();
    }

    public async Task<List<Booking>> GetAllBookingsForHubertAsync(
        DateOnly fromDate,
        DateOnly toDate)
    {
        if (toDate < fromDate)
        {
            return new List<Booking>();
        }

        var fromLocal = fromDate.ToDateTime(TimeOnly.MinValue);
        var toLocalExclusive = toDate.AddDays(1)
            .ToDateTime(TimeOnly.MinValue);

        var fromUtc = _timeService.ToUtc(fromLocal);
        var toUtcExclusive = _timeService.ToUtc(toLocalExclusive);

        var bookings = await _bookingRepository.GetAllAsync();

        return bookings
            .Where(b =>
                b.StartTime < toUtcExclusive &&
                b.EndTime > fromUtc)
            .OrderBy(b => b.StartTime)
            .ToList();
    }

    public async Task<Booking?> CreateBookingAsync(
        HubertBookingRequestDto request,
        string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

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

        if (startLocal < _timeService.GetCurrentSwedishTime() ||
            startLocal.TimeOfDay < TimeSpan.FromHours(7) ||
            endLocal > startLocal.Date.AddDays(1))
        {
            return null;
        }

        var startUtc = _timeService.ToUtc(startLocal);
        var endUtc = _timeService.ToUtc(endLocal);

        var resources = await _resourceRepository.GetByTypeAsync(
            request.ResourceType.Value);

        foreach (var resource in resources)
        {
            var isAvailable =
                await _bookingRepository.IsResourceAvailableAsync(
                    startUtc,
                    endUtc,
                    resource.ResourceId);

            if (!isAvailable)
            {
                continue;
            }

            var booking = new Booking
            {
                ResourceId = resource.ResourceId,
                UserId = userId,
                StartTime = startUtc,
                EndTime = endUtc
            };

            var createdBooking =
                await _bookingRepository.CreateBookingAsync(booking);

            if (createdBooking == null)
            {
                continue;
            }

            await _hubContext.Clients.All.SendAsync("BookingsChanged");

            return createdBooking;
        }

        return null;
    }
}

