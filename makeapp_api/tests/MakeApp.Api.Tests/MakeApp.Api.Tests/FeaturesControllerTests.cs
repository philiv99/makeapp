using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MakeApp.Api.Controllers;
using MakeApp.Application.DTOs;
using MakeApp.Application.Services;
using MakeApp.Core.Entities;
using MakeApp.Core.Enums;
using MakeApp.Core.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MakeApp.Api.Tests;

/// <summary>
/// Integration tests for Features endpoints
/// </summary>
public class FeaturesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public FeaturesControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    #region GET /api/v1/features Tests

    [Fact]
    public async Task ListFeatures_ReturnsOkWithEmptyList()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/features");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var features = await response.Content.ReadFromJsonAsync<FeatureResponse[]>();
        features.Should().BeEmpty();
    }

    [Fact]
    public async Task ListFeatures_WithStatusFilter_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/features?status=Draft");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListFeatures_WithRepositoryFilter_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/features?repositoryOwner=testuser&repositoryName=test-repo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region GET /api/v1/features/{id} Tests

    [Fact]
    public async Task GetFeature_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/features/nonexistent-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/v1/features Tests

    [Fact]
    public async Task CreateFeature_WithMissingTitle_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new CreateFeatureRequestDto
        {
            Title = "", // Empty title should fail
            RepositoryOwner = "testuser",
            RepositoryName = "test-repo"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/features", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateFeature_WithMissingRepository_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new CreateFeatureRequestDto
        {
            Title = "Test Feature",
            RepositoryOwner = "",
            RepositoryName = ""
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/features", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateFeature_WithNonExistentRepository_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureServices(services =>
            {
                var mockRepoService = new Mock<IRepositoryService>();
                mockRepoService.Setup(s => s.GetRepositoryPathAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync((string?)null);

                ReplaceService(services, mockRepoService.Object);
            });
        }).CreateClient();

        var request = new CreateFeatureRequestDto
        {
            Title = "Test Feature",
            Description = "A test feature",
            RepositoryOwner = "testuser",
            RepositoryName = "nonexistent-repo"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/features", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateFeature_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var expectedResponse = new FeatureResponse
        {
            Id = "feat-123",
            Title = "Test Feature",
            Description = "A test feature description",
            Status = FeatureStatus.Ready,
            RepositoryOwner = "testuser",
            RepositoryName = "test-repo",
            BaseBranch = "main",
            FeatureBranch = "feature/test-feature"
        };

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureServices(services =>
            {
                var mockRepoService = new Mock<IRepositoryService>();
                mockRepoService.Setup(s => s.GetRepositoryPathAsync("testuser", "test-repo"))
                    .ReturnsAsync("/repos/test-repo");

                var mockBranchService = new Mock<IBranchService>();
                mockBranchService.Setup(s => s.FormatFeatureBranchName(It.IsAny<string>()))
                    .Returns("feature/test-feature");
                mockBranchService.Setup(s => s.CreateFeatureBranchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new BranchInfo { Name = "feature/test-feature" });
                mockBranchService.Setup(s => s.SwitchToBranchAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(true);

                ReplaceService(services, mockRepoService.Object);
                ReplaceService(services, mockBranchService.Object);
            });
        }).CreateClient();

        var request = new CreateFeatureRequestDto
        {
            Title = "Test Feature",
            Description = "A test feature description",
            RepositoryOwner = "testuser",
            RepositoryName = "test-repo",
            AcceptanceCriteria = new List<string> { "AC1", "AC2" },
            TechnicalNotes = new List<string> { "Note 1" },
            Priority = FeaturePriority.High
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/features", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var result = await response.Content.ReadFromJsonAsync<FeatureResponse>();
        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Feature");
        result.Status.Should().Be(FeatureStatus.Ready);
        result.FeatureBranch.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region PUT /api/v1/features/{id} Tests

    [Fact]
    public async Task UpdateFeature_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new UpdateFeatureRequestDto
        {
            Title = "Updated Title"
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/v1/features/nonexistent-id", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region DELETE /api/v1/features/{id} Tests

    [Fact]
    public async Task DeleteFeature_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.DeleteAsync("/api/v1/features/nonexistent-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GET /api/v1/features/{id}/status Tests

    [Fact]
    public async Task GetFeatureStatus_ReturnsOkWithStatus()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/features/feat-123/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = await response.Content.ReadFromJsonAsync<FeatureStatusResponse>();
        status.Should().NotBeNull();
        status!.Id.Should().Be("feat-123");
        status.Progress.Should().NotBeNull();
    }

    #endregion

    #region POST /api/v1/features/{id}/start Tests

    [Fact]
    public async Task StartFeature_WithValidId_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsync("/api/v1/features/feat-123/start", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<FeatureResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be(FeatureStatus.InProgress);
    }

    #endregion

    #region POST /api/v1/features/{id}/cancel Tests

    [Fact]
    public async Task CancelFeature_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsync("/api/v1/features/feat-123/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    #endregion

    #region GET /api/v1/features/{id}/prompt Tests

    [Fact]
    public async Task GetFeaturePrompt_WithDefaultStyle_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/features/feat-123/prompt");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PromptResponse>();
        result.Should().NotBeNull();
        result!.FeatureId.Should().Be("feat-123");
        result.Content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetFeaturePrompt_WithStructuredStyle_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/features/feat-123/prompt?style=Structured");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PromptResponse>();
        result.Should().NotBeNull();
        result!.Style.Should().Be(PromptStyle.Structured);
    }

    [Fact]
    public async Task GetFeaturePrompt_WithConversationalStyle_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/features/feat-123/prompt?style=Conversational");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PromptResponse>();
        result.Should().NotBeNull();
        result!.Style.Should().Be(PromptStyle.Conversational);
    }

    [Fact]
    public async Task GetFeaturePrompt_WithMinimalStyle_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/features/feat-123/prompt?style=Minimal");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PromptResponse>();
        result.Should().NotBeNull();
        result!.Style.Should().Be(PromptStyle.Minimal);
    }

    #endregion

    #region Helper Methods

    private static void ReplaceService<T>(IServiceCollection services, T implementation) where T : class
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor != null)
        {
            services.Remove(descriptor);
        }
        services.AddScoped(_ => implementation);
    }

    #endregion
}
