# RULES — Working rules for the AI agent (Cline) and for the code

> Read this file before every task. Goal: code that is **easy to read and easy to explain**, because the project owner must defend every line in interviews.

## A. Workflow
1. Do **one task** from `TASKS.md` at a time; do not start later tasks.
2. Before coding, briefly summarize the plan (which files will be created or changed). If anything is unclear, **ask — do not guess**.
3. **Never run `git commit` or `git push`.** The owner reviews and commits.
4. At the end of each task, report: what was done, files changed, how to test it, whether the acceptance criteria are met, and anything uncertain.
5. Do not leave diagnostic or temporary files (`*_diag.txt`, log dumps) in the repo; if you create any, delete them before reporting done.
6. Do not add packages or features outside `STACK.md` and `SPEC.md`. No unrequested extras.
7. Record each important design decision in 1–3 lines in `docs/DECISIONS.md`.
8. The build must be clean (0 errors, minimal warnings) and the app must run after every task.

## B. Naming & structure
- Class, method, and variable names in **English**: PascalCase for types and methods, camelCase for locals and parameters, `_camelCase` for private fields, interfaces start with `I`.
- Name controllers in the plural by resource (`ShipmentsController`) and use **attribute routing** `[Route("api/[controller]")]`; URLs must match the API table in SPEC.
- One class per file; the file name matches the class name.
- Meaningful suffixes: `...Dto`, `...Request`, `...Response`, `...Service`, `...Repository`.
- Comments explain **why** (especially around rules R1–R15), written briefly in English; do not restate what the code already says clearly.

## C. Architecture
- Keep controllers **thin**: receive the request, call a service, return the result. **No** business logic or EF queries in controllers.
- Business logic lives in Service/Domain. Each of R1–R15 must live in exactly one place and be testable.
- **Never return entities from the API**; always use DTOs.
- Use constructor Dependency Injection with sensible lifetimes (DbContext/Service: Scoped; stateless calculators: Singleton or Scoped, state which).
- Depend on interfaces (`IShippingFeeCalculator`, `IShipmentService`, …) for testing and extension (SOLID: S, O, D).

## D. EF Core & SQL Server
- Configure with Fluent API in `IEntityTypeConfiguration<T>` classes; declare precision for every `decimal`.
- All schema changes go through **migrations** with meaningful names (`AddShipmentIndexes`); never edit the database by hand.
- Read queries use `AsNoTracking()`; avoid N+1 (use `Include` or projection); lists are always **paged**.
- Every database-accessing method is `async`; public controller/service methods accept a `CancellationToken`.
- Guard against conflicts with `RowVersion`; catch `DbUpdateConcurrencyException` in the service and return `409`.
- Writes that touch several tables (shipment + history) happen in **one `SaveChangesAsync`** call or a transaction.

## E. API & error handling
- Return correct HTTP codes: 200/201 (with `Location`), 204, 400 (validation), 401, 403, 404, 409 (rule violation/conflict), 500.
- Errors return a consistent `ProblemDetails` body from the **global exception handler**; never expose stack traces to clients.
- Use dedicated business exceptions (`BusinessRuleException`, `NotFoundException`) and map them to HTTP codes in one place.
- Every endpoint has an appropriate `[Authorize]` (role/policy); only `/auth/*` and `/tracking/*` are public.
- Validate input with DataAnnotations plus service-level checks; never trust client data (for example, do not accept `Status` or `ShippingFee` from the request body when creating a shipment).

## F. Security
- Hash passwords with Identity (do not roll your own). JWTs have a short lifetime and the key comes from configuration.
- Never log passwords, tokens, or full phone numbers.
- Never commit secrets; `.env` and any `appsettings.*.json` containing secrets must be in `.gitignore`.
- Enforce ownership: a Customer sees and cancels only their own shipments; a Shipper updates only shipments assigned to them (R2, R10).

## G. Logging
- Use `ILogger<T>` (never `Console.WriteLine`).
- Levels: `Information` for business events (shipment created, status changed, shipper assigned), `Warning` for rule violations, `Error` for unexpected failures.
- Use structured logging: `_logger.LogInformation("Shipment {TrackingCode} moved {From} -> {To}", ...)`.

## H. Testing (xUnit)
- Each important rule (R1, R2, R4, R5, R7, R9, R11) has at least one test; name tests `Method_Condition_ExpectedResult`.
- Follow Arrange–Act–Assert; one behavior per test.
- Test pure logic (`ShipmentStateMachine`, fee calculators) without a database. Test services with in-memory SQLite or mocks.
- `dotnet test` must be green before a task is reported done.

## I. Frontend
- Plain HTML/CSS/JS with Bootstrap 5 via CDN; **do not write much custom CSS**.
- One shared JS file for API calls (`api.js`: attaches the JWT, handles `ProblemDetails` errors); do not repeat `fetch` code.
- Show API errors to users in a friendly way; never show raw technical errors.

## J. Git & documentation
- Small commits per task, message format `feat(task-8): create shipment with tracking code`.
- `.gitignore` must include: `bin/`, `obj/`, `.vs/`, `.idea/`, `*.user`, `logs/`, secrets.
- The final README includes: description, screenshots, ERD, how to run (Docker + `dotnet run`), the R1–R15 table, and an explanation of patterns and architecture.

## K. Never do
- Do not change scope or rules in `SPEC.md` without asking.
- Do not delete or edit an applied migration; add a new one.
- Do not write `catch (Exception)` and swallow the error.
- Do not scatter business logic across controllers, frontend JS, or raw SQL.
