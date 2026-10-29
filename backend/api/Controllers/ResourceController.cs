using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Enums;
using api.Interfaces;
using api.Models;
using api.Repositories;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;

namespace api.Controllers
{
    [ApiController]
    [Route("api/[controller]s")]
    public class ResourceController : ControllerBase
    {
        private readonly IResourceRepository _resourceRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly AvailabilityService _availabilityService;
        public ResourceController(IResourceRepository resourceRepository, IBookingRepository bookingRepository, AvailabilityService availabilityService)
        {
            _resourceRepository = resourceRepository;
            _bookingRepository = bookingRepository;
            _availabilityService = availabilityService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            var resources = await _resourceRepository.GetAllAsync();
            return Ok(resources);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var resource = await _resourceRepository.GetResourceAsync(id);

            if (resource == null)
            {
                return NotFound();
            }
            return Ok(resource);
        }

        [HttpGet("types/{type}")]
        public async Task<IActionResult> GetByType([FromRoute] ResourceType type)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var resources = await _resourceRepository.GetByTypeAsync(type);

            return Ok(resources);
        }

        [HttpGet("{resourceId:int}/bookings")]
        public async Task<IActionResult> GetBookingsByResourceId([FromRoute] int resourceId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var bookings = await _bookingRepository.GetByResourceIdAsync(resourceId);

            var visibleBookings = User.IsInRole("Admin")
                ? bookings
                : bookings.Where(b => b.EndTime > DateTime.UtcNow);

            return Ok(visibleBookings);
        }

        [HttpGet("{id:int}/availability")]
        public async Task<IActionResult> GetAvailabilityById(int id, [FromQuery] DateTime startTime, [FromQuery] DateTime endTime)
        {
            if (startTime >= endTime)
            {
                return BadRequest("Starttiden måste vara lägre än sluttiden");
            }

            var availability = await _availabilityService.GetResourceAvailabilityAsync(id, startTime, endTime);

            if (availability == null)
            {
                return NotFound();
            }

            return Ok(availability);
        }

        [HttpGet("types/{type}/availability")]
        public async Task<IActionResult> GetAvailabilityByType(ResourceType type, [FromQuery] DateTime startTime, [FromQuery] DateTime endTime)
        {
            if (startTime >= endTime)
            {
                return BadRequest("Starttiden måste vara lägre än sluttiden");
            }

            var availability = await _availabilityService.GetResourceTypeAvailabilityAsync(type, startTime, endTime);

            return Ok(availability);
        }
    }
}
