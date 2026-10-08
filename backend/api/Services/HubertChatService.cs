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
        string? userId)
    {
        // En bekräftelse gäller bara ett sparat bokningsförslag.
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var reply = userMessage.Trim().ToLowerInvariant();

            if (reply is "ja" or "ja tack" or "bekräfta" or "bekräfta bokningen")
            {
                if (!_pendingBookings.TryTake(userId, out var pendingRequest)
                    || pendingRequest == null)
                {
                    return (
                        "Det finns ingen väntande bokning att bekräfta. " +
                        "Berätta vad du vill boka så kontrollerar jag tillgängligheten.",
                        previousResponseId ?? ""
                    );
                }

                var booking = await _hubertService.CreateBookingAsync(
                    pendingRequest,
                    userId
                );

                if (booking == null)
                {
                    return (
                        "Bokningen kunde inte genomföras. " +
                        "Tiden kan ha blivit upptagen eller vara ogiltig. " +
                        "Be mig kontrollera en ny tid.",
                        previousResponseId ?? ""
                    );
                }

                var start = pendingRequest.Date!.Value.ToDateTime(
                    pendingRequest.StartTime!.Value
                );

                var end = start.AddMinutes(
                    pendingRequest.DurationMinutes!.Value
                );

                return (
                    $"""
                    ✅ Bokningen är bekräftad!

                    Bokningsuppgifter:
                    • Resurs: {pendingRequest.ResourceType}
                    • Datum: {start:yyyy-MM-dd}
                    • Starttid: {start:HH:mm}
                    • Sluttid: {end:HH:mm}
                    • Längd: {pendingRequest.DurationMinutes} minuter
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

        var client = _httpClientFactory.CreateClient("openai");

        var resources = await _hubertService.GetResourcesAsync();

        var resourceInfo = string.Join(
            "\n",
            resources
                .GroupBy(resource => resource.ResourceType)
                .Select(group => $"{group.Key}: {group.Count()} stycken")
        );

        var swedishNow = _hubertService.GetCurrentSwedishTime();

        var loginStatus = string.IsNullOrWhiteSpace(userId)
            ? "Användaren är inte inloggad."
            : "Användaren är inloggad.";

        var instructions = $"""
            Du är Hubert, InnoviaHubs personliga digitala assistent.

            INLOGGNINGSSTATUS
            {loginStatus}

            Du är kunnig, trevlig, hjälpsam och professionell.
            Skriv naturligt på svenska, som en vänlig medarbetare.

            SAMTALSSTIL
            Svara direkt på användarens fråga.
            Håll svaren korta, tydliga och relevanta.

            Ställ bara följdfrågor när information verkligen saknas.
            Fråga inte om något som användaren redan har angett.

            Föreslå inte andra tjänster eller alternativ
            om användaren inte har bett om dem.

            Upprepa inte samma fråga flera gånger.
            Om användaren har svarat, gå vidare.

            Använd naturliga formuleringar.
            Undvik onödiga numrerade alternativ och långa förklaringar.

            Om användarens avsikt är tydlig,
            be inte om en extra bekräftelse av vad frågan betyder.

            Du hjälper användare med frågor om InnoviaHubs
            resurser, tillgänglighet och bokningar.

            AKTUELL RESURSINFORMATION
            {resourceInfo}

            Resursinformationen visar vilka resurser som finns totalt.
            Den visar INTE vilka resurser som är lediga just nu.

            RESURSLISTOR
            När användaren frågar vilka resurser som finns
            eller vilka resurstyper InnoviaHub erbjuder,
            presentera dem alltid som en tydlig punktlista.

            Använd tecknet • framför varje resurs.
            Använd inte bindestreck (-) eller numrerade listor.

            Exempel på format:

            📋 Våra resurser:

            • Skrivbord: [antal] stycken
            • Mötesrum: [antal] stycken
            • VRHeadset: [antal] stycken
            • AIServer: [antal] stycken

            Använd de faktiska antalen från AKTUELL RESURSINFORMATION.
            Hitta aldrig på resurser eller antal.

            Om användaren bara frågar vilka resurser som finns,
            visa listan direkt utan att fråga efter datum eller tid.

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

            • totalResources: totalt antal resurser
            • availableResources: antal lediga resurser
            • occupiedResources: antal upptagna resurser
            • isAvailable: om minst en resurs är ledig

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
            Du kan kontrollera tillgänglighet med check_availability.

            När användaren vill genomföra en bokning och alla
            uppgifter finns, använd prepare_booking.

            Det verktyget kontrollerar tillgänglighet
            och lagrar ett väntande bokningsförslag på servern.

            Om användaren inte är inloggad,
            förklara att inloggning krävs.

            Om prepare_booking returnerar prepared=true,
            ska du alltid presentera bokningsförslaget
            i följande tydliga format:

            📋 Förslag till bokning:
            
            • Resurs: [resurstyp]
            • Datum: [YYYY-MM-DD]
            • Starttid: [HH:mm]
            • Sluttid: [HH:mm]
            • Längd: [antal timmar och minuter]

            Vill du bekräfta bokningen?
            Svara ja eller nej.

            Använd alltid radbrytningar och en punktlista.
            Skriv aldrig hela bokningsförslaget som ett
            sammanhängande textstycke.

            Beräkna sluttiden från starttiden och
            durationMinutes som verktyget returnerar.
            Använd endast uppgifter från verktygsresultatet.

            Be inte om rubrik eller kommentar, det behövs inte.

            Ett ja behandlas av servern i nästa användarmeddelande.

            Du får aldrig själv påstå att bokningen är skapad.

            Om användaren ändrar uppgifter,
            använd prepare_booking igen.

            Vid ren fråga om tillgänglighet ska du
            enbart använda check_availability.
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
            },
            new
            {
                type = "function",
                name = "prepare_booking",
                description =
                    "Förbereder en bokning som användaren uttryckligen vill göra. " +
                    "Kontrollerar tillgänglighet och sparar förslaget för bekräftelse. " +
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

                if (name is not ("check_availability" or "prepare_booking") ||
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

                            var preparing = name == "prepare_booking";
                            var prepared = false;

                            if (preparing &&
                                availability.AvailableResources > 0 &&
                                !string.IsNullOrWhiteSpace(userId))
                            {
                                _pendingBookings.Save(
                                    userId,
                                    bookingRequest
                                );

                                prepared = true;
                            }

                            toolResult = JsonSerializer.Serialize(new
                            {
                                valid = true,
                                prepared,
                                loginRequired =
                                    preparing &&
                                    string.IsNullOrWhiteSpace(userId),
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

