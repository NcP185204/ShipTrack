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
