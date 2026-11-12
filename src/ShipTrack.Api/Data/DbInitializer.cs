using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShipTrack.Api.Domain.Entities;
using ShipTrack.Api.Domain.Enums;

namespace ShipTrack.Api.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await SeedRolesAsync(roleManager);

        var dispatcher = await SeedUserAsync(userManager, "dispatcher@shiptrack.com", "Dispatcher@123", "Dispatcher", "Dispatcher");
        var shipper1 = await SeedUserAsync(userManager, "shipper1@shiptrack.com", "Shipper@123", "Shipper One", "Shipper");
        var shipper2 = await SeedUserAsync(userManager, "shipper2@shiptrack.com", "Shipper@123", "Shipper Two", "Shipper");
        var customer = await SeedUserAsync(userManager, "customer@shiptrack.com", "Customer@123", "Customer", "Customer");

        var zones = await SeedZonesAsync(db);

        await SeedShipmentsAsync(db, customer, shipper1, shipper2, dispatcher, zones);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = { "Customer", "Dispatcher", "Shipper" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task<ApplicationUser> SeedUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string fullName,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create user {email}: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }

        return user;
    }

    private static async Task<Dictionary<string, Zone>> SeedZonesAsync(ApplicationDbContext db)
    {
        var zones = new Dictionary<string, Zone>();

        var definitions = new (string Name, string Description, decimal BaseFee, decimal ExtraFeePerKg, decimal ExpressMultiplier)[]
        {
            ("InnerCity", "Inner city delivery", 20000m, 5000m, 1.5m),
            ("Suburban", "Suburban delivery", 30000m, 8000m, 1.5m),
            ("Province", "Province delivery", 50000m, 12000m, 1.5m)
        };

        foreach (var def in definitions)
        {
            var zone = await db.Zones.FirstOrDefaultAsync(z => z.Name == def.Name);

            if (zone is null)
            {
                zone = new Zone
                {
                    Name = def.Name,
                    Description = def.Description,
                    PricingRule = new PricingRule
                    {
                        BaseFee = def.BaseFee,
                        ExtraFeePerKg = def.ExtraFeePerKg,
                        ExpressMultiplier = def.ExpressMultiplier
                    }
                };

                db.Zones.Add(zone);
                await db.SaveChangesAsync();
            }

            zones[def.Name] = zone;
        }

        return zones;
    }

    private static async Task SeedShipmentsAsync(
        ApplicationDbContext db,
        ApplicationUser customer,
        ApplicationUser shipper1,
        ApplicationUser shipper2,
        ApplicationUser dispatcher,
        Dictionary<string, Zone> zones)
    {
        // Idempotency guard: if any shipment already exists, assume seeding is done.
        if (await db.Shipments.AnyAsync())
        {
            return;
        }

        var now = DateTime.UtcNow;
        var datePart = now.ToString("yyyyMMdd");

        // Fees below follow R4: BaseFee + ceil(max(0, weight - 1)) * ExtraFeePerKg,
        // and Express = Standard * ExpressMultiplier (R5).
        var shipments = new List<Shipment>
        {
            BuildShipment($"ST{datePart}A1B2C3", customer, null, zones["InnerCity"], ServiceType.Standard, 2m, 25000m, ShipmentStatus.Created, 0, now),
            BuildShipment($"ST{datePart}A1B2C4", customer, shipper1, zones["InnerCity"], ServiceType.Standard, 3m, 30000m, ShipmentStatus.Assigned, 0, now),
            BuildShipment($"ST{datePart}A1B2C5", customer, shipper1, zones["Suburban"], ServiceType.Express, 2m, 57000m, ShipmentStatus.PickedUp, 0, now),
            BuildShipment($"ST{datePart}A1B2C6", customer, shipper1, zones["Suburban"], ServiceType.Standard, 5m, 62000m, ShipmentStatus.InTransit, 0, now),
            BuildShipment($"ST{datePart}A1B2C7", customer, shipper1, zones["Province"], ServiceType.Express, 1m, 75000m, ShipmentStatus.Delivered, 0, now),
            BuildShipment($"ST{datePart}A1B2C8", customer, shipper2, zones["Province"], ServiceType.Standard, 4m, 86000m, ShipmentStatus.DeliveryFailed, 1, now),
            BuildShipment($"ST{datePart}A1B2C9", customer, shipper2, zones["InnerCity"], ServiceType.Standard, 2m, 25000m, ShipmentStatus.Returned, 3, now),
            BuildShipment($"ST{datePart}A1B2D0", customer, null, zones["InnerCity"], ServiceType.Standard, 1m, 20000m, ShipmentStatus.Cancelled, 0, now)
        };

        foreach (var shipment in shipments)
        {
            AddHistory(shipment, customer, dispatcher, shipper1, shipper2, now);
            db.Shipments.Add(shipment);
        }

        await db.SaveChangesAsync();
    }

    private static Shipment BuildShipment(
        string trackingCode,
        ApplicationUser customer,
        ApplicationUser? shipper,
        Zone zone,
        ServiceType serviceType,
        decimal weightKg,
        decimal shippingFee,
        ShipmentStatus status,
        int failedAttempts,
        DateTime createdAt)
    {
        return new Shipment
        {
            TrackingCode = trackingCode,
            CustomerId = customer.Id,
            ShipperId = shipper?.Id,
            SenderName = "Nguyen Van A",
            SenderPhone = "0901234567",
            PickupAddress = "123 Le Loi, District 1",
            ReceiverName = "Tran Thi B",
            ReceiverPhone = "0912345678",
            DeliveryAddress = "456 Nguyen Hue, District 1",
            ZoneId = zone.Id,
            ServiceType = serviceType,
            WeightKg = weightKg,
            ShippingFee = shippingFee,
            Status = status,
            FailedAttempts = failedAttempts,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    private static void AddHistory(
        Shipment shipment,
        ApplicationUser customer,
        ApplicationUser dispatcher,
        ApplicationUser shipper1,
        ApplicationUser shipper2,
        DateTime now)
    {
        // Every shipment starts with a creation row (Created -> Created).
        shipment.StatusHistory.Add(new ShipmentStatusHistory
        {
            FromStatus = ShipmentStatus.Created,
            ToStatus = ShipmentStatus.Created,
            ChangedByUserId = customer.Id,
            ChangedAt = now,
            Note = "Shipment created"
        });

        switch (shipment.Status)
        {
            case ShipmentStatus.Created:
            case ShipmentStatus.Cancelled:
                if (shipment.Status == ShipmentStatus.Cancelled)
                {
                    shipment.StatusHistory.Add(new ShipmentStatusHistory
                    {
                        FromStatus = ShipmentStatus.Created,
                        ToStatus = ShipmentStatus.Cancelled,
                        ChangedByUserId = customer.Id,
                        ChangedAt = now.AddMinutes(5),
                        Note = "Cancelled by customer"
                    });
                }
                break;

            case ShipmentStatus.Assigned:
                shipment.StatusHistory.Add(new ShipmentStatusHistory
                {
                    FromStatus = ShipmentStatus.Created,
                    ToStatus = ShipmentStatus.Assigned,
                    ChangedByUserId = dispatcher.Id,
                    ChangedAt = now.AddMinutes(5),
                    Note = "Assigned to shipper"
                });
                break;

            case ShipmentStatus.PickedUp:
                AddTransition(shipment, ShipmentStatus.Created, ShipmentStatus.Assigned, dispatcher.Id, now.AddMinutes(5), "Assigned to shipper");
                AddTransition(shipment, ShipmentStatus.Assigned, ShipmentStatus.PickedUp, shipper1.Id, now.AddMinutes(10), "Picked up");
                break;

            case ShipmentStatus.InTransit:
                AddTransition(shipment, ShipmentStatus.Created, ShipmentStatus.Assigned, dispatcher.Id, now.AddMinutes(5), "Assigned to shipper");
                AddTransition(shipment, ShipmentStatus.Assigned, ShipmentStatus.PickedUp, shipper1.Id, now.AddMinutes(10), "Picked up");
                AddTransition(shipment, ShipmentStatus.PickedUp, ShipmentStatus.InTransit, shipper1.Id, now.AddMinutes(15), "In transit");
                break;

            case ShipmentStatus.Delivered:
                AddTransition(shipment, ShipmentStatus.Created, ShipmentStatus.Assigned, dispatcher.Id, now.AddMinutes(5), "Assigned to shipper");
                AddTransition(shipment, ShipmentStatus.Assigned, ShipmentStatus.PickedUp, shipper1.Id, now.AddMinutes(10), "Picked up");
                AddTransition(shipment, ShipmentStatus.PickedUp, ShipmentStatus.InTransit, shipper1.Id, now.AddMinutes(15), "In transit");
                AddTransition(shipment, ShipmentStatus.InTransit, ShipmentStatus.Delivered, shipper1.Id, now.AddMinutes(20), "Delivered");
                break;

            case ShipmentStatus.DeliveryFailed:
                AddTransition(shipment, ShipmentStatus.Created, ShipmentStatus.Assigned, dispatcher.Id, now.AddMinutes(5), "Assigned to shipper");
                AddTransition(shipment, ShipmentStatus.Assigned, ShipmentStatus.PickedUp, shipper2.Id, now.AddMinutes(10), "Picked up");
                AddTransition(shipment, ShipmentStatus.PickedUp, ShipmentStatus.InTransit, shipper2.Id, now.AddMinutes(15), "In transit");
                AddTransition(shipment, ShipmentStatus.InTransit, ShipmentStatus.DeliveryFailed, shipper2.Id, now.AddMinutes(20), "Delivery failed");
                break;

            case ShipmentStatus.Returned:
                AddTransition(shipment, ShipmentStatus.Created, ShipmentStatus.Assigned, dispatcher.Id, now.AddMinutes(5), "Assigned to shipper");
                AddTransition(shipment, ShipmentStatus.Assigned, ShipmentStatus.PickedUp, shipper2.Id, now.AddMinutes(10), "Picked up");
                AddTransition(shipment, ShipmentStatus.PickedUp, ShipmentStatus.InTransit, shipper2.Id, now.AddMinutes(15), "In transit");
                AddTransition(shipment, ShipmentStatus.InTransit, ShipmentStatus.DeliveryFailed, shipper2.Id, now.AddMinutes(20), "Delivery failed (1)");
                AddTransition(shipment, ShipmentStatus.DeliveryFailed, ShipmentStatus.InTransit, shipper2.Id, now.AddMinutes(25), "Retry");
                AddTransition(shipment, ShipmentStatus.InTransit, ShipmentStatus.DeliveryFailed, shipper2.Id, now.AddMinutes(30), "Delivery failed (2)");
                AddTransition(shipment, ShipmentStatus.DeliveryFailed, ShipmentStatus.InTransit, shipper2.Id, now.AddMinutes(35), "Retry");
                AddTransition(shipment, ShipmentStatus.InTransit, ShipmentStatus.DeliveryFailed, shipper2.Id, now.AddMinutes(40), "Delivery failed (3)");
                AddTransition(shipment, ShipmentStatus.DeliveryFailed, ShipmentStatus.Returned, null, now.AddMinutes(45), "Returned automatically after 3 failed attempts");
                break;
        }
    }

    private static void AddTransition(
        Shipment shipment,
        ShipmentStatus from,
        ShipmentStatus to,
        string? actorId,
        DateTime at,
        string note)
    {
        shipment.StatusHistory.Add(new ShipmentStatusHistory
        {
            FromStatus = from,
            ToStatus = to,
            ChangedByUserId = actorId,
            ChangedAt = at,
            Note = note
        });
    }
}