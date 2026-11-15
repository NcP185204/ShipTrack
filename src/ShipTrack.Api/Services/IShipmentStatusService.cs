using ShipTrack.Api.Domain.Entities;
using ShipTrack.Api.Domain.Enums;

namespace ShipTrack.Api.Services;

public interface IShipmentStatusService
{
    Task<Shipment> ApplyStatusAsync(
        int shipmentId,
        ShipmentStatus newStatus,
        string actorUserId,
        string? note,
        CancellationToken ct = default);
}