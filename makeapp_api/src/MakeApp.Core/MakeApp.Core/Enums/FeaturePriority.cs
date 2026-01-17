namespace MakeApp.Core.Enums;

/// <summary>
/// Priority level of a feature
/// </summary>
public enum FeaturePriority
{
    /// <summary>Low priority</summary>
    Low,
    
    /// <summary>Medium priority (default)</summary>
    Medium,
    
    /// <summary>High priority</summary>
    High,
    
    /// <summary>Critical priority - must be done immediately</summary>
    Critical
}
