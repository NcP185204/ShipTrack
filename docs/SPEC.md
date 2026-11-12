# SPEC — ShipTrack (.NET) — Shipment & Delivery Management System

> Portfolio project for a .NET Backend/Fullstack Intern position.
> All documentation, class names, file names, and endpoints are in English.

## 1. Goals
- **ASP.NET Core Web API** (.NET 10) + **SQL Server** (Docker) + **EF Core Code-First**.
- Simple frontend: plain HTML/CSS/JavaScript (fetch API) in `wwwroot`, using Bootstrap 5. **Do not write much custom CSS.**
- Focus: **business rules**, layered design, design patterns, tests. Out of scope: maps, GPS, online payment, email/SMS notifications.
- Timeline: 5–7 days.

## 2. Roles
| Role | Permissions |
|------|-------------|
| Customer | Register, create shipments, view own shipments, cancel when allowed |
| Dispatcher | View all shipments, assign shippers, manage zones and pricing rules, view dashboard |
| Shipper | View shipments assigned to them, update delivery status |
| (Public) | Look up shipment progress by tracking code (no login required) |

## 3. Features (MUST)
1. Register / log in with JWT (Customers self-register; Dispatcher/Shipper accounts are seeded only — 1 Dispatcher, 2 Shippers). JWT lifetime is 60 minutes with no refresh token.
2. Create a shipment: sender, receiver, pickup/delivery addresses, delivery zone, weight (kg), service type (Standard/Express), note.
3. Automatic fee calculation on creation, plus an estimate endpoint (not persisted).
4. Auto-generated tracking code and public lookup with status history.
5. Dispatcher assigns a shipper to a shipment.
6. Shipper updates status; every change writes one history row.
7. Customer cancels a shipment; failed delivery leads to retry or return.
8. Dispatcher manages Zones and PricingRules (CRUD).
9. Dashboard: shipments by status, revenue (Delivered), delivery success rate (`Delivered / (Delivered + Returned)`), top shippers (top 5 by Delivered count).
10. Lists support **pagination, filtering by status/date range, and search by tracking code**. Paging returns `PagedResult<T> { items, page, pageSize, totalItems, totalPages }`; search matches tracking code by **contains, case-insensitive**; filter by **exact status** and `fromDate`/`toDate` on `CreatedAt` (inclusive).

## 4. Status lifecycle
`Created → Assigned → PickedUp → InTransit → Delivered`

| From | To | Performed by |
|------|----|--------------|
| Created | Assigned, Cancelled | Dispatcher / Customer |
| Assigned | PickedUp, Cancelled | Shipper (PickedUp) / Customer or Dispatcher (Cancelled) |
| PickedUp | InTransit | Shipper |
| InTransit | Delivered, DeliveryFailed | Shipper |
| DeliveryFailed | InTransit (retry), Returned | Shipper (retry) / system (Returned) |
| Delivered, Returned, Cancelled | — (terminal) | — |

## 5. Business rules
| # | Rule |
|---|------|
| R1 | A status change is valid only per the table in section 4; violations return `409 Conflict` with a reason |
| R2 | A shipment can be cancelled only in `Created` or `Assigned`; only the owner (or a Dispatcher) may cancel |
| R3 | Weight must be > 0 and ≤ 30 kg |
| R4 | Fee = `BaseFee(zone, service) + ceil(max(0, weight - 1)) * ExtraFeePerKg(zone, service)` (the first kg is included in BaseFee) |
| R5 | Fee calculation is chosen by `IShippingFeeCalculator` according to service type (Standard, Express). Standard = R4 formula; Express = Standard fee × `ExpressMultiplier` (one PricingRule per Zone) |
| R6 | Tracking code is unique, format `ST` + `yyyyMMdd` + 6 random **uppercase** alphanumeric characters; lookup is case-insensitive |
| R7 | Each shipper may have at most **5 active shipments** (Assigned/PickedUp/InTransit) at once; exceeding returns `409` |
| R8 | A shipper can be assigned only while the shipment is `Created`; the shipper must be `IsActive` |
| R9 | If two Dispatchers assign the same shipment concurrently, only one succeeds and the other gets `409` (use `RowVersion` on Shipment) |
| R10 | A shipper may update only shipments **assigned to them** |
| R11 | At most 3 `DeliveryFailed` events per shipment; the 3rd automatically moves it to `Returned` |
| R12 | Every status change writes a `ShipmentStatusHistory` row (old status, new status, time, actor, note); history is never edited or deleted |
| R13 | `Zone.Name` is unique; `PricingRule.ZoneId` is unique (one rule per Zone); a Zone that has shipments cannot be deleted; deleting a Zone that only has a PricingRule deletes the rule with it |
| R14 | Email must be unique on registration; password ≥ 6 characters |
| R15 | Public tracking returns minimal information only (status, history, partially masked receiver name); no full phone number or address |

## 6. API list (REST, prefix `/api`)
| Method | URL | Role | Description |
|--------|-----|------|-------------|
| POST | /auth/register | Public | Register a Customer |
| POST | /auth/login | Public | Returns a JWT |
| GET | /zones | Customer+ | List zones |
| POST/PUT/DELETE | /zones, /zones/{id} | Dispatcher | Zone CRUD |
| GET/PUT | /pricing-rules | Dispatcher | Manage pricing rules |
| POST | /shipments/estimate | Customer | Estimate fee |
| POST | /shipments | Customer | Create shipment |
| GET | /shipments/my | Customer | My shipments (paged, filtered) |
| GET | /shipments | Dispatcher | All shipments (paged, filtered, searchable) |
| GET | /shipments/{id} | Owner/Dispatcher/assigned Shipper | Details + history |
| POST | /shipments/{id}/cancel | Customer/Dispatcher | Cancel shipment |
| POST | /shipments/{id}/assign | Dispatcher | Body: `shipperId` |
| POST | /shipments/{id}/status | Shipper | Body: `newStatus`, `note` |
| GET | /shippers/my-tasks | Shipper | Shipments assigned to me |
| GET | /shippers | Dispatcher | Shippers with their active shipment counts |
| GET | /tracking/{code} | Public | Tracking lookup |
| GET | /dashboard/summary | Dispatcher | Statistics |

All errors return a consistent `ProblemDetails` body (from the global exception handler).

## 7. Validation
- Sender and receiver name, phone, and address are required; phone must match `^(0|\+84)\d{9}$`.
- `Weight` per R3; `ServiceType` must be a valid enum value; `ZoneId` must exist.
- Pagination: `page ≥ 1`, `pageSize` 1–50 (default 10).

## 8. Architecture & design patterns (must be explained in the README)
- **Layered**: `Api` (controllers) → Services → `Domain` (entities, enums, rules) → data access (EF Core, repositories). May be one project with several folders for simplicity.
- **Repository (+ Unit of Work), or DbContext behind an interface**, and **Dependency Injection**.
- **Strategy**: `IShippingFeeCalculator` (Standard/Express).
- **State machine**: `ShipmentStateMachine` holds the valid transition table (R1).
- **DTO + mapping**: never return entities from the API.
- **Middleware**: global exception handling, request logging (Serilog/ILogger).
- **Open/Closed**: adding a new service type only requires a new calculator.

## 9. Seed data
- Dispatcher: `dispatcher@shiptrack.com` / `Dispatcher@123`
- Shippers: `shipper1@shiptrack.com`, `shipper2@shiptrack.com` / `Shipper@123`
- Customer: `customer@shiptrack.com` / `Customer@123`
- 3 sample Zones (InnerCity, Suburban, Province), each with one PricingRule (BaseFee, ExtraFeePerKg, ExpressMultiplier), plus a few shipments in various statuses.

## 10. Out of scope
Maps/GPS, online payment, email/SMS notifications, image upload, multi-language support, microservices.
