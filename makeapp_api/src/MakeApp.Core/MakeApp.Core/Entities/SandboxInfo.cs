namespace MakeApp.Core.Entities;

/// <summary>
/// Information about the sandbox environment
/// </summary>
public class SandboxInfo
{
    /// <summary>Path to the sandbox directory</summary>
    public string Path { get; set; } = "";
    
    /// <summary>Whether the sandbox directory exists</summary>
    public bool Exists { get; set; }
    
    /// <summary>Number of repositories in the sandbox</summary>
    public int RepoCount { get; set; }
    
    /// <summary>List of repositories in the sandbox</summary>
    public List<RepositorySummary> Repositories { get; set; } = new();
    
    /// <summary>Total size of the sandbox in bytes</summary>
    public long TotalSize { get; set; }
    
    /// <summary>Human-readable total size</summary>
    public string TotalSizeFormatted => FormatBytes(TotalSize);
    
    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int i = 0;
        double size = bytes;
        while (size >= 1024 && i < suffixes.Length - 1)
        {
            size /= 1024;
            i++;
        }
        return $"{size:0.##} {suffixes[i]}";
    }
}
