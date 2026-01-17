using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using MakeApp.Application.DTOs;
using MakeApp.Application.Services;
using MakeApp.Core.Enums;

namespace MakeApp.Api.Controllers;

/// <summary>
/// Controller for feature management operations
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/features")]
[Produces("application/json")]
public class FeaturesController : ControllerBase
{
    private readonly FeatureService _featureService;
    private readonly PromptFormatterService _promptFormatterService;
    private readonly ILogger<FeaturesController> _logger;

    public FeaturesController(
        FeatureService featureService,
        PromptFormatterService promptFormatterService,
        ILogger<FeaturesController> logger)
    {
        _featureService = featureService;
        _promptFormatterService = promptFormatterService;
        _logger = logger;
    }

    /// <summary>
    /// List all features
    /// </summary>
    /// <param name="status">Filter by status (optional)</param>
    /// <param name="repositoryOwner">Filter by repository owner (optional)</param>
    /// <param name="repositoryName">Filter by repository name (optional)</param>
    /// <returns>List of features</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<FeatureResponse>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<FeatureResponse>> ListFeatures(
        [FromQuery] FeatureStatus? status = null,
        [FromQuery] string? repositoryOwner = null,
        [FromQuery] string? repositoryName = null)
    {
        _logger.LogInformation("Listing features with status: {Status}, repo: {Owner}/{Name}", 
            status, repositoryOwner, repositoryName);

        // TODO: Implement feature listing from persistence with filters
        return Ok(Array.Empty<FeatureResponse>());
    }

    /// <summary>
    /// Get a feature by ID
    /// </summary>
    /// <param name="id">Feature ID</param>
    /// <returns>Feature details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(FeatureResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<FeatureResponse> GetFeature(string id)
    {
        _logger.LogInformation("Getting feature: {FeatureId}", id);

        // TODO: Implement feature retrieval from persistence
        return NotFound(new ProblemDetails
        {
            Title = "Feature not found",
            Detail = $"Feature with ID '{id}' was not found",
            Status = StatusCodes.Status404NotFound
        });
    }

    /// <summary>
    /// Create a new feature
    /// </summary>
    /// <param name="request">Feature creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created feature</returns>
    [HttpPost]
    [ProducesResponseType(typeof(FeatureResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FeatureResponse>> CreateFeature(
        [FromBody] CreateFeatureRequestDto request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating feature: {Title} for {Owner}/{Name}", 
            request.Title, request.RepositoryOwner, request.RepositoryName);

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "Feature title is required",
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (string.IsNullOrWhiteSpace(request.RepositoryOwner) || string.IsNullOrWhiteSpace(request.RepositoryName))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "Repository owner and name are required",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            var result = await _featureService.CreateFeatureAsync(request, cancellationToken);
            
            _logger.LogInformation("Successfully created feature: {FeatureId}", result.Id);
            
            return CreatedAtAction(nameof(GetFeature), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create feature: repository not found");
            return BadRequest(new ProblemDetails
            {
                Title = "Repository not found",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating feature: {Title}", request.Title);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An unexpected error occurred while creating the feature",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Update a feature
    /// </summary>
    /// <param name="id">Feature ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated feature</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(FeatureResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<FeatureResponse> UpdateFeature(string id, [FromBody] UpdateFeatureRequestDto request)
    {
        _logger.LogInformation("Updating feature: {FeatureId}", id);

        // TODO: Implement feature update in persistence
        return NotFound(new ProblemDetails
        {
            Title = "Feature not found",
            Detail = $"Feature with ID '{id}' was not found",
            Status = StatusCodes.Status404NotFound
        });
    }

    /// <summary>
    /// Delete a feature
    /// </summary>
    /// <param name="id">Feature ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult DeleteFeature(string id)
    {
        _logger.LogInformation("Deleting feature: {FeatureId}", id);

        // TODO: Implement feature deletion from persistence
        return NotFound(new ProblemDetails
        {
            Title = "Feature not found",
            Detail = $"Feature with ID '{id}' was not found",
            Status = StatusCodes.Status404NotFound
        });
    }

    /// <summary>
    /// Get feature status
    /// </summary>
    /// <param name="id">Feature ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Feature status with progress</returns>
    [HttpGet("{id}/status")]
    [ProducesResponseType(typeof(FeatureStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FeatureStatusResponse>> GetFeatureStatus(string id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting status for feature: {FeatureId}", id);

        try
        {
            var status = await _featureService.GetFeatureStatusAsync(id, cancellationToken);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feature status: {FeatureId}", id);
            return NotFound(new ProblemDetails
            {
                Title = "Feature not found",
                Detail = $"Feature with ID '{id}' was not found",
                Status = StatusCodes.Status404NotFound
            });
        }
    }

    /// <summary>
    /// Start implementing a feature
    /// </summary>
    /// <param name="id">Feature ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated feature with in-progress status</returns>
    [HttpPost("{id}/start")]
    [ProducesResponseType(typeof(FeatureResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FeatureResponse>> StartFeature(string id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting feature implementation: {FeatureId}", id);

        try
        {
            var result = await _featureService.StartFeatureAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot start feature: {FeatureId}", id);
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid operation",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting feature: {FeatureId}", id);
            return NotFound(new ProblemDetails
            {
                Title = "Feature not found",
                Detail = $"Feature with ID '{id}' was not found",
                Status = StatusCodes.Status404NotFound
            });
        }
    }

    /// <summary>
    /// Cancel a feature
    /// </summary>
    /// <param name="id">Feature ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> CancelFeature(string id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cancelling feature: {FeatureId}", id);

        try
        {
            var result = await _featureService.CancelFeatureAsync(id, cancellationToken);
            if (!result)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Feature not found",
                    Detail = $"Feature with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound
                });
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling feature: {FeatureId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An unexpected error occurred while cancelling the feature",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Get feature formatted as a prompt
    /// </summary>
    /// <param name="id">Feature ID</param>
    /// <param name="style">Prompt style (optional, defaults to Structured)</param>
    /// <returns>Formatted prompt string</returns>
    [HttpGet("{id}/prompt")]
    [ProducesResponseType(typeof(PromptResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PromptResponse>> GetFeaturePrompt(
        string id,
        [FromQuery] PromptStyle style = PromptStyle.Structured)
    {
        _logger.LogInformation("Getting prompt for feature: {FeatureId} with style: {Style}", id, style);

        try
        {
            var task = new MakeApp.Core.Entities.PhaseTask
            {
                Id = id,
                Description = $"Implement feature {id}"
            };

            var prompt = await _promptFormatterService.FormatTaskPromptAsync(
                task,
                null,
                cancellationToken: default);

            return Ok(new PromptResponse
            {
                FeatureId = id,
                Style = style,
                Content = prompt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting prompt for feature: {FeatureId}", id);
            return NotFound(new ProblemDetails
            {
                Title = "Feature not found",
                Detail = $"Feature with ID '{id}' was not found",
                Status = StatusCodes.Status404NotFound
            });
        }
    }
}

/// <summary>
/// Request to update a feature
/// </summary>
public class UpdateFeatureRequestDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public List<string>? AcceptanceCriteria { get; set; }
    public List<string>? TechnicalNotes { get; set; }
    public FeaturePriority? Priority { get; set; }
}

/// <summary>
/// Response containing a formatted prompt
/// </summary>
public class PromptResponse
{
    public string FeatureId { get; set; } = "";
    public PromptStyle Style { get; set; }
    public string Content { get; set; } = "";
}
