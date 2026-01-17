namespace MakeApp.Core.Interfaces;

/// <summary>
/// Abstraction for file system operations to enable testing
/// </summary>
public interface IFileSystem
{
    /// <summary>File operations</summary>
    IFileOperations File { get; }
    
    /// <summary>Directory operations</summary>
    IDirectoryOperations Directory { get; }
    
    /// <summary>Path operations</summary>
    IPathOperations Path { get; }
}

/// <summary>
/// File operations interface
/// </summary>
public interface IFileOperations
{
    /// <summary>Check if a file exists</summary>
    bool Exists(string path);
    
    /// <summary>Read all text from a file</summary>
    string ReadAllText(string path);
    
    /// <summary>Read all text from a file asynchronously</summary>
    Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default);
    
    /// <summary>Read all lines from a file</summary>
    string[] ReadAllLines(string path);
    
    /// <summary>Read all lines from a file asynchronously</summary>
    Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken = default);
    
    /// <summary>Write all text to a file</summary>
    void WriteAllText(string path, string contents);
    
    /// <summary>Write all text to a file asynchronously</summary>
    Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default);
    
    /// <summary>Append all text to a file asynchronously</summary>
    Task AppendAllTextAsync(string path, string contents, CancellationToken cancellationToken = default);
    
    /// <summary>Delete a file</summary>
    void Delete(string path);
    
    /// <summary>Copy a file</summary>
    void Copy(string sourceFileName, string destFileName, bool overwrite = false);
    
    /// <summary>Move a file</summary>
    void Move(string sourceFileName, string destFileName, bool overwrite = false);
    
    /// <summary>Get file info</summary>
    MakeAppFileInfo GetInfo(string path);
}

/// <summary>
/// Directory operations interface
/// </summary>
public interface IDirectoryOperations
{
    /// <summary>Check if a directory exists</summary>
    bool Exists(string path);
    
    /// <summary>Create a directory</summary>
    DirectoryInfo CreateDirectory(string path);
    
    /// <summary>Delete a directory</summary>
    void Delete(string path, bool recursive = false);
    
    /// <summary>Get files in a directory</summary>
    string[] GetFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);
    
    /// <summary>Get directories in a directory</summary>
    string[] GetDirectories(string path);
    
    /// <summary>Get directories with search pattern</summary>
    string[] GetDirectories(string path, string searchPattern, SearchOption searchOption);
    
    /// <summary>Get last write time for a directory</summary>
    DateTime GetLastWriteTime(string path);
    
    /// <summary>Move a directory</summary>
    void Move(string sourceDirName, string destDirName);
}

/// <summary>
/// Path operations interface
/// </summary>
public interface IPathOperations
{
    /// <summary>Combine paths</summary>
    string Combine(params string[] paths);
    
    /// <summary>Get directory name</summary>
    string? GetDirectoryName(string? path);
    
    /// <summary>Get file name</summary>
    string? GetFileName(string? path);
    
    /// <summary>Get file name without extension</summary>
    string? GetFileNameWithoutExtension(string? path);
    
    /// <summary>Get extension</summary>
    string? GetExtension(string? path);
    
    /// <summary>Get full path</summary>
    string GetFullPath(string path);
    
    /// <summary>Check if path is rooted</summary>
    bool IsPathRooted(string? path);
}

/// <summary>
/// File info wrapper (named MakeAppFileInfo to avoid conflict with System.IO.FileInfo)
/// </summary>
public class MakeAppFileInfo
{
    /// <summary>File name</summary>
    public string Name { get; set; } = "";
    
    /// <summary>Full path</summary>
    public string FullName { get; set; } = "";
    
    /// <summary>File size in bytes</summary>
    public long Length { get; set; }
    
    /// <summary>Whether the file exists</summary>
    public bool Exists { get; set; }
    
    /// <summary>Last write time</summary>
    public DateTime LastWriteTime { get; set; }
    
    /// <summary>Creation time</summary>
    public DateTime CreationTime { get; set; }
}
