namespace ShipTrack.Api.Repositories;

public class ShipmentOwnership
{
    public string CustomerId { get; set; } = string.Empty;

    public string? ShipperId { get; set; }
}