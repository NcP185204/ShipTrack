using ShipTrack.Api.Domain.Enums;

namespace ShipTrack.Api.Dtos.Tracking;

public class TrackingHistoryResponse
{
    public ShipmentStatus FromStatus { get; set; }

    public ShipmentStatus ToStatus { get; set; }

    public DateTime ChangedAt { get; set; }

    public string? Note { get; set; }
}