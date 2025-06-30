namespace WaterDelivery.Application.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendVerificationEmailAsync(string email, string otp);
        Task<bool> SendEmailAsync(string to, string subject, string body);
    }
}