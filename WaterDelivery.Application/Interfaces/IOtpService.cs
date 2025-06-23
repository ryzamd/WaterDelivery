using WaterDelivery.Domain.Entities;

namespace WaterDelivery.Application.Interfaces
{
    public interface IOtpService
    {
        Task<string> GenerateOtpAsync(string target, OtpPurpose purpose, Guid? userId = null);
        Task<bool> ValidateOtpAsync(string target, string otp, OtpPurpose purpose);
        Task<bool> CanSendOtpAsync(string target, OtpPurpose purpose);
        Task<int> GetRemainingTimeAsync(string target, OtpPurpose purpose);
    }
}