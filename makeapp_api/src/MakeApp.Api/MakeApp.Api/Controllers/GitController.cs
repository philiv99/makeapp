using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using MakeApp.Core.Entities;
using MakeApp.Core.Interfaces;

namespace MakeApp.Api.Controllers;

/// <summary>
/// Controller for Git operations (commit, push, pull requests)
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/repos/{owner}/{name}/git")]
[Produces("application/json")]
public class GitController : ControllerBase
{
    private readonly IRepositoryService _repositoryService;
    private readonly IGitService _gitService;
    private readonly IGitHubService _gitHubService;
    private readonly ILogger<GitController> _logger;

    public GitController(
        IRepositoryService repositoryService,
        IGitService gitService,
        IGitHubService gitHubService,
        ILogger<GitController> logger)
    {
        _repositoryService = repositoryService;
        _gitService = gitService;
        _gitHubService = gitHubService;
        _logger = logger;
    }

    /// <summary>
    /// Get unstaged changes in the repository
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="name">Repository name</param>
    /// <returns>List of unstaged file changes</returns>
    [HttpGet("changes/unstaged")]
    [ProducesResponseType(typeof(IReadOnlyList<FileChange>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<FileChange>>> GetUnstagedChanges(string owner, string name)
    {
        _logger.LogInformation("Getting unstaged changes for repository: {Owner}/{Name}", owner, name);
        
        var repoPath = await _repositoryService.GetRepositoryPathAsync(owner, name);
        
        if (string.IsNullOrEmpty(repoPath))
        {
            return NotFound(CreateNotFoundProblem(owner, name));
        }

        var changes = await _gitService.GetUnstagedChangesAsync(repoPath);
        return Ok(changes);
    }

    /// <summary>
    /// Get staged changes in the repository
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="name">Repository name</param>
    /// <returns>List of staged file changes</returns>
    [HttpGet("changes/staged")]
    [ProducesResponseType(typeof(IReadOnlyList<FileChange>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<FileChange>>> GetStagedChanges(string owner, string name)
    {
        _logger.LogInformation("Getting staged changes for repository: {Owner}/{Name}", owner, name);
        
        var repoPath = await _repositoryService.GetRepositoryPathAsync(owner, name);
        
        if (string.IsNullOrEmpty(repoPath))
        {
            return NotFound(CreateNotFoundProblem(owner, name));
        }

        var changes = await _gitService.GetStagedChangesAsync(repoPath);
        return Ok(changes);
    }

    /// <summary>
    /// Get unpushed commits in the repository
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="name">Repository name</param>
    /// <returns>List of unpushed commits</returns>
    [HttpGet("commits/unpushed")]
    [ProducesResponseType(typeof(IReadOnlyList<CommitInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CommitInfo>>> GetUnpushedCommits(string owner, string name)
    {
        _logger.LogInformation("Getting unpushed commits for repository: {Owner}/{Name}", owner, name);
        
        var repoPath = await _repositoryService.GetRepositoryPathAsync(owner, name);
        
        if (string.IsNullOrEmpty(repoPath))
        {
            return NotFound(CreateNotFoundProblem(owner, name));
        }

        var commits = await _gitService.GetUnpushedCommitsAsync(repoPath);
        return Ok(commits);
    }

    /// <summary>
    /// Stage changes in the repository
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="name">Repository name</param>
    /// <param name="request">Stage request</param>
    /// <returns>Stage result</returns>
    [HttpPost("stage")]
    [ProducesResponseType(typeof(StageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StageResult>> StageChanges(
        string owner, 
        string name, 
        [FromBody] StageRequest request)
    {
        _logger.LogInformation("Staging changes for repository: {Owner}/{Name}, PathSpec: {PathSpec}", 
            owner, name, request.PathSpec ?? "all");
        
        var repoPath = await _repositoryService.GetRepositoryPathAsync(owner, name);
        
        if (string.IsNullOrEmpty(repoPath))
        {
            return NotFound(CreateNotFoundProblem(owner, name));
        }

        try
        {
            bool success;
            if (request.StageAll || string.IsNullOrEmpty(request.PathSpec))
            {
                success = await _gitService.StageAllAsync(repoPath);
            }
            else
            {
                success = await _gitService.StageChangesAsync(repoPath, request.PathSpec);
            }

            var status = await _gitService.GetStatusAsync(repoPath);

            return Ok(new StageResult
            {
                Success = success,
                StagedCount = status.StagedCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stage changes");
            return BadRequest(new ProblemDetails
            {
                Title = "Stage failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Create a commit
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="name">Repository name</param>
    /// <param name="request">Commit request</param>
    /// <returns>Commit result</returns>
    [HttpPost("commit")]
    [ProducesResponseType(typeof(CommitResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommitResult>> CreateCommit(
        string owner, 
        string name, 
        [FromBody] CreateCommitRequest request)
    {
        _logger.LogInformation("Creating commit for repository: {Owner}/{Name}", owner, name);
        
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "Commit message is required",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var repoPath = await _repositoryService.GetRepositoryPathAsync(owner, name);
        
        if (string.IsNullOrEmpty(repoPath))
        {
            return NotFound(CreateNotFoundProblem(owner, name));
        }

        try
        {
            // Stage all if requested
            if (request.StageAll)
            {
                await _gitService.StageAllAsync(repoPath);
            }

            var options = new CommitOptions
            {
                AuthorName = request.AuthorName,
                AuthorEmail = request.AuthorEmail,
                Amend = request.Amend
            };

            var result = await _gitService.CommitAsync(repoPath, request.Message, options);

            if (result.Success)
            {
                return CreatedAtAction(
                    nameof(GetUnpushedCommits), 
                    new { owner, name }, 
                    result);
            }

            return BadRequest(new ProblemDetails
            {
                Title = "Commit failed",
                Detail = result.Error ?? "Unknown error",
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create commit");
            return BadRequest(new ProblemDetails
            {
                Title = "Commit failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Push changes to remote
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="name">Repository name</param>
    /// <param name="request">Push request</param>
    /// <returns>Push result</returns>
    [HttpPost("push")]
    [ProducesResponseType(typeof(PushResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PushResult>> PushChanges(
        string owner, 
        string name, 
        [FromBody] PushRequest? request = null)
    {
        _logger.LogInformation("Pushing changes for repository: {Owner}/{Name}", owner, name);
        
        var repoPath = await _repositoryService.GetRepositoryPathAsync(owner, name);
        
        if (string.IsNullOrEmpty(repoPath))
        {
            return NotFound(CreateNotFoundProblem(owner, name));
        }

        try
        {
            var options = new PushOptions
            {
                RemoteName = request?.RemoteName ?? "origin",
                SetUpstream = request?.SetUpstream ?? false,
                Force = request?.Force ?? false
            };

            var result = await _gitService.PushAsync(repoPath, options);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(new ProblemDetails
            {
                Title = "Push failed",
                Detail = result.Error ?? "Unknown error",
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to push changes");
            return BadRequest(new ProblemDetails
            {
                Title = "Push failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Create a pull request
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="name">Repository name</param>
    /// <param name="request">Pull request creation request</param>
    /// <returns>Created pull request information</returns>
    [HttpPost("pull-requests")]
    [ProducesResponseType(typeof(PullRequestInfo), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PullRequestInfo>> CreatePullRequest(
        string owner, 
        string name, 
        [FromBody] CreatePullRequestRequest request)
    {
        _logger.LogInformation("Creating pull request for repository: {Owner}/{Name}", owner, name);
        
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "Pull request title is required",
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (string.IsNullOrWhiteSpace(request.Head))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "Head branch is required",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var repoPath = await _repositoryService.GetRepositoryPathAsync(owner, name);
        
        if (string.IsNullOrEmpty(repoPath))
        {
            return NotFound(CreateNotFoundProblem(owner, name));
        }

        try
        {
            var options = new CreatePullRequestOptions
            {
                Owner = owner,
                Name = name,
                Title = request.Title,
                Body = request.Body,
                Head = request.Head,
                Base = request.Base ?? "main",
                Draft = request.Draft
            };

            var pr = await _gitHubService.CreatePullRequestAsync(options);

            return CreatedAtAction(
                nameof(GetPullRequest), 
                new { owner, name, number = pr.Number }, 
                pr);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create pull request");
            return BadRequest(new ProblemDetails
            {
                Title = "Pull request creation failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Get a pull request
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="name">Repository name</param>
    /// <param name="number">Pull request number</param>
    /// <returns>Pull request information</returns>
    [HttpGet("pull-requests/{number:int}")]
    [ProducesResponseType(typeof(PullRequestInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PullRequestInfo>> GetPullRequest(
        string owner, 
        string name, 
        int number)
    {
        _logger.LogInformation("Getting pull request #{Number} for repository: {Owner}/{Name}", 
            number, owner, name);
        
        try
        {
            var pr = await _gitHubService.GetPullRequestAsync(owner, name, number);

            if (pr == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Pull request not found",
                    Detail = $"Pull request #{number} was not found in repository '{owner}/{name}'",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Ok(pr);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pull request #{Number}", number);
            return NotFound(new ProblemDetails
            {
                Title = "Pull request not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
    }

    private static ProblemDetails CreateNotFoundProblem(string owner, string name)
    {
        return new ProblemDetails
        {
            Title = "Repository not found",
            Detail = $"Repository '{owner}/{name}' was not found in configured folders",
            Status = StatusCodes.Status404NotFound
        };
    }
}

/// <summary>
/// Request to stage changes
/// </summary>
public class StageRequest
{
    /// <summary>Path specification for files to stage</summary>
    public string? PathSpec { get; set; }
    
    /// <summary>Whether to stage all changes</summary>
    public bool StageAll { get; set; }
}

/// <summary>
/// Result of a stage operation
/// </summary>
public class StageResult
{
    /// <summary>Whether the stage was successful</summary>
    public bool Success { get; set; }
    
    /// <summary>Number of staged files</summary>
    public int StagedCount { get; set; }
}

/// <summary>
/// Request to create a commit
/// </summary>
public class CreateCommitRequest
{
    /// <summary>Commit message</summary>
    public string Message { get; set; } = "";
    
    /// <summary>Whether to stage all changes before committing</summary>
    public bool StageAll { get; set; }
    
    /// <summary>Author name (optional)</summary>
    public string? AuthorName { get; set; }
    
    /// <summary>Author email (optional)</summary>
    public string? AuthorEmail { get; set; }
    
    /// <summary>Whether to amend the previous commit</summary>
    public bool Amend { get; set; }
}

/// <summary>
/// Request to push changes
/// </summary>
public class PushRequest
{
    /// <summary>Remote name (default: origin)</summary>
    public string RemoteName { get; set; } = "origin";
    
    /// <summary>Whether to set upstream tracking</summary>
    public bool SetUpstream { get; set; }
    
    /// <summary>Whether to force push</summary>
    public bool Force { get; set; }
}

/// <summary>
/// Request to create a pull request
/// </summary>
public class CreatePullRequestRequest
{
    /// <summary>Pull request title</summary>
    public string Title { get; set; } = "";
    
    /// <summary>Pull request body/description</summary>
    public string? Body { get; set; }
    
    /// <summary>Head branch (source)</summary>
    public string Head { get; set; } = "";
    
    /// <summary>Base branch (target, default: main)</summary>
    public string? Base { get; set; }
    
    /// <summary>Whether to create as draft</summary>
    public bool Draft { get; set; }
}
