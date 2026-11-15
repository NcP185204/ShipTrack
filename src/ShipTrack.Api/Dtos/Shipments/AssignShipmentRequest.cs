using System.ComponentModel.DataAnnotations;

namespace ShipTrack.Api.Dtos.Shipments;

public class AssignShipmentRequest
{
    [Required]
    public string ShipperId { get; set; } = string.Empty;
}