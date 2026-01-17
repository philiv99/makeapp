using MakeApp.Core.Entities;
using MakeApp.Core.Enums;

namespace MakeApp.Core.Interfaces;

/// <summary>
/// Service interface for generating implementation plans using LLM
/// </summary>
public interface IPlanGeneratorService
{
    /// <summary>
    /// Generate an implementation plan for the given requirements
    /// </summary>
    Task<ImplementationPlan> GeneratePlanAsync(PlanRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Load an existing plan from a repository
    /// </summary>
    Task<ImplementationPlan?> LoadPlanAsync(string repositoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save a plan to a repository
    /// </summary>
    Task SavePlanAsync(ImplementationPlan plan, string repositoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update the status of a task in the plan
    /// </summary>
    Task<ImplementationPlan> UpdateTaskStatusAsync(string repositoryPath, string taskId, Enums.TaskStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the current workflow status for a repository
    /// </summary>
    Task<WorkflowStatus?> GetWorkflowStatusAsync(string repositoryPath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request to generate an implementation plan
/// </summary>
public class PlanRequest
{
    /// <summary>Associated app ID (if creating new app)</summary>
    public string? AppId { get; set; }

    /// <summary>Associated feature ID (if adding feature)</summary>
    public string? FeatureId { get; set; }

    /// <summary>Path to the repository</summary>
    public string RepositoryPath { get; set; } = "";

    /// <summary>Requirements to plan for</summary>
    public string Requirements { get; set; } = "";

    /// <summary>Project type</summary>
    public string ProjectType { get; set; } = "";

    /// <summary>Existing context from the repository</summary>
    public string? ExistingContext { get; set; }

    /// <summary>Additional instructions for planning</summary>
    public string? AdditionalInstructions { get; set; }
}

/// <summary>
/// Current workflow status for a repository
/// </summary>
public class WorkflowStatus
{
    /// <summary>Current phase number (1-based)</summary>
    public int CurrentPhase { get; set; }

    /// <summary>Current task ID</summary>
    public string CurrentTask { get; set; } = "";

    /// <summary>Overall status</summary>
    public string Status { get; set; } = "not-started";

    /// <summary>When the workflow started</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>When the workflow completed</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Error message if failed</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Number of completed tasks</summary>
    public int CompletedTasks { get; set; }

    /// <summary>Total number of tasks</summary>
    public int TotalTasks { get; set; }
}
