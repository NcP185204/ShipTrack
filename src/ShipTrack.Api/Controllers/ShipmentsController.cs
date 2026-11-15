using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShipTrack.Api.Dtos.Common;
using ShipTrack.Api.Dtos.Shipments;
using ShipTrack.Api.Services;

namespace ShipTrack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShipmentsController : ControllerBase
{
    private readonly IShipmentService _shipmentService;

    public ShipmentsController(IShipmentService shipmentService)
    {
        _shipmentService = shipmentService;
    }

    [HttpPost("estimate")]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<EstimateResponse>> Estimate(EstimateRequest request, CancellationToken ct)
    {
        var estimate = await _shipmentService.EstimateAsync(request, ct);
        return Ok(estimate);
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<ShipmentResponse>> Create(CreateShipmentRequest request, CancellationToken ct)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Authenticated user has no identifier.");

        var shipment = await _shipmentService.CreateAsync(request, customerId, ct);

        // The details endpoint (GET /api/shipments/{id}) is implemented in a later task.
        return Created($"/api/shipments/{shipment.Id}", shipment);
    }

    [HttpPost("{id:int}/assign")]
    [Authorize(Roles = "Dispatcher")]
    public async Task<ActionResult<ShipmentResponse>> Assign(int id, AssignShipmentRequest request, CancellationToken ct)
    {
        var dispatcherId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Authenticated user has no identifier.");

        var shipment = await _shipmentService.AssignAsync(id, request.ShipperId, dispatcherId, ct);
        return Ok(shipment);
    }

    [HttpPost("{id:int}/status")]
    [Authorize(Roles = "Shipper")]
    public async Task<ActionResult<ShipmentResponse>> UpdateStatus(int id, UpdateStatusRequest request, CancellationToken ct)
    {
        var shipperId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Authenticated user has no identifier.");

        var shipment = await _shipmentService.UpdateStatusAsync(id, request, shipperId, ct);
        return Ok(shipment);
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = "Customer,Dispatcher")]
    public async Task<ActionResult<ShipmentResponse>> Cancel(int id, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Authenticated user has no identifier.");

        var isDispatcher = User.IsInRole("Dispatcher");

        var shipment = await _shipmentService.CancelAsync(id, userId, isDispatcher, ct);
        return Ok(shipment);
    }

    [HttpGet("my")]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<PagedResult<ShipmentResponse>>> GetMy([FromQuery] ShipmentQuery query, CancellationToken ct)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Authenticated user has no identifier.");

        var result = await _shipmentService.GetMyShipmentsAsync(customerId, query, ct);
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = "Dispatcher")]
    public async Task<ActionResult<PagedResult<ShipmentResponse>>> GetAll([FromQuery] ShipmentQuery query, CancellationToken ct)
    {
        var result = await _shipmentService.GetAllShipmentsAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Customer,Dispatcher,Shipper")]
    public async Task<ActionResult<ShipmentDetailsResponse>> GetById(int id, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Authenticated user has no identifier.");

        var isDispatcher = User.IsInRole("Dispatcher");
        var isShipper = User.IsInRole("Shipper");

        var details = await _shipmentService.GetDetailsAsync(id, userId, isDispatcher, isShipper, ct);
        return Ok(details);
    }
}