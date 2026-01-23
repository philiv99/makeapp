using MakeApp.Core.Enums;

namespace MakeApp.Core.Entities;

/// <summary>
/// Represents a memory in the agentic memory system
/// </summary>
public class Memory
{
    /// <summary>Unique identifier for the memory</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    
    /// <summary>Path to the repository</summary>
    public string RepositoryPath { get; set; } = "";
    
    /// <summary>Repository owner</summary>
    public string RepositoryOwner { get; set; } = "";
    
    /// <summary>Repository name</summary>
    public string RepositoryName { get; set; } = "";
    
    /// <summary>Subject/title of the memory</summary>
    public string Subject { get; set; } = "";
    
    /// <summary>The fact or convention learned</summary>
    public string Fact { get; set; } = "";
    
    /// <summary>Citations supporting this memory</summary>
    public List<MemoryCitation> Citations { get; set; } = new();
    
    /// <summary>Reason why this is worth remembering</summary>
    public string Reason { get; set; } = "";
    
    /// <summary>Workflow ID that created this memory</summary>
    public string CreatedByWorkflowId { get; set; } = "";
    
    /// <summary>User ID that created this memory</summary>
    public string CreatedByUserId { get; set; } = "";
    
    /// <summary>When the memory was created</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>When the memory was last validated</summary>
    public DateTime? LastValidatedAt { get; set; }
    
    /// <summary>When the memory was last used</summary>
    public DateTime? LastUsedAt { get; set; }
    
    /// <summary>Number of times this memory has been used</summary>
    public int UseCount { get; set; }
    
    /// <summary>Current status of the memory</summary>
    public MemoryStatus Status { get; set; } = MemoryStatus.Active;
    
    /// <summary>When the memory expires (28 days from last validation)</summary>
    public DateTime ExpiresAt => (LastValidatedAt ?? CreatedAt).AddDays(28);
    
    /// <summary>Whether the memory has expired</summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}
