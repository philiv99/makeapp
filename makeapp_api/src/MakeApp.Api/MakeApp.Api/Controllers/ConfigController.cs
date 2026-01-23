using Asp.Versioning;
using MakeApp.Core.Configuration;
using MakeApp.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MakeApp.Api.Controllers;

/// <summary>
/// Controller for managing MakeApp configuration
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<ConfigController> _logger;

    public ConfigController(
        IConfigurationService configurationService,
        ILogger<ConfigController> logger)
    {
        _configurationService = configurationService;
        _logger = logger;
    }

    /// <summary>
    /// Get current MakeApp configuration
    /// </summary>
    /// <returns>The current configuration</returns>
    [HttpGet]
    [ProducesResponseType(typeof(MakeAppOptions), StatusCodes.Status200OK)]
    public async Task<ActionResult<MakeAppOptions>> GetConfiguration()
    {
        _logger.LogInformation("Getting MakeApp configuration");
        var config = await _configurationService.GetConfigurationAsync();
        return Ok(config);
    }

    /// <summary>
    /// Update MakeApp configuration
    /// </summary>
    /// <param name="options">The configuration options to update</param>
    /// <returns>The updated configuration</returns>
    [HttpPut]
    [ProducesResponseType(typeof(MakeAppOptions), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MakeAppOptions>> UpdateConfiguration([FromBody] MakeAppOptions options)
    {
        if (options == null)
        {
            return BadRequest("Configuration options cannot be null");
        }

        _logger.LogInformation("Updating MakeApp configuration");
        var updated = await _configurationService.UpdateConfigurationAsync(options);
        return Ok(updated);
    }

    /// <summary>
    /// Get default configuration values
    /// </summary>
    /// <returns>The default configuration</returns>
    [HttpGet("defaults")]
    [ProducesResponseType(typeof(MakeAppOptions), StatusCodes.Status200OK)]
    public async Task<ActionResult<MakeAppOptions>> GetDefaults()
    {
        _logger.LogInformation("Getting default configuration");
        var defaults = await _configurationService.GetDefaultConfigurationAsync();
        return Ok(defaults);
    }

    /// <summary>
    /// Get current user configuration
    /// </summary>
    /// <returns>The current user configuration</returns>
    [HttpGet("user")]
    [ProducesResponseType(typeof(UserConfiguration), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserConfiguration>> GetUserConfiguration()
    {
        _logger.LogInformation("Getting user configuration");
        var config = await _configurationService.GetUserConfigurationAsync();
        return Ok(config);
    }

    /// <summary>
    /// Update user configuration
    /// </summary>
    /// <param name="configuration">The user configuration to update</param>
    /// <returns>The updated user configuration</returns>
    [HttpPut("user")]
    [ProducesResponseType(typeof(UserConfiguration), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserConfiguration>> UpdateUserConfiguration([FromBody] UserConfiguration configuration)
    {
        if (configuration == null)
        {
            return BadRequest("User configuration cannot be null");
        }

        _logger.LogInformation("Updating user configuration for {Username}", configuration.GitHubUsername);
        var updated = await _configurationService.UpdateUserConfigurationAsync(configuration);
        return Ok(updated);
    }

    /// <summary>
    /// Validate current user configuration
    /// </summary>
    /// <remarks>
    /// Validates GitHub credentials and sandbox path access.
    /// Returns validation results including any errors or warnings.
    /// </remarks>
    /// <returns>Validation result</returns>
    [HttpGet("user/validate")]
    [ProducesResponseType(typeof(ConfigurationValidationResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<ConfigurationValidationResult>> ValidateUserConfiguration()
    {
        _logger.LogInformation("Validating user configuration");
        var result = await _configurationService.ValidateUserConfigurationAsync();
        return Ok(result);
    }
}
