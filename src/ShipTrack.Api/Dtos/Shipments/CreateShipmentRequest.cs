using System.ComponentModel.DataAnnotations;
using ShipTrack.Api.Domain.Enums;

namespace ShipTrack.Api.Dtos.Shipments;

public class CreateShipmentRequest
{
    [Required]
    [MaxLength(200)]
    public string SenderName { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^(0|\+84)\d{9}$", ErrorMessage = "Phone must match 0xxxxxxxxx or +84xxxxxxxxx.")]
    public string SenderPhone { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string PickupAddress { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string ReceiverName { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^(0|\+84)\d{9}$", ErrorMessage = "Phone must match 0xxxxxxxxx or +84xxxxxxxxx.")]
    public string ReceiverPhone { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string DeliveryAddress { get; set; } = string.Empty;

    [Required]
    public int ZoneId { get; set; }

    // R3: weight must be > 0 and <= 30 kg.
    [Required]
    [Range(0.01, 30)]
    public decimal WeightKg { get; set; }

    [Required]
    [EnumDataType(typeof(ServiceType))]
    public ServiceType ServiceType { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }
}