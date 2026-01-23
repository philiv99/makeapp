using MakeApp.Core.Entities;

namespace MakeApp.Core.Interfaces;

/// <summary>
/// Service interface for creating new repositories and initializing MakeApp projects
/// </summary>
public interface IRepositoryCreationService
{
    /// <summary>
    /// Create a new app from requirements (creates GitHub repo, clones locally, initializes MakeApp)
    /// </summary>
    Task<CreateAppResult> CreateAppAsync(CreateAppRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initialize MakeApp folder structure in an existing repository
    /// </summary>
    Task InitializeMakeAppFoldersAsync(string repoPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate initial files for a new repository
    /// </summary>
    Task<IReadOnlyList<string>> GenerateInitialFilesAsync(string repoPath, AppRequirements? requirements, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request to create a new app
/// </summary>
public class CreateAppRequest
{
    /// <summary>Name of the application/repository</summary>
    public string Name { get; set; } = "";

    /// <summary>Optional description</summary>
    public string? Description { get; set; }

    /// <summary>GitHub owner (user or organization) - uses configured default if not provided</summary>
    public string? Owner { get; set; }

    /// <summary>Whether the repository should be private</summary>
    public bool IsPrivate { get; set; } = true;

    /// <summary>Local base path for cloning (optional)</summary>
    public string? LocalBasePath { get; set; }

    /// <summary>App requirements including project type</summary>
    public AppRequirements? Requirements { get; set; }
}

/// <summary>
/// Result of creating a new app
/// </summary>
public class CreateAppResult
{
    /// <summary>Whether the creation was successful</summary>
    public bool Success { get; set; }

    /// <summary>Error message if creation failed</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Generated app ID</summary>
    public string AppId { get; set; } = "";

    /// <summary>App name</summary>
    public string AppName { get; set; } = "";

    /// <summary>Repository owner</summary>
    public string? Owner { get; set; }

    /// <summary>Repository URL</summary>
    public string? RepositoryUrl { get; set; }

    /// <summary>Local path where the repository was cloned</summary>
    public string? LocalPath { get; set; }

    /// <summary>List of files that were generated</summary>
    public IReadOnlyList<string> GeneratedFiles { get; set; } = Array.Empty<string>();

    /// <summary>When the app was created</summary>
    public DateTime? CreatedAt { get; set; }
}

/// <summary>
/// Requirements for generating an application
/// </summary>
public class AppRequirements
{
    /// <summary>Application name</summary>
    public string? Name { get; set; }

    /// <summary>Application description</summary>
    public string? Description { get; set; }

    /// <summary>Project type (dotnet, python, node, etc.)</summary>
    public string? ProjectType { get; set; }

    /// <summary>Additional context</summary>
    public string? AdditionalContext { get; set; }

    /// <summary>Custom instructions for the agent</summary>
    public IReadOnlyList<string>? CustomInstructions { get; set; }
}
