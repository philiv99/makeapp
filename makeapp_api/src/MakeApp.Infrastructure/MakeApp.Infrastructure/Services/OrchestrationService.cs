using MakeApp.Core.Entities;
using MakeApp.Core.Interfaces;

namespace MakeApp.Infrastructure.Services;

/// <summary>
/// Implementation of IOrchestrationService
/// Manages workflow orchestration and execution
/// </summary>
public class OrchestrationService : IOrchestrationService
{
    private readonly IGitService _gitService;
    private readonly IGitHubService _gitHubService;
    private readonly IBranchService _branchService;
    private readonly ISandboxService _sandboxService;
    private readonly ICopilotService _copilotService;
    private readonly Dictionary<string, Workflow> _workflows = new();

    public OrchestrationService(
        IGitService gitService,
        IGitHubService gitHubService,
        IBranchService branchService,
        ISandboxService sandboxService,
        ICopilotService copilotService)
    {
        _gitService = gitService;
        _gitHubService = gitHubService;
        _branchService = branchService;
        _sandboxService = sandboxService;
        _copilotService = copilotService;
    }

    /// <inheritdoc/>
    public Task<Workflow> StartWorkflowAsync(StartWorkflowRequest dto)
    {
        var workflow = new Workflow
        {
            FeatureId = dto.FeatureId ?? "",
            AppId = dto.AppId ?? "",
            CurrentPhase = Core.Enums.WorkflowPhase.Planning,
            Status = Core.Enums.PhaseStatus.InProgress,
            StartedAt = DateTime.UtcNow
        };

        _workflows[workflow.Id] = workflow;
        return Task.FromResult(workflow);
    }

    /// <inheritdoc/>
    public Task<Workflow?> GetWorkflowAsync(string id)
    {
        _workflows.TryGetValue(id, out var workflow);
        return Task.FromResult(workflow);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<WorkflowEvent> StreamWorkflowEventsAsync(string id, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!_workflows.TryGetValue(id, out var workflow))
        {
            yield return new WorkflowEvent
            {
                Type = "error",
                Message = "Workflow not found",
                Timestamp = DateTime.UtcNow
            };
            yield break;
        }

        yield return new WorkflowEvent
        {
            Type = "started",
            Message = "Workflow started",
            Timestamp = DateTime.UtcNow,
            Data = new { workflowId = id, phase = workflow.CurrentPhase.ToString() }
        };

        // Simulate workflow execution with progress events
        while (workflow.Status == Core.Enums.PhaseStatus.InProgress && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000, cancellationToken);
            
            yield return new WorkflowEvent
            {
                Type = "progress",
                Message = $"Processing phase: {workflow.CurrentPhase}",
                Timestamp = DateTime.UtcNow,
                Data = new { phase = workflow.CurrentPhase.ToString() }
            };

            // Advance phase (simplified)
            if (workflow.CurrentPhase != Core.Enums.WorkflowPhase.Complete)
            {
                workflow.CurrentPhase = GetNextPhase(workflow.CurrentPhase);
            }
            else
            {
                workflow.Status = Core.Enums.PhaseStatus.Completed;
                workflow.CompletedAt = DateTime.UtcNow;
            }
        }

        yield return new WorkflowEvent
        {
            Type = "completed",
            Message = "Workflow completed",
            Timestamp = DateTime.UtcNow
        };
    }

    /// <inheritdoc/>
    public Task<Workflow> AbortWorkflowAsync(string id)
    {
        if (!_workflows.TryGetValue(id, out var workflow))
        {
            throw new InvalidOperationException($"Workflow {id} not found");
        }

        workflow.Status = Core.Enums.PhaseStatus.Failed;
        workflow.CompletedAt = DateTime.UtcNow;
        return Task.FromResult(workflow);
    }

    /// <inheritdoc/>
    public Task<Workflow> RetryStepAsync(string id)
    {
        if (!_workflows.TryGetValue(id, out var workflow))
        {
            throw new InvalidOperationException($"Workflow {id} not found");
        }

        // Reset current phase to restart
        workflow.Status = Core.Enums.PhaseStatus.InProgress;
        return Task.FromResult(workflow);
    }

    /// <inheritdoc/>
    public Task<Workflow> SkipStepAsync(string id)
    {
        if (!_workflows.TryGetValue(id, out var workflow))
        {
            throw new InvalidOperationException($"Workflow {id} not found");
        }

        // Move to next phase
        workflow.CurrentPhase = GetNextPhase(workflow.CurrentPhase);
        return Task.FromResult(workflow);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<WorkflowStep>> GetImplementationPlanAsync(string id)
    {
        if (!_workflows.TryGetValue(id, out var workflow))
        {
            return Task.FromResult<IEnumerable<WorkflowStep>>(Array.Empty<WorkflowStep>());
        }

        var steps = new List<WorkflowStep>
        {
            new() { Name = "Planning", Description = "Analyze requirements and create implementation plan", Order = 1, Status = GetStepStatus(workflow, Core.Enums.WorkflowPhase.Planning) },
            new() { Name = "Design", Description = "Design architecture and components", Order = 2, Status = GetStepStatus(workflow, Core.Enums.WorkflowPhase.Design) },
            new() { Name = "Implementation", Description = "Implement the feature code", Order = 3, Status = GetStepStatus(workflow, Core.Enums.WorkflowPhase.Implementation) },
            new() { Name = "Testing", Description = "Write and run tests", Order = 4, Status = GetStepStatus(workflow, Core.Enums.WorkflowPhase.Testing) },
            new() { Name = "Review", Description = "Review and refine code", Order = 5, Status = GetStepStatus(workflow, Core.Enums.WorkflowPhase.Review) },
        };

        return Task.FromResult<IEnumerable<WorkflowStep>>(steps);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<Workflow>> ListActiveWorkflowsAsync()
    {
        var activeWorkflows = _workflows.Values
            .Where(w => w.Status == Core.Enums.PhaseStatus.InProgress || w.Status == Core.Enums.PhaseStatus.Pending);
        return Task.FromResult(activeWorkflows);
    }

    private Core.Enums.WorkflowPhase GetNextPhase(Core.Enums.WorkflowPhase currentPhase)
    {
        return currentPhase switch
        {
            Core.Enums.WorkflowPhase.Planning => Core.Enums.WorkflowPhase.Design,
            Core.Enums.WorkflowPhase.Design => Core.Enums.WorkflowPhase.Implementation,
            Core.Enums.WorkflowPhase.Implementation => Core.Enums.WorkflowPhase.Testing,
            Core.Enums.WorkflowPhase.Testing => Core.Enums.WorkflowPhase.Review,
            Core.Enums.WorkflowPhase.Review => Core.Enums.WorkflowPhase.Complete,
            _ => Core.Enums.WorkflowPhase.Complete
        };
    }

    private Core.Enums.PhaseStatus GetStepStatus(Workflow workflow, Core.Enums.WorkflowPhase phase)
    {
        var currentPhaseIndex = (int)workflow.CurrentPhase;
        var phaseIndex = (int)phase;

        if (phaseIndex < currentPhaseIndex)
            return Core.Enums.PhaseStatus.Completed;
        if (phaseIndex == currentPhaseIndex)
            return workflow.Status == Core.Enums.PhaseStatus.InProgress ? Core.Enums.PhaseStatus.InProgress : Core.Enums.PhaseStatus.Pending;
        return Core.Enums.PhaseStatus.Pending;
    }
}
