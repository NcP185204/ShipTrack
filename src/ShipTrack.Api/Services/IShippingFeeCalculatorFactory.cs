using ShipTrack.Api.Domain.Enums;

namespace ShipTrack.Api.Services;

public interface IShippingFeeCalculatorFactory
{
    IShippingFeeCalculator GetCalculator(ServiceType serviceType);
}