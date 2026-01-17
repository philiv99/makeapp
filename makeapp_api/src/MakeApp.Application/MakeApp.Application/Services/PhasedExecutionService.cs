using MakeApp.Core.Entities;
using MakeApp.Core.Enums;
using MakeApp.Core.Interfaces;
using CoreTaskStatus = MakeApp.Core.Enums.TaskStatus;

namespace MakeApp.Application.Services;

/// <summary>
/// Service for phased execution of implementation plans
/// </summary>
public class PhasedExecutionService
{
    private readonly ICopilotService _copilotService;
    private readonly IGitService _gitService;
    private readonly PlanGeneratorService _planGenerator;

    public PhasedExecutionService(
        ICopilotService copilotService,
        IGitService gitService,
        PlanGeneratorService planGenerator)
    {
        _copilotService = copilotService;
        _gitService = gitService;
        _planGenerator = planGenerator;
    }

    /// <summary>
    /// Execute a single phase of the plan
    /// </summary>
    public async Task<PhaseExecutionResult> ExecutePhaseAsync(
        ImplementationPlan plan,
        int phaseNumber,
        AgentConfiguration agentConfig,
        CancellationToken cancellationToken = default)
    {
        var phase = plan.Phases.FirstOrDefault(p => p.Phase == phaseNumber);
        if (phase == null)
        {
            return new PhaseExecutionResult
            {
                Success = false,
                ErrorMessage = $"Phase {phaseNumber} not found"
            };
        }

        phase.Status = PhaseStatus.InProgress;
        phase.StartedAt = DateTime.UtcNow;

        var result = new PhaseExecutionResult
        {
            PhaseNumber = phaseNumber,
            PhaseName = phase.Name
        };

        try
        {
            // Create a session for this phase
            var sessionId = await _copilotService.CreateSessionAsync(new CreateSessionRequest
            {
                RepositoryPath = plan.RepositoryPath,
                Streaming = false
            });

            try
            {
                // Execute each task
                foreach (var task in phase.Tasks)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    var taskResult = await ExecuteTaskAsync(sessionId, task, plan.RepositoryPath, cancellationToken);
                    result.TaskResults.Add(taskResult);

                    if (!taskResult.Success)
                    {
                        await CommitPhaseProgressAsync(plan.RepositoryPath, phase);
                        result.Success = false;
                        result.ErrorMessage = $"Task {task.Id} failed: {taskResult.ErrorMessage}";
                        phase.Status = PhaseStatus.Failed;
                        return result;
                    }
                }

                await CommitPhaseProgressAsync(plan.RepositoryPath, phase);
                phase.Status = PhaseStatus.Completed;
                phase.CompletedAt = DateTime.UtcNow;
                result.Success = true;
            }
            finally
            {
                await _copilotService.CloseSessionAsync(sessionId);
            }
        }
        catch (Exception ex)
        {
            phase.Status = PhaseStatus.Failed;
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Execute the entire plan
    /// </summary>
    public async Task<PlanExecutionResult> ExecutePlanAsync(
        ImplementationPlan plan,
        AgentConfiguration agentConfig,
        CancellationToken cancellationToken = default)
    {
        var result = new PlanExecutionResult { PlanId = plan.Id };

        foreach (var phase in plan.Phases.OrderBy(p => p.Phase))
        {
            if (cancellationToken.IsCancellationRequested) break;

            var phaseResult = await ExecutePhaseAsync(plan, phase.Phase, agentConfig, cancellationToken);
            result.PhaseResults.Add(phaseResult);

            if (!phaseResult.Success)
            {
                result.Success = false;
                result.ErrorMessage = phaseResult.ErrorMessage;
                await _planGenerator.UpdatePlanStatusAsync(plan, PlanStatus.Failed, cancellationToken);
                return result;
            }

            plan.CurrentPhase = phase.Phase + 1;
        }

        result.Success = true;
        await _planGenerator.UpdatePlanStatusAsync(plan, PlanStatus.Completed, cancellationToken);
        return result;
    }

    private async Task<TaskExecutionResult> ExecuteTaskAsync(
        string sessionId,
        PhaseTask task,
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        task.Status = CoreTaskStatus.InProgress;
        task.StartedAt = DateTime.UtcNow;
        task.Attempts++;

        var result = new TaskExecutionResult
        {
            TaskId = task.Id,
            Description = task.Description
        };

        try
        {
            var prompt = $"Execute task: {task.Description}\nFiles: {string.Join(", ", task.Files)}";
            var response = await _copilotService.SendMessageAsync(sessionId, prompt);

            if (string.IsNullOrEmpty(response.Content))
            {
                result.Success = false;
                result.ErrorMessage = "No response from Copilot";
                task.Status = CoreTaskStatus.Failed;
                task.ErrorMessage = result.ErrorMessage;
                return result;
            }

            task.Status = CoreTaskStatus.Completed;
            task.CompletedAt = DateTime.UtcNow;
            result.Success = true;
            result.Output = response.Content;
        }
        catch (Exception ex)
        {
            task.Status = CoreTaskStatus.Failed;
            task.ErrorMessage = ex.Message;
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    private async Task CommitPhaseProgressAsync(string repositoryPath, ImplementationPhase phase)
    {
        await _gitService.StageAllAsync(repositoryPath);
        var status = await _gitService.GetStatusAsync(repositoryPath);
        
        if (status.StagedCount > 0)
        {
            var message = $"Phase {phase.Phase}: {phase.Name}";
            await _gitService.CommitAsync(repositoryPath, message);
        }
    }
}

/// <summary>
/// Result of executing a phase
/// </summary>
public class PhaseExecutionResult
{
    public int PhaseNumber { get; set; }
    public string PhaseName { get; set; } = "";
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<TaskExecutionResult> TaskResults { get; set; } = new();
}

/// <summary>
/// Result of executing a task
/// </summary>
public class TaskExecutionResult
{
    public string TaskId { get; set; } = "";
    public string Description { get; set; } = "";
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Output { get; set; }
}

/// <summary>
/// Result of executing an entire plan
/// </summary>
public class PlanExecutionResult
{
    public string PlanId { get; set; } = "";
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<PhaseExecutionResult> PhaseResults { get; set; } = new();
}
