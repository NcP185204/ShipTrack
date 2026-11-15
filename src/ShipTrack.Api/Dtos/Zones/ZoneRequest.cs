using System.ComponentModel.DataAnnotations;

namespace ShipTrack.Api.Dtos.Zones;

public class ZoneRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}