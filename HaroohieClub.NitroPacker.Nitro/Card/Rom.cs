using System;
using System.IO;
using System.Linq;
using HaroohieClub.NitroPacker.IO;
using HaroohieClub.NitroPacker.IO.Archive;
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
        MainRom = er.Read<byte>((int)Header.Arm9Size);
        if (er.Read<uint>() == 0xDEC00621) //Nitro Footer
        {
            er.BaseStream.Position -= 4;
            StaticFooter = new(er);
        }

        er.BaseStream.Position = Header.Arm7RomOffset;
        SubRom = er.Read<byte>((int)Header.Arm7Size);

        er.BaseStream.Position = Header.FntOffset;
        Fnt = new(er);

        er.BaseStream.Position = Header.Arm9OvtOffset;
        MainOvt = new RomOVT[Header.Arm9OvtSize / 32];
        for (int i = 0; i < Header.Arm9OvtSize / 32; i++) MainOvt[i] = new(er);

        er.BaseStream.Position = Header.Arm7OvtOffset;
        SubOvt = new RomOVT[Header.Arm7OvtSize / 32];
        for (int i = 0; i < Header.Arm7OvtSize / 32; i++) SubOvt[i] = new(er);

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

        //RSA Signature
        if (Header.RomSizeExcludingDSiArea + 0x88 <= er.BaseStream.Length)
        {
            er.BaseStream.Position = Header.RomSizeExcludingDSiArea;
            byte[] rsaSig = er.Read<byte>(0x88);
            if (rsaSig[0] == 'a' && rsaSig[1] == 'c')
                RSASignature = rsaSig;
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
            Header.Arm9Size = (uint)MainRom.Length;
            ew.Write(MainRom, 0, MainRom.Length);
            //Static Footer
            StaticFooter?.Write(ew);
            if (MainOvt.Length != 0)
            {
                ew.WritePadding(0x200, 0xFF);
                //Main Ovt
                Header.Arm9OvtOffset = (uint)ew.BaseStream.Position;
                Header.Arm9OvtSize = (uint)MainOvt.Length * 0x20;
                foreach (RomOVT v in MainOvt)
                {
                    v.Compressed = (uint)FileData[v.FileId].Data.Length;
                    v.Write(ew);
                }
                foreach (RomOVT v in MainOvt)
                {
                    ew.WritePadding(0x200, 0xFF);
                    Fat[v.FileId].FileTop = (uint)ew.BaseStream.Position;
                    Fat[v.FileId].FileBottom = (uint)ew.BaseStream.Position + (uint)FileData[v.FileId].Data.Length;
                    ew.Write(FileData[v.FileId].Data, 0, FileData[v.FileId].Data.Length);
                }
            }
            else
            {
                Header.Arm9OvtOffset = 0;
                Header.Arm9OvtSize = 0;
            }

            ew.WritePadding(0x200, 0xFF);
            //SubRom
            Header.Arm7RomOffset = (uint)ew.BaseStream.Position;
            Header.Arm7Size = (uint)SubRom.Length;
            ew.Write(SubRom, 0, SubRom.Length);
            //I assume this works the same as the main ovt?
            if (SubOvt.Length != 0)
            {
                ew.WritePadding(0x200, 0xFF);
                //Sub Ovt
                Header.Arm7OvtOffset = (uint)ew.BaseStream.Position;
                Header.Arm7OvtSize = (uint)SubOvt.Length * 0x20;
                foreach (RomOVT v in SubOvt)
                    v.Write(ew);
                foreach (RomOVT v in SubOvt)
                {
                    ew.WritePadding(0x200, 0xFF);
                    Fat[v.FileId].FileTop = (uint)ew.BaseStream.Position;
                    Fat[v.FileId].FileBottom = (uint)ew.BaseStream.Position + (uint)FileData[v.FileId].Data.Length;
                    ew.Write(FileData[v.FileId].Data, 0, FileData[v.FileId].Data.Length);
                }
            }
            else
            {
                Header.Arm7OvtOffset = 0;
                Header.Arm7OvtSize = 0;
            }

            ew.WritePadding(0x200, 0xFF);
            //FNT
            Header.FntOffset = (uint)ew.BaseStream.Position;
            Fnt.Write(ew);
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
                for (int i = (int)(Header.Arm9OvtSize / 32 + Header.Arm7OvtSize / 32); i < FileData.Length; i++)
                {
                    ew.WritePadding(0x200, 0xFF);
                    Fat[i].FileTop = (uint)ew.BaseStream.Position;
                    Fat[i].FileBottom = (uint)ew.BaseStream.Position + (uint)FileData[i].Data.Length;
                    ew.Write(FileData[i].Data, 0, FileData[i].Data.Length);
                }
            }
            else
            {
                foreach ((int i, byte[] data) in FileData.Skip((int)(Header.Arm9OvtSize / 32 + Header.Arm7OvtSize / 32))
                             .Select((f, i) => (f, i))
                             .OrderBy(t => t.f.NameFat?.FatOffset ?? 0).Select(t => (t.i, t.f.Data)))
                {
                    int idx = i + (int)(Header.Arm9OvtSize / 32 + Header.Arm7OvtSize / 32);
                    ew.WritePadding(0x200, 0xFF);
                    Fat[idx].FileTop = (uint)ew.BaseStream.Position;
                    Fat[idx].FileBottom = (uint)ew.BaseStream.Position + (uint)data.Length;
                    ew.Write(data, 0, data.Length);
                }
            }

            ew.WritePadding(4);
            Header.RomSizeExcludingDSiArea = (uint)ew.BaseStream.Position;
            uint capacitySize = Header.RomSizeExcludingDSiArea;
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
            //RSA
            if (RSASignature != null)
                ew.Write(RSASignature, 0, 0x88);

            //if writing untrimmed write padding up to the power of 2 size of the rom
            if (!trimmed)
                ew.WritePadding((int)capacitySize, 0xFF);

            //Fat
            ew.BaseStream.Position = Header.FatOffset;
            foreach (FatEntry v in Fat)
                v.Write(ew);
            //Header
            ew.BaseStream.Position = 0;
            Header.Write(ew);
        }
    }

    public RomHeader Header { get; set; }

    public byte[] KeyPadding0 { get; set; }
    public uint[] PTable { get; set; }
    public byte[] KeyPadding1 { get; set; }
    public uint[][] SBoxes { get; set; }
    public byte[] KeyPadding2 { get; set; }

    public byte[] MainRom { get; set; }
    public NitroFooter StaticFooter { get; set; }
    
    public byte[] SubRom { get; set; }
    public RomFNT Fnt { get; set; }

    public RomOVT[] MainOvt { get; set; }
    public RomOVT[] SubOvt { get; set; }

    public FatEntry[] Fat { get; set; }
    public RomBanner Banner { get; set; }

    public NameFatWithData[] FileData { get; set; }

    public byte[] RSASignature { get; set; }

    public NitroFsArchive ToArchive()
    {
        return new(Fnt.DirectoryTable, Fnt.NameTable, FileData);
    }

    public void FromArchive(Archive archive, NameEntryWithFatEntry[] nameFat = null)
    {
        int nrOverlays = MainOvt.Length + SubOvt.Length;
        
        NitroFsArchive nitroArc = new(archive, (ushort)nrOverlays, nameFat?.ToList());
        Fnt.DirectoryTable = nitroArc.DirTable;
        Fnt.NameTable = nitroArc.NameTable;

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