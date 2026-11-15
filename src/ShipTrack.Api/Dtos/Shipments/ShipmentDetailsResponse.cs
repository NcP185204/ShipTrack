using ShipTrack.Api.Domain.Enums;

namespace ShipTrack.Api.Dtos.Shipments;

public class ShipmentDetailsResponse
{
    public int Id { get; set; }

    public string TrackingCode { get; set; } = string.Empty;

    public string SenderName { get; set; } = string.Empty;

    public string SenderPhone { get; set; } = string.Empty;

    public string PickupAddress { get; set; } = string.Empty;

    public string ReceiverName { get; set; } = string.Empty;

    public string ReceiverPhone { get; set; } = string.Empty;

    public string DeliveryAddress { get; set; } = string.Empty;

    public int ZoneId { get; set; }

    public string ZoneName { get; set; } = string.Empty;

    public ServiceType ServiceType { get; set; }

    public decimal WeightKg { get; set; }

    public decimal ShippingFee { get; set; }

    public ShipmentStatus Status { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<ShipmentHistoryResponse> History { get; set; } = new();
}