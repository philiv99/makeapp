using MakeApp.Core.Entities;

namespace MakeApp.Core.Interfaces;

/// <summary>
/// Service interface for branch operations
/// </summary>
public interface IBranchService
{
    /// <summary>Get branches in a repository</summary>
    Task<IEnumerable<BranchInfo>> GetBranchesAsync(string repoPath, bool includeRemote = false);
    
    /// <summary>Create a feature branch</summary>
    Task<BranchInfo> CreateFeatureBranchAsync(string repoPath, string branchName, string baseBranch = "main");
    
    /// <summary>Switch to a branch</summary>
    Task<bool> SwitchToBranchAsync(string repoPath, string branchName);
    
    /// <summary>Get the current branch name</summary>
    Task<string> GetCurrentBranchAsync(string repoPath);
    
    /// <summary>Delete a branch</summary>
    Task<bool> DeleteBranchAsync(string repoPath, string branchName, bool force = false);
    
    /// <summary>Format a feature branch name</summary>
    string FormatFeatureBranchName(string featureName);
}
