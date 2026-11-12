using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShipTrack.Api.Domain.Entities;

namespace ShipTrack.Api.Data.Configurations;

public class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.Property(s => s.TrackingCode)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(s => s.TrackingCode)
            .IsUnique();

        builder.Property(s => s.SenderName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.SenderPhone)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.PickupAddress)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(s => s.ReceiverName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.ReceiverPhone)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.DeliveryAddress)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(s => s.WeightKg)
            .HasPrecision(6, 2)
            .IsRequired();

        builder.Property(s => s.ShippingFee)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(s => s.Note)
            .HasMaxLength(1000);

        builder.Property(s => s.RowVersion)
            .IsRowVersion();

        builder.HasIndex(s => new { s.Status, s.CreatedAt });

        builder.HasIndex(s => new { s.ShipperId, s.Status });

        builder.HasOne(s => s.Customer)
            .WithMany()
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Shipper)
            .WithMany()
            .HasForeignKey(s => s.ShipperId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Zone)
            .WithMany(z => z.Shipments)
            .HasForeignKey(s => s.ZoneId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}