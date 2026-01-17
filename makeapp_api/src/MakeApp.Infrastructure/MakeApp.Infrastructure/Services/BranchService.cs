using MakeApp.Core.Entities;
using MakeApp.Core.Interfaces;

namespace MakeApp.Infrastructure.Services;

/// <summary>
/// Implementation of IBranchService
/// </summary>
public class BranchService : IBranchService
{
    private readonly IGitService _gitService;

    public BranchService(IGitService gitService)
    {
        _gitService = gitService;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<BranchInfo>> GetBranchesAsync(string repoPath, bool includeRemote = false)
    {
        return await _gitService.GetBranchesAsync(repoPath, includeRemote);
    }

    /// <inheritdoc/>
    public async Task<BranchInfo> CreateFeatureBranchAsync(string repoPath, string branchName, string baseBranch = "main")
    {
        var formattedName = FormatFeatureBranchName(branchName);
        return await _gitService.CreateBranchAsync(repoPath, formattedName, baseBranch);
    }

    /// <inheritdoc/>
    public async Task<bool> SwitchToBranchAsync(string repoPath, string branchName)
    {
        return await _gitService.CheckoutAsync(repoPath, branchName);
    }

    /// <inheritdoc/>
    public async Task<string> GetCurrentBranchAsync(string repoPath)
    {
        return await _gitService.GetCurrentBranchAsync(repoPath);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteBranchAsync(string repoPath, string branchName, bool force = false)
    {
        return await _gitService.DeleteBranchAsync(repoPath, branchName, force);
    }

    /// <inheritdoc/>
    public string FormatFeatureBranchName(string featureName)
    {
        // Convert to lowercase and replace spaces/special chars with hyphens
        var formatted = featureName
            .ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-");

        // Remove any invalid characters
        formatted = System.Text.RegularExpressions.Regex.Replace(formatted, @"[^a-z0-9\-]", "");

        // Remove consecutive hyphens
        formatted = System.Text.RegularExpressions.Regex.Replace(formatted, @"-+", "-");

        // Trim hyphens from start and end
        formatted = formatted.Trim('-');

        // Add feature prefix if not already present
        if (!formatted.StartsWith("feature/"))
        {
            formatted = $"feature/{formatted}";
        }

        return formatted;
    }
}
