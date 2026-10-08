using Microsoft.AspNetCore.Mvc;
using api.Dtos.HubertDtos;
using api.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

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
    [AllowAnonymous]
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
            var userId = User.Identity?.IsAuthenticated == true
                ? User.FindFirstValue(ClaimTypes.NameIdentifier)
                : null;

            var result = await _hubertChatService.GetResponseAsync(
                request.Message,
                request.PreviousResponseId,
                userId
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


