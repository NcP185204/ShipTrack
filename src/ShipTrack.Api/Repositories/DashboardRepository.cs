using Microsoft.EntityFrameworkCore;
using ShipTrack.Api.Data;
using ShipTrack.Api.Domain.Enums;
using ShipTrack.Api.Dtos.Dashboard;

namespace ShipTrack.Api.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly ApplicationDbContext _db;

    public DashboardRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken ct = default)
    {
        // Counts by status are computed in the database with a single GroupBy.
        var statusCounts = await _db.Shipments
            .AsNoTracking()
            .GroupBy(s => s.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, ct);

        // Revenue is the sum of ShippingFee for Delivered shipments only.
        var revenue = await _db.Shipments
            .AsNoTracking()
            .Where(s => s.Status == ShipmentStatus.Delivered)
            .SumAsync(s => s.ShippingFee, ct);

        var delivered = statusCounts.GetValueOrDefault(ShipmentStatus.Delivered);
        var returned = statusCounts.GetValueOrDefault(ShipmentStatus.Returned);

        // Success rate = Delivered / (Delivered + Returned); 0 when the denominator is 0.
        var successRate = delivered + returned == 0
            ? 0m
            : decimal.Round((decimal)delivered / (delivered + returned), 4);

        // Top 5 shippers by Delivered count, joined to users for the shipper name.
        var topShippers = await (
            from s in _db.Shipments.AsNoTracking()
            join u in _db.Users.AsNoTracking() on s.ShipperId equals u.Id
            where s.Status == ShipmentStatus.Delivered
            group s by new { s.ShipperId, u.FullName } into g
            orderby g.Count() descending
            select new TopShipperResponse
            {
                ShipperId = g.Key.ShipperId!,
                FullName = g.Key.FullName,
                DeliveredCount = g.Count()
            })
            .Take(5)
            .ToListAsync(ct);

        return new DashboardSummaryResponse
        {
            ShipmentsByStatus = statusCounts,
            Revenue = revenue,
            SuccessRate = successRate,
            TopShippers = topShippers
        };
    }
}