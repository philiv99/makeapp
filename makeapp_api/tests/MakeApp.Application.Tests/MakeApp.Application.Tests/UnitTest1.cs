using FluentAssertions;
using MakeApp.Core.Configuration;
using MakeApp.Core.Interfaces;
using MakeApp.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Moq;

namespace MakeApp.Application.Tests;

/// <summary>
/// Unit tests for ConfigurationService
/// </summary>
public class ConfigurationServiceTests
{
    private readonly Mock<IOptionsMonitor<MakeAppOptions>> _makeAppOptionsMock;
    private readonly Mock<IOptionsMonitor<UserConfiguration>> _userOptionsMock;
    private readonly Mock<IGitHubService> _gitHubServiceMock;
    private readonly Mock<IFileSystem> _fileSystemMock;
    private readonly Mock<IFileOperations> _fileOperationsMock;
    private readonly Mock<IDirectoryOperations> _directoryOperationsMock;
    private readonly Mock<IPathOperations> _pathOperationsMock;
    private readonly ConfigurationService _sut;

    public ConfigurationServiceTests()
    {
        _makeAppOptionsMock = new Mock<IOptionsMonitor<MakeAppOptions>>();
        _userOptionsMock = new Mock<IOptionsMonitor<UserConfiguration>>();
        _gitHubServiceMock = new Mock<IGitHubService>();
        _fileSystemMock = new Mock<IFileSystem>();
        _fileOperationsMock = new Mock<IFileOperations>();
        _directoryOperationsMock = new Mock<IDirectoryOperations>();
        _pathOperationsMock = new Mock<IPathOperations>();

        // Setup default options
        var makeAppOptions = new MakeAppOptions
        {
            Folders = new FolderOptions { Sandbox = "/test/sandbox" },
            Agent = new AgentOptions { MaxRetries = 3 }
        };
        _makeAppOptionsMock.Setup(x => x.CurrentValue).Returns(makeAppOptions);

        var userConfig = new UserConfiguration
        {
            GitHubUsername = "testuser",
            DefaultBranch = "main"
        };
        _userOptionsMock.Setup(x => x.CurrentValue).Returns(userConfig);

        // Setup file system mocks
        _fileSystemMock.Setup(x => x.File).Returns(_fileOperationsMock.Object);
        _fileSystemMock.Setup(x => x.Directory).Returns(_directoryOperationsMock.Object);
        _fileSystemMock.Setup(x => x.Path).Returns(_pathOperationsMock.Object);
        _pathOperationsMock.Setup(x => x.Combine(It.IsAny<string[]>()))
            .Returns((string[] paths) => string.Join("/", paths));

        _sut = new ConfigurationService(
            _makeAppOptionsMock.Object,
            _userOptionsMock.Object,
            _gitHubServiceMock.Object,
            _fileSystemMock.Object);
    }

    [Fact]
    public async Task GetConfigurationAsync_ReturnsCurrentConfiguration()
    {
        // Act
        var result = await _sut.GetConfigurationAsync();

        // Assert
        result.Should().NotBeNull();
        result.Folders.Sandbox.Should().Be("/test/sandbox");
        result.Agent.MaxRetries.Should().Be(3);
    }

    [Fact]
    public async Task GetUserConfigurationAsync_ReturnsCurrentUserConfiguration()
    {
        // Act
        var result = await _sut.GetUserConfigurationAsync();

        // Assert
        result.Should().NotBeNull();
        result.GitHubUsername.Should().Be("testuser");
        result.DefaultBranch.Should().Be("main");
    }

    [Fact]
    public async Task GetDefaultConfigurationAsync_ReturnsFreshDefaultOptions()
    {
        // Act
        var result = await _sut.GetDefaultConfigurationAsync();

        // Assert
        result.Should().NotBeNull();
        result.Folders.Sandbox.Should().BeEmpty(); // Default is empty string
        result.Agent.MaxRetries.Should().Be(3); // Default value
        result.Git.DefaultBranch.Should().Be("main");
    }

    [Fact]
    public async Task ValidateUserConfigurationAsync_ValidCredentials_ReturnsValid()
    {
        // Arrange
        _gitHubServiceMock.Setup(x => x.GetCurrentUserAsync())
            .ReturnsAsync(new GitHubUser 
            { 
                Login = "testuser",
                Scopes = new List<string> { "repo", "read:org" }
            });

        _directoryOperationsMock.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
        _fileOperationsMock.Setup(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);
        _fileOperationsMock.Setup(x => x.Delete(It.IsAny<string>()));

        // Act
        var result = await _sut.ValidateUserConfigurationAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.GitHubUsername.Should().Be("testuser");
        result.SandboxPathValid.Should().BeTrue();
        result.GitHubScopes.Should().Contain("repo");
    }

    [Fact]
    public async Task ValidateUserConfigurationAsync_InvalidCredentials_ReturnsInvalid()
    {
        // Arrange
        _gitHubServiceMock.Setup(x => x.GetCurrentUserAsync())
            .ThrowsAsync(new Exception("Unauthorized"));

        _directoryOperationsMock.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
        _fileOperationsMock.Setup(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);
        _fileOperationsMock.Setup(x => x.Delete(It.IsAny<string>()));

        // Act
        var result = await _sut.ValidateUserConfigurationAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainMatch("*GitHub authentication failed*");
    }

    [Fact]
    public async Task ValidateUserConfigurationAsync_MissingRepoScope_AddsWarning()
    {
        // Arrange
        _gitHubServiceMock.Setup(x => x.GetCurrentUserAsync())
            .ReturnsAsync(new GitHubUser 
            { 
                Login = "testuser",
                Scopes = new List<string> { "read:org" } // Missing "repo" scope
            });

        _directoryOperationsMock.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
        _fileOperationsMock.Setup(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);
        _fileOperationsMock.Setup(x => x.Delete(It.IsAny<string>()));

        // Act
        var result = await _sut.ValidateUserConfigurationAsync();

        // Assert
        result.Should().NotBeNull();
        result.Warnings.Should().ContainMatch("*Missing 'repo' scope*");
    }

    [Fact]
    public async Task ValidateUserConfigurationAsync_InvalidSandboxPath_ReturnsInvalid()
    {
        // Arrange
        _gitHubServiceMock.Setup(x => x.GetCurrentUserAsync())
            .ReturnsAsync(new GitHubUser 
            { 
                Login = "testuser",
                Scopes = new List<string> { "repo" }
            });

        _directoryOperationsMock.Setup(x => x.Exists(It.IsAny<string>())).Returns(false);
        _directoryOperationsMock.Setup(x => x.CreateDirectory(It.IsAny<string>()))
            .Throws(new UnauthorizedAccessException("Access denied"));

        // Act
        var result = await _sut.ValidateUserConfigurationAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.SandboxPathValid.Should().BeFalse();
        result.Errors.Should().ContainMatch("*Sandbox path not writable*");
    }

    [Fact]
    public async Task UpdateUserConfigurationAsync_ValidConfig_PersistsToFile()
    {
        // Arrange
        var newConfig = new UserConfiguration
        {
            GitHubUsername = "newuser",
            DefaultBranch = "develop",
            SandboxPath = "/new/sandbox"
        };

        _directoryOperationsMock.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
        _fileOperationsMock.Setup(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.UpdateUserConfigurationAsync(newConfig);

        // Assert
        result.Should().NotBeNull();
        result.GitHubUsername.Should().Be("newuser");
        _fileOperationsMock.Verify(x => x.WriteAllTextAsync(
            It.IsAny<string>(), 
            It.Is<string>(json => json.Contains("newuser")),
            default), Times.Once);
    }
}
