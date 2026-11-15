using ShipTrack.Api.Domain.Entities;

namespace ShipTrack.Api.Repositories;

public interface IZoneRepository
{
    Task<List<Zone>> GetAllAsync(CancellationToken ct = default);

    Task<Zone?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<Zone?> GetByNameAsync(string name, CancellationToken ct = default);

    Task<bool> HasShipmentsAsync(int id, CancellationToken ct = default);

    void Add(Zone zone);

    void Update(Zone zone);

    void Delete(Zone zone);
}