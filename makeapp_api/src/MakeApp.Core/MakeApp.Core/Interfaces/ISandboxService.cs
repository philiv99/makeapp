using MakeApp.Core.Entities;

namespace MakeApp.Core.Interfaces;

/// <summary>
/// Service interface for sandbox operations
/// </summary>
public interface ISandboxService
{
    // Sandbox-level operations
    
    /// <summary>Get sandbox information</summary>
    Task<SandboxInfo> GetSandboxInfoAsync();
    
    /// <summary>List all repositories in the sandbox</summary>
    Task<IEnumerable<RepositorySummary>> ListSandboxReposAsync();
    
    /// <summary>Validate the sandbox path exists and is accessible</summary>
    Task<bool> ValidateSandboxPathAsync();
    
    // Repo-level operations
    
    /// <summary>Get detailed status for a repository</summary>
    Task<RepositoryStatus> GetRepoStatusAsync(string repoName);
    
    /// <summary>Remove a repository from the sandbox</summary>
    Task<RemoveResult> RemoveRepoAsync(string repoName, bool force = false);
    
    /// <summary>Clean working files (cache/logs/temp) for a repository</summary>
    Task CleanupRepoWorkingFilesAsync(string repoName);
    
    /// <summary>Clean working files for all repositories</summary>
    Task CleanupAllWorkingFilesAsync();
}
