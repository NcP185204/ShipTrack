# TASKS — ShipTrack (.NET)

> Work in order. Each task must run on its own and has acceptance criteria. **After each task: build, test manually, then make a small commit.**

| # | Task | Content | Acceptance criteria |
|---|------|---------|---------------------|
| 1 | Project setup | `dotnet new webapi`, solution, EF Core SqlServer/Tools/Identity packages, Swagger, SQL Server in Docker (`docker-compose.yml`), `appsettings` | `dotnet run` opens Swagger; connects to SQL Server |
| 2 | Domain + DbContext | Entities, enums, `ApplicationDbContext` (Fluent API: precision, indexes, unique, RowVersion) | Build succeeds; `Init` migration created |
| 3 | Migration + Seed | `database update`, `DbInitializer` seeding roles, users, Zones, PricingRules, a few Shipments | All tables exist; seeded accounts can log in |
| 4 | JWT auth | Identity + JWT, `/auth/register`, `/auth/login`, role-based policies | Swagger Authorize works; wrong role → 403 |
| 5 | Global error + logging | Middleware/`IExceptionHandler` returning `ProblemDetails`, ILogger/Serilog | Errors return consistent JSON and are logged |
| 6 | Zone & Pricing CRUD | Controller + Service + Repository, validation (R13) | CRUD works; a Zone with shipments cannot be deleted |
| 7 | Fee calculator | `IShippingFeeCalculator` + Standard/Express (Strategy), `/shipments/estimate` (R3–R5) | Estimated fee matches worked examples |
| 8 | Create shipment | `POST /shipments`, tracking code (R6), first history row (R12) | Shipment created with fee and code |
| 9 | State machine | `ShipmentStateMachine` + status-change service (R1, R11, R12) | Invalid transition → 409 |
| 10 | Assign shipper | `/assign` (R7, R8, R9) using RowVersion + `DbUpdateConcurrencyException` | Duplicate assignment or over-limit → 409 |
| 11 | Shipper updates | `/shippers/my-tasks`, `/shipments/{id}/status` (R10) | Shipper sees and updates only their own shipments |
| 12 | Cancel shipment | `/cancel` (R2) | Cancels only when allowed; writes history |
| 13 | Lists + tracking | `/shipments/my`, `/shipments` with paging/filter/search; `/tracking/{code}` (R15) | Correct data; sensitive fields masked |
| 14 | Dashboard | `/dashboard/summary` using LINQ/GroupBy | Figures match the seed data |
| 15 | Frontend | HTML/JS pages in `wwwroot`: login, create shipment, my shipments, tracking, dispatcher (assign), shipper (update), dashboard | Full flow works in the browser |
| 16 | xUnit | Tests for fee calculator, state machine, R7, R9, R11, cancellation | `dotnet test` is green; each test names the rule it covers |
| 17 | README | ERD, Swagger/UI screenshots, how to run, patterns and design decisions, R1–R15 table | A stranger can clone and run it following the README |

## Hint
Task 9 (state machine) and Task 10 (concurrency) are the two parts to be ready to explain in an interview. Read and understand the code the agent writes for these two yourself.
