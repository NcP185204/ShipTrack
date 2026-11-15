using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using ShipTrack.Api.Data;
using ShipTrack.Api.Domain.Entities;
using ShipTrack.Api.Middleware;
using ShipTrack.Api.Repositories;
using ShipTrack.Api.Services;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/shiptrack-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting ShipTrack.Api");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    // Add services to the container.
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter a JWT token (without the 'Bearer ' prefix)."
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    builder.Services.AddProblemDetails();

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    builder.Services.Configure<ApiBehaviorOptions>(options =>
    {
        // Map model validation errors to 400 with a consistent ProblemDetails body.
        options.InvalidModelStateResponseFactory = context =>
        {
            var problem = new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed"
            };

            return new BadRequestObjectResult(problem);
        };
    });

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            // R14: password must be at least 6 characters (no other complexity requirements).
            options.Password.RequiredLength = 6;
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>();

    builder.Services.AddScoped<IAuthService, AuthService>();

    builder.Services.AddScoped<IZoneRepository, ZoneRepository>();
    builder.Services.AddScoped<IPricingRuleRepository, PricingRuleRepository>();
    builder.Services.AddScoped<IShipmentRepository, ShipmentRepository>();
    builder.Services.AddScoped<IShipperRepository, ShipperRepository>();
    builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

    builder.Services.AddScoped<IZoneService, ZoneService>();
    builder.Services.AddScoped<IPricingRuleService, PricingRuleService>();

    // Stateless fee calculators (Strategy pattern) and their resolver.
    builder.Services.AddSingleton<StandardFeeCalculator>();
    builder.Services.AddSingleton<ExpressFeeCalculator>();
    builder.Services.AddSingleton<IShippingFeeCalculatorFactory, ShippingFeeCalculatorFactory>();

    builder.Services.AddScoped<IShipmentService, ShipmentService>();
    builder.Services.AddScoped<IShipmentStatusService, ShipmentStatusService>();
    builder.Services.AddScoped<IShipperService, ShipperService>();
    builder.Services.AddScoped<IDashboardService, DashboardService>();

    builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
            };
        });

    builder.Services.AddAuthorization();

    var app = builder.Build();

    // Apply pending migrations and seed data (idempotent, safe to run on every startup).
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
    }

    await DbInitializer.SeedAsync(app.Services);

    // Configure the HTTP request pipeline.
    app.UseSerilogRequestLogging();

    app.UseExceptionHandler();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ShipTrack.Api terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}