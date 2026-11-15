using ShipTrack.Api.Dtos.Zones;

namespace ShipTrack.Api.Services;

public interface IZoneService
{
    Task<List<ZoneResponse>> GetAllAsync(CancellationToken ct = default);

    Task<ZoneResponse> GetByIdAsync(int id, CancellationToken ct = default);

    Task<ZoneResponse> CreateAsync(ZoneRequest request, CancellationToken ct = default);

    Task<ZoneResponse> UpdateAsync(int id, ZoneRequest request, CancellationToken ct = default);

    Task DeleteAsync(int id, CancellationToken ct = default);
}