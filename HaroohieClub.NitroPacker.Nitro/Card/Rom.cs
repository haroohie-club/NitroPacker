using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using HaroohieClub.NitroPacker.IO;
using HaroohieClub.NitroPacker.IO.Archive;
using HaroohieClub.NitroPacker.IO.Serialization;
using HaroohieClub.NitroPacker.Nitro.Fs;
using HaroohieClub.NitroPacker.Nitro.Gx;

namespace HaroohieClub.NitroPacker.Nitro.Card;

public class Rom
{
    public Rom() { }

    public Rom(byte[] data)
        : this(new MemoryStream(data)) { }

    public Rom(Stream stream)
    {
        using (var er = new EndianBinaryReaderEx(stream, Endianness.LittleEndian))
        {
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

            er.BaseStream.Position = Header.MainRomOffset;
            MainRom = er.Read<byte>((int)Header.MainSize);
            if (er.Read<uint>() == 0xDEC00621) //Nitro Footer
            {
                er.BaseStream.Position -= 4;
                StaticFooter = new(er);
            }

            er.BaseStream.Position = Header.SubRomOffset;
            SubRom = er.Read<byte>((int)Header.SubSize);

            er.BaseStream.Position = Header.FntOffset;
            Fnt = new(er);

            er.BaseStream.Position = Header.MainOvtOffset;
            MainOvt = new RomOVT[Header.MainOvtSize / 32];
            for (int i = 0; i < Header.MainOvtSize / 32; i++) MainOvt[i] = new(er);

            er.BaseStream.Position = Header.SubOvtOffset;
            SubOvt = new RomOVT[Header.SubOvtSize / 32];
            for (int i = 0; i < Header.SubOvtSize / 32; i++) SubOvt[i] = new(er);

            er.BaseStream.Position = Header.FatOffset;
            Fat = new FatEntry[Header.FatSize / 8];
            for (int i = 0; i < Header.FatSize / 8; i++)
                Fat[i] = new(er);

            if (Header.BannerOffset != 0)
            {
                er.BaseStream.Position = Header.BannerOffset;
                Banner = new(er);
            }

            byte[][] fileData = new byte[Header.FatSize / 8][];
            for (int i = 0; i < Header.FatSize / 8; i++)
            {
                er.BaseStream.Position = Fat[i].FileTop;
                fileData[i] = er.Read<byte>((int)Fat[i].FileSize);
            }

            FileData = fileData.Select(t => new NameFatWithData(t)).ToArray();

            //RSA Signature
            if (Header.RomSize + 0x88 <= er.BaseStream.Length)
            {
                er.BaseStream.Position = Header.RomSize;
                byte[] rsaSig = er.Read<byte>(0x88);
                if (rsaSig[0] == 'a' && rsaSig[1] == 'c')
                    RSASignature = rsaSig;
            }
        }
    }

    public byte[] Write(bool trimmed = true)
    {
        using (var m = new MemoryStream())
        {
            Write(m, trimmed);

            return m.ToArray();
        }
    }

    public void Write(Stream stream, bool trimmed = true)
    {
        using (var er = new EndianBinaryWriterEx(stream, Endianness.LittleEndian))
        {
            //Header
            //skip the header, and write it afterwards
            if (PTable != null && SBoxes != null && PTable.Any(p => p != 0))
            {
                er.BaseStream.Position = 0x1000;
                if (KeyPadding0 != null)
                {
                    if (KeyPadding0.Length != 0x600)
                        throw new();
                    er.Write(KeyPadding0);
                }
                else
                    er.BaseStream.Position += 0x600;

                if (PTable.Length != Blowfish.PTableEntryCount)
                    throw new();
                er.Write(PTable);

                if (KeyPadding1 != null)
                {
                    if (KeyPadding1.Length != 0x5B8)
                        throw new();
                    er.Write(KeyPadding1);
                }
                else
                    er.BaseStream.Position += 0x5B8;

                if (SBoxes.Length != Blowfish.SBoxCount)
                    throw new();

                for (int i = 0; i < Blowfish.SBoxCount; i++)
                {
                    if (SBoxes[i] == null || SBoxes[i].Length != Blowfish.SBoxEntryCount)
                        throw new();
                    er.Write(SBoxes[i]);
                }

                if (KeyPadding2 != null)
                {
                    if (KeyPadding2.Length != 0x400)
                        throw new();
                    er.Write(KeyPadding2);
                }
                else
                    er.BaseStream.Position += 0x400;

                // test patterns
                er.Write(new byte[] { 0xFF, 0x00, 0xFF, 0x00, 0xAA, 0x55, 0xAA, 0x55 });

                for (int i = 8; i < 0x200; i++)
                    er.Write((byte)(i & 0xFF));

                for (int i = 0; i < 0x200; i++)
                    er.Write((byte)(0xFF - (i & 0xFF)));

                for (int i = 0; i < 0x200; i++)
                    er.Write((byte)0);

                for (int i = 0; i < 0x200; i++)
                    er.Write((byte)0xFF);

                for (int i = 0; i < 0x200; i++)
                    er.Write((byte)0x0F);

                for (int i = 0; i < 0x200; i++)
                    er.Write((byte)0xF0);

                for (int i = 0; i < 0x200; i++)
                    er.Write((byte)0x55);

                for (int i = 0; i < 0x1FF; i++)
                    er.Write((byte)0xAA);

                er.Write((byte)0);
            }

            er.BaseStream.Position = 0x4000;
            Header.HeaderSize = (uint)er.BaseStream.Position;
            //MainRom
            Header.MainRomOffset = (uint)er.BaseStream.Position;
            Header.MainSize = (uint)MainRom.Length;
            er.Write(MainRom, 0, MainRom.Length);
            //Static Footer
            StaticFooter?.Write(er);
            if (MainOvt.Length != 0)
            {
                er.WritePadding(0x200, 0xFF);
                //Main Ovt
                Header.MainOvtOffset = (uint)er.BaseStream.Position;
                Header.MainOvtSize = (uint)MainOvt.Length * 0x20;
                foreach (RomOVT v in MainOvt)
                {
                    v.Compressed = (uint)FileData[v.FileId].Data.Length;
                    v.Write(er);
                }
                foreach (RomOVT v in MainOvt)
                {
                    er.WritePadding(0x200, 0xFF);
                    Fat[v.FileId].FileTop = (uint)er.BaseStream.Position;
                    Fat[v.FileId].FileBottom = (uint)er.BaseStream.Position + (uint)FileData[v.FileId].Data.Length;
                    er.Write(FileData[v.FileId].Data, 0, FileData[v.FileId].Data.Length);
                }
            }
            else
            {
                Header.MainOvtOffset = 0;
                Header.MainOvtSize = 0;
            }

            er.WritePadding(0x200, 0xFF);
            //SubRom
            Header.SubRomOffset = (uint)er.BaseStream.Position;
            Header.SubSize = (uint)SubRom.Length;
            er.Write(SubRom, 0, SubRom.Length);
            //I assume this works the same as the main ovt?
            if (SubOvt.Length != 0)
            {
                er.WritePadding(0x200, 0xFF);
                //Sub Ovt
                Header.SubOvtOffset = (uint)er.BaseStream.Position;
                Header.SubOvtSize = (uint)SubOvt.Length * 0x20;
                foreach (RomOVT v in SubOvt)
                    v.Write(er);
                foreach (RomOVT v in SubOvt)
                {
                    er.WritePadding(0x200, 0xFF);
                    Fat[v.FileId].FileTop = (uint)er.BaseStream.Position;
                    Fat[v.FileId].FileBottom = (uint)er.BaseStream.Position + (uint)FileData[v.FileId].Data.Length;
                    er.Write(FileData[v.FileId].Data, 0, FileData[v.FileId].Data.Length);
                }
            }
            else
            {
                Header.SubOvtOffset = 0;
                Header.SubOvtSize = 0;
            }

            er.WritePadding(0x200, 0xFF);
            //FNT
            Header.FntOffset = (uint)er.BaseStream.Position;
            Fnt.Write(er);
            Header.FntSize = (uint)er.BaseStream.Position - Header.FntOffset;
            er.WritePadding(0x200, 0xFF);
            //FAT
            Header.FatOffset = (uint)er.BaseStream.Position;
            Header.FatSize = (uint)Fat.Length * 8;
            //Skip the fat, and write it after writing the data itself
            er.BaseStream.Position += Header.FatSize;
            //Banner
            if (Banner != null)
            {
                er.WritePadding(0x200, 0xFF);
                Header.BannerOffset = (uint)er.BaseStream.Position;
                Banner.Write(er);
            }
            else
                Header.BannerOffset = 0;

            //Files
            if (FileData.All(f => f.NameFat is null))
            {
                for (int i = (int)(Header.MainOvtSize / 32 + Header.SubOvtSize / 32); i < FileData.Length; i++)
                {
                    er.WritePadding(0x200, 0xFF);
                    Fat[i].FileTop = (uint)er.BaseStream.Position;
                    Fat[i].FileBottom = (uint)er.BaseStream.Position + (uint)FileData[i].Data.Length;
                    er.Write(FileData[i].Data, 0, FileData[i].Data.Length);
                }
            }
            else
            {
                foreach ((int i, byte[] data) in FileData.Skip((int)(Header.MainOvtSize / 32 + Header.SubOvtSize / 32))
                             .Select((f, i) => (f, i))
                             .OrderBy(t => t.f.NameFat?.FatOffset ?? 0).Select(t => (t.i, t.f.Data)))
                {
                    int idx = i + (int)(Header.MainOvtSize / 32 + Header.SubOvtSize / 32);
                    er.WritePadding(0x200, 0xFF);
                    Fat[idx].FileTop = (uint)er.BaseStream.Position;
                    Fat[idx].FileBottom = (uint)er.BaseStream.Position + (uint)data.Length;
                    er.Write(data, 0, data.Length);
                }
            }

            er.WritePadding(4);
            Header.RomSize = (uint)er.BaseStream.Position;
            uint capacitySize = Header.RomSize;
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

            Header.DeviceSize = (byte)(capacity < 0 ? 0 : capacity);
            //RSA
            if (RSASignature != null)
                er.Write(RSASignature, 0, 0x88);

            //if writing untrimmed write padding up to the power of 2 size of the rom
            if (!trimmed)
                er.WritePadding((int)capacitySize, 0xFF);

            //Fat
            er.BaseStream.Position = Header.FatOffset;
            foreach (FatEntry v in Fat)
                v.Write(er);
            //Header
            er.BaseStream.Position = 0;
            Header.Write(er);
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