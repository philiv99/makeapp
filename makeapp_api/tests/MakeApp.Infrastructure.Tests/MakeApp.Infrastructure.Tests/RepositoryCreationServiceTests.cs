using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MakeApp.Core.Configuration;
using MakeApp.Core.Entities;
using MakeApp.Core.Interfaces;
using MakeApp.Infrastructure.Services;
using Xunit;

namespace MakeApp.Infrastructure.Tests;

public class RepositoryCreationServiceTests
{
    private readonly Mock<IGitHubService> _gitHubServiceMock;
    private readonly Mock<IGitService> _gitServiceMock;
    private readonly Mock<IFileSystem> _fileSystemMock;
    private readonly Mock<IAgentConfigurationService> _agentConfigServiceMock;
    private readonly Mock<IOptions<MakeAppOptions>> _optionsMock;
    private readonly Mock<ILogger<RepositoryCreationService>> _loggerMock;
    private readonly Mock<IFileOperations> _fileOpsMock;
    private readonly Mock<IDirectoryOperations> _dirOpsMock;
    private readonly Mock<IPathOperations> _pathOpsMock;
    private readonly RepositoryCreationService _service;

    public RepositoryCreationServiceTests()
    {
        _gitHubServiceMock = new Mock<IGitHubService>();
        _gitServiceMock = new Mock<IGitService>();
        _fileSystemMock = new Mock<IFileSystem>();
        _agentConfigServiceMock = new Mock<IAgentConfigurationService>();
        _optionsMock = new Mock<IOptions<MakeAppOptions>>();
        _loggerMock = new Mock<ILogger<RepositoryCreationService>>();
        _fileOpsMock = new Mock<IFileOperations>();
        _dirOpsMock = new Mock<IDirectoryOperations>();
        _pathOpsMock = new Mock<IPathOperations>();

        // Setup file system mock
        _fileSystemMock.Setup(x => x.File).Returns(_fileOpsMock.Object);
        _fileSystemMock.Setup(x => x.Directory).Returns(_dirOpsMock.Object);
        _fileSystemMock.Setup(x => x.Path).Returns(_pathOpsMock.Object);
        
        // Setup path operations
        _pathOpsMock.Setup(x => x.Combine(It.IsAny<string[]>()))
            .Returns((string[] paths) => string.Join("/", paths));

        // Setup options
        _optionsMock.Setup(x => x.Value).Returns(new MakeAppOptions
        {
            Folders = new FolderOptions { Sandbox = "C:/repos" }
        });

        _service = new RepositoryCreationService(
            _gitHubServiceMock.Object,
            _gitServiceMock.Object,
            _fileSystemMock.Object,
            _agentConfigServiceMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreateAppAsync_WithValidRequest_ReturnsSuccessResult()
    {
        // Arrange
        var request = new CreateAppRequest
        {
            Name = "test-app",
            Description = "Test application",
            Owner = "testuser",
            IsPrivate = true,
            Requirements = new AppRequirements
            {
                Name = "test-app",
                Description = "Test application",
                ProjectType = "dotnet"
            }
        };

        _gitHubServiceMock.Setup(x => x.GetCurrentUserAsync())
            .ReturnsAsync(new GitHubUser { Login = "testuser" });

        _gitHubServiceMock.Setup(x => x.CreateRepositoryAsync(It.IsAny<CreateRepoOptions>()))
            .ReturnsAsync(new RepositoryInfo 
            { 
                Name = "test-app",
                HtmlUrl = "https://github.com/testuser/test-app",
                CloneUrl = "https://github.com/testuser/test-app.git"
            });

        _gitServiceMock.Setup(x => x.CloneAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CloneOptions>()))
            .ReturnsAsync("C:/repos/test-app");

        _dirOpsMock.Setup(x => x.Exists(It.IsAny<string>())).Returns(false);
        _dirOpsMock.Setup(x => x.CreateDirectory(It.IsAny<string>()))
            .Returns(new DirectoryInfo("C:/repos/test-app"));

        _fileOpsMock.Setup(x => x.Exists(It.IsAny<string>())).Returns(false);
        _fileOpsMock.Setup(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _agentConfigServiceMock.Setup(x => x.GetDefaultConfiguration(It.IsAny<string>()))
            .Returns(new AgentConfiguration());
        _agentConfigServiceMock.Setup(x => x.GenerateCopilotInstructions(It.IsAny<AgentConfiguration>()))
            .Returns("# Copilot Instructions");
        _agentConfigServiceMock.Setup(x => x.SaveConfigurationAsync(It.IsAny<AgentConfiguration>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _gitServiceMock.Setup(x => x.StageAllAsync(It.IsAny<string>())).ReturnsAsync(true);
        _gitServiceMock.Setup(x => x.CommitAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CommitOptions>()))
            .ReturnsAsync(new CommitResult { Success = true });
        _gitServiceMock.Setup(x => x.PushAsync(It.IsAny<string>(), It.IsAny<PushOptions>()))
            .ReturnsAsync(new PushResult { Success = true });

        // Act
        var result = await _service.CreateAppAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AppName.Should().Be("test-app");
        result.Owner.Should().Be("testuser");
        result.RepositoryUrl.Should().Be("https://github.com/testuser/test-app");
    }

    [Fact]
    public async Task CreateAppAsync_WhenGitHubCreateFails_ReturnsFailureResult()
    {
        // Arrange
        var request = new CreateAppRequest
        {
            Name = "test-app",
            Owner = "testuser"
        };

        _gitHubServiceMock.Setup(x => x.GetCurrentUserAsync())
            .ReturnsAsync(new GitHubUser { Login = "testuser" });

        _gitHubServiceMock.Setup(x => x.CreateRepositoryAsync(It.IsAny<CreateRepoOptions>()))
            .ThrowsAsync(new Exception("Repository creation failed"));

        // Act
        var result = await _service.CreateAppAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Repository creation failed");
    }

    [Fact]
    public async Task InitializeMakeAppFoldersAsync_CreatesExpectedDirectories()
    {
        // Arrange
        var repoPath = "C:/repos/test-app";
        var createdDirs = new List<string>();

        _dirOpsMock.Setup(x => x.Exists(It.IsAny<string>())).Returns(false);
        _dirOpsMock.Setup(x => x.CreateDirectory(It.IsAny<string>()))
            .Callback<string>(path => createdDirs.Add(path))
            .Returns(new DirectoryInfo(repoPath));

        // Act
        await _service.InitializeMakeAppFoldersAsync(repoPath);

        // Assert
        createdDirs.Should().Contain(dir => dir.Contains(".makeapp"));
        createdDirs.Should().Contain(dir => dir.Contains("plans"));
        createdDirs.Should().Contain(dir => dir.Contains("features"));
        createdDirs.Should().Contain(dir => dir.Contains("memory"));
        createdDirs.Should().Contain(dir => dir.Contains("agents"));
    }

    [Fact]
    public async Task GenerateInitialFilesAsync_CreatesExpectedFiles()
    {
        // Arrange
        var repoPath = "C:/repos/test-app";
        var requirements = new AppRequirements
        {
            Name = "test-app",
            Description = "Test app",
            ProjectType = "dotnet"
        };

        var writtenFiles = new List<string>();

        _fileOpsMock.Setup(x => x.Exists(It.IsAny<string>())).Returns(false);
        _fileOpsMock.Setup(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((path, _, _) => writtenFiles.Add(path))
            .Returns(Task.CompletedTask);

        _agentConfigServiceMock.Setup(x => x.GetDefaultConfiguration(It.IsAny<string>()))
            .Returns(new AgentConfiguration());
        _agentConfigServiceMock.Setup(x => x.GenerateCopilotInstructions(It.IsAny<AgentConfiguration>()))
            .Returns("# Instructions");
        _agentConfigServiceMock.Setup(x => x.SaveConfigurationAsync(It.IsAny<AgentConfiguration>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.GenerateInitialFilesAsync(repoPath, requirements);

        // Assert
        result.Should().NotBeEmpty();
        writtenFiles.Should().Contain(f => f.Contains("copilot-instructions.md"));
        writtenFiles.Should().Contain(f => f.Contains("config.json"));
        writtenFiles.Should().Contain(f => f.Contains("initial-plan.md"));
    }
}
