using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using HaroohieClub.NitroPacker.IO;
using HaroohieClub.NitroPacker.IO.Serialization;

namespace HaroohieClub.NitroPacker.Nitro.Card;

public class RomHeader
{
    public RomHeader() { }

    public RomHeader(EndianBinaryReader er)
    {
        GameName = er.ReadString(Encoding.ASCII, 12).TrimEnd('\0');     // 0x00
        GameCode = er.ReadString(Encoding.ASCII, 4).TrimEnd('\0');      // 0x0C
        MakerCode = er.ReadString(Encoding.ASCII, 2).TrimEnd('\0');     // 0x10
        UnitCode = er.Read<byte>();
        DeviceType = er.Read<byte>();
        DeviceSize = er.Read<byte>();
        ReservedA = er.Read<byte>(9);
        GameVersion = er.Read<byte>();
        Property = er.Read<byte>();

        MainRomOffset = er.Read<uint>();
        MainEntryAddress = er.Read<uint>();
        MainRamAddress = er.Read<uint>();
        MainSize = er.Read<uint>();
        SubRomOffset = er.Read<uint>();
        SubEntryAddress = er.Read<uint>();
        SubRamAddress = er.Read<uint>();
        SubSize = er.Read<uint>();

        FntOffset = er.Read<uint>();
        FntSize = er.Read<uint>();

        FatOffset = er.Read<uint>();
        FatSize = er.Read<uint>();

        MainOvtOffset = er.Read<uint>();
        MainOvtSize = er.Read<uint>();

        SubOvtOffset = er.Read<uint>();
        SubOvtSize = er.Read<uint>();

        RomParamA = er.Read<byte>(8);
        BannerOffset = er.Read<uint>();
        SecureCRC = er.Read<ushort>();
        RomParamB = er.Read<byte>(2);

        MainAutoloadDone = er.Read<uint>();
        SubAutoloadDone = er.Read<uint>();

        RomParamC = er.Read<byte>(8);
        RomSize = er.Read<uint>();
        HeaderSize = er.Read<uint>();
        ReservedB = er.Read<byte>(0x38);

        LogoData = er.Read<byte>(0x9C);
        LogoCRC = er.Read<ushort>();
        HeaderCRC = er.Read<ushort>();
    }

    public void Write(EndianBinaryWriter er)
    {
        var m = new MemoryStream();
        byte[] header;
        using (var ew = new EndianBinaryWriter(m, Endianness.LittleEndian))
        {
            ew.Write(GameName.PadRight(12, '\0')[..12], Encoding.ASCII, false);
            ew.Write(GameCode.PadRight(4, '\0')[..4], Encoding.ASCII, false);
            ew.Write(MakerCode.PadRight(2, '\0')[..2], Encoding.ASCII, false);
            ew.Write(UnitCode);
            ew.Write(DeviceType);
            ew.Write(DeviceSize);
            ew.Write(ReservedA, 0, 9);
            ew.Write(GameVersion);
            ew.Write(Property);

            ew.Write(MainRomOffset);
            ew.Write(MainEntryAddress);
            ew.Write(MainRamAddress);
            ew.Write(MainSize);
            ew.Write(SubRomOffset);
            ew.Write(SubEntryAddress);
            ew.Write(SubRamAddress);
            ew.Write(SubSize);

            ew.Write(FntOffset);
            ew.Write(FntSize);

            ew.Write(FatOffset);
            ew.Write(FatSize);

            ew.Write(MainOvtOffset);
            ew.Write(MainOvtSize);

            ew.Write(SubOvtOffset);
            ew.Write(SubOvtSize);

            ew.Write(RomParamA, 0, 8);
            ew.Write(BannerOffset);
            ew.Write(SecureCRC);
            ew.Write(RomParamB, 0, 2);

            ew.Write(MainAutoloadDone);
            ew.Write(SubAutoloadDone);

            ew.Write(RomParamC, 0, 8);
            ew.Write(RomSize);
            ew.Write(HeaderSize);
            ew.Write(ReservedB, 0, 0x38);

            ew.Write(LogoData, 0, 0x9C);
            LogoCRC = Crc16.GetCrc16(LogoData);
            ew.Write(LogoCRC);

            header = m.ToArray();
        }

        HeaderCRC = Crc16.GetCrc16(header);

        er.Write(header);
        er.Write(HeaderCRC);
    }

    public enum UnitCodes
    {
        NDS = 0,
        NDS_DSi = 2,
        DSi = 3,
    }

    public string GameName { get; set; }  //12
    public string GameCode { get; set; }  //4
    public string MakerCode { get; set; } //2
    [XmlAttribute("ProductId")]
    public byte UnitCode { get; set; }
    public byte DeviceType { get; set; }
    public byte DeviceSize { get; set; }

    [ArraySize(9)]
    public byte[] ReservedA { get; set; }

    public byte GameVersion { get; set; }
    public byte Property { get; set; }

    [JsonIgnore]
    public uint MainRomOffset { get; set; }

    public uint MainEntryAddress { get; set; }
    public uint MainRamAddress { get; set; }

    [JsonIgnore]
    public uint MainSize { get; set; }

    [JsonIgnore]
    public uint SubRomOffset { get; set; }

    public uint SubEntryAddress { get; set; }
    public uint SubRamAddress { get; set; }

    [JsonIgnore]
    public uint SubSize { get; set; }

    [JsonIgnore]
    public uint FntOffset { get; set; }

    [JsonIgnore]
    public uint FntSize { get; set; }

    [JsonIgnore]
    public uint FatOffset { get; set; }

    [JsonIgnore]
    public uint FatSize { get; set; }

    [JsonIgnore]
    public uint MainOvtOffset { get; set; }

    [JsonIgnore]
    public uint MainOvtSize { get; set; }

    [JsonIgnore]
    public uint SubOvtOffset { get; set; }

    [JsonIgnore]
    public uint SubOvtSize { get; set; }

    [ArraySize(8)]
    public byte[] RomParamA { get; set; }

    [JsonIgnore]
    public uint BannerOffset { get; set; }

    public ushort SecureCRC { get; set; }

    [ArraySize(2)]
    public byte[] RomParamB { get; set; }

    public uint MainAutoloadDone { get; set; }
    public uint SubAutoloadDone { get; set; }

    [ArraySize(8)]
    public byte[] RomParamC { get; set; } //8

    [JsonIgnore]
    public uint RomSize { get; set; }

    [JsonIgnore]
    public uint HeaderSize { get; set; }
    
    [ArraySize(0x38)]
    public byte[] ReservedB { get; set; }

    [ArraySize(0x9C)]
    public byte[] LogoData { get; set; }

    [JsonIgnore]
    public ushort LogoCRC { get; set; }

    [JsonIgnore]
    public ushort HeaderCRC { get; set; }
}