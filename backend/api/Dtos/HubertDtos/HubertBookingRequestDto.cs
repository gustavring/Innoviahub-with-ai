using api.Enums;

namespace api.Dtos.HubertDtos;

public class HubertBookingRequestDto
{
    public ResourceType? ResourceType { get; set; }

    public DateOnly? Date { get; set; }

    public TimeOnly? StartTime { get; set; }

    public int? DurationMinutes { get; set; }

    public string? TimePreference { get; set; }
}