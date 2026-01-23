namespace MakeApp.Core.Enums;

/// <summary>
/// Status of an application being created or managed by MakeApp
/// </summary>
public enum AppStatus
{
    /// <summary>App creation pending</summary>
    Pending,
    
    /// <summary>Repository initialization in progress</summary>
    Initializing,
    
    /// <summary>Implementation plan being generated</summary>
    Planning,
    
    /// <summary>Agent configuration being set up</summary>
    ConfiguringAgents,
    
    /// <summary>Phased implementation in progress</summary>
    Implementing,
    
    /// <summary>All phases complete, finalizing</summary>
    Finalizing,
    
    /// <summary>App creation completed successfully</summary>
    Complete,
    
    /// <summary>App creation failed</summary>
    Failed,
    
    /// <summary>App creation was aborted</summary>
    Aborted
}
