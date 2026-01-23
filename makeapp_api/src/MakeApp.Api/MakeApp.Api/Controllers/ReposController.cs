using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using MakeApp.Core.Entities;
using MakeApp.Core.Interfaces;

namespace MakeApp.Api.Controllers;

/// <summary>
/// Controller for repository management operations
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/repos")]
[Produces("application/json")]
public class ReposController : ControllerBase
{
    private readonly IRepositoryService _repositoryService;
    private readonly IBranchService _branchService;
    private readonly IGitService _gitService;
    private readonly ILogger<ReposController> _logger;

    public ReposController(
        IRepositoryService repositoryService,
        IBranchService branchService,
        IGitService gitService,
        ILogger<ReposController> logger)
    {
        _repositoryService = repositoryService;
        _branchService = branchService;
        _gitService = gitService;
        _logger = logger;
    }

    /// <summary>
    /// Get all available repositories from configured folders
    /// </summary>
    /// <param name="path">Optional path to scan for repositories</param>
    /// <returns>List of available repositories</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RepositoryInfo>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<RepositoryInfo>>> GetRepositories([FromQuery] string? path = null)
    {
        _logger.LogInformation("Getting available repositories from path: {Path}", path ?? "default");
        
        var repos = await _repositoryService.GetAvailableRepositoriesAsync(path);
        return Ok(repos);
    }

    /// <summary>
    /// Get a specific repository by owner and name
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="name">Repository name</param>
    /// <returns>Repository information</returns>
    [HttpGet("{owner}/{name}")]
    [ProducesResponseType(typeof(RepositoryInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RepositoryInfo>> GetRepository(string owner, string name)
    {
        _logger.LogInformation("Getting repository: {Owner}/{Name}", owner, name);
        
        var repo = await _repositoryService.GetRepositoryInfoAsync(owner, name);
        
        if (repo == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Repository not found",
                Detail = $"Repository '{owner}/{name}' was not found in configured folders",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(repo);
    }

    /// <summary>
    /// Get configuration status for a repository
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="name">Repository name</param>
    /// <returns>Repository configuration status</returns>
    [HttpGet("{owner}/{name}/status")]
    [ProducesResponseType(typeof(RepositoryConfigStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RepositoryConfigStatus>> GetRepositoryStatus(string owner, string name)
    {
        _logger.LogInformation("Getting repository status: {Owner}/{Name}", owner, name);
        
        var repoPath = await _repositoryService.GetRepositoryPathAsync(owner, name);
        
        if (string.IsNullOrEmpty(repoPath))
        {
            return NotFound(new ProblemDetails
            {
                Title = "Repository not found",
                Detail = $"Repository '{owner}/{name}' was not found in configured folders",
                Status = StatusCodes.Status404NotFound
            });
        }

        var status = await _repositoryService.GetConfigurationStatusAsync(repoPath);
        return Ok(status);
    }

    /// <summary>
    /// Get branches for a repository
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="name">Repository name</param>
    /// <param name="includeRemote">Include remote branches</param>
    /// <returns>List of branches</returns>
    [HttpGet("{owner}/{name}/branches")]
    [ProducesResponseType(typeof(IEnumerable<BranchInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<BranchInfo>>> GetBranches(
        string owner, 
        string name, 
        [FromQuery] bool includeRemote = false)
    {
        _logger.LogInformation("Getting branches for repository: {Owner}/{Name}", owner, name);
        
        var repoPath = await _repositoryService.GetRepositoryPathAsync(owner, name);
        
        if (string.IsNullOrEmpty(repoPath))
        {
            return NotFound(new ProblemDetails
            {
                Title = "Repository not found",
                Detail = $"Repository '{owner}/{name}' was not found in configured folders",
                Status = StatusCodes.Status404NotFound
            });
        }

        var branches = await _branchService.GetBranchesAsync(repoPath, includeRemote);
        return Ok(branches);
    }

    /// <summary>
    /// Create a new feature branch
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="name">Repository name</param>
    /// <param name="request">Branch creation request</param>
    /// <returns>Created branch information</returns>
    [HttpPost("{owner}/{name}/branches")]
    [ProducesResponseType(typeof(BranchInfo), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BranchInfo>> CreateBranch(
        string owner, 
        string name, 
        [FromBody] CreateBranchRequest request)
    {
        _logger.LogInformation("Creating branch '{BranchName}' in repository: {Owner}/{Name}", 
            request.BranchName, owner, name);
        
        if (string.IsNullOrWhiteSpace(request.BranchName))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "Branch name is required",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var repoPath = await _repositoryService.GetRepositoryPathAsync(owner, name);
        
        if (string.IsNullOrEmpty(repoPath))
        {
            return NotFound(new ProblemDetails
            {
                Title = "Repository not found",
                Detail = $"Repository '{owner}/{name}' was not found in configured folders",
                Status = StatusCodes.Status404NotFound
            });
        }

        try
        {
            var branch = await _branchService.CreateFeatureBranchAsync(
                repoPath, 
                request.BranchName, 
                request.BaseBranch ?? "main");

            return CreatedAtAction(
                nameof(GetBranches), 
                new { owner, name }, 
                branch);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create branch '{BranchName}'", request.BranchName);
            return BadRequest(new ProblemDetails
            {
                Title = "Branch creation failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Switch to a different branch
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="name">Repository name</param>
    /// <param name="request">Checkout request</param>
    /// <returns>Success status</returns>
    [HttpPost("{owner}/{name}/checkout")]
    [ProducesResponseType(typeof(CheckoutResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CheckoutResult>> CheckoutBranch(
        string owner, 
        string name, 
        [FromBody] CheckoutRequest request)
    {
        _logger.LogInformation("Checking out branch '{BranchName}' in repository: {Owner}/{Name}", 
            request.BranchName, owner, name);

        if (string.IsNullOrWhiteSpace(request.BranchName))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "Branch name is required",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var repoPath = await _repositoryService.GetRepositoryPathAsync(owner, name);
        
        if (string.IsNullOrEmpty(repoPath))
        {
            return NotFound(new ProblemDetails
            {
                Title = "Repository not found",
                Detail = $"Repository '{owner}/{name}' was not found in configured folders",
                Status = StatusCodes.Status404NotFound
            });
        }

        try
        {
            var success = await _branchService.SwitchToBranchAsync(repoPath, request.BranchName);
            var currentBranch = await _branchService.GetCurrentBranchAsync(repoPath);

            return Ok(new CheckoutResult
            {
                Success = success,
                CurrentBranch = currentBranch
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to checkout branch '{BranchName}'", request.BranchName);
            return BadRequest(new ProblemDetails
            {
                Title = "Checkout failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Delete a branch
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="name">Repository name</param>
    /// <param name="branchName">Branch to delete</param>
    /// <param name="force">Force delete even if not merged</param>
    /// <returns>Success status</returns>
    [HttpDelete("{owner}/{name}/branches/{**branchName}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBranch(
        string owner, 
        string name, 
        string branchName,
        [FromQuery] bool force = false)
    {
        _logger.LogInformation("Deleting branch '{BranchName}' in repository: {Owner}/{Name}", 
            branchName, owner, name);

        var repoPath = await _repositoryService.GetRepositoryPathAsync(owner, name);
        
        if (string.IsNullOrEmpty(repoPath))
        {
            return NotFound(new ProblemDetails
            {
                Title = "Repository not found",
                Detail = $"Repository '{owner}/{name}' was not found in configured folders",
                Status = StatusCodes.Status404NotFound
            });
        }

        try
        {
            var success = await _branchService.DeleteBranchAsync(repoPath, branchName, force);

            if (!success)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Branch deletion failed",
                    Detail = $"Could not delete branch '{branchName}'. It may be the current branch or not exist.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete branch '{BranchName}'", branchName);
            return BadRequest(new ProblemDetails
            {
                Title = "Branch deletion failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Get git status for a repository
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="name">Repository name</param>
    /// <returns>Git status information</returns>
    [HttpGet("{owner}/{name}/git/status")]
    [ProducesResponseType(typeof(GitStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GitStatus>> GetGitStatus(string owner, string name)
    {
        _logger.LogInformation("Getting git status for repository: {Owner}/{Name}", owner, name);
        
        var repoPath = await _repositoryService.GetRepositoryPathAsync(owner, name);
        
        if (string.IsNullOrEmpty(repoPath))
        {
            return NotFound(new ProblemDetails
            {
                Title = "Repository not found",
                Detail = $"Repository '{owner}/{name}' was not found in configured folders",
                Status = StatusCodes.Status404NotFound
            });
        }

        var status = await _gitService.GetStatusAsync(repoPath);
        return Ok(status);
    }
}

/// <summary>
/// Request to create a new branch
/// </summary>
public class CreateBranchRequest
{
    /// <summary>Name for the new branch</summary>
    public string BranchName { get; set; } = "";
    
    /// <summary>Base branch to create from (default: main)</summary>
    public string? BaseBranch { get; set; }
}

/// <summary>
/// Request to checkout a branch
/// </summary>
public class CheckoutRequest
{
    /// <summary>Name of the branch to checkout</summary>
    public string BranchName { get; set; } = "";
}

/// <summary>
/// Result of a checkout operation
/// </summary>
public class CheckoutResult
{
    /// <summary>Whether the checkout was successful</summary>
    public bool Success { get; set; }
    
    /// <summary>Current branch after checkout</summary>
    public string CurrentBranch { get; set; } = "";
}
