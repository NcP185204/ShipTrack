using Microsoft.EntityFrameworkCore;
using ShipTrack.Api.Data;
using ShipTrack.Api.Domain.Entities;
using ShipTrack.Api.Domain.Enums;

namespace ShipTrack.Api.Repositories;

public class ShipperRepository : IShipperRepository
{
    private static readonly ShipmentStatus[] ActiveStatuses =
    {
        ShipmentStatus.Assigned,
        ShipmentStatus.PickedUp,
        ShipmentStatus.InTransit
    };

    private readonly ApplicationDbContext _db;

    public ShipperRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<ApplicationUser?> GetShipperByIdAsync(string shipperId, CancellationToken ct = default)
    {
        return _db.Users
            .FirstOrDefaultAsync(u => u.Id == shipperId, ct);
    }

    public Task<int> GetActiveShipmentCountAsync(string shipperId, CancellationToken ct = default)
    {
        // R7: active shipments are Assigned, PickedUp, or InTransit.
        return _db.Shipments
            .CountAsync(s => s.ShipperId == shipperId && ActiveStatuses.Contains(s.Status), ct);
    }

    public async Task<List<ShipperSummary>> GetShippersWithActiveCountAsync(CancellationToken ct = default)
    {
        var shipperRoleId = await _db.Roles
            .Where(r => r.Name == "Shipper")
            .Select(r => r.Id)
            .FirstOrDefaultAsync(ct);

        if (shipperRoleId is null)
        {
            return new List<ShipperSummary>();
        }

        var shipperIds = await _db.UserRoles
            .Where(ur => ur.RoleId == shipperRoleId)
            .Select(ur => ur.UserId)
            .ToListAsync(ct);

        var users = await _db.Users
            .Where(u => shipperIds.Contains(u.Id))
            .ToListAsync(ct);

        var activeCounts = await _db.Shipments
            .Where(s => s.ShipperId != null && ActiveStatuses.Contains(s.Status))
            .GroupBy(s => s.ShipperId!)
            .Select(g => new { ShipperId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ShipperId, x => x.Count, ct);

        return users
            .Select(u => new ShipperSummary
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? string.Empty,
                IsActive = u.IsActive,
                ActiveShipmentCount = activeCounts.TryGetValue(u.Id, out var count) ? count : 0
            })
            .OrderBy(s => s.FullName)
            .ToList();
    }
}