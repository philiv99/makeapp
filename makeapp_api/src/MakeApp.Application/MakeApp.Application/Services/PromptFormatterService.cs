using MakeApp.Core.Entities;
using MakeApp.Core.Enums;
using MakeApp.Core.Interfaces;
using System.Text;

namespace MakeApp.Application.Services;

/// <summary>
/// Service for formatting prompts for Copilot
/// </summary>
public class PromptFormatterService
{
    private readonly IMemoryService _memoryService;

    public PromptFormatterService(IMemoryService memoryService)
    {
        _memoryService = memoryService;
    }

    /// <summary>
    /// Format a prompt for a task
    /// </summary>
    public async Task<string> FormatTaskPromptAsync(
        PhaseTask task,
        string repositoryPath,
        CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Task");
        sb.AppendLine($"**ID:** {task.Id}");
        sb.AppendLine($"**Description:** {task.Description}");
        sb.AppendLine();

        if (task.Files.Count > 0)
        {
            sb.AppendLine("## Files to create/modify");
            foreach (var file in task.Files)
            {
                sb.AppendLine($"- {file}");
            }
            sb.AppendLine();
        }

        if (task.IntegrationPoints.Count > 0)
        {
            sb.AppendLine("## Integration Points");
            foreach (var point in task.IntegrationPoints)
            {
                sb.AppendLine($"- {point}");
            }
            sb.AppendLine();
        }

        if (!string.IsNullOrEmpty(task.Context))
        {
            sb.AppendLine("## Additional Context");
            sb.AppendLine(task.Context);
            sb.AppendLine();
        }

        // Add relevant memories
        var memories = await GetRelevantMemoriesAsync(repositoryPath, task.Description, cancellationToken);
        if (memories.Count > 0)
        {
            sb.AppendLine("## Relevant Knowledge from Memory");
            foreach (var memory in memories)
            {
                sb.AppendLine($"### {memory.Subject}");
                sb.AppendLine(memory.Fact);
                if (memory.Citations.Count > 0)
                {
                    sb.AppendLine("**Citations:**");
                    foreach (var citation in memory.Citations)
                    {
                        sb.AppendLine($"- {citation}");
                    }
                }
                sb.AppendLine();
            }
        }

        sb.AppendLine("## Instructions");
        sb.AppendLine("Implement the task as described. Follow best practices.");
        sb.AppendLine($"Complexity level: {task.Complexity}");

        return sb.ToString();
    }

    /// <summary>
    /// Format a prompt for a feature
    /// </summary>
    public async Task<string> FormatFeaturePromptAsync(
        Feature feature,
        string repositoryPath,
        PromptStyle style = PromptStyle.Structured,
        CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();

        switch (style)
        {
            case PromptStyle.Structured:
                sb.AppendLine($"# Feature: {feature.Title}");
                sb.AppendLine();
                sb.AppendLine("## Description");
                sb.AppendLine(feature.Description);
                sb.AppendLine();

                if (feature.AcceptanceCriteria.Count > 0)
                {
                    sb.AppendLine("## Acceptance Criteria");
                    foreach (var criteria in feature.AcceptanceCriteria)
                    {
                        sb.AppendLine($"- [ ] {criteria}");
                    }
                    sb.AppendLine();
                }

                if (feature.TechnicalNotes.Count > 0)
                {
                    sb.AppendLine("## Technical Notes");
                    foreach (var note in feature.TechnicalNotes)
                    {
                        sb.AppendLine($"- {note}");
                    }
                }
                break;

            case PromptStyle.Conversational:
                sb.AppendLine($"Implement the \"{feature.Title}\" feature.");
                sb.AppendLine(feature.Description);
                if (feature.AcceptanceCriteria.Count > 0)
                {
                    sb.AppendLine("Acceptance criteria:");
                    foreach (var criteria in feature.AcceptanceCriteria)
                    {
                        sb.AppendLine($"- {criteria}");
                    }
                }
                break;

            case PromptStyle.Minimal:
                sb.AppendLine($"Implement: {feature.Title}");
                sb.AppendLine(feature.Description);
                break;
        }

        var memories = await GetRelevantMemoriesAsync(repositoryPath, feature.Title + " " + feature.Description, cancellationToken);
        if (memories.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Relevant Context");
            foreach (var memory in memories)
            {
                sb.AppendLine($"- **{memory.Subject}:** {memory.Fact}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Format a system message for an agent
    /// </summary>
    public string FormatSystemMessage(AgentRole role, AgentConfiguration config)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are an AI coding assistant.");
        sb.AppendLine();

        switch (role)
        {
            case AgentRole.Orchestrator:
                sb.AppendLine("Your role is to coordinate the implementation workflow.");
                foreach (var resp in config.Orchestrator.Responsibilities)
                {
                    sb.AppendLine($"- {resp}");
                }
                break;

            case AgentRole.Coder:
                sb.AppendLine("Your role is to implement code changes.");
                foreach (var resp in config.Coder.Responsibilities)
                {
                    sb.AppendLine($"- {resp}");
                }
                break;

            case AgentRole.Tester:
                sb.AppendLine("Your role is to create and validate tests.");
                foreach (var resp in config.Tester.Responsibilities)
                {
                    sb.AppendLine($"- {resp}");
                }
                break;

            case AgentRole.Reviewer:
                sb.AppendLine("Your role is to review code quality.");
                foreach (var resp in config.Reviewer.Responsibilities)
                {
                    sb.AppendLine($"- {resp}");
                }
                break;
        }

        return sb.ToString();
    }

    private async Task<List<Memory>> GetRelevantMemoriesAsync(
        string repositoryPath,
        string query,
        CancellationToken cancellationToken)
    {
        try
        {
            var memories = await _memoryService.SearchMemoriesAsync(repositoryPath, query);
            return memories.Take(5).ToList();
        }
        catch
        {
            return new List<Memory>();
        }
    }
}
