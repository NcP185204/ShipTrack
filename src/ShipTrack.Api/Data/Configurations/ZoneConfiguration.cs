using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShipTrack.Api.Domain.Entities;

namespace ShipTrack.Api.Data.Configurations;

public class ZoneConfiguration : IEntityTypeConfiguration<Zone>
{
    public void Configure(EntityTypeBuilder<Zone> builder)
    {
        builder.Property(z => z.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(z => z.Name)
            .IsUnique();

        builder.Property(z => z.Description)
            .HasMaxLength(500);
    }
}