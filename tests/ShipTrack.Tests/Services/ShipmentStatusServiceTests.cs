using Moq;
using ShipTrack.Api.Domain.Entities;
using ShipTrack.Api.Domain.Enums;
using ShipTrack.Api.Domain.Exceptions;
using ShipTrack.Api.Repositories;
using ShipTrack.Api.Services;
using Xunit;

namespace ShipTrack.Tests.Services;

public class ShipmentStatusServiceTests
{
    private static Shipment CreateShipment(ShipmentStatus status, int failedAttempts = 0) =>
        new()
        {
            Id = 1,
            Status = status,
            FailedAttempts = failedAttempts,
            StatusHistory = new List<ShipmentStatusHistory>()
        };

    private static (ShipmentStatusService Service, Mock<IShipmentRepository> Repo, Mock<IUnitOfWork> UnitOfWork)
        CreateSut(Shipment shipment)
    {
        var repo = new Mock<IShipmentRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(shipment);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return (new ShipmentStatusService(repo.Object, unitOfWork.Object), repo, unitOfWork);
    }

    [Fact]
    public async Task ApplyStatusAsync_InvalidTransition_ThrowsBusinessRuleException()
    {
        var (service, _, _) = CreateSut(CreateShipment(ShipmentStatus.Created));

        // R1: Created -> Delivered is not a valid transition, so it must fail with 409.
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.ApplyStatusAsync(1, ShipmentStatus.Delivered, "shipper-1", "done"));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task ApplyStatusAsync_DirectReturnedStatus_ThrowsBusinessRuleException()
    {
        var (service, _, _) = CreateSut(CreateShipment(ShipmentStatus.InTransit));

        // R11: Returned is never requested directly, it is set automatically on the 3rd failure.
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.ApplyStatusAsync(1, ShipmentStatus.Returned, "shipper-1", null));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task ApplyStatusAsync_ValidTransition_WritesSingleHistoryRow()
    {
        var (service, _, _) = CreateSut(CreateShipment(ShipmentStatus.Created));

        var result = await service.ApplyStatusAsync(1, ShipmentStatus.Assigned, "dispatcher-1", "assigned");

        // R12: every status change writes exactly one history row.
        Assert.Equal(ShipmentStatus.Assigned, result.Status);
        var row = Assert.Single(result.StatusHistory);
        Assert.Equal(ShipmentStatus.Created, row.FromStatus);
        Assert.Equal(ShipmentStatus.Assigned, row.ToStatus);
        Assert.Equal("dispatcher-1", row.ChangedByUserId);
        Assert.Equal("assigned", row.Note);
    }

    [Fact]
    public async Task ApplyStatusAsync_ThirdDeliveryFailed_AutoReturnsAndWritesTwoHistoryRows()
    {
        var (service, _, _) = CreateSut(CreateShipment(ShipmentStatus.InTransit, failedAttempts: 2));

        var result = await service.ApplyStatusAsync(1, ShipmentStatus.DeliveryFailed, "shipper-1", "failed again");

        // R11: the 3rd DeliveryFailed automatically moves the shipment to Returned.
        Assert.Equal(ShipmentStatus.Returned, result.Status);
        Assert.Equal(3, result.FailedAttempts);
        Assert.Equal(2, result.StatusHistory.Count);
        Assert.Contains(result.StatusHistory, h => h.ToStatus == ShipmentStatus.DeliveryFailed);
        Assert.Contains(result.StatusHistory, h => h.ToStatus == ShipmentStatus.Returned);
    }

    [Fact]
    public async Task ApplyStatusAsync_FirstDeliveryFailed_StaysDeliveryFailed()
    {
        var (service, _, _) = CreateSut(CreateShipment(ShipmentStatus.InTransit, failedAttempts: 0));

        var result = await service.ApplyStatusAsync(1, ShipmentStatus.DeliveryFailed, "shipper-1", "failed");

        // R11: only the 3rd failure returns the shipment; the 1st keeps it DeliveryFailed.
        Assert.Equal(ShipmentStatus.DeliveryFailed, result.Status);
        Assert.Equal(1, result.FailedAttempts);
    }
}