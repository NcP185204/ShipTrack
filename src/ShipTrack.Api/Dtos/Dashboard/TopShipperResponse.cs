namespace ShipTrack.Api.Dtos.Dashboard;

public class TopShipperResponse
{
    public string ShipperId { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public int DeliveredCount { get; set; }
}