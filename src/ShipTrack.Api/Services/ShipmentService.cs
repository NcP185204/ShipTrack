using Microsoft.EntityFrameworkCore;
using ShipTrack.Api.Domain.Entities;
using ShipTrack.Api.Domain.Enums;
using ShipTrack.Api.Domain.Exceptions;
using ShipTrack.Api.Dtos.Common;
using ShipTrack.Api.Dtos.Shipments;
using ShipTrack.Api.Dtos.Tracking;
using ShipTrack.Api.Repositories;

namespace ShipTrack.Api.Services;

public class ShipmentService : IShipmentService
{
    private const string TrackingCodeAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    private readonly IPricingRuleRepository _pricingRuleRepository;
    private readonly IZoneRepository _zoneRepository;
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IShipperRepository _shipperRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IShippingFeeCalculatorFactory _feeCalculatorFactory;
    private readonly IShipmentStatusService _shipmentStatusService;

    public ShipmentService(
        IPricingRuleRepository pricingRuleRepository,
        IZoneRepository zoneRepository,
        IShipmentRepository shipmentRepository,
        IShipperRepository shipperRepository,
        IUnitOfWork unitOfWork,
        IShippingFeeCalculatorFactory feeCalculatorFactory,
        IShipmentStatusService shipmentStatusService)
    {
        _pricingRuleRepository = pricingRuleRepository;
        _zoneRepository = zoneRepository;
        _shipmentRepository = shipmentRepository;
        _shipperRepository = shipperRepository;
        _unitOfWork = unitOfWork;
        _feeCalculatorFactory = feeCalculatorFactory;
        _shipmentStatusService = shipmentStatusService;
    }

    public async Task<EstimateResponse> EstimateAsync(EstimateRequest request, CancellationToken ct = default)
    {
        // R3: weight must be > 0 and <= 30 kg (service-level check in addition to DataAnnotations).
        if (request.WeightKg <= 0 || request.WeightKg > 30)
        {
            throw new BusinessRuleException("Weight must be greater than 0 and at most 30 kg.", StatusCodes.Status400BadRequest);
        }

        var rule = await _pricingRuleRepository.GetByZoneIdAsync(request.ZoneId, ct)
            ?? throw new NotFoundException($"No pricing rule found for zone {request.ZoneId}.");

        var calculator = _feeCalculatorFactory.GetCalculator(request.ServiceType);
        var fee = calculator.Calculate(rule, request.WeightKg);

        return new EstimateResponse
        {
            ZoneId = request.ZoneId,
            WeightKg = request.WeightKg,
            ServiceType = request.ServiceType,
            EstimatedFee = fee
        };
    }

    public async Task<ShipmentResponse> CreateAsync(CreateShipmentRequest request, string customerId, CancellationToken ct = default)
    {
        // R3: weight must be > 0 and <= 30 kg (service-level check in addition to DataAnnotations).
        if (request.WeightKg <= 0 || request.WeightKg > 30)
        {
            throw new BusinessRuleException("Weight must be greater than 0 and at most 30 kg.", StatusCodes.Status400BadRequest);
        }

        var zone = await _zoneRepository.GetByIdAsync(request.ZoneId, ct)
            ?? throw new NotFoundException($"Zone {request.ZoneId} not found.");

        var rule = await _pricingRuleRepository.GetByZoneIdAsync(request.ZoneId, ct)
            ?? throw new NotFoundException($"No pricing rule found for zone {request.ZoneId}.");

        var calculator = _feeCalculatorFactory.GetCalculator(request.ServiceType);
        var fee = calculator.Calculate(rule, request.WeightKg);

        var now = DateTime.UtcNow;
        var trackingCode = await GenerateUniqueTrackingCodeAsync(ct);

        var shipment = new Shipment
        {
            TrackingCode = trackingCode,
            CustomerId = customerId,
            SenderName = request.SenderName,
            SenderPhone = request.SenderPhone,
            PickupAddress = request.PickupAddress,
            ReceiverName = request.ReceiverName,
            ReceiverPhone = request.ReceiverPhone,
            DeliveryAddress = request.DeliveryAddress,
            ZoneId = request.ZoneId,
            ServiceType = request.ServiceType,
            WeightKg = request.WeightKg,
            ShippingFee = fee,
            Status = ShipmentStatus.Created,
            Note = request.Note,
            CreatedAt = now,
            UpdatedAt = now
        };

        // R12: every status change writes a history row; the first row records the creation.
        shipment.StatusHistory.Add(new ShipmentStatusHistory
        {
            FromStatus = ShipmentStatus.Created,
            ToStatus = ShipmentStatus.Created,
            ChangedByUserId = customerId,
            ChangedAt = now,
            Note = "Shipment created"
        });

        _shipmentRepository.Add(shipment);

        // Shipment + its first history row are persisted in one SaveChangesAsync call.
        await _unitOfWork.SaveChangesAsync(ct);

        return ToResponse(shipment, zone.Name);
    }

    public async Task<ShipmentResponse> AssignAsync(int shipmentId, string shipperId, string dispatcherId, CancellationToken ct = default)
    {
        var shipment = await _shipmentRepository.GetByIdAsync(shipmentId, ct)
            ?? throw new NotFoundException($"Shipment {shipmentId} not found.");

        // R8: a shipper can be assigned only while the shipment is Created.
        if (shipment.Status != ShipmentStatus.Created)
        {
            throw new BusinessRuleException($"Shipment {shipmentId} can only be assigned while it is in Created status.");
        }

        var shipper = await _shipperRepository.GetShipperByIdAsync(shipperId, ct)
            ?? throw new NotFoundException($"Shipper {shipperId} not found.");

        // R8: the shipper must be active.
        if (!shipper.IsActive)
        {
            throw new BusinessRuleException($"Shipper '{shipper.FullName}' is inactive and cannot be assigned.");
        }

        // R7: each shipper may have at most 5 active shipments (Assigned/PickedUp/InTransit).
        var activeCount = await _shipperRepository.GetActiveShipmentCountAsync(shipperId, ct);
        if (activeCount >= 5)
        {
            throw new BusinessRuleException($"Shipper '{shipper.FullName}' already has 5 active shipments.");
        }

        var now = DateTime.UtcNow;

        shipment.ShipperId = shipperId;
        shipment.Status = ShipmentStatus.Assigned;
        shipment.UpdatedAt = now;

        // R12: record the assignment.
        shipment.StatusHistory.Add(new ShipmentStatusHistory
        {
            FromStatus = ShipmentStatus.Created,
            ToStatus = ShipmentStatus.Assigned,
            ChangedByUserId = dispatcherId,
            ChangedAt = now,
            Note = "Assigned to shipper"
        });

        try
        {
            // R9: RowVersion on Shipment makes this save fail with DbUpdateConcurrencyException
            // if another Dispatcher assigned the same shipment concurrently.
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessRuleException($"Shipment {shipmentId} was already assigned by another dispatcher.");
        }

        return ToResponse(shipment, shipment.Zone?.Name ?? string.Empty);
    }

    public async Task<List<ShipmentResponse>> GetMyTasksAsync(string shipperId, CancellationToken ct = default)
    {
        // R10: a shipper sees only shipments assigned to them.
        var shipments = await _shipmentRepository.GetByShipperIdAsync(shipperId, ct);

        return shipments
            .Select(s => ToResponse(s, s.Zone?.Name ?? string.Empty))
            .ToList();
    }

    public async Task<ShipmentResponse> UpdateStatusAsync(int shipmentId, UpdateStatusRequest request, string shipperId, CancellationToken ct = default)
    {
        var shipment = await _shipmentRepository.GetByIdAsync(shipmentId, ct)
            ?? throw new NotFoundException($"Shipment {shipmentId} not found.");

        // R10: a shipper may update only shipments assigned to them.
        if (shipment.ShipperId != shipperId)
        {
            throw new BusinessRuleException("Shipper can only update shipments assigned to them.", StatusCodes.Status403Forbidden);
        }

        var updated = await _shipmentStatusService.ApplyStatusAsync(shipmentId, request.NewStatus, shipperId, request.Note, ct);

        return ToResponse(updated, updated.Zone?.Name ?? string.Empty);
    }

    public async Task<ShipmentResponse> CancelAsync(int shipmentId, string userId, bool isDispatcher, CancellationToken ct = default)
    {
        var shipment = await _shipmentRepository.GetByIdAsync(shipmentId, ct)
            ?? throw new NotFoundException($"Shipment {shipmentId} not found.");

        // R2: only the owner (or a Dispatcher) may cancel; a Customer cannot cancel another's shipment.
        if (!isDispatcher && shipment.CustomerId != userId)
        {
            throw new BusinessRuleException("Customer can only cancel their own shipments.", StatusCodes.Status403Forbidden);
        }

        var updated = await _shipmentStatusService.ApplyStatusAsync(shipmentId, ShipmentStatus.Cancelled, userId, "Cancelled", ct);

        return ToResponse(updated, updated.Zone?.Name ?? string.Empty);
    }

    public Task<PagedResult<ShipmentResponse>> GetMyShipmentsAsync(string customerId, ShipmentQuery query, CancellationToken ct = default)
    {
        return _shipmentRepository.GetPagedAsync(query, customerId, ct);
    }

    public Task<PagedResult<ShipmentResponse>> GetAllShipmentsAsync(ShipmentQuery query, CancellationToken ct = default)
    {
        return _shipmentRepository.GetPagedAsync(query, null, ct);
    }

    public async Task<ShipmentDetailsResponse> GetDetailsAsync(int id, string userId, bool isDispatcher, bool isShipper, CancellationToken ct = default)
    {
        var details = await _shipmentRepository.GetDetailsAsync(id, ct)
            ?? throw new NotFoundException($"Shipment {id} not found.");

        // Ownership rules: owner, Dispatcher, or assigned Shipper may view details.
        var ownership = await _shipmentRepository.GetOwnershipAsync(id, ct);
        var isOwner = ownership?.CustomerId == userId;
        var isAssignedShipper = isShipper && ownership?.ShipperId == userId;

        if (!isDispatcher && !isOwner && !isAssignedShipper)
        {
            throw new BusinessRuleException("You are not allowed to view this shipment.", StatusCodes.Status403Forbidden);
        }

        return details;
    }

    public async Task<TrackingResponse> GetTrackingAsync(string trackingCode, CancellationToken ct = default)
    {
        return await _shipmentRepository.GetTrackingAsync(trackingCode, ct)
            ?? throw new NotFoundException($"Shipment with tracking code {trackingCode} not found.");
    }

    private async Task<string> GenerateUniqueTrackingCodeAsync(CancellationToken ct)
    {
        // R6: format ST + yyyyMMdd + 6 random uppercase alphanumeric characters, unique.
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var code = $"ST{DateTime.UtcNow:yyyyMMdd}{GenerateRandomSuffix(6)}";
            var existing = await _shipmentRepository.GetByTrackingCodeAsync(code, ct);
            if (existing is null)
            {
                return code;
            }
        }

        throw new BusinessRuleException("Could not generate a unique tracking code. Please try again.");
    }

    private static string GenerateRandomSuffix(int length)
    {
        return string.Create(length, Random.Shared, (span, _) =>
        {
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = TrackingCodeAlphabet[Random.Shared.Next(TrackingCodeAlphabet.Length)];
            }
        });
    }

    private static ShipmentResponse ToResponse(Shipment shipment, string zoneName)
    {
        return new ShipmentResponse
        {
            Id = shipment.Id,
            TrackingCode = shipment.TrackingCode,
            SenderName = shipment.SenderName,
            SenderPhone = shipment.SenderPhone,
            PickupAddress = shipment.PickupAddress,
            ReceiverName = shipment.ReceiverName,
            ReceiverPhone = shipment.ReceiverPhone,
            DeliveryAddress = shipment.DeliveryAddress,
            ZoneId = shipment.ZoneId,
            ZoneName = zoneName,
            ServiceType = shipment.ServiceType,
            WeightKg = shipment.WeightKg,
            ShippingFee = shipment.ShippingFee,
            Status = shipment.Status,
            Note = shipment.Note,
            CreatedAt = shipment.CreatedAt
        };
    }
}