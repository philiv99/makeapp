using MakeApp.Core.Entities;

namespace MakeApp.Core.Interfaces;

/// <summary>
/// Service interface for managing Copilot agent configurations
/// </summary>
public interface IAgentConfigurationService
{
    /// <summary>
    /// Load agent configuration from a repository
    /// </summary>
    Task<AgentConfiguration> LoadConfigurationAsync(string repositoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save agent configuration to a repository
    /// </summary>
    Task SaveConfigurationAsync(AgentConfiguration configuration, string repositoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the default agent configuration
    /// </summary>
    AgentConfiguration GetDefaultConfiguration();

    /// <summary>
    /// Get the default agent configuration based on project type
    /// </summary>
    AgentConfiguration GetDefaultConfiguration(string projectType);

    /// <summary>
    /// Validate an agent configuration
    /// </summary>
    AgentConfigurationValidationResult ValidateConfiguration(AgentConfiguration configuration);

    /// <summary>
    /// Generate copilot-instructions.md content from agent configuration
    /// </summary>
    string GenerateCopilotInstructions(AgentConfiguration configuration);

    /// <summary>
    /// Initialize agent configuration for a repository
    /// </summary>
    Task<AgentConfiguration> InitializeAsync(string repositoryPath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of validating an agent configuration
/// </summary>
public class AgentConfigurationValidationResult
{
    /// <summary>Whether the configuration is valid</summary>
    public bool IsValid { get; set; }

    /// <summary>Validation errors</summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>Validation warnings</summary>
    public List<string> Warnings { get; set; } = new();
}
