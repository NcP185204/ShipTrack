using ShipTrack.Api.Domain.Enums;

namespace ShipTrack.Api.Domain.Entities;

public class ShipmentStatusHistory
{
    public int Id { get; set; }

    public int ShipmentId { get; set; }

    public Shipment Shipment { get; set; } = null!;

    public ShipmentStatus FromStatus { get; set; }

    public ShipmentStatus ToStatus { get; set; }

    public string? ChangedByUserId { get; set; }

    public ApplicationUser? ChangedByUser { get; set; }

    public DateTime ChangedAt { get; set; }

    public string? Note { get; set; }
}