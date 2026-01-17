using MakeApp.Core.Entities;
using MakeApp.Core.Interfaces;

namespace MakeApp.Infrastructure.Services;

/// <summary>
/// Implementation of IMemoryService
/// Note: This is an in-memory implementation. Production would use a database.
/// </summary>
public class MemoryService : IMemoryService
{
    private readonly Dictionary<string, Memory> _memories = new();

    /// <inheritdoc/>
    public Task<IEnumerable<Memory>> GetRepositoryMemoriesAsync(string owner, string name, MemoryFilterOptions? filter = null)
    {
        var query = _memories.Values
            .Where(m => m.RepositoryOwner == owner && m.RepositoryName == name);

        if (filter != null)
        {
            if (!string.IsNullOrEmpty(filter.SubjectContains))
            {
                query = query.Where(m => m.Subject.Contains(filter.SubjectContains, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(filter.FactContains))
            {
                query = query.Where(m => m.Fact.Contains(filter.FactContains, StringComparison.OrdinalIgnoreCase));
            }

            if (!filter.IncludeExpired)
            {
                query = query.Where(m => !m.IsExpired);
            }

            if (filter.MaxResults.HasValue)
            {
                query = query.Take(filter.MaxResults.Value);
            }
        }

        return Task.FromResult(query);
    }

    /// <inheritdoc/>
    public Task<Memory?> GetMemoryAsync(string id)
    {
        _memories.TryGetValue(id, out var memory);
        return Task.FromResult(memory);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<Memory>> SearchMemoriesAsync(string repoPath, string query)
    {
        var results = _memories.Values
            .Where(m => m.RepositoryPath == repoPath)
            .Where(m => 
                m.Subject.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                m.Fact.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(m => m.LastUsedAt ?? m.CreatedAt)
            .Take(10);

        return Task.FromResult(results);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<Memory>> GetMemoriesByCitationAsync(string filePath)
    {
        var results = _memories.Values
            .Where(m => m.Citations.Any(c => c.FilePath == filePath));

        return Task.FromResult(results);
    }

    /// <inheritdoc/>
    public Task<Memory> StoreMemoryAsync(CreateMemoryRequest dto)
    {
        var memory = new Memory
        {
            RepositoryPath = dto.RepositoryPath,
            RepositoryOwner = dto.RepositoryOwner ?? "",
            RepositoryName = dto.RepositoryName ?? "",
            Subject = dto.Subject,
            Fact = dto.Fact,
            Reason = dto.Reason,
            Citations = dto.Citations.Select(c => new MemoryCitation
            {
                FilePath = c.FilePath,
                LineNumber = c.LineNumber
            }).ToList(),
            CreatedByWorkflowId = dto.CreatedByWorkflowId ?? "",
            CreatedByUserId = dto.CreatedByUserId ?? ""
        };

        _memories[memory.Id] = memory;
        return Task.FromResult(memory);
    }

    /// <inheritdoc/>
    public Task<Memory> UpdateMemoryAsync(string id, UpdateMemoryRequest dto)
    {
        if (!_memories.TryGetValue(id, out var memory))
        {
            throw new InvalidOperationException($"Memory {id} not found");
        }

        if (dto.Subject != null)
        {
            memory.Subject = dto.Subject;
        }

        if (dto.Fact != null)
        {
            memory.Fact = dto.Fact;
        }

        if (dto.Reason != null)
        {
            memory.Reason = dto.Reason;
        }

        return Task.FromResult(memory);
    }

    /// <inheritdoc/>
    public Task<bool> DeleteMemoryAsync(string id)
    {
        return Task.FromResult(_memories.Remove(id));
    }

    /// <inheritdoc/>
    public Task<MemoryValidationResult> ValidateMemoryAsync(string id)
    {
        if (!_memories.TryGetValue(id, out var memory))
        {
            return Task.FromResult(new MemoryValidationResult
            {
                MemoryId = id,
                IsValid = false,
                ValidatedAt = DateTime.UtcNow
            });
        }

        // Validate all citations
        var citationResults = memory.Citations.Select(c => new CitationValidationResult
        {
            Citation = c,
            IsValid = File.Exists(c.FilePath) // Simple validation
        }).ToList();

        var isValid = citationResults.All(c => c.IsValid);

        if (isValid)
        {
            memory.LastValidatedAt = DateTime.UtcNow;
        }

        return Task.FromResult(new MemoryValidationResult
        {
            MemoryId = id,
            ValidatedAt = DateTime.UtcNow,
            IsValid = isValid,
            Confidence = isValid ? 1.0 : 0.5,
            CitationResults = citationResults,
            RecommendedAction = isValid ? MemoryAction.Keep : MemoryAction.Review
        });
    }

    /// <inheritdoc/>
    public Task<MemoryValidationReport> ValidateAllMemoriesAsync(string owner, string name)
    {
        var memories = _memories.Values
            .Where(m => m.RepositoryOwner == owner && m.RepositoryName == name)
            .ToList();

        var results = new List<MemoryValidationResult>();
        foreach (var memory in memories)
        {
            var result = ValidateMemoryAsync(memory.Id).Result;
            results.Add(result);
        }

        return Task.FromResult(new MemoryValidationReport
        {
            RepositoryOwner = owner,
            RepositoryName = name,
            ValidatedAt = DateTime.UtcNow,
            TotalMemories = memories.Count,
            ValidMemories = results.Count(r => r.IsValid),
            InvalidMemories = results.Count(r => !r.IsValid),
            Results = results
        });
    }

    /// <inheritdoc/>
    public Task<bool> RefreshMemoryAsync(string id)
    {
        if (!_memories.TryGetValue(id, out var memory))
        {
            return Task.FromResult(false);
        }

        memory.LastValidatedAt = DateTime.UtcNow;
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public Task<int> PruneExpiredMemoriesAsync(string? owner = null, string? name = null)
    {
        var toRemove = _memories.Values
            .Where(m => m.IsExpired)
            .Where(m => owner == null || m.RepositoryOwner == owner)
            .Where(m => name == null || m.RepositoryName == name)
            .Select(m => m.Id)
            .ToList();

        foreach (var id in toRemove)
        {
            _memories.Remove(id);
        }

        return Task.FromResult(toRemove.Count);
    }

    /// <inheritdoc/>
    public Task<MemoryStatistics> GetStatisticsAsync(string owner, string name)
    {
        var memories = _memories.Values
            .Where(m => m.RepositoryOwner == owner && m.RepositoryName == name)
            .ToList();

        return Task.FromResult(new MemoryStatistics
        {
            TotalMemories = memories.Count,
            ActiveMemories = memories.Count(m => !m.IsExpired),
            ExpiredMemories = memories.Count(m => m.IsExpired),
            TotalUseCount = memories.Sum(m => m.UseCount),
            AverageUseCount = memories.Count > 0 ? memories.Average(m => m.UseCount) : 0
        });
    }
}
