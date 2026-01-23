using MakeApp.Core.Configuration;

namespace MakeApp.Core.Interfaces;

/// <summary>
/// Service for managing application and user configuration
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Get the current MakeApp configuration
    /// </summary>
    Task<MakeAppOptions> GetConfigurationAsync();

    /// <summary>
    /// Update MakeApp configuration
    /// </summary>
    Task<MakeAppOptions> UpdateConfigurationAsync(MakeAppOptions options);

    /// <summary>
    /// Get the current user configuration
    /// </summary>
    Task<UserConfiguration> GetUserConfigurationAsync();

    /// <summary>
    /// Update user configuration
    /// </summary>
    Task<UserConfiguration> UpdateUserConfigurationAsync(UserConfiguration configuration);

    /// <summary>
    /// Validate user configuration (checks GitHub credentials, paths, etc.)
    /// </summary>
    Task<ConfigurationValidationResult> ValidateUserConfigurationAsync();

    /// <summary>
    /// Get the default configuration values
    /// </summary>
    Task<MakeAppOptions> GetDefaultConfigurationAsync();
}

/// <summary>
/// Result of configuration validation
/// </summary>
public class ConfigurationValidationResult
{
    /// <summary>Whether the configuration is valid</summary>
    public bool IsValid { get; set; }

    /// <summary>GitHub username if authenticated</summary>
    public string? GitHubUsername { get; set; }

    /// <summary>GitHub authenticated scopes</summary>
    public IList<string> GitHubScopes { get; set; } = new List<string>();

    /// <summary>Whether the sandbox path is valid and writable</summary>
    public bool SandboxPathValid { get; set; }

    /// <summary>Validation error messages</summary>
    public IList<string> Errors { get; set; } = new List<string>();

    /// <summary>Validation warning messages</summary>
    public IList<string> Warnings { get; set; } = new List<string>();
}
