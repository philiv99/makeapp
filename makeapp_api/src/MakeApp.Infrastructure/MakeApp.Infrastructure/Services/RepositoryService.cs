using MakeApp.Core.Entities;
using MakeApp.Core.Interfaces;

namespace MakeApp.Infrastructure.Services;

/// <summary>
/// Implementation of IRepositoryService
/// </summary>
public class RepositoryService : IRepositoryService
{
    private readonly IFileSystem _fileSystem;
    private readonly string _defaultReposPath;

    public RepositoryService(IFileSystem fileSystem, string? defaultReposPath = null)
    {
        _fileSystem = fileSystem;
        _defaultReposPath = defaultReposPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "repos");
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<RepositoryInfo>> GetAvailableRepositoriesAsync(string? reposPath = null)
    {
        var path = reposPath ?? _defaultReposPath;
        
        if (!_fileSystem.Directory.Exists(path))
        {
            return Enumerable.Empty<RepositoryInfo>();
        }

        var repos = await ScanFolderAsync(path);
        return repos;
    }

    /// <inheritdoc/>
    public async Task<RepositoryInfo?> GetRepositoryInfoAsync(string owner, string name)
    {
        var repos = await GetAvailableRepositoriesAsync();
        return repos.FirstOrDefault(r => 
            r.Owner.Equals(owner, StringComparison.OrdinalIgnoreCase) && 
            r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc/>
    public async Task<string?> GetRepositoryPathAsync(string owner, string name)
    {
        var repo = await GetRepositoryInfoAsync(owner, name);
        return repo?.LocalPath;
    }

    /// <inheritdoc/>
    public Task<RepositoryConfigStatus> GetConfigurationStatusAsync(string repoPath)
    {
        var status = new RepositoryConfigStatus();

        // Check for copilot-instructions.md
        var copilotPath = _fileSystem.Path.Combine(repoPath, ".github", "copilot-instructions.md");
        status.HasCopilotInstructions = _fileSystem.File.Exists(copilotPath);
        if (status.HasCopilotInstructions)
        {
            status.CopilotInstructionsPath = copilotPath;
        }

        // Check for .makeapp folder
        var makeappPath = _fileSystem.Path.Combine(repoPath, ".makeapp");
        status.HasMakeAppFolder = _fileSystem.Directory.Exists(makeappPath);

        // Check for agent configs
        var agentsPath = _fileSystem.Path.Combine(makeappPath, "agents");
        status.HasAgentConfigs = _fileSystem.Directory.Exists(agentsPath);

        // Check for MCP config
        var mcpPath = _fileSystem.Path.Combine(repoPath, ".vscode", "mcp.json");
        status.HasMcpConfig = _fileSystem.File.Exists(mcpPath);

        return Task.FromResult(status);
    }

    /// <inheritdoc/>
    public Task<bool> ValidateRepositoryAsync(string repoPath)
    {
        // Check if .git folder exists
        var gitPath = _fileSystem.Path.Combine(repoPath, ".git");
        return Task.FromResult(_fileSystem.Directory.Exists(gitPath));
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<RepositoryInfo>> ScanFolderAsync(string folderPath)
    {
        var repos = new List<RepositoryInfo>();

        if (!_fileSystem.Directory.Exists(folderPath))
        {
            return repos;
        }

        var directories = _fileSystem.Directory.GetDirectories(folderPath);
        
        foreach (var dir in directories)
        {
            if (await ValidateRepositoryAsync(dir))
            {
                var name = _fileSystem.Path.GetFileName(dir);
                
                // Try to get remote URL to determine owner
                var owner = GetOwnerFromRemote(dir) ?? "local";
                
                repos.Add(new RepositoryInfo
                {
                    Name = name,
                    Owner = owner,
                    FullName = $"{owner}/{name}",
                    LocalPath = dir,
                    Description = ""
                });
            }
        }

        return repos;
    }

    private string? GetOwnerFromRemote(string repoPath)
    {
        try
        {
            var configPath = _fileSystem.Path.Combine(repoPath, ".git", "config");
            if (!_fileSystem.File.Exists(configPath))
            {
                return null;
            }

            var config = _fileSystem.File.ReadAllText(configPath);
            
            // Try to parse the remote URL
            var urlMatch = System.Text.RegularExpressions.Regex.Match(
                config, 
                @"url\s*=\s*(?:https?://github\.com/|git@github\.com:)([^/]+)/");
            
            if (urlMatch.Success)
            {
                return urlMatch.Groups[1].Value;
            }
        }
        catch
        {
            // Ignore errors
        }

        return null;
    }
}
