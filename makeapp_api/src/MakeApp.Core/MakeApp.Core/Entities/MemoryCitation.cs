namespace MakeApp.Core.Entities;

/// <summary>
/// Represents a citation in a memory
/// </summary>
public class MemoryCitation
{
    /// <summary>File path relative to repository root</summary>
    public string FilePath { get; set; } = "";
    
    /// <summary>Optional line number in the file</summary>
    public int? LineNumber { get; set; }
    
    /// <summary>Optional code snippet at the citation</summary>
    public string? CodeSnippet { get; set; }
    
    /// <summary>When the citation was last verified</summary>
    public DateTime LastVerified { get; set; } = DateTime.UtcNow;
    
    /// <summary>Whether the citation is currently valid</summary>
    public bool IsValid { get; set; } = true;
    
    /// <summary>Formats the citation as a string (e.g., "file.ts:42")</summary>
    public override string ToString()
    {
        return LineNumber.HasValue 
            ? $"{FilePath}:{LineNumber}" 
            : FilePath;
    }
    
    /// <summary>Parses a citation string into a MemoryCitation</summary>
    public static MemoryCitation Parse(string citation)
    {
        var parts = citation.Split(':');
        return new MemoryCitation
        {
            FilePath = parts[0],
            LineNumber = parts.Length > 1 && int.TryParse(parts[1], out var line) ? line : null
        };
    }
}
