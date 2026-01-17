using MakeApp.Core.Entities;

namespace MakeApp.Core.Interfaces;

/// <summary>
/// Service interface for memory operations
/// </summary>
public interface IMemoryService
{
    // Retrieval
    
    /// <summary>Get memories for a repository</summary>
    Task<IEnumerable<Memory>> GetRepositoryMemoriesAsync(string owner, string name, MemoryFilterOptions? filter = null);
    
    /// <summary>Get a specific memory</summary>
    Task<Memory?> GetMemoryAsync(string id);
    
    /// <summary>Search memories</summary>
    Task<IEnumerable<Memory>> SearchMemoriesAsync(string repoPath, string query);
    
    /// <summary>Get memories by citation file</summary>
    Task<IEnumerable<Memory>> GetMemoriesByCitationAsync(string filePath);
    
    // Storage
    
    /// <summary>Store a new memory</summary>
    Task<Memory> StoreMemoryAsync(CreateMemoryRequest dto);
    
    /// <summary>Update a memory</summary>
    Task<Memory> UpdateMemoryAsync(string id, UpdateMemoryRequest dto);
    
    /// <summary>Delete a memory</summary>
    Task<bool> DeleteMemoryAsync(string id);
    
    // Validation
    
    /// <summary>Validate a memory's citations</summary>
    Task<MemoryValidationResult> ValidateMemoryAsync(string id);
    
    /// <summary>Validate all memories for a repository</summary>
    Task<MemoryValidationReport> ValidateAllMemoriesAsync(string owner, string name);
    
    /// <summary>Refresh a memory's timestamp (extend TTL)</summary>
    Task<bool> RefreshMemoryAsync(string id);
    
    // Maintenance
    
    /// <summary>Prune expired memories</summary>
    Task<int> PruneExpiredMemoriesAsync(string? owner = null, string? name = null);
    
    /// <summary>Get memory statistics</summary>
    Task<MemoryStatistics> GetStatisticsAsync(string owner, string name);
}

/// <summary>
/// Memory filter options
/// </summary>
public class MemoryFilterOptions
{
    /// <summary>Filter by subject containing text</summary>
    public string? SubjectContains { get; set; }
    
    /// <summary>Filter by fact containing text</summary>
    public string? FactContains { get; set; }
    
    /// <summary>Filter by file affected</summary>
    public string? AffectsFile { get; set; }
    
    /// <summary>Include expired memories</summary>
    public bool IncludeExpired { get; set; }
    
    /// <summary>Include invalid memories</summary>
    public bool IncludeInvalid { get; set; }
    
    /// <summary>Filter by created after date</summary>
    public DateTime? CreatedAfter { get; set; }
    
    /// <summary>Maximum results to return</summary>
    public int? MaxResults { get; set; } = 20;
    
    /// <summary>Sort order</summary>
    public MemorySortBy SortBy { get; set; } = MemorySortBy.LastUsed;
}

/// <summary>
/// Memory sort options
/// </summary>
public enum MemorySortBy
{
    /// <summary>Sort by last used</summary>
    LastUsed,
    
    /// <summary>Sort by last validated</summary>
    LastValidated,
    
    /// <summary>Sort by creation date</summary>
    Created,
    
    /// <summary>Sort by use count</summary>
    UseCount,
    
    /// <summary>Sort by relevance</summary>
    Relevance
}

/// <summary>
/// Request to create a memory
/// </summary>
public class CreateMemoryRequest
{
    /// <summary>Repository path</summary>
    public string RepositoryPath { get; set; } = "";
    
    /// <summary>Repository owner</summary>
    public string? RepositoryOwner { get; set; }
    
    /// <summary>Repository name</summary>
    public string? RepositoryName { get; set; }
    
    /// <summary>Memory subject</summary>
    public string Subject { get; set; } = "";
    
    /// <summary>Memory fact</summary>
    public string Fact { get; set; } = "";
    
    /// <summary>Citations</summary>
    public List<CreateMemoryCitationRequest> Citations { get; set; } = new();
    
    /// <summary>Reason for the memory</summary>
    public string Reason { get; set; } = "";
    
    /// <summary>Workflow ID that created this memory</summary>
    public string? CreatedByWorkflowId { get; set; }
    
    /// <summary>User ID that created this memory</summary>
    public string? CreatedByUserId { get; set; }
}

/// <summary>
/// Request to create a memory citation
/// </summary>
public class CreateMemoryCitationRequest
{
    /// <summary>File path</summary>
    public string FilePath { get; set; } = "";
    
    /// <summary>Line number</summary>
    public int? LineNumber { get; set; }
}

/// <summary>
/// Request to update a memory
/// </summary>
public class UpdateMemoryRequest
{
    /// <summary>Updated subject</summary>
    public string? Subject { get; set; }
    
    /// <summary>Updated fact</summary>
    public string? Fact { get; set; }
    
    /// <summary>Updated reason</summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Memory validation result
/// </summary>
public class MemoryValidationResult
{
    /// <summary>Memory ID</summary>
    public string MemoryId { get; set; } = "";
    
    /// <summary>When validation was performed</summary>
    public DateTime ValidatedAt { get; set; }
    
    /// <summary>Whether the memory is valid</summary>
    public bool IsValid { get; set; }
    
    /// <summary>Validation confidence (0-1)</summary>
    public double Confidence { get; set; }
    
    /// <summary>Citation validation results</summary>
    public List<CitationValidationResult> CitationResults { get; set; } = new();
    
    /// <summary>Recommended action</summary>
    public MemoryAction RecommendedAction { get; set; }
}

/// <summary>
/// Citation validation result
/// </summary>
public class CitationValidationResult
{
    /// <summary>The citation</summary>
    public MemoryCitation Citation { get; set; } = new();
    
    /// <summary>Whether the citation is valid</summary>
    public bool IsValid { get; set; }
    
    /// <summary>Issue description if invalid</summary>
    public string? Issue { get; set; }
    
    /// <summary>Current content at location</summary>
    public string? CurrentContent { get; set; }
    
    /// <summary>When verified</summary>
    public DateTime? VerifiedAt { get; set; }
}

/// <summary>
/// Recommended action for a memory
/// </summary>
public enum MemoryAction
{
    /// <summary>Keep the memory</summary>
    Keep,
    
    /// <summary>Review the memory</summary>
    Review,
    
    /// <summary>Refresh the memory</summary>
    Refresh,
    
    /// <summary>Update citations</summary>
    UpdateCitations,
    
    /// <summary>Review manually</summary>
    ReviewManually,
    
    /// <summary>Delete the memory</summary>
    Delete
}

/// <summary>
/// Memory validation report for a repository
/// </summary>
public class MemoryValidationReport
{
    /// <summary>Repository owner</summary>
    public string Owner { get; set; } = "";
    
    /// <summary>Repository owner (alias for Owner)</summary>
    public string RepositoryOwner { get => Owner; set => Owner = value; }
    
    /// <summary>Repository name</summary>
    public string Name { get; set; } = "";
    
    /// <summary>Repository name (alias for Name)</summary>
    public string RepositoryName { get => Name; set => Name = value; }
    
    /// <summary>When the validation was performed</summary>
    public DateTime ValidatedAt { get; set; }
    
    /// <summary>Total memories checked</summary>
    public int TotalMemories { get; set; }
    
    /// <summary>Valid memories</summary>
    public int ValidCount { get; set; }
    
    /// <summary>Valid memories (alias for ValidCount)</summary>
    public int ValidMemories { get => ValidCount; set => ValidCount = value; }
    
    /// <summary>Stale memories</summary>
    public int StaleCount { get; set; }
    
    /// <summary>Invalid memories</summary>
    public int InvalidCount { get; set; }
    
    /// <summary>Invalid memories (alias for InvalidCount)</summary>
    public int InvalidMemories { get => InvalidCount; set => InvalidCount = value; }
    
    /// <summary>Individual validation results</summary>
    public List<MemoryValidationResult> Results { get; set; } = new();
}

/// <summary>
/// Memory statistics for a repository
/// </summary>
public class MemoryStatistics
{
    /// <summary>Total memories</summary>
    public int TotalMemories { get; set; }
    
    /// <summary>Active memories</summary>
    public int ActiveMemories { get; set; }
    
    /// <summary>Expired memories</summary>
    public int ExpiredMemories { get; set; }
    
    /// <summary>Total use count</summary>
    public int TotalUseCount { get; set; }
    
    /// <summary>Total citations</summary>
    public int TotalCitations { get; set; }
    
    /// <summary>Average use count</summary>
    public double AverageUseCount { get; set; }
    
    /// <summary>Most used memory subject</summary>
    public string? MostUsedSubject { get; set; }
    
    /// <summary>Most cited file</summary>
    public string? MostCitedFile { get; set; }
}
