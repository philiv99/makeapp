using MakeApp.Core.Entities;
using MakeApp.Core.Interfaces;
using System.Text;
using System.Text.Json;

namespace MakeApp.Application.Services;

/// <summary>
/// Service for managing agent configurations
/// </summary>
public class AgentConfigurationService : IAgentConfigurationService
{
    private readonly IFileSystem _fileSystem;

    public AgentConfigurationService(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    /// <inheritdoc />
    public async Task<AgentConfiguration> LoadConfigurationAsync(
        string repositoryPath,
        CancellationToken cancellationToken = default)
    {
        var configPath = _fileSystem.Path.Combine(repositoryPath, ".makeapp", "agents", "config.json");

        if (_fileSystem.File.Exists(configPath))
        {
            var json = await _fileSystem.File.ReadAllTextAsync(configPath, cancellationToken);
            var config = JsonSerializer.Deserialize<AgentConfiguration>(json);
            return config ?? GetDefaultConfiguration();
        }

        return GetDefaultConfiguration();
    }

    /// <inheritdoc />
    public async Task SaveConfigurationAsync(
        AgentConfiguration config,
        string repositoryPath,
        CancellationToken cancellationToken = default)
    {
        var agentsPath = _fileSystem.Path.Combine(repositoryPath, ".makeapp", "agents");
        _fileSystem.Directory.CreateDirectory(agentsPath);

        var configPath = _fileSystem.Path.Combine(agentsPath, "config.json");
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });

        await _fileSystem.File.WriteAllTextAsync(configPath, json, cancellationToken);
    }

    /// <inheritdoc />
    public AgentConfiguration GetDefaultConfiguration()
    {
        return new AgentConfiguration
        {
            Orchestrator = new OrchestratorAgent
            {
                Description = "Coordinates workflow execution and agent interactions",
                Responsibilities = new List<string>
                {
                    "Break down tasks into manageable steps",
                    "Delegate to appropriate agents",
                    "Track progress and handle failures"
                },
                PhaseCriteria = new PhaseCriteria
                {
                    RequireAllTasksComplete = true,
                    RequireTestsPassing = true,
                    RequireReviewApproval = true
                }
            },
            Coder = new CoderAgent
            {
                Description = "Implements code changes and features",
                Responsibilities = new List<string>
                {
                    "Write clean, well-structured code",
                    "Follow project conventions",
                    "Add appropriate comments"
                },
                Constraints = new List<string>
                {
                    "Maximum 300 lines per file change",
                    "Follow existing code patterns"
                },
                OutputRequirements = new OutputRequirements
                {
                    MustInclude = new List<string> { "implementation", "imports", "tests" },
                    MustValidate = new List<string> { "syntax", "types", "documentation" }
                }
            },
            Tester = new TesterAgent
            {
                Description = "Creates and runs tests",
                Responsibilities = new List<string>
                {
                    "Write comprehensive unit tests",
                    "Ensure edge cases are covered",
                    "Validate test coverage"
                },
                TestingRules = new TestingRules
                {
                    UnitTestsRequired = true,
                    IntegrationTestsRequired = true,
                    MinimumCoverage = 80
                },
                ValidationChecks = new List<string>
                {
                    "All tests pass",
                    "No regressions introduced"
                }
            },
            Reviewer = new ReviewerAgent
            {
                Description = "Reviews code for quality and best practices",
                Responsibilities = new List<string>
                {
                    "Check for code quality issues",
                    "Verify security best practices",
                    "Ensure performance considerations"
                },
                Checkpoints = new List<string>
                {
                    "Code style compliance",
                    "Security vulnerabilities",
                    "Performance implications"
                },
                ApprovalRequired = true
            }
        };
    }

    /// <inheritdoc />
    public AgentConfiguration GetDefaultConfiguration(string projectType)
    {
        var config = GetDefaultConfiguration();
        
        // Customize based on project type
        switch (projectType.ToLowerInvariant())
        {
            case "dotnet":
            case "csharp":
                config.Tester.TestingRules.Frameworks = new TestingFrameworks
                {
                    TestFramework = "xunit",
                    Assertions = "FluentAssertions",
                    Mocking = "Moq"
                };
                config.Coder.Constraints.Add("Use async/await patterns");
                config.Coder.Constraints.Add("Follow .NET naming conventions");
                break;
                
            case "python":
                config.Tester.TestingRules.Frameworks = new TestingFrameworks
                {
                    TestFramework = "pytest",
                    Assertions = "pytest",
                    Mocking = "pytest-mock"
                };
                config.Coder.Constraints.Add("Follow PEP 8 style guide");
                config.Coder.Constraints.Add("Use type hints");
                break;
                
            case "node":
            case "nodejs":
            case "typescript":
                config.Tester.TestingRules.Frameworks = new TestingFrameworks
                {
                    TestFramework = "vitest",
                    Assertions = "vitest",
                    Mocking = "vitest"
                };
                config.Coder.Constraints.Add("Use TypeScript strict mode");
                config.Coder.Constraints.Add("Prefer const over let");
                break;
        }

        return config;
    }

    /// <inheritdoc />
    public AgentConfigurationValidationResult ValidateConfiguration(AgentConfiguration configuration)
    {
        var result = new AgentConfigurationValidationResult { IsValid = true };

        // Validate Orchestrator
        if (string.IsNullOrWhiteSpace(configuration.Orchestrator?.Description))
        {
            result.Warnings.Add("Orchestrator description is empty");
        }

        // Validate Coder
        if (configuration.Coder?.Constraints?.Count == 0)
        {
            result.Warnings.Add("Coder has no constraints defined");
        }

        // Validate Tester
        if (configuration.Tester?.TestingRules?.MinimumCoverage < 0 || 
            configuration.Tester?.TestingRules?.MinimumCoverage > 100)
        {
            result.Errors.Add("Minimum coverage must be between 0 and 100");
            result.IsValid = false;
        }

        // Validate Reviewer
        if (configuration.Reviewer?.Checkpoints?.Count == 0)
        {
            result.Warnings.Add("Reviewer has no checkpoints defined");
        }

        return result;
    }

    /// <inheritdoc />
    public string GenerateCopilotInstructions(AgentConfiguration configuration)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("# GitHub Copilot Instructions");
        sb.AppendLine();
        sb.AppendLine("This file provides context and guidelines for GitHub Copilot when working in this repository.");
        sb.AppendLine();
        
        // Coder guidelines
        sb.AppendLine("## Code Style Guidelines");
        sb.AppendLine();
        if (configuration.Coder?.Constraints?.Any() == true)
        {
            foreach (var constraint in configuration.Coder.Constraints)
            {
                sb.AppendLine($"- {constraint}");
            }
            sb.AppendLine();
        }

        // Testing guidelines
        sb.AppendLine("## Testing Guidelines");
        sb.AppendLine();
        if (configuration.Tester?.TestingRules != null)
        {
            var rules = configuration.Tester.TestingRules;
            sb.AppendLine($"- Test naming convention: {rules.NamingConvention}");
            sb.AppendLine($"- Minimum coverage: {rules.MinimumCoverage}%");
            if (rules.UnitTestsRequired)
                sb.AppendLine("- Unit tests are required");
            if (rules.IntegrationTestsRequired)
                sb.AppendLine("- Integration tests are required");
            sb.AppendLine();
        }

        // Review checkpoints
        sb.AppendLine("## Code Review Checkpoints");
        sb.AppendLine();
        if (configuration.Reviewer?.Checkpoints?.Any() == true)
        {
            foreach (var checkpoint in configuration.Reviewer.Checkpoints)
            {
                sb.AppendLine($"- {checkpoint}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine("*This file was generated by MakeApp.*");

        return sb.ToString();
    }

    /// <inheritdoc />
    public async Task<AgentConfiguration> InitializeAsync(
        string repositoryPath,
        CancellationToken cancellationToken = default)
    {
        var config = GetDefaultConfiguration();
        await SaveConfigurationAsync(config, repositoryPath, cancellationToken);
        return config;
    }
}
