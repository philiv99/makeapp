namespace MakeApp.Core.Configuration;

/// <summary>
/// User/owner configuration for client applications
/// </summary>
public class UserConfiguration
{
    /// <summary>Configuration section name</summary>
    public const string SectionName = "User";
    
    /// <summary>GitHub username</summary>
    public string GitHubUsername { get; set; } = "";
    
    /// <summary>GitHub owner (can be user or organization)</summary>
    public string GitHubOwner { get; set; } = "";
    
    /// <summary>Default branch name for new repositories</summary>
    public string DefaultBranch { get; set; } = "main";
    
    /// <summary>Whether to automatically create remote repositories</summary>
    public bool AutoCreateRemote { get; set; } = true;
    
    /// <summary>Path to sandbox folder (all repos created/checked out here)</summary>
    public string SandboxPath { get; set; } = "";
    
    /// <summary>Default email for commits</summary>
    public string? Email { get; set; }
    
    /// <summary>Default name for commits</summary>
    public string? Name { get; set; }
}
