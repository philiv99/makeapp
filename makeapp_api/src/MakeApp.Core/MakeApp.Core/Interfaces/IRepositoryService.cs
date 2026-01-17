using MakeApp.Core.Entities;

namespace MakeApp.Core.Interfaces;

/// <summary>
/// Service interface for repository operations
/// </summary>
public interface IRepositoryService
{
    /// <summary>Get available repositories from configured folders</summary>
    Task<IEnumerable<RepositoryInfo>> GetAvailableRepositoriesAsync(string? reposPath = null);
    
    /// <summary>Get repository information by owner and name</summary>
    Task<RepositoryInfo?> GetRepositoryInfoAsync(string owner, string name);
    
    /// <summary>Get repository path by owner and name</summary>
    Task<string?> GetRepositoryPathAsync(string owner, string name);
    
    /// <summary>Get repository configuration status</summary>
    Task<RepositoryConfigStatus> GetConfigurationStatusAsync(string repoPath);
    
    /// <summary>Validate that a path is a valid git repository</summary>
    Task<bool> ValidateRepositoryAsync(string repoPath);
    
    /// <summary>Scan a folder for git repositories</summary>
    Task<IEnumerable<RepositoryInfo>> ScanFolderAsync(string folderPath);
}

/// <summary>
/// Repository configuration status
/// </summary>
public class RepositoryConfigStatus
{
    /// <summary>Whether copilot-instructions.md exists</summary>
    public bool HasCopilotInstructions { get; set; }
    
    /// <summary>Whether .makeapp folder exists</summary>
    public bool HasMakeAppFolder { get; set; }
    
    /// <summary>Whether agent configurations exist</summary>
    public bool HasAgentConfigs { get; set; }
    
    /// <summary>Whether MCP configuration exists</summary>
    public bool HasMcpConfig { get; set; }
    
    /// <summary>Path to copilot instructions if exists</summary>
    public string? CopilotInstructionsPath { get; set; }
    
    /// <summary>Overall readiness status</summary>
    public bool IsReady => HasCopilotInstructions && HasMakeAppFolder;
}
