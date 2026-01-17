using FluentAssertions;
using Moq;
using MakeApp.Core.Entities;
using MakeApp.Core.Interfaces;
using MakeApp.Infrastructure.Services;
using Xunit;

namespace MakeApp.Infrastructure.Tests;

/// <summary>
/// Unit tests for RepositoryService
/// </summary>
public class RepositoryServiceTests
{
    private readonly Mock<IFileSystem> _fileSystemMock;
    private readonly Mock<IFileOperations> _fileOpsMock;
    private readonly Mock<IDirectoryOperations> _dirOpsMock;
    private readonly Mock<IPathOperations> _pathOpsMock;
    private readonly RepositoryService _service;

    public RepositoryServiceTests()
    {
        _fileSystemMock = new Mock<IFileSystem>();
        _fileOpsMock = new Mock<IFileOperations>();
        _dirOpsMock = new Mock<IDirectoryOperations>();
        _pathOpsMock = new Mock<IPathOperations>();

        _fileSystemMock.Setup(x => x.File).Returns(_fileOpsMock.Object);
        _fileSystemMock.Setup(x => x.Directory).Returns(_dirOpsMock.Object);
        _fileSystemMock.Setup(x => x.Path).Returns(_pathOpsMock.Object);

        // Setup Path.Combine to work like the real thing
        _pathOpsMock.Setup(x => x.Combine(It.IsAny<string[]>()))
            .Returns((string[] paths) => string.Join("/", paths));
        
        // Setup Path.GetFileName to get the last segment
        _pathOpsMock.Setup(x => x.GetFileName(It.IsAny<string>()))
            .Returns((string path) => path.Split(new[] { '/', '\\' }).Last());

        _service = new RepositoryService(_fileSystemMock.Object, "C:/repos");
    }

    #region GetAvailableRepositoriesAsync Tests

    [Fact]
    public async Task GetAvailableRepositoriesAsync_WithDefaultPath_ScansDefaultFolder()
    {
        // Arrange
        var directories = new[] { "C:/repos/repo1", "C:/repos/repo2" };
        
        _dirOpsMock.Setup(x => x.Exists("C:/repos")).Returns(true);
        _dirOpsMock.Setup(x => x.GetDirectories("C:/repos")).Returns(directories);
        
        // Setup .git folder exists for both repos
        _dirOpsMock.Setup(x => x.Exists("C:/repos/repo1/.git")).Returns(true);
        _dirOpsMock.Setup(x => x.Exists("C:/repos/repo2/.git")).Returns(true);
        
        // Mock git config for owner detection
        _fileOpsMock.Setup(x => x.Exists(It.IsAny<string>())).Returns(false);

        // Act
        var result = await _service.GetAvailableRepositoriesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Select(r => r.Name).Should().Contain(new[] { "repo1", "repo2" });
    }

    [Fact]
    public async Task GetAvailableRepositoriesAsync_WithCustomPath_ScansCustomFolder()
    {
        // Arrange
        var customPath = "D:/projects";
        var directories = new[] { "D:/projects/myproject" };
        
        _dirOpsMock.Setup(x => x.Exists(customPath)).Returns(true);
        _dirOpsMock.Setup(x => x.GetDirectories(customPath)).Returns(directories);
        _dirOpsMock.Setup(x => x.Exists("D:/projects/myproject/.git")).Returns(true);
        _fileOpsMock.Setup(x => x.Exists(It.IsAny<string>())).Returns(false);

        // Act
        var result = await _service.GetAvailableRepositoriesAsync(customPath);

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("myproject");
    }

    [Fact]
    public async Task GetAvailableRepositoriesAsync_NonExistentPath_ReturnsEmpty()
    {
        // Arrange
        _dirOpsMock.Setup(x => x.Exists("C:/nonexistent")).Returns(false);

        // Act
        var result = await _service.GetAvailableRepositoriesAsync("C:/nonexistent");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailableRepositoriesAsync_ExcludesNonGitFolders()
    {
        // Arrange
        var directories = new[] { "C:/repos/git-repo", "C:/repos/not-a-repo" };
        
        _dirOpsMock.Setup(x => x.Exists("C:/repos")).Returns(true);
        _dirOpsMock.Setup(x => x.GetDirectories("C:/repos")).Returns(directories);
        _dirOpsMock.Setup(x => x.Exists("C:/repos/git-repo/.git")).Returns(true);
        _dirOpsMock.Setup(x => x.Exists("C:/repos/not-a-repo/.git")).Returns(false);
        _fileOpsMock.Setup(x => x.Exists(It.IsAny<string>())).Returns(false);

        // Act
        var result = await _service.GetAvailableRepositoriesAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("git-repo");
    }

    [Fact]
    public async Task GetAvailableRepositoriesAsync_WithGitHubRemote_ExtractsOwner()
    {
        // Arrange
        var directories = new[] { "C:/repos/myrepo" };
        var gitConfig = @"
[remote ""origin""]
    url = https://github.com/testuser/myrepo.git
    fetch = +refs/heads/*:refs/remotes/origin/*
";
        
        _dirOpsMock.Setup(x => x.Exists("C:/repos")).Returns(true);
        _dirOpsMock.Setup(x => x.GetDirectories("C:/repos")).Returns(directories);
        _dirOpsMock.Setup(x => x.Exists("C:/repos/myrepo/.git")).Returns(true);
        _fileOpsMock.Setup(x => x.Exists("C:/repos/myrepo/.git/config")).Returns(true);
        _fileOpsMock.Setup(x => x.ReadAllText("C:/repos/myrepo/.git/config")).Returns(gitConfig);

        // Act
        var result = await _service.GetAvailableRepositoriesAsync();

        // Assert
        result.Should().HaveCount(1);
        var repo = result.First();
        repo.Owner.Should().Be("testuser");
        repo.FullName.Should().Be("testuser/myrepo");
    }

    [Fact]
    public async Task GetAvailableRepositoriesAsync_WithSshRemote_ExtractsOwner()
    {
        // Arrange
        var directories = new[] { "C:/repos/sshrepo" };
        var gitConfig = @"
[remote ""origin""]
    url = git@github.com:anotheruser/sshrepo.git
    fetch = +refs/heads/*:refs/remotes/origin/*
";
        
        _dirOpsMock.Setup(x => x.Exists("C:/repos")).Returns(true);
        _dirOpsMock.Setup(x => x.GetDirectories("C:/repos")).Returns(directories);
        _dirOpsMock.Setup(x => x.Exists("C:/repos/sshrepo/.git")).Returns(true);
        _fileOpsMock.Setup(x => x.Exists("C:/repos/sshrepo/.git/config")).Returns(true);
        _fileOpsMock.Setup(x => x.ReadAllText("C:/repos/sshrepo/.git/config")).Returns(gitConfig);

        // Act
        var result = await _service.GetAvailableRepositoriesAsync();

        // Assert
        var repo = result.First();
        repo.Owner.Should().Be("anotheruser");
    }

    [Fact]
    public async Task GetAvailableRepositoriesAsync_NoRemote_SetsLocalOwner()
    {
        // Arrange
        var directories = new[] { "C:/repos/localrepo" };
        
        _dirOpsMock.Setup(x => x.Exists("C:/repos")).Returns(true);
        _dirOpsMock.Setup(x => x.GetDirectories("C:/repos")).Returns(directories);
        _dirOpsMock.Setup(x => x.Exists("C:/repos/localrepo/.git")).Returns(true);
        _fileOpsMock.Setup(x => x.Exists("C:/repos/localrepo/.git/config")).Returns(false);

        // Act
        var result = await _service.GetAvailableRepositoriesAsync();

        // Assert
        var repo = result.First();
        repo.Owner.Should().Be("local");
    }

    #endregion

    #region GetRepositoryInfoAsync Tests

    [Fact]
    public async Task GetRepositoryInfoAsync_ExistingRepo_ReturnsInfo()
    {
        // Arrange
        var directories = new[] { "C:/repos/myrepo" };
        
        _dirOpsMock.Setup(x => x.Exists("C:/repos")).Returns(true);
        _dirOpsMock.Setup(x => x.GetDirectories("C:/repos")).Returns(directories);
        _dirOpsMock.Setup(x => x.Exists("C:/repos/myrepo/.git")).Returns(true);
        _fileOpsMock.Setup(x => x.Exists(It.IsAny<string>())).Returns(false);

        // Act
        var result = await _service.GetRepositoryInfoAsync("local", "myrepo");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("myrepo");
    }

    [Fact]
    public async Task GetRepositoryInfoAsync_NonExistentRepo_ReturnsNull()
    {
        // Arrange
        var directories = new[] { "C:/repos/otherrepo" };
        
        _dirOpsMock.Setup(x => x.Exists("C:/repos")).Returns(true);
        _dirOpsMock.Setup(x => x.GetDirectories("C:/repos")).Returns(directories);
        _dirOpsMock.Setup(x => x.Exists("C:/repos/otherrepo/.git")).Returns(true);
        _fileOpsMock.Setup(x => x.Exists(It.IsAny<string>())).Returns(false);

        // Act
        var result = await _service.GetRepositoryInfoAsync("local", "nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRepositoryInfoAsync_CaseInsensitive_MatchesRepo()
    {
        // Arrange
        var directories = new[] { "C:/repos/MyRepo" };
        
        _dirOpsMock.Setup(x => x.Exists("C:/repos")).Returns(true);
        _dirOpsMock.Setup(x => x.GetDirectories("C:/repos")).Returns(directories);
        _dirOpsMock.Setup(x => x.Exists("C:/repos/MyRepo/.git")).Returns(true);
        _fileOpsMock.Setup(x => x.Exists(It.IsAny<string>())).Returns(false);

        // Act
        var result = await _service.GetRepositoryInfoAsync("LOCAL", "MYREPO");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("MyRepo");
    }

    #endregion

    #region GetRepositoryPathAsync Tests

    [Fact]
    public async Task GetRepositoryPathAsync_ExistingRepo_ReturnsPath()
    {
        // Arrange
        var directories = new[] { "C:/repos/testrepo" };
        
        _dirOpsMock.Setup(x => x.Exists("C:/repos")).Returns(true);
        _dirOpsMock.Setup(x => x.GetDirectories("C:/repos")).Returns(directories);
        _dirOpsMock.Setup(x => x.Exists("C:/repos/testrepo/.git")).Returns(true);
        _fileOpsMock.Setup(x => x.Exists(It.IsAny<string>())).Returns(false);

        // Act
        var result = await _service.GetRepositoryPathAsync("local", "testrepo");

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("testrepo");
    }

    [Fact]
    public async Task GetRepositoryPathAsync_NonExistentRepo_ReturnsNull()
    {
        // Arrange
        _dirOpsMock.Setup(x => x.Exists("C:/repos")).Returns(true);
        _dirOpsMock.Setup(x => x.GetDirectories("C:/repos")).Returns(Array.Empty<string>());

        // Act
        var result = await _service.GetRepositoryPathAsync("local", "nonexistent");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetConfigurationStatusAsync Tests

    [Fact]
    public async Task GetConfigurationStatusAsync_FullyConfigured_ReturnsAllTrue()
    {
        // Arrange
        var repoPath = "C:/repos/configured-repo";
        
        _fileOpsMock.Setup(x => x.Exists("C:/repos/configured-repo/.github/copilot-instructions.md")).Returns(true);
        _fileOpsMock.Setup(x => x.Exists("C:/repos/configured-repo/.vscode/mcp.json")).Returns(true);
        _dirOpsMock.Setup(x => x.Exists("C:/repos/configured-repo/.makeapp")).Returns(true);
        _dirOpsMock.Setup(x => x.Exists("C:/repos/configured-repo/.makeapp/agents")).Returns(true);

        // Act
        var result = await _service.GetConfigurationStatusAsync(repoPath);

        // Assert
        result.HasCopilotInstructions.Should().BeTrue();
        result.HasMcpConfig.Should().BeTrue();
        result.HasMakeAppFolder.Should().BeTrue();
        result.HasAgentConfigs.Should().BeTrue();
    }

    [Fact]
    public async Task GetConfigurationStatusAsync_Unconfigured_ReturnsAllFalse()
    {
        // Arrange
        var repoPath = "C:/repos/unconfigured-repo";
        
        _fileOpsMock.Setup(x => x.Exists(It.IsAny<string>())).Returns(false);
        _dirOpsMock.Setup(x => x.Exists(It.IsAny<string>())).Returns(false);

        // Act
        var result = await _service.GetConfigurationStatusAsync(repoPath);

        // Assert
        result.HasCopilotInstructions.Should().BeFalse();
        result.HasMcpConfig.Should().BeFalse();
        result.HasMakeAppFolder.Should().BeFalse();
        result.HasAgentConfigs.Should().BeFalse();
    }

    [Fact]
    public async Task GetConfigurationStatusAsync_PartiallyConfigured_ReturnsMixedStatus()
    {
        // Arrange
        var repoPath = "C:/repos/partial-repo";
        
        _fileOpsMock.Setup(x => x.Exists("C:/repos/partial-repo/.github/copilot-instructions.md")).Returns(true);
        _fileOpsMock.Setup(x => x.Exists("C:/repos/partial-repo/.vscode/mcp.json")).Returns(false);
        _dirOpsMock.Setup(x => x.Exists("C:/repos/partial-repo/.makeapp")).Returns(true);
        _dirOpsMock.Setup(x => x.Exists("C:/repos/partial-repo/.makeapp/agents")).Returns(false);

        // Act
        var result = await _service.GetConfigurationStatusAsync(repoPath);

        // Assert
        result.HasCopilotInstructions.Should().BeTrue();
        result.HasMcpConfig.Should().BeFalse();
        result.HasMakeAppFolder.Should().BeTrue();
        result.HasAgentConfigs.Should().BeFalse();
    }

    [Fact]
    public async Task GetConfigurationStatusAsync_WithInstructions_SetsCopilotInstructionsPath()
    {
        // Arrange
        var repoPath = "C:/repos/with-instructions";
        
        _fileOpsMock.Setup(x => x.Exists("C:/repos/with-instructions/.github/copilot-instructions.md")).Returns(true);
        _fileOpsMock.Setup(x => x.Exists(It.Is<string>(s => !s.Contains("copilot-instructions")))).Returns(false);
        _dirOpsMock.Setup(x => x.Exists(It.IsAny<string>())).Returns(false);

        // Act
        var result = await _service.GetConfigurationStatusAsync(repoPath);

        // Assert
        result.HasCopilotInstructions.Should().BeTrue();
        result.CopilotInstructionsPath.Should().Contain("copilot-instructions.md");
    }

    #endregion

    #region ValidateRepositoryAsync Tests

    [Fact]
    public async Task ValidateRepositoryAsync_WithGitFolder_ReturnsTrue()
    {
        // Arrange
        var repoPath = "C:/repos/valid-repo";
        _dirOpsMock.Setup(x => x.Exists("C:/repos/valid-repo/.git")).Returns(true);

        // Act
        var result = await _service.ValidateRepositoryAsync(repoPath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateRepositoryAsync_WithoutGitFolder_ReturnsFalse()
    {
        // Arrange
        var repoPath = "C:/repos/not-a-repo";
        _dirOpsMock.Setup(x => x.Exists("C:/repos/not-a-repo/.git")).Returns(false);

        // Act
        var result = await _service.ValidateRepositoryAsync(repoPath);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ScanFolderAsync Tests

    [Fact]
    public async Task ScanFolderAsync_EmptyFolder_ReturnsEmpty()
    {
        // Arrange
        var folderPath = "C:/empty-folder";
        _dirOpsMock.Setup(x => x.Exists(folderPath)).Returns(true);
        _dirOpsMock.Setup(x => x.GetDirectories(folderPath)).Returns(Array.Empty<string>());

        // Act
        var result = await _service.ScanFolderAsync(folderPath);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ScanFolderAsync_NonExistentFolder_ReturnsEmpty()
    {
        // Arrange
        var folderPath = "C:/nonexistent";
        _dirOpsMock.Setup(x => x.Exists(folderPath)).Returns(false);

        // Act
        var result = await _service.ScanFolderAsync(folderPath);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ScanFolderAsync_MultipleRepos_ReturnsAll()
    {
        // Arrange
        var folderPath = "C:/projects";
        var directories = new[] { "C:/projects/repo1", "C:/projects/repo2", "C:/projects/repo3" };
        
        _dirOpsMock.Setup(x => x.Exists(folderPath)).Returns(true);
        _dirOpsMock.Setup(x => x.GetDirectories(folderPath)).Returns(directories);
        
        foreach (var dir in directories)
        {
            _dirOpsMock.Setup(x => x.Exists($"{dir}/.git")).Returns(true);
        }
        _fileOpsMock.Setup(x => x.Exists(It.IsAny<string>())).Returns(false);

        // Act
        var result = await _service.ScanFolderAsync(folderPath);

        // Assert
        result.Should().HaveCount(3);
    }

    #endregion
}
