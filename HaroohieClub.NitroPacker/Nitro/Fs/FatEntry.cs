using HaroohieClub.NitroPacker.IO;
using HaroohieClub.NitroPacker.IO.Serialization;

namespace HaroohieClub.NitroPacker.Nitro.Fs;

/// <summary>
/// Represents an entry in the file allocation table
/// </summary>
public class FatEntry
{
    /// <summary>
    /// Constructs an empty FAT entry (used for serialization)
    /// </summary>
    public FatEntry()
    {
    }
    
    /// <summary>
    /// Constructs a FAT entry from a file offset and size
    /// </summary>
    /// <param name="offset">The offset of the file in the ROM</param>
    /// <param name="size">The size of the file</param>
    public FatEntry(uint offset, uint size)
    {
        FileTop = offset;
        FileBottom = offset + size;
    }

    /// <summary>
    /// Reads a FAT entry from an extended endian binary reader
    /// </summary>
    /// <param name="er">The extended endian binary reader with an initialized stream</param>
    public FatEntry(EndianBinaryReaderEx er)
        => er.ReadObject(this);

    /// <summary>
    /// Writes a FAT entry to a stream using an extended endian binary writer
    /// </summary>
    /// <param name="ew">The extended endian binary writer with an initialized stream</param>
    public void Write(EndianBinaryWriterEx ew)
        => ew.WriteObject(this);

    /// <summary>
    /// The start offset of the file in the ROM
    /// </summary>
    public uint FileTop { get; set; }
    /// <summary>
    /// The end offset of the file in the ROM
    /// </summary>
    public uint FileBottom { get; set; }

    /// <summary>
    /// The size of the file
    /// </summary>
    [Ignore]
    public uint FileSize => FileBottom - FileTop;
}