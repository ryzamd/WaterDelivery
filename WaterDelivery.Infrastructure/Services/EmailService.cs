using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using WaterDelivery.Application.Interfaces;
using WaterDelivery.Infrastructure.TemplatesEmail;

namespace WaterDelivery.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPass;
        private readonly string _fromEmail;

        public EmailService(IConfiguration configuration)
        {
            _smtpHost = configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
            _smtpPort = int.Parse(configuration["Email:SmtpPort"] ?? "587");
            _smtpUser = configuration["Email:SmtpUser"] ?? throw new ArgumentNullException("Email:SmtpUser");
            _smtpPass = configuration["Email:SmtpPassword"] ?? throw new ArgumentNullException("Email:SmtpPassword");
            _fromEmail = configuration["Email:FromEmail"] ?? _smtpUser;
        }

        public async Task<bool> SendVerificationEmailAsync(string email, string otp)
        {
            var subject = "WaterDelivery - Xác thực tài khoản";
            var body = TemplateEmailVerify.GetEmailTemplate(otp);

            return await SendEmailAsync(email, subject, body);
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("WaterDelivery", _fromEmail));
                message.To.Add(new MailboxAddress("", to));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = body };

                using var client = new SmtpClient();
                await client.ConnectAsync(_smtpHost, _smtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_smtpUser, _smtpPass);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email Error: {ex.Message}");
                return false;
            }
        }
    }
}