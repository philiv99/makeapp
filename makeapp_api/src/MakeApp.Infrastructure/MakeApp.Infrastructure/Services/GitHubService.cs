using MakeApp.Core.Entities;
using MakeApp.Core.Interfaces;
using Octokit;
using RepositoryInfo = MakeApp.Core.Entities.RepositoryInfo;

namespace MakeApp.Infrastructure.Services;

/// <summary>
/// Implementation of IGitHubService using Octokit
/// </summary>
public class GitHubService : IGitHubService
{
    private readonly GitHubClient _client;

    public GitHubService(string? token = null)
    {
        _client = new GitHubClient(new ProductHeaderValue("MakeApp"));
        
        if (!string.IsNullOrEmpty(token))
        {
            _client.Credentials = new Credentials(token);
        }
    }

    /// <inheritdoc/>
    public async Task<RepositoryInfo> CreateRepositoryAsync(CreateRepoOptions options)
    {
        var newRepo = new NewRepository(options.Name)
        {
            Description = options.Description,
            Private = options.Private,
            AutoInit = options.AutoInit,
            GitignoreTemplate = options.GitignoreTemplate,
            LicenseTemplate = options.LicenseTemplate
        };

        Repository repo;
        if (string.IsNullOrEmpty(options.Owner) || options.Owner == (await GetCurrentUserAsync()).Login)
        {
            repo = await _client.Repository.Create(newRepo);
        }
        else
        {
            repo = await _client.Repository.Create(options.Owner, newRepo);
        }

        return MapRepository(repo);
    }

    /// <inheritdoc/>
    public async Task<RepositoryInfo?> GetRepositoryAsync(string owner, string name)
    {
        try
        {
            var repo = await _client.Repository.Get(owner, name);
            return MapRepository(repo);
        }
        catch (NotFoundException)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteRepositoryAsync(string owner, string name)
    {
        try
        {
            await _client.Repository.Delete(owner, name);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<PullRequestInfo> CreatePullRequestAsync(CreatePullRequestOptions options)
    {
        var newPr = new NewPullRequest(options.Title, options.Head, options.Base)
        {
            Body = options.Body,
            Draft = options.Draft
        };

        var pr = await _client.PullRequest.Create(options.Owner, options.Name, newPr);
        return MapPullRequest(pr);
    }

    /// <inheritdoc/>
    public async Task<PullRequestInfo?> GetPullRequestAsync(string owner, string name, int number)
    {
        try
        {
            var pr = await _client.PullRequest.Get(owner, name, number);
            return MapPullRequest(pr);
        }
        catch (NotFoundException)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<GitHubUser> GetCurrentUserAsync()
    {
        var user = await _client.User.Current();
        return new GitHubUser
        {
            Id = user.Id,
            Login = user.Login,
            Name = user.Name,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl
        };
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateCredentialsAsync()
    {
        try
        {
            await _client.User.Current();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RepositoryInfo>> GetRepositoriesAsync(string owner)
    {
        var repos = await _client.Repository.GetAllForUser(owner);
        return repos.Select(MapRepository).ToList();
    }

    private static RepositoryInfo MapRepository(Repository repo)
    {
        return new RepositoryInfo
        {
            Id = repo.Id,
            Name = repo.Name,
            FullName = repo.FullName,
            Owner = repo.Owner.Login,
            Description = repo.Description,
            HtmlUrl = repo.HtmlUrl,
            CloneUrl = repo.CloneUrl,
            SshUrl = repo.SshUrl,
            DefaultBranch = repo.DefaultBranch,
            IsPrivate = repo.Private,
            IsFork = repo.Fork
        };
    }

    private static PullRequestInfo MapPullRequest(PullRequest pr)
    {
        return new PullRequestInfo
        {
            Number = pr.Number,
            Title = pr.Title,
            Body = pr.Body,
            State = pr.State.StringValue,
            HeadRef = pr.Head.Ref,
            BaseRef = pr.Base.Ref,
            HtmlUrl = pr.HtmlUrl,
            IsDraft = pr.Draft,
            IsMerged = pr.Merged,
            CreatedAt = pr.CreatedAt.DateTime,
            UpdatedAt = pr.UpdatedAt.DateTime
        };
    }
}
