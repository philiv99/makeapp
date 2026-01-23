using MakeApp.Core.Entities;

namespace MakeApp.Core.Exceptions;

/// <summary>
/// Exception thrown when a repository has pending changes that block an operation
/// </summary>
public class PendingChangesException : Exception
{
    /// <summary>Repository name</summary>
    public string RepositoryName { get; }
    
    /// <summary>Details of the pending changes</summary>
    public PendingChanges PendingChanges { get; }
    
    /// <summary>Creates a new PendingChangesException</summary>
    public PendingChangesException(string repositoryName, PendingChanges pendingChanges) 
        : base($"Repository '{repositoryName}' has pending changes that must be committed and pushed first")
    {
        RepositoryName = repositoryName;
        PendingChanges = pendingChanges;
    }
    
    /// <summary>Creates a new PendingChangesException with custom message</summary>
    public PendingChangesException(string repositoryName, PendingChanges pendingChanges, string message) 
        : base(message)
    {
        RepositoryName = repositoryName;
        PendingChanges = pendingChanges;
    }
}
