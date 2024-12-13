using System;
using System.Collections.Generic;
using System.IO;

namespace HaroohieClub.NitroPacker.IO.Archive;

/// <summary>
/// Interface for read-only archives
/// </summary>
public interface IReadOnlyArchive
{
    /// <summary>
    /// Enumerates the files in a directory in the archive
    /// </summary>
    /// <param name="path">Path to the directory</param>
    /// <param name="fullPath">If true, returns the full path of each file; if false, returns only the file name</param>
    /// <returns>An enumerable of all the files in the specified directory</returns>
    IEnumerable<string> EnumerateFiles(string path, bool fullPath);
    /// <summary>
    /// Enumerates the subdirectories in a directory in the archive
    /// </summary>
    /// <param name="path">Path to the directory</param>
    /// <param name="fullPath">If true, returns the full path of each directory; if false, returns only the directory name</param>
    /// <returns>An enumerable of all the subdirectories in the specified directory</returns>
    IEnumerable<string> EnumerateDirectories(string path, bool fullPath);

    /// <summary>
    /// Checks if a file exists
    /// </summary>
    /// <param name="path">The path to the file</param>
    /// <returns>True if the file exists, false if it doesn't</returns>
    bool ExistsFile(string path);
    /// <summary>
    /// Checks if a subdirectory exists
    /// </summary>
    /// <param name="path">The path to the directory</param>
    /// <returns>True if the subdirectory exists, false if it doesn't</returns>
    bool ExistsDirectory(string path);

    /// <summary>
    /// Gets a file's data as a span of bytes
    /// </summary>
    /// <param name="path">The path to the file</param>
    /// <returns>A <see cref="ReadOnlySpan{T}"/> of bytes containing the file data</returns>
    ReadOnlySpan<byte> GetFileDataSpan(string path);
    /// <summary>
    /// Opens a stream to a particular file
    /// </summary>
    /// <param name="path">The path to the file</param>
    /// <returns>A read-only stream for that file's data</returns>
    Stream OpenFileReadStream(string path);
}