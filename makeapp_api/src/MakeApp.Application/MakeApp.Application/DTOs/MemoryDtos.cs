using MakeApp.Core.Enums;

namespace MakeApp.Application.DTOs;

/// <summary>
/// Request to create a memory
/// </summary>
public class CreateMemoryRequest
{
    public string RepositoryOwner { get; set; } = "";
    public string RepositoryName { get; set; } = "";
    public string? RepositoryPath { get; set; }
    public string Subject { get; set; } = "";
    public string Fact { get; set; } = "";
    public string? Reason { get; set; }
    public List<CitationDto>? Citations { get; set; }
    public string? WorkflowId { get; set; }
    public string? UserId { get; set; }
}

/// <summary>
/// Citation DTO
/// </summary>
public class CitationDto
{
    public string FilePath { get; set; } = "";
    public int LineStart { get; set; }
    public int? LineEnd { get; set; }
    public string? Snippet { get; set; }
}

/// <summary>
/// Response for memory operations
/// </summary>
public class MemoryResponse
{
    public string Id { get; set; } = "";
    public string RepositoryOwner { get; set; } = "";
    public string RepositoryName { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Fact { get; set; } = "";
    public string Reason { get; set; } = "";
    public List<CitationResponse> Citations { get; set; } = new();
    public MemoryStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastValidatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public int UseCount { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsExpired { get; set; }
}

/// <summary>
/// Citation response
/// </summary>
public class CitationResponse
{
    public string FilePath { get; set; } = "";
    public int LineStart { get; set; }
    public int? LineEnd { get; set; }
    public string? Snippet { get; set; }
}

/// <summary>
/// Memory validation response
/// </summary>
public class MemoryValidationResponse
{
    public string MemoryId { get; set; } = "";
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
}
