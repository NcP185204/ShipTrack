using ShipTrack.Api.Domain.Enums;

namespace ShipTrack.Api.Dtos.Tracking;

public class TrackingResponse
{
    public string TrackingCode { get; set; } = string.Empty;

    public ShipmentStatus Status { get; set; }

    public string ReceiverName { get; set; } = string.Empty;

    public List<TrackingHistoryResponse> History { get; set; } = new();
}