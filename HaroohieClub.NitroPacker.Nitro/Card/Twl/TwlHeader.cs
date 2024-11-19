using HaroohieClub.NitroPacker.IO.Serialization;

namespace HaroohieClub.NitroPacker.Nitro.Card.Twl;

/// <summary>
/// Class representing the TWL/DSi portion of the ROM header
/// </summary>
public class TwlHeader
{
    /// <summary>
    /// [DSi] Global MBK1..MBK5 Setting, WRAM Slots
    /// </summary>
    [ArraySize(0x14)]
    public byte[] GlobalWramSlots { get; set; }
    /// <summary>
    /// [DSi] Local ARM9 MBK6..MBK8 Setting, WRAM Areas
    /// </summary>
    [ArraySize(0x0C)]
    public byte[] Arm9WramAreas { get; set; }
    /// <summary>
    /// [DSi] Local ARM7 MBK6..MBK8 Setting, WRAM Areas
    /// </summary>
    [ArraySize(0x0C)]
    public byte[] Arm7WramAreas { get; set; }
    /// <summary>
    /// [DSi] Global MBK9 Setting, WRAM Slot Write Protect (24-bit integer)
    /// </summary>
    [ArraySize(3)]
    public byte[] GlobalWramSlotWriteProtect { get; set; }
    /// <summary>
    /// [DSi] Global WRAMCNT Setting (usually 03h) (FCh/00h in SysMenu/Settings)
    /// </summary>
    public byte GlobalWRAMCNTSetting { get; set; }
    /// <summary>
    /// [DSi] Flags indicating the region of the DSi ROM
    /// </summary>
    public RomHeader.DsiRegionFlag DsiRegionFlags { get; set; }
    /// <summary>
    /// [DSi] Access control (AES Key Select)
    ///
    /// Notes from gbatek:
    /// bit0 Common Client Key ;want 0x380F000=0x3FFC600+0x00 "common key"
    /// bit1 AES Slot B  ;0x380F010=0x3FFC400+0x180 and KEY1=unchanged
    ///     bit2 AES Slot C  ;0x380F020=0x3FFC400+0x190 and KEY2.Y=0x3FFC400+0x1A0
    ///     bit3 SD Card            ;want Device I
    ///     bit4 NAND Access        ;want Device A-H and KEY3=intact
    ///     bit5 Game Card Power On                 ;tested with bit8
    ///     bit6 Shared2 File                       ;used... but WHAT for?
    /// bit7 Sign JPEG For Launcher (AES Slot B);select 1 of 2 jpeg keys?
    /// bit8 Game Card NTR Mode                 ;tested with bit5
    ///     bit9 SSL Client Cert (AES Slot A) ;KEY0=0x3FFC600+0x30 (twl-*.der)
    /// bit10 Sign JPEG For User (AES Slot B) ;\
    /// bit11 Photo Read Access               ; seems to be unused
    /// bit12 Photo Write Access              ; (and, usually ZERO,
    /// bit13 SD Card Read Access             ; even if the stuff is
    /// bit14 SD Card Write Access            ; accessed)
    /// bit15 Game Card Save Read Access      ; (bit11 set in flipnote)
    /// bit16 Game Card Save Write Access     ;/
    /// bit31 Debugger Common Client Key  ;want 0x380F000=0x3FFC600+0x10
    /// </summary>
    public uint AccessControl { get; set; }
    /// <summary>
    /// [DSi] ARM7 SCFG_EXT7 setting (bit0,1,2,10,18,31)
    /// </summary>
    public uint Arm7ScfgExt7Setting { get; set; }
    /// <summary>
    /// [DSi] Reserved/flags? (zerofilled)
    /// </summary>
    [ArraySize(3)]
    public byte[] ReservedC { get; set; }
    /// <summary>
    /// [DSi] Flags (usually 0x01) (DSiWare Browser: 0x0B)
    /// </summary>
    public RomHeader.DsiExtraFlag DsiExtraFlags { get; set; }
    
    /// <summary>
    /// [DSi] ARM9i ROM Offset (usually 0xXX03000, XX=1MiB-boundary after NDS area)
    /// </summary>
    public uint Arm9iRomOffset { get; set; }
    /// <summary>
    /// [DSi] Reserved (zero)
    /// </summary>
    public uint ReservedD { get; set; }
    /// <summary>
    /// [DSi] ARM9i RAM Load address
    /// </summary>
    public uint Arm9iRamLoadAddress { get; set; }
    /// <summary>
    /// [DSi] ARM9i Size
    /// </summary>
    public uint Arm9iSize { get; set; }
    /// <summary>
    /// [DSi] ARM7i ROM Offset
    /// </summary>
    public uint Arm7iRomOffset { get; set; }
    /// <summary>
    /// [DSi] SD/MMC Device List ARM7 RAM Addr; 0x400-byte initialized by firmware
    /// </summary>
    public uint SdMmcDeviceListArm7RamAddress { get; set; }
    /// <summary>
    /// [DSi] ARM7i RAM Load address
    /// </summary>
    public uint Arm7iRamLoadAddress { get; set; }
    /// <summary>
    /// [DSi] ARM7i Size
    /// </summary>
    public uint Arm7iSize { get; set; }
    
    /// <summary>
    /// [DSi] Digest NTR region offset (usually same as ARM9 rom offset, 0x0004000)
    /// </summary>
    public uint DigestNTRRegionOffset { get; set; }
    /// <summary>
    /// [DSi] Digest NTR region length
    /// </summary>
    public uint DigestNTRRegionLength { get; set; }
    /// <summary>
    /// [DSi] Digest TWL region offset (usually same as ARM9i rom offset, 0xXX03000)
    /// </summary>
    public uint DigestTWLRegionOffset { get; set; }
    /// <summary>
    /// [DSi] Digest TWL region length
    /// </summary>
    public uint DigestTWLRegionLength { get; set; }
    /// <summary>
    /// [DSi] Digest Sector Hashtable offset (SHA1-HMACs on all sectors)
    /// </summary>
    public uint DigestSectorHashtableOffset { get; set; }
    /// <summary>
    /// [DSi] Digest Sector Hashtable length (in above NTR+TWL regions)
    /// </summary>
    public uint DigestSectorHashtableLength { get; set; }
    /// <summary>
    /// [DSi] Digest Block Hashtable offset (SHA1-HMAC's on each N entries)
    /// </summary>
    public uint DigestBlockHashtableOffset { get; set; }
    /// <summary>
    /// [DSi] Digest Block Hashtable length (in above Sector Hashtable)
    /// </summary>
    public uint DigestBlockHashtableLength { get; set; }
    /// <summary>
    /// [DSi] Digest Sector size (e.g. 0x400 bytes per sector)
    /// </summary>
    public uint DigestSectorSize { get; set; }
    /// <summary>
    /// [DSi] Digest Block sector count (e.g. 0x20 sectors per block)
    /// </summary>
    public uint DigestBlockSectorCount { get; set; }
    
    /// <summary>
    /// [DSi] Icon/Title size (usually 0x23C0 for DSi) (older 0x840-byte works too)
    /// </summary>
    public uint IconTitleSize { get; set; }
    /// <summary>
    /// [DSi] SD/MMC size of "shared2\0000" file in 32KiB units? (DSi sound)
    /// </summary>
    public byte Shared20000Size { get; set; }
    /// <summary>
    /// [DSi] SD/MMC size of "shared2\0001" file in 32Kbyte units?
    /// </summary>
    public byte Shared20001Size { get; set; }
    /// <summary>
    /// [DSi] EULA Version (0x01) ?
    /// </summary>
    public byte EulaVersion { get; set; }
    /// <summary>
    /// [DSi] Use Ratings (0x00)
    /// </summary>
    public byte UseRatings { get; set; }
    /// <summary>
    /// [DSi] Total Used ROM size, INCLUDING DSi area
    /// (optional, can be 0)
    /// </summary>
    public uint TotalRomSizeIncludingDSiArea { get; set; }
    /// <summary>
    /// [DSi] SD/MMC size of "shared2\0002" file in 32Kbyte units?
    /// </summary>
    public byte Shared20002Size { get; set; }
    /// <summary>
    /// [DSi] SD/MMC size of "shared2\0003" file in 32Kbyte units?
    /// </summary>
    public byte Shared20003Size { get; set; }
    /// <summary>
    /// [DSi] SD/MMC size of "shared2\0004" file in 32Kbyte units?
    /// </summary>
    public byte Shared20004Size { get; set; }
    /// <summary>
    /// [DSi] SD/MMC size of "shared2\0005" file in 32Kbyte units?
    /// </summary>
    public byte Shared20005Size { get; set; }
    /// <summary>
    /// [DSi] ARM9i Parameters Table Offset (84 D0 04 00) ??? (base=[0x028])
    /// </summary>
    public uint Arm9iParametersTableOffset { get; set; }
    /// <summary>
    /// [DSi] ARM7i Parameters Table Offset (2C 05 00 00) ???  (base=[038h])
    /// </summary>
    public uint Arm7iParametersTableOffset { get; set; }
    
    /// <summary>
    /// [DSi] Modcrypt area 1 offset (usually same as ARM9i rom offs (0xXX03000))
    /// </summary>
    public uint ModcryptArea1Offset { get; set; }
    /// <summary>
    /// [DSi] Modcrypt area 1 size (usually min(0x4000,ARM9iSize+0xF AND not 0xF)
    /// </summary>
    public uint ModcryptArea1Size { get; set; }
    /// <summary>
    /// [DSi] Modcrypt area 2 offset (0=None)
    /// </summary>
    public uint ModcryptArea2Offset { get; set; }
    /// <summary>
    /// [DSi] Modcrypt area 2 size (0=None)
    /// </summary>
    public uint ModcryptArea2Size { get; set; }
    
    /// <summary>
    /// [DSi] Title ID, Emagcode (aka Gamecode spelled backwards)
    /// </summary>
    public uint TitleIdEmag { get; set; }
    /// <summary>
    /// [DSi] Title ID, Filetype
    /// </summary>
    public RomHeader.TitleIdType TitleIdFiletype { get; set; }
    /// <summary>
    /// [DSi] Title ID, Zero (0x00=Normal)
    /// </summary>
    public byte TitleIdZeroA { get; set; }
    /// <summary>
    /// [DSi] Title ID, Three (0x03=DSi) (as opposed to Wii or 3DS)
    /// </summary>
    public byte TitleIdThree { get; set; }
    /// <summary>
    /// [DSi] Title ID, Zero (0x00=Normal)
    /// </summary>
    public byte TitleIdZeroB { get; set; }

    /// <summary>
    /// [DSi] SD/MMC (DSiWare) "public.sav" filesize in bytes (0=none)
    /// </summary>
    public uint PublicSavSize { get; set; }
    /// <summary>
    /// [DSi] SD/MMC (DSiWare) "private.sav" filesize in bytes (0=none)
    /// </summary>
    public uint PrivateSavSize { get; set; }
    /// <summary>
    /// [DSi] Reserved (zero-filled)
    /// </summary>
    [ArraySize(0xB0)]
    public byte[] ReservedE { get; set; }

    /// <summary>
    /// [DSi] Japanese regional rating for this game
    /// </summary>
    public JapanRegionRating JapanRegionRating { get; set; }
    /// <summary>
    /// [DSi] US/Canadian regional rating for this game
    /// </summary>
    public USCanadaRegionRating USCanadaRegionRating { get; set; }
    /// <summary>
    /// [DSi] Reserved
    /// </summary>
    public byte ReservedRatingA { get; set; }
    /// <summary>
    /// [DSi] German regional rating for this game
    /// </summary>
    public GermanyRegionRating GermanyRegionRating { get; set; }
    /// <summary>
    /// [DSi] European regional rating for this game
    /// </summary>
    public EuropeanRegionRating EuropeanRegionRating { get; set; }
    /// <summary>
    /// [DSi] Reserved
    /// </summary>
    public byte ReserveRatingB { get; set; }
    /// <summary>
    /// [DSi] Portuguese regional rating for this game
    /// </summary>
    public PortugalRegionRating PortugalRegionRating { get; set; }
    /// <summary>
    /// [DSi] British regional rating for this game
    /// </summary>
    public UnitedKingdomRegionRating UnitedKingdomRegionRating { get; set; }
    /// <summary>
    /// [DSi] Australian regional rating for this game
    /// </summary>
    public AustraliaRegionRating AustraliaRegionRating { get; set; }
    /// <summary>
    /// [DSi] South Korean regional rating for this game
    /// </summary>
    public SouthKoreaRegionRating SouthKoreaRegionRating { get; set; }
    /// <summary>
    /// [DSi] Reserved
    /// </summary>
    [ArraySize(6)]
    public byte[] ReservedRatingsC { get; set; }

    /// <summary>
    /// [DSi] Cryptographic section of the DSi header
    /// </summary>
    public Cryptography Crypto { get; set; } = new();
}