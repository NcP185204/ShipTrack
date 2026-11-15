using ShipTrack.Api.Dtos.PricingRules;

namespace ShipTrack.Api.Services;

public interface IPricingRuleService
{
    Task<List<PricingRuleResponse>> GetAllAsync(CancellationToken ct = default);

    Task<PricingRuleResponse> GetByIdAsync(int id, CancellationToken ct = default);

    Task<PricingRuleResponse> CreateAsync(PricingRuleRequest request, CancellationToken ct = default);

    Task<PricingRuleResponse> UpdateAsync(int id, PricingRuleRequest request, CancellationToken ct = default);
}