using System.Collections.Concurrent;
using api.Dtos.HubertDtos;

namespace api.Services;

public class HubertPendingBookingService
{
    private sealed record PendingBooking(
        string UserId,
        HubertBookingRequestDto Request,
        DateTime ExpiresAtUtc);

    private readonly ConcurrentDictionary<string, PendingBooking>
        _pendingBookings = new();

    public void Save(
        string userId,
        HubertBookingRequestDto request)
    {
        var copy = new HubertBookingRequestDto
        {
            ResourceType = request.ResourceType,
            Date = request.Date,
            StartTime = request.StartTime,
            DurationMinutes = request.DurationMinutes
        };

        _pendingBookings[userId] = new PendingBooking(
            userId,
            copy,
            DateTime.UtcNow.AddMinutes(10));
    }

    public bool TryTake(
        string userId,
        out HubertBookingRequestDto? request)
    {
        request = null;

        if (!_pendingBookings.TryRemove(
                userId, out var pending))
        {
            return false;
        }

        if (pending.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return false;
        }

        request = pending.Request;
        return true;
    }

    public void Cancel(string userId)
    {
        _pendingBookings.TryRemove(userId, out _);
    }
}
