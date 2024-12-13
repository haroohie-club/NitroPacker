using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HaroohieClub.NitroPacker.IO.Archive;

/// <summary>
/// An "archive" representing a particular folder on the user's computer
/// </summary>
public class DiskArchive : Archive
{
    /// <summary>
    /// The root path of the disk
    /// </summary>
    public string RootDiskPath { get; }

    /// <inheritdoc />
    public DiskArchive(string rootPath)
    {
        RootDiskPath = rootPath;
        if (!Directory.Exists(RootDiskPath))
            throw new DirectoryNotFoundException();
    }

    /// <inheritdoc />
    public override bool IsReadOnly => false;

    private string CreateDiskPath(string path)
    {
        // remove any root and trailing slashes
        path = path.Trim(PathSeparator);

        // convert to the right slash type
        path = path.Replace(PathSeparator, Path.DirectorySeparatorChar);

        if (!string.IsNullOrEmpty(Path.GetPathRoot(path)))
            throw new("Invalid path specified");

        path = Path.Combine(RootDiskPath, path);

        // check that the path does not accidentally go up past the root of the archive
        string relPath = Path.GetRelativePath(RootDiskPath, path);
        if (relPath.StartsWith(".."))
            throw new("Invalid path specified");

        return path;
    }

    /// <inheritdoc />
    public override IEnumerable<string> EnumerateFiles(string path, bool fullPath)
    {
        var files = Directory.EnumerateFiles(CreateDiskPath(path));
        return fullPath ? files.Select(file =>
                PathSeparator + Path.GetRelativePath(RootDiskPath, file)
                    .Replace(Path.DirectorySeparatorChar, PathSeparator)) :
            files.Select(Path.GetFileName);
    }

    /// <inheritdoc />
    public override IEnumerable<string> EnumerateDirectories(string path, bool fullPath)
    {
        var dirs = Directory.EnumerateDirectories(CreateDiskPath(path));
        return fullPath ? dirs.Select(file =>
                PathSeparator + Path.GetRelativePath(RootDiskPath, file)
                    .Replace(Path.DirectorySeparatorChar, PathSeparator)) :
            dirs.Select(d => d.Substring(Path.GetDirectoryName(d.TrimEnd(Path.DirectorySeparatorChar)).Length + 1));
    }

    /// <inheritdoc />
    public override void DeleteFile(string path)
        => File.Delete(CreateDiskPath(path));

    /// <inheritdoc />
    public override void DeleteDirectory(string path)
    {
        string diskPath = CreateDiskPath(path);
        if (Path.GetRelativePath(RootDiskPath, diskPath) == ".")
            throw new("Can't delete root directory");

        Directory.Delete(diskPath, true);
    }

    /// <inheritdoc />
    public override bool ExistsFile(string path)
        => File.Exists(CreateDiskPath(path));

    /// <inheritdoc />
    public override bool ExistsDirectory(string path)
        => Directory.Exists(CreateDiskPath(path));

    /// <inheritdoc />
    public override void CreateDirectory(string path)
        => Directory.CreateDirectory(CreateDiskPath(path));

    /// <inheritdoc />
    public override byte[] GetFileData(string path)
        => File.ReadAllBytes(CreateDiskPath(path));

    /// <inheritdoc />
    public override Stream OpenFileReadStream(string path)
        => File.OpenRead(CreateDiskPath(path));

    /// <inheritdoc />
    public override void SetFileData(string path, byte[] data)
        => File.WriteAllBytes(CreateDiskPath(path), data);
}