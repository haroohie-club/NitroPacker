using HaroohieClub.NitroPacker.IO;

namespace HaroohieClub.NitroPacker.Nitro.Fs;

public class DirectoryTableEntry
{
    public DirectoryTableEntry() { }

    public DirectoryTableEntry(EndianBinaryReaderEx er)
        => er.ReadObject(this);

    public void Write(EndianBinaryWriterEx er)
        => er.WriteObject(this);

    public uint EntryStart { get; set; }
    public ushort EntryFileId { get; set; }
    public ushort ParentId { get; set; }
}