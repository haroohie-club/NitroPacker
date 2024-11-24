using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HaroohieClub.NitroPacker.IO.Archive;

/// <summary>
/// A generic archive
/// </summary>
public abstract class Archive : IReadOnlyArchive
{
    /// <summary>
    /// The path separator for the archive (usually '/' but '\' on windows)
    /// </summary>
    public const char PathSeparator = '/';
    /// <summary>
    /// The root path of the archive
    /// </summary>
    public const string RootPath = "/";

    /// <summary>
    /// If true, the archive is read-only
    /// </summary>
    public virtual bool IsReadOnly => true;

    /// <inheritdoc />
    public abstract IEnumerable<string> EnumerateFiles(string path, bool fullPath);

    /// <inheritdoc />
    public abstract IEnumerable<string> EnumerateDirectories(string path, bool fullPath);

    /// <summary>
    /// Deletes a file from the archive
    /// </summary>
    /// <param name="path">The path to the file to delete</param>
    /// <exception cref="NotSupportedException">Throws if the archive is read-only</exception>
    public virtual void DeleteFile(string path) => throw new NotSupportedException();
    /// <summary>
    /// Deletes a directory from the archive
    /// </summary>
    /// <param name="path">The path to the directory to delete</param>
    /// <exception cref="NotSupportedException">Throws if the archive is read-only</exception>
    public virtual void DeleteDirectory(string path) => throw new NotSupportedException();

    /// <inheritdoc />
    public abstract bool ExistsFile(string path);

    /// <inheritdoc />
    public abstract bool ExistsDirectory(string path);

    /// <summary>
    /// Creates a directory in the archive
    /// </summary>
    /// <param name="path">The path to the directory to create</param>
    /// <exception cref="NotSupportedException">Throws if the archive is read-only</exception>
    public virtual void CreateDirectory(string path) => throw new NotSupportedException();

    /// <summary>
    /// Gets a file's contents
    /// </summary>
    /// <param name="path">The path to the file</param>
    /// <returns>The file's contents as a byte array</returns>
    public abstract byte[] GetFileData(string path);

    /// <summary>
    /// Gets a file's contents as a read-only span
    /// </summary>
    /// <param name="path">The path to the file</param>
    /// <returns>A <see cref="ReadOnlySpan{T}"/> of bytes of the file's data</returns>
    public ReadOnlySpan<byte> GetFileDataSpan(string path)
        => GetFileData(path);

    /// <inheritdoc />
    public abstract Stream OpenFileReadStream(string path);
    
    /// <summary>
    /// Sets a file's data
    /// </summary>
    /// <param name="path">The path to the file</param>
    /// <param name="data">The data to set</param>
    /// <exception cref="NotSupportedException">Throws in archives which are read-only</exception>
    public virtual void SetFileData(string path, byte[] data) => throw new NotSupportedException();

    /// <summary>
    /// Joins multiple paths together with the path separator
    /// </summary>
    /// <param name="parts">The various parts of the path</param>
    /// <returns>A properly joined path</returns>
    public static string JoinPath(params string[] parts)
        => JoinPath((IEnumerable<string>)parts);

    /// <summary>
    /// Joins multiple paths together with the path separator
    /// </summary>
    /// <param name="parts">The various parts of the path</param>
    /// <returns>A properly joined path</returns>
    public static string JoinPath(IEnumerable<string> parts)
        => PathSeparator + string.Join(PathSeparator, parts.Select(p => p.Trim(PathSeparator)).Where(p => p != ""));

    /// <summary>
    /// Normalizes a path for consistency
    /// </summary>
    /// <param name="path">The path to normalize</param>
    /// <returns>A normalized version of that path</returns>
    /// <exception cref="ArgumentException">Thrown if an invalid path is specified</exception>
    public static string NormalizePath(string path)
    {
        path = path.Trim(PathSeparator);
        string[] parts = path.Split(PathSeparator);

        var newPath = new Stack<string>();
        foreach (string part in parts)
        {
            if (part == ".")
                continue;

            if (part == "..")
            {
                if (newPath.Count == 0)
                    throw new ArgumentException("Invalid path specified");
                newPath.Pop();
                continue;
            }

            newPath.Push(part);
        }

        return JoinPath(newPath.Reverse());
    }

    /// <summary>
    /// Checks to see if two paths are equivalent
    /// </summary>
    /// <param name="path1">The first path</param>
    /// <param name="path2">The second path</param>
    /// <returns>True if they are equal, false if not</returns>
    public static bool PathEqual(string path1, string path2)
        => NormalizePath(path1) == NormalizePath(path2);
}