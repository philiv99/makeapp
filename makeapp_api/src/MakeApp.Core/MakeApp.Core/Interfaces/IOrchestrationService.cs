using MakeApp.Core.Entities;

namespace MakeApp.Core.Interfaces;

/// <summary>
/// Service interface for workflow orchestration
/// </summary>
public interface IOrchestrationService
{
    /// <summary>Start a new workflow</summary>
    Task<Workflow> StartWorkflowAsync(StartWorkflowRequest dto);
    
    /// <summary>Get a workflow by ID</summary>
    Task<Workflow?> GetWorkflowAsync(string id);
    
    /// <summary>Stream workflow events</summary>
    IAsyncEnumerable<WorkflowEvent> StreamWorkflowEventsAsync(string id, CancellationToken cancellationToken);
    
    /// <summary>Abort a workflow</summary>
    Task<Workflow> AbortWorkflowAsync(string id);
    
    /// <summary>Retry the current step</summary>
    Task<Workflow> RetryStepAsync(string id);
    
    /// <summary>Skip the current step</summary>
    Task<Workflow> SkipStepAsync(string id);
    
    /// <summary>Get the implementation plan for a workflow</summary>
    Task<IEnumerable<WorkflowStep>> GetImplementationPlanAsync(string id);
    
    /// <summary>List active workflows</summary>
    Task<IEnumerable<Workflow>> ListActiveWorkflowsAsync();
}

/// <summary>
/// Request to start a workflow
/// </summary>
public class StartWorkflowRequest
{
    /// <summary>App ID (if creating new app)</summary>
    public string? AppId { get; set; }
    
    /// <summary>Feature ID (if adding feature)</summary>
    public string? FeatureId { get; set; }
    
    /// <summary>Repository path</summary>
    public string RepositoryPath { get; set; } = "";
    
    /// <summary>Maximum iterations</summary>
    public int? MaxIterations { get; set; }
    
    /// <summary>Additional context to include</summary>
    public string? AdditionalContext { get; set; }
    
    /// <summary>Session configuration</summary>
    public CreateSessionRequest? SessionConfig { get; set; }
    
    /// <summary>Whether to use memory system</summary>
    public bool UseMemory { get; set; } = true;
    
    /// <summary>Whether to store new memories</summary>
    public bool StoreNewMemories { get; set; } = true;
}

/// <summary>
/// Workflow event for streaming
/// </summary>
public class WorkflowEvent
{
    /// <summary>Event type</summary>
    public string Type { get; set; } = "";
    
    /// <summary>Event data</summary>
    public object? Data { get; set; }
    
    /// <summary>When the event occurred</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>Event message</summary>
    public string? Message { get; set; }
}
