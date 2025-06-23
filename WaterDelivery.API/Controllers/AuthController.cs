using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WaterDelivery.Application.Interfaces;
using WaterDelivery.Domain.Entities;
using WaterDelivery.Infrastructure.Data;

namespace WaterDelivery.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IPasswordService _passwordService;
        private readonly IOtpService _otpService;
        private readonly ISmsService _smsService;

        public AuthController(
            ApplicationDbContext context,
            IJwtService jwtService,
            IPasswordService passwordService,
            IOtpService otpService,
            ISmsService smsService)
        {
            _context = context;
            _jwtService = jwtService;
            _passwordService = passwordService;
            _otpService = otpService;
            _smsService = smsService;
        }

        [HttpPost("register/username")]
        public async Task<IActionResult> RegisterWithUsername([FromBody] RegisterUsernameRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validate password
            if (!_passwordService.IsValidPassword(request.Password))
                return BadRequest("Password must be at least 8 characters with uppercase, lowercase, number and symbol");

            if (request.Password != request.ConfirmPassword)
                return BadRequest("Passwords do not match");

            // Check if username/email already exists
            var existingUser = await _context.Users
                .Where(u => u.Username == request.Username || u.Email == request.Email)
                .FirstOrDefaultAsync();

            if (existingUser != null)
                return BadRequest("Username or email already exists");

            // Generate salt and hash password
            var salt = _passwordService.GenerateSalt();
            var passwordHash = _passwordService.HashPassword(request.Password, salt);

            // Create user
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                Salt = salt,
                AuthProvider = AuthProvider.Email,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);

            // Generate email verification OTP
            var emailOtp = await _otpService.GenerateOtpAsync(request.Email, OtpPurpose.EmailVerification, user.Id);

            await _context.SaveChangesAsync();

            // TODO: Send email verification
            // await _emailService.SendVerificationEmailAsync(request.Email, emailOtp);

            return Ok(new { Message = "Registration successful. Please check your email for verification code." });
        }

        [HttpPost("register/phone")]
        public async Task<IActionResult> RegisterWithPhone([FromBody] RegisterPhoneRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Check if phone already exists
            var existingUser = await _context.Users
                .Where(u => u.PhoneNumber == request.PhoneNumber)
                .FirstOrDefaultAsync();

            if (existingUser != null)
                return BadRequest("Phone number already registered");

            // Generate and send OTP
            var otp = await _otpService.GenerateOtpAsync(request.PhoneNumber, OtpPurpose.Register);

            // Send SMS
            var smsSent = await _smsService.SendOtpAsync(request.PhoneNumber, otp);
            if (!smsSent)
                return StatusCode(500, "Failed to send OTP. Please try again.");

            return Ok(new { Message = "OTP sent to your phone number", ExpiresInMinutes = 3 });
        }

        [HttpPost("verify-phone-otp")]
        public async Task<IActionResult> VerifyPhoneOtp([FromBody] VerifyOtpRequest request)
        {
            if (!await _otpService.ValidateOtpAsync(request.PhoneNumber, request.Otp, OtpPurpose.Register))
                return BadRequest("Invalid or expired OTP");

            // Create user
            var user = new User
            {
                Id = Guid.NewGuid(),
                PhoneNumber = request.PhoneNumber,
                Salt = _passwordService.GenerateSalt(),
                AuthProvider = AuthProvider.Phone,
                IsPhoneVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Generate tokens
            var accessToken = await _jwtService.GenerateAccessTokenAsync(user.Id, user.PhoneNumber ?? "");
            var refreshToken = await _jwtService.GenerateRefreshTokenAsync();

            // Store refresh token
            var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            _context.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 30 * 60 // 30 minutes in seconds
            });
        }

        [HttpPost("login/username")]
        public async Task<IActionResult> LoginWithUsername([FromBody] LoginUsernameRequest request)
        {
            var user = await _context.Users
                .Where(u => u.Username == request.Username || u.Email == request.Username)
                .FirstOrDefaultAsync();

            if (user == null || user.PasswordHash == null)
                return Unauthorized("Invalid credentials");

            if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash, user.Salt))
                return Unauthorized("Invalid credentials");

            if (!user.IsActive)
                return Unauthorized("Account is deactivated");

            // Generate tokens
            var accessToken = await _jwtService.GenerateAccessTokenAsync(user.Id, user.Email ?? user.Username ?? "");
            var refreshToken = await _jwtService.GenerateRefreshTokenAsync();

            // Store refresh token
            var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            _context.RefreshTokens.Add(refreshTokenEntity);

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 30 * 60
            });
        }

        [HttpPost("login/phone")]
        public async Task<IActionResult> LoginWithPhone([FromBody] LoginPhoneRequest request)
        {
            var user = await _context.Users
                .Where(u => u.PhoneNumber == request.PhoneNumber)
                .FirstOrDefaultAsync();

            if (user == null)
                return BadRequest("Phone number not registered");

            // Generate and send OTP
            var otp = await _otpService.GenerateOtpAsync(request.PhoneNumber, OtpPurpose.Login, user.Id);

            // Send SMS
            var smsSent = await _smsService.SendOtpAsync(request.PhoneNumber, otp);
            if (!smsSent)
                return StatusCode(500, "Failed to send OTP. Please try again.");

            return Ok(new { Message = "OTP sent to your phone number", ExpiresInMinutes = 3 });
        }

        [HttpPost("verify-login-otp")]
        public async Task<IActionResult> VerifyLoginOtp([FromBody] VerifyOtpRequest request)
        {
            if (!await _otpService.ValidateOtpAsync(request.PhoneNumber, request.Otp, OtpPurpose.Login))
                return BadRequest("Invalid or expired OTP");

            var user = await _context.Users
                .Where(u => u.PhoneNumber == request.PhoneNumber)
                .FirstOrDefaultAsync();

            if (user == null || !user.IsActive)
                return Unauthorized("Account not found or deactivated");

            // Generate tokens
            var accessToken = await _jwtService.GenerateAccessTokenAsync(user.Id, user.PhoneNumber ?? "");
            var refreshToken = await _jwtService.GenerateRefreshTokenAsync();

            // Store refresh token
            var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            _context.RefreshTokens.Add(refreshTokenEntity);

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 30 * 60
            });
        }
    }

    // Request DTOs
    public class RegisterUsernameRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class RegisterPhoneRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class VerifyOtpRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
    }

    public class LoginUsernameRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginPhoneRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
    }
}