using MakeApp.Core.Enums;

namespace MakeApp.Core.Entities;

/// <summary>
/// Represents a workflow execution
/// </summary>
public class Workflow
{
    /// <summary>Unique identifier for the workflow</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    
    /// <summary>Associated app ID (if creating new app)</summary>
    public string? AppId { get; set; }
    
    /// <summary>Associated feature ID (if adding feature)</summary>
    public string? FeatureId { get; set; }
    
    /// <summary>Path to the repository</summary>
    public string RepositoryPath { get; set; } = "";
    
    /// <summary>Current workflow phase</summary>
    public WorkflowPhase Phase { get; set; } = WorkflowPhase.Pending;
    
    /// <summary>Current phase (alias for Phase)</summary>
    public WorkflowPhase CurrentPhase { get => Phase; set => Phase = value; }
    
    /// <summary>Workflow status</summary>
    public PhaseStatus Status { get; set; } = PhaseStatus.Pending;
    
    /// <summary>Remaining steps to execute</summary>
    public List<WorkflowStep> Steps { get; set; } = new();
    
    /// <summary>Completed steps</summary>
    public List<WorkflowStep> CompletedSteps { get; set; } = new();
    
    /// <summary>Errors that occurred during execution</summary>
    public List<WorkflowError> Errors { get; set; } = new();
    
    /// <summary>Current iteration count</summary>
    public int CurrentIteration { get; set; }
    
    /// <summary>Maximum allowed iterations</summary>
    public int MaxIterations { get; set; } = 50;
    
    /// <summary>When the workflow started</summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>When the workflow completed</summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>Associated Copilot session ID</summary>
    public string? CopilotSessionId { get; set; }
}

/// <summary>
/// Represents a step in a workflow
/// </summary>
public class WorkflowStep
{
    /// <summary>Step identifier</summary>
    public string Id { get; set; } = "";
    
    /// <summary>Step name</summary>
    public string Name { get; set; } = "";
    
    /// <summary>Description of the step</summary>
    public string Description { get; set; } = "";
    
    /// <summary>Type of step</summary>
    public string StepType { get; set; } = "";
    
    /// <summary>Step order</summary>
    public int Order { get; set; }
    
    /// <summary>Step status</summary>
    public PhaseStatus Status { get; set; } = PhaseStatus.Pending;
    
    /// <summary>Step parameters</summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
    
    /// <summary>Step result</summary>
    public object? Result { get; set; }
    
    /// <summary>When the step completed</summary>
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Represents an error in a workflow
/// </summary>
public class WorkflowError
{
    /// <summary>Step that caused the error</summary>
    public WorkflowStep? Step { get; set; }
    
    /// <summary>Error message</summary>
    public string Message { get; set; } = "";
    
    /// <summary>Iteration when error occurred</summary>
    public int Iteration { get; set; }
    
    /// <summary>When the error occurred</summary>
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
