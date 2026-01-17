namespace MakeApp.Core.Enums;

/// <summary>
/// Status of an implementation phase
/// </summary>
public enum PhaseStatus
{
    /// <summary>Phase is pending</summary>
    Pending,
    
    /// <summary>Phase has not started</summary>
    NotStarted,
    
    /// <summary>Phase is currently in progress</summary>
    InProgress,
    
    /// <summary>Phase is waiting for user input or approval</summary>
    Blocked,
    
    /// <summary>Phase completed successfully</summary>
    Completed,
    
    /// <summary>Phase failed</summary>
    Failed,
    
    /// <summary>Phase was skipped</summary>
    Skipped
}
