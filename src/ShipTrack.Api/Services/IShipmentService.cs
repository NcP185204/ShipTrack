using ShipTrack.Api.Dtos.Common;
using ShipTrack.Api.Dtos.Shipments;
using ShipTrack.Api.Dtos.Tracking;

namespace ShipTrack.Api.Services;

public interface IShipmentService
{
    Task<EstimateResponse> EstimateAsync(EstimateRequest request, CancellationToken ct = default);

    Task<ShipmentResponse> CreateAsync(CreateShipmentRequest request, string customerId, CancellationToken ct = default);

    Task<ShipmentResponse> AssignAsync(int shipmentId, string shipperId, string dispatcherId, CancellationToken ct = default);

    Task<List<ShipmentResponse>> GetMyTasksAsync(string shipperId, CancellationToken ct = default);

    Task<ShipmentResponse> UpdateStatusAsync(int shipmentId, UpdateStatusRequest request, string shipperId, CancellationToken ct = default);

    Task<ShipmentResponse> CancelAsync(int shipmentId, string userId, bool isDispatcher, CancellationToken ct = default);

    Task<PagedResult<ShipmentResponse>> GetMyShipmentsAsync(string customerId, ShipmentQuery query, CancellationToken ct = default);

    Task<PagedResult<ShipmentResponse>> GetAllShipmentsAsync(ShipmentQuery query, CancellationToken ct = default);

    Task<ShipmentDetailsResponse> GetDetailsAsync(int id, string userId, bool isDispatcher, bool isShipper, CancellationToken ct = default);

    Task<TrackingResponse> GetTrackingAsync(string trackingCode, CancellationToken ct = default);
}
