using MakeApp.Core.Entities;
using MakeApp.Core.Interfaces;

namespace MakeApp.Infrastructure.Services;

/// <summary>
/// Implementation of ISandboxService
/// </summary>
public class SandboxService : ISandboxService
{
    private readonly IFileSystem _fileSystem;
    private readonly IRepositoryService _repositoryService;
    private readonly string _sandboxPath;

    public SandboxService(
        IFileSystem fileSystem,
        IRepositoryService repositoryService,
        string? sandboxPath = null)
    {
        _fileSystem = fileSystem;
        _repositoryService = repositoryService;
        _sandboxPath = sandboxPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".makeapp",
            "sandbox");
    }

    /// <inheritdoc/>
    public async Task<SandboxInfo> GetSandboxInfoAsync()
    {
        var info = new SandboxInfo
        {
            Path = _sandboxPath,
            Exists = _fileSystem.Directory.Exists(_sandboxPath)
        };

        if (info.Exists)
        {
            var repos = await ListSandboxReposAsync();
            info.Repositories = repos.ToList();
            info.RepoCount = info.Repositories.Count;
            
            // Calculate total size
            info.TotalSize = CalculateFolderSize(_sandboxPath);
        }

        return info;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<RepositorySummary>> ListSandboxReposAsync()
    {
        if (!_fileSystem.Directory.Exists(_sandboxPath))
        {
            return Enumerable.Empty<RepositorySummary>();
        }

        var repos = await _repositoryService.ScanFolderAsync(_sandboxPath);
        
        return repos.Select(r => new RepositorySummary
        {
            Name = r.Name,
            Owner = r.Owner,
            LocalPath = r.LocalPath ?? "",
            LastModified = GetLastModified(r.LocalPath ?? "")
        });
    }

    /// <inheritdoc/>
    public Task<bool> ValidateSandboxPathAsync()
    {
        try
        {
            if (!_fileSystem.Directory.Exists(_sandboxPath))
            {
                _fileSystem.Directory.CreateDirectory(_sandboxPath);
            }
            
            // Test write access
            var testFile = _fileSystem.Path.Combine(_sandboxPath, ".test");
            _fileSystem.File.WriteAllText(testFile, "test");
            _fileSystem.File.Delete(testFile);
            
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc/>
    public async Task<RepositoryStatus> GetRepoStatusAsync(string repoName)
    {
        var repoPath = _fileSystem.Path.Combine(_sandboxPath, repoName);
        
        if (!_fileSystem.Directory.Exists(repoPath))
        {
            return new RepositoryStatus
            {
                Name = repoName,
                Status = Core.Enums.RepoStatusType.NotFound
            };
        }

        var isValid = await _repositoryService.ValidateRepositoryAsync(repoPath);
        
        return new RepositoryStatus
        {
            Name = repoName,
            Path = repoPath,
            Status = isValid ? Core.Enums.RepoStatusType.Ready : Core.Enums.RepoStatusType.Invalid,
            LastModified = GetLastModified(repoPath),
            Size = CalculateFolderSize(repoPath)
        };
    }

    /// <inheritdoc/>
    public Task<RemoveResult> RemoveRepoAsync(string repoName, bool force = false)
    {
        var repoPath = _fileSystem.Path.Combine(_sandboxPath, repoName);
        
        if (!_fileSystem.Directory.Exists(repoPath))
        {
            return Task.FromResult(new RemoveResult
            {
                Success = false,
                Message = $"Repository '{repoName}' not found"
            });
        }

        try
        {
            _fileSystem.Directory.Delete(repoPath, recursive: true);
            return Task.FromResult(new RemoveResult
            {
                Success = true,
                Message = $"Repository '{repoName}' removed successfully"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new RemoveResult
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <inheritdoc/>
    public Task CleanupRepoWorkingFilesAsync(string repoName)
    {
        var repoPath = _fileSystem.Path.Combine(_sandboxPath, repoName);
        
        CleanupFolder(_fileSystem.Path.Combine(repoPath, "cache"));
        CleanupFolder(_fileSystem.Path.Combine(repoPath, "logs"));
        CleanupFolder(_fileSystem.Path.Combine(repoPath, "temp"));
        
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task CleanupAllWorkingFilesAsync()
    {
        var repos = await ListSandboxReposAsync();
        foreach (var repo in repos)
        {
            await CleanupRepoWorkingFilesAsync(repo.Name);
        }
    }

    private void CleanupFolder(string path)
    {
        if (_fileSystem.Directory.Exists(path))
        {
            _fileSystem.Directory.Delete(path, recursive: true);
            _fileSystem.Directory.CreateDirectory(path);
        }
    }

    private DateTime GetLastModified(string path)
    {
        try
        {
            return new DirectoryInfo(path).LastWriteTimeUtc;
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    private long CalculateFolderSize(string path)
    {
        try
        {
            var files = _fileSystem.Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            return files.Sum(f => new FileInfo(f).Length);
        }
        catch
        {
            return 0;
        }
    }
}
