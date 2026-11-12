# STACK — ShipTrack (.NET)

> Fixed technology choices. **Do not add or replace libraries outside this list.** If something is needed, ask first and record the reason in `DECISIONS.md`.

## Platform
| Item | Choice | Reason |
|------|--------|--------|
| Runtime / SDK | .NET 10 (same version as GoPlay.Web) | Consistent, already installed |
| Language | C# (`Nullable` and `ImplicitUsings` enabled) | |
| App type | ASP.NET Core **Web API** (Controllers, not Minimal API) | Maps clearly to Spring `@RestController`; shows layered design |
| Database | SQL Server (Docker, `mssql/server` image, port 1433) | Many .NET job descriptions require SQL Server |
| ORM | Entity Framework Core (Code-First, Migrations) | |
| Auth | ASP.NET Core Identity + JWT Bearer | |
| API docs | Swagger (Swashbuckle.AspNetCore) | |
| Logging | ILogger + Serilog (console + file) | |
| Validation | DataAnnotations (+ FluentValidation only if needed, ask first) | |
| Mapping | Hand-written (extension methods / static mappers), **no AutoMapper** | Easy to understand and explain in interviews |
| Frontend | Plain HTML + CSS + JavaScript (fetch API), Bootstrap 5 via CDN, in `wwwroot` | Shows HTML/CSS/JS without a framework |
| Testing | xUnit + Moq (or NSubstitute) + EF Core InMemory/SQLite for service tests | |
| Containers | Docker Compose (SQL Server, optionally the API) | |
| Source control | Git + GitHub, small commits per task | |

## Expected NuGet packages
- `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.EntityFrameworkCore.Tools`
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `Swashbuckle.AspNetCore`
- `Serilog.AspNetCore`, `Serilog.Sinks.File`
- Test project: `xunit`, `Moq`, `Microsoft.NET.Test.Sdk`, `Microsoft.EntityFrameworkCore.Sqlite` (or InMemory)

## Solution structure
```
ShipTrack/
├─ docs/                      # SPEC, DATA-MODEL, TASKS, STACK, RULES, DECISIONS
├─ src/
│  └─ ShipTrack.Api/
│     ├─ Controllers/
│     ├─ Services/            # Interfaces + implementations (business logic)
│     ├─ Repositories/        # (or use DbContext in services; record the choice in DECISIONS)
│     ├─ Domain/              # Entities, Enums, ShipmentStateMachine, fee calculators
│     ├─ Data/                # DbContext, Configurations, Migrations, DbInitializer
│     ├─ Dtos/                # Request/Response DTOs
│     ├─ Middleware/          # Global exception handler
│     └─ wwwroot/             # Static frontend
├─ tests/
│  └─ ShipTrack.Tests/
├─ docker-compose.yml
├─ .gitignore
└─ README.md
```

## Versions & environment
- On macOS (Apple Silicon): run SQL Server in Docker with emulation (`--platform linux/amd64`) or the `azure-sql-edge` image; if that is slow or fails, **ask** before switching to SQLite.
- Keep secrets (JWT key, connection string) in `appsettings.Development.json` or user-secrets and **never commit them**.
