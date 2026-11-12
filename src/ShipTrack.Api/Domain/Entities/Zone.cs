namespace ShipTrack.Api.Domain.Entities;

public class Zone
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public PricingRule? PricingRule { get; set; }

    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}