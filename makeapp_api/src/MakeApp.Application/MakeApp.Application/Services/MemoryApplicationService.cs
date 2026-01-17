using MakeApp.Core.Entities;
using MakeApp.Core.Interfaces;
using MakeApp.Application.DTOs;
using CoreCreateMemoryRequest = MakeApp.Core.Interfaces.CreateMemoryRequest;

namespace MakeApp.Application.Services;

/// <summary>
/// Application service for memory operations
/// </summary>
public class MemoryApplicationService
{
    private readonly IMemoryService _memoryService;

    public MemoryApplicationService(IMemoryService memoryService)
    {
        _memoryService = memoryService;
    }

    /// <summary>
    /// Get memories for a repository
    /// </summary>
    public async Task<IEnumerable<MemoryResponse>> GetMemoriesAsync(
        string owner,
        string name,
        MemoryFilterOptions? filter = null,
        CancellationToken cancellationToken = default)
    {
        var memories = await _memoryService.GetRepositoryMemoriesAsync(owner, name, filter);
        return memories.Select(MapToResponse);
    }

    /// <summary>
    /// Get a specific memory
    /// </summary>
    public async Task<MemoryResponse?> GetMemoryAsync(string id, CancellationToken cancellationToken = default)
    {
        var memory = await _memoryService.GetMemoryAsync(id);
        return memory != null ? MapToResponse(memory) : null;
    }

    /// <summary>
    /// Create a new memory
    /// </summary>
    public async Task<MemoryResponse> CreateMemoryAsync(
        DTOs.CreateMemoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var coreRequest = new CoreCreateMemoryRequest
        {
            RepositoryOwner = request.RepositoryOwner,
            RepositoryName = request.RepositoryName,
            RepositoryPath = request.RepositoryPath ?? "",
            Subject = request.Subject,
            Fact = request.Fact,
            Reason = request.Reason ?? "",
            Citations = request.Citations?.Select(c => new CreateMemoryCitationRequest
            {
                FilePath = c.FilePath,
                LineNumber = c.LineStart
            }).ToList() ?? new List<CreateMemoryCitationRequest>(),
            CreatedByWorkflowId = request.WorkflowId,
            CreatedByUserId = request.UserId
        };

        var memory = await _memoryService.StoreMemoryAsync(coreRequest);
        return MapToResponse(memory);
    }

    /// <summary>
    /// Delete a memory
    /// </summary>
    public async Task<bool> DeleteMemoryAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _memoryService.DeleteMemoryAsync(id);
    }

    /// <summary>
    /// Validate a memory
    /// </summary>
    public async Task<MemoryValidationResponse> ValidateMemoryAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = await _memoryService.ValidateMemoryAsync(id);
        return new MemoryValidationResponse
        {
            MemoryId = id,
            IsValid = result.IsValid,
            ValidationErrors = result.CitationResults
                .Where(c => !c.IsValid)
                .Select(c => $"Invalid citation: {c.Citation.FilePath}")
                .ToList()
        };
    }

    /// <summary>
    /// Refresh a memory's TTL
    /// </summary>
    public async Task<bool> RefreshMemoryAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _memoryService.RefreshMemoryAsync(id);
    }

    /// <summary>
    /// Search memories
    /// </summary>
    public async Task<IEnumerable<MemoryResponse>> SearchMemoriesAsync(
        string repoPath,
        string query,
        CancellationToken cancellationToken = default)
    {
        var memories = await _memoryService.SearchMemoriesAsync(repoPath, query);
        return memories.Select(MapToResponse);
    }

    /// <summary>
    /// Prune expired memories
    /// </summary>
    public async Task<int> PruneExpiredAsync(
        string? owner = null,
        string? name = null,
        CancellationToken cancellationToken = default)
    {
        return await _memoryService.PruneExpiredMemoriesAsync(owner, name);
    }

    private static MemoryResponse MapToResponse(Memory memory)
    {
        return new MemoryResponse
        {
            Id = memory.Id,
            RepositoryOwner = memory.RepositoryOwner,
            RepositoryName = memory.RepositoryName,
            Subject = memory.Subject,
            Fact = memory.Fact,
            Reason = memory.Reason,
            Citations = memory.Citations.Select(c => new CitationResponse
            {
                FilePath = c.FilePath,
                LineStart = c.LineNumber ?? 0,
                Snippet = c.CodeSnippet
            }).ToList(),
            Status = memory.Status,
            CreatedAt = memory.CreatedAt,
            LastValidatedAt = memory.LastValidatedAt,
            LastUsedAt = memory.LastUsedAt,
            UseCount = memory.UseCount,
            ExpiresAt = memory.ExpiresAt,
            IsExpired = memory.IsExpired
        };
    }
}
