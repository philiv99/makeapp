using MakeApp.Core.Enums;

namespace MakeApp.Core.Entities;

/// <summary>
/// Represents a feature to be implemented in an application
/// </summary>
public class Feature
{
    /// <summary>Unique identifier for the feature</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    
    /// <summary>Feature title</summary>
    public string Title { get; set; } = "";
    
    /// <summary>Detailed description of the feature</summary>
    public string Description { get; set; } = "";
    
    /// <summary>List of acceptance criteria</summary>
    public List<string> AcceptanceCriteria { get; set; } = new();
    
    /// <summary>Technical implementation notes</summary>
    public List<string> TechnicalNotes { get; set; } = new();
    
    /// <summary>Areas of the codebase affected by this feature</summary>
    public List<string> AffectedAreas { get; set; } = new();
    
    /// <summary>Priority level of the feature</summary>
    public FeaturePriority Priority { get; set; } = FeaturePriority.Medium;
    
    /// <summary>Current status of the feature</summary>
    public FeatureStatus Status { get; set; } = FeatureStatus.Draft;
    
    /// <summary>Repository owner</summary>
    public string? RepositoryOwner { get; set; }
    
    /// <summary>Repository name</summary>
    public string? RepositoryName { get; set; }
    
    /// <summary>Base branch for the feature</summary>
    public string BaseBranch { get; set; } = "main";
    
    /// <summary>Feature branch name</summary>
    public string? FeatureBranch { get; set; }
    
    /// <summary>When the feature was created</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>When the feature was last updated</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
