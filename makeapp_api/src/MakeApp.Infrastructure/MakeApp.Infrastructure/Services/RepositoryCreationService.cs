using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MakeApp.Core.Configuration;
using MakeApp.Core.Entities;
using MakeApp.Core.Interfaces;

namespace MakeApp.Infrastructure.Services;

/// <summary>
/// Service for creating new app repositories with MakeApp structure
/// </summary>
public class RepositoryCreationService : IRepositoryCreationService
{
    private readonly IGitHubService _gitHubService;
    private readonly IGitService _gitService;
    private readonly IFileSystem _fileSystem;
    private readonly IAgentConfigurationService _agentConfigService;
    private readonly ILogger<RepositoryCreationService> _logger;
    private readonly MakeAppOptions _options;

    public RepositoryCreationService(
        IGitHubService gitHubService,
        IGitService gitService,
        IFileSystem fileSystem,
        IAgentConfigurationService agentConfigService,
        IOptions<MakeAppOptions> options,
        ILogger<RepositoryCreationService> logger)
    {
        _gitHubService = gitHubService;
        _gitService = gitService;
        _fileSystem = fileSystem;
        _agentConfigService = agentConfigService;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<CreateAppResult> CreateAppAsync(CreateAppRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new app: {AppName}", request.Name);

        var result = new CreateAppResult
        {
            AppId = Guid.NewGuid().ToString("N"),
            AppName = request.Name
        };

        try
        {
            // Step 1: Create GitHub repository
            var repoOptions = new CreateRepoOptions
            {
                Name = request.Name,
                Owner = request.Owner ?? await GetCurrentUserAsync(),
                Description = request.Description,
                Private = request.IsPrivate,
                AutoInit = true,
                GitignoreTemplate = GetGitignoreTemplate(request.Requirements?.ProjectType)
            };

            _logger.LogDebug("Creating GitHub repository: {Owner}/{Name}", repoOptions.Owner, repoOptions.Name);
            
            var repoInfo = await _gitHubService.CreateRepositoryAsync(repoOptions);
            result.RepositoryUrl = repoInfo.HtmlUrl;
            result.Owner = repoOptions.Owner;

            // Step 2: Clone repository locally
            var localPath = GetLocalPath(request.Name, request.LocalBasePath);
            _logger.LogDebug("Cloning repository to: {LocalPath}", localPath);

            await _gitService.CloneAsync(repoInfo.CloneUrl, localPath, new CloneOptions());
            result.LocalPath = localPath;

            // Step 3: Initialize MakeApp folder structure
            _logger.LogDebug("Initializing MakeApp folders");
            await InitializeMakeAppFoldersAsync(localPath, cancellationToken);

            // Step 4: Generate initial files
            _logger.LogDebug("Generating initial files");
            var generatedFiles = await GenerateInitialFilesAsync(localPath, request.Requirements, cancellationToken);
            result.GeneratedFiles = generatedFiles;

            // Step 5: Commit and push initial structure
            await CommitAndPushInitialStructureAsync(localPath);

            result.Success = true;
            result.CreatedAt = DateTime.UtcNow;

            _logger.LogInformation("Successfully created app: {AppName} at {LocalPath}", request.Name, localPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create app: {AppName}", request.Name);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task InitializeMakeAppFoldersAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating MakeApp folder structure at: {Path}", repositoryPath);

        // Create .makeapp directory structure
        var makeAppPath = _fileSystem.Path.Combine(repositoryPath, ".makeapp");
        var folders = new[]
        {
            makeAppPath,
            _fileSystem.Path.Combine(makeAppPath, "plans"),
            _fileSystem.Path.Combine(makeAppPath, "features"),
            _fileSystem.Path.Combine(makeAppPath, "memory"),
            _fileSystem.Path.Combine(makeAppPath, "agents"),
            _fileSystem.Path.Combine(makeAppPath, "templates"),
            _fileSystem.Path.Combine(makeAppPath, "logs")
        };

        foreach (var folder in folders)
        {
            if (!_fileSystem.Directory.Exists(folder))
            {
                _fileSystem.Directory.CreateDirectory(folder);
                _logger.LogDebug("Created directory: {Folder}", folder);
            }
        }

        // Create .github directory if it doesn't exist
        var githubPath = _fileSystem.Path.Combine(repositoryPath, ".github");
        if (!_fileSystem.Directory.Exists(githubPath))
        {
            _fileSystem.Directory.CreateDirectory(githubPath);
        }

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GenerateInitialFilesAsync(
        string repositoryPath, 
        AppRequirements? requirements, 
        CancellationToken cancellationToken = default)
    {
        var generatedFiles = new List<string>();

        // Generate copilot-instructions.md
        var copilotInstructionsPath = _fileSystem.Path.Combine(repositoryPath, ".github", "copilot-instructions.md");
        var agentConfig = _agentConfigService.GetDefaultConfiguration(requirements?.ProjectType ?? "generic");
        
        var copilotInstructions = _agentConfigService.GenerateCopilotInstructions(agentConfig);
        await _fileSystem.File.WriteAllTextAsync(copilotInstructionsPath, copilotInstructions, cancellationToken);
        generatedFiles.Add(copilotInstructionsPath);
        _logger.LogDebug("Generated: {File}", copilotInstructionsPath);

        // Save agent configuration
        await _agentConfigService.SaveConfigurationAsync(agentConfig, repositoryPath, cancellationToken);
        generatedFiles.Add(_fileSystem.Path.Combine(repositoryPath, ".makeapp", "agents", "config.json"));

        // Generate .makeapp/config.json
        var configPath = _fileSystem.Path.Combine(repositoryPath, ".makeapp", "config.json");
        var configContent = GenerateMakeAppConfig(requirements);
        await _fileSystem.File.WriteAllTextAsync(configPath, configContent, cancellationToken);
        generatedFiles.Add(configPath);
        _logger.LogDebug("Generated: {File}", configPath);

        // Generate initial plan.md (empty template)
        var planPath = _fileSystem.Path.Combine(repositoryPath, ".makeapp", "plans", "initial-plan.md");
        var planContent = GenerateInitialPlanTemplate(requirements);
        await _fileSystem.File.WriteAllTextAsync(planPath, planContent, cancellationToken);
        generatedFiles.Add(planPath);
        _logger.LogDebug("Generated: {File}", planPath);

        // Generate README.md if it doesn't exist or is minimal
        var readmePath = _fileSystem.Path.Combine(repositoryPath, "README.md");
        if (!_fileSystem.File.Exists(readmePath) || await IsMinimalReadmeAsync(readmePath, cancellationToken))
        {
            var readmeContent = GenerateReadme(requirements);
            await _fileSystem.File.WriteAllTextAsync(readmePath, readmeContent, cancellationToken);
            generatedFiles.Add(readmePath);
            _logger.LogDebug("Generated: {File}", readmePath);
        }

        return generatedFiles;
    }

    private async Task<string> GetCurrentUserAsync()
    {
        var user = await _gitHubService.GetCurrentUserAsync();
        return user.Login;
    }

    private string GetLocalPath(string appName, string? basePath)
    {
        var baseDir = basePath ?? _options.Folders?.Sandbox ?? 
            _fileSystem.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "repos");
        
        return _fileSystem.Path.Combine(baseDir, appName);
    }

    private static string? GetGitignoreTemplate(string? projectType)
    {
        return projectType?.ToLowerInvariant() switch
        {
            "dotnet" or "csharp" => "VisualStudio",
            "python" => "Python",
            "node" or "nodejs" or "javascript" or "typescript" => "Node",
            "go" or "golang" => "Go",
            "rust" => "Rust",
            "java" => "Java",
            _ => null
        };
    }

    private string GenerateMakeAppConfig(AppRequirements? requirements)
    {
        var config = new
        {
            version = "1.0.0",
            projectType = requirements?.ProjectType ?? "generic",
            name = requirements?.Name ?? "New Project",
            description = requirements?.Description ?? "",
            created = DateTime.UtcNow.ToString("o"),
            features = Array.Empty<object>(),
            settings = new
            {
                autoCommit = true,
                autoPush = true,
                planBeforeCode = true
            }
        };

        return System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
    }

    private static string GenerateInitialPlanTemplate(AppRequirements? requirements)
    {
        var projectName = requirements?.Name ?? "New Project";
        var description = requirements?.Description ?? "Project description goes here.";

        return $"""
            # {projectName} - Implementation Plan

            ## Overview
            {description}

            ## Requirements
            - [ ] Add requirements here

            ## Phases

            ### Phase 1: Foundation
            - [ ] Set up project structure
            - [ ] Configure development environment
            - [ ] Add initial dependencies

            ### Phase 2: Core Features
            - [ ] Implement core functionality

            ### Phase 3: Testing & Documentation
            - [ ] Add unit tests
            - [ ] Write documentation

            ## Notes
            This plan was auto-generated by MakeApp. Update it as you develop your application.
            """;
    }

    private static string GenerateReadme(AppRequirements? requirements)
    {
        var projectName = requirements?.Name ?? "New Project";
        var description = requirements?.Description ?? "Project description goes here.";
        var projectType = requirements?.ProjectType ?? "generic";

        return $"""
            # {projectName}

            {description}

            ## Getting Started

            ### Prerequisites
            - Add prerequisites here based on project type: {projectType}

            ### Installation
            ```bash
            # Clone the repository
            git clone <repository-url>
            cd {projectName.ToLowerInvariant().Replace(" ", "-")}

            # Install dependencies
            # Add installation commands here
            ```

            ### Running
            ```bash
            # Add run commands here
            ```

            ## Development

            This project uses [MakeApp](https://github.com/makeapp) for AI-assisted development.

            ### Project Structure
            ```
            .
            ├── .makeapp/           # MakeApp configuration and plans
            ├── .github/            # GitHub configuration
            │   └── copilot-instructions.md
            └── README.md
            ```

            ## Contributing
            1. Create a feature branch
            2. Make your changes
            3. Submit a pull request

            ## License
            Add license information here.
            """;
    }

    private async Task<bool> IsMinimalReadmeAsync(string readmePath, CancellationToken cancellationToken)
    {
        try
        {
            var content = await _fileSystem.File.ReadAllTextAsync(readmePath, cancellationToken);
            // Consider README minimal if it's less than 200 characters (typical auto-generated README)
            return content.Length < 200;
        }
        catch
        {
            return true;
        }
    }

    private async Task CommitAndPushInitialStructureAsync(string repositoryPath)
    {
        _logger.LogDebug("Committing and pushing initial structure");

        await _gitService.StageAllAsync(repositoryPath);
        await _gitService.CommitAsync(repositoryPath, "chore: initialize MakeApp structure", new CommitOptions());
        await _gitService.PushAsync(repositoryPath, new PushOptions());
    }
}
