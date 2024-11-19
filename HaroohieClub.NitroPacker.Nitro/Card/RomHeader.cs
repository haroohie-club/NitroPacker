using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using HaroohieClub.NitroPacker.IO;
using HaroohieClub.NitroPacker.IO.Serialization;
using HaroohieClub.NitroPacker.Nitro.Card.Twl;

namespace HaroohieClub.NitroPacker.Nitro.Card;

/// <summary>
/// Representation of the NDS (and DSi) ROM header
/// </summary>
public class RomHeader
{
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
    internal ushort NTRHeaderCRC { get; set; }

    /// <summary>
    /// The DSi portion of the ROM header
    /// </summary>
    public TwlHeader DSiHeader { get; set; } = new();

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
        RegionOrJump = er.Read<RegionOrPermitJump>();                       // 0x1D
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
        RomSizeExcludingDSiArea = er.Read<uint>();                          // 0x80
        HeaderSize = er.Read<uint>();                                       // 0x84
        Arm9ParametersTableOffset = er.Read<uint>();                        // 0x88
        Arm7ParametersTableOffset = er.Read<uint>();                        // 0x8C
        DSiNTRRomRegionEnd = er.Read<ushort>();                             // 0x90
        DSiTWLRomRegionStart = er.Read<ushort>();                           // 0x92
        ReservedB = er.Read<byte>(0x2C);                               // 0x94

        NintendoLogoData = er.Read<byte>(0x9C);                        // 0xC0
        LogoCRC = er.Read<ushort>();                                        // 0x15C
        NTRHeaderCRC = er.Read<ushort>();                                   // 0x15E
        
        er.Skip(0x20);                                              // 0x160
        DSiHeader.GlobalWramSlots = er.Read<byte>(0x14);               // 0x180
        DSiHeader.Arm9WramAreas = er.Read<byte>(0x0C);                 // 0x194
        DSiHeader.Arm7WramAreas = er.Read<byte>(0x0C);                 // 0x1A0
        DSiHeader.GlobalWramSlotWriteProtect = er.Read<byte>(0x03);    // 0x1AC
        DSiHeader.GlobalWRAMCNTSetting = er.Read<byte>();                   // 0x1AF
        DSiHeader.DsiRegionFlags = er.Read<DsiRegionFlag>();                // 0x1B0
        DSiHeader.AccessControl = er.Read<uint>();                          // 0x1B4
        DSiHeader.Arm7ScfgExt7Setting = er.Read<uint>();                    // 0x1B8
        DSiHeader.ReservedC = er.Read<byte>(3);                        // 0x1BC
        DSiHeader.DsiExtraFlags = er.Read<DsiExtraFlag>();                  // 0x1BF
        
        DSiHeader.Arm9iRomOffset = er.Read<uint>();                         // 0x1C0
        DSiHeader.ReservedD = er.Read<uint>();                              // 0x1C4
        DSiHeader.Arm9iRamLoadAddress = er.Read<uint>();                    // 0x1C8
        DSiHeader.Arm9iSize = er.Read<uint>();                              // 0x1CC
        DSiHeader.Arm7iRomOffset = er.Read<uint>();                         // 0x1D0
        DSiHeader.SdMmcDeviceListArm7RamAddress = er.Read<uint>();          // 0x1D4
        DSiHeader.Arm7iRamLoadAddress = er.Read<uint>();                    // 0x1D8
        DSiHeader.Arm7iSize = er.Read<uint>();                              // 0x1DC

        DSiHeader.DigestNTRRegionOffset = er.Read<uint>();                  // 0x1E0
        DSiHeader.DigestNTRRegionLength = er.Read<uint>();                  // 0x1E4
        DSiHeader.DigestTWLRegionOffset = er.Read<uint>();                  // 0x1E8
        DSiHeader.DigestTWLRegionLength = er.Read<uint>();                  // 0x1EC
        DSiHeader.DigestSectorHashtableOffset = er.Read<uint>();            // 0x1F0
        DSiHeader.DigestSectorHashtableLength = er.Read<uint>();            // 0x1F4
        DSiHeader.DigestBlockHashtableOffset = er.Read<uint>();             // 0x1F8
        DSiHeader.DigestBlockHashtableLength = er.Read<uint>();             // 0x1FC
        DSiHeader.DigestSectorSize = er.Read<uint>();                       // 0x200
        DSiHeader.DigestBlockSectorCount = er.Read<uint>();                 // 0x204
        
        DSiHeader.IconTitleSize = er.Read<uint>();                          // 0x208
        DSiHeader.Shared20000Size = er.Read<byte>();                        // 0x20C
        DSiHeader.Shared20001Size = er.Read<byte>();                        // 0x20D
        DSiHeader.EulaVersion = er.Read<byte>();                            // 0x20E
        DSiHeader.UseRatings = er.Read<byte>();                             // 0x20F
        DSiHeader.TotalRomSizeIncludingDSiArea = er.Read<uint>();           // 0x210
        DSiHeader.Shared20002Size = er.Read<byte>();                        // 0x214
        DSiHeader.Shared20003Size = er.Read<byte>();                        // 0x215
        DSiHeader.Shared20004Size = er.Read<byte>();                        // 0x216
        DSiHeader.Shared20005Size = er.Read<byte>();                        // 0x217
        DSiHeader.Arm9iParametersTableOffset = er.Read<uint>();             // 0x218
        DSiHeader.Arm7iParametersTableOffset = er.Read<uint>();             // 0x21C
        
        DSiHeader.ModcryptArea1Offset = er.Read<uint>();                    // 0x220
        DSiHeader.ModcryptArea1Size = er.Read<uint>();                      // 0x224
        DSiHeader.ModcryptArea2Offset = er.Read<uint>();                    // 0x228
        DSiHeader.ModcryptArea2Size = er.Read<uint>();                      // 0x22C
        
        DSiHeader.TitleIdEmag = er.Read<uint>();                            // 0x230
        DSiHeader.TitleIdFiletype = er.Read<TitleIdType>();                 // 0x234
        DSiHeader.TitleIdZeroA = er.Read<byte>();                           // 0x235
        DSiHeader.TitleIdThree = er.Read<byte>();                           // 0x236
        DSiHeader.TitleIdZeroB = er.Read<byte>();                           // 0x237
        DSiHeader.PublicSavSize = er.Read<uint>();                          // 0x238
        DSiHeader.PrivateSavSize = er.Read<uint>();                         // 0x23C
        DSiHeader.ReservedE = er.Read<byte>(0xB0);                     // 0x240

        DSiHeader.JapanRegionRating = new(er.Read<byte>());                 // 0x2F0
        DSiHeader.USCanadaRegionRating = new(er.Read<byte>());              // 0x2F1
        DSiHeader.ReservedRatingA = er.Read<byte>();                        // 0x2F2
        DSiHeader.GermanyRegionRating = new(er.Read<byte>());               // 0x2F3
        DSiHeader.EuropeanRegionRating = new(er.Read<byte>());              // 0x2F4
        DSiHeader.ReserveRatingB = er.Read<byte>();                         // 0x2F5
        DSiHeader.PortugalRegionRating = new(er.Read<byte>());              // 0x2F6
        DSiHeader.UnitedKingdomRegionRating = new(er.Read<byte>());         // 0x2F7
        DSiHeader.AustraliaRegionRating = new(er.Read<byte>());             // 0x2F8
        DSiHeader.SouthKoreaRegionRating = new(er.Read<byte>());            // 0x2F9
        DSiHeader.ReservedRatingsC = er.Read<byte>(6);                 // 0x2FA
        
        DSiHeader.Crypto.Arm9WithSecureAreaSha1HmacHash = er.Read<byte>(20); // 0x300
        DSiHeader.Crypto.Arm7Sha1HmacHash = er.Read<byte>(20);         // 0x314
        DSiHeader.Crypto.DigestMasterSha1HmacHash = er.Read<byte>(20); // 0x328
        DSiHeader.Crypto.IconTitleSha1HmacHash = er.Read<byte>(20);    // 0x33C
        DSiHeader.Crypto.Arm9iDecryptedSha1HmacHash = er.Read<byte>(20); // 0x350
        DSiHeader.Crypto.Arm7iDecryptedSha1HmacHash = er.Read<byte>(20); // 0x364
        DSiHeader.Crypto.ReservedA = er.Read<byte>(20);                 // 0x378
        DSiHeader.Crypto.ReservedB = er.Read<byte>(20);                 // 0x38C
        DSiHeader.Crypto.Arm9WithoutSecureAreaSha1HmacHash = er.Read<byte>(20); // 0x3A0
        DSiHeader.Crypto.ReservedC = er.Read<byte>(0xA4C);              // 0x3B4
        DSiHeader.Crypto.ReservedD = er.Read<byte>(0x180);              // 0x3E0
        DSiHeader.Crypto.RsaSha1Signature = er.Read<byte>(0x80);        // 0xF80
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
            noCrcEw.Write(DSiNTRRomRegionEnd);
            noCrcEw.Write(DSiTWLRomRegionStart);
            noCrcEw.Write(ReservedB, 0, 0x2C);

            noCrcEw.Write(NintendoLogoData, 0, 0x9C);
            LogoCRC = Crc16.GetCrc16(NintendoLogoData);
            noCrcEw.Write(LogoCRC);

            header = m.ToArray();
        }

        NTRHeaderCRC = Crc16.GetCrc16(header);

        ew.Write(header);
        ew.Write(NTRHeaderCRC);
        
        // TWL-only settings here
        ew.Skip(0x48);
        ew.Write(DSiHeader.GlobalWramSlots);
        ew.Write(DSiHeader.Arm9WramAreas);
        ew.Write(DSiHeader.Arm7WramAreas);
        ew.Write(DSiHeader.GlobalWramSlotWriteProtect);
        ew.Write(DSiHeader.GlobalWRAMCNTSetting);
        ew.Write(DSiHeader.DsiRegionFlags);
        ew.Write(DSiHeader.AccessControl);
        ew.Write(DSiHeader.Arm7ScfgExt7Setting);
        ew.Write(DSiHeader.ReservedC);
        ew.Write(DSiHeader.DsiExtraFlags);
        
        ew.Write(DSiHeader.Arm9iRomOffset);
        ew.Write(DSiHeader.ReservedD);
        ew.Write(DSiHeader.Arm9iRamLoadAddress);
        ew.Write(DSiHeader.Arm9iSize);
        ew.Write(DSiHeader.Arm7iRomOffset);
        ew.Write(DSiHeader.SdMmcDeviceListArm7RamAddress);
        ew.Write(DSiHeader.Arm7iRamLoadAddress);
        ew.Write(DSiHeader.Arm7iSize);
        
        ew.Write(DSiHeader.DigestNTRRegionOffset);
        ew.Write(DSiHeader.DigestNTRRegionLength);
        ew.Write(DSiHeader.DigestTWLRegionOffset);
        ew.Write(DSiHeader.DigestTWLRegionLength);
        ew.Write(DSiHeader.DigestSectorHashtableOffset);
        ew.Write(DSiHeader.DigestSectorHashtableLength);
        ew.Write(DSiHeader.DigestBlockHashtableOffset);
        ew.Write(DSiHeader.DigestBlockHashtableLength);
        ew.Write(DSiHeader.DigestSectorSize);
        ew.Write(DSiHeader.DigestBlockSectorCount);
        
        ew.Write(DSiHeader.IconTitleSize);
        ew.Write(DSiHeader.Shared20000Size);
        ew.Write(DSiHeader.Shared20001Size);
        ew.Write(DSiHeader.EulaVersion);
        ew.Write(DSiHeader.UseRatings);
        ew.Write(DSiHeader.TotalRomSizeIncludingDSiArea);
        ew.Write(DSiHeader.Shared20002Size);
        ew.Write(DSiHeader.Shared20003Size);
        ew.Write(DSiHeader.Shared20004Size);
        ew.Write(DSiHeader.Shared20005Size);
        ew.Write(DSiHeader.Arm9iParametersTableOffset);
        ew.Write(DSiHeader.Arm7iParametersTableOffset);
        
        ew.Write(DSiHeader.ModcryptArea1Offset);
        ew.Write(DSiHeader.ModcryptArea1Size);
        ew.Write(DSiHeader.ModcryptArea2Offset);
        ew.Write(DSiHeader.ModcryptArea2Size);
        
        ew.Write(DSiHeader.TitleIdEmag);
        ew.Write(DSiHeader.TitleIdFiletype);
        ew.Write(DSiHeader.TitleIdZeroA);
        ew.Write(DSiHeader.TitleIdThree);
        ew.Write(DSiHeader.TitleIdZeroB);
        ew.Write(DSiHeader.PublicSavSize);
        ew.Write(DSiHeader.PrivateSavSize);
        ew.Write(DSiHeader.ReservedE);
        
        ew.Write(DSiHeader.JapanRegionRating.Pack());
        ew.Write(DSiHeader.USCanadaRegionRating.Pack());
        ew.Write(DSiHeader.ReservedRatingA);
        ew.Write(DSiHeader.GermanyRegionRating.Pack());
        ew.Write(DSiHeader.EuropeanRegionRating.Pack());
        ew.Write(DSiHeader.ReserveRatingB);
        ew.Write(DSiHeader.PortugalRegionRating.Pack());
        ew.Write(DSiHeader.UnitedKingdomRegionRating.Pack());
        ew.Write(DSiHeader.AustraliaRegionRating.Pack());
        ew.Write(DSiHeader.SouthKoreaRegionRating.Pack());
        ew.Write(DSiHeader.ReservedRatingsC);

        ew.Write(DSiHeader.Crypto.Arm9WithSecureAreaSha1HmacHash);
        ew.Write(DSiHeader.Crypto.Arm7Sha1HmacHash);
        ew.Write(DSiHeader.Crypto.DigestMasterSha1HmacHash);
        ew.Write(DSiHeader.Crypto.IconTitleSha1HmacHash);
        ew.Write(DSiHeader.Crypto.Arm9iDecryptedSha1HmacHash);
        ew.Write(DSiHeader.Crypto.Arm7iDecryptedSha1HmacHash);
        ew.Write(DSiHeader.Crypto.ReservedA);
        ew.Write(DSiHeader.Crypto.ReservedB);
        ew.Write(DSiHeader.Crypto.Arm9WithoutSecureAreaSha1HmacHash);
        ew.Write(DSiHeader.Crypto.ReservedC);
        ew.Write(DSiHeader.Crypto.ReservedD);
        ew.Write(DSiHeader.Crypto.RsaSha1Signature);
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
        Reserved = 0, // NDS
        /// <summary>
        /// Must set for DSi titles
        /// </summary>
        HasTwlExclusiveRegion = 1 << 0,
        /// <summary>
        /// If set, the ROM is modcrypted
        /// </summary>
        Modcrypted = 1 << 1,
        /// <summary>
        /// Clear = Retail, Set = Debug
        /// </summary>
        ModcryptKeySelect = 1 << 2,
        /// <summary>
        /// Unknown on gbatek
        /// </summary>
        DisableDebug = 1 << 3, // ? on gbatek lol
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
    /// On DSi ROMs, indicates the ROM's region
    /// </summary>
    [Flags]
    public enum DsiRegionFlag : uint
    {
        /// <summary>
        /// Japan
        /// </summary>
        JPN = 1 << 0,
        /// <summary>
        /// United States of America
        /// </summary>
        USA = 1 << 1,
        /// <summary>
        /// Europe
        /// </summary>
        EUR = 1 << 2,
        /// <summary>
        /// Australia
        /// </summary>
        AUS = 1 << 3,
        /// <summary>
        /// China
        /// </summary>
        CHN = 1 << 4,
        /// <summary>
        /// Korea
        /// </summary>
        KOR = 1 << 5,
        /// <summary>
        /// Region-free
        /// </summary>
        RegionFree = 0xFFFFFFFF,
    }

    /// <summary>
    /// Extra flags specified on DSi ROMs
    /// </summary>
    public enum DsiExtraFlag : byte
    {
        /// <summary>
        /// TSC Touchscreen/Sound Controller Mode (Clear: NDS, Set: DSi)
        /// </summary>
        TouchscreenSoundControllerMode = 1 << 0,
        /// <summary>
        /// If set, require EULA Agreement
        /// </summary>
        RequireEulaAgreement = 1 << 1,
        /// <summary>
        /// Custom Icon (Clear: No/Normal, Set: Use banner.sav
        /// </summary>
        CustomIcon = 1 << 2,
        /// <summary>
        /// Show Nintendo Wi-Fi Connection icon in Launcher
        /// </summary>
        ShowWifiConnectionIconInLauncher = 1 << 3,
        /// <summary>
        /// Show DS Wireless icon in Launcher
        /// </summary>
        ShowDsWirelessIconInLauncher = 1 << 4,
        /// <summary>
        /// NDS cart with icon SHA1  (DSi firmware v1.4 and up)
        /// </summary>
        NdsCartWithIconSha1 = 1 << 5,
        /// <summary>
        /// NDS cart with header RSA (DSi firmware v1.0 and up)
        /// </summary>
        NdsCartWithHeaderRsa = 1 << 6,
        /// <summary>
        /// Developer App
        /// </summary>
        DeveloperApp = 1 << 7,
    }

    /// <summary>
    /// Values for the filetype identified for the ROM
    /// </summary>
    public enum TitleIdType : byte
    {
        /// <summary>
        /// DSi ROM Cartridges
        /// </summary>
        Cartridge = 0x00,
        /// <summary>
        /// DSiware (browser, flipnote, and games)
        /// </summary>
        DSiWare = 0x04,
        /// <summary>
        /// System Fun Tools (Camera, Sound, Zone, Pictochat, DS Download Play)
        /// </summary>
        SystemFunTools = 0x05,
        /// <summary>
        /// System Data (non-executable, without cart header)
        /// </summary>
        NonExecutableDataFile = 0x0F,
        /// <summary>
        /// System Base Tools (system settings, DSi Shop, 3DS transfer tool)
        /// </summary>
        SystemBaseTools = 0x15,
        /// <summary>
        /// System Menu (launcher)
        /// </summary>
        SystemMenu = 0x17,
    }
}