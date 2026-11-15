using ShipTrack.Api.Dtos.Dashboard;

namespace ShipTrack.Api.Repositories;

public interface IDashboardRepository
{
    Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken ct = default);
}