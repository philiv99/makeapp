using MakeApp.Core.Enums;

namespace MakeApp.Core.Entities;

/// <summary>
/// Configuration for an orchestration agent
/// </summary>
public class AgentConfiguration
{
    /// <summary>Orchestrator agent configuration</summary>
    public OrchestratorAgent Orchestrator { get; set; } = new();
    
    /// <summary>Coder agent configuration</summary>
    public CoderAgent Coder { get; set; } = new();
    
    /// <summary>Tester agent configuration</summary>
    public TesterAgent Tester { get; set; } = new();
    
    /// <summary>Reviewer agent configuration</summary>
    public ReviewerAgent Reviewer { get; set; } = new();
}

/// <summary>
/// Base class for agent configurations
/// </summary>
public abstract class AgentConfigBase
{
    /// <summary>Agent role</summary>
    public abstract AgentRole Role { get; }
    
    /// <summary>Description of the agent's purpose</summary>
    public string Description { get; set; } = "";
    
    /// <summary>Agent responsibilities</summary>
    public List<string> Responsibilities { get; set; } = new();
}

/// <summary>
/// Orchestrator agent configuration
/// </summary>
public class OrchestratorAgent : AgentConfigBase
{
    /// <inheritdoc/>
    public override AgentRole Role => AgentRole.Orchestrator;
    
    /// <summary>Criteria for phase completion</summary>
    public PhaseCriteria PhaseCriteria { get; set; } = new();
}

/// <summary>
/// Coder agent configuration
/// </summary>
public class CoderAgent : AgentConfigBase
{
    /// <inheritdoc/>
    public override AgentRole Role => AgentRole.Coder;
    
    /// <summary>Constraints for code generation</summary>
    public List<string> Constraints { get; set; } = new();
    
    /// <summary>Output requirements</summary>
    public OutputRequirements OutputRequirements { get; set; } = new();
}

/// <summary>
/// Tester agent configuration
/// </summary>
public class TesterAgent : AgentConfigBase
{
    /// <inheritdoc/>
    public override AgentRole Role => AgentRole.Tester;
    
    /// <summary>Testing rules and requirements</summary>
    public TestingRules TestingRules { get; set; } = new();
    
    /// <summary>Validation checks to perform</summary>
    public List<string> ValidationChecks { get; set; } = new();
    
    /// <summary>Test structure template</summary>
    public TestStructureTemplate TestStructureTemplate { get; set; } = new();
}

/// <summary>
/// Reviewer agent configuration
/// </summary>
public class ReviewerAgent : AgentConfigBase
{
    /// <inheritdoc/>
    public override AgentRole Role => AgentRole.Reviewer;
    
    /// <summary>Checkpoints for code review</summary>
    public List<string> Checkpoints { get; set; } = new();
    
    /// <summary>Whether approval is required to proceed</summary>
    public bool ApprovalRequired { get; set; } = true;
}

/// <summary>
/// Criteria for phase completion
/// </summary>
public class PhaseCriteria
{
    /// <summary>Whether all tasks must be complete</summary>
    public bool RequireAllTasksComplete { get; set; } = true;
    
    /// <summary>Whether tests must pass</summary>
    public bool RequireTestsPassing { get; set; } = true;
    
    /// <summary>Whether review approval is required</summary>
    public bool RequireReviewApproval { get; set; } = true;
}

/// <summary>
/// Output requirements for coder agent
/// </summary>
public class OutputRequirements
{
    /// <summary>Items that must be included in output</summary>
    public List<string> MustInclude { get; set; } = new() { "implementation", "imports" };
    
    /// <summary>Validations that must pass</summary>
    public List<string> MustValidate { get; set; } = new() { "syntax", "types" };
}

/// <summary>
/// Testing rules for tester agent
/// </summary>
public class TestingRules
{
    /// <summary>Whether unit tests are required</summary>
    public bool UnitTestsRequired { get; set; } = true;
    
    /// <summary>Whether integration tests are required</summary>
    public bool IntegrationTestsRequired { get; set; } = true;
    
    /// <summary>Minimum code coverage percentage</summary>
    public int MinimumCoverage { get; set; } = 80;
    
    /// <summary>Naming convention for tests</summary>
    public string NamingConvention { get; set; } = "MethodName_StateUnderTest_ExpectedBehavior";
    
    /// <summary>Required test paths</summary>
    public List<string> RequiredTestPaths { get; set; } = new() { "success_path", "error_path", "edge_cases" };
    
    /// <summary>Testing frameworks to use</summary>
    public TestingFrameworks Frameworks { get; set; } = new();
}

/// <summary>
/// Testing frameworks configuration
/// </summary>
public class TestingFrameworks
{
    /// <summary>Test framework to use</summary>
    public string TestFramework { get; set; } = "xunit";
    
    /// <summary>Assertion library</summary>
    public string Assertions { get; set; } = "FluentAssertions";
    
    /// <summary>Mocking framework</summary>
    public string Mocking { get; set; } = "Moq";
}

/// <summary>
/// Test structure template
/// </summary>
public class TestStructureTemplate
{
    /// <summary>Arrange step description</summary>
    public string Arrange { get; set; } = "Set up test data and mocks";
    
    /// <summary>Act step description</summary>
    public string Act { get; set; } = "Call the method under test";
    
    /// <summary>Assert step description</summary>
    public string Assert { get; set; } = "Verify expected outcomes";
}
