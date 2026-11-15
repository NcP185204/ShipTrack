using ShipTrack.Api.Domain.Enums;

namespace ShipTrack.Api.Dtos.Dashboard;

public class DashboardSummaryResponse
{
    public Dictionary<ShipmentStatus, int> ShipmentsByStatus { get; set; } = new();

    public decimal Revenue { get; set; }

    public decimal SuccessRate { get; set; }

    public List<TopShipperResponse> TopShippers { get; set; } = new();
}