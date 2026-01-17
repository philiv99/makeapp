using MakeApp.Core.Enums;

namespace MakeApp.Application.DTOs;

/// <summary>
/// Request to create a new app
/// </summary>
public class CreateAppRequest
{
    public string Name { get; set; } = "";
    public string Requirements { get; set; } = "";
    public string? Description { get; set; }
    public string? ProjectType { get; set; }
    public string? Owner { get; set; }
    public bool Private { get; set; } = true;
    public string? AdditionalContext { get; set; }
}

/// <summary>
/// Response for app operations
/// </summary>
public class AppResponse
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Owner { get; set; } = "";
    public string Description { get; set; } = "";
    public AppStatus Status { get; set; }
    public string RepositoryUrl { get; set; } = "";
    public string LocalPath { get; set; } = "";
    public string CurrentBranch { get; set; } = "";
    public string? WorkflowId { get; set; }
    public string? PlanId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// App status response
/// </summary>
public class AppStatusResponse
{
    public string Id { get; set; } = "";
    public AppStatus Status { get; set; }
    public ProgressInfo? Progress { get; set; }
    public string? WorkflowId { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Progress information
/// </summary>
public class ProgressInfo
{
    public int CurrentPhase { get; set; }
    public int TotalPhases { get; set; }
    public int PercentComplete { get; set; }
    public string? CurrentTask { get; set; }
}

/// <summary>
/// Workflow options
/// </summary>
public class WorkflowOptionsDto
{
    public int? MaxIterations { get; set; }
    public bool UseMemory { get; set; } = true;
    public bool StoreNewMemories { get; set; } = true;
    public string? AdditionalContext { get; set; }
}
