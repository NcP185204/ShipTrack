using ShipTrack.Api.Domain.Enums;

namespace ShipTrack.Api.Domain.Entities;

public class Shipment
{
    public int Id { get; set; }

    public string TrackingCode { get; set; } = string.Empty;

    public string CustomerId { get; set; } = string.Empty;

    public ApplicationUser Customer { get; set; } = null!;

    public string? ShipperId { get; set; }

    public ApplicationUser? Shipper { get; set; }

    public string SenderName { get; set; } = string.Empty;

    public string SenderPhone { get; set; } = string.Empty;

    public string PickupAddress { get; set; } = string.Empty;

    public string ReceiverName { get; set; } = string.Empty;

    public string ReceiverPhone { get; set; } = string.Empty;

    public string DeliveryAddress { get; set; } = string.Empty;

    public int ZoneId { get; set; }

    public Zone Zone { get; set; } = null!;

    public ServiceType ServiceType { get; set; }

    public decimal WeightKg { get; set; }

    public decimal ShippingFee { get; set; }

    public ShipmentStatus Status { get; set; }

    public int FailedAttempts { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<ShipmentStatusHistory> StatusHistory { get; set; } = new List<ShipmentStatusHistory>();
}