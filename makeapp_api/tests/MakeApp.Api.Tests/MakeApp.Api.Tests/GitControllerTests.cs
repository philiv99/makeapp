using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MakeApp.Api.Controllers;
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
/// Integration tests for Git endpoints
/// </summary>
public class GitControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public GitControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetUnstagedChanges_WithValidRepo_ReturnsOk()
    {
        // Arrange
        var changes = new[]
        {
            new FileChange { FilePath = "src/main.cs", ChangeType = ChangeType.Modified },
            new FileChange { FilePath = "README.md", ChangeType = ChangeType.Modified }
        };

        var client = CreateClientWithMocks(
            repoPath: "/repos/test-repo",
            configureGitService: mock =>
            {
                mock.Setup(s => s.GetUnstagedChangesAsync("/repos/test-repo"))
                    .ReturnsAsync(changes);
            });

        // Act
        var response = await client.GetAsync("/api/v1/repos/testuser/test-repo/git/changes/unstaged");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<FileChange[]>();
        result.Should().HaveCount(2);
        result![0].FilePath.Should().Be("src/main.cs");
    }

    [Fact]
    public async Task GetUnstagedChanges_WithNonExistentRepo_ReturnsNotFound()
    {
        // Arrange
        var client = CreateClientWithMocks(repoPath: null);

        // Act
        var response = await client.GetAsync("/api/v1/repos/nonexistent/repo/git/changes/unstaged");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetStagedChanges_WithValidRepo_ReturnsOk()
    {
        // Arrange
        var changes = new[]
        {
            new FileChange { FilePath = "src/new-file.cs", ChangeType = ChangeType.Added }
        };

        var client = CreateClientWithMocks(
            repoPath: "/repos/test-repo",
            configureGitService: mock =>
            {
                mock.Setup(s => s.GetStagedChangesAsync("/repos/test-repo"))
                    .ReturnsAsync(changes);
            });

        // Act
        var response = await client.GetAsync("/api/v1/repos/testuser/test-repo/git/changes/staged");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<FileChange[]>();
        result.Should().HaveCount(1);
        result![0].ChangeType.Should().Be(ChangeType.Added);
    }

    [Fact]
    public async Task GetUnpushedCommits_WithValidRepo_ReturnsOk()
    {
        // Arrange
        var commits = new[]
        {
            new CommitInfo
            {
                Sha = "abc123",
                Message = "Add new feature",
                Author = "Test User",
                Date = DateTime.UtcNow
            }
        };

        var client = CreateClientWithMocks(
            repoPath: "/repos/test-repo",
            configureGitService: mock =>
            {
                mock.Setup(s => s.GetUnpushedCommitsAsync("/repos/test-repo", "origin"))
                    .ReturnsAsync(commits);
            });

        // Act
        var response = await client.GetAsync("/api/v1/repos/testuser/test-repo/git/commits/unpushed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CommitInfo[]>();
        result.Should().HaveCount(1);
        result![0].Sha.Should().Be("abc123");
    }

    [Fact]
    public async Task StageChanges_WithStageAll_ReturnsOk()
    {
        // Arrange
        var status = new GitStatus { StagedCount = 3 };

        var client = CreateClientWithMocks(
            repoPath: "/repos/test-repo",
            configureGitService: mock =>
            {
                mock.Setup(s => s.StageAllAsync("/repos/test-repo"))
                    .ReturnsAsync(true);
                mock.Setup(s => s.GetStatusAsync("/repos/test-repo"))
                    .ReturnsAsync(status);
            });

        var request = new StageRequest { StageAll = true };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/repos/testuser/test-repo/git/stage", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<StageResult>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.StagedCount.Should().Be(3);
    }

    [Fact]
    public async Task StageChanges_WithPathSpec_ReturnsOk()
    {
        // Arrange
        var status = new GitStatus { StagedCount = 1 };

        var client = CreateClientWithMocks(
            repoPath: "/repos/test-repo",
            configureGitService: mock =>
            {
                mock.Setup(s => s.StageChangesAsync("/repos/test-repo", "src/*.cs"))
                    .ReturnsAsync(true);
                mock.Setup(s => s.GetStatusAsync("/repos/test-repo"))
                    .ReturnsAsync(status);
            });

        var request = new StageRequest { PathSpec = "src/*.cs" };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/repos/testuser/test-repo/git/stage", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task StageChanges_WithNonExistentRepo_ReturnsNotFound()
    {
        // Arrange
        var client = CreateClientWithMocks(repoPath: null);

        var request = new StageRequest { StageAll = true };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/repos/nonexistent/repo/git/stage", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateCommit_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var commitResult = new CommitResult
        {
            Success = true,
            CommitSha = "abc123def456"
        };

        var client = CreateClientWithMocks(
            repoPath: "/repos/test-repo",
            configureGitService: mock =>
            {
                mock.Setup(s => s.CommitAsync("/repos/test-repo", "Add new feature", It.IsAny<CommitOptions>()))
                    .ReturnsAsync(commitResult);
            });

        var request = new CreateCommitRequest
        {
            Message = "Add new feature"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/repos/testuser/test-repo/git/commit", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CommitResult>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.CommitSha.Should().Be("abc123def456");
    }

    [Fact]
    public async Task CreateCommit_WithStageAll_StagesBeforeCommit()
    {
        // Arrange
        var commitResult = new CommitResult { Success = true, CommitSha = "abc123" };
        var stageAllCalled = false;

        var client = CreateClientWithMocks(
            repoPath: "/repos/test-repo",
            configureGitService: mock =>
            {
                mock.Setup(s => s.StageAllAsync("/repos/test-repo"))
                    .Callback(() => stageAllCalled = true)
                    .ReturnsAsync(true);
                mock.Setup(s => s.CommitAsync("/repos/test-repo", "Test commit", It.IsAny<CommitOptions>()))
                    .ReturnsAsync(commitResult);
            });

        var request = new CreateCommitRequest
        {
            Message = "Test commit",
            StageAll = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/repos/testuser/test-repo/git/commit", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        stageAllCalled.Should().BeTrue();
    }

    [Fact]
    public async Task CreateCommit_WithEmptyMessage_ReturnsBadRequest()
    {
        // Arrange
        var client = CreateClientWithMocks(repoPath: "/repos/test-repo");

        var request = new CreateCommitRequest
        {
            Message = ""
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/repos/testuser/test-repo/git/commit", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCommit_WithNonExistentRepo_ReturnsNotFound()
    {
        // Arrange
        var client = CreateClientWithMocks(repoPath: null);

        var request = new CreateCommitRequest
        {
            Message = "Test commit"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/repos/nonexistent/repo/git/commit", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PushChanges_WithValidRepo_ReturnsOk()
    {
        // Arrange
        var pushResult = new PushResult { Success = true };

        var client = CreateClientWithMocks(
            repoPath: "/repos/test-repo",
            configureGitService: mock =>
            {
                mock.Setup(s => s.PushAsync("/repos/test-repo", It.IsAny<PushOptions>()))
                    .ReturnsAsync(pushResult);
            });

        var request = new PushRequest();

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/repos/testuser/test-repo/git/push", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PushResult>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task PushChanges_WithSetUpstream_PassesOption()
    {
        // Arrange
        var pushResult = new PushResult { Success = true };
        PushOptions? capturedOptions = null;

        var client = CreateClientWithMocks(
            repoPath: "/repos/test-repo",
            configureGitService: mock =>
            {
                mock.Setup(s => s.PushAsync("/repos/test-repo", It.IsAny<PushOptions>()))
                    .Callback<string, PushOptions>((_, opts) => capturedOptions = opts)
                    .ReturnsAsync(pushResult);
            });

        var request = new PushRequest
        {
            SetUpstream = true,
            RemoteName = "origin"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/repos/testuser/test-repo/git/push", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        capturedOptions.Should().NotBeNull();
        capturedOptions!.SetUpstream.Should().BeTrue();
    }

    [Fact]
    public async Task PushChanges_WithNonExistentRepo_ReturnsNotFound()
    {
        // Arrange
        var client = CreateClientWithMocks(repoPath: null);

        var request = new PushRequest();

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/repos/nonexistent/repo/git/push", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreatePullRequest_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var prInfo = new PullRequestInfo
        {
            Number = 42,
            Title = "Add new feature",
            HtmlUrl = "https://github.com/testuser/test-repo/pull/42",
            State = "open"
        };

        var client = CreateClientWithMocks(
            repoPath: "/repos/test-repo",
            configureGitHubService: mock =>
            {
                mock.Setup(s => s.CreatePullRequestAsync(It.IsAny<CreatePullRequestOptions>()))
                    .ReturnsAsync(prInfo);
            });

        var request = new CreatePullRequestRequest
        {
            Title = "Add new feature",
            Body = "This PR adds a new feature",
            Head = "feature/new-feature",
            Base = "main"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/repos/testuser/test-repo/git/pull-requests", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<PullRequestInfo>();
        result.Should().NotBeNull();
        result!.Number.Should().Be(42);
        result.Title.Should().Be("Add new feature");
    }

    [Fact]
    public async Task CreatePullRequest_WithEmptyTitle_ReturnsBadRequest()
    {
        // Arrange
        var client = CreateClientWithMocks(repoPath: "/repos/test-repo");

        var request = new CreatePullRequestRequest
        {
            Title = "",
            Head = "feature/test"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/repos/testuser/test-repo/git/pull-requests", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePullRequest_WithEmptyHead_ReturnsBadRequest()
    {
        // Arrange
        var client = CreateClientWithMocks(repoPath: "/repos/test-repo");

        var request = new CreatePullRequestRequest
        {
            Title = "Test PR",
            Head = ""
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/repos/testuser/test-repo/git/pull-requests", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePullRequest_WithNonExistentRepo_ReturnsNotFound()
    {
        // Arrange
        var client = CreateClientWithMocks(repoPath: null);

        var request = new CreatePullRequestRequest
        {
            Title = "Test PR",
            Head = "feature/test"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/repos/nonexistent/repo/git/pull-requests", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPullRequest_WithValidNumber_ReturnsOk()
    {
        // Arrange
        var prInfo = new PullRequestInfo
        {
            Number = 42,
            Title = "Test PR",
            State = "open"
        };

        var client = CreateClientWithMocks(
            repoPath: "/repos/test-repo",
            configureGitHubService: mock =>
            {
                mock.Setup(s => s.GetPullRequestAsync("testuser", "test-repo", 42))
                    .ReturnsAsync(prInfo);
            });

        // Act
        var response = await client.GetAsync("/api/v1/repos/testuser/test-repo/git/pull-requests/42");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PullRequestInfo>();
        result.Should().NotBeNull();
        result!.Number.Should().Be(42);
    }

    [Fact]
    public async Task GetPullRequest_WithNonExistentPR_ReturnsNotFound()
    {
        // Arrange
        var client = CreateClientWithMocks(
            repoPath: "/repos/test-repo",
            configureGitHubService: mock =>
            {
                mock.Setup(s => s.GetPullRequestAsync("testuser", "test-repo", 999))
                    .ReturnsAsync((PullRequestInfo?)null);
            });

        // Act
        var response = await client.GetAsync("/api/v1/repos/testuser/test-repo/git/pull-requests/999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private HttpClient CreateClientWithMocks(
        string? repoPath,
        Action<Mock<IGitService>>? configureGitService = null,
        Action<Mock<IGitHubService>>? configureGitHubService = null)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureServices(services =>
            {
                // Mock repository service
                var mockRepoService = new Mock<IRepositoryService>();
                mockRepoService.Setup(s => s.GetRepositoryPathAsync("testuser", "test-repo"))
                    .ReturnsAsync(repoPath);
                mockRepoService.Setup(s => s.GetRepositoryPathAsync("nonexistent", "repo"))
                    .ReturnsAsync((string?)null);

                ReplaceService(services, mockRepoService.Object);

                // Mock git service
                var mockGitService = new Mock<IGitService>();
                configureGitService?.Invoke(mockGitService);
                ReplaceService(services, mockGitService.Object);

                // Mock GitHub service
                var mockGitHubService = new Mock<IGitHubService>();
                configureGitHubService?.Invoke(mockGitHubService);
                ReplaceService(services, mockGitHubService.Object);
            });
        }).CreateClient();
    }

    private static void ReplaceService<T>(IServiceCollection services, T implementation) where T : class
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor != null)
        {
            services.Remove(descriptor);
        }
        services.AddScoped(_ => implementation);
    }
}
