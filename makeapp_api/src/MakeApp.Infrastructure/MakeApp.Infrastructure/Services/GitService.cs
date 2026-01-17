using LibGit2Sharp;
using MakeApp.Core.Entities;
using MakeApp.Core.Interfaces;
using BranchInfo = MakeApp.Core.Entities.BranchInfo;
using CommitInfo = MakeApp.Core.Entities.CommitInfo;
using FileChange = MakeApp.Core.Entities.FileChange;
using CoreCommitOptions = MakeApp.Core.Interfaces.CommitOptions;
using CoreCommitResult = MakeApp.Core.Interfaces.CommitResult;
using CorePushOptions = MakeApp.Core.Interfaces.PushOptions;
using CorePushResult = MakeApp.Core.Interfaces.PushResult;
using CoreCloneOptions = MakeApp.Core.Interfaces.CloneOptions;

namespace MakeApp.Infrastructure.Services;

/// <summary>
/// Implementation of IGitService using LibGit2Sharp
/// </summary>
public class GitService : IGitService
{
    /// <inheritdoc/>
    public Task<GitStatus> GetStatusAsync(string repoPath)
    {
        using var repo = new Repository(repoPath);
        var status = repo.RetrieveStatus();
        
        return Task.FromResult(new GitStatus
        {
            IsDirty = status.IsDirty,
            CurrentBranch = repo.Head.FriendlyName,
            StagedCount = status.Staged.Count(),
            ModifiedCount = status.Modified.Count(),
            UntrackedCount = status.Untracked.Count(),
            DeletedCount = status.Missing.Count()
        });
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<FileChange>> GetUnstagedChangesAsync(string repoPath)
    {
        using var repo = new Repository(repoPath);
        var status = repo.RetrieveStatus();
        
        var changes = status.Modified.Concat(status.Missing).Concat(status.Untracked)
            .Select(e => new FileChange
            {
                FilePath = e.FilePath,
                ChangeType = MapChangeType(e.State)
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<FileChange>>(changes);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<FileChange>> GetStagedChangesAsync(string repoPath)
    {
        using var repo = new Repository(repoPath);
        var status = repo.RetrieveStatus();
        
        var changes = status.Staged
            .Select(e => new FileChange
            {
                FilePath = e.FilePath,
                ChangeType = MapChangeType(e.State)
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<FileChange>>(changes);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<CommitInfo>> GetUnpushedCommitsAsync(string repoPath, string? remoteName = "origin")
    {
        using var repo = new Repository(repoPath);
        
        var commits = new List<CommitInfo>();
        
        var tracking = repo.Head.TrackedBranch;
        if (tracking != null)
        {
            var unpushed = repo.Commits.QueryBy(new CommitFilter
            {
                IncludeReachableFrom = repo.Head.Tip,
                ExcludeReachableFrom = tracking.Tip
            });
            
            commits.AddRange(unpushed.Select(c => new CommitInfo
            {
                Sha = c.Sha,
                Message = c.Message,
                Author = c.Author.Name,
                AuthorEmail = c.Author.Email,
                Date = c.Author.When.DateTime
            }));
        }

        return Task.FromResult<IReadOnlyList<CommitInfo>>(commits);
    }

    /// <inheritdoc/>
    public Task<bool> IsCleanAsync(string repoPath)
    {
        using var repo = new Repository(repoPath);
        return Task.FromResult(!repo.RetrieveStatus().IsDirty);
    }

    /// <inheritdoc/>
    public Task<string> GetCurrentBranchAsync(string repoPath)
    {
        using var repo = new Repository(repoPath);
        return Task.FromResult(repo.Head.FriendlyName);
    }

    /// <inheritdoc/>
    public Task<bool> StageChangesAsync(string repoPath, string pathSpec = ".")
    {
        using var repo = new Repository(repoPath);
        Commands.Stage(repo, pathSpec);
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public Task<bool> StageAllAsync(string repoPath)
    {
        using var repo = new Repository(repoPath);
        Commands.Stage(repo, "*");
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public Task<CoreCommitResult> CommitAsync(string repoPath, string message, CoreCommitOptions? options = null)
    {
        using var repo = new Repository(repoPath);
        
        var author = new Signature(
            options?.AuthorName ?? repo.Config.Get<string>("user.name")?.Value ?? "MakeApp",
            options?.AuthorEmail ?? repo.Config.Get<string>("user.email")?.Value ?? "makeapp@local",
            DateTimeOffset.Now);

        var commit = repo.Commit(message, author, author);
        
        return Task.FromResult(new CoreCommitResult
        {
            Success = true,
            CommitSha = commit.Sha
        });
    }

    /// <inheritdoc/>
    public Task<CorePushResult> PushAsync(string repoPath, CorePushOptions? options = null)
    {
        using var repo = new Repository(repoPath);
        
        var remote = repo.Network.Remotes[options?.RemoteName ?? "origin"];
        if (remote == null)
        {
            return Task.FromResult(new CorePushResult { Success = false, Error = "Remote 'origin' not found" });
        }

        try
        {
            var pushOptions = new LibGit2Sharp.PushOptions();
            repo.Network.Push(repo.Head, pushOptions);
            
            return Task.FromResult(new CorePushResult { Success = true });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new CorePushResult { Success = false, Error = ex.Message });
        }
    }

    /// <inheritdoc/>
    public Task<CorePushResult> PushAsync(string repoPath, string branchName, bool setUpstream = false)
    {
        using var repo = new Repository(repoPath);
        
        var remote = repo.Network.Remotes["origin"];
        if (remote == null)
        {
            return Task.FromResult(new CorePushResult { Success = false, Error = "Remote 'origin' not found" });
        }

        try
        {
            var branch = repo.Branches[branchName];
            if (branch == null)
            {
                return Task.FromResult(new CorePushResult { Success = false, Error = $"Branch '{branchName}' not found" });
            }

            var pushOptions = new LibGit2Sharp.PushOptions();
            repo.Network.Push(branch, pushOptions);
            
            return Task.FromResult(new CorePushResult { Success = true });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new CorePushResult { Success = false, Error = ex.Message });
        }
    }

    /// <inheritdoc/>
    public Task<BranchInfo> CreateBranchAsync(string repoPath, string branchName, string? baseBranch = null)
    {
        using var repo = new Repository(repoPath);
        
        var startPoint = baseBranch != null 
            ? repo.Branches[baseBranch].Tip 
            : repo.Head.Tip;
        
        var branch = repo.CreateBranch(branchName, startPoint);
        
        return Task.FromResult(new BranchInfo
        {
            Name = branch.FriendlyName,
            IsRemote = branch.IsRemote,
            IsCurrentHead = branch.IsCurrentRepositoryHead,
            LastCommitSha = branch.Tip?.Sha
        });
    }

    /// <inheritdoc/>
    public Task<bool> CheckoutAsync(string repoPath, string branchName)
    {
        using var repo = new Repository(repoPath);
        
        var branch = repo.Branches[branchName];
        if (branch == null)
        {
            return Task.FromResult(false);
        }

        Commands.Checkout(repo, branch);
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<BranchInfo>> GetBranchesAsync(string repoPath, bool includeRemote = false)
    {
        using var repo = new Repository(repoPath);
        
        var branches = repo.Branches
            .Where(b => includeRemote || !b.IsRemote)
            .Select(b => new BranchInfo
            {
                Name = b.FriendlyName,
                IsRemote = b.IsRemote,
                IsCurrentHead = b.IsCurrentRepositoryHead,
                LastCommitSha = b.Tip?.Sha
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<BranchInfo>>(branches);
    }

    /// <inheritdoc/>
    public Task<bool> DeleteBranchAsync(string repoPath, string branchName, bool force = false)
    {
        using var repo = new Repository(repoPath);
        
        var branch = repo.Branches[branchName];
        if (branch == null)
        {
            return Task.FromResult(false);
        }

        repo.Branches.Remove(branch);
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public Task<string> CloneAsync(string cloneUrl, string localPath, CoreCloneOptions? options = null)
    {
        var cloneOptions = new LibGit2Sharp.CloneOptions
        {
            IsBare = false
        };

        var path = Repository.Clone(cloneUrl, localPath, cloneOptions);
        return Task.FromResult(path);
    }

    private static Core.Enums.ChangeType MapChangeType(FileStatus status)
    {
        return status switch
        {
            FileStatus.NewInIndex or FileStatus.NewInWorkdir => Core.Enums.ChangeType.Added,
            FileStatus.ModifiedInIndex or FileStatus.ModifiedInWorkdir => Core.Enums.ChangeType.Modified,
            FileStatus.DeletedFromIndex or FileStatus.DeletedFromWorkdir => Core.Enums.ChangeType.Deleted,
            FileStatus.RenamedInIndex or FileStatus.RenamedInWorkdir => Core.Enums.ChangeType.Renamed,
            _ => Core.Enums.ChangeType.Modified
        };
    }
}
