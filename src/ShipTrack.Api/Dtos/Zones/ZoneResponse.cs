using ShipTrack.Api.Dtos.PricingRules;

namespace ShipTrack.Api.Dtos.Zones;

public class ZoneResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public PricingRuleResponse? PricingRule { get; set; }
}