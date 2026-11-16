using ShipTrack.Api.Domain.Entities;
using ShipTrack.Api.Services;
using Xunit;

namespace ShipTrack.Tests.Services;

public class ExpressFeeCalculatorTests
{
    private readonly ExpressFeeCalculator _calculator = new(new StandardFeeCalculator());

    [Fact]
    public void Calculate_ExpressOverWeight_ReturnsStandardFeeTimesExpressMultiplier()
    {
        // R5: Express = Standard × ExpressMultiplier.
        // Standard(2 kg) = 20000 + 1 * 5000 = 25000 -> Express = 25000 * 1.5 = 37500.
        var rule = new PricingRule { BaseFee = 20000m, ExtraFeePerKg = 5000m, ExpressMultiplier = 1.5m };

        var fee = _calculator.Calculate(rule, 2m);

        Assert.Equal(37500m, fee);
    }

    [Fact]
    public void Calculate_ExpressWithinFirstKilogram_AppliesMultiplierToBaseFee()
    {
        // R5: BaseFee is multiplied since the weight stays within the first kg.
        var rule = new PricingRule { BaseFee = 20000m, ExtraFeePerKg = 5000m, ExpressMultiplier = 1.5m };

        var fee = _calculator.Calculate(rule, 1m);

        Assert.Equal(30000m, fee);
    }

    [Fact]
    public void Calculate_FractionalMultiplierResult_RoundsToTwoDecimals()
    {
        // R5: the express fee is rounded to 2 decimals.
        var rule = new PricingRule { BaseFee = 100m, ExtraFeePerKg = 0m, ExpressMultiplier = 1.333m };

        var fee = _calculator.Calculate(rule, 1m);

        Assert.Equal(133.30m, fee);
    }
}