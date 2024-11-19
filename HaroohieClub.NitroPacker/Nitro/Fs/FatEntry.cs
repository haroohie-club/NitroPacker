using HaroohieClub.NitroPacker.IO;
using HaroohieClub.NitroPacker.IO.Serialization;

namespace HaroohieClub.NitroPacker.Nitro.Fs;

public class FatEntry
{
    public FatEntry()
    {
    }
    
    public FatEntry(uint offset, uint size)
    {
        FileTop = offset;
        FileBottom = offset + size;
    }

    public FatEntry(EndianBinaryReaderEx er)
        => er.ReadObject(this);

    public void Write(EndianBinaryWriterEx er)
        => er.WriteObject(this);

    public uint FileTop { get; set; }
    public uint FileBottom { get; set; }

    [Ignore]
    public uint FileSize => FileBottom - FileTop;
}