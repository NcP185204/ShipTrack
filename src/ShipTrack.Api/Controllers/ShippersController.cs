using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShipTrack.Api.Dtos.Shipments;
using ShipTrack.Api.Dtos.Shippers;
using ShipTrack.Api.Services;

namespace ShipTrack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShippersController : ControllerBase
{
    private readonly IShipperService _shipperService;
    private readonly IShipmentService _shipmentService;

    public ShippersController(IShipperService shipperService, IShipmentService shipmentService)
    {
        _shipperService = shipperService;
        _shipmentService = shipmentService;
    }

    [HttpGet]
    [Authorize(Roles = "Dispatcher")]
    public async Task<ActionResult<List<ShipperResponse>>> GetAll(CancellationToken ct)
    {
        var shippers = await _shipperService.GetShippersAsync(ct);
        return Ok(shippers);
    }

    [HttpGet("my-tasks")]
    [Authorize(Roles = "Shipper")]
    public async Task<ActionResult<List<ShipmentResponse>>> GetMyTasks(CancellationToken ct)
    {
        var shipperId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Authenticated user has no identifier.");

        var shipments = await _shipmentService.GetMyTasksAsync(shipperId, ct);
        return Ok(shipments);
    }
}