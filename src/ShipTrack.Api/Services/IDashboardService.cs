using ShipTrack.Api.Dtos.Dashboard;

namespace ShipTrack.Api.Services;

public interface IDashboardService
{
    Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken ct = default);
}