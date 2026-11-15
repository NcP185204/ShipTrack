using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShipTrack.Api.Dtos.Zones;
using ShipTrack.Api.Services;

namespace ShipTrack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ZonesController : ControllerBase
{
    private readonly IZoneService _zoneService;

    public ZonesController(IZoneService zoneService)
    {
        _zoneService = zoneService;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<ZoneResponse>>> GetAll(CancellationToken ct)
    {
        var zones = await _zoneService.GetAllAsync(ct);
        return Ok(zones);
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<ZoneResponse>> GetById(int id, CancellationToken ct)
    {
        var zone = await _zoneService.GetByIdAsync(id, ct);
        return Ok(zone);
    }

    [HttpPost]
    [Authorize(Roles = "Dispatcher")]
    public async Task<ActionResult<ZoneResponse>> Create(ZoneRequest request, CancellationToken ct)
    {
        var zone = await _zoneService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = zone.Id }, zone);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Dispatcher")]
    public async Task<ActionResult<ZoneResponse>> Update(int id, ZoneRequest request, CancellationToken ct)
    {
        var zone = await _zoneService.UpdateAsync(id, request, ct);
        return Ok(zone);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Dispatcher")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _zoneService.DeleteAsync(id, ct);
        return NoContent();
    }
}