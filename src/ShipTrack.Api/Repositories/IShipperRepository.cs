using ShipTrack.Api.Domain.Entities;

namespace ShipTrack.Api.Repositories;

public interface IShipperRepository
{
    Task<ApplicationUser?> GetShipperByIdAsync(string shipperId, CancellationToken ct = default);

    Task<int> GetActiveShipmentCountAsync(string shipperId, CancellationToken ct = default);

    Task<List<ShipperSummary>> GetShippersWithActiveCountAsync(CancellationToken ct = default);
}