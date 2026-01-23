using MakeApp.Core.Entities;

namespace MakeApp.Core.Interfaces;

/// <summary>
/// Service interface for Git operations
/// </summary>
public interface IGitService
{
    // Status operations
    
    /// <summary>Get the current git status of a repository</summary>
    Task<GitStatus> GetStatusAsync(string repoPath);
    
    /// <summary>Get unstaged changes in a repository</summary>
    Task<IReadOnlyList<FileChange>> GetUnstagedChangesAsync(string repoPath);
    
    /// <summary>Get staged changes in a repository</summary>
    Task<IReadOnlyList<FileChange>> GetStagedChangesAsync(string repoPath);
    
    /// <summary>Get unpushed commits in a repository</summary>
    Task<IReadOnlyList<CommitInfo>> GetUnpushedCommitsAsync(string repoPath, string? remoteName = "origin");
    
    /// <summary>Check if a repository is clean (no pending changes)</summary>
    Task<bool> IsCleanAsync(string repoPath);
    
    /// <summary>Get the current branch name</summary>
    Task<string> GetCurrentBranchAsync(string repoPath);
    
    // Working tree operations
    
    /// <summary>Stage changes in a repository</summary>
    Task<bool> StageChangesAsync(string repoPath, string pathSpec = ".");
    
    /// <summary>Stage all changes in a repository</summary>
    Task<bool> StageAllAsync(string repoPath);
    
    /// <summary>Create a commit</summary>
    Task<CommitResult> CommitAsync(string repoPath, string message, CommitOptions? options = null);
    
    /// <summary>Push changes to remote</summary>
    Task<PushResult> PushAsync(string repoPath, PushOptions? options = null);
    
    /// <summary>Push changes to remote with specific branch</summary>
    Task<PushResult> PushAsync(string repoPath, string branchName, bool setUpstream = false);
    
    // Branch operations
    
    /// <summary>Create a new branch</summary>
    Task<BranchInfo> CreateBranchAsync(string repoPath, string branchName, string? baseBranch = null);
    
    /// <summary>Checkout a branch</summary>
    Task<bool> CheckoutAsync(string repoPath, string branchName);
    
    /// <summary>Get branches in a repository</summary>
    Task<IReadOnlyList<BranchInfo>> GetBranchesAsync(string repoPath, bool includeRemote = false);
    
    /// <summary>Delete a branch</summary>
    Task<bool> DeleteBranchAsync(string repoPath, string branchName, bool force = false);
    
    // Clone operations
    
    /// <summary>Clone a repository</summary>
    Task<string> CloneAsync(string cloneUrl, string localPath, CloneOptions? options = null);
}

/// <summary>
/// Git status information
/// </summary>
public class GitStatus
{
    /// <summary>Whether the repository has any changes</summary>
    public bool IsDirty { get; set; }
    
    /// <summary>Current branch name</summary>
    public string CurrentBranch { get; set; } = "";
    
    /// <summary>Number of staged files</summary>
    public int StagedCount { get; set; }
    
    /// <summary>Number of unstaged modified files</summary>
    public int ModifiedCount { get; set; }
    
    /// <summary>Number of untracked files</summary>
    public int UntrackedCount { get; set; }
    
    /// <summary>Number of deleted files</summary>
    public int DeletedCount { get; set; }
}

/// <summary>
/// Options for creating a commit
/// </summary>
public class CommitOptions
{
    /// <summary>Author name</summary>
    public string? AuthorName { get; set; }
    
    /// <summary>Author email</summary>
    public string? AuthorEmail { get; set; }
    
    /// <summary>Whether to amend the previous commit</summary>
    public bool Amend { get; set; }
    
    /// <summary>Whether to sign the commit</summary>
    public bool Sign { get; set; }
}

/// <summary>
/// Result of a commit operation
/// </summary>
public class CommitResult
{
    /// <summary>Whether the commit was successful</summary>
    public bool Success { get; set; }
    
    /// <summary>Commit SHA</summary>
    public string? CommitSha { get; set; }
    
    /// <summary>Error message if commit failed</summary>
    public string? Error { get; set; }
}

/// <summary>
/// Options for pushing changes
/// </summary>
public class PushOptions
{
    /// <summary>Remote name</summary>
    public string RemoteName { get; set; } = "origin";
    
    /// <summary>Whether to set upstream tracking</summary>
    public bool SetUpstream { get; set; }
    
    /// <summary>Whether to force push</summary>
    public bool Force { get; set; }
}

/// <summary>
/// Result of a push operation
/// </summary>
public class PushResult
{
    /// <summary>Whether the push was successful</summary>
    public bool Success { get; set; }
    
    /// <summary>Error message if push failed</summary>
    public string? Error { get; set; }
}

/// <summary>
/// Options for cloning a repository
/// </summary>
public class CloneOptions
{
    /// <summary>Branch to checkout after clone</summary>
    public string? Branch { get; set; }
    
    /// <summary>Clone depth (0 for full clone)</summary>
    public int Depth { get; set; }
    
    /// <summary>Whether to clone recursively (submodules)</summary>
    public bool Recursive { get; set; }
}
