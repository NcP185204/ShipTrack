namespace ShipTrack.Api.Dtos.Shippers;

public class ShipperResponse
{
    public string Id { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public int ActiveShipmentCount { get; set; }
}