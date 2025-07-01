using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using WaterDelivery.Infrastructure.Data;

namespace WaterDelivery.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // Admin only
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("users")]
        [EnableRateLimiting("AdminPolicy")]
        public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] int size = 10)
        {
            var users = await _context.Users
                .Skip((page - 1) * size)
                .Take(size)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.PhoneNumber,
                    u.AuthProvider,
                    u.IsActive,
                    u.CreatedAt,
                    u.LastLoginAt
                })
                .ToListAsync();

            var totalUsers = await _context.Users.CountAsync();

            return Ok(new
            {
                Users = users,
                TotalCount = totalUsers,
                Page = page,
                PageSize = size,
                TotalPages = (int)Math.Ceiling(totalUsers / (double)size)
            });
        }

        [HttpGet("stats")]
        [EnableRateLimiting("AdminPolicy")]
        public async Task<IActionResult> GetSystemStats()
        {
            var stats = new
            {
                TotalUsers = await _context.Users.CountAsync(),
                ActiveUsers = await _context.Users.CountAsync(u => u.IsActive),
                NewUsersToday = await _context.Users.CountAsync(u => u.CreatedAt.Date == DateTime.UtcNow.Date),
                TotalOtpsSent = await _context.OtpVerifications.CountAsync(),
                ActiveSessions = await _context.LoginSessions.CountAsync(s => s.IsActive && s.ExpiresAt > DateTime.UtcNow)
            };

            return Ok(stats);
        }

        [HttpPut("users/{userId}/status")]
        [EnableRateLimiting("StrictPolicy")]
        public async Task<IActionResult> UpdateUserStatus(Guid userId, [FromBody] UpdateUserStatusRequest request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found");

            user.IsActive = request.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { Message = $"User {(request.IsActive ? "activated" : "deactivated")} successfully" });
        }
    }
}