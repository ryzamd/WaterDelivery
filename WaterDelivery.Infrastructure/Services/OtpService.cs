using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using WaterDelivery.Application.Interfaces;
using WaterDelivery.Domain.Entities;
using WaterDelivery.Infrastructure.Data;

namespace WaterDelivery.Infrastructure.Services
{
    public class OtpService : IOtpService
    {
        private readonly ApplicationDbContext _context;
        private const int OtpExpiryMinutes = 3;
        private const int MaxAttempts = 3;
        private const int ResendCooldownSeconds = 60;

        public OtpService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateOtpAsync(string target, OtpPurpose purpose, Guid? userId = null)
        {
            if (!await CanSendOtpAsync(target, purpose))
                throw new InvalidOperationException("Too many OTP requests. Please wait.");

            var otp = GenerateRandomOtp();

            // Invalidate previous OTPs
            var existingOtps = await _context.OtpVerifications
                .Where(x => (x.PhoneNumber == target || x.Email == target) &&
                           x.Purpose == purpose && !x.IsUsed)
                .ToListAsync();

            foreach (var existingOtp in existingOtps)
                existingOtp.IsUsed = true;

            // Create new OTP
            _context.OtpVerifications.Add(new OtpVerification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PhoneNumber = IsPhoneNumber(target) ? target : null,
                Email = IsEmail(target) ? target : null,
                OtpCode = otp,
                Purpose = purpose,
                ExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes),
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return otp;
        }

        public async Task<bool> ValidateOtpAsync(string target, string otp, OtpPurpose purpose)
        {
            var otpRecord = await _context.OtpVerifications
                .Where(x => (x.PhoneNumber == target || x.Email == target) &&
                           x.Purpose == purpose && !x.IsUsed &&
                           x.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (otpRecord == null) return false;

            otpRecord.AttemptCount++;

            if (otpRecord.AttemptCount > MaxAttempts)
            {
                otpRecord.IsUsed = true;
                await _context.SaveChangesAsync();
                return false;
            }

            if (otpRecord.OtpCode == otp)
            {
                otpRecord.IsUsed = true;
                await _context.SaveChangesAsync();
                return true;
            }

            await _context.SaveChangesAsync();
            return false;
        }

        public async Task<bool> CanSendOtpAsync(string target, OtpPurpose purpose)
        {
            var lastOtp = await _context.OtpVerifications
                .Where(x => (x.PhoneNumber == target || x.Email == target) && x.Purpose == purpose)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (lastOtp == null) return true;

            var cooldownEnd = lastOtp.CreatedAt.AddSeconds(ResendCooldownSeconds);
            return DateTime.UtcNow > cooldownEnd;
        }

        public async Task<int> GetRemainingTimeAsync(string target, OtpPurpose purpose)
        {
            var lastOtp = await _context.OtpVerifications
                .Where(x => (x.PhoneNumber == target || x.Email == target) && x.Purpose == purpose)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (lastOtp == null) return 0;

            var remaining = (int)(lastOtp.CreatedAt.AddSeconds(ResendCooldownSeconds) - DateTime.UtcNow).TotalSeconds;
            return Math.Max(0, remaining);
        }

        private static string GenerateRandomOtp()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var randomNumber = Math.Abs(BitConverter.ToInt32(bytes, 0));
            return (randomNumber % 1000000).ToString("D6");
        }

        private static bool IsPhoneNumber(string input) => input.StartsWith("+") || input.All(char.IsDigit);
        private static bool IsEmail(string input) => input.Contains("@") && input.Contains(".");
    }
}