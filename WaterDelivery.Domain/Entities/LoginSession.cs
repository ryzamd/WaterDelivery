namespace WaterDelivery.Domain.Entities
{
    public class LoginSession
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string JwtTokenId { get; set; } = string.Empty;
        public string? DeviceInfo { get; set; }
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual User User { get; set; } = null!;
    }
}