using ShipTrack.Api.Domain.Entities;
using ShipTrack.Api.Dtos.Common;
using ShipTrack.Api.Dtos.Shipments;
using ShipTrack.Api.Dtos.Tracking;

namespace ShipTrack.Api.Repositories;

public interface IShipmentRepository
{
    Task<Shipment?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<Shipment?> GetByTrackingCodeAsync(string trackingCode, CancellationToken ct = default);

    Task<List<Shipment>> GetByShipperIdAsync(string shipperId, CancellationToken ct = default);

    Task<PagedResult<ShipmentResponse>> GetPagedAsync(ShipmentQuery query, string? customerId, CancellationToken ct = default);

    Task<ShipmentDetailsResponse?> GetDetailsAsync(int id, CancellationToken ct = default);

    Task<ShipmentOwnership?> GetOwnershipAsync(int id, CancellationToken ct = default);

    Task<TrackingResponse?> GetTrackingAsync(string trackingCode, CancellationToken ct = default);

    void Add(Shipment shipment);
}
