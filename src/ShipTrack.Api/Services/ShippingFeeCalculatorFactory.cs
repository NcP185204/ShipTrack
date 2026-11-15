using ShipTrack.Api.Domain.Enums;

namespace ShipTrack.Api.Services;

public class ShippingFeeCalculatorFactory : IShippingFeeCalculatorFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ShippingFeeCalculatorFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IShippingFeeCalculator GetCalculator(ServiceType serviceType)
    {
        // Open/Closed: adding a new service type only requires a new calculator
        // and a new entry in this resolver.
        return serviceType switch
        {
            ServiceType.Standard => _serviceProvider.GetRequiredService<StandardFeeCalculator>(),
            ServiceType.Express => _serviceProvider.GetRequiredService<ExpressFeeCalculator>(),
            _ => throw new ArgumentOutOfRangeException(nameof(serviceType), serviceType, "Unsupported service type.")
        };
    }
}