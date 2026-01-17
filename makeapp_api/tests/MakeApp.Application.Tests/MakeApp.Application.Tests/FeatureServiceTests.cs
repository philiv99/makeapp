using FluentAssertions;
using Moq;
using MakeApp.Application.DTOs;
using MakeApp.Application.Services;
using MakeApp.Core.Entities;
using MakeApp.Core.Enums;
using MakeApp.Core.Interfaces;
using Xunit;

namespace MakeApp.Application.Tests;

/// <summary>
/// Unit tests for FeatureService
/// </summary>
public class FeatureServiceTests
{
    private readonly Mock<IOrchestrationService> _orchestrationServiceMock;
    private readonly Mock<IBranchService> _branchServiceMock;
    private readonly Mock<IRepositoryService> _repositoryServiceMock;
    private readonly FeatureService _service;

    public FeatureServiceTests()
    {
        _orchestrationServiceMock = new Mock<IOrchestrationService>();
        _branchServiceMock = new Mock<IBranchService>();
        _repositoryServiceMock = new Mock<IRepositoryService>();

        _service = new FeatureService(
            _orchestrationServiceMock.Object,
            _branchServiceMock.Object,
            _repositoryServiceMock.Object);
    }

    #region CreateFeatureAsync Tests

    [Fact]
    public async Task CreateFeatureAsync_WithValidRequest_ReturnsFeatureResponse()
    {
        // Arrange
        var request = new CreateFeatureRequestDto
        {
            RepositoryOwner = "testuser",
            RepositoryName = "test-repo",
            Title = "Add user authentication",
            Description = "Implement JWT-based authentication",
            AcceptanceCriteria = new List<string> { "Users can log in", "Users can log out" },
            Priority = FeaturePriority.High,
            BaseBranch = "main"
        };

        _repositoryServiceMock.Setup(x => x.GetRepositoryPathAsync("testuser", "test-repo"))
            .ReturnsAsync("/repos/test-repo");
        
        _branchServiceMock.Setup(x => x.FormatFeatureBranchName(It.IsAny<string>()))
            .Returns("feature/add-user-authentication");
        
        _branchServiceMock.Setup(x => x.CreateFeatureBranchAsync(
            "/repos/test-repo", 
            "feature/add-user-authentication", 
            "main"))
            .ReturnsAsync(new BranchInfo { Name = "feature/add-user-authentication" });
        
        _branchServiceMock.Setup(x => x.SwitchToBranchAsync(
            "/repos/test-repo", 
            "feature/add-user-authentication"))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CreateFeatureAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Add user authentication");
        result.Status.Should().Be(FeatureStatus.Ready);
        result.FeatureBranch.Should().Be("feature/add-user-authentication");
        
        _branchServiceMock.Verify(x => x.CreateFeatureBranchAsync(
            "/repos/test-repo",
            "feature/add-user-authentication",
            "main"), Times.Once);
    }

    [Fact]
    public async Task CreateFeatureAsync_WithNonExistentRepository_ThrowsException()
    {
        // Arrange
        var request = new CreateFeatureRequestDto
        {
            RepositoryOwner = "unknown",
            RepositoryName = "nonexistent",
            Title = "Test Feature"
        };

        _repositoryServiceMock.Setup(x => x.GetRepositoryPathAsync("unknown", "nonexistent"))
            .ReturnsAsync((string?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateFeatureAsync(request));
    }

    [Fact]
    public async Task CreateFeatureAsync_WithDefaultBaseBranch_UsesMain()
    {
        // Arrange
        var request = new CreateFeatureRequestDto
        {
            RepositoryOwner = "testuser",
            RepositoryName = "test-repo",
            Title = "New Feature",
            BaseBranch = null // Should default to "main"
        };

        _repositoryServiceMock.Setup(x => x.GetRepositoryPathAsync("testuser", "test-repo"))
            .ReturnsAsync("/repos/test-repo");
        
        _branchServiceMock.Setup(x => x.FormatFeatureBranchName(It.IsAny<string>()))
            .Returns("feature/new-feature");
        
        _branchServiceMock.Setup(x => x.CreateFeatureBranchAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            "main"))
            .ReturnsAsync(new BranchInfo { Name = "feature/new-feature" });
        
        _branchServiceMock.Setup(x => x.SwitchToBranchAsync(
            It.IsAny<string>(), 
            It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CreateFeatureAsync(request);

        // Assert
        _branchServiceMock.Verify(x => x.CreateFeatureBranchAsync(
            "/repos/test-repo",
            "feature/new-feature",
            "main"), Times.Once);
    }

    [Fact]
    public async Task CreateFeatureAsync_SetsCorrectPriority()
    {
        // Arrange
        var request = new CreateFeatureRequestDto
        {
            RepositoryOwner = "testuser",
            RepositoryName = "test-repo",
            Title = "Critical Fix",
            Priority = FeaturePriority.Critical
        };

        _repositoryServiceMock.Setup(x => x.GetRepositoryPathAsync("testuser", "test-repo"))
            .ReturnsAsync("/repos/test-repo");
        
        _branchServiceMock.Setup(x => x.FormatFeatureBranchName(It.IsAny<string>()))
            .Returns("feature/critical-fix");
        
        _branchServiceMock.Setup(x => x.CreateFeatureBranchAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<string>()))
            .ReturnsAsync(new BranchInfo { Name = "feature/critical-fix" });
        
        _branchServiceMock.Setup(x => x.SwitchToBranchAsync(
            It.IsAny<string>(), 
            It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CreateFeatureAsync(request);

        // Assert
        result.Priority.Should().Be(FeaturePriority.Critical);
    }

    #endregion

    #region StartFeatureAsync Tests

    [Fact]
    public async Task StartFeatureAsync_WithValidId_ReturnsFeatureInProgress()
    {
        // Act
        var result = await _service.StartFeatureAsync("feat-123");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("feat-123");
        result.Status.Should().Be(FeatureStatus.InProgress);
    }

    #endregion

    #region GetFeatureStatusAsync Tests

    [Fact]
    public async Task GetFeatureStatusAsync_ReturnsStatusWithProgress()
    {
        // Act
        var result = await _service.GetFeatureStatusAsync("feat-123");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("feat-123");
        result.Progress.Should().NotBeNull();
        result.Progress!.CurrentPhase.Should().BeGreaterThanOrEqualTo(1);
        result.Progress.TotalPhases.Should().BeGreaterThan(0);
    }

    #endregion

    #region CancelFeatureAsync Tests

    [Fact]
    public async Task CancelFeatureAsync_WithValidId_ReturnsTrue()
    {
        // Act
        var result = await _service.CancelFeatureAsync("feat-123");

        // Assert
        result.Should().BeTrue();
    }

    #endregion
}
