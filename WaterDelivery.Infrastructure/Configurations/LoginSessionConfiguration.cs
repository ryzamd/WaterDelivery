using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using WaterDelivery.Domain.Entities;

namespace WaterDelivery.Infrastructure.Configurations
{
    public class LoginSessionConfiguration : IEntityTypeConfiguration<LoginSession>
    {
        public void Configure(EntityTypeBuilder<LoginSession> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasMaxLength(36)
                .IsRequired();

            builder.Property(x => x.JwtTokenId)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(x => x.DeviceInfo)
                .HasMaxLength(500);

            builder.Property(x => x.IpAddress)
                .HasMaxLength(45);

            builder.Property(x => x.CreatedAt)
                .HasConversion(
                    v => v.ToUniversalTime(),
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
                .ValueGeneratedOnAdd();

            // Indexes  
            builder.HasIndex(x => new { x.UserId, x.IsActive });
            builder.HasIndex(x => x.JwtTokenId);
        }
    }
}
