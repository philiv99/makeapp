using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MakeApp.Core.Configuration;
using MakeApp.Core.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace MakeApp.Api.Tests;

/// <summary>
/// Custom factory that handles Serilog issues in tests
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
        });
    }
}

/// <summary>
/// Integration tests for Health endpoints
/// </summary>
public class HealthEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHealthReady_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHealthLive_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

/// <summary>
/// Integration tests for Config endpoints
/// </summary>
public class ConfigEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ConfigEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithMocks()
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureServices(services =>
            {
                // Remove the real service and add a mock
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IConfigurationService));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                var mockConfigService = new Mock<IConfigurationService>();
                mockConfigService.Setup(x => x.GetConfigurationAsync())
                    .ReturnsAsync(new MakeAppOptions
                    {
                        Folders = new FolderOptions { Sandbox = "/test/sandbox" }
                    });

                mockConfigService.Setup(x => x.GetUserConfigurationAsync())
                    .ReturnsAsync(new UserConfiguration
                    {
                        GitHubUsername = "testuser",
                        DefaultBranch = "main"
                    });

                mockConfigService.Setup(x => x.GetDefaultConfigurationAsync())
                    .ReturnsAsync(new MakeAppOptions());

                mockConfigService.Setup(x => x.ValidateUserConfigurationAsync())
                    .ReturnsAsync(new ConfigurationValidationResult
                    {
                        IsValid = true,
                        GitHubUsername = "testuser",
                        SandboxPathValid = true
                    });

                services.AddScoped<IConfigurationService>(_ => mockConfigService.Object);
            });
        }).CreateClient();
    }

    [Fact]
    public async Task GetConfig_ReturnsOkWithConfiguration()
    {
        // Arrange
        var client = CreateClientWithMocks();

        // Act
        var response = await client.GetAsync("/api/v1/config");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var config = await response.Content.ReadFromJsonAsync<MakeAppOptions>();
        config.Should().NotBeNull();
        config!.Folders.Sandbox.Should().Be("/test/sandbox");
    }

    [Fact]
    public async Task GetConfigDefaults_ReturnsOkWithDefaults()
    {
        // Arrange
        var client = CreateClientWithMocks();

        // Act
        var response = await client.GetAsync("/api/v1/config/defaults");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var defaults = await response.Content.ReadFromJsonAsync<MakeAppOptions>();
        defaults.Should().NotBeNull();
    }

    [Fact]
    public async Task GetConfigUser_ReturnsOkWithUserConfiguration()
    {
        // Arrange
        var client = CreateClientWithMocks();

        // Act
        var response = await client.GetAsync("/api/v1/config/user");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userConfig = await response.Content.ReadFromJsonAsync<UserConfiguration>();
        userConfig.Should().NotBeNull();
        userConfig!.GitHubUsername.Should().Be("testuser");
    }

    [Fact]
    public async Task GetConfigUserValidate_ReturnsOkWithValidationResult()
    {
        // Arrange
        var client = CreateClientWithMocks();

        // Act
        var response = await client.GetAsync("/api/v1/config/user/validate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ConfigurationValidationResult>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeTrue();
        result.GitHubUsername.Should().Be("testuser");
    }

    [Fact]
    public async Task PutConfig_ValidInput_ReturnsOk()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IConfigurationService));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                var mockConfigService = new Mock<IConfigurationService>();
                mockConfigService.Setup(x => x.UpdateConfigurationAsync(It.IsAny<MakeAppOptions>()))
                    .ReturnsAsync((MakeAppOptions opts) => opts);

                services.AddScoped<IConfigurationService>(_ => mockConfigService.Object);
            });
        });
        var client = factory.CreateClient();
        var newConfig = new MakeAppOptions
        {
            Folders = new FolderOptions { Sandbox = "/new/sandbox" }
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/v1/config", newConfig);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PutConfigUser_ValidInput_ReturnsOk()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IConfigurationService));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                var mockConfigService = new Mock<IConfigurationService>();
                mockConfigService.Setup(x => x.UpdateUserConfigurationAsync(It.IsAny<UserConfiguration>()))
                    .ReturnsAsync((UserConfiguration cfg) => cfg);

                services.AddScoped<IConfigurationService>(_ => mockConfigService.Object);
            });
        });
        var client = factory.CreateClient();
        var newConfig = new UserConfiguration
        {
            GitHubUsername = "newuser",
            DefaultBranch = "develop"
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/v1/config/user", newConfig);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UserConfiguration>();
        result.Should().NotBeNull();
        result!.GitHubUsername.Should().Be("newuser");
    }
}
