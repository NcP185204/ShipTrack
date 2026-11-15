using ShipTrack.Api.Domain.Entities;

namespace ShipTrack.Api.Services;

public class StandardFeeCalculator : IShippingFeeCalculator
{
    public decimal Calculate(PricingRule rule, decimal weightKg)
    {
        // R4: Fee = BaseFee + ceil(max(0, weight - 1)) * ExtraFeePerKg.
        // The first kg is included in BaseFee.
        var extraKg = Math.Ceiling(Math.Max(0m, weightKg - 1m));
        return decimal.Round(rule.BaseFee + extraKg * rule.ExtraFeePerKg, 2);
    }
}