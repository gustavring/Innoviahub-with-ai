
using Microsoft.AspNetCore.Mvc;
using api.Dtos.HubertDtos;
using api.Services;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HubertController : ControllerBase
{
    private readonly HubertChatService _hubertChatService;

    public HubertController(HubertChatService hubertChatService)
    {
        _hubertChatService = hubertChatService;
    }

    [HttpPost]
    public async Task<IActionResult> Chat(
        [FromBody] HubertRequestDto request)
    {
        if (request == null ||
            string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Meddelandet får inte vara tomt.");
        }

        try
        {
            var result = await _hubertChatService.GetResponseAsync(
                request.Message,
                request.PreviousResponseId
            );

            return Ok(new HubertResponseDto
            {
                Message = result.Message,
                ResponseId = result.ResponseId
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Hubert-fel: {ex}");

            return StatusCode(
                502,
                "Hubert kunde inte behandla meddelandet."
            );
        }
    }
}
