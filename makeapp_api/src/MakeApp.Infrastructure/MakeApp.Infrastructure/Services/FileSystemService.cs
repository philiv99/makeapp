using MakeApp.Core.Interfaces;

namespace MakeApp.Infrastructure.Services;

/// <summary>
/// Implementation of IFileSystem for real file system operations
/// </summary>
public class FileSystemService : IFileSystem
{
    /// <inheritdoc/>
    public IFileOperations File { get; } = new FileOperationsService();

    /// <inheritdoc/>
    public IDirectoryOperations Directory { get; } = new DirectoryOperationsService();

    /// <inheritdoc/>
    public IPathOperations Path { get; } = new PathOperationsService();
}

/// <summary>
/// Implementation of file operations
/// </summary>
public class FileOperationsService : IFileOperations
{
    /// <inheritdoc/>
    public bool Exists(string path) => System.IO.File.Exists(path);

    /// <inheritdoc/>
    public string ReadAllText(string path) => System.IO.File.ReadAllText(path);

    /// <inheritdoc/>
    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
        => System.IO.File.ReadAllTextAsync(path, cancellationToken);

    /// <inheritdoc/>
    public string[] ReadAllLines(string path) => System.IO.File.ReadAllLines(path);

    /// <inheritdoc/>
    public Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken = default)
        => System.IO.File.ReadAllLinesAsync(path, cancellationToken);

    /// <inheritdoc/>
    public void WriteAllText(string path, string contents) => System.IO.File.WriteAllText(path, contents);

    /// <inheritdoc/>
    public Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
        => System.IO.File.WriteAllTextAsync(path, contents, cancellationToken);

    /// <inheritdoc/>
    public Task AppendAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
        => System.IO.File.AppendAllTextAsync(path, contents, cancellationToken);

    /// <inheritdoc/>
    public void Delete(string path) => System.IO.File.Delete(path);

    /// <inheritdoc/>
    public void Copy(string sourceFileName, string destFileName, bool overwrite = false)
        => System.IO.File.Copy(sourceFileName, destFileName, overwrite);

    /// <inheritdoc/>
    public void Move(string sourceFileName, string destFileName, bool overwrite = false)
        => System.IO.File.Move(sourceFileName, destFileName, overwrite);

    /// <inheritdoc/>
    public Core.Interfaces.MakeAppFileInfo GetInfo(string path)
    {
        var info = new System.IO.FileInfo(path);
        return new Core.Interfaces.MakeAppFileInfo
        {
            Name = info.Name,
            FullName = info.FullName,
            Length = info.Exists ? info.Length : 0,
            Exists = info.Exists,
            LastWriteTime = info.Exists ? info.LastWriteTime : DateTime.MinValue,
            CreationTime = info.Exists ? info.CreationTime : DateTime.MinValue
        };
    }
}

/// <summary>
/// Implementation of directory operations
/// </summary>
public class DirectoryOperationsService : IDirectoryOperations
{
    /// <inheritdoc/>
    public bool Exists(string path) => System.IO.Directory.Exists(path);

    /// <inheritdoc/>
    public DirectoryInfo CreateDirectory(string path) => System.IO.Directory.CreateDirectory(path);

    /// <inheritdoc/>
    public void Delete(string path, bool recursive = false) => System.IO.Directory.Delete(path, recursive);

    /// <inheritdoc/>
    public string[] GetFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        => System.IO.Directory.GetFiles(path, searchPattern, searchOption);

    /// <inheritdoc/>
    public string[] GetDirectories(string path)
        => System.IO.Directory.GetDirectories(path);

    /// <inheritdoc/>
    public string[] GetDirectories(string path, string searchPattern, SearchOption searchOption)
        => System.IO.Directory.GetDirectories(path, searchPattern, searchOption);

    /// <inheritdoc/>
    public DateTime GetLastWriteTime(string path)
        => System.IO.Directory.GetLastWriteTime(path);

    /// <inheritdoc/>
    public void Move(string sourceDirName, string destDirName) => System.IO.Directory.Move(sourceDirName, destDirName);
}

/// <summary>
/// Implementation of path operations
/// </summary>
public class PathOperationsService : IPathOperations
{
    /// <inheritdoc/>
    public string Combine(params string[] paths) => System.IO.Path.Combine(paths);

    /// <inheritdoc/>
    public string? GetDirectoryName(string? path) => System.IO.Path.GetDirectoryName(path);

    /// <inheritdoc/>
    public string? GetFileName(string? path) => System.IO.Path.GetFileName(path);

    /// <inheritdoc/>
    public string? GetFileNameWithoutExtension(string? path) => System.IO.Path.GetFileNameWithoutExtension(path);

    /// <inheritdoc/>
    public string? GetExtension(string? path) => System.IO.Path.GetExtension(path);

    /// <inheritdoc/>
    public string GetFullPath(string path) => System.IO.Path.GetFullPath(path);

    /// <inheritdoc/>
    public bool IsPathRooted(string? path) => System.IO.Path.IsPathRooted(path);
}
