// WaterDelivery.API/Controllers/DashboardController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WaterDelivery.Infrastructure.Data;

namespace WaterDelivery.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requires JWT authentication
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("profile")]
        [EnableRateLimiting("UserPolicy")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetCurrentUserId();

            var user = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.PhoneNumber,
                    u.IsEmailVerified,
                    u.IsPhoneVerified,
                    u.AuthProvider,
                    u.CreatedAt,
                    u.LastLoginAt
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound("User not found");

            return Ok(user);
        }

        [HttpGet("stats")]
        [EnableRateLimiting("UserPolicy")]
        public async Task<IActionResult> GetUserStats()
        {
            var userId = GetCurrentUserId();

            var stats = new
            {
                TotalOrders = 0, // TODO: Implement when order system ready
                TotalSpent = 0m,
                LastLogin = await _context.Users
                    .Where(u => u.Id == userId)
                    .Select(u => u.LastLoginAt)
                    .FirstOrDefaultAsync(),
                AccountAge = await _context.Users
                    .Where(u => u.Id == userId)
                    .Select(u => DateTime.UtcNow.Subtract(u.CreatedAt).Days)
                    .FirstOrDefaultAsync()
            };

            return Ok(stats);
        }

        [HttpPut("profile")]
        [EnableRateLimiting("StrictPolicy")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var userId = GetCurrentUserId();

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found");

            // Update allowed fields
            if (!string.IsNullOrEmpty(request.Username))
                user.Username = request.Username;

            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Profile updated successfully" });
        }

        [HttpGet("sessions")]
        [EnableRateLimiting("UserPolicy")]
        public async Task<IActionResult> GetActiveSessions()
        {
            var userId = GetCurrentUserId();

            var sessions = await _context.LoginSessions
                .Where(s => s.UserId == userId && s.IsActive && s.ExpiresAt > DateTime.UtcNow)
                .Select(s => new
                {
                    s.Id,
                    s.DeviceInfo,
                    s.IpAddress,
                    s.CreatedAt,
                    s.ExpiresAt,
                    IsCurrent = s.JwtTokenId == GetCurrentTokenId()
                })
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return Ok(sessions);
        }

        [HttpDelete("sessions/{sessionId}")]
        [EnableRateLimiting("StrictPolicy")]
        public async Task<IActionResult> RevokeSession(Guid sessionId)
        {
            var userId = GetCurrentUserId();

            var session = await _context.LoginSessions
                .Where(s => s.Id == sessionId && s.UserId == userId)
                .FirstOrDefaultAsync();

            if (session == null)
                return NotFound("Session not found");

            session.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Session revoked successfully" });
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(userIdClaim ?? throw new UnauthorizedAccessException("User ID not found"));
        }

        private string GetCurrentTokenId()
        {
            return User.FindFirstValue("jti") ?? string.Empty;
        }
    }
}