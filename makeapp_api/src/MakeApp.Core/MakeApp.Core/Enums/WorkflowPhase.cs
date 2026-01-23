namespace MakeApp.Core.Enums;

/// <summary>
/// Phase of a workflow execution
/// </summary>
public enum WorkflowPhase
{
    /// <summary>Workflow is pending start</summary>
    Pending,
    
    /// <summary>Workflow is in planning phase</summary>
    Planning,
    
    /// <summary>Workflow is in design phase</summary>
    Design,
    
    /// <summary>Workflow is implementing features</summary>
    Implementation,
    
    /// <summary>Workflow is in testing phase</summary>
    Testing,
    
    /// <summary>Workflow is validating implementation</summary>
    Validation,
    
    /// <summary>Workflow is in review phase</summary>
    Review,
    
    /// <summary>Workflow completed successfully</summary>
    Complete,
    
    /// <summary>Workflow failed</summary>
    Failed,
    
    /// <summary>Workflow was aborted by user</summary>
    Aborted
}
