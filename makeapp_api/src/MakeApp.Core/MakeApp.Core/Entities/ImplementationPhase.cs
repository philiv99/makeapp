using MakeApp.Core.Enums;

namespace MakeApp.Core.Entities;

/// <summary>
/// Represents a phase within an implementation plan
/// </summary>
public class ImplementationPhase
{
    /// <summary>Phase number (1-indexed)</summary>
    public int Phase { get; set; }
    
    /// <summary>Name of the phase</summary>
    public string Name { get; set; } = "";
    
    /// <summary>Description of what this phase accomplishes</summary>
    public string Description { get; set; } = "";
    
    /// <summary>Current status of the phase</summary>
    public PhaseStatus Status { get; set; } = PhaseStatus.NotStarted;
    
    /// <summary>Tasks within this phase</summary>
    public List<PhaseTask> Tasks { get; set; } = new();
    
    /// <summary>Acceptance criteria for phase completion</summary>
    public List<string> AcceptanceCriteria { get; set; } = new();
    
    /// <summary>Phase numbers that must be completed before this phase</summary>
    public List<int> Dependencies { get; set; } = new();
    
    /// <summary>When the phase started</summary>
    public DateTime? StartedAt { get; set; }
    
    /// <summary>When the phase completed</summary>
    public DateTime? CompletedAt { get; set; }
}
