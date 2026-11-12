namespace ShipTrack.Api.Domain.Entities;

public class PricingRule
{
    public int Id { get; set; }

    public int ZoneId { get; set; }

    public Zone Zone { get; set; } = null!;

    public decimal BaseFee { get; set; }

    public decimal ExtraFeePerKg { get; set; }

    public decimal ExpressMultiplier { get; set; }
}