using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShipTrack.Api.Dtos.Tracking;
using ShipTrack.Api.Services;

namespace ShipTrack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrackingController : ControllerBase
{
    private readonly IShipmentService _shipmentService;

    public TrackingController(IShipmentService shipmentService)
    {
        _shipmentService = shipmentService;
    }

    [HttpGet("{code}")]
    [AllowAnonymous]
    public async Task<ActionResult<TrackingResponse>> GetByCode(string code, CancellationToken ct)
    {
        var tracking = await _shipmentService.GetTrackingAsync(code, ct);
        return Ok(tracking);
    }
}