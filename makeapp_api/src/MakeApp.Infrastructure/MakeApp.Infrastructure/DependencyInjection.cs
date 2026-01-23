using Microsoft.Extensions.DependencyInjection;
using MakeApp.Core.Interfaces;
using MakeApp.Infrastructure.Services;

namespace MakeApp.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure services
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Infrastructure layer services to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // File system operations
        services.AddSingleton<IFileSystem, FileSystemService>();

        // Configuration service
        services.AddScoped<IConfigurationService, ConfigurationService>();

        // Git operations
        services.AddScoped<IGitService, GitService>();
        services.AddScoped<IBranchService, BranchService>();

        // GitHub API operations
        services.AddScoped<IGitHubService, GitHubService>();

        // Repository management
        services.AddScoped<IRepositoryService, RepositoryService>();
        services.AddScoped<IRepositoryCreationService, RepositoryCreationService>();

        // Sandbox operations
        services.AddScoped<ISandboxService, SandboxService>();

        // Copilot integration (placeholder)
        services.AddSingleton<ICopilotService, CopilotService>();

        // Memory system
        services.AddSingleton<IMemoryService, MemoryService>();

        // Orchestration
        services.AddScoped<IOrchestrationService, OrchestrationService>();

        return services;
    }
}
