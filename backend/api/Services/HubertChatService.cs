
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using api.Dtos.HubertDtos;

namespace api.Services;

public class HubertChatService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly HubertService _hubertService;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public HubertChatService(
        IHttpClientFactory httpClientFactory,
        HubertService hubertService)
    {
        _httpClientFactory = httpClientFactory;
        _hubertService = hubertService;
    }

    public async Task<(string Message, string ResponseId)> GetResponseAsync(
        string userMessage,
        string? previousResponseId)
    {
        var client = _httpClientFactory.CreateClient("openai");

        var resources = await _hubertService.GetResourcesAsync();

        var resourceInfo = string.Join(
            "\n",
            resources
                .GroupBy(resource => resource.ResourceType)
                .Select(group => $"{group.Key}: {group.Count()} stycken")
        );

        var swedishNow = _hubertService.GetCurrentSwedishTime();

        var instructions = $"""
            Du är Hubert, InnoviaHubs personliga digitala assistent.

            Du är kunnig, trevlig, hjälpsam och professionell.
            Skriv naturligt på svenska, som en vänlig medarbetare.
            Undvik robotliknande formuleringar och onödiga förklaringar.

            Du hjälper användare med frågor om InnoviaHubs
            resurser, tillgänglighet och bokningar.

            AKTUELL RESURSINFORMATION
            {resourceInfo}

            Resursinformationen visar vilka resurser som finns totalt.
            Den visar INTE vilka resurser som är lediga just nu.

            DATUM OCH TID
            Aktuellt datum och tid i Sverige:
            {swedishNow:yyyy-MM-dd HH:mm}

            Tolka relativa datum enligt svensk kalender.
            Bokningsbara tider är 07:00–24:00.

            SAMTALSMINNE
            Kom ihåg information som användaren har angett
            tidigare i samma samtal.

            Om användaren exempelvis först frågar om ett
            mötesrum nästa onsdag och sedan säger
            "från 12 i två timmar", ska du kombinera
            informationen från båda meddelandena.

            Om användaren ändrar datum, tid eller resurstyp
            ska den nya informationen gälla.

            VERKTYG OCH TILLGÄNGLIGHET
            Du har tillgång till verktyget check_availability.

            Använd verktyget när användaren frågar om
            tillgänglighet, lediga resurser eller upptagna resurser
            under ett bestämt tidsintervall.

            Använd även verktyget om användaren frågar
            om ALLA resurser är lediga eller hur många
            som är bokade under ett tidsintervall.

            Om information saknas, ställ en naturlig följdfråga.
            Gissa inte resurstyp, datum, starttid eller bokningslängd.

            När användaren anger starttid och sluttid,
            räkna ut bokningslängden i minuter.

            Verktyget returnerar:
            - totalResources: totalt antal resurser
            - availableResources: antal lediga resurser
            - occupiedResources: antal upptagna resurser
            - isAvailable: om minst en resurs är ledig

            Använd alltid verktygets faktiska siffror
            när du svarar på frågor om tillgänglighet.

            Om totalResources är 4 och availableResources är 3
            ska du förklara att 3 är lediga och 1 är upptagen.

            Om availableResources är lika med totalResources
            är alla resurser lediga under tidsintervallet.

            Om availableResources är 0 är ingen resurs ledig.

            Påstå aldrig att resurser är lediga eller upptagna
            utan ett verifierat resultat från verktyget.

            Om användaren frågar om en annan tid,
            använd verktyget igen.

            Verktyget visar antal lediga och upptagna resurser,
            men inte vilka specifika resurs-ID som är bokade.

            BOKNINGAR
            Du kan för närvarande kontrollera tillgänglighet,
            men du kan INTE skapa eller genomföra bokningar.

            Erbjud inte att boka åt användaren.
            Påstå aldrig att en bokning har skapats.

            Om användaren vill boka, förklara vänligt
            att bokning ännu inte stöds direkt i chatten.

            Håll dig till InnoviaHub och dess verksamhet.
            """;

        var tools = new object[]
        {
            new
            {
                type = "function",
                name = "check_availability",
                description =
                    "Kontrollerar verklig tillgänglighet för en resurstyp " +
                    "under ett angivet tidsintervall på InnoviaHub. " +
                    "Returnerar totalt antal, antal lediga och antal upptagna.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        resourceType = new
                        {
                            type = "string",
                            @enum = new[]
                            {
                                "Skrivbord",
                                "Mötesrum",
                                "VRHeadset",
                                "AIServer"
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
                            description = "Bokningens längd i minuter"
                        }
                    },
                    required = new[]
                    {
                        "resourceType",
                        "date",
                        "startTime",
                        "durationMinutes"
                    },
                    additionalProperties = false
                },
                strict = true
            }
        };

        var body = new
        {
            model = "gpt-5-mini",
            instructions,
            input = userMessage,
            previous_response_id = previousResponseId,
            tools,
            parallel_tool_calls = false
        };

        var response = await client.PostAsJsonAsync("", body);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new HttpRequestException(
                $"OpenAI-fel: {error}");
        }

        using var firstJson = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var currentResponseId = firstJson.RootElement
            .GetProperty("id")
            .GetString()
            ?? throw new InvalidOperationException(
                "Svar-ID saknas.");

        var currentOutput = firstJson.RootElement
            .GetProperty("output")
            .Clone();

        for (int attempt = 0; attempt < 8; attempt++)
        {
            var functionCalls = currentOutput
                .EnumerateArray()
                .Where(item =>
                    item.GetProperty("type").GetString()
                    == "function_call")
                .ToList();

            if (functionCalls.Count == 0)
            {
                return (
                    ExtractMessage(currentOutput),
                    currentResponseId
                );
            }

            var toolOutputs = new List<object>();

            foreach (var functionCall in functionCalls)
            {
                var name = functionCall
                    .GetProperty("name")
                    .GetString();

                var callId = functionCall
                    .GetProperty("call_id")
                    .GetString();

                var arguments = functionCall
                    .GetProperty("arguments")
                    .GetString();

                if (name != "check_availability" ||
                    string.IsNullOrWhiteSpace(callId) ||
                    string.IsNullOrWhiteSpace(arguments))
                {
                    throw new InvalidOperationException(
                        "Ogiltigt verktygsanrop från Hubert.");
                }

                var bookingRequest =
                    JsonSerializer.Deserialize<HubertBookingRequestDto>(
                        arguments,
                        _jsonOptions
                    );

                string toolResult;

                if (bookingRequest?.ResourceType == null ||
                    bookingRequest.Date == null ||
                    bookingRequest.StartTime == null ||
                    bookingRequest.DurationMinutes is null or <= 0)
                {
                    toolResult = JsonSerializer.Serialize(new
                    {
                        valid = false,
                        message = "Bokningsuppgifterna är ofullständiga."
                    });
                }
                else
                {
                    var startLocal =
                        bookingRequest.Date.Value.ToDateTime(
                            bookingRequest.StartTime.Value
                        );

                    var endLocal = startLocal.AddMinutes(
                        bookingRequest.DurationMinutes.Value
                    );

                    var now = _hubertService.GetCurrentSwedishTime();

                    if (startLocal < now ||
                        startLocal.TimeOfDay < TimeSpan.FromHours(7) ||
                        endLocal > startLocal.Date.AddDays(1))
                    {
                        toolResult = JsonSerializer.Serialize(new
                        {
                            valid = false,
                            message =
                                "Tiden är passerad eller ligger " +
                                "utanför bokningsbara tider 07:00–24:00."
                        });
                    }
                    else
                    {
                        var availability =
                            await _hubertService.GetBookingAvailabilityAsync(
                                bookingRequest
                            );

                        if (availability == null)
                        {
                            toolResult = JsonSerializer.Serialize(new
                            {
                                valid = false,
                                message =
                                    "Kunde inte kontrollera tillgängligheten."
                            });
                        }
                        else
                        {
                            var occupiedResources =
                                availability.TotalResources -
                                availability.AvailableResources;

                            toolResult = JsonSerializer.Serialize(new
                            {
                                valid = true,
                                resourceType =
                                    bookingRequest.ResourceType.ToString(),
                                date =
                                    bookingRequest.Date.Value.ToString(
                                        "yyyy-MM-dd"),
                                startTime =
                                    bookingRequest.StartTime.Value.ToString(
                                        "HH:mm:ss"),
                                durationMinutes =
                                    bookingRequest.DurationMinutes.Value,
                                totalResources =
                                    availability.TotalResources,
                                availableResources =
                                    availability.AvailableResources,
                                occupiedResources,
                                isAvailable =
                                    availability.AvailableResources > 0
                            });
                        }
                    }
                }

                toolOutputs.Add(new
                {
                    type = "function_call_output",
                    call_id = callId,
                    output = toolResult
                });
            }

            var followUpBody = new
            {
                model = "gpt-5-mini",
                previous_response_id = currentResponseId,
                instructions,
                tools,
                parallel_tool_calls = false,
                input = toolOutputs
            };

            var followUpResponse =
                await client.PostAsJsonAsync("", followUpBody);

            if (!followUpResponse.IsSuccessStatusCode)
            {
                var error =
                    await followUpResponse.Content.ReadAsStringAsync();

                throw new HttpRequestException(
                    $"OpenAI-fel: {error}");
            }

            using var followUpJson = JsonDocument.Parse(
                await followUpResponse.Content.ReadAsStringAsync());

            currentResponseId = followUpJson.RootElement
                .GetProperty("id")
                .GetString()
                ?? throw new InvalidOperationException(
                    "Svar-ID saknas.");

            currentOutput = followUpJson.RootElement
                .GetProperty("output")
                .Clone();
        }

        throw new InvalidOperationException(
            "Hubert gjorde för många verktygsanrop.");
    }

    private static string ExtractMessage(JsonElement output)
    {
        foreach (var item in output.EnumerateArray())
        {
            if (item.GetProperty("type").GetString() != "message")
            {
                continue;
            }

            foreach (var content in item
                .GetProperty("content")
                .EnumerateArray())
            {
                if (content.TryGetProperty("text", out var text))
                {
                    return text.GetString() ?? "";
                }
            }
        }

        return "Jag kunde inte formulera ett svar just nu.";
    }
}
