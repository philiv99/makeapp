namespace MakeApp.Core.Entities;

/// <summary>
/// Information about a repository
/// </summary>
public class RepositoryInfo
{
    /// <summary>Unique identifier for the repository (GitHub ID)</summary>
    public long Id { get; set; }
    
    /// <summary>Repository name</summary>
    public string Name { get; set; } = "";
    
    /// <summary>Repository owner</summary>
    public string Owner { get; set; } = "";
    
    /// <summary>Full name (owner/name)</summary>
    public string FullName { get; set; } = "";
    
    /// <summary>Local path to the repository</summary>
    public string Path { get; set; } = "";
    
    /// <summary>Local path to the repository (alias for Path)</summary>
    public string? LocalPath { get; set; }
    
    /// <summary>GitHub clone URL</summary>
    public string? CloneUrl { get; set; }
    
    /// <summary>GitHub HTML URL</summary>
    public string? HtmlUrl { get; set; }
    
    /// <summary>GitHub SSH URL</summary>
    public string? SshUrl { get; set; }
    
    /// <summary>Repository description</summary>
    public string? Description { get; set; }
    
    /// <summary>Default branch name</summary>
    public string DefaultBranch { get; set; } = "main";
    
    /// <summary>Current branch name</summary>
    public string? CurrentBranch { get; set; }
    
    /// <summary>Whether the repository is private</summary>
    public bool IsPrivate { get; set; }
    
    /// <summary>Whether the repository is a fork</summary>
    public bool IsFork { get; set; }
    
    /// <summary>Whether MakeApp is configured for this repo</summary>
    public bool HasMakeAppConfig { get; set; }
    
    /// <summary>When the repository was last modified</summary>
    public DateTime LastModified { get; set; }
    
    /// <summary>When the repository was created</summary>
    public DateTime? CreatedAt { get; set; }
}
