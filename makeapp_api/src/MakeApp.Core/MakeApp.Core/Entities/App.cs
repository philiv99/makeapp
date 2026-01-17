using MakeApp.Core.Enums;

namespace MakeApp.Core.Entities;

/// <summary>
/// Represents an application being created or managed by MakeApp
/// </summary>
public class App
{
    /// <summary>Unique identifier for the app</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..12];
    
    /// <summary>Name of the application</summary>
    public string Name { get; set; } = "";
    
    /// <summary>GitHub owner (user or organization)</summary>
    public string Owner { get; set; } = "";
    
    /// <summary>Application description</summary>
    public string Description { get; set; } = "";
    
    /// <summary>Original requirements used to create the app</summary>
    public string Requirements { get; set; } = "";
    
    /// <summary>Project type (e.g., node, dotnet, python)</summary>
    public string ProjectType { get; set; } = "";
    
    /// <summary>Local path to the repository in sandbox</summary>
    public string LocalPath { get; set; } = "";
    
    /// <summary>GitHub repository URL</summary>
    public string RepositoryUrl { get; set; } = "";
    
    /// <summary>Current branch name</summary>
    public string CurrentBranch { get; set; } = "main";
    
    /// <summary>Current app status</summary>
    public AppStatus Status { get; set; } = AppStatus.Pending;
    
    /// <summary>Associated workflow ID</summary>
    public string? WorkflowId { get; set; }
    
    /// <summary>Associated implementation plan ID</summary>
    public string? PlanId { get; set; }
    
    /// <summary>When the app was created</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>When the app was last updated</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>When app creation completed</summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>Error message if status is Failed</summary>
    public string? ErrorMessage { get; set; }
}
