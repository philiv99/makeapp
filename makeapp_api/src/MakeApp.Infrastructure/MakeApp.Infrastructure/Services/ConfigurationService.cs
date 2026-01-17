using MakeApp.Core.Configuration;
using MakeApp.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace MakeApp.Infrastructure.Services;

/// <summary>
/// Implementation of IConfigurationService with layered configuration loading
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly IOptionsMonitor<MakeAppOptions> _makeAppOptions;
    private readonly IOptionsMonitor<UserConfiguration> _userOptions;
    private readonly IGitHubService _gitHubService;
    private readonly IFileSystem _fileSystem;

    // User config file location
    private readonly string _userConfigPath;

    public ConfigurationService(
        IOptionsMonitor<MakeAppOptions> makeAppOptions,
        IOptionsMonitor<UserConfiguration> userOptions,
        IGitHubService gitHubService,
        IFileSystem fileSystem)
    {
        _makeAppOptions = makeAppOptions;
        _userOptions = userOptions;
        _gitHubService = gitHubService;
        _fileSystem = fileSystem;

        // User config is stored in ~/.makeapp/config.json
        _userConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".makeapp",
            "config.json");
    }

    /// <inheritdoc/>
    public Task<MakeAppOptions> GetConfigurationAsync()
    {
        return Task.FromResult(_makeAppOptions.CurrentValue);
    }

    /// <inheritdoc/>
    public async Task<MakeAppOptions> UpdateConfigurationAsync(MakeAppOptions options)
    {
        // In a real implementation, this would persist to a configuration file
        // For now, we return the merged configuration
        var current = _makeAppOptions.CurrentValue;

        // Merge the options (incoming takes precedence)
        MergeOptions(current, options);

        // Persist user overrides to user config file
        await PersistUserOverridesAsync(options);

        return current;
    }

    /// <inheritdoc/>
    public Task<UserConfiguration> GetUserConfigurationAsync()
    {
        return Task.FromResult(_userOptions.CurrentValue);
    }

    /// <inheritdoc/>
    public async Task<UserConfiguration> UpdateUserConfigurationAsync(UserConfiguration configuration)
    {
        // Persist to user config file
        var configDir = Path.GetDirectoryName(_userConfigPath);
        if (!string.IsNullOrEmpty(configDir) && !_fileSystem.Directory.Exists(configDir))
        {
            _fileSystem.Directory.CreateDirectory(configDir);
        }

        var json = System.Text.Json.JsonSerializer.Serialize(configuration, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        await _fileSystem.File.WriteAllTextAsync(_userConfigPath, json);

        return configuration;
    }

    /// <inheritdoc/>
    public async Task<ConfigurationValidationResult> ValidateUserConfigurationAsync()
    {
        var result = new ConfigurationValidationResult { IsValid = true };
        var userConfig = _userOptions.CurrentValue;

        // Validate GitHub credentials
        try
        {
            var currentUser = await _gitHubService.GetCurrentUserAsync();
            result.GitHubUsername = currentUser.Login;
            result.GitHubScopes = currentUser.Scopes ?? new List<string>();

            // Check required scopes
            if (!result.GitHubScopes.Contains("repo"))
            {
                result.Warnings.Add("Missing 'repo' scope - repository operations may fail");
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add($"GitHub authentication failed: {ex.Message}");
        }

        // Validate sandbox path
        var sandboxPath = userConfig.SandboxPath;
        if (string.IsNullOrEmpty(sandboxPath))
        {
            sandboxPath = _makeAppOptions.CurrentValue.Folders.Sandbox;
        }

        if (string.IsNullOrEmpty(sandboxPath))
        {
            result.Warnings.Add("Sandbox path not configured - using default");
            sandboxPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".makeapp",
                "sandbox");
        }

        try
        {
            // Check if path exists and is writable
            if (!_fileSystem.Directory.Exists(sandboxPath))
            {
                _fileSystem.Directory.CreateDirectory(sandboxPath);
            }

            // Test write access
            var testFile = _fileSystem.Path.Combine(sandboxPath, ".test_write");
            await _fileSystem.File.WriteAllTextAsync(testFile, "test");
            _fileSystem.File.Delete(testFile);

            result.SandboxPathValid = true;
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.SandboxPathValid = false;
            result.Errors.Add($"Sandbox path not writable: {ex.Message}");
        }

        return result;
    }

    /// <inheritdoc/>
    public Task<MakeAppOptions> GetDefaultConfigurationAsync()
    {
        // Return a fresh instance with default values
        return Task.FromResult(new MakeAppOptions());
    }

    /// <summary>
    /// Merge source options into target (source values take precedence where set)
    /// </summary>
    private void MergeOptions(MakeAppOptions target, MakeAppOptions source)
    {
        // Folders
        if (!string.IsNullOrEmpty(source.Folders.Sandbox))
            target.Folders.Sandbox = source.Folders.Sandbox;

        // GitHub
        if (!string.IsNullOrEmpty(source.GitHub.Token))
            target.GitHub.Token = source.GitHub.Token;
        if (!string.IsNullOrEmpty(source.GitHub.DefaultOwner))
            target.GitHub.DefaultOwner = source.GitHub.DefaultOwner;

        // Git
        if (!string.IsNullOrEmpty(source.Git.DefaultBranch))
            target.Git.DefaultBranch = source.Git.DefaultBranch;
        if (!string.IsNullOrEmpty(source.Git.AuthorName))
            target.Git.AuthorName = source.Git.AuthorName;
        if (!string.IsNullOrEmpty(source.Git.AuthorEmail))
            target.Git.AuthorEmail = source.Git.AuthorEmail;

        // Agent
        if (source.Agent.MaxRetries > 0)
            target.Agent.MaxRetries = source.Agent.MaxRetries;
        if (source.Agent.MaxIterations > 0)
            target.Agent.MaxIterations = source.Agent.MaxIterations;
    }

    /// <summary>
    /// Persist user-specific overrides to the user config file
    /// </summary>
    private async Task PersistUserOverridesAsync(MakeAppOptions options)
    {
        var configDir = Path.GetDirectoryName(_userConfigPath);
        if (!string.IsNullOrEmpty(configDir) && !_fileSystem.Directory.Exists(configDir))
        {
            _fileSystem.Directory.CreateDirectory(configDir);
        }

        // Only persist certain user-level settings
        var userOverrides = new
        {
            MakeApp = new
            {
                options.Folders,
                options.Git,
                options.Agent,
                options.Ui
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(userOverrides, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        await _fileSystem.File.WriteAllTextAsync(_userConfigPath, json);
    }
}
