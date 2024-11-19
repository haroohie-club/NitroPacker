using System;
using System.Text.Json.Serialization;
using HaroohieClub.NitroPacker.IO;
using HaroohieClub.NitroPacker.IO.Serialization;

namespace HaroohieClub.NitroPacker.Nitro.Card;

public class RomOVT
{
    [Flags]
    public enum OVTFlag : byte
    {
        Compressed = 1,
        AuthenticationCode = 2
    }

    public RomOVT() { }

    public RomOVT(EndianBinaryReaderEx er)
    {
        er.ReadObject(this);
        uint tmp = er.Read<uint>();
        Compressed = tmp & 0xFFFFFF;
        Flag = (OVTFlag)(tmp >> 24);
    }

    public void Write(EndianBinaryWriterEx er)
    {
        er.WriteObject(this);
        er.Write(((uint)Flag & 0xFF) << 24 | Compressed & 0xFFFFFF);
    }

    public uint Id { get; set; }

    public uint RamAddress { get; set; }
    public uint RamSize { get; set; }
    public uint BssSize { get; set; }
    public uint SinitInit { get; set; }
    public uint SinitInitEnd { get; set; }

    [JsonIgnore]
    public uint FileId { get; set; }

    [Ignore]
    public uint Compressed { get; set; } //:24;

    [Ignore]
    public OVTFlag Flag { get; set; } // :8;
}