using Microsoft.Extensions.Configuration;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using WaterDelivery.Application.Interfaces;

namespace WaterDelivery.Infrastructure.Services
{
    public class TwilioSmsService : ISmsService
    {
        private readonly string _accountSid;
        private readonly string _authToken;
        private readonly string _fromPhoneNumber;

        public TwilioSmsService(IConfiguration configuration)
        {
            _accountSid = configuration["Twilio:AccountSid"] ?? throw new ArgumentNullException("Twilio:AccountSid");
            _authToken = configuration["Twilio:AuthToken"] ?? throw new ArgumentNullException("Twilio:AuthToken");
            _fromPhoneNumber = configuration["Twilio:FromPhoneNumber"] ?? throw new ArgumentNullException("Twilio:FromPhoneNumber");

            TwilioClient.Init(_accountSid, _authToken);
        }

        public async Task<bool> SendOtpAsync(string phoneNumber, string otp)
        {
            var message = $"Your WaterDelivery verification code is: {otp}. Valid for 3 minutes.";
            return await SendMessageAsync(phoneNumber, message);
        }

        public async Task<bool> SendMessageAsync(string phoneNumber, string message)
        {
            try
            {
                var messageResource = await MessageResource.CreateAsync(
                    body: message,
                    from: new Twilio.Types.PhoneNumber(_fromPhoneNumber),
                    to: new Twilio.Types.PhoneNumber(FormatPhoneNumber(phoneNumber))
                );

                return messageResource.ErrorCode == null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SMS Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        private static string FormatPhoneNumber(string phoneNumber)
        {
            // Ensure phone number starts with +
            if (!phoneNumber.StartsWith("+"))
            {
                // Default to Vietnam if no country code
                if (phoneNumber.StartsWith("0"))
                    phoneNumber = phoneNumber.Substring(1);
                phoneNumber = "+84" + phoneNumber;
            }
            return phoneNumber;
        }
    }
}
