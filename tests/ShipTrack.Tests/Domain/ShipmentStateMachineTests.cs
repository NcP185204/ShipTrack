using ShipTrack.Api.Domain;
using ShipTrack.Api.Domain.Enums;
using Xunit;

namespace ShipTrack.Tests.Domain;

public class ShipmentStateMachineTests
{
    [Theory]
    [InlineData(ShipmentStatus.Created, ShipmentStatus.Assigned)]
    [InlineData(ShipmentStatus.Created, ShipmentStatus.Cancelled)]
    [InlineData(ShipmentStatus.Assigned, ShipmentStatus.PickedUp)]
    [InlineData(ShipmentStatus.Assigned, ShipmentStatus.Cancelled)]
    [InlineData(ShipmentStatus.PickedUp, ShipmentStatus.InTransit)]
    [InlineData(ShipmentStatus.InTransit, ShipmentStatus.Delivered)]
    [InlineData(ShipmentStatus.InTransit, ShipmentStatus.DeliveryFailed)]
    [InlineData(ShipmentStatus.DeliveryFailed, ShipmentStatus.InTransit)]
    [InlineData(ShipmentStatus.DeliveryFailed, ShipmentStatus.Returned)]
    public void CanTransition_ValidTransitionPerSpec_ReturnsTrue(ShipmentStatus from, ShipmentStatus to)
    {
        // R1: transitions valid per SPEC section 4.
        Assert.True(ShipmentStateMachine.CanTransition(from, to));
    }

    [Theory]
    [InlineData(ShipmentStatus.Created, ShipmentStatus.PickedUp)]
    [InlineData(ShipmentStatus.Created, ShipmentStatus.Delivered)]
    [InlineData(ShipmentStatus.Assigned, ShipmentStatus.InTransit)]
    [InlineData(ShipmentStatus.PickedUp, ShipmentStatus.Delivered)]
    [InlineData(ShipmentStatus.InTransit, ShipmentStatus.Assigned)]
    [InlineData(ShipmentStatus.Delivered, ShipmentStatus.InTransit)]
    [InlineData(ShipmentStatus.Returned, ShipmentStatus.InTransit)]
    [InlineData(ShipmentStatus.Cancelled, ShipmentStatus.Assigned)]
    [InlineData(ShipmentStatus.Delivered, ShipmentStatus.Delivered)]
    public void CanTransition_InvalidTransition_ReturnsFalse(ShipmentStatus from, ShipmentStatus to)
    {
        // R1: terminal states and unlisted transitions must be rejected.
        Assert.False(ShipmentStateMachine.CanTransition(from, to));
    }
}