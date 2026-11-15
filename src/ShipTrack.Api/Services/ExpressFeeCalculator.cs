using ShipTrack.Api.Domain.Entities;

namespace ShipTrack.Api.Services;

public class ExpressFeeCalculator : IShippingFeeCalculator
{
    private readonly StandardFeeCalculator _standardFeeCalculator;

    public ExpressFeeCalculator(StandardFeeCalculator standardFeeCalculator)
    {
        _standardFeeCalculator = standardFeeCalculator;
    }

    public decimal Calculate(PricingRule rule, decimal weightKg)
    {
        // R5: Express = Standard fee × ExpressMultiplier.
        var standardFee = _standardFeeCalculator.Calculate(rule, weightKg);
        return decimal.Round(standardFee * rule.ExpressMultiplier, 2);
    }
}