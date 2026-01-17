using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MakeApp.Api.Controllers;
using MakeApp.Core.Entities;
using MakeApp.Core.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MakeApp.Api.Tests;

/// <summary>
/// Integration tests for Repos endpoints
/// </summary>
public class ReposControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ReposControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetRepositories_ReturnsOkWithEmptyList()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureServices(services =>
            {
                // Mock repository service
                var mockRepoService = new Mock<IRepositoryService>();
                mockRepoService.Setup(s => s.GetAvailableRepositoriesAsync(It.IsAny<string?>()))
                    .ReturnsAsync(Enumerable.Empty<RepositoryInfo>());

                ReplaceService(services, mockRepoService.Object);
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/repos");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var repos = await response.Content.ReadFromJsonAsync<RepositoryInfo[]>();
        repos.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRepositories_ReturnsOkWithRepoList()
    {
        // Arrange
        var testRepos = new[]
        {
            new RepositoryInfo
            {
                Name = "test-repo",
                Owner = "testuser",
                FullName = "testuser/test-repo",
                LocalPath = "/repos/test-repo"
            },
            new RepositoryInfo
            {
                Name = "another-repo",
                Owner = "testuser",
                FullName = "testuser/another-repo",
                LocalPath = "/repos/another-repo"
            }
        };

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureServices(services =>
            {
                var mockRepoService = new Mock<IRepositoryService>();
                mockRepoService.Setup(s => s.GetAvailableRepositoriesAsync(It.IsAny<string?>()))
                    .ReturnsAsync(testRepos);

                ReplaceService(services, mockRepoService.Object);
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/repos");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var repos = await response.Content.ReadFromJsonAsync<RepositoryInfo[]>();
        repos.Should().HaveCount(2);
        repos![0].Name.Should().Be("test-repo");
        repos[1].Name.Should().Be("another-repo");
    }

    [Fact]
    public async Task GetRepository_WithValidOwnerAndName_ReturnsOk()
    {
        // Arrange
        var testRepo = new RepositoryInfo
        {
            Name = "test-repo",
            Owner = "testuser",
            FullName = "testuser/test-repo",
            LocalPath = "/repos/test-repo",
            Description = "A test repository"
        };

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureServices(services =>
            {
                var mockRepoService = new Mock<IRepositoryService>();
                mockRepoService.Setup(s => s.GetRepositoryInfoAsync("testuser", "test-repo"))
                    .ReturnsAsync(testRepo);

                ReplaceService(services, mockRepoService.Object);
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/repos/testuser/test-repo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var repo = await response.Content.ReadFromJsonAsync<RepositoryInfo>();
        repo.Should().NotBeNull();
        repo!.Name.Should().Be("test-repo");
        repo.Owner.Should().Be("testuser");
    }

    [Fact]
    public async Task GetRepository_WithNonExistentRepo_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureServices(services =>
            {
                var mockRepoService = new Mock<IRepositoryService>();
                mockRepoService.Setup(s => s.GetRepositoryInfoAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync((RepositoryInfo?)null);

                ReplaceService(services, mockRepoService.Object);
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/repos/nonexistent/repo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetRepositoryStatus_WithValidRepo_ReturnsOk()
    {
        // Arrange
        var status = new RepositoryConfigStatus
        {
            HasCopilotInstructions = true,
            HasMakeAppFolder = true,
            HasAgentConfigs = true,
            HasMcpConfig = false,
            CopilotInstructionsPath = "/repos/test-repo/.github/copilot-instructions.md"
        };

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureServices(services =>
            {
                var mockRepoService = new Mock<IRepositoryService>();
                mockRepoService.Setup(s => s.GetRepositoryPathAsync("testuser", "test-repo"))
                    .ReturnsAsync("/repos/test-repo");
                mockRepoService.Setup(s => s.GetConfigurationStatusAsync("/repos/test-repo"))
                    .ReturnsAsync(status);

                ReplaceService(services, mockRepoService.Object);
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/repos/testuser/test-repo/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RepositoryConfigStatus>();
        result.Should().NotBeNull();
        result!.HasCopilotInstructions.Should().BeTrue();
        result.HasMakeAppFolder.Should().BeTrue();
        result.IsReady.Should().BeTrue();
    }

    [Fact]
    public async Task GetRepositoryStatus_WithNonExistentRepo_ReturnsNotFound()
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

        // Act
        var response = await client.GetAsync("/api/v1/repos/nonexistent/repo/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBranches_WithValidRepo_ReturnsOk()
    {
        // Arrange
        var branches = new[]
        {
            new BranchInfo { Name = "main", IsCurrent = true },
            new BranchInfo { Name = "feature/test", IsCurrent = false }
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
                mockBranchService.Setup(s => s.GetBranchesAsync("/repos/test-repo", false))
                    .ReturnsAsync(branches);

                ReplaceService(services, mockRepoService.Object);
                ReplaceService(services, mockBranchService.Object);
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/repos/testuser/test-repo/branches");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BranchInfo[]>();
        result.Should().HaveCount(2);
        result![0].Name.Should().Be("main");
        result[0].IsCurrent.Should().BeTrue();
    }

    [Fact]
    public async Task GetBranches_WithNonExistentRepo_ReturnsNotFound()
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

        // Act
        var response = await client.GetAsync("/api/v1/repos/nonexistent/repo/branches");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateBranch_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var newBranch = new BranchInfo
        {
            Name = "feature/new-feature",
            IsCurrent = true
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
                mockBranchService.Setup(s => s.CreateFeatureBranchAsync("/repos/test-repo", "new-feature", "main"))
                    .ReturnsAsync(newBranch);

                ReplaceService(services, mockRepoService.Object);
                ReplaceService(services, mockBranchService.Object);
            });
        }).CreateClient();

        var request = new CreateBranchRequest
        {
            BranchName = "new-feature",
            BaseBranch = "main"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/repos/testuser/test-repo/branches", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<BranchInfo>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("feature/new-feature");
    }

    [Fact]
    public async Task CreateBranch_WithEmptyBranchName_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureServices(services =>
            {
                var mockRepoService = new Mock<IRepositoryService>();
                mockRepoService.Setup(s => s.GetRepositoryPathAsync("testuser", "test-repo"))
                    .ReturnsAsync("/repos/test-repo");

                ReplaceService(services, mockRepoService.Object);
            });
        }).CreateClient();

        var request = new CreateBranchRequest
        {
            BranchName = ""
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/repos/testuser/test-repo/branches", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateBranch_WithNonExistentRepo_ReturnsNotFound()
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

        var request = new CreateBranchRequest
        {
            BranchName = "new-feature"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/repos/nonexistent/repo/branches", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CheckoutBranch_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureServices(services =>
            {
                var mockRepoService = new Mock<IRepositoryService>();
                mockRepoService.Setup(s => s.GetRepositoryPathAsync("testuser", "test-repo"))
                    .ReturnsAsync("/repos/test-repo");

                var mockBranchService = new Mock<IBranchService>();
                mockBranchService.Setup(s => s.SwitchToBranchAsync("/repos/test-repo", "feature/test"))
                    .ReturnsAsync(true);
                mockBranchService.Setup(s => s.GetCurrentBranchAsync("/repos/test-repo"))
                    .ReturnsAsync("feature/test");

                ReplaceService(services, mockRepoService.Object);
                ReplaceService(services, mockBranchService.Object);
            });
        }).CreateClient();

        var request = new CheckoutRequest
        {
            BranchName = "feature/test"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/repos/testuser/test-repo/checkout", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CheckoutResult>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.CurrentBranch.Should().Be("feature/test");
    }

    [Fact]
    public async Task CheckoutBranch_WithEmptyBranchName_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureServices(services =>
            {
                var mockRepoService = new Mock<IRepositoryService>();
                mockRepoService.Setup(s => s.GetRepositoryPathAsync("testuser", "test-repo"))
                    .ReturnsAsync("/repos/test-repo");

                ReplaceService(services, mockRepoService.Object);
            });
        }).CreateClient();

        var request = new CheckoutRequest
        {
            BranchName = ""
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/repos/testuser/test-repo/checkout", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteBranch_WithValidBranch_ReturnsNoContent()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureServices(services =>
            {
                var mockRepoService = new Mock<IRepositoryService>();
                mockRepoService.Setup(s => s.GetRepositoryPathAsync("testuser", "test-repo"))
                    .ReturnsAsync("/repos/test-repo");

                var mockBranchService = new Mock<IBranchService>();
                mockBranchService.Setup(s => s.DeleteBranchAsync("/repos/test-repo", "feature/old", false))
                    .ReturnsAsync(true);

                ReplaceService(services, mockRepoService.Object);
                ReplaceService(services, mockBranchService.Object);
            });
        }).CreateClient();

        // Act
        var response = await client.DeleteAsync("/api/v1/repos/testuser/test-repo/branches/feature/old");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteBranch_WithNonExistentRepo_ReturnsNotFound()
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

        // Act
        var response = await client.DeleteAsync("/api/v1/repos/nonexistent/repo/branches/feature/old");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetGitStatus_WithValidRepo_ReturnsOk()
    {
        // Arrange
        var status = new GitStatus
        {
            IsDirty = true,
            CurrentBranch = "main",
            StagedCount = 2,
            ModifiedCount = 3,
            UntrackedCount = 1,
            DeletedCount = 0
        };

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureServices(services =>
            {
                var mockRepoService = new Mock<IRepositoryService>();
                mockRepoService.Setup(s => s.GetRepositoryPathAsync("testuser", "test-repo"))
                    .ReturnsAsync("/repos/test-repo");

                var mockGitService = new Mock<IGitService>();
                mockGitService.Setup(s => s.GetStatusAsync("/repos/test-repo"))
                    .ReturnsAsync(status);

                ReplaceService(services, mockRepoService.Object);
                ReplaceService(services, mockGitService.Object);
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/repos/testuser/test-repo/git/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GitStatus>();
        result.Should().NotBeNull();
        result!.IsDirty.Should().BeTrue();
        result.CurrentBranch.Should().Be("main");
        result.StagedCount.Should().Be(2);
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
