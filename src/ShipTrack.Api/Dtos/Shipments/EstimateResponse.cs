using ShipTrack.Api.Domain.Enums;

namespace ShipTrack.Api.Dtos.Shipments;

public class EstimateResponse
{
    public int ZoneId { get; set; }

    public decimal WeightKg { get; set; }

    public ServiceType ServiceType { get; set; }

    public decimal EstimatedFee { get; set; }
}