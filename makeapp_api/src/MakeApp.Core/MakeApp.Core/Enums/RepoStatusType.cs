namespace MakeApp.Core.Enums;

/// <summary>
/// Status of a repository in the sandbox
/// </summary>
public enum RepoStatusType
{
    /// <summary>Repository is clean with no pending changes</summary>
    Clean,
    
    /// <summary>Repository has pending changes (unstaged, staged, or unpushed)</summary>
    HasPendingChanges,
    
    /// <summary>Repository status could not be determined</summary>
    Unknown,
    
    /// <summary>Repository was not found</summary>
    NotFound,
    
    /// <summary>Repository is ready</summary>
    Ready,
    
    /// <summary>Repository is invalid</summary>
    Invalid
}
