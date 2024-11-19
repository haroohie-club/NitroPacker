using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using HaroohieClub.NitroPacker.IO;
using HaroohieClub.NitroPacker.IO.Serialization;

namespace HaroohieClub.NitroPacker.Nitro.Card;

/// <summary>
/// Representation of the NDS (and DSi) ROM header
/// </summary>
public class RomHeader
{
    /// <summary>
    /// Blank constructor, only used for deserialization. Do not use.
    /// </summary>
    public RomHeader() { }

    /// <summary>
    /// Creates a ROM header given an EndianBinaryReader with an initialized stream
    /// </summary>
    /// <param name="er">The endian binary reader initalized with the ROM stream</param>
    public RomHeader(EndianBinaryReader er)
    {
        GameName = er.ReadString(Encoding.ASCII, 12).TrimEnd('\0');    // 0x00
        GameCode = er.ReadString(Encoding.ASCII, 4).TrimEnd('\0');     // 0x0C
        MakerCode = er.ReadString(Encoding.ASCII, 2).TrimEnd('\0');    // 0x10
        UnitCode = er.Read<UnitCodes>();                                    // 0x12
        EncryptionSeedSelect = er.Read<byte>();                             // 0x13
        DeviceCapacity = er.Read<byte>();                                   // 0x14
        ReservedA = er.Read<byte>(7);                                  // 0x15
        Flags = er.Read<DSiFlags>();                                        // 0x1C
        RegionOrJump = er.Read<RegionOrPermitJump>();                         // 0x1D
        RomVersion = er.Read<byte>();                                       // 0x1E
        AutoStart = er.Read<byte>();                                        // 0x1F

        Arm9RomOffset = er.Read<uint>();                                    // 0x20
        Arm9EntryAddress = er.Read<uint>();                                 // 0x24
        Arm9RamAddress = er.Read<uint>();                                   // 0x28
        Arm9Size = er.Read<uint>();                                         // 0x2C
        Arm7RomOffset = er.Read<uint>();                                    // 0x30
        Arm7EntryAddress = er.Read<uint>();                                 // 0x34
        Arm7RamAddress = er.Read<uint>();                                   // 0x38
        Arm7Size = er.Read<uint>();                                         // 0x3C

        FntOffset = er.Read<uint>();                                        // 0x40
        FntSize = er.Read<uint>();                                          // 0x44

        FatOffset = er.Read<uint>();                                        // 0x48
        FatSize = er.Read<uint>();                                          // 0x4C

        Arm9OvtOffset = er.Read<uint>();                                    // 0x50
        Arm9OvtSize = er.Read<uint>();                                      // 0x54
        
        Arm7OvtOffset = er.Read<uint>();                                    // 0x58
        Arm7OvtSize = er.Read<uint>();                                      // 0x5C

        NormalCommandSettings = er.Read<uint>();                            // 0x60
        Key1CommandSettings = er.Read<uint>();                              // 0x64
        IconTitleOffset = er.Read<uint>();                                  // 0x68
        SecureCRC = er.Read<ushort>();                                      // 0x6C
        SecureAreaDelay = er.Read<ushort>();                                // 0x6E

        Arm9AutoloadHookRamAddress = er.Read<uint>();                       // 0x70
        Arm7AutoloadHookRamAddress = er.Read<uint>();                       // 0x74

        SecureAreaDisable = er.Read<byte>(8);                          // 0x78
        RomSizeExcludingDSiArea = er.Read<uint>();                                          // 0x80
        HeaderSize = er.Read<uint>();                                       // 0x84
        Arm9EntryAddress = er.Read<uint>();                                 // 0x88
        Arm7EntryAddress = er.Read<uint>();                                 // 0x8C
        DSiNTRRomRegionEnd = er.Read<ushort>();                             // 0x90
        DSiTWLRomRegionStart = er.Read<ushort>();                           // 0x92
        ReservedB = er.Read<byte>(0x2C);                               // 0x94

        NintendoLogoData = er.Read<byte>(0x9C);
        LogoCRC = er.Read<ushort>();
        HeaderCRC = er.Read<ushort>();
    }

    /// <summary>
    /// Writes a ROM to disk using an EndianBinaryWriter initialized with a stream
    /// </summary>
    /// <param name="ew">The endian binary writer initialized with a ROM stream</param>
    public void Write(EndianBinaryWriter ew)
    {
        var m = new MemoryStream();
        byte[] header;
        using (var noCrcEw = new EndianBinaryWriter(m, Endianness.LittleEndian))
        {
            noCrcEw.Write(GameName.PadRight(12, '\0')[..12], Encoding.ASCII, false);
            noCrcEw.Write(GameCode.PadRight(4, '\0')[..4], Encoding.ASCII, false);
            noCrcEw.Write(MakerCode.PadRight(2, '\0')[..2], Encoding.ASCII, false);
            noCrcEw.Write(UnitCode);
            noCrcEw.Write(EncryptionSeedSelect);
            noCrcEw.Write(DeviceCapacity);
            noCrcEw.Write(ReservedA, 0, 7);
            noCrcEw.Write(Flags);
            noCrcEw.Write(RegionOrJump);
            noCrcEw.Write(RomVersion);
            noCrcEw.Write(AutoStart);

            noCrcEw.Write(Arm9RomOffset);
            noCrcEw.Write(Arm9EntryAddress);
            noCrcEw.Write(Arm9RamAddress);
            noCrcEw.Write(Arm9Size);
            noCrcEw.Write(Arm7RomOffset);
            noCrcEw.Write(Arm7EntryAddress);
            noCrcEw.Write(Arm7RamAddress);
            noCrcEw.Write(Arm7Size);

            noCrcEw.Write(FntOffset);
            noCrcEw.Write(FntSize);

            noCrcEw.Write(FatOffset);
            noCrcEw.Write(FatSize);

            noCrcEw.Write(Arm9OvtOffset);
            noCrcEw.Write(Arm9OvtSize);

            noCrcEw.Write(Arm7OvtOffset);
            noCrcEw.Write(Arm7OvtSize);

            noCrcEw.Write(NormalCommandSettings);
            noCrcEw.Write(Key1CommandSettings);
            noCrcEw.Write(IconTitleOffset);
            noCrcEw.Write(SecureCRC);
            noCrcEw.Write(SecureAreaDelay);

            noCrcEw.Write(Arm9AutoloadHookRamAddress);
            noCrcEw.Write(Arm7AutoloadHookRamAddress);

            noCrcEw.Write(SecureAreaDisable, 0, 8);
            noCrcEw.Write(RomSizeExcludingDSiArea);
            noCrcEw.Write(HeaderSize);
            noCrcEw.Write(Arm9ParametersTableOffset);
            noCrcEw.Write(Arm7ParametersTableOffset);
            noCrcEw.Write(Arm7ParametersTableOffset);
            noCrcEw.Write(DSiNTRRomRegionEnd);
            noCrcEw.Write(DSiTWLRomRegionStart);
            noCrcEw.Write(ReservedB, 0, 0x2C);

            noCrcEw.Write(NintendoLogoData, 0, 0x9C);
            LogoCRC = Crc16.GetCrc16(NintendoLogoData);
            noCrcEw.Write(LogoCRC);

            header = m.ToArray();
        }

        HeaderCRC = Crc16.GetCrc16(header);

        ew.Write(header);
        ew.Write(HeaderCRC);
    }

    /// <summary>
    /// Unit codes, values indicating the system the ROM was intended to run on
    /// </summary>
    public enum UnitCodes : byte
    {
        /// <summary>
        /// NDS (Nintendo DS)
        /// </summary>
        NDS = 0,
        /// <summary>
        /// NDS+DSi (DSi Enhanced)
        /// </summary>
        NDS_DSi = 2,
        /// <summary>
        /// DSi exclusive
        /// </summary>
        DSi = 3,
    }

    /// <summary>
    /// Flags used only in DSi ROMs (in NDS ROMs, this byte is reserved)
    /// </summary>
    [Flags]
    public enum DSiFlags : byte
    {
        /// <summary>
        /// Reserved marker for NDS games
        /// </summary>
        Reserved = 0b0000_0000, // NDS
        /// <summary>
        /// Must set for DSi titles
        /// </summary>
        HasTwlExclusiveRegion = 0b0000_0001,
        /// <summary>
        /// If set, the ROM is modcrypted
        /// </summary>
        Modcrypted = 0b0000_0010,
        /// <summary>
        /// Clear = Retail, Set = Debug
        /// </summary>
        ModcryptKeySelect = 0b0000_0100,
        /// <summary>
        /// Unknown on gbatek
        /// </summary>
        DisableDebug = 0b0000_1000, // ? on gbatek lol
    }

    /// <summary>
    /// On NDS ROMs, this byte indicates region, while DSi ROMs use it to indicate jump permission
    /// </summary>
    public enum RegionOrPermitJump : byte
    {
        /// <summary>
        /// Normal region / don't jump
        /// </summary>
        Normal = 0x00,
        /// <summary>
        /// System settings (jump)
        /// </summary>
        SystemSettings = 0x01,
        /// <summary>
        /// Korean region NDS ROM
        /// </summary>
        Korea = 0x40,
        /// <summary>
        /// Chinese region NDS ROM
        /// </summary>
        China = 0x80,
    }

    /// <summary>
    /// Game title, always uppercase ASCII
    /// </summary>
    public string GameName { get; set; }  //12
    /// <summary>
    /// Game code, always uppercase ASCII, as seen with NTR-[CODE] on box
    /// </summary>
    public string GameCode { get; set; }  //4
    /// <summary>
    /// Uppercase ASCII, "01" = Nintendo
    /// </summary>
    public string MakerCode { get; set; } //2
    /// <summary>
    /// Unit code indicating the system the ROM was intended to run on
    /// </summary>
    [XmlAttribute("ProductId")]
    public UnitCodes UnitCode { get; set; }
    /// <summary>
    /// only bottom 8 bits are used, usually 0x00
    /// </summary>
    [XmlAttribute("DeviceType")]
    public byte EncryptionSeedSelect { get; set; }
    /// <summary>
    /// The size of the device, represented as a left bitshift value of 128 KiB
    /// e.g., a value of 7 represents 16 MiB
    /// </summary>
    [XmlAttribute("DeviceSize")]
    public byte DeviceCapacity { get; set; }

    private byte[] _reservedA;
    /// <summary>
    /// Reserved
    /// </summary>
    public byte[] ReservedA
    {
        get => _reservedA;
        set
        {
            if (value.Length == 9)
            {
                _reservedA = value[0..6];
                Flags = (DSiFlags)value[7];
                RegionOrJump = (RegionOrPermitJump)value[8];
            }
            else
            {
                _reservedA = value;
            }
        }
    }

    /// <summary>
    /// Flags used only on the DSi to represent certain DSi settings
    /// On NDS titles, this value is always 0 (reserved)
    /// </summary>
    public DSiFlags Flags { get; set; }
    /// <summary>
    /// On NDS ROMs, this byte indicates region, while DSi ROMs use it to indicate jump permission
    /// </summary>
    public RegionOrPermitJump RegionOrJump { get; set; }
   
    /// <summary>
    /// The version of the ROM (usually 0)
    /// </summary>
    [XmlAttribute("GameVersion")]
    public byte RomVersion { get; set; }
    /// <summary>
    /// Auto start; if bit 2 is set, skips "Press Button" after Health and Safety and also skips boot menu
    /// </summary>
    [XmlAttribute("Property")]
    public byte AutoStart { get; set; }

    internal uint Arm9RomOffset { get; set; }
    /// <summary>
    /// Entry address of ARM9 in RAM (0x2000000..0x23BFE00)
    /// </summary>
    [XmlAttribute("MainEntryAddress")]
    public uint Arm9EntryAddress { get; set; }
    /// <summary>
    /// Loading address of ARM9 in RAM (0x2000000..0x23BFE00)
    /// </summary>
    [XmlAttribute("MainRamAddress")]
    public uint Arm9RamAddress { get; set; }
    internal uint Arm9Size { get; set; }

    internal uint Arm7RomOffset { get; set; }
    /// <summary>
    /// ARM7 entry address in RAM (0x2000000..0x23BFE00, or 0x37F8000..0x3807E00)
    /// </summary>
    [XmlAttribute("SubEntryAddress")]
    public uint Arm7EntryAddress { get; set; }
    /// <summary>
    /// ARM7 loading address in RAM (0x2000000..0x23BFE00, or 0x37F8000..0x3807E00)
    /// </summary>
    [XmlAttribute("SubRamAddress")]
    public uint Arm7RamAddress { get; set; }
    internal uint Arm7Size { get; set; }

    internal uint FntOffset { get; set; }
    internal uint FntSize { get; set; }

    internal uint FatOffset { get; set; }
    internal uint FatSize { get; set; }

    internal uint Arm9OvtOffset { get; set; }
    internal uint Arm9OvtSize { get; set; }
    internal uint Arm7OvtOffset { get; set; }
    internal uint Arm7OvtSize { get; set; }

    /// <summary>
    /// Deprecated property for unknown values that are now known to be <see cref="NormalCommandSettings"/> and <see cref="Key1CommandSettings"/>
    /// </summary>
    [ArraySize(8)]
    [JsonIgnore]
    [Obsolete("RomParamA is deprecated, please use NormalCommandSettings and Key1CommandSettings instead")]
    public byte[] RomParamA
    {
        get => [.. BitConverter.GetBytes(NormalCommandSettings).Concat(BitConverter.GetBytes(Key1CommandSettings))];
        set
        {
            NormalCommandSettings = IOUtil.ReadU32Le(value, 0);
            Key1CommandSettings = IOUtil.ReadU32Le(value, 4);
        }
    }
    
    /// <summary>
    /// Port 0x40001A4 setting for normal commands (usually 0x00586000)
    /// </summary>
    [XmlIgnore]
    public uint NormalCommandSettings { get; set; }
    
    /// <summary>
    /// Port 0x40001A4 setting for KEY1 commands (usually 0x001808F8)
    /// </summary>
    [XmlIgnore]
    public uint Key1CommandSettings { get; set; }
    
    internal uint IconTitleOffset { get; set; }

    /// <summary>
    /// Secure Area Checksum, CRC-16 of [[0x020]..0x00007FFF]
    /// </summary>
    public ushort SecureCRC { get; set; }

    /// <summary>
    /// Deprecated property for unknown value that is now known to be the <see cref="SecureAreaDelay"/>
    /// </summary>
    [ArraySize(2)]
    [JsonIgnore]
    [Obsolete("RomParamB is deprecated, please use SecureAreaDelay instead")]
    public byte[] RomParamB { get; set; }
    
    /// <summary>
    /// Secure Area Delay (in 131kHz units) (051Eh=10ms or 0D7Eh=26ms)
    /// </summary>
    [XmlIgnore]
    public ushort SecureAreaDelay { get; set; }

    /// <summary>
    /// ARM9 Auto Load List Hook RAM Address; end address of autoload
    /// </summary>
    [XmlAttribute("MainAutoloadDone")]
    public uint Arm9AutoloadHookRamAddress { get; set; }
    /// <summary>
    /// ARM7 Auto Load List Hook RAM Address; functions
    /// </summary>
    [XmlAttribute("SubAutoloadDone")]
    public uint Arm7AutoloadHookRamAddress { get; set; }

    /// <summary>
    /// Secure Area Disable (by encrypted "NmMdOnly") (usually zero)
    /// </summary>
    [ArraySize(8)]
    [XmlAttribute("RomParamC")]
    public byte[] SecureAreaDisable { get; set; } //8

    internal uint RomSizeExcludingDSiArea { get; set; }

    internal uint HeaderSize { get; set; }
    
    /// <summary>
    /// Unknown on the NDS, and not well-documented on the DSi
    /// </summary>
    public uint Arm9ParametersTableOffset { get; set; }
    /// <summary>
    /// Reserved on the NDS, and not well-documented on the DSi
    /// </summary>
    public uint Arm7ParametersTableOffset { get; set; }
    /// <summary>
    /// Reserved on the NDS, indicates the end of the NTR (regular DS) region of the ROM on DSi
    /// Usually the same as <see cref="DSiTWLRomRegionStart"/>
    /// </summary>
    public ushort DSiNTRRomRegionEnd { get; set; }
    /// <summary>
    /// Reserved on the NDS, indicates the beginning of the TWL (DSi) region of the ROM on the DSi
    /// Usually the same as <see cref="DSiNTRRomRegionEnd"/>
    /// </summary>
    public ushort DSiTWLRomRegionStart { get; set; }

    private byte[] _reservedB;
    /// <summary>
    /// Reserved section of the ROM header
    /// </summary>
    public byte[] ReservedB
    {
        get => _reservedB;
        set
        {
            if (value.Length == 0x38)
            {
                Arm9ParametersTableOffset = IOUtil.ReadU32Le(value, 0x00);
                Arm7ParametersTableOffset = IOUtil.ReadU32Le(value, 0x04);
                DSiNTRRomRegionEnd = IOUtil.ReadU16Le(value, 0x08);
                DSiTWLRomRegionStart = IOUtil.ReadU16Le(value, 0x0A);
                _reservedB = value[0x0C..];
            }
            else
            {
                _reservedB = value;
            }
        }
    }

    /// <summary>
    /// Nintendo Logo (compressed bitmap, same as in GBA Headers)
    /// </summary>
    [ArraySize(0x9C)]
    [XmlAttribute("LogoData")]
    public byte[] NintendoLogoData { get; set; }
    
    [JsonIgnore]
    internal ushort LogoCRC { get; set; }

    [JsonIgnore]
    internal ushort HeaderCRC { get; set; }
}