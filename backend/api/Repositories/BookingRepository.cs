using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace api.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly AppDbContext _context;

        public BookingRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<Booking>> GetAllAsync()
        {
            return await _context.Bookings
                .Include(b => b.Resource)
                .Include(b => b.User)
                .ToListAsync();
        }
        public async Task<Booking?> GetByIdAsync(int id)
        {
            return await _context.Bookings
                .Include(b => b.Resource)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.BookingId == id);
        }

        public async Task<bool> IsResourceAvailableAsync(DateTime startTime, DateTime endTime, int resourceId)
        {
            var alreadyBooked = await _context.Bookings.AnyAsync
            (b => b.ResourceId == resourceId && startTime < b.EndTime && endTime > b.StartTime);
            return !alreadyBooked;
        }

        public async Task<Booking?> CreateBookingAsync(Booking booking)
        {
            var alreadyBooked = await IsResourceAvailableAsync(booking.StartTime, booking.EndTime, booking.ResourceId);

            if (!alreadyBooked)
            {
                return null;
            }
            await _context.Bookings.AddAsync(booking);
            try
            {
                await _context.SaveChangesAsync();
                return booking;
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is PostgresException
                {
                    SqlState: PostgresErrorCodes.ExclusionViolation,
                    ConstraintName: "Bookings_NoOverlap"
                })
            {
                _context.Entry(booking).State = EntityState.Detached;
                return null;
            }
        }

        public async Task<IEnumerable<Booking>> GetByResourceIdAsync(int resourceId)
        {
            return await _context.Bookings.Where(b => b.ResourceId == resourceId).ToListAsync();
        }

        public async Task<IEnumerable<Booking>> GetByUserIdAsync(string id)
{
    return await _context.Bookings
        .Include(b => b.Resource)
        .Include(b => b.User)
        .Where(b => b.UserId == id && b.EndTime > DateTime.UtcNow)
        .ToListAsync();
}

        public async Task<Booking?> DeleteBookingByIdAsync(int id)
        {
            var booking = await GetByIdAsync(id);
            if (booking == null)
            {
                return null;
            }
            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();
            return booking;
        }
    }
}
