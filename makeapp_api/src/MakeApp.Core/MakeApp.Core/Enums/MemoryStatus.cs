namespace MakeApp.Core.Enums;

/// <summary>
/// Status of a memory in the agentic memory system
/// </summary>
public enum MemoryStatus
{
    /// <summary>Memory is active and valid</summary>
    Active,
    
    /// <summary>Memory citations are partially stale</summary>
    Stale,
    
    /// <summary>Memory citations are invalid</summary>
    Invalid,
    
    /// <summary>Memory has been superseded by a newer memory</summary>
    Superseded,
    
    /// <summary>Memory has been archived</summary>
    Archived
}
