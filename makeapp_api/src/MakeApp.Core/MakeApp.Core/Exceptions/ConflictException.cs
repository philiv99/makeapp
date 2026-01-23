namespace MakeApp.Core.Exceptions;

/// <summary>
/// Exception thrown when there is a conflict with the current state
/// </summary>
public class ConflictException : Exception
{
    /// <summary>Name of the conflicting resource</summary>
    public string? ResourceType { get; }
    
    /// <summary>Identifier of the conflicting resource</summary>
    public string? ResourceId { get; }
    
    /// <summary>Creates a new ConflictException</summary>
    public ConflictException(string message) : base(message)
    {
    }
    
    /// <summary>Creates a new ConflictException with resource details</summary>
    public ConflictException(string resourceType, string resourceId, string message) 
        : base(message)
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }
    
    /// <summary>Creates a new ConflictException with inner exception</summary>
    public ConflictException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
