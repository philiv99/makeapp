using MakeApp.Core.Enums;

namespace MakeApp.Core.Entities;

/// <summary>
/// Represents an implementation plan for an app or feature
/// </summary>
public class ImplementationPlan
{
    /// <summary>Unique identifier for the plan</summary>
    public string Id { get; set; } = $"plan_{Guid.NewGuid():N}"[..12];
    
    /// <summary>Associated app ID (if creating new app)</summary>
    public string? AppId { get; set; }
    
    /// <summary>Associated feature ID (if adding feature)</summary>
    public string? FeatureId { get; set; }
    
    /// <summary>Repository path</summary>
    public string RepositoryPath { get; set; } = "";
    
    /// <summary>Total number of phases</summary>
    public int TotalPhases => Phases.Count;
    
    /// <summary>Estimated duration</summary>
    public string EstimatedDuration { get; set; } = "";
    
    /// <summary>List of implementation phases</summary>
    public List<ImplementationPhase> Phases { get; set; } = new();
    
    /// <summary>Current phase number (1-indexed)</summary>
    public int CurrentPhase { get; set; } = 1;
    
    /// <summary>Plan status</summary>
    public PlanStatus Status { get; set; } = PlanStatus.Active;
    
    /// <summary>When the plan was created</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>When the plan was last updated</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>When the plan was completed</summary>
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Status of an implementation plan
/// </summary>
public enum PlanStatus
{
    /// <summary>Plan is active</summary>
    Active,
    
    /// <summary>Plan is paused</summary>
    Paused,
    
    /// <summary>Plan completed successfully</summary>
    Completed,
    
    /// <summary>Plan failed</summary>
    Failed,
    
    /// <summary>Plan was cancelled</summary>
    Cancelled
}
