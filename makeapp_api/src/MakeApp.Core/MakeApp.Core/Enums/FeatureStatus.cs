namespace MakeApp.Core.Enums;

/// <summary>
/// Status of a feature in the system
/// </summary>
public enum FeatureStatus
{
    /// <summary>Feature is in draft state, not yet submitted</summary>
    Draft,
    
    /// <summary>Feature is ready to be implemented</summary>
    Ready,
    
    /// <summary>Feature implementation is in progress</summary>
    InProgress,
    
    /// <summary>Feature is in code review</summary>
    InReview,
    
    /// <summary>Feature implementation is complete</summary>
    Complete,
    
    /// <summary>Feature was cancelled</summary>
    Cancelled
}
