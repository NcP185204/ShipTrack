using Microsoft.EntityFrameworkCore;
using ShipTrack.Api.Data;
using ShipTrack.Api.Domain.Entities;
using ShipTrack.Api.Dtos.Common;
using ShipTrack.Api.Dtos.Shipments;
using ShipTrack.Api.Dtos.Tracking;

namespace ShipTrack.Api.Repositories;

public class ShipmentRepository : IShipmentRepository
{
    private readonly ApplicationDbContext _db;

    public ShipmentRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<Shipment?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return _db.Shipments
            .Include(s => s.Zone)
            .Include(s => s.StatusHistory)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public Task<Shipment?> GetByTrackingCodeAsync(string trackingCode, CancellationToken ct = default)
    {
        // R6: lookup is case-insensitive.
        return _db.Shipments
            .FirstOrDefaultAsync(s => s.TrackingCode.ToLower() == trackingCode.ToLower(), ct);
    }

    public Task<List<Shipment>> GetByShipperIdAsync(string shipperId, CancellationToken ct = default)
    {
        return _db.Shipments
            .AsNoTracking()
            .Include(s => s.Zone)
            .Where(s => s.ShipperId == shipperId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<PagedResult<ShipmentResponse>> GetPagedAsync(ShipmentQuery query, string? customerId, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        var source = _db.Shipments.AsNoTracking();

        if (customerId is not null)
        {
            source = source.Where(s => s.CustomerId == customerId);
        }

        if (query.Status.HasValue)
        {
            source = source.Where(s => s.Status == query.Status.Value);
        }

        if (query.FromDate.HasValue)
        {
            source = source.Where(s => s.CreatedAt >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            source = source.Where(s => s.CreatedAt <= query.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // Search matches tracking code by contains, case-insensitive.
            var search = query.Search.Trim();
            source = source.Where(s => s.TrackingCode.ToLower().Contains(search.ToLower()));
        }

        var totalItems = await source.CountAsync(ct);

        var items = await source
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new ShipmentResponse
            {
                Id = s.Id,
                TrackingCode = s.TrackingCode,
                SenderName = s.SenderName,
                SenderPhone = s.SenderPhone,
                PickupAddress = s.PickupAddress,
                ReceiverName = s.ReceiverName,
                ReceiverPhone = s.ReceiverPhone,
                DeliveryAddress = s.DeliveryAddress,
                ZoneId = s.ZoneId,
                ZoneName = s.Zone.Name,
                ServiceType = s.ServiceType,
                WeightKg = s.WeightKg,
                ShippingFee = s.ShippingFee,
                Status = s.Status,
                Note = s.Note,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync(ct);

        return new PagedResult<ShipmentResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
        };
    }

    public Task<ShipmentDetailsResponse?> GetDetailsAsync(int id, CancellationToken ct = default)
    {
        return _db.Shipments
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new ShipmentDetailsResponse
            {
                Id = s.Id,
                TrackingCode = s.TrackingCode,
                SenderName = s.SenderName,
                SenderPhone = s.SenderPhone,
                PickupAddress = s.PickupAddress,
                ReceiverName = s.ReceiverName,
                ReceiverPhone = s.ReceiverPhone,
                DeliveryAddress = s.DeliveryAddress,
                ZoneId = s.ZoneId,
                ZoneName = s.Zone.Name,
                ServiceType = s.ServiceType,
                WeightKg = s.WeightKg,
                ShippingFee = s.ShippingFee,
                Status = s.Status,
                Note = s.Note,
                CreatedAt = s.CreatedAt,
                History = s.StatusHistory
                    .OrderBy(h => h.ChangedAt)
                    .Select(h => new ShipmentHistoryResponse
                    {
                        FromStatus = h.FromStatus,
                        ToStatus = h.ToStatus,
                        ChangedAt = h.ChangedAt,
                        Note = h.Note
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);
    }

    public Task<ShipmentOwnership?> GetOwnershipAsync(int id, CancellationToken ct = default)
    {
        return _db.Shipments
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new ShipmentOwnership
            {
                CustomerId = s.CustomerId,
                ShipperId = s.ShipperId
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<TrackingResponse?> GetTrackingAsync(string trackingCode, CancellationToken ct = default)
    {
        var shipment = await _db.Shipments
            .AsNoTracking()
            .Include(s => s.StatusHistory)
            .FirstOrDefaultAsync(s => s.TrackingCode.ToLower() == trackingCode.ToLower(), ct);

        if (shipment is null)
        {
            return null;
        }

        return new TrackingResponse
        {
            TrackingCode = shipment.TrackingCode,
            Status = shipment.Status,
            // R15: public tracking returns only a partially masked receiver name.
            ReceiverName = MaskName(shipment.ReceiverName),
            History = shipment.StatusHistory
                .OrderBy(h => h.ChangedAt)
                .Select(h => new TrackingHistoryResponse
                {
                    FromStatus = h.FromStatus,
                    ToStatus = h.ToStatus,
                    ChangedAt = h.ChangedAt,
                    Note = h.Note
                })
                .ToList()
        };
    }

    public void Add(Shipment shipment)
    {
        _db.Shipments.Add(shipment);
    }

    private static string MaskName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var trimmed = name.Trim();
        if (trimmed.Length <= 2)
        {
            return trimmed[0] + "*";
        }

        return trimmed[0] + new string('*', trimmed.Length - 2) + trimmed[^1];
    }
}