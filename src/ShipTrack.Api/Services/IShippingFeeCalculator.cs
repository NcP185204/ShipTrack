using ShipTrack.Api.Domain.Entities;

namespace ShipTrack.Api.Services;

public interface IShippingFeeCalculator
{
    decimal Calculate(PricingRule rule, decimal weightKg);
}