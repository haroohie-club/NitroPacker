using System;
using System.IO;
using System.Linq;
using HaroohieClub.NitroPacker.IO;
using HaroohieClub.NitroPacker.IO.Archive;
using HaroohieClub.NitroPacker.Nitro.Card.Cryptography;
using HaroohieClub.NitroPacker.Nitro.Card.Headers;
using HaroohieClub.NitroPacker.Nitro.Fs;

namespace HaroohieClub.NitroPacker.Nitro.Card;

/// <summary>
/// Representation of an NDS ROM file
/// </summary>
public class Rom
{
    /// <summary>
    /// Blank constructor used for serialization
    /// </summary>
    public Rom() { }

    /// <summary>
    /// Constructs a ROM from binary data
    /// </summary>
    /// <param name="data">A byte array of the ROM's binary</param>
    public Rom(byte[] data)
        : this(new MemoryStream(data)) { }

    /// <summary>
    /// Constructs the ROM from a stream
    /// </summary>
    /// <param name="stream">A stream to the ROM</param>
    public Rom(Stream stream)
    {
        using EndianBinaryReaderEx er = new(stream, Endianness.LittleEndian);
        
        Header = new(er);
        
        if (er.BaseStream.Length >= 0x4000)
        {
            er.BaseStream.Position = 0x1000;

            KeyPadding0 = er.Read<byte>(0x600);
            PTable = er.Read<uint>(Blowfish.PTableEntryCount);
            KeyPadding1 = er.Read<byte>(0x5B8);

            SBoxes = new uint[Blowfish.SBoxCount][];
            for (int i = 0; i < Blowfish.SBoxCount; i++)
                SBoxes[i] = er.Read<uint>(Blowfish.SBoxEntryCount);

            KeyPadding2 = er.Read<byte>(0x400);
        }

        er.BaseStream.Position = Header.Arm9RomOffset;
        Arm9Binary = er.Read<byte>((int)Header.Arm9Size);
        if (er.Read<uint>() == 0xDEC00621) //Nitro Footer
        {
            er.BaseStream.Position -= 4;
            StaticFooter = new(er);
        }

        er.BaseStream.Position = Header.Arm7RomOffset;
        Arm7Binary = er.Read<byte>((int)Header.Arm7Size);

        er.BaseStream.Position = Header.FntOffset;
        FileNameTable = new(er);

        er.BaseStream.Position = Header.Arm9OverlayTableOffset;
        Arm9OverlayTable = new RomOverlayTable[Header.Arm9OverlayTableSize / 32];
        for (int i = 0; i < Header.Arm9OverlayTableSize / 32; i++) Arm9OverlayTable[i] = new(er);

        er.BaseStream.Position = Header.Arm7OverlayTableOffset;
        Arm7OverlayTable = new RomOverlayTable[Header.Arm7OverlayTableSize / 32];
        for (int i = 0; i < Header.Arm7OverlayTableSize / 32; i++) Arm7OverlayTable[i] = new(er);

        er.BaseStream.Position = Header.FatOffset;
        Fat = new FatEntry[Header.FatSize / 8];
        for (int i = 0; i < Header.FatSize / 8; i++)
            Fat[i] = new(er);

        if (Header.IconTitleOffset != 0)
        {
            er.BaseStream.Position = Header.IconTitleOffset;
            Banner = new(er);
        }

        var fileData = new byte[Header.FatSize / 8][];
        for (int i = 0; i < Header.FatSize / 8; i++)
        {
            er.BaseStream.Position = Fat[i].FileTop;
            fileData[i] = er.Read<byte>((int)Fat[i].FileSize);
        }

        FileData = fileData.Select(t => new NameFatWithData(t)).ToArray();

        if (Header.DSiHeader?.DigestSectorHashtableOffset > 0)
        {
            er.BaseStream.Position = Header.DSiHeader.DigestSectorHashtableOffset;
            DigestSectorHashtableBinary = er.Read<byte>((int)Header.DSiHeader.DigestSectorHashtableLength);
        }
        if (Header.DSiHeader?.DigestBlockHashtableOffset > 0)
        {
            er.BaseStream.Position = Header.DSiHeader.DigestBlockHashtableOffset;
            DigestBlockHashtableBinary = er.Read<byte>((int)Header.DSiHeader.DigestBlockHashtableLength);
        }
        
        if (Header.DSiHeader?.Arm9iRomOffset > 0)
        {
            er.BaseStream.Position = Header.DSiHeader.Arm9iRomOffset - 0x3000;
            TwlStaticHeader = er.Read<byte>(0x3000);
            Arm9iBinary = er.Read<byte>((int)Header.DSiHeader.Arm9iSize);
        }
        if (Header.DSiHeader?.Arm7iRomOffset > 0)
        {
            er.BaseStream.Position = Header.DSiHeader.Arm7iRomOffset;
            Arm7iBinary = er.Read<byte>((int)Header.DSiHeader.Arm7iSize);
        }
        
        // Extra data for DSiWare
        er.BaseStream.Position = 0x1000;
        if (er.Read<byte>(0x10).Any(b => b != 0x00))
        {
            er.BaseStream.Position = 0x1000;
            DSiWareExtraData = er.Read<byte>(0x3000);
        }
        
        //RSA Signature
        if (Header.RomSizeExcludingDSiArea + 0x88 <= er.BaseStream.Length)
        {
            er.BaseStream.Position = Header.RomSizeExcludingDSiArea;
            byte[] rsaSig = er.Read<byte>(0x88);
            if (rsaSig[0] == 'a' && rsaSig[1] == 'c')
                RsaSignature = rsaSig;
        }
    }

    /// <summary>
    /// Returns a binary representation of the ROM to be written to disk
    /// </summary>
    /// <param name="trimmed">If set, trims the junk data off the end of the ROM</param>
    /// <returns>Returns a byte array containing the binary data representing the ROM</returns>
    public byte[] Write(bool trimmed = true)
    {
        using (var m = new MemoryStream())
        {
            Write(m, trimmed);

            return m.ToArray();
        }
    }

    /// <summary>
    /// Writes the ROM to a stream
    /// </summary>
    /// <param name="stream">The stream to write to</param>
    /// <param name="trimmed">If set, trims the junk data off the end of the ROM</param>
    /// <exception cref="Exception">Throws if the ROM is invalid</exception>
    public void Write(Stream stream, bool trimmed = true)
    {
        using (var ew = new EndianBinaryWriterEx(stream, Endianness.LittleEndian))
        {
            //Header
            //skip the header, and write it afterward
            if (PTable != null && SBoxes != null && PTable.Any(p => p != 0))
            {
                ew.BaseStream.Position = 0x1000;
                if (KeyPadding0 != null)
                {
                    if (KeyPadding0.Length != 0x600)
                        throw new();
                    ew.Write(KeyPadding0);
                }
                else
                    ew.BaseStream.Position += 0x600;

                if (PTable.Length != Blowfish.PTableEntryCount)
                    throw new();
                ew.Write(PTable);

                if (KeyPadding1 != null)
                {
                    if (KeyPadding1.Length != 0x5B8)
                        throw new();
                    ew.Write(KeyPadding1);
                }
                else
                    ew.BaseStream.Position += 0x5B8;

                if (SBoxes.Length != Blowfish.SBoxCount)
                    throw new();

                for (int i = 0; i < Blowfish.SBoxCount; i++)
                {
                    if (SBoxes[i] == null || SBoxes[i].Length != Blowfish.SBoxEntryCount)
                        throw new();
                    ew.Write(SBoxes[i]);
                }

                if (KeyPadding2 != null)
                {
                    if (KeyPadding2.Length != 0x400)
                        throw new();
                    ew.Write(KeyPadding2);
                }
                else
                    ew.BaseStream.Position += 0x400;

                // test patterns
                ew.Write(new byte[] { 0xFF, 0x00, 0xFF, 0x00, 0xAA, 0x55, 0xAA, 0x55 });

                for (int i = 8; i < 0x200; i++)
                    ew.Write((byte)(i & 0xFF));

                for (int i = 0; i < 0x200; i++)
                    ew.Write((byte)(0xFF - (i & 0xFF)));

                for (int i = 0; i < 0x200; i++)
                    ew.Write((byte)0);

                for (int i = 0; i < 0x200; i++)
                    ew.Write((byte)0xFF);

                for (int i = 0; i < 0x200; i++)
                    ew.Write((byte)0x0F);

                for (int i = 0; i < 0x200; i++)
                    ew.Write((byte)0xF0);

                for (int i = 0; i < 0x200; i++)
                    ew.Write((byte)0x55);

                for (int i = 0; i < 0x1FF; i++)
                    ew.Write((byte)0xAA);

                ew.Write((byte)0);
            }

            ew.BaseStream.Position = 0x4000;
            Header.HeaderSize = (uint)ew.BaseStream.Position;
            //MainRom
            Header.Arm9RomOffset = (uint)ew.BaseStream.Position;
            Header.Arm9Size = (uint)Arm9Binary.Length;
            ew.Write(Arm9Binary, 0, Arm9Binary.Length);
            //Static Footer
            StaticFooter?.Write(ew);
            
            if (Arm9OverlayTable.Length != 0)
            {
                ew.WritePadding(0x200, 0xFF);
                //Main Ovt
                Header.Arm9OverlayTableOffset = (uint)ew.BaseStream.Position;
                Header.Arm9OverlayTableSize = (uint)Arm9OverlayTable.Length * 0x20;
                foreach (RomOverlayTable v in Arm9OverlayTable)
                {
                    if (v.Flag.HasFlag(RomOverlayTable.OverlayTableFlag.Compressed))
                    {
                        v.Compressed = (uint)FileData[v.FileId].Data.Length;
                    }
                    v.Write(ew);
                }
                foreach (RomOverlayTable v in Arm9OverlayTable)
                {
                    ew.WritePadding(0x200, 0xFF);
                    Fat[v.FileId].FileTop = (uint)ew.BaseStream.Position;
                    Fat[v.FileId].FileBottom = (uint)ew.BaseStream.Position + (uint)FileData[v.FileId].Data.Length;
                    ew.Write(FileData[v.FileId].Data, 0, FileData[v.FileId].Data.Length);
                }
            }
            else
            {
                Header.Arm9OverlayTableOffset = 0;
                Header.Arm9OverlayTableSize = 0;
            }
            
            //SubRom
            ew.WritePadding(0x200, 0xFF);
            Header.Arm7RomOffset = (uint)ew.BaseStream.Position;
            Header.Arm7Size = (uint)Arm7Binary.Length;
            ew.Write(Arm7Binary, 0, Arm7Binary.Length);

            //I assume this works the same as the main ovt?
            if (Arm7OverlayTable.Length != 0)
            {
                ew.WritePadding(0x200, 0xFF);
                //Sub Ovt
                Header.Arm7OverlayTableOffset = (uint)ew.BaseStream.Position;
                Header.Arm7OverlayTableSize = (uint)Arm7OverlayTable.Length * 0x20;
                foreach (RomOverlayTable v in Arm7OverlayTable)
                    v.Write(ew);
                foreach (RomOverlayTable v in Arm7OverlayTable)
                {
                    ew.WritePadding(0x200, 0xFF);
                    Fat[v.FileId].FileTop = (uint)ew.BaseStream.Position;
                    Fat[v.FileId].FileBottom = (uint)ew.BaseStream.Position + (uint)FileData[v.FileId].Data.Length;
                    ew.Write(FileData[v.FileId].Data, 0, FileData[v.FileId].Data.Length);
                }
            }
            else
            {
                Header.Arm7OverlayTableOffset = 0;
                Header.Arm7OverlayTableSize = 0;
            }

            ew.WritePadding(0x200, 0xFF);
            //FNT
            Header.FntOffset = (uint)ew.BaseStream.Position;
            FileNameTable.Write(ew);
            Header.FntSize = (uint)ew.BaseStream.Position - Header.FntOffset;
            ew.WritePadding(0x200, 0xFF);
            //FAT
            Header.FatOffset = (uint)ew.BaseStream.Position;
            Header.FatSize = (uint)Fat.Length * 8;
            //Skip the fat, and write it after writing the data itself
            ew.BaseStream.Position += Header.FatSize;
            //Banner
            if (Banner != null)
            {
                ew.WritePadding(0x200, 0xFF);
                Header.IconTitleOffset = (uint)ew.BaseStream.Position;
                Banner.Write(ew);
            }
            else
            {
                Header.IconTitleOffset = 0;
            }

            //Files
            if (FileData.All(f => f.NameFat is null))
            {
                for (int i = (int)(Header.Arm9OverlayTableSize / 32 + Header.Arm7OverlayTableSize / 32); i < FileData.Length; i++)
                {
                    ew.WritePadding(0x200, 0xFF);
                    Fat[i].FileTop = (uint)ew.BaseStream.Position;
                    Fat[i].FileBottom = (uint)ew.BaseStream.Position + (uint)FileData[i].Data.Length;
                    ew.Write(FileData[i].Data, 0, FileData[i].Data.Length);
                }
            }
            else
            {
                foreach ((int i, byte[] data) in FileData.Skip((int)(Header.Arm9OverlayTableSize / 32 + Header.Arm7OverlayTableSize / 32))
                             .Select((f, i) => (f, i))
                             .OrderBy(t => t.f.NameFat?.FatOffset ?? 0).Select(t => (t.i, t.f.Data)))
                {
                    int idx = i + (int)(Header.Arm9OverlayTableSize / 32 + Header.Arm7OverlayTableSize / 32);
                    ew.WritePadding(0x200, 0xFF);
                    Fat[idx].FileTop = (uint)ew.BaseStream.Position;
                    Fat[idx].FileBottom = (uint)ew.BaseStream.Position + (uint)data.Length;
                    ew.Write(data, 0, data.Length);
                }
            }

            ew.WritePadding(4);
            Header.RomSizeExcludingDSiArea = (uint)ew.BaseStream.Position;

            //RSA
            // if (RsaSignature != null)
            //     ew.Write(RsaSignature, 0, 0x88);
            
            ew.WritePadding(0x400, 0xFF);
            if (DigestSectorHashtableBinary?.Length > 0)
            {
                Header.DSiHeader.DigestNTRRegionOffset = Header.Arm9RomOffset;
                Header.DSiHeader.DigestNTRRegionLength = (uint)ew.BaseStream.Position - Header.DSiHeader.DigestNTRRegionOffset;
                
                Header.DSiHeader.DigestSectorHashtableOffset = (uint)ew.BaseStream.Position;
                Header.DSiHeader.DigestSectorHashtableLength = (uint)DigestSectorHashtableBinary.Length;
                ew.Write(DigestSectorHashtableBinary, 0, DigestSectorHashtableBinary.Length);
                ew.WritePadding(0x400, 0xFF);
            }
            if (DigestBlockHashtableBinary?.Length > 0)
            {
                Header.DSiHeader.DigestBlockHashtableOffset = (uint)ew.BaseStream.Position;
                Header.DSiHeader.DigestBlockHashtableLength = (uint)DigestBlockHashtableBinary.Length;
                ew.Write(DigestBlockHashtableBinary, 0, DigestBlockHashtableBinary.Length);
                ew.WritePadding(0x200, 0xFF);
            }

            //Fat
            ew.BaseStream.Position = Header.FatOffset;
            foreach (FatEntry v in Fat)
                v.Write(ew);

            ew.BaseStream.Position = Header.RomSizeExcludingDSiArea;
            ew.WritePadding(0x3000, 0xFF);
            byte[] padding = new byte[0x1000];
            Array.Fill<byte>(padding, 0xFF);
            while ($"{ew.BaseStream.Position:X8}"[^5..] != "03000")
                ew.Write(padding);
            
            //TWL section
            if (Arm9iBinary?.Length > 0)
            {
                if (TwlStaticHeader?.Length > 0)
                {
                    ew.BaseStream.Seek(-0x3000, SeekOrigin.Current);
                    ew.Write(TwlStaticHeader);
                }
                Header.DSiHeader.DigestTWLRegionOffset = (uint)ew.BaseStream.Position;
                Header.DSiHeader.Arm9iRomOffset = (uint)ew.BaseStream.Position;
                Header.DSiHeader.Arm9iSize = (uint)Arm9iBinary.Length;
                ew.Write(Arm9iBinary, 0, Arm9iBinary.Length);
            }
            
            if (Arm7iBinary?.Length > 0)
            {
                ew.WritePadding(0x400, 0xFF);
                Header.DSiHeader.Arm7iRomOffset = (uint)ew.BaseStream.Position;
                Header.DSiHeader.Arm7iSize = (uint)Arm7iBinary.Length;
                ew.Write(Arm7iBinary, 0, Arm7iBinary.Length);
                ew.WritePadding(0x400, 0xFF);
                Header.DSiHeader.DigestTWLRegionLength = (uint)ew.BaseStream.Position - Header.DSiHeader.DigestTWLRegionOffset;
                Header.DSiHeader.TotalRomSizeIncludingDSiArea = (uint)ew.BaseStream.Position;
            }

            uint capacitySize = (Header.DSiHeader?.TotalRomSizeIncludingDSiArea ?? 0) == 0 ? Header.RomSizeExcludingDSiArea : Header.DSiHeader.TotalRomSizeIncludingDSiArea;
            capacitySize |= capacitySize >> 16;
            capacitySize |= capacitySize >> 8;
            capacitySize |= capacitySize >> 4;
            capacitySize |= capacitySize >> 2;
            capacitySize |= capacitySize >> 1;
            capacitySize++;
            if (capacitySize < 0x20000)
                capacitySize = 0x20000;
            uint capacitySize2 = capacitySize;
            int capacity = -18;
            while (capacitySize2 != 0)
            {
                capacitySize2 >>= 1;
                capacity++;
            }

            Header.DeviceCapacity = (byte)(capacity < 0 ? 0 : capacity);
            //if writing untrimmed write padding up to the power of 2 size of the rom
            if (!trimmed)
                ew.WritePadding((int)capacitySize, 0xFF);

            if (DSiWareExtraData?.Length > 0)
            {
                ew.BaseStream.Position = 0x1000;
                ew.Write(DSiWareExtraData, 0, DSiWareExtraData.Length);
            }
            
            //Header
            ew.BaseStream.Position = 0;
            Header.Write(ew);
        }
    }

    /// <summary>
    /// ROM header (<see cref="RomHeader"/>)
    /// </summary>
    public RomHeader Header { get; set; }

    /// <summary>
    /// TODO
    /// </summary>
    public byte[] KeyPadding0 { get; set; }
    /// <summary>
    /// TODO
    /// </summary>
    public uint[] PTable { get; set; }
    /// <summary>
    /// TODO
    /// </summary>
    public byte[] KeyPadding1 { get; set; }
    /// <summary>
    /// TODO
    /// </summary>
    public uint[][] SBoxes { get; set; }
    /// <summary>
    /// TODO
    /// </summary>
    public byte[] KeyPadding2 { get; set; }

    /// <summary>
    /// The contents of the game's arm9.bin
    /// </summary>
    public byte[] Arm9Binary { get; set; }
    /// <summary>
    /// Static footer for ARM9
    /// </summary>
    public NitroFooter StaticFooter { get; set; }
    
    /// <summary>
    /// The contents of the game's arm7.bin
    /// </summary>
    public byte[] Arm7Binary { get; set; }
    /// <summary>
    /// File name table
    /// </summary>
    public RomFileNameTable FileNameTable { get; set; }

    /// <summary>
    /// The ARM9 overlay table
    /// </summary>
    public RomOverlayTable[] Arm9OverlayTable { get; set; }
    /// <summary>
    /// The ARM7 overlay table
    /// </summary>
    public RomOverlayTable[] Arm7OverlayTable { get; set; }

    /// <summary>
    /// File allocation table
    /// </summary>
    public FatEntry[] Fat { get; set; }
    /// <summary>
    /// The ROM's banner/title/icon
    /// </summary>
    public RomBanner Banner { get; set; }

    /// <summary>
    /// NitroFS file data
    /// </summary>
    public NameFatWithData[] FileData { get; set; }
    
    /// <summary>
    /// For DSi and DSi-enhanced games, the contents of the digest sector hashtable
    /// </summary>
    // TODO: Replace with actual sector hashing?
    public byte[] DigestSectorHashtableBinary { get; set; }
    
    /// <summary>
    /// For DSi and DSi-enhanced games, the contents of the digest block hashtable
    /// </summary>
    public byte[] DigestBlockHashtableBinary { get; set; }
    
    /// <summary>
    /// For DSi and DSi-enhanced games, the contents of the 0x3000 bytes before arm9i.bin
    /// </summary>
    public byte[] TwlStaticHeader { get; set; }
    
    /// <summary>
    /// For DSi and DSi-enhanced games, the contents of the game's arm9i.bin
    /// </summary>
    public byte[] Arm9iBinary { get; set; }

    /// <summary>
    /// For DSi and DSi-enhanced games, the contents of the game's arm7i.bin
    /// </summary>
    public byte[] Arm7iBinary { get; set; }

    /// <summary>
    /// RSA signature
    /// </summary>
    public byte[] RsaSignature { get; set; }

    /// <summary>
    /// DSiWare contains a confusing binary section that needs to be preserved
    /// </summary>
    public byte[] DSiWareExtraData { get; set; }

    /// <summary>
    /// Generates a Nitro filesystem archive from a ROM
    /// </summary>
    /// <returns>A <see cref="NitroFsArchive"/> with the contents of the ROM</returns>
    public NitroFsArchive ToArchive()
    {
        return new(FileNameTable.DirectoryTable, FileNameTable.NameTable, FileData);
    }

    /// <summary>
    /// Packs an arbitrary archive into a ROM
    /// </summary>
    /// <param name="archive">The generic archive to pack into the ROM</param>
    /// <param name="nameFat">Optionally, a table correlating name table entries to FAT entries
    /// (used for maintaining file offset structure)</param>
    public void FromArchive(Archive archive, NameEntryWithFatEntry[] nameFat = null)
    {
        int nrOverlays = Arm9OverlayTable.Length + Arm7OverlayTable.Length;
        
        NitroFsArchive nitroArc = new(archive, (ushort)nrOverlays, nameFat?.ToList());
        FileNameTable.DirectoryTable = nitroArc.DirTable;
        FileNameTable.NameTable = nitroArc.NameTable;

        int nrFiles = nitroArc.FileData.Length;

        var fat = new FatEntry[nrOverlays + nrFiles];
        Array.Copy(Fat, fat, nrOverlays);
        Fat = fat;

        var fileData = new byte[nrOverlays + nrFiles][];
        Array.Copy(FileData.Select(f => f.Data).ToArray(), fileData, nrOverlays);
        FileData = fileData.Select(t => new NameFatWithData(t)).ToArray();

        for (int i = nrOverlays; i < nrFiles + nrOverlays; i++)
        {
            Fat[i] = new(0, 0);
            FileData[i] = nitroArc.FileData[i - nitroArc.FileIdOffset];
        }
    }
}