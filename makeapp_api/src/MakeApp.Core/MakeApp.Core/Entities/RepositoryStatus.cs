using MakeApp.Core.Enums;

namespace MakeApp.Core.Entities;

/// <summary>
/// Detailed status of a repository including pending changes
/// </summary>
public class RepositoryStatus
{
    /// <summary>Repository name</summary>
    public string RepoName { get; set; } = "";
    
    /// <summary>Repository name (alias for RepoName)</summary>
    public string Name { get; set; } = "";
    
    /// <summary>Local path to the repository</summary>
    public string Path { get; set; } = "";
    
    /// <summary>When the repository was last modified</summary>
    public DateTime LastModified { get; set; }
    
    /// <summary>Size of the repository in bytes</summary>
    public long Size { get; set; }
    
    /// <summary>Current branch name</summary>
    public string CurrentBranch { get; set; } = "";
    
    /// <summary>Overall status type</summary>
    public RepoStatusType Status { get; set; }
    
    /// <summary>Whether the repository can be safely removed</summary>
    public bool CanRemove => Status == RepoStatusType.Clean;
    
    /// <summary>Details of pending changes</summary>
    public PendingChanges PendingChanges { get; set; } = new();
    
    /// <summary>Human-readable status summary</summary>
    public string Summary { get; set; } = "";
}

/// <summary>
/// Details of pending changes in a repository
/// </summary>
public class PendingChanges
{
    /// <summary>Whether there are unstaged changes</summary>
    public bool HasUnstagedChanges { get; set; }
    
    /// <summary>Whether there are staged changes</summary>
    public bool HasStagedChanges { get; set; }
    
    /// <summary>Whether there are unpushed commits</summary>
    public bool HasUnpushedCommits { get; set; }
    
    /// <summary>List of unstaged file changes</summary>
    public List<FileChange> UnstagedFiles { get; set; } = new();
    
    /// <summary>List of staged file changes</summary>
    public List<FileChange> StagedFiles { get; set; } = new();
    
    /// <summary>List of unpushed commits</summary>
    public List<CommitInfo> UnpushedCommits { get; set; } = new();
    
    /// <summary>Whether there are any pending changes</summary>
    public bool HasAnyPendingChanges => 
        HasUnstagedChanges || HasStagedChanges || HasUnpushedCommits;
}

/// <summary>
/// Represents a file change in a repository
/// </summary>
public class FileChange
{
    /// <summary>Relative path to the file</summary>
    public string Path { get; set; } = "";
    
    /// <summary>File path (alias for Path)</summary>
    public string FilePath { get; set; } = "";
    
    /// <summary>Type of change (string representation)</summary>
    public string Status { get; set; } = "";
    
    /// <summary>Change type (enum)</summary>
    public ChangeType ChangeType { get; set; }
}

/// <summary>
/// Information about a commit
/// </summary>
public class CommitInfo
{
    /// <summary>Short commit hash (7 characters)</summary>
    public string Hash { get; set; } = "";
    
    /// <summary>Full commit SHA</summary>
    public string? Sha { get; set; }
    
    /// <summary>Commit message</summary>
    public string Message { get; set; } = "";
    
    /// <summary>Short commit message</summary>
    public string? MessageShort { get; set; }
    
    /// <summary>When the commit was made</summary>
    public DateTime Date { get; set; }
    
    /// <summary>Author name (convenience property)</summary>
    public string Author { get; set; } = "";
    
    /// <summary>Author email (convenience property)</summary>
    public string AuthorEmail { get; set; } = "";
    
    /// <summary>Author information (detailed)</summary>
    public CommitAuthor? AuthorInfo { get; set; }
}

/// <summary>
/// Commit author information
/// </summary>
public class CommitAuthor
{
    /// <summary>Author name</summary>
    public string Name { get; set; } = "";
    
    /// <summary>Author email</summary>
    public string Email { get; set; } = "";
    
    /// <summary>When the commit was authored</summary>
    public DateTimeOffset When { get; set; }
}
