namespace MakeApp.Core.Entities;

/// <summary>
/// Information about a branch in a repository
/// </summary>
public class BranchInfo
{
    /// <summary>Branch name</summary>
    public string Name { get; set; } = "";
    
    /// <summary>Whether this is the current branch</summary>
    public bool IsCurrent { get; set; }
    
    /// <summary>Whether this is the current HEAD</summary>
    public bool IsCurrentHead { get; set; }
    
    /// <summary>Last commit SHA on this branch</summary>
    public string? LastCommitSha { get; set; }
    
    /// <summary>Whether this is a remote tracking branch</summary>
    public bool IsRemote { get; set; }
    
    /// <summary>Remote name if this is a remote branch</summary>
    public string? RemoteName { get; set; }
    
    /// <summary>Tracking branch name</summary>
    public string? TrackingBranch { get; set; }
    
    /// <summary>Latest commit SHA on this branch</summary>
    public string? LatestCommitSha { get; set; }
    
    /// <summary>Latest commit message</summary>
    public string? LatestCommitMessage { get; set; }
    
    /// <summary>When the latest commit was made</summary>
    public DateTime? LatestCommitDate { get; set; }
    
    /// <summary>Number of commits ahead of tracking branch</summary>
    public int? AheadBy { get; set; }
    
    /// <summary>Number of commits behind tracking branch</summary>
    public int? BehindBy { get; set; }
}
