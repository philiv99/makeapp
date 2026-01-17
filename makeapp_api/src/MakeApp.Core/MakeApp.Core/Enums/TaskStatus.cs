namespace MakeApp.Core.Enums;

/// <summary>
/// Status of a task within a phase
/// </summary>
public enum TaskStatus
{
    /// <summary>Task has not started</summary>
    NotStarted,
    
    /// <summary>Task is currently being worked on</summary>
    InProgress,
    
    /// <summary>Task is in code review</summary>
    InReview,
    
    /// <summary>Task completed successfully</summary>
    Completed,
    
    /// <summary>Task failed</summary>
    Failed,
    
    /// <summary>Task was skipped</summary>
    Skipped,
    
    /// <summary>Task is being retried after failure</summary>
    Retrying
}
