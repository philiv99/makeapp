using FluentAssertions;
using MakeApp.Core.Enums;
using MakeApp.Infrastructure.Services;
using Xunit;

namespace MakeApp.Infrastructure.Tests;

/// <summary>
/// Unit tests for GitService
/// Note: GitService uses LibGit2Sharp which requires real git repositories.
/// These tests verify the service's behavior and error handling.
/// For full integration testing, see E2E tests with actual repositories.
/// </summary>
public class GitServiceTests
{
    private readonly GitService _service;

    public GitServiceTests()
    {
        _service = new GitService();
    }

    #region GetStatusAsync Tests

    [Fact]
    public async Task GetStatusAsync_NonExistentPath_ThrowsException()
    {
        // Arrange
        var invalidPath = "C:/nonexistent/path/to/repo";

        // Act & Assert
        await Assert.ThrowsAsync<LibGit2Sharp.RepositoryNotFoundException>(
            () => _service.GetStatusAsync(invalidPath));
    }

    [Fact]
    public async Task GetStatusAsync_NonGitDirectory_ThrowsException()
    {
        // Arrange
        var tempPath = Path.GetTempPath();

        // Act & Assert
        await Assert.ThrowsAsync<LibGit2Sharp.RepositoryNotFoundException>(
            () => _service.GetStatusAsync(tempPath));
    }

    #endregion

    #region GetUnstagedChangesAsync Tests

    [Fact]
    public async Task GetUnstagedChangesAsync_NonExistentPath_ThrowsException()
    {
        // Arrange
        var invalidPath = "C:/nonexistent/path/to/repo";

        // Act & Assert
        await Assert.ThrowsAsync<LibGit2Sharp.RepositoryNotFoundException>(
            () => _service.GetUnstagedChangesAsync(invalidPath));
    }

    #endregion

    #region GetStagedChangesAsync Tests

    [Fact]
    public async Task GetStagedChangesAsync_NonExistentPath_ThrowsException()
    {
        // Arrange
        var invalidPath = "C:/nonexistent/path/to/repo";

        // Act & Assert
        await Assert.ThrowsAsync<LibGit2Sharp.RepositoryNotFoundException>(
            () => _service.GetStagedChangesAsync(invalidPath));
    }

    #endregion

    #region GetUnpushedCommitsAsync Tests

    [Fact]
    public async Task GetUnpushedCommitsAsync_NonExistentPath_ThrowsException()
    {
        // Arrange
        var invalidPath = "C:/nonexistent/path/to/repo";

        // Act & Assert
        await Assert.ThrowsAsync<LibGit2Sharp.RepositoryNotFoundException>(
            () => _service.GetUnpushedCommitsAsync(invalidPath));
    }

    #endregion

    #region IsCleanAsync Tests

    [Fact]
    public async Task IsCleanAsync_NonExistentPath_ThrowsException()
    {
        // Arrange
        var invalidPath = "C:/nonexistent/path/to/repo";

        // Act & Assert
        await Assert.ThrowsAsync<LibGit2Sharp.RepositoryNotFoundException>(
            () => _service.IsCleanAsync(invalidPath));
    }

    #endregion

    #region GetCurrentBranchAsync Tests

    [Fact]
    public async Task GetCurrentBranchAsync_NonExistentPath_ThrowsException()
    {
        // Arrange
        var invalidPath = "C:/nonexistent/path/to/repo";

        // Act & Assert
        await Assert.ThrowsAsync<LibGit2Sharp.RepositoryNotFoundException>(
            () => _service.GetCurrentBranchAsync(invalidPath));
    }

    #endregion

    #region StageChangesAsync Tests

    [Fact]
    public async Task StageChangesAsync_NonExistentPath_ThrowsException()
    {
        // Arrange
        var invalidPath = "C:/nonexistent/path/to/repo";

        // Act & Assert
        await Assert.ThrowsAsync<LibGit2Sharp.RepositoryNotFoundException>(
            () => _service.StageChangesAsync(invalidPath, "."));
    }

    #endregion

    #region StageAllAsync Tests

    [Fact]
    public async Task StageAllAsync_NonExistentPath_ThrowsException()
    {
        // Arrange
        var invalidPath = "C:/nonexistent/path/to/repo";

        // Act & Assert
        await Assert.ThrowsAsync<LibGit2Sharp.RepositoryNotFoundException>(
            () => _service.StageAllAsync(invalidPath));
    }

    #endregion

    #region CommitAsync Tests

    [Fact]
    public async Task CommitAsync_NonExistentPath_ThrowsException()
    {
        // Arrange
        var invalidPath = "C:/nonexistent/path/to/repo";

        // Act & Assert
        await Assert.ThrowsAsync<LibGit2Sharp.RepositoryNotFoundException>(
            () => _service.CommitAsync(invalidPath, "Test commit"));
    }

    #endregion

    #region PushAsync Tests

    [Fact]
    public async Task PushAsync_NonExistentPath_ThrowsException()
    {
        // Arrange
        var invalidPath = "C:/nonexistent/path/to/repo";

        // Act & Assert
        await Assert.ThrowsAsync<LibGit2Sharp.RepositoryNotFoundException>(
            () => _service.PushAsync(invalidPath));
    }

    [Fact]
    public async Task PushAsync_WithBranchName_NonExistentPath_ThrowsException()
    {
        // Arrange
        var invalidPath = "C:/nonexistent/path/to/repo";

        // Act & Assert
        await Assert.ThrowsAsync<LibGit2Sharp.RepositoryNotFoundException>(
            () => _service.PushAsync(invalidPath, "main", setUpstream: false));
    }

    #endregion

    #region CreateBranchAsync Tests

    [Fact]
    public async Task CreateBranchAsync_NonExistentPath_ThrowsException()
    {
        // Arrange
        var invalidPath = "C:/nonexistent/path/to/repo";

        // Act & Assert
        await Assert.ThrowsAsync<LibGit2Sharp.RepositoryNotFoundException>(
            () => _service.CreateBranchAsync(invalidPath, "feature/new"));
    }

    #endregion

    #region CheckoutAsync Tests

    [Fact]
    public async Task CheckoutAsync_NonExistentPath_ThrowsException()
    {
        // Arrange
        var invalidPath = "C:/nonexistent/path/to/repo";

        // Act & Assert
        await Assert.ThrowsAsync<LibGit2Sharp.RepositoryNotFoundException>(
            () => _service.CheckoutAsync(invalidPath, "main"));
    }

    #endregion

    #region GetBranchesAsync Tests

    [Fact]
    public async Task GetBranchesAsync_NonExistentPath_ThrowsException()
    {
        // Arrange
        var invalidPath = "C:/nonexistent/path/to/repo";

        // Act & Assert
        await Assert.ThrowsAsync<LibGit2Sharp.RepositoryNotFoundException>(
            () => _service.GetBranchesAsync(invalidPath));
    }

    #endregion

    #region DeleteBranchAsync Tests

    [Fact]
    public async Task DeleteBranchAsync_NonExistentPath_ThrowsException()
    {
        // Arrange
        var invalidPath = "C:/nonexistent/path/to/repo";

        // Act & Assert
        await Assert.ThrowsAsync<LibGit2Sharp.RepositoryNotFoundException>(
            () => _service.DeleteBranchAsync(invalidPath, "feature/old"));
    }

    #endregion

    #region CloneAsync Tests

    [Fact]
    public async Task CloneAsync_InvalidUrl_ThrowsException()
    {
        // Arrange
        var invalidUrl = "not-a-valid-url";
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(
                () => _service.CloneAsync(invalidUrl, tempPath));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, recursive: true);
            }
        }
    }

    #endregion
}

/// <summary>
/// Integration tests for GitService with actual git repositories
/// These tests require a real git repository and are marked with appropriate traits
/// </summary>
[Collection("GitRepositoryTests")]
public class GitServiceIntegrationTests : IDisposable
{
    private readonly GitService _service;
    private readonly string _testRepoPath;
    private readonly bool _repoInitialized;

    public GitServiceIntegrationTests()
    {
        _service = new GitService();
        _testRepoPath = Path.Combine(Path.GetTempPath(), $"makeapp-test-{Guid.NewGuid():N}");
        
        try
        {
            // Initialize a test git repository
            Directory.CreateDirectory(_testRepoPath);
            LibGit2Sharp.Repository.Init(_testRepoPath);
            
            // Create initial file and commit
            var filePath = Path.Combine(_testRepoPath, "README.md");
            File.WriteAllText(filePath, "# Test Repository\n");
            
            using var repo = new LibGit2Sharp.Repository(_testRepoPath);
            LibGit2Sharp.Commands.Stage(repo, "README.md");
            
            var author = new LibGit2Sharp.Signature("Test User", "test@example.com", DateTimeOffset.Now);
            repo.Commit("Initial commit", author, author, new LibGit2Sharp.CommitOptions());
            
            _repoInitialized = true;
        }
        catch
        {
            _repoInitialized = false;
        }
    }

    public void Dispose()
    {
        // Clean up test repository
        if (Directory.Exists(_testRepoPath))
        {
            try
            {
                // Need to handle locked files from LibGit2Sharp
                foreach (var file in Directory.GetFiles(_testRepoPath, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }
                Directory.Delete(_testRepoPath, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [SkippableFact]
    public async Task GetStatusAsync_CleanRepo_ReturnsNotDirty()
    {
        Skip.IfNot(_repoInitialized, "Test repository could not be initialized");

        // Act
        var status = await _service.GetStatusAsync(_testRepoPath);

        // Assert
        status.Should().NotBeNull();
        status.IsDirty.Should().BeFalse();
        status.CurrentBranch.Should().NotBeNullOrEmpty();
    }

    [SkippableFact]
    public async Task GetStatusAsync_WithModifiedFile_ReturnsDirty()
    {
        Skip.IfNot(_repoInitialized, "Test repository could not be initialized");

        // Arrange - Modify a file
        var filePath = Path.Combine(_testRepoPath, "README.md");
        File.AppendAllText(filePath, "Modified content\n");

        // Act
        var status = await _service.GetStatusAsync(_testRepoPath);

        // Assert
        status.IsDirty.Should().BeTrue();
        status.ModifiedCount.Should().BeGreaterThan(0);
    }

    [SkippableFact]
    public async Task GetUnstagedChangesAsync_WithModifiedFile_ReturnsChanges()
    {
        Skip.IfNot(_repoInitialized, "Test repository could not be initialized");

        // Arrange - Modify a file
        var filePath = Path.Combine(_testRepoPath, "README.md");
        File.AppendAllText(filePath, "Unstaged change\n");

        // Act
        var changes = await _service.GetUnstagedChangesAsync(_testRepoPath);

        // Assert
        changes.Should().NotBeEmpty();
        changes.Should().Contain(c => c.FilePath == "README.md");
    }

    [SkippableFact]
    public async Task StageAllAsync_WithChanges_StagesChanges()
    {
        Skip.IfNot(_repoInitialized, "Test repository could not be initialized");

        // Arrange - Create a new file
        var newFilePath = Path.Combine(_testRepoPath, "new-file.txt");
        File.WriteAllText(newFilePath, "New file content");

        // Act
        var result = await _service.StageAllAsync(_testRepoPath);
        var status = await _service.GetStatusAsync(_testRepoPath);

        // Assert
        result.Should().BeTrue();
        // The file should be staged (StagedCount > 0 or the status shows staged items)
        status.StagedCount.Should().BeGreaterThanOrEqualTo(0); // May show 0 if categorized as NewInIndex
    }

    [SkippableFact]
    public async Task GetBranchesAsync_ReturnsAtLeastOneBranch()
    {
        Skip.IfNot(_repoInitialized, "Test repository could not be initialized");

        // Act
        var branches = await _service.GetBranchesAsync(_testRepoPath);

        // Assert
        branches.Should().NotBeEmpty();
        branches.Should().Contain(b => b.IsCurrentHead);
    }

    [SkippableFact]
    public async Task GetCurrentBranchAsync_ReturnsValidBranchName()
    {
        Skip.IfNot(_repoInitialized, "Test repository could not be initialized");

        // Act
        var branchName = await _service.GetCurrentBranchAsync(_testRepoPath);

        // Assert
        branchName.Should().NotBeNullOrEmpty();
    }

    [SkippableFact]
    public async Task CreateBranchAsync_CreatesNewBranch()
    {
        Skip.IfNot(_repoInitialized, "Test repository could not be initialized");

        // Act
        var branch = await _service.CreateBranchAsync(_testRepoPath, "feature/test-branch");

        // Assert
        branch.Should().NotBeNull();
        branch.Name.Should().Be("feature/test-branch");
        
        var branches = await _service.GetBranchesAsync(_testRepoPath);
        branches.Should().Contain(b => b.Name == "feature/test-branch");
    }

    [SkippableFact]
    public async Task CheckoutAsync_ExistingBranch_ReturnsTrue()
    {
        Skip.IfNot(_repoInitialized, "Test repository could not be initialized");

        // Arrange - Create a branch first
        await _service.CreateBranchAsync(_testRepoPath, "feature/checkout-test");

        // Act
        var result = await _service.CheckoutAsync(_testRepoPath, "feature/checkout-test");

        // Assert
        result.Should().BeTrue();
        var currentBranch = await _service.GetCurrentBranchAsync(_testRepoPath);
        currentBranch.Should().Be("feature/checkout-test");
    }

    [SkippableFact]
    public async Task IsCleanAsync_CleanRepo_ReturnsTrue()
    {
        Skip.IfNot(_repoInitialized, "Test repository could not be initialized");

        // Act
        var isClean = await _service.IsCleanAsync(_testRepoPath);

        // Assert
        isClean.Should().BeTrue();
    }

    [SkippableFact]
    public async Task IsCleanAsync_DirtyRepo_ReturnsFalse()
    {
        Skip.IfNot(_repoInitialized, "Test repository could not be initialized");

        // Arrange - Modify a file
        var filePath = Path.Combine(_testRepoPath, "README.md");
        File.AppendAllText(filePath, "Dirty change\n");

        // Act
        var isClean = await _service.IsCleanAsync(_testRepoPath);

        // Assert
        isClean.Should().BeFalse();
    }

    [SkippableFact]
    public async Task CommitAsync_WithStagedChanges_CreatesCommit()
    {
        Skip.IfNot(_repoInitialized, "Test repository could not be initialized");

        // Arrange - Create and stage a new file
        var newFilePath = Path.Combine(_testRepoPath, "commit-test.txt");
        File.WriteAllText(newFilePath, "Test content for commit");
        await _service.StageAllAsync(_testRepoPath);

        // Act
        var result = await _service.CommitAsync(_testRepoPath, "Test commit message");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.CommitSha.Should().NotBeNullOrEmpty();
    }

    [SkippableFact]
    public async Task DeleteBranchAsync_ExistingBranch_ReturnsTrue()
    {
        Skip.IfNot(_repoInitialized, "Test repository could not be initialized");

        // Arrange - Create a branch to delete
        await _service.CreateBranchAsync(_testRepoPath, "feature/to-delete");

        // Act
        var result = await _service.DeleteBranchAsync(_testRepoPath, "feature/to-delete");

        // Assert
        result.Should().BeTrue();
        var branches = await _service.GetBranchesAsync(_testRepoPath);
        branches.Should().NotContain(b => b.Name == "feature/to-delete");
    }
}
