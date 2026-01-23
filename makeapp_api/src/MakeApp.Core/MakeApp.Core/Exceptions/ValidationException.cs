namespace MakeApp.Core.Exceptions;

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public class ValidationException : Exception
{
    /// <summary>Validation errors</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }
    
    /// <summary>Creates a new ValidationException</summary>
    public ValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }
    
    /// <summary>Creates a new ValidationException with errors</summary>
    public ValidationException(IDictionary<string, string[]> errors) 
        : base("One or more validation errors occurred")
    {
        Errors = new Dictionary<string, string[]>(errors);
    }
    
    /// <summary>Creates a new ValidationException with a single field error</summary>
    public ValidationException(string fieldName, string error) 
        : base($"Validation error on {fieldName}: {error}")
    {
        Errors = new Dictionary<string, string[]>
        {
            [fieldName] = new[] { error }
        };
    }
    
    /// <summary>Creates a new ValidationException with inner exception</summary>
    public ValidationException(string message, Exception innerException) 
        : base(message, innerException)
    {
        Errors = new Dictionary<string, string[]>();
    }
}
