using HaroohieClub.NitroPacker.IO;

namespace HaroohieClub.NitroPacker.Nitro.Fs;

/// <summary>
/// Represents a directory table entry within the file name table
/// </summary>
public class DirectoryTableEntry
{
    /// <summary>
    /// Empty constructor, used for serialization
    /// </summary>
    public DirectoryTableEntry() { }

    /// <summary>
    /// Constructs a directory table entry from a stream using an extended endian binary reader
    /// </summary>
    /// <param name="er"><see cref="EndianBinaryReaderEx"/> initialized with a stream</param>
    public DirectoryTableEntry(EndianBinaryReaderEx er)
        => er.ReadObject(this);

    /// <summary>
    /// Writes the directory table entry's binary representation to a stream using an extended endian binary writer
    /// </summary>
    /// <param name="ew"><see cref="EndianBinaryWriterEx"/> initialized with a stream</param>
    public void Write(EndianBinaryWriterEx ew)
        => ew.WriteObject(this);

    /// <summary>
    /// Offset to associated name table
    /// </summary>
    public uint EntryStart { get; set; }
    /// <summary>
    /// The first file ID of the directory entry
    /// </summary>
    public ushort EntryFileId { get; set; }
    /// <summary>
    /// The directory ID of this directory's parent directory (or, for the root directory, the number of total entries in the directory table)
    /// </summary>
    public ushort ParentId { get; set; }
}