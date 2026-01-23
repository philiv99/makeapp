using Asp.Versioning;
using FluentValidation;
using MakeApp.Application;
using MakeApp.Core.Configuration;
using MakeApp.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Only configure Serilog if not in Testing environment
if (builder.Environment.EnvironmentName != "Testing")
{
    // Configure Serilog early to capture startup logs
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/makeapp-.log", rollingInterval: RollingInterval.Day)
        .CreateLogger();

    builder.Host.UseSerilog();
}

try
{
    Log.Information("Starting MakeApp API...");

    // Configure Options pattern
    builder.Services.Configure<MakeAppOptions>(
        builder.Configuration.GetSection(MakeAppOptions.SectionName));
    builder.Services.Configure<UserConfiguration>(
        builder.Configuration.GetSection(UserConfiguration.SectionName));

    // Add API versioning
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new HeaderApiVersionReader("X-Api-Version"));
    }).AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    // Add controllers
    builder.Services.AddControllers();

    // Add FluentValidation
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    // Add health checks
    builder.Services.AddHealthChecks();

    // Add Swagger/OpenAPI
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "MakeApp API",
            Version = "v1",
            Description = "API for orchestrating feature development workflows using GitHub Copilot"
        });
    });

    // Register Application layer services
    builder.Services.AddApplicationServices();

    // Register Infrastructure layer services
    builder.Services.AddInfrastructureServices();

    var app = builder.Build();

    // Configure request logging only when Serilog is configured
    if (builder.Environment.EnvironmentName != "Testing")
    {
        app.UseSerilogRequestLogging();
    }

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "MakeApp API v1");
        });
    }

    app.UseHttpsRedirection();
    app.UseAuthorization();

    // Map health check endpoints
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });
    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false // Liveness check - just returns healthy if app is running
    });

    app.MapControllers();

    Log.Information("MakeApp API started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make the implicit Program class public so test projects can access it
public partial class Program { }
