using FluentAssertions;
using Moq;
using MakeApp.Application.Services;
using MakeApp.Core.Entities;
using MakeApp.Core.Enums;
using MakeApp.Core.Interfaces;
using Xunit;

namespace MakeApp.Application.Tests;

/// <summary>
/// Unit tests for PromptFormatterService
/// </summary>
public class PromptFormatterServiceTests
{
    private readonly Mock<IMemoryService> _memoryServiceMock;
    private readonly PromptFormatterService _service;

    public PromptFormatterServiceTests()
    {
        _memoryServiceMock = new Mock<IMemoryService>();
        
        // Default setup: return empty memories
        _memoryServiceMock.Setup(x => x.SearchMemoriesAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Memory>());
        
        _service = new PromptFormatterService(_memoryServiceMock.Object);
    }

    #region FormatTaskPromptAsync Tests

    [Fact]
    public async Task FormatTaskPromptAsync_WithBasicTask_IncludesTaskInfo()
    {
        // Arrange
        var task = new PhaseTask
        {
            Id = "task-001",
            Description = "Implement user login",
            Complexity = "moderate"
        };

        // Act
        var result = await _service.FormatTaskPromptAsync(task, "/repos/test");

        // Assert
        result.Should().Contain("task-001");
        result.Should().Contain("Implement user login");
        result.Should().Contain("moderate");
    }

    [Fact]
    public async Task FormatTaskPromptAsync_WithFiles_IncludesFilesList()
    {
        // Arrange
        var task = new PhaseTask
        {
            Id = "task-002",
            Description = "Add service layer",
            Files = new List<string> { "Services/UserService.cs", "Services/IUserService.cs" }
        };

        // Act
        var result = await _service.FormatTaskPromptAsync(task, "/repos/test");

        // Assert
        result.Should().Contain("Files to create/modify");
        result.Should().Contain("Services/UserService.cs");
        result.Should().Contain("Services/IUserService.cs");
    }

    [Fact]
    public async Task FormatTaskPromptAsync_WithIntegrationPoints_IncludesIntegrationPoints()
    {
        // Arrange
        var task = new PhaseTask
        {
            Id = "task-003",
            Description = "Integrate authentication",
            IntegrationPoints = new List<string> { "UserController", "AuthMiddleware" }
        };

        // Act
        var result = await _service.FormatTaskPromptAsync(task, "/repos/test");

        // Assert
        result.Should().Contain("Integration Points");
        result.Should().Contain("UserController");
        result.Should().Contain("AuthMiddleware");
    }

    [Fact]
    public async Task FormatTaskPromptAsync_WithContext_IncludesContext()
    {
        // Arrange
        var task = new PhaseTask
        {
            Id = "task-004",
            Description = "Fix bug",
            Context = "Previous implementation had performance issues"
        };

        // Act
        var result = await _service.FormatTaskPromptAsync(task, "/repos/test");

        // Assert
        result.Should().Contain("Additional Context");
        result.Should().Contain("Previous implementation had performance issues");
    }

    [Fact]
    public async Task FormatTaskPromptAsync_WithRelevantMemories_IncludesMemories()
    {
        // Arrange
        var task = new PhaseTask
        {
            Id = "task-005",
            Description = "Add caching"
        };

        var memories = new List<Memory>
        {
            new Memory 
            { 
                Subject = "Caching Strategy", 
                Fact = "Use Redis for distributed caching",
                Citations = new List<MemoryCitation> 
                { 
                    new MemoryCitation { FilePath = "docs/architecture.md" } 
                }
            }
        };

        _memoryServiceMock.Setup(x => x.SearchMemoriesAsync("/repos/test", task.Description))
            .ReturnsAsync(memories);

        // Act
        var result = await _service.FormatTaskPromptAsync(task, "/repos/test");

        // Assert
        result.Should().Contain("Relevant Knowledge from Memory");
        result.Should().Contain("Caching Strategy");
        result.Should().Contain("Use Redis for distributed caching");
    }

    #endregion

    #region FormatFeaturePromptAsync - Structured Style Tests

    [Fact]
    public async Task FormatFeaturePromptAsync_StructuredStyle_IncludesTitle()
    {
        // Arrange
        var feature = new Feature
        {
            Title = "User Authentication",
            Description = "Implement OAuth2 authentication"
        };

        // Act
        var result = await _service.FormatFeaturePromptAsync(
            feature, "/repos/test", PromptStyle.Structured);

        // Assert
        result.Should().Contain("# Feature: User Authentication");
        result.Should().Contain("## Description");
        result.Should().Contain("Implement OAuth2 authentication");
    }

    [Fact]
    public async Task FormatFeaturePromptAsync_StructuredStyle_WithAcceptanceCriteria_IncludesCheckboxes()
    {
        // Arrange
        var feature = new Feature
        {
            Title = "Login Feature",
            Description = "Allow users to log in",
            AcceptanceCriteria = new List<string> 
            { 
                "Users can enter email and password",
                "Invalid credentials show error",
                "Successful login redirects to dashboard"
            }
        };

        // Act
        var result = await _service.FormatFeaturePromptAsync(
            feature, "/repos/test", PromptStyle.Structured);

        // Assert
        result.Should().Contain("## Acceptance Criteria");
        result.Should().Contain("- [ ] Users can enter email and password");
        result.Should().Contain("- [ ] Invalid credentials show error");
        result.Should().Contain("- [ ] Successful login redirects to dashboard");
    }

    [Fact]
    public async Task FormatFeaturePromptAsync_StructuredStyle_WithTechnicalNotes_IncludesNotes()
    {
        // Arrange
        var feature = new Feature
        {
            Title = "API Integration",
            Description = "Integrate with external API",
            TechnicalNotes = new List<string>
            {
                "Use HttpClient with retry policy",
                "Implement circuit breaker pattern"
            }
        };

        // Act
        var result = await _service.FormatFeaturePromptAsync(
            feature, "/repos/test", PromptStyle.Structured);

        // Assert
        result.Should().Contain("## Technical Notes");
        result.Should().Contain("- Use HttpClient with retry policy");
        result.Should().Contain("- Implement circuit breaker pattern");
    }

    #endregion

    #region FormatFeaturePromptAsync - Conversational Style Tests

    [Fact]
    public async Task FormatFeaturePromptAsync_ConversationalStyle_UsesFriendlyFormat()
    {
        // Arrange
        var feature = new Feature
        {
            Title = "Add Search",
            Description = "Implement full-text search functionality"
        };

        // Act
        var result = await _service.FormatFeaturePromptAsync(
            feature, "/repos/test", PromptStyle.Conversational);

        // Assert
        result.Should().Contain("Implement the \"Add Search\" feature");
        result.Should().Contain("Implement full-text search functionality");
    }

    [Fact]
    public async Task FormatFeaturePromptAsync_ConversationalStyle_WithCriteria_IncludesWithoutCheckboxes()
    {
        // Arrange
        var feature = new Feature
        {
            Title = "Filter Results",
            Description = "Add filtering capability",
            AcceptanceCriteria = new List<string> { "Filter by date", "Filter by category" }
        };

        // Act
        var result = await _service.FormatFeaturePromptAsync(
            feature, "/repos/test", PromptStyle.Conversational);

        // Assert
        result.Should().Contain("Acceptance criteria:");
        result.Should().Contain("- Filter by date");
        result.Should().Contain("- Filter by category");
        result.Should().NotContain("[ ]"); // No checkboxes in conversational style
    }

    #endregion

    #region FormatFeaturePromptAsync - Minimal Style Tests

    [Fact]
    public async Task FormatFeaturePromptAsync_MinimalStyle_IsCompact()
    {
        // Arrange
        var feature = new Feature
        {
            Title = "Quick Fix",
            Description = "Fix the null reference bug"
        };

        // Act
        var result = await _service.FormatFeaturePromptAsync(
            feature, "/repos/test", PromptStyle.Minimal);

        // Assert
        result.Should().Contain("Implement: Quick Fix");
        result.Should().Contain("Fix the null reference bug");
        result.Should().NotContain("## Description"); // No headers in minimal style
    }

    #endregion

    #region FormatSystemMessage Tests

    [Fact]
    public void FormatSystemMessage_OrchestratorRole_IncludesCoordinationResponsibilities()
    {
        // Arrange
        var config = CreateTestAgentConfiguration();

        // Act
        var result = _service.FormatSystemMessage(AgentRole.Orchestrator, config);

        // Assert
        result.Should().Contain("coordinate the implementation workflow");
        result.Should().Contain("Monitor phase completion");
    }

    [Fact]
    public void FormatSystemMessage_CoderRole_IncludesImplementationResponsibilities()
    {
        // Arrange
        var config = CreateTestAgentConfiguration();

        // Act
        var result = _service.FormatSystemMessage(AgentRole.Coder, config);

        // Assert
        result.Should().Contain("implement code changes");
        result.Should().Contain("Write clean code");
    }

    [Fact]
    public void FormatSystemMessage_TesterRole_IncludesTestingResponsibilities()
    {
        // Arrange
        var config = CreateTestAgentConfiguration();

        // Act
        var result = _service.FormatSystemMessage(AgentRole.Tester, config);

        // Assert
        result.Should().Contain("create and validate tests");
        result.Should().Contain("Generate unit tests");
    }

    [Fact]
    public void FormatSystemMessage_ReviewerRole_IncludesReviewResponsibilities()
    {
        // Arrange
        var config = CreateTestAgentConfiguration();

        // Act
        var result = _service.FormatSystemMessage(AgentRole.Reviewer, config);

        // Assert
        result.Should().Contain("review code quality");
        result.Should().Contain("Check code standards");
    }

    [Fact]
    public void FormatSystemMessage_IncludesAiAssistantIntro()
    {
        // Arrange
        var config = CreateTestAgentConfiguration();

        // Act
        var result = _service.FormatSystemMessage(AgentRole.Coder, config);

        // Assert
        result.Should().Contain("You are an AI coding assistant");
    }

    #endregion

    #region Helper Methods

    private static AgentConfiguration CreateTestAgentConfiguration()
    {
        return new AgentConfiguration
        {
            Orchestrator = new OrchestratorAgent
            {
                Responsibilities = new List<string> { "Monitor phase completion", "Coordinate agents" }
            },
            Coder = new CoderAgent
            {
                Responsibilities = new List<string> { "Write clean code", "Follow patterns" }
            },
            Tester = new TesterAgent
            {
                Responsibilities = new List<string> { "Generate unit tests", "Verify coverage" }
            },
            Reviewer = new ReviewerAgent
            {
                Responsibilities = new List<string> { "Check code standards", "Approve changes" }
            }
        };
    }

    #endregion
}
