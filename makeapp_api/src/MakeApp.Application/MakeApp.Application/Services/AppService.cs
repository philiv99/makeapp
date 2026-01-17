using MakeApp.Core.Entities;
using MakeApp.Core.Enums;
using MakeApp.Core.Interfaces;
using MakeApp.Application.DTOs;
using DtoCreateAppRequest = MakeApp.Application.DTOs.CreateAppRequest;
using DtoAppResponse = MakeApp.Application.DTOs.AppResponse;

namespace MakeApp.Application.Services;

/// <summary>
/// Application service for managing apps
/// </summary>
public class AppService
{
    private readonly IOrchestrationService _orchestrationService;
    private readonly IGitHubService _gitHubService;
    private readonly IGitService _gitService;
    private readonly IBranchService _branchService;
    private readonly ISandboxService _sandboxService;

    public AppService(
        IOrchestrationService orchestrationService,
        IGitHubService gitHubService,
        IGitService gitService,
        IBranchService branchService,
        ISandboxService sandboxService)
    {
        _orchestrationService = orchestrationService;
        _gitHubService = gitHubService;
        _gitService = gitService;
        _branchService = branchService;
        _sandboxService = sandboxService;
    }

    /// <summary>
    /// Create a new app
    /// </summary>
    public async Task<DtoAppResponse> CreateAppAsync(DtoCreateAppRequest request, CancellationToken cancellationToken = default)
    {
        // Get sandbox info
        var sandboxInfo = await _sandboxService.GetSandboxInfoAsync();

        // Get current user for repository owner
        var currentUser = await _gitHubService.GetCurrentUserAsync();
        var owner = request.Owner ?? currentUser.Login;

        // Create GitHub repository
        var createRepoOptions = new CreateRepoOptions
        {
            Name = request.Name,
            Owner = owner,
            Description = request.Description,
            Private = request.Private,
            AutoInit = true
        };
        var repoInfo = await _gitHubService.CreateRepositoryAsync(createRepoOptions);

        // Clone to sandbox
        var localPath = Path.Combine(sandboxInfo.Path, request.Name);
        await _gitService.CloneAsync(repoInfo.CloneUrl ?? repoInfo.HtmlUrl, localPath);

        // Create feature branch
        await _branchService.CreateFeatureBranchAsync(localPath, "initial-setup", "main");
        await _branchService.SwitchToBranchAsync(localPath, "feature/initial-setup");

        // Create app entity
        var app = new App
        {
            Name = request.Name,
            Owner = owner,
            Description = request.Description ?? "",
            Requirements = request.Requirements,
            ProjectType = request.ProjectType ?? "node",
            LocalPath = localPath,
            RepositoryUrl = repoInfo.HtmlUrl,
            CurrentBranch = "feature/initial-setup",
            Status = AppStatus.Initializing
        };

        // Start workflow
        var workflowRequest = new StartWorkflowRequest
        {
            AppId = app.Id,
            RepositoryPath = localPath,
            AdditionalContext = request.AdditionalContext,
            UseMemory = true,
            StoreNewMemories = true
        };

        var workflow = await _orchestrationService.StartWorkflowAsync(workflowRequest);
        app.WorkflowId = workflow.Id;
        app.Status = AppStatus.Planning;

        return MapToResponse(app);
    }

    /// <summary>
    /// Get app status
    /// </summary>
    public async Task<AppStatusResponse> GetAppStatusAsync(string appId, CancellationToken cancellationToken = default)
    {
        // In a real implementation, this would fetch from a repository
        return await Task.FromResult(new AppStatusResponse
        {
            Id = appId,
            Status = AppStatus.Pending,
            Progress = new ProgressInfo
            {
                CurrentPhase = 1,
                TotalPhases = 5,
                PercentComplete = 0
            }
        });
    }

    /// <summary>
    /// Delete an app
    /// </summary>
    public async Task<bool> DeleteAppAsync(string appId, bool deleteGitHub = false, CancellationToken cancellationToken = default)
    {
        // In a real implementation, would delete from repository and optionally GitHub
        return await Task.FromResult(true);
    }

    private static DtoAppResponse MapToResponse(App app)
    {
        return new DtoAppResponse
        {
            Id = app.Id,
            Name = app.Name,
            Owner = app.Owner,
            Description = app.Description,
            Status = app.Status,
            RepositoryUrl = app.RepositoryUrl,
            LocalPath = app.LocalPath,
            CurrentBranch = app.CurrentBranch,
            WorkflowId = app.WorkflowId,
            PlanId = app.PlanId,
            CreatedAt = app.CreatedAt,
            UpdatedAt = app.UpdatedAt
        };
    }
}
