namespace MakeApp.Core.Configuration;

/// <summary>
/// Memory system configuration options
/// </summary>
public class MemoryOptions
{
    /// <summary>Configuration section name</summary>
    public const string SectionName = "Memory";
    
    /// <summary>Whether memory system is enabled</summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>Maximum memories to include in prompts</summary>
    public int MaxMemoriesPerPrompt { get; set; } = 10;
    
    /// <summary>Whether to verify citations before use</summary>
    public bool VerifyBeforeUse { get; set; } = true;
    
    /// <summary>Whether to automatically store discoveries</summary>
    public bool AutoStoreDiscoveries { get; set; } = true;
    
    /// <summary>Memory TTL in days (default 28)</summary>
    public int TtlDays { get; set; } = 28;
    
    /// <summary>Minimum confidence for memory to be valid</summary>
    public double MinimumConfidence { get; set; } = 0.5;
}
