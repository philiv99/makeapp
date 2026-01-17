using Microsoft.Extensions.DependencyInjection;
using MakeApp.Application.Services;
using MakeApp.Core.Interfaces;

namespace MakeApp.Application;

/// <summary>
/// Extension methods for registering Application layer services
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Add Application layer services to the service collection
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register Application services
        services.AddScoped<AppService>();
        services.AddScoped<FeatureService>();
        services.AddScoped<MemoryApplicationService>();
        services.AddScoped<PlanGeneratorService>();
        services.AddScoped<PhasedExecutionService>();
        services.AddScoped<PromptFormatterService>();

        // Register interfaces
        services.AddScoped<IAgentConfigurationService, AgentConfigurationService>();

        return services;
    }
}
