
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using api.Dtos.HubertDtos;

namespace api.Services;

public class HubertChatService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly HubertService _hubertService;
    private readonly HubertPendingBookingService _pendingBookings;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public HubertChatService(
        IHttpClientFactory httpClientFactory,
        HubertService hubertService,
        HubertPendingBookingService pendingBookings)
    {
        _httpClientFactory = httpClientFactory;
        _hubertService = hubertService;
        _pendingBookings = pendingBookings;
    }

    public async Task<(string Message, string ResponseId)> GetResponseAsync(
        string userMessage,
        string? previousResponseId,
        string? userId,
        bool isAdmin)
    {
        // Behåll den befintliga bokningsbekräftelsen på servern.
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var reply = userMessage.Trim().ToLowerInvariant();

            if (reply is "ja" or "ja tack" or "bekräfta" or "bekräfta bokningen")
            {
                if (!_pendingBookings.TryTake(userId, out var pending)
                    || pending == null)
                {
                    return (
                        "Det finns ingen väntande bokning att bekräfta. " +
                        "Berätta vad du vill boka så kontrollerar jag tillgängligheten.",
                        previousResponseId ?? ""
                    );
                }

                var booking = await _hubertService.CreateBookingAsync(
                    pending, userId);

                if (booking == null)
                {
                    return (
                        "Bokningen kunde inte genomföras. " +
                        "Tiden kan ha blivit upptagen eller vara ogiltig. " +
                        "Be mig kontrollera en ny tid.",
                        previousResponseId ?? ""
                    );
                }

                var start = pending.Date!.Value.ToDateTime(
                    pending.StartTime!.Value);

                var end = start.AddMinutes(
                    pending.DurationMinutes!.Value);

                return (
                    $"""
                    ✅ Bokningen är bekräftad!

                    Bokningsuppgifter:
                    • Resurs: {pending.ResourceType}
                    • Datum: {start:yyyy-MM-dd}
                    • Starttid: {start:HH:mm}
                    • Sluttid: {end:HH:mm}
                    • Längd: {pending.DurationMinutes} minuter
                    • Bokningsnummer: {booking.BookingId}

                    Du hittar din bokning under Mina bokningar.
                    """,
                    previousResponseId ?? ""
                );
            }

            _pendingBookings.Cancel(userId);

            if (reply is "nej" or "nej tack" or "avbryt")
            {
                return (
                    "Okej, jag avbröt bokningen. " +
                    "Säg till om du vill boka något annat.",
                    previousResponseId ?? ""
                );
            }
        }

        var resources = await _hubertService.GetResourcesAsync();

        var resourceInfo = string.Join("\n",
            resources
                .GroupBy(r => r.ResourceType)
                .Select(g => $"{g.Key}: {g.Count()} stycken"));

        var now = _hubertService.GetCurrentSwedishTime();

        var instructions = BuildInstructions(
            resourceInfo,
            now,
            !string.IsNullOrWhiteSpace(userId),
            isAdmin);

        var tools = BuildTools(isAdmin && !string.IsNullOrWhiteSpace(userId));

        var client = _httpClientFactory.CreateClient("openai");

        var (responseId, output) = await SendToOpenAiAsync(
            client,
            new
            {
                model = "gpt-5-mini",
                instructions,
                input = userMessage,
                previous_response_id = previousResponseId,
                tools,
                parallel_tool_calls = false
            });

        for (int attempt = 0; attempt < 8; attempt++)
        {
            var calls = output.EnumerateArray()
                .Where(x => x.GetProperty("type").GetString() == "function_call")
                .ToList();

            if (calls.Count == 0)
                return (ExtractMessage(output), responseId);

            var results = new List<object>();

            foreach (var call in calls)
            {
                var name = call.GetProperty("name").GetString();
                var callId = call.GetProperty("call_id").GetString();
                var arguments = call.GetProperty("arguments").GetString();

                if (string.IsNullOrWhiteSpace(name) ||
                    string.IsNullOrWhiteSpace(callId) ||
                    string.IsNullOrWhiteSpace(arguments))
                {
                    throw new InvalidOperationException(
                        "Ogiltigt verktygsanrop från Hubert.");
                }

                var result = await HandleToolAsync(
                    name, arguments, userId, isAdmin);

                results.Add(new
                {
                    type = "function_call_output",
                    call_id = callId,
                    output = result
                });
            }

            (responseId, output) = await SendToOpenAiAsync(
                client,
                new
                {
                    model = "gpt-5-mini",
                    previous_response_id = responseId,
                    instructions,
                    tools,
                    parallel_tool_calls = false,
                    input = results
                });
        }

        throw new InvalidOperationException(
            "Hubert gjorde för många verktygsanrop.");
    }

    private static string BuildInstructions(
        string resourceInfo,
        DateTime now,
        bool loggedIn,
        bool isAdmin)
    {
        return $"""
            Du är Hubert, InnoviaHubs personliga digitala assistent.
            Svara naturligt, vänligt, kort och tydligt på svenska.

            STATUS
            Inloggad: {loggedIn}
            Administratör: {isAdmin}

            RESURSER
            {resourceInfo}

            Dessa antal visar totalt antal resurser,
            inte hur många som är lediga.

            När användaren frågar vilka resurser som finns,
            visa en punktlista med • och de faktiska antalen.
            Fråga inte efter datum eller tid vid en ren resursfråga.

            DATUM OCH TID
            Svensk tid just nu: {now:yyyy-MM-dd HH:mm}
            Tolka idag, imorgon och relativa datum enligt svensk tid.
            Nästa vecka betyder måndag till söndag nästa kalendervecka.
            Bokningsbara tider är 07:00–24:00.

            SAMTAL
            Kom ihåg tidigare uppgifter i samma samtal.
            Kombinera exempelvis tidigare resurstyp och datum
            med en starttid som användaren anger senare.
            Nya uppgifter ersätter gamla.
            Ställ bara följdfrågor när nödvändig information saknas.
            Föreslå inte oombedda alternativ.

            TILLGÄNGLIGHET
            Använd check_availability vid frågor om lediga,
            upptagna eller samtliga resurser under en viss tid.
            Verktyget ger totalResources, availableResources,
            occupiedResources och isAvailable.
            Använd bara verifierade siffror.
            Gissa aldrig resurstyp, datum, tid eller längd.
            Räkna ut durationMinutes om start och slut anges.
            Använd verktyget igen när tidsintervallet ändras.

            BOKA
            När användaren vill boka och alla uppgifter finns,
            använd prepare_booking.
            Vid ren tillgänglighetsfråga används bara
            check_availability.

            Om inloggning krävs, förklara det.
            Om prepared=true, visa:

            📋 Förslag till bokning:
            • Resurs: [resurstyp]
            • Datum: [YYYY-MM-DD]
            • Starttid: [HH:mm]
            • Sluttid: [HH:mm]
            • Längd: [timmar och minuter]

            Vill du bekräfta bokningen?
            Svara ja eller nej.

            Beräkna sluttiden från verktygets uppgifter.
            Begär inte rubrik eller kommentar.
            Servern hanterar användarens ja.
            Påstå aldrig att bokningen är skapad
            innan servern har bekräftat den.
            Om uppgifterna ändras, använd prepare_booking igen.

            MINA BOKNINGAR
            Vid frågor om användarens egna bokningar,
            använd get_my_bookings.
            Om ingen period anges, använd idag till 30 dagar framåt.

            Presentera varje bokning separat med en tydlig rubrik:
            Bokning 1, Bokning 2, Bokning 3 och så vidare.

            Använd alltid följande format:

            Här är dina bokningar [period]:

            Bokning 1:
            • Resurs: [resurstyp]
            • Datum: [YYYY-MM-DD]
            • Starttid: [HH:mm]
            • Sluttid: [HH:mm]

            Bokning 2
            • Resurs: [resurstyp]
            • Datum: [YYYY-MM-DD]
            • Starttid: [HH:mm]
            • Sluttid: [HH:mm]

            Visa varje uppgift på en egen rad.
            Använd alltid • framför uppgifterna.
            Skriv aldrig en bokning som en enda lång rad.
            Ha en tom rad mellan bokningarna.
            Använd bara uppgifter från verktyget.
            Tiderna är redan konverterade till svensk tid.

            Om inga bokningar finns, säg det tydligt.
            Om användaren inte är inloggad, kräv inloggning.
            """;
    }

    private static List<object> BuildTools(bool isAdmin)
    {
        var bookingParameters = new
        {
            type = "object",
            properties = new
            {
                resourceType = new
                {
                    type = "string",
                    @enum = new[]
                    {
                        "Skrivbord", "Mötesrum", "VRHeadset", "AIServer"
                    }
                },
                date = new
                {
                    type = "string",
                    description = "Datum YYYY-MM-DD"
                },
                startTime = new
                {
                    type = "string",
                    description = "Svensk lokal tid HH:mm:ss"
                },
                durationMinutes = new
                {
                    type = "integer",
                    description = "Längd i minuter"
                }
            },
            required = new[]
            {
                "resourceType", "date", "startTime", "durationMinutes"
            },
            additionalProperties = false
        };

        var dateParameters = new
        {
            type = "object",
            properties = new
            {
                fromDate = new
                {
                    type = "string",
                    description = "Startdatum YYYY-MM-DD"
                },
                toDate = new
                {
                    type = "string",
                    description = "Slutdatum YYYY-MM-DD"
                }
            },
            required = new[] { "fromDate", "toDate" },
            additionalProperties = false
        };

        var tools = new List<object>
        {
            new
            {
                type = "function",
                name = "check_availability",
                description =
                    "Kontrollerar verklig tillgänglighet och returnerar " +
                    "totalt antal, antal lediga och antal upptagna resurser.",
                parameters = bookingParameters,
                strict = true
            },
            new
            {
                type = "function",
                name = "prepare_booking",
                description =
                    "Förbereder en bokning, kontrollerar tillgänglighet " +
                    "och sparar förslaget för användarens bekräftelse.",
                parameters = bookingParameters,
                strict = true
            },
            new
            {
                type = "function",
                name = "get_my_bookings",
                description =
                    "Hämtar den inloggade användarens egna bokningar.",
                parameters = dateParameters,
                strict = true
            }
        };

        if (isAdmin)
        {
            tools.Add(new
            {
                type = "function",
                name = "get_all_bookings",
                description =
                    "Hämtar alla användares bokningar. Endast admin.",
                parameters = dateParameters,
                strict = true
            });
        }

        return tools;
    }

    private async Task<string> HandleToolAsync(
        string name,
        string arguments,
        string? userId,
        bool isAdmin)
    {
        return name switch
        {
            "check_availability" or "prepare_booking" =>
                await HandleAvailabilityAsync(name, arguments, userId),

            "get_my_bookings" or "get_all_bookings" =>
                await HandleBookingsAsync(name, arguments, userId, isAdmin),

            _ => throw new InvalidOperationException(
                "Ogiltigt verktygsanrop från Hubert.")
        };
    }

    private async Task<string> HandleAvailabilityAsync(
        string name,
        string arguments,
        string? userId)
    {
        var request = JsonSerializer.Deserialize<HubertBookingRequestDto>(
            arguments, _jsonOptions);

        if (request?.ResourceType == null ||
            request.Date == null ||
            request.StartTime == null ||
            request.DurationMinutes is null or <= 0)
        {
            return JsonSerializer.Serialize(new
            {
                valid = false,
                message = "Bokningsuppgifterna är ofullständiga."
            });
        }

        var start = request.Date.Value.ToDateTime(
            request.StartTime.Value);

        var end = start.AddMinutes(
            request.DurationMinutes.Value);

        var now = _hubertService.GetCurrentSwedishTime();

        if (start < now ||
            start.TimeOfDay < TimeSpan.FromHours(7) ||
            end > start.Date.AddDays(1))
        {
            return JsonSerializer.Serialize(new
            {
                valid = false,
                message =
                    "Tiden är passerad eller ligger utanför " +
                    "bokningsbara tider 07:00–24:00."
            });
        }

        var availability =
            await _hubertService.GetBookingAvailabilityAsync(request);

        if (availability == null)
        {
            return JsonSerializer.Serialize(new
            {
                valid = false,
                message = "Kunde inte kontrollera tillgängligheten."
            });
        }

        var preparing = name == "prepare_booking";
        var prepared = false;

        if (preparing &&
            availability.AvailableResources > 0 &&
            !string.IsNullOrWhiteSpace(userId))
        {
            _pendingBookings.Save(userId, request);
            prepared = true;
        }

        return JsonSerializer.Serialize(new
        {
            valid = true,
            prepared,
            loginRequired = preparing && string.IsNullOrWhiteSpace(userId),
            resourceType = request.ResourceType.ToString(),
            date = request.Date.Value.ToString("yyyy-MM-dd"),
            startTime = request.StartTime.Value.ToString("HH:mm:ss"),
            durationMinutes = request.DurationMinutes.Value,
            totalResources = availability.TotalResources,
            availableResources = availability.AvailableResources,
            occupiedResources =
                availability.TotalResources - availability.AvailableResources,
            isAvailable = availability.AvailableResources > 0
        });
    }

    private async Task<string> HandleBookingsAsync(
        string name,
        string arguments,
        string? userId,
        bool isAdmin)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                message = "Du måste vara inloggad för att se bokningar."
            });
        }

        if (name == "get_all_bookings" && !isAdmin)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                message = "Endast administratörer får se alla bokningar."
            });
        }

        using var json = JsonDocument.Parse(arguments);
        var root = json.RootElement;

        var fromText = root.TryGetProperty("fromDate", out var fromElement)
            && fromElement.ValueKind == JsonValueKind.String
                ? fromElement.GetString()
                : null;

        var toText = root.TryGetProperty("toDate", out var toElement)
            && toElement.ValueKind == JsonValueKind.String
                ? toElement.GetString()
                : null;

        if (!DateOnly.TryParseExact(
                fromText, "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var fromDate) ||
            !DateOnly.TryParseExact(
                toText, "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var toDate) ||
            toDate < fromDate)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                message = "Ogiltigt datumintervall."
            });
        }

        var allUsers = name == "get_all_bookings";

        var bookings = allUsers
            ? await _hubertService.GetAllBookingsForHubertAsync(
                fromDate, toDate)
            : await _hubertService.GetMyBookingsAsync(
                userId, fromDate, toDate);

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(
            "Europe/Stockholm");

        var result = bookings.Select(b =>
        {
            var start = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(b.StartTime, DateTimeKind.Utc),
                timeZone);

            var end = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(b.EndTime, DateTimeKind.Utc),
                timeZone);

            return new
            {
                bookingId = b.BookingId,
                resourceType =
                    b.Resource?.ResourceType.ToString() ?? "Okänd resurs",
                date = start.ToString("yyyy-MM-dd"),
                startTime = start.ToString("HH:mm"),
                endTime = end.ToString("HH:mm"),
                userEmail = allUsers
                    ? b.User?.Email ?? "Okänd användare"
                    : null
            };
        }).ToList();

        return JsonSerializer.Serialize(new
        {
            success = true,
            fromDate = fromDate.ToString("yyyy-MM-dd"),
            toDate = toDate.ToString("yyyy-MM-dd"),
            bookings = result
        });
    }

    private static async Task<(string ResponseId, JsonElement Output)>
        SendToOpenAiAsync(HttpClient client, object body)
    {
        using var response = await client.PostAsJsonAsync("", body);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new HttpRequestException($"OpenAI-fel: {error}");
        }

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var responseId = json.RootElement
            .GetProperty("id")
            .GetString()
            ?? throw new InvalidOperationException("Svar-ID saknas.");

        var output = json.RootElement
            .GetProperty("output")
            .Clone();

        return (responseId, output);
    }

    private static string ExtractMessage(JsonElement output)
    {
        foreach (var item in output.EnumerateArray())
        {
            if (item.GetProperty("type").GetString() != "message")
                continue;

            foreach (var content in item
                .GetProperty("content")
                .EnumerateArray())
            {
                if (content.TryGetProperty("text", out var text))
                    return text.GetString() ?? "";
            }
        }

        return "Jag kunde inte formulera ett svar just nu.";
    }
}


