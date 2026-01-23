using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using MakeApp.Application.DTOs;
using MakeApp.Core.Interfaces;
using CoreCreateAppRequest = MakeApp.Core.Interfaces.CreateAppRequest;
using CoreAppRequirements = MakeApp.Core.Interfaces.AppRequirements;
using DtoCreateAppRequest = MakeApp.Application.DTOs.CreateAppRequest;

namespace MakeApp.Api.Controllers;

/// <summary>
/// Controller for app creation and management operations
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/apps")]
[Produces("application/json")]
public class AppsController : ControllerBase
{
    private readonly IRepositoryCreationService _repositoryCreationService;
    private readonly ILogger<AppsController> _logger;

    public AppsController(
        IRepositoryCreationService repositoryCreationService,
        ILogger<AppsController> logger)
    {
        _repositoryCreationService = repositoryCreationService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new app with MakeApp structure
    /// </summary>
    /// <param name="request">App creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created app information</returns>
    [HttpPost]
    [ProducesResponseType(typeof(AppResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AppResponse>> CreateApp(
        [FromBody] DtoCreateAppRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received request to create app: {AppName}", request.Name);

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "App name is required",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            // Map DTO to service request
            var serviceRequest = new CoreCreateAppRequest
            {
                Name = request.Name,
                Description = request.Description,
                Owner = request.Owner,
                IsPrivate = request.Private,
                Requirements = new CoreAppRequirements
                {
                    Name = request.Name,
                    Description = request.Description ?? request.Requirements,
                    ProjectType = request.ProjectType
                }
            };

            var result = await _repositoryCreationService.CreateAppAsync(serviceRequest, cancellationToken);

            if (!result.Success)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "App creation failed",
                    Detail = result.ErrorMessage,
                    Status = StatusCodes.Status500InternalServerError
                });
            }

            var response = new AppResponse
            {
                Id = result.AppId,
                Name = result.AppName,
                Owner = result.Owner ?? "",
                Description = request.Description ?? "",
                Status = Core.Enums.AppStatus.Initializing,
                RepositoryUrl = result.RepositoryUrl ?? "",
                LocalPath = result.LocalPath ?? "",
                CurrentBranch = "main",
                CreatedAt = result.CreatedAt ?? DateTime.UtcNow,
                UpdatedAt = result.CreatedAt ?? DateTime.UtcNow
            };

            _logger.LogInformation("Successfully created app: {AppId} at {RepositoryUrl}", response.Id, response.RepositoryUrl);

            return CreatedAtAction(nameof(GetApp), new { id = response.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating app: {AppName}", request.Name);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An unexpected error occurred while creating the app",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Get app by ID
    /// </summary>
    /// <param name="id">App ID</param>
    /// <returns>App information</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AppResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<AppResponse> GetApp(string id)
    {
        // TODO: Implement app retrieval from persistence
        _logger.LogInformation("Getting app: {AppId}", id);

        return NotFound(new ProblemDetails
        {
            Title = "App not found",
            Detail = $"App with ID '{id}' was not found",
            Status = StatusCodes.Status404NotFound
        });
    }

    /// <summary>
    /// Get app status
    /// </summary>
    /// <param name="id">App ID</param>
    /// <returns>App status</returns>
    [HttpGet("{id}/status")]
    [ProducesResponseType(typeof(AppStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<AppStatusResponse> GetAppStatus(string id)
    {
        // TODO: Implement status retrieval
        _logger.LogInformation("Getting status for app: {AppId}", id);

        return NotFound(new ProblemDetails
        {
            Title = "App not found",
            Detail = $"App with ID '{id}' was not found",
            Status = StatusCodes.Status404NotFound
        });
    }

    /// <summary>
    /// List all apps
    /// </summary>
    /// <param name="owner">Filter by owner (optional)</param>
    /// <returns>List of apps</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AppResponse>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<AppResponse>> ListApps([FromQuery] string? owner = null)
    {
        // TODO: Implement app listing from persistence
        _logger.LogInformation("Listing apps for owner: {Owner}", owner ?? "all");

        return Ok(Array.Empty<AppResponse>());
    }

    /// <summary>
    /// Delete an app
    /// </summary>
    /// <param name="id">App ID</param>
    /// <param name="deleteRepository">Whether to delete the GitHub repository</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult DeleteApp(string id, [FromQuery] bool deleteRepository = false)
    {
        // TODO: Implement app deletion
        _logger.LogInformation("Deleting app: {AppId}, deleteRepository: {DeleteRepository}", id, deleteRepository);

        return NotFound(new ProblemDetails
        {
            Title = "App not found",
            Detail = $"App with ID '{id}' was not found",
            Status = StatusCodes.Status404NotFound
        });
    }
}
