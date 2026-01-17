using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MakeApp.Application.DTOs;
using Xunit;

namespace MakeApp.Api.Tests;

public class AppsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AppsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListApps_ReturnsEmptyArray()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/apps");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var apps = await response.Content.ReadFromJsonAsync<AppResponse[]>();
        apps.Should().BeEmpty();
    }

    [Fact]
    public async Task GetApp_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/apps/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAppStatus_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/apps/nonexistent/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteApp_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.DeleteAsync("/api/v1/apps/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateApp_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateAppRequest
        {
            Name = "",
            Requirements = "Build a test app"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/apps", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateApp_WithNullName_ReturnsBadRequest()
    {
        // Arrange
        var request = new { Requirements = "Build a test app" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/apps", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
