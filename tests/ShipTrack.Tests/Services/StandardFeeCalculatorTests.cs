using ShipTrack.Api.Domain.Entities;
using ShipTrack.Api.Services;
using Xunit;

namespace ShipTrack.Tests.Services;

public class StandardFeeCalculatorTests
{
    private readonly StandardFeeCalculator _calculator = new();

    private static PricingRule Rule(decimal baseFee, decimal extraFeePerKg) =>
        new() { BaseFee = baseFee, ExtraFeePerKg = extraFeePerKg };

    [Fact]
    public void Calculate_WeightWithinFirstKilogram_ReturnsBaseFee()
    {
        // R4: the first kg is included in BaseFee.
        var fee = _calculator.Calculate(Rule(20000m, 5000m), 1m);

        Assert.Equal(20000m, fee);
    }

    [Fact]
    public void Calculate_WeightBelowFirstKilogram_ReturnsBaseFee()
    {
        // R4: max(0, weight - 1) is 0 for weights below 1 kg.
        var fee = _calculator.Calculate(Rule(20000m, 5000m), 0.5m);

        Assert.Equal(20000m, fee);
    }

    [Fact]
    public void Calculate_WeightAboveFirstKilogram_AddsExtraFeePerExtraKilogram()
    {
        // R4: 4 kg -> BaseFee + (4 - 1) * ExtraFeePerKg.
        var fee = _calculator.Calculate(Rule(20000m, 5000m), 4m);

        Assert.Equal(35000m, fee);
    }

    [Fact]
    public void Calculate_FractionalExtraKilogram_RoundsUpToFullKilogram()
    {
        // R4: ceil(2.5 - 1) = 2 extra kg -> BaseFee + 2 * ExtraFeePerKg.
        var fee = _calculator.Calculate(Rule(20000m, 5000m), 2.5m);

        Assert.Equal(30000m, fee);
    }

    [Fact]
    public void Calculate_ResultWithMoreThanTwoDecimals_RoundsToTwoDecimals()
    {
        // R4: the result is rounded to 2 decimals.
        var fee = _calculator.Calculate(Rule(100m, 33.334m), 2m);

        Assert.Equal(133.33m, fee);
    }
}