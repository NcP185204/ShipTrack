using ShipTrack.Api.Dtos.Dashboard;
using ShipTrack.Api.Repositories;

namespace ShipTrack.Api.Services;

public class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _dashboardRepository;

    public DashboardService(IDashboardRepository dashboardRepository)
    {
        _dashboardRepository = dashboardRepository;
    }

    public Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken ct = default)
    {
        return _dashboardRepository.GetSummaryAsync(ct);
    }
}