using MakeApp.Core.Entities;
using MakeApp.Core.Enums;
using MakeApp.Core.Interfaces;
using System.Text.Json;

namespace MakeApp.Application.Services;

/// <summary>
/// Service for generating implementation plans using Copilot
/// </summary>
public class PlanGeneratorService
{
    private readonly ICopilotService _copilotService;
    private readonly IFileSystem _fileSystem;

    public PlanGeneratorService(ICopilotService copilotService, IFileSystem fileSystem)
    {
        _copilotService = copilotService;
        _fileSystem = fileSystem;
    }

    /// <summary>
    /// Generate an implementation plan for the given requirements
    /// </summary>
    public async Task<ImplementationPlan> GeneratePlanAsync(
        string? appId,
        string? featureId,
        string repositoryPath,
        string requirements,
        CancellationToken cancellationToken = default)
    {
        // Create a Copilot session for plan generation
        var sessionRequest = new CreateSessionRequest
        {
            Model = "gpt-5",
            Streaming = false,
            RepositoryPath = repositoryPath
        };

        var sessionId = await _copilotService.CreateSessionAsync(sessionRequest);

        try
        {
            // Build the prompt for plan generation
            var prompt = BuildPlanGenerationPrompt(requirements);

            // Get the plan from Copilot
            var response = await _copilotService.SendMessageAsync(sessionId, prompt);

            // Parse the response into a plan
            var plan = ParsePlanFromResponse(response.Content, appId, featureId, repositoryPath);

            // Save the plan
            await SavePlanAsync(plan, repositoryPath);

            return plan;
        }
        finally
        {
            await _copilotService.CloseSessionAsync(sessionId);
        }
    }

    /// <summary>
    /// Load a plan from file
    /// </summary>
    public async Task<ImplementationPlan?> LoadPlanAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        var planPath = _fileSystem.Path.Combine(repositoryPath, ".makeapp", "plan.json");

        if (!_fileSystem.File.Exists(planPath))
        {
            return null;
        }

        var json = await _fileSystem.File.ReadAllTextAsync(planPath, cancellationToken);
        return JsonSerializer.Deserialize<ImplementationPlan>(json);
    }

    /// <summary>
    /// Update plan status
    /// </summary>
    public async Task<ImplementationPlan> UpdatePlanStatusAsync(
        ImplementationPlan plan,
        PlanStatus status,
        CancellationToken cancellationToken = default)
    {
        plan.Status = status;
        plan.UpdatedAt = DateTime.UtcNow;

        if (status == PlanStatus.Completed)
        {
            plan.CompletedAt = DateTime.UtcNow;
        }

        await SavePlanAsync(plan, plan.RepositoryPath);
        return plan;
    }

    private static string BuildPlanGenerationPrompt(string requirements)
    {
        var jsonSchema = """
            {
              "phases": [
                {
                  "name": "Phase 1: Foundation",
                  "description": "Set up basic structure",
                  "tasks": [
                    {
                      "id": "1.1",
                      "description": "Create project structure",
                      "files": ["src/index.ts"],
                      "complexity": "simple"
                    }
                  ]
                }
              ],
              "estimatedDuration": "2-3 hours"
            }
            """;

        return $"""
            Analyze the following requirements and create a detailed implementation plan.
            
            ## Requirements
            {requirements}
            
            ## Instructions
            Create a phased implementation plan. Return as JSON matching this schema:
            {jsonSchema}
            """;
    }

    private static ImplementationPlan ParsePlanFromResponse(string response, string? appId, string? featureId, string repositoryPath)
    {
        var plan = new ImplementationPlan
        {
            AppId = appId,
            FeatureId = featureId,
            RepositoryPath = repositoryPath,
            Status = PlanStatus.Active
        };

        try
        {
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var parsed = JsonSerializer.Deserialize<PlanJsonResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (parsed?.Phases != null)
                {
                    plan.Phases = parsed.Phases.Select((p, idx) => new ImplementationPhase
                    {
                        Phase = idx + 1,
                        Name = p.Name ?? $"Phase {idx + 1}",
                        Description = p.Description ?? "",
                        Status = PhaseStatus.NotStarted,
                        Tasks = p.Tasks?.Select(t => new PhaseTask
                        {
                            Id = t.Id ?? "",
                            Description = t.Description ?? "",
                            Files = t.Files ?? new List<string>(),
                            Complexity = t.Complexity ?? "moderate",
                            Status = Core.Enums.TaskStatus.NotStarted,
                            AgentRole = AgentRole.Coder
                        }).ToList() ?? new List<PhaseTask>()
                    }).ToList();

                    plan.EstimatedDuration = parsed.EstimatedDuration ?? "Unknown";
                }
            }
        }
        catch
        {
            // Create default plan on parse failure
            plan.Phases = new List<ImplementationPhase>
            {
                new()
                {
                    Phase = 1,
                    Name = "Implementation",
                    Description = "Implement the requirements",
                    Status = PhaseStatus.NotStarted,
                    Tasks = new List<PhaseTask>
                    {
                        new()
                        {
                            Id = "1.1",
                            Description = "Implement requirements",
                            Status = Core.Enums.TaskStatus.NotStarted,
                            AgentRole = AgentRole.Coder
                        }
                    }
                }
            };
            plan.EstimatedDuration = "Unknown";
        }

        return plan;
    }

    private async Task SavePlanAsync(ImplementationPlan plan, string repositoryPath)
    {
        var makeappPath = _fileSystem.Path.Combine(repositoryPath, ".makeapp");
        _fileSystem.Directory.CreateDirectory(makeappPath);

        var planPath = _fileSystem.Path.Combine(makeappPath, "plan.json");
        var json = JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true });

        await _fileSystem.File.WriteAllTextAsync(planPath, json);
    }

    private class PlanJsonResponse
    {
        public List<PhaseJson>? Phases { get; set; }
        public string? EstimatedDuration { get; set; }
    }

    private class PhaseJson
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<TaskJson>? Tasks { get; set; }
    }

    private class TaskJson
    {
        public string? Id { get; set; }
        public string? Description { get; set; }
        public List<string>? Files { get; set; }
        public string? Complexity { get; set; }
    }
}
