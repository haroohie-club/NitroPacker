using HaroohieClub.NitroPacker.IO;

namespace HaroohieClub.NitroPacker.Nitro.Card;

public class NitroFooter
{
    public NitroFooter() { }

    public NitroFooter(EndianBinaryReaderEx er) => er.ReadObject(this);
    public void Write(EndianBinaryWriterEx er) => er.WriteObject(this);

    public uint NitroCode { get; set; }
    public uint _start_ModuleParamsOffset { get; set; }
    public uint Unknown { get; set; }
}