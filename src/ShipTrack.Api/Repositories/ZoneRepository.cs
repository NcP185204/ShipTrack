using Microsoft.EntityFrameworkCore;
using ShipTrack.Api.Data;
using ShipTrack.Api.Domain.Entities;

namespace ShipTrack.Api.Repositories;

public class ZoneRepository : IZoneRepository
{
    private readonly ApplicationDbContext _db;

    public ZoneRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<List<Zone>> GetAllAsync(CancellationToken ct = default)
    {
        return _db.Zones
            .AsNoTracking()
            .Include(z => z.PricingRule)
            .OrderBy(z => z.Name)
            .ToListAsync(ct);
    }

    public Task<Zone?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return _db.Zones
            .Include(z => z.PricingRule)
            .FirstOrDefaultAsync(z => z.Id == id, ct);
    }

    public Task<Zone?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        return _db.Zones
            .FirstOrDefaultAsync(z => z.Name == name, ct);
    }

    public Task<bool> HasShipmentsAsync(int id, CancellationToken ct = default)
    {
        return _db.Shipments.AnyAsync(s => s.ZoneId == id, ct);
    }

    public void Add(Zone zone)
    {
        _db.Zones.Add(zone);
    }

    public void Update(Zone zone)
    {
        _db.Zones.Update(zone);
    }

    public void Delete(Zone zone)
    {
        _db.Zones.Remove(zone);
    }
}