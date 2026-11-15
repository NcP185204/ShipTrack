# DECISIONS — ShipTrack (.NET)

> A short log of important design decisions. Add a new entry whenever a decision affects architecture, data, or behavior. The agent writes 1–3 lines per decision; the owner reviews them.

## Entry format
```
### D-<number>: <short title> (<date>)
- Context: why a decision was needed
- Decision: what was chosen
- Alternatives considered: what was rejected and why
- Consequences: trade-offs and follow-ups
```

## Initial decisions

### D-1: ASP.NET Core Web API with Controllers
- Context: GoPlay.Web already covers MVC + Razor; this project should show API skills.
- Decision: Web API with Controllers and a plain HTML/JS frontend.
- Alternatives considered: Minimal API (less structure to show), MVC + Razor (repeats GoPlay.Web).
- Consequences: Swagger documents the API; the frontend calls it via fetch.

### D-2: Hand-written mapping (no AutoMapper)
- Context: DTOs are required so entities never leave the API.
- Decision: Static mapper/extension methods.
- Alternatives considered: AutoMapper (hides behavior, harder to explain).
- Consequences: A bit more code, but explicit and easy to test.

### D-3: Status lifecycle through a state machine class
- Context: R1 requires strictly valid transitions.
- Decision: `ShipmentStateMachine` holds the transition table; services call it.
- Alternatives considered: `if/else` in the service (scattered, hard to test).
- Consequences: One place to change rules; easy unit tests.

### D-4: Optimistic concurrency with RowVersion for assignment
- Context: R9 requires that two Dispatchers cannot assign the same shipment.
- Decision: `[Timestamp] RowVersion` on Shipment; handle `DbUpdateConcurrencyException` and return `409`.
- Alternatives considered: pessimistic locking (more complex, blocks other requests).
- Consequences: The losing request must retry.

### D-5: Repository layer
- Context: EF Core `DbContext` already acts like a repository/unit of work.
- Decision: One repository per entity (`IShipmentRepository`, `IZoneRepository`, `IPricingRuleRepository`, `IShipmentStatusHistoryRepository`; no generic base) plus an `IUnitOfWork` that wraps `SaveChangesAsync`.
- Alternatives considered: Generic `IRepository<T>` (too abstract, hides intent); using `DbContext` directly in services (harder to mock/test).
- Consequences: Explicit, testable data access; a bit more boilerplate.

### D-6: Pricing model — one PricingRule per Zone (2026-11-12)
- Context: R5 was ambiguous about whether Express uses its own fees or a multiplier on Standard.
- Decision: One `PricingRule` per Zone (`ZoneId` unique) with `BaseFee`, `ExtraFeePerKg`, `ExpressMultiplier`. Standard = R4 formula; Express = Standard fee × `ExpressMultiplier`.
- Alternatives considered: Per-service pricing rows (redundant, confusing).
- Consequences: Simpler model; `ServiceType` removed from `PricingRule`.

### D-7: Project structure (2026-11-12)
- Context: STACK.md layout (`src/ShipTrack.Api/`, `tests/ShipTrack.Tests/`) did not match the existing scaffold.
- Decision: Move the API project into `src/ShipTrack.Api/`, add `tests/ShipTrack.Tests/`, keep the `.sln` at the repo root.
- Alternatives considered: Keep the flat `ShipTrack/` layout (does not match STACK.md).
- Consequences: Matches STACK.md; requires restructuring the existing scaffold.

### D-8: JWT lifetime and account provisioning (2026-11-12)
- Context: JWT lifetime and how Dispatcher/Shipper accounts are created were unspecified.
- Decision: JWT lifetime 60 minutes, no refresh token. Dispatcher/Shipper accounts are seeded only (1 Dispatcher, 2 Shippers); no endpoint creates them.
- Alternatives considered: Refresh tokens (out of scope); admin account-creation endpoint (not required).
- Consequences: Simpler auth; re-login after expiry.

### D-9: Fee calculator strategy and resolver (2026-11-12)
- Context: R5 requires fee calculation to be chosen by service type (Standard/Express) and to stay open for new service types.
- Decision: `IShippingFeeCalculator` with `StandardFeeCalculator` and `ExpressFeeCalculator`; a `ShippingFeeCalculatorFactory` resolves the calculator by `ServiceType`. Calculators are stateless Singletons; the factory is a Singleton that resolves them from the container.
- Alternatives considered: `if/else` in the service (scattered, not Open/Closed); keyed DI (less explicit for interview explanation).
- Consequences: Adding a service type only requires a new calculator and one factory entry.

### D-10: Tracking code generation and shipment creation (2026-11-12)
- Context: R6 requires a unique `STyyyyMMdd` + 6 uppercase alphanumeric code; R12 requires the first history row to be written with the shipment.
- Decision: Generate the code with `Random.Shared` and retry up to 10 times on a uniqueness collision; persist the Shipment and its first `ShipmentStatusHistory` row in one `SaveChangesAsync` call.
- Alternatives considered: GUID-based codes (not the required format); separate saves for shipment and history (violates R12's single-write intent).
- Consequences: Collision retries are bounded; the creation is atomic.

### D-11: Data-driven state machine and status service (2026-11-12)
- Context: R1 requires strictly valid transitions; R11 requires automatic Returned on the 3rd DeliveryFailed; R12 requires a history row per change.
- Decision: `ShipmentStateMachine` holds the transition table as data; `ShipmentStatusService` validates against it, increments `FailedAttempts`, auto-transitions to `Returned` on the 3rd failure, and writes history rows in the same `SaveChangesAsync`.
- Alternatives considered: `if/else` transition checks in the service (scattered, hard to test); separate history writes (not atomic).
- Consequences: R1 is a single source of truth; R11/R12 are centralized and testable.

### D-12: Assignment concurrency handling (2026-11-12)
- Context: R9 requires that if two Dispatchers assign the same shipment concurrently, only one succeeds and the other gets `409`.
- Decision: `Shipment.RowVersion` is a SQL Server `rowversion` column. `AssignAsync` loads the shipment, validates R7/R8, mutates it, and calls `SaveChangesAsync`; EF Core includes the original `RowVersion` in the `WHERE` clause, so the second writer gets `DbUpdateConcurrencyException`, which the service catches and rethrows as `BusinessRuleException` (409) with a clear message.
- Alternatives considered: pessimistic locking (`UPDLOCK`/transactions) — more complex and blocks other requests; re-querying inside a transaction — still needs a concurrency token to be safe.
- Consequences: The losing request must retry; no partial assignment is persisted.

### D-13: Read queries use projection and paging (2026-11-12)
- Context: Lists must support paging/filter/search, details must include history, and public tracking must expose only minimal data (R15).
- Decision: Read queries use `AsNoTracking()` and project directly to DTOs (`ShipmentResponse`, `ShipmentDetailsResponse`, `TrackingResponse`) in the repository; paging is computed with `CountAsync` + `Skip/Take`. Public tracking masks the receiver name in the projection.
- Alternatives considered: loading full entities with `Include` and mapping in the service (pulls unnecessary columns and risks N+1).
- Consequences: No N+1, only needed columns are selected, and R15 masking is enforced at the query boundary.

### D-14: Dashboard aggregation in the database (2026-11-12)
- Context: The dashboard needs counts by status, Delivered revenue, success rate, and top 5 shippers by Delivered count.
- Decision: Compute all aggregations with `GroupBy`/`Sum`/`Count` LINQ queries executed in SQL Server (`AsNoTracking`), then assemble the DTO in the repository. Success rate is `Delivered / (Delivered + Returned)` and is 0 when the denominator is 0.
- Alternatives considered: loading all shipments into memory and aggregating in C# (inefficient, does not scale).
- Consequences: Minimal data transferred; the database does the heavy lifting.
