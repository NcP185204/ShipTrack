using System.ComponentModel.DataAnnotations;
using ShipTrack.Api.Domain.Enums;

namespace ShipTrack.Api.Dtos.Shipments;

public class EstimateRequest
{
    [Required]
    public int ZoneId { get; set; }

    // R3: weight must be > 0 and <= 30 kg.
    [Required]
    [Range(0.01, 30)]
    public decimal WeightKg { get; set; }

    [Required]
    [EnumDataType(typeof(ServiceType))]
    public ServiceType ServiceType { get; set; }
}