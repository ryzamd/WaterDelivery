namespace WaterDelivery.Application.Interfaces
{
    public interface ISmsService
    {
        Task<bool> SendOtpAsync(string phoneNumber, string otp);
        Task<bool> SendMessageAsync(string phoneNumber, string message);
    }
}