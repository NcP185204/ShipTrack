namespace ShipTrack.Api.Domain.Enums;

public enum ShipmentStatus
{
    Created = 0,
    Assigned = 1,
    PickedUp = 2,
    InTransit = 3,
    Delivered = 4,
    DeliveryFailed = 5,
    Returned = 6,
    Cancelled = 7
}