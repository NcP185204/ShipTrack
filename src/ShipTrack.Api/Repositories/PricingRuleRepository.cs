using Microsoft.EntityFrameworkCore;
using ShipTrack.Api.Data;
using ShipTrack.Api.Domain.Entities;

namespace ShipTrack.Api.Repositories;

public class PricingRuleRepository : IPricingRuleRepository
{
    private readonly ApplicationDbContext _db;

    public PricingRuleRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<List<PricingRule>> GetAllAsync(CancellationToken ct = default)
    {
        return _db.PricingRules
            .AsNoTracking()
            .Include(p => p.Zone)
            .OrderBy(p => p.Zone.Name)
            .ToListAsync(ct);
    }

    public Task<PricingRule?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return _db.PricingRules
            .Include(p => p.Zone)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public Task<PricingRule?> GetByZoneIdAsync(int zoneId, CancellationToken ct = default)
    {
        return _db.PricingRules
            .FirstOrDefaultAsync(p => p.ZoneId == zoneId, ct);
    }

    public void Add(PricingRule pricingRule)
    {
        _db.PricingRules.Add(pricingRule);
    }

    public void Update(PricingRule pricingRule)
    {
        _db.PricingRules.Update(pricingRule);
    }

    public void Delete(PricingRule pricingRule)
    {
        _db.PricingRules.Remove(pricingRule);
    }
}