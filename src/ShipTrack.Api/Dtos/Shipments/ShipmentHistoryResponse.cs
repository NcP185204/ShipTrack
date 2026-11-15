using ShipTrack.Api.Domain.Enums;

namespace ShipTrack.Api.Dtos.Shipments;

public class ShipmentHistoryResponse
{
    public ShipmentStatus FromStatus { get; set; }

    public ShipmentStatus ToStatus { get; set; }

    public DateTime ChangedAt { get; set; }

    public string? Note { get; set; }
}