using System.ComponentModel.DataAnnotations;
using ShipTrack.Api.Domain.Enums;

namespace ShipTrack.Api.Dtos.Shipments;

public class UpdateStatusRequest
{
    [Required]
    [EnumDataType(typeof(ShipmentStatus))]
    public ShipmentStatus NewStatus { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }
}