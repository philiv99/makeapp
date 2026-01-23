using MakeApp.Core.Enums;

namespace MakeApp.Application.DTOs;

/// <summary>
/// Request to create a new feature (Application layer)
/// </summary>
public class CreateFeatureRequestDto
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string RepositoryOwner { get; set; } = "";
    public string RepositoryName { get; set; } = "";
    public List<string>? AcceptanceCriteria { get; set; }
    public List<string>? TechnicalNotes { get; set; }
    public FeaturePriority Priority { get; set; } = FeaturePriority.Medium;
    public string? BaseBranch { get; set; }
}

/// <summary>
/// Response for feature operations
/// </summary>
public class FeatureResponse
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> AcceptanceCriteria { get; set; } = new();
    public List<string> TechnicalNotes { get; set; } = new();
    public FeaturePriority Priority { get; set; }
    public FeatureStatus Status { get; set; }
    public string? RepositoryOwner { get; set; }
    public string? RepositoryName { get; set; }
    public string BaseBranch { get; set; } = "main";
    public string? FeatureBranch { get; set; }
    public PlanSummary? Plan { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Feature status response
/// </summary>
public class FeatureStatusResponse
{
    public string Id { get; set; } = "";
    public FeatureStatus Status { get; set; }
    public ProgressInfo? Progress { get; set; }
    public string? WorkflowId { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Summary of an implementation plan
/// </summary>
public class PlanSummary
{
    public string Id { get; set; } = "";
    public int TotalPhases { get; set; }
    public int CurrentPhase { get; set; }
    public string EstimatedDuration { get; set; } = "";
    public string Status { get; set; } = "";
}
