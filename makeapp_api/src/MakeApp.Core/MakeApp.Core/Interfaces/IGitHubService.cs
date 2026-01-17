using MakeApp.Core.Entities;

namespace MakeApp.Core.Interfaces;

/// <summary>
/// Service interface for GitHub API operations
/// </summary>
public interface IGitHubService
{
    /// <summary>Create a new repository on GitHub</summary>
    Task<RepositoryInfo> CreateRepositoryAsync(CreateRepoOptions options);
    
    /// <summary>Get repository information</summary>
    Task<RepositoryInfo?> GetRepositoryAsync(string owner, string name);
    
    /// <summary>Delete a repository</summary>
    Task<bool> DeleteRepositoryAsync(string owner, string name);
    
    /// <summary>Create a pull request</summary>
    Task<PullRequestInfo> CreatePullRequestAsync(CreatePullRequestOptions options);
    
    /// <summary>Get pull request information</summary>
    Task<PullRequestInfo?> GetPullRequestAsync(string owner, string name, int number);
    
    /// <summary>Get the authenticated user</summary>
    Task<GitHubUser> GetCurrentUserAsync();
    
    /// <summary>Validate GitHub credentials</summary>
    Task<bool> ValidateCredentialsAsync();
    
    /// <summary>Get repositories for a user or organization</summary>
    Task<IReadOnlyList<RepositoryInfo>> GetRepositoriesAsync(string owner);
}

/// <summary>
/// Options for creating a repository
/// </summary>
public class CreateRepoOptions
{
    /// <summary>Repository name</summary>
    public string Name { get; set; } = "";
    
    /// <summary>Owner (user or organization)</summary>
    public string Owner { get; set; } = "";
    
    /// <summary>Repository description</summary>
    public string? Description { get; set; }
    
    /// <summary>Whether the repository should be private</summary>
    public bool Private { get; set; }
    
    /// <summary>Whether to auto-initialize with README</summary>
    public bool AutoInit { get; set; }
    
    /// <summary>Gitignore template to use</summary>
    public string? GitignoreTemplate { get; set; }
    
    /// <summary>License template to use</summary>
    public string? LicenseTemplate { get; set; }
}

/// <summary>
/// Options for creating a pull request
/// </summary>
public class CreatePullRequestOptions
{
    /// <summary>Repository owner</summary>
    public string Owner { get; set; } = "";
    
    /// <summary>Repository name</summary>
    public string Name { get; set; } = "";
    
    /// <summary>Pull request title</summary>
    public string Title { get; set; } = "";
    
    /// <summary>Pull request body</summary>
    public string? Body { get; set; }
    
    /// <summary>Head branch (source)</summary>
    public string Head { get; set; } = "";
    
    /// <summary>Base branch (target)</summary>
    public string Base { get; set; } = "main";
    
    /// <summary>Whether to create as draft</summary>
    public bool Draft { get; set; }
}

/// <summary>
/// Information about a pull request
/// </summary>
public class PullRequestInfo
{
    /// <summary>Pull request number</summary>
    public int Number { get; set; }
    
    /// <summary>Pull request title</summary>
    public string Title { get; set; } = "";
    
    /// <summary>Pull request body</summary>
    public string? Body { get; set; }
    
    /// <summary>Pull request state</summary>
    public string State { get; set; } = "";
    
    /// <summary>HTML URL</summary>
    public string HtmlUrl { get; set; } = "";
    
    /// <summary>Head branch reference</summary>
    public string HeadRef { get; set; } = "";
    
    /// <summary>Base branch reference</summary>
    public string BaseRef { get; set; } = "";
    
    /// <summary>Whether the PR is a draft</summary>
    public bool IsDraft { get; set; }

    /// <summary>Whether the PR has been merged</summary>
    public bool IsMerged { get; set; }
    
    /// <summary>When the PR was created</summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>When the PR was last updated</summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Information about a GitHub user
/// </summary>
public class GitHubUser
{
    /// <summary>User login/username</summary>
    public string Login { get; set; } = "";
    
    /// <summary>User ID</summary>
    public long Id { get; set; }
    
    /// <summary>User display name</summary>
    public string? Name { get; set; }
    
    /// <summary>User email</summary>
    public string? Email { get; set; }
    
    /// <summary>User avatar URL</summary>
    public string? AvatarUrl { get; set; }
    
    /// <summary>User profile URL</summary>
    public string? HtmlUrl { get; set; }

    /// <summary>OAuth scopes granted to the token</summary>
    public IList<string>? Scopes { get; set; }
}
