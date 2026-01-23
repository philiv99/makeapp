namespace MakeApp.Core.Exceptions;

/// <summary>
/// Exception thrown when a requested resource is not found
/// </summary>
public class NotFoundException : Exception
{
    /// <summary>Name of the resource type</summary>
    public string ResourceType { get; }
    
    /// <summary>Identifier of the resource</summary>
    public string? ResourceId { get; }
    
    /// <summary>Creates a new NotFoundException</summary>
    public NotFoundException(string message) : base(message)
    {
        ResourceType = "Resource";
    }
    
    /// <summary>Creates a new NotFoundException with resource details</summary>
    public NotFoundException(string resourceType, string resourceId) 
        : base($"{resourceType} '{resourceId}' not found")
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }
    
    /// <summary>Creates a new NotFoundException with inner exception</summary>
    public NotFoundException(string message, Exception innerException) 
        : base(message, innerException)
    {
        ResourceType = "Resource";
    }
}
