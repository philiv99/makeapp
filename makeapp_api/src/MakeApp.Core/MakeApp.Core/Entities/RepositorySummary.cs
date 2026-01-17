using MakeApp.Core.Enums;

namespace MakeApp.Core.Entities;

/// <summary>
/// Summary information about a repository in the sandbox
/// </summary>
public class RepositorySummary
{
    /// <summary>Repository name</summary>
    public string Name { get; set; } = "";
    
    /// <summary>Repository owner</summary>
    public string Owner { get; set; } = "";
    
    /// <summary>Local path to the repository</summary>
    public string Path { get; set; } = "";
    
    /// <summary>Local path to the repository (alias for Path)</summary>
    public string LocalPath { get; set; } = "";
    
    /// <summary>Current branch name</summary>
    public string CurrentBranch { get; set; } = "";
    
    /// <summary>Overall status type</summary>
    public RepoStatusType Status { get; set; }
    
    /// <summary>Whether MakeApp is configured for this repo</summary>
    public bool HasMakeAppConfig { get; set; }
    
    /// <summary>Whether the repository can be safely removed</summary>
    public bool CanRemove => Status == RepoStatusType.Clean;
    
    /// <summary>Human-readable status summary</summary>
    public string StatusSummary { get; set; } = "";
    
    /// <summary>Number of unstaged files</summary>
    public int UnstagedCount { get; set; }
    
    /// <summary>Number of staged files</summary>
    public int StagedCount { get; set; }
    
    /// <summary>Number of unpushed commits</summary>
    public int UnpushedCount { get; set; }
    
    /// <summary>When the repository was last modified</summary>
    public DateTime LastModified { get; set; }
}
