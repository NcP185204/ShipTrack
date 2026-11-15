using ShipTrack.Api.Dtos.Shippers;
using ShipTrack.Api.Repositories;

namespace ShipTrack.Api.Services;

public class ShipperService : IShipperService
{
    private readonly IShipperRepository _shipperRepository;

    public ShipperService(IShipperRepository shipperRepository)
    {
        _shipperRepository = shipperRepository;
    }

    public async Task<List<ShipperResponse>> GetShippersAsync(CancellationToken ct = default)
    {
        var shippers = await _shipperRepository.GetShippersWithActiveCountAsync(ct);

        return shippers.Select(s => new ShipperResponse
        {
            Id = s.Id,
            FullName = s.FullName,
            Email = s.Email,
            IsActive = s.IsActive,
            ActiveShipmentCount = s.ActiveShipmentCount
        }).ToList();
    }
}