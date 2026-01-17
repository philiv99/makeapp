namespace MakeApp.Core.Enums;

/// <summary>
/// Role of an agent in the orchestration system
/// </summary>
public enum AgentRole
{
    /// <summary>Coordinates all agents and manages phase progression</summary>
    Orchestrator,
    
    /// <summary>Generates and modifies code files</summary>
    Coder,
    
    /// <summary>Generates and executes tests</summary>
    Tester,
    
    /// <summary>Reviews code quality and approves for commit</summary>
    Reviewer
}
