using MakeApp.Core.Enums;

namespace MakeApp.Core.Entities;

/// <summary>
/// Represents a task within an implementation phase
/// </summary>
public class PhaseTask
{
    /// <summary>Task identifier (e.g., "1.1", "2.3")</summary>
    public string Id { get; set; } = "";
    
    /// <summary>Description of the task</summary>
    public string Description { get; set; } = "";
    
    /// <summary>Files to create or modify</summary>
    public List<string> Files { get; set; } = new();
    
    /// <summary>Integration points with existing code</summary>
    public List<string> IntegrationPoints { get; set; } = new();
    
    /// <summary>Agent role responsible for this task</summary>
    public AgentRole AgentRole { get; set; } = AgentRole.Coder;
    
    /// <summary>Task complexity (simple, moderate, complex)</summary>
    public string Complexity { get; set; } = "moderate";
    
    /// <summary>Current status of the task</summary>
    public Enums.TaskStatus Status { get; set; } = Enums.TaskStatus.NotStarted;
    
    /// <summary>Additional context for the task (e.g., feedback from previous attempt)</summary>
    public string? Context { get; set; }
    
    /// <summary>Number of attempts made</summary>
    public int Attempts { get; set; }
    
    /// <summary>When the task started</summary>
    public DateTime? StartedAt { get; set; }
    
    /// <summary>When the task completed</summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>Error message if task failed</summary>
    public string? ErrorMessage { get; set; }
}
