namespace MakeApp.Core.Entities;

/// <summary>
/// Result of attempting to remove a repository
/// </summary>
public class RemoveResult
{
    /// <summary>Whether the removal was successful</summary>
    public bool Success { get; set; }
    
    /// <summary>Error message if removal failed</summary>
    public string? Error { get; set; }
    
    /// <summary>Result message</summary>
    public string? Message { get; set; }
    
    /// <summary>Blocking status if removal was blocked due to pending changes</summary>
    public RepositoryStatus? BlockingStatus { get; set; }
}
