using ShipTrack.Api.Domain;
using ShipTrack.Api.Domain.Entities;
using ShipTrack.Api.Domain.Enums;
using ShipTrack.Api.Domain.Exceptions;
using ShipTrack.Api.Repositories;

namespace ShipTrack.Api.Services;

public class ShipmentStatusService : IShipmentStatusService
{
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ShipmentStatusService(IShipmentRepository shipmentRepository, IUnitOfWork unitOfWork)
    {
        _shipmentRepository = shipmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Shipment> ApplyStatusAsync(
        int shipmentId,
        ShipmentStatus newStatus,
        string actorUserId,
        string? note,
        CancellationToken ct = default)
    {
        var shipment = await _shipmentRepository.GetByIdAsync(shipmentId, ct)
            ?? throw new NotFoundException($"Shipment {shipmentId} not found.");

        var fromStatus = shipment.Status;

        // Returned is never requested directly; it is set automatically on the 3rd DeliveryFailed (R11).
        if (newStatus == ShipmentStatus.Returned)
        {
            throw new BusinessRuleException("Returned status is set automatically and cannot be requested.");
        }

        // R1: a status change is valid only per the transition table.
        if (!ShipmentStateMachine.CanTransition(fromStatus, newStatus))
        {
            throw new BusinessRuleException($"Cannot change shipment status from {fromStatus} to {newStatus}.");
        }

        var now = DateTime.UtcNow;

        if (newStatus == ShipmentStatus.DeliveryFailed)
        {
            shipment.FailedAttempts++;

            // R12: record the DeliveryFailed event.
            shipment.StatusHistory.Add(new ShipmentStatusHistory
            {
                FromStatus = fromStatus,
                ToStatus = ShipmentStatus.DeliveryFailed,
                ChangedByUserId = actorUserId,
                ChangedAt = now,
                Note = note
            });

            // R11: the 3rd DeliveryFailed automatically moves the shipment to Returned.
            if (shipment.FailedAttempts >= 3)
            {
                shipment.StatusHistory.Add(new ShipmentStatusHistory
                {
                    FromStatus = ShipmentStatus.DeliveryFailed,
                    ToStatus = ShipmentStatus.Returned,
                    ChangedByUserId = null,
                    ChangedAt = now,
                    Note = "Automatically returned after the 3rd delivery failure."
                });

                shipment.Status = ShipmentStatus.Returned;
            }
            else
            {
                shipment.Status = ShipmentStatus.DeliveryFailed;
            }
        }
        else
        {
            // R12: record the status change.
            shipment.StatusHistory.Add(new ShipmentStatusHistory
            {
                FromStatus = fromStatus,
                ToStatus = newStatus,
                ChangedByUserId = actorUserId,
                ChangedAt = now,
                Note = note
            });

            shipment.Status = newStatus;
        }

        shipment.UpdatedAt = now;

        // Shipment + history row(s) are persisted in one SaveChangesAsync call.
        await _unitOfWork.SaveChangesAsync(ct);

        return shipment;
    }
}