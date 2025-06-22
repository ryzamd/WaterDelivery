namespace WaterDelivery.Domain.Entities
{
    public class OtpVerification
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string OtpCode { get; set; } = string.Empty;
        public OtpPurpose Purpose { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; }
        public int AttemptCount { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public virtual User? User { get; set; }
    }

    public enum OtpPurpose
    {
        Register = 0,
        Login = 1,
        EmailVerification = 2
    }
}