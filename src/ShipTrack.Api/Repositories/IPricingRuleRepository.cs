using ShipTrack.Api.Domain.Entities;

namespace ShipTrack.Api.Repositories;

public interface IPricingRuleRepository
{
    Task<List<PricingRule>> GetAllAsync(CancellationToken ct = default);

    Task<PricingRule?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<PricingRule?> GetByZoneIdAsync(int zoneId, CancellationToken ct = default);

    void Add(PricingRule pricingRule);

    void Update(PricingRule pricingRule);

    void Delete(PricingRule pricingRule);
}