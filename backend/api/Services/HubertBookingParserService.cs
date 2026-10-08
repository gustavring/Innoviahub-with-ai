using System.Net.Http.Json;
using System.Text.Json;
using api.Dtos.HubertDtos;

namespace api.Services;

public class HubertBookingParserService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TimeService _timeService;

    public HubertBookingParserService(
        IHttpClientFactory httpClientFactory,
        TimeService timeService)
    {
        _httpClientFactory = httpClientFactory;
        _timeService = timeService;
    }

    public async Task<HubertBookingRequestDto?> ParseAsync(string message)
    {
        var client = _httpClientFactory.CreateClient("openai");
        var now = _timeService.GetCurrentSwedishTime();

        var body = new
        {
            model = "gpt-5-mini",

            instructions = $"""
                Tolka användarens bokningsönskemål för InnoviaHub.

                Aktuellt datum och tid i Sverige:
                {now:yyyy-MM-dd HH:mm}

                Returnera ENDAST ett JSON-objekt med dessa fält:
                resourceType, date, startTime, durationMinutes, timePreference.

                resourceType ska vara ett av:
                Skrivbord, Mötesrum, VRHeadset, AIServer.

                date ska vara YYYY-MM-DD.
                startTime ska vara HH:mm:ss, exempelvis "12:00:00".
                Använd alltid sekunder, även när de är 00.
                
                durationMinutes ska vara ett heltal.
                timePreference kan vara exempelvis "förmiddag" eller "eftermiddag".

                Använd null när information saknas.
                Gissa inte på saknade uppgifter.

                Tolka relativa datum utifrån dagens svenska datum.
                Om datumet är tvetydigt, använd null.

                Returnera ingen förklarande text och ingen markdown.
                """,

            input = message
        };

        var response = await client.PostAsJsonAsync("", body);

        if (!response.IsSuccessStatusCode)
        {
            var errorText = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"Hubert parser API-fel: {response.StatusCode}");
            Console.WriteLine(errorText);

            return null;
        }

        using var result = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync()
        );

        foreach (var item in result.RootElement.GetProperty("output").EnumerateArray())
        {
            if (item.GetProperty("type").GetString() != "message")
                continue;

            var content = item.GetProperty("content")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrWhiteSpace(content))
                return null;

            try
            {
                return JsonSerializer.Deserialize<HubertBookingRequestDto>(
                    content,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Converters =
                        {
                            new System.Text.Json.Serialization.JsonStringEnumConverter()
                        }
                    }
                );
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Hubert parser JSON-fel: {ex.Message}");
                Console.WriteLine($"AI-svar: {content}");

                return null;
            }
        }

        return null;
    }
}