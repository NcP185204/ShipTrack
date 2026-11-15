using ShipTrack.Api.Dtos.Shippers;

namespace ShipTrack.Api.Services;

public interface IShipperService
{
    Task<List<ShipperResponse>> GetShippersAsync(CancellationToken ct = default);
}