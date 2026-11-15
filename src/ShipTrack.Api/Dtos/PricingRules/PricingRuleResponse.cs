namespace ShipTrack.Api.Dtos.PricingRules;

public class PricingRuleResponse
{
    public int Id { get; set; }

    public int ZoneId { get; set; }

    public string ZoneName { get; set; } = string.Empty;

    public decimal BaseFee { get; set; }

    public decimal ExtraFeePerKg { get; set; }

    public decimal ExpressMultiplier { get; set; }
}