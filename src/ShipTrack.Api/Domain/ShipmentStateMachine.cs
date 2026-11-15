using ShipTrack.Api.Domain.Enums;

namespace ShipTrack.Api.Domain;

public static class ShipmentStateMachine
{
    // Data-driven transition table: each source status maps to the set of target statuses it may
    // move to (SPEC section 4). Keeping this as data instead of if/else branches makes R1 a single
    // source of truth that is easy to read and unit-test, and adding a new status only requires
    // editing this table.
    //
    // Notes:
    // - Cancellation is role-restricted (Customer/Dispatcher only) and is enforced by the caller,
    //   not by this table.
    // - Returned is never requested directly by an actor; it is set automatically by the status
    //   service when a shipment reaches its 3rd DeliveryFailed (R11).
    private static readonly IReadOnlyDictionary<ShipmentStatus, IReadOnlySet<ShipmentStatus>> Transitions =
        new Dictionary<ShipmentStatus, IReadOnlySet<ShipmentStatus>>
        {
            [ShipmentStatus.Created] = new HashSet<ShipmentStatus>
            {
                ShipmentStatus.Assigned,
                ShipmentStatus.Cancelled
            },
            [ShipmentStatus.Assigned] = new HashSet<ShipmentStatus>
            {
                ShipmentStatus.PickedUp,
                ShipmentStatus.Cancelled
            },
            [ShipmentStatus.PickedUp] = new HashSet<ShipmentStatus>
            {
                ShipmentStatus.InTransit
            },
            [ShipmentStatus.InTransit] = new HashSet<ShipmentStatus>
            {
                ShipmentStatus.Delivered,
                ShipmentStatus.DeliveryFailed
            },
            [ShipmentStatus.DeliveryFailed] = new HashSet<ShipmentStatus>
            {
                ShipmentStatus.InTransit,
                ShipmentStatus.Returned
            }
            // Delivered, Returned, and Cancelled are terminal and intentionally have no entries.
        };

    public static bool CanTransition(ShipmentStatus from, ShipmentStatus to)
    {
        return Transitions.TryGetValue(from, out var allowedTargets) && allowedTargets.Contains(to);
    }
}