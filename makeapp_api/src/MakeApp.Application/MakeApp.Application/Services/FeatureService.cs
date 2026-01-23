using MakeApp.Core.Entities;
using MakeApp.Core.Enums;
using MakeApp.Core.Interfaces;
using MakeApp.Application.DTOs;

namespace MakeApp.Application.Services;

/// <summary>
/// Application service for managing features
/// </summary>
public class FeatureService
{
    private readonly IOrchestrationService _orchestrationService;
    private readonly IBranchService _branchService;
    private readonly IRepositoryService _repositoryService;

    public FeatureService(
        IOrchestrationService orchestrationService,
        IBranchService branchService,
        IRepositoryService repositoryService)
    {
        _orchestrationService = orchestrationService;
        _branchService = branchService;
        _repositoryService = repositoryService;
    }

    /// <summary>
    /// Create a new feature
    /// </summary>
    public async Task<FeatureResponse> CreateFeatureAsync(CreateFeatureRequestDto request, CancellationToken cancellationToken = default)
    {
        // Get repository path
        var repoPath = await _repositoryService.GetRepositoryPathAsync(request.RepositoryOwner, request.RepositoryName);
        if (string.IsNullOrEmpty(repoPath))
        {
            throw new InvalidOperationException($"Repository {request.RepositoryOwner}/{request.RepositoryName} not found");
        }

        // Create feature entity
        var feature = new Feature
        {
            Title = request.Title,
            Description = request.Description ?? "",
            AcceptanceCriteria = request.AcceptanceCriteria ?? new List<string>(),
            TechnicalNotes = request.TechnicalNotes ?? new List<string>(),
            Priority = request.Priority,
            RepositoryOwner = request.RepositoryOwner,
            RepositoryName = request.RepositoryName,
            BaseBranch = request.BaseBranch ?? "main",
            Status = FeatureStatus.Draft
        };

        // Create feature branch
        var branchName = _branchService.FormatFeatureBranchName(feature.Title);
        await _branchService.CreateFeatureBranchAsync(repoPath, branchName, feature.BaseBranch);
        await _branchService.SwitchToBranchAsync(repoPath, branchName);
        feature.FeatureBranch = branchName;
        feature.Status = FeatureStatus.Ready;

        return MapToResponse(feature);
    }

    /// <summary>
    /// Start implementing a feature
    /// </summary>
    public async Task<FeatureResponse> StartFeatureAsync(string featureId, CancellationToken cancellationToken = default)
    {
        // In a real implementation, this would fetch the feature from repository
        var feature = new Feature
        {
            Id = featureId,
            Title = "Feature",
            Status = FeatureStatus.InProgress
        };

        return await Task.FromResult(MapToResponse(feature));
    }

    /// <summary>
    /// Get feature status
    /// </summary>
    public async Task<FeatureStatusResponse> GetFeatureStatusAsync(string featureId, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(new FeatureStatusResponse
        {
            Id = featureId,
            Status = FeatureStatus.Draft,
            Progress = new ProgressInfo
            {
                CurrentPhase = 1,
                TotalPhases = 3,
                PercentComplete = 0
            }
        });
    }

    /// <summary>
    /// Cancel a feature
    /// </summary>
    public async Task<bool> CancelFeatureAsync(string featureId, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(true);
    }

    private static FeatureResponse MapToResponse(Feature feature)
    {
        return new FeatureResponse
        {
            Id = feature.Id,
            Title = feature.Title,
            Description = feature.Description,
            AcceptanceCriteria = feature.AcceptanceCriteria,
            TechnicalNotes = feature.TechnicalNotes,
            Priority = feature.Priority,
            Status = feature.Status,
            RepositoryOwner = feature.RepositoryOwner,
            RepositoryName = feature.RepositoryName,
            BaseBranch = feature.BaseBranch,
            FeatureBranch = feature.FeatureBranch,
            CreatedAt = feature.CreatedAt,
            UpdatedAt = feature.UpdatedAt
        };
    }
}
