using HaroohieClub.NitroPacker.IO;

namespace HaroohieClub.NitroPacker.Nitro.Fs;

public class DirectoryTableEntry
{
    public DirectoryTableEntry() { }

    public DirectoryTableEntry(EndianBinaryReaderEx er)
        => er.ReadObject(this);

    public void Write(EndianBinaryWriterEx er)
        => er.WriteObject(this);

    public uint EntryStart;
    public ushort EntryFileId;
    public ushort ParentId;
}