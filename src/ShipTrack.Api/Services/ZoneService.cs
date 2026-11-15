using ShipTrack.Api.Domain.Entities;
using ShipTrack.Api.Domain.Exceptions;
using ShipTrack.Api.Dtos.PricingRules;
using ShipTrack.Api.Dtos.Zones;
using ShipTrack.Api.Repositories;

namespace ShipTrack.Api.Services;

public class ZoneService : IZoneService
{
    private readonly IZoneRepository _zoneRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ZoneService(IZoneRepository zoneRepository, IUnitOfWork unitOfWork)
    {
        _zoneRepository = zoneRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ZoneResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var zones = await _zoneRepository.GetAllAsync(ct);
        return zones.Select(ToResponse).ToList();
    }

    public async Task<ZoneResponse> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var zone = await _zoneRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Zone {id} not found.");

        return ToResponse(zone);
    }

    public async Task<ZoneResponse> CreateAsync(ZoneRequest request, CancellationToken ct = default)
    {
        // R13: Zone.Name must be unique.
        var existing = await _zoneRepository.GetByNameAsync(request.Name, ct);
        if (existing is not null)
        {
            throw new BusinessRuleException($"Zone name '{request.Name}' already exists.");
        }

        var zone = new Zone
        {
            Name = request.Name,
            Description = request.Description
        };

        _zoneRepository.Add(zone);
        await _unitOfWork.SaveChangesAsync(ct);

        return ToResponse(zone);
    }

    public async Task<ZoneResponse> UpdateAsync(int id, ZoneRequest request, CancellationToken ct = default)
    {
        var zone = await _zoneRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Zone {id} not found.");

        // R13: Zone.Name must be unique (excluding itself).
        var existing = await _zoneRepository.GetByNameAsync(request.Name, ct);
        if (existing is not null && existing.Id != id)
        {
            throw new BusinessRuleException($"Zone name '{request.Name}' already exists.");
        }

        zone.Name = request.Name;
        zone.Description = request.Description;

        _zoneRepository.Update(zone);
        await _unitOfWork.SaveChangesAsync(ct);

        return ToResponse(zone);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var zone = await _zoneRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Zone {id} not found.");

        // R13: a Zone that has shipments cannot be deleted.
        if (await _zoneRepository.HasShipmentsAsync(id, ct))
        {
            throw new BusinessRuleException($"Zone '{zone.Name}' has shipments and cannot be deleted.");
        }

        // If the Zone has a PricingRule, it is deleted with the Zone (cascade).
        _zoneRepository.Delete(zone);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static ZoneResponse ToResponse(Zone zone)
    {
        return new ZoneResponse
        {
            Id = zone.Id,
            Name = zone.Name,
            Description = zone.Description,
            PricingRule = zone.PricingRule is null
                ? null
                : new PricingRuleResponse
                {
                    Id = zone.PricingRule.Id,
                    ZoneId = zone.PricingRule.ZoneId,
                    ZoneName = zone.Name,
                    BaseFee = zone.PricingRule.BaseFee,
                    ExtraFeePerKg = zone.PricingRule.ExtraFeePerKg,
                    ExpressMultiplier = zone.PricingRule.ExpressMultiplier
                }
        };
    }
}