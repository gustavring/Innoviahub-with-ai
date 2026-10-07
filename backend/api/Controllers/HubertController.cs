using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text.Json;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HubertController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HubertController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] HubertRequest request)
    {
        var client = _httpClientFactory.CreateClient("openai");

        var body = new
        {
            model = "gpt-5-mini",
            input = request.Message
        };

        var response = await client.PostAsJsonAsync("", body);

        var result = await response.Content.ReadAsStringAsync();

        using var json = JsonDocument.Parse(result);

        var output = json.RootElement.GetProperty("output");

        string? message = null;

        foreach (var item in output.EnumerateArray())
        {
            if (item.GetProperty("type").GetString() == "message")
            {
                message = item
                    .GetProperty("content")[0]
                    .GetProperty("text")
                    .GetString();

                break;
            }
        }

        return Ok(new
        {
            message = message
        });
    }

    public class HubertRequest
    {
        public string Message { get; set; } = "";
    }
}
