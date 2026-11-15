using ShipTrack.Api.Domain.Entities;
using ShipTrack.Api.Domain.Exceptions;
using ShipTrack.Api.Dtos.PricingRules;
using ShipTrack.Api.Repositories;

namespace ShipTrack.Api.Services;

public class PricingRuleService : IPricingRuleService
{
    private readonly IPricingRuleRepository _pricingRuleRepository;
    private readonly IZoneRepository _zoneRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PricingRuleService(
        IPricingRuleRepository pricingRuleRepository,
        IZoneRepository zoneRepository,
        IUnitOfWork unitOfWork)
    {
        _pricingRuleRepository = pricingRuleRepository;
        _zoneRepository = zoneRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<PricingRuleResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var rules = await _pricingRuleRepository.GetAllAsync(ct);
        return rules.Select(ToResponse).ToList();
    }

    public async Task<PricingRuleResponse> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var rule = await _pricingRuleRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Pricing rule {id} not found.");

        return ToResponse(rule);
    }

    public async Task<PricingRuleResponse> CreateAsync(PricingRuleRequest request, CancellationToken ct = default)
    {
        var zone = await _zoneRepository.GetByIdAsync(request.ZoneId, ct)
            ?? throw new NotFoundException($"Zone {request.ZoneId} not found.");

        // R13: one PricingRule per Zone (ZoneId unique).
        var existing = await _pricingRuleRepository.GetByZoneIdAsync(request.ZoneId, ct);
        if (existing is not null)
        {
            throw new BusinessRuleException($"Zone '{zone.Name}' already has a pricing rule.");
        }

        var rule = new PricingRule
        {
            ZoneId = request.ZoneId,
            BaseFee = request.BaseFee,
            ExtraFeePerKg = request.ExtraFeePerKg,
            ExpressMultiplier = request.ExpressMultiplier
        };

        _pricingRuleRepository.Add(rule);
        await _unitOfWork.SaveChangesAsync(ct);

        rule.Zone = zone;
        return ToResponse(rule);
    }

    public async Task<PricingRuleResponse> UpdateAsync(int id, PricingRuleRequest request, CancellationToken ct = default)
    {
        var rule = await _pricingRuleRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Pricing rule {id} not found.");

        var zone = await _zoneRepository.GetByIdAsync(request.ZoneId, ct)
            ?? throw new NotFoundException($"Zone {request.ZoneId} not found.");

        // R13: one PricingRule per Zone (ZoneId unique, excluding itself).
        var existing = await _pricingRuleRepository.GetByZoneIdAsync(request.ZoneId, ct);
        if (existing is not null && existing.Id != id)
        {
            throw new BusinessRuleException($"Zone '{zone.Name}' already has a pricing rule.");
        }

        rule.ZoneId = request.ZoneId;
        rule.BaseFee = request.BaseFee;
        rule.ExtraFeePerKg = request.ExtraFeePerKg;
        rule.ExpressMultiplier = request.ExpressMultiplier;

        _pricingRuleRepository.Update(rule);
        await _unitOfWork.SaveChangesAsync(ct);

        rule.Zone = zone;
        return ToResponse(rule);
    }

    private static PricingRuleResponse ToResponse(PricingRule rule)
    {
        return new PricingRuleResponse
        {
            Id = rule.Id,
            ZoneId = rule.ZoneId,
            ZoneName = rule.Zone?.Name ?? string.Empty,
            BaseFee = rule.BaseFee,
            ExtraFeePerKg = rule.ExtraFeePerKg,
            ExpressMultiplier = rule.ExpressMultiplier
        };
    }
}