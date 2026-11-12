# DATA-MODEL — ShipTrack

## Enums
- `ServiceType`: Standard, Express
- `ShipmentStatus`: Created, Assigned, PickedUp, InTransit, Delivered, DeliveryFailed, Returned, Cancelled

## Entities
**ApplicationUser** (extends IdentityUser): `FullName`, `IsActive`  
Roles: Customer, Dispatcher, Shipper

**Zone**: `Id`, `Name` (unique), `Description`

**PricingRule**: `Id`, `ZoneId` (FK, unique), `BaseFee` (decimal 18,2), `ExtraFeePerKg` (decimal 18,2), `ExpressMultiplier` (decimal 18,2)  
Unique index: (`ZoneId`) — one PricingRule per Zone

**Shipment**: `Id`, `TrackingCode` (unique), `CustomerId` (FK), `ShipperId` (FK, nullable),  
`SenderName`, `SenderPhone`, `PickupAddress`, `ReceiverName`, `ReceiverPhone`, `DeliveryAddress`,  
`ZoneId` (FK), `ServiceType`, `WeightKg` (decimal 6,2), `ShippingFee` (decimal 18,2),  
`Status`, `FailedAttempts` (int), `Note`, `CreatedAt`, `UpdatedAt`, `RowVersion` (`[Timestamp]`)

**ShipmentStatusHistory**: `Id`, `ShipmentId` (FK), `FromStatus`, `ToStatus`, `ChangedByUserId`, `ChangedAt`, `Note`

## Relationships
- User (Customer) 1—N Shipment
- User (Shipper) 1—N Shipment (`ShipperId`, nullable)
- Zone 1—0..1 PricingRule; Zone 1—N Shipment (restrict delete)
- Shipment 1—N ShipmentStatusHistory (cascade delete)

## Suggested indexes
- `Shipment(TrackingCode)` unique
- `Shipment(Status, CreatedAt)` for listing, filtering, and the dashboard
- `Shipment(ShipperId, Status)` for counting active shipments (R7)
