using System;
using System.Data;
using System.Text;
using HaroohieClub.NitroPacker.IO;

namespace HaroohieClub.NitroPacker.Nitro.Fs;

/// <summary>
/// Enum describing the types of name table entries
/// </summary>
public enum NameTableEntryType
{
    /// <summary>
    /// Used to mark the end of a directory
    /// </summary>
    EndOfDirectory,
    /// <summary>
    /// Indicates that this entry names a file
    /// </summary>
    File,
    /// <summary>
    /// Indicates that this entry names a directory
    /// </summary>
    Directory,
}

/// <summary>
/// Represents an entry in one of the name (or sub) tables in the file name table
/// </summary>
public class NameTableEntry
{
    /// <summary>
    /// The type of entry (file, directory, or end)
    /// </summary>
    public NameTableEntryType Type { get; set; }
    /// <summary>
    /// The name of this entry as a Shift-JIS string
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// If a directory, the ID of the directory; otherwise, 0
    /// </summary>
    public ushort DirectoryId { get; set; }

    /// <summary>
    /// Empty constructor, used for serialization
    /// </summary>
    public NameTableEntry()
    {
    }
    
    /// <summary>
    /// Constructs a name table entry from the given arguments
    /// </summary>
    /// <param name="type">The type of the entry</param>
    /// <param name="name">The name of the entry</param>
    /// <param name="directoryId">The directory ID</param>
    /// <exception cref="ArgumentNullException">Thrown if name is null and this is not an end of directory</exception>
    /// <exception cref="ArgumentException">Thrown if the name is too long or if the directory ID is invalid</exception>
    private NameTableEntry(NameTableEntryType type, string name = null, ushort directoryId = 0)
    {
        Type = type;
        if (type != NameTableEntryType.EndOfDirectory)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (name.Length == 0 || name.Length > 0x7F)
                throw new ArgumentException("Name length invalid", nameof(name));
            Name = name;
        }

        if (type == NameTableEntryType.Directory)
        {
            if (directoryId < 0xF000)
                throw new ArgumentException("Directory ID invalid", nameof(directoryId));
            DirectoryId = directoryId;
        }
    }

    /// <summary>
    /// Constructs a name table entry from a stream using an endian binary reader
    /// </summary>
    /// <param name="er"><see cref="EndianBinaryReader"/> initialized with a stream</param>
    public NameTableEntry(EndianBinaryReader er)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        byte length = er.Read<byte>();
        if (length == 0)
            Type = NameTableEntryType.EndOfDirectory;
        else if ((length & 0x80) != 0)
        {
            Type = NameTableEntryType.Directory;
            Name = er.ReadString(Encoding.GetEncoding("Shift-JIS"), length & ~0x80);
            DirectoryId = er.Read<ushort>();
        }
        else
        {
            Type = NameTableEntryType.File;
            Name = er.ReadString(Encoding.GetEncoding("Shift-JIS"), length);
        }
    }

    /// <summary>
    /// Writes a name table entry to a stream using an endian binary writer
    /// </summary>
    /// <param name="er"><see cref="EndianBinaryWriter"/> initialized with a stream</param>
    /// <exception cref="DataException">Thrown if the name is too long</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if an invalid entry type is specified</exception>
    public void Write(EndianBinaryWriter er)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        switch (Type)
        {
            case NameTableEntryType.EndOfDirectory:
                er.Write((byte)0);
                break;
            case NameTableEntryType.File:
                if (Name.Length > 0x7F)
                    throw new DataException($"File name '{Name}' too long");
                er.Write((byte)Name.Length);
                er.Write(Name, Encoding.GetEncoding("Shift-JIS"), false);
                break;
            case NameTableEntryType.Directory:
                if (Name.Length > 0x7F)
                    throw new DataException($"Directory name '{Name}' too long");
                er.Write((byte)(Name.Length | 0x80));
                er.Write(Name, Encoding.GetEncoding("Shift-JIS"), false);
                er.Write(DirectoryId);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    /// Constructs an end of directory entry
    /// </summary>
    /// <returns>An end of directory name table entry</returns>
    public static NameTableEntry EndOfDirectory() => new(NameTableEntryType.EndOfDirectory);
    /// <summary>
    /// Constructs a file entry
    /// </summary>
    /// <param name="name">The name of the file</param>
    /// <returns>A file name table entry</returns>
    public static NameTableEntry File(string name) => new(NameTableEntryType.File, name);
    /// <summary>
    /// Constructs a directory entry
    /// </summary>
    /// <param name="name">The name of the directory</param>
    /// <param name="directoryId">The ID of the directory</param>
    /// <returns>A directory name table entry</returns>
    public static NameTableEntry Directory(string name, ushort directoryId) =>
        new(NameTableEntryType.Directory, name, directoryId);
}