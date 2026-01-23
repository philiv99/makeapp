namespace MakeApp.Core.Enums;

/// <summary>
/// Type of file change in a git repository
/// </summary>
public enum ChangeType
{
    /// <summary>File was modified</summary>
    Modified,
    
    /// <summary>File was added</summary>
    Added,
    
    /// <summary>File was deleted</summary>
    Deleted,
    
    /// <summary>File was renamed</summary>
    Renamed,
    
    /// <summary>File was copied</summary>
    Copied,
    
    /// <summary>File type was changed</summary>
    TypeChanged,
    
    /// <summary>File status is untracked</summary>
    Untracked
}
