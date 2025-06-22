using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using WaterDelivery.Domain.Entities;

namespace WaterDelivery.Infrastructure.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasMaxLength(36)
                .IsRequired();

            builder.Property(x => x.Token)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .HasConversion(
                    v => v.ToUniversalTime(),
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
                .ValueGeneratedOnAdd();

            // Indexes
            builder.HasIndex(x => new { x.UserId, x.IsRevoked });
            builder.HasIndex(x => x.Token);
            builder.HasIndex(x => x.ExpiresAt);
        }
    }
}
