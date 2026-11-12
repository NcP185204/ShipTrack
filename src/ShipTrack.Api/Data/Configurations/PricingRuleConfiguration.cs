using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShipTrack.Api.Domain.Entities;

namespace ShipTrack.Api.Data.Configurations;

public class PricingRuleConfiguration : IEntityTypeConfiguration<PricingRule>
{
    public void Configure(EntityTypeBuilder<PricingRule> builder)
    {
        builder.Property(p => p.BaseFee)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(p => p.ExtraFeePerKg)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(p => p.ExpressMultiplier)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasIndex(p => p.ZoneId)
            .IsUnique();

        builder.HasOne(p => p.Zone)
            .WithOne(z => z.PricingRule)
            .HasForeignKey<PricingRule>(p => p.ZoneId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}