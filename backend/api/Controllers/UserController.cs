using api.Dtos.UserDtos;
using api.Interfaces;
using api.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace api.Controllers
{
    [ApiController]
    [Route("api/[controller]s")]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IHubContext<BookingHub> _hubContext;

        public UserController(
            IUserRepository userRepository,
            IHubContext<BookingHub> hubContext)
        {
            _userRepository = userRepository;
            _hubContext = hubContext;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
           var users = await _userRepository.GetAllAsync();

           var userDtos = new List<UserDto>();

           foreach (var user in users)
           {
               var roles = await _userRepository.GetRolesAsync(user);

               userDtos.Add(new UserDto
               {
                   UserId = user.Id,
                   Email = user.Email ?? string.Empty,
                   Role = roles.Contains("Admin") ? "Admin" : "User"
                });
            }

            return Ok(userDtos);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userRepository.GetByIdAsync(id);

            if (user == null)
            {
                return NotFound("Användaren hittades inte.");
            }

            var roles = await _userRepository.GetRolesAsync(user);

            if (roles.Contains("Admin"))
            {
                return BadRequest("Administratörskonton kan inte tas bort här.");
            }

            var deleted = await _userRepository.DeleteUserAsync(user);

            if (!deleted)
            {
                return BadRequest("Kunde inte ta bort användaren.");
            }

            await _hubContext.Clients.All.SendAsync("BookingsChanged");

            return NoContent();
        }
    }
}

