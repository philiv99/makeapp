using FluentAssertions;
using Moq;
using MakeApp.Application.Services;
using MakeApp.Core.Entities;
using MakeApp.Core.Interfaces;
using Xunit;

namespace MakeApp.Application.Tests;

public class AgentConfigurationServiceTests
{
    private readonly Mock<IFileSystem> _fileSystemMock;
    private readonly Mock<IFileOperations> _fileOpsMock;
    private readonly Mock<IDirectoryOperations> _dirOpsMock;
    private readonly Mock<IPathOperations> _pathOpsMock;
    private readonly AgentConfigurationService _service;

    public AgentConfigurationServiceTests()
    {
        _fileSystemMock = new Mock<IFileSystem>();
        _fileOpsMock = new Mock<IFileOperations>();
        _dirOpsMock = new Mock<IDirectoryOperations>();
        _pathOpsMock = new Mock<IPathOperations>();

        _fileSystemMock.Setup(x => x.File).Returns(_fileOpsMock.Object);
        _fileSystemMock.Setup(x => x.Directory).Returns(_dirOpsMock.Object);
        _fileSystemMock.Setup(x => x.Path).Returns(_pathOpsMock.Object);
        
        _pathOpsMock.Setup(x => x.Combine(It.IsAny<string[]>()))
            .Returns((string[] paths) => string.Join("/", paths));

        _service = new AgentConfigurationService(_fileSystemMock.Object);
    }

    [Fact]
    public void GetDefaultConfiguration_ReturnsValidConfiguration()
    {
        // Act
        var config = _service.GetDefaultConfiguration();

        // Assert
        config.Should().NotBeNull();
        config.Orchestrator.Should().NotBeNull();
        config.Coder.Should().NotBeNull();
        config.Tester.Should().NotBeNull();
        config.Reviewer.Should().NotBeNull();
    }

    [Fact]
    public void GetDefaultConfiguration_ForDotNet_SetsCorrectFrameworks()
    {
        // Act
        var config = _service.GetDefaultConfiguration("dotnet");

        // Assert
        config.Tester.TestingRules.Frameworks.TestFramework.Should().Be("xunit");
        config.Tester.TestingRules.Frameworks.Assertions.Should().Be("FluentAssertions");
        config.Tester.TestingRules.Frameworks.Mocking.Should().Be("Moq");
        config.Coder.Constraints.Should().Contain("Use async/await patterns");
    }

    [Fact]
    public void GetDefaultConfiguration_ForPython_SetsCorrectFrameworks()
    {
        // Act
        var config = _service.GetDefaultConfiguration("python");

        // Assert
        config.Tester.TestingRules.Frameworks.TestFramework.Should().Be("pytest");
        config.Coder.Constraints.Should().Contain("Follow PEP 8 style guide");
    }

    [Fact]
    public void GetDefaultConfiguration_ForTypeScript_SetsCorrectFrameworks()
    {
        // Act
        var config = _service.GetDefaultConfiguration("typescript");

        // Assert
        config.Tester.TestingRules.Frameworks.TestFramework.Should().Be("vitest");
        config.Coder.Constraints.Should().Contain("Use TypeScript strict mode");
    }

    [Fact]
    public void ValidateConfiguration_WithValidConfig_ReturnsIsValid()
    {
        // Arrange
        var config = _service.GetDefaultConfiguration();

        // Act
        var result = _service.ValidateConfiguration(config);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateConfiguration_WithInvalidCoverage_ReturnsError()
    {
        // Arrange
        var config = _service.GetDefaultConfiguration();
        config.Tester.TestingRules.MinimumCoverage = 150; // Invalid: > 100

        // Act
        var result = _service.ValidateConfiguration(config);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainMatch("*coverage*");
    }

    [Fact]
    public void GenerateCopilotInstructions_ReturnsMarkdownContent()
    {
        // Arrange
        var config = _service.GetDefaultConfiguration("dotnet");

        // Act
        var instructions = _service.GenerateCopilotInstructions(config);

        // Assert
        instructions.Should().NotBeNullOrEmpty();
        instructions.Should().Contain("# GitHub Copilot Instructions");
        instructions.Should().Contain("Code Style Guidelines");
        instructions.Should().Contain("Testing Guidelines");
    }

    [Fact]
    public async Task LoadConfigurationAsync_WhenFileDoesNotExist_ReturnsDefault()
    {
        // Arrange
        _fileOpsMock.Setup(x => x.Exists(It.IsAny<string>())).Returns(false);

        // Act
        var config = await _service.LoadConfigurationAsync("C:/repos/test");

        // Assert
        config.Should().NotBeNull();
        config.Orchestrator.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveConfigurationAsync_CreatesDirectoryAndWritesFile()
    {
        // Arrange
        var config = _service.GetDefaultConfiguration();
        string? writtenContent = null;
        string? writtenPath = null;

        _dirOpsMock.Setup(x => x.CreateDirectory(It.IsAny<string>()))
            .Returns(new DirectoryInfo("C:/test"));
        _fileOpsMock.Setup(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((path, content, _) => 
            {
                writtenPath = path;
                writtenContent = content;
            })
            .Returns(Task.CompletedTask);

        // Act
        await _service.SaveConfigurationAsync(config, "C:/repos/test");

        // Assert
        writtenPath.Should().Contain("config.json");
        writtenContent.Should().NotBeNullOrEmpty();
        writtenContent.Should().Contain("Orchestrator");
    }

    [Fact]
    public async Task InitializeAsync_SavesDefaultConfiguration()
    {
        // Arrange
        _dirOpsMock.Setup(x => x.CreateDirectory(It.IsAny<string>()))
            .Returns(new DirectoryInfo("C:/test"));
        _fileOpsMock.Setup(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var config = await _service.InitializeAsync("C:/repos/test");

        // Assert
        config.Should().NotBeNull();
        _fileOpsMock.Verify(x => x.WriteAllTextAsync(
            It.Is<string>(s => s.Contains("config.json")),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
