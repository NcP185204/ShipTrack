using System.ComponentModel.DataAnnotations;

namespace ShipTrack.Api.Dtos.PricingRules;

public class PricingRuleRequest
{
    [Required]
    public int ZoneId { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal BaseFee { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal ExtraFeePerKg { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal ExpressMultiplier { get; set; }
}