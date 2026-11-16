using Microsoft.EntityFrameworkCore;
using Moq;
using ShipTrack.Api.Domain.Entities;
using ShipTrack.Api.Domain.Enums;
using ShipTrack.Api.Domain.Exceptions;
using ShipTrack.Api.Dtos.Shipments;
using ShipTrack.Api.Repositories;
using ShipTrack.Api.Services;
using Xunit;

namespace ShipTrack.Tests.Services;

public class ShipmentServiceTests
{
    private static (
        ShipmentService Service,
        Mock<IShipmentRepository> ShipmentRepo,
        Mock<IShipperRepository> ShipperRepo,
        Mock<IUnitOfWork> UnitOfWork,
        Mock<IShipmentStatusService> StatusService) CreateSut()
    {
        var shipmentRepo = new Mock<IShipmentRepository>();
        var shipperRepo = new Mock<IShipperRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var statusService = new Mock<IShipmentStatusService>();

        var service = new ShipmentService(
            new Mock<IPricingRuleRepository>().Object,
            new Mock<IZoneRepository>().Object,
            shipmentRepo.Object,
            shipperRepo.Object,
            unitOfWork.Object,
            new Mock<IShippingFeeCalculatorFactory>().Object,
            statusService.Object);

        return (service, shipmentRepo, shipperRepo, unitOfWork, statusService);
    }

    [Fact]
    public async Task AssignAsync_ShipperHasFiveActiveShipments_ThrowsBusinessRuleException()
    {
        var (service, shipmentRepo, shipperRepo, _, _) = CreateSut();

        shipmentRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(
            new Shipment { Id = 1, Status = ShipmentStatus.Created });
        shipperRepo.Setup(r => r.GetShipperByIdAsync("shipper-1", It.IsAny<CancellationToken>())).ReturnsAsync(
            new ApplicationUser { Id = "shipper-1", FullName = "Shipper One", IsActive = true });
        shipperRepo.Setup(r => r.GetActiveShipmentCountAsync("shipper-1", It.IsAny<CancellationToken>())).ReturnsAsync(5);

        // R7: a shipper may have at most 5 active shipments -> 409.
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.AssignAsync(1, "shipper-1", "dispatcher-1"));

        Assert.Equal(409, ex.StatusCode);
        Assert.Contains("5 active shipments", ex.Message);
    }

    [Fact]
    public async Task AssignAsync_ConcurrentAssignment_ThrowsBusinessRuleException()
    {
        var (service, shipmentRepo, shipperRepo, unitOfWork, _) = CreateSut();

        shipmentRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(
            new Shipment { Id = 1, Status = ShipmentStatus.Created });
        shipperRepo.Setup(r => r.GetShipperByIdAsync("shipper-1", It.IsAny<CancellationToken>())).ReturnsAsync(
            new ApplicationUser { Id = "shipper-1", FullName = "Shipper One", IsActive = true });
        shipperRepo.Setup(r => r.GetActiveShipmentCountAsync("shipper-1", It.IsAny<CancellationToken>())).ReturnsAsync(0);
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException("rowversion changed"));

        // R9: two Dispatchers assigning concurrently -> only one succeeds, the other gets 409.
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.AssignAsync(1, "shipper-1", "dispatcher-1"));

        Assert.Equal(409, ex.StatusCode);
        Assert.Contains("already assigned", ex.Message);
    }

    [Fact]
    public async Task CancelAsync_NonOwnerCustomer_ThrowsForbidden()
    {
        var (service, shipmentRepo, _, _, _) = CreateSut();

        shipmentRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(
            new Shipment { Id = 1, CustomerId = "owner-1", Status = ShipmentStatus.Created });

        // R2: only the owner (or a Dispatcher) may cancel -> 403.
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.CancelAsync(1, "someone-else", isDispatcher: false));

        Assert.Equal(403, ex.StatusCode);
    }

    [Fact]
    public async Task CancelAsync_OwnerInAllowedState_CancelsAndReturnsUpdatedShipment()
    {
        var (service, shipmentRepo, _, _, statusService) = CreateSut();

        shipmentRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(
            new Shipment { Id = 1, CustomerId = "owner-1", Status = ShipmentStatus.Created });
        statusService.Setup(s => s.ApplyStatusAsync(1, ShipmentStatus.Cancelled, "owner-1", "Cancelled", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Shipment { Id = 1, CustomerId = "owner-1", Status = ShipmentStatus.Cancelled });

        // R2: the owner may cancel a Created shipment.
        var result = await service.CancelAsync(1, "owner-1", isDispatcher: false);

        Assert.Equal(ShipmentStatus.Cancelled, result.Status);
        statusService.Verify(
            s => s.ApplyStatusAsync(1, ShipmentStatus.Cancelled, "owner-1", "Cancelled", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShipperNotAssigned_ThrowsForbidden()
    {
        var (service, shipmentRepo, _, _, _) = CreateSut();

        shipmentRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(
            new Shipment { Id = 1, ShipperId = "other-shipper", Status = ShipmentStatus.Assigned });

        // R10: a shipper may update only the shipments assigned to them -> 403.
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.UpdateStatusAsync(1, new UpdateStatusRequest { NewStatus = ShipmentStatus.PickedUp }, "shipper-1"));

        Assert.Equal(403, ex.StatusCode);
    }
}